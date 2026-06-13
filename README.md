# Card Deck Handler

A high-performance .NET 10 card hand evaluation library built around a 64-bit integer representation of a full hand. Designed for speed — hand evaluation uses bitwise operations and population counts rather than object comparisons or sorting algorithms.

## Why Bits?

A standard approach to representing a poker hand might use a `List<Card>` and evaluate by sorting, counting, and comparing objects. This library instead packs an entire hand into a single `ulong`. Every card is one bit. Every evaluation is a mask, a shift, or a population count.

The difference matters at scale. Texas Hold'em equity calculators need to evaluate millions of hands to compute win percentages accurately. At that volume, the overhead of heap allocations, object comparisons, and list iteration adds up fast. With a 64-bit hand:

- **Flush detection** is a `PopCount` on a masked 16-bit value — one instruction on modern CPUs.
- **Straight detection** is a sliding 5-bit mask ANDed against a 14-bit rank word — a loop of at most 10 iterations with no allocations.
- **Pair/set/quads detection** collapses to counting how many of the four suit blocks have a specific bit set.
- **Combining hands** (hole cards + community cards in Hold'em) is a single `|` operation on two `ulong` values.

Hardware `PopCount` via `System.Numerics.BitOperations` runs in a single clock cycle on x86/x64 with the `POPCNT` instruction. No loops, no allocations, no GC pressure.

## The 64-Bit Layout

The `ulong` is divided into four 16-bit blocks, one per suit:

```
Bits 63–48 : Spades
Bits 47–32 : Hearts
Bits 31–16 : Diamonds
Bits 15–0  : Clubs
```

Within each 16-bit block, bits map to ranks:

```
Bit  0  : Ace (low)
Bit  1  : Two
Bit  2  : Three
...
Bit 12  : King
Bit 13  : Ace (high)
Bits 14–15 : Unused
```

The Ace appears twice. This is intentional. A straight can begin with Ace-low (A-2-3-4-5, the wheel) or end with Ace-high (T-J-Q-K-A, broadway). Setting both bits when an Ace is added to a hand means both straight patterns are detectable with the same sliding-window algorithm, with no special cases needed.

### Flush Detection

Extract the 16-bit block for one suit, strip the low-Ace bit to avoid double-counting, and call `PopCount`. Five or more set bits is a flush.

```csharp
ushort suitBits = (ushort)((hand >> suitShift) & 0x3FFF);
suitBits &= 0x3FFE;  // strip bit 0 (Ace-low)
bool flush = BitOperations.PopCount(suitBits) >= 5;
```

### Straight Detection

OR all four suit blocks together to produce a single 14-bit rank word — a bit is set if that rank appears in any suit. Then slide a 5-bit mask from the top of the word downward. The first window where all 5 bits are present is the highest straight.

```csharp
ushort rankWord = (ushort)(clubs | diamonds | hearts | spades);
for (int start = 9; start >= 0; start--)
{
    ushort window = (ushort)(0x1F << start);
    if ((rankWord & window) == window)
        return start;  // straight found, high bit at start + 4
}
```

Ten iterations maximum, no allocations, no sorting.

### Pair / Set / Quads Detection

For any rank, `RankAcrossSuits(rankBit)` returns a mask with that bit set in all four suit blocks simultaneously. `PopCount` of `hand & mask` tells you how many suits hold that rank — 2 is a pair, 3 is trips, 4 is quads.

## Wild Cards and Jokers

Wild card rules vary by game. The library separates the hand itself from the wild configuration using two parallel structures:

- `Cards` — the actual cards dealt, as a `ulong`
- `WildMask` — same bit layout; a set bit means that card is wild this game
- `Jokers` — a `byte` count of pure jokers with no suit identity

This means the core bit operations always work on clean card data. Wild substitution is applied at evaluation time by checking `WildCount = PopCount(Cards & WildMask) + Jokers` and filling in the best possible substitution from the top of the hand rank cascade downward.

Named wilds like one-eyed Jacks (Jack of Spades, Jack of Hearts) set exactly two bits in `WildMask`. Rank-based wilds like deuces wild set the Two bit across all four suit blocks. Jokers add to the byte count.

## Project Structure

```
Core/
  BitLayout.cs          — constants, suit shifts, masks, straight patterns
  BitExtensions.cs      — PopCount, ExtractSuit, CollapseToRanks, GetRankCounts
  Card.cs               — immutable Card struct, converts to/from bit positions
  Enums.cs              — Suit and Rank enums (values match bit positions)
  HandState.cs          — ulong Cards + ulong WildMask + byte Jokers

Evaluation/
  HandRank.cs           — enum from None up to FiveOfAKind
  HandResult.cs         — rank + primary/secondary rank bits + kickers, IComparable
  EvaluationOptions.cs  — game rules (flush size, straight length, wild rules)
  HandEvaluator.cs      — top-down rank cascade with wild substitution

Games/
  Decks/
    IDeckDefinition.cs  — interface for any deck type
    StandardDeck.cs     — 52-card deck, factory methods for common wild variants
  Conversion/
    CardConverter.cs    — Card ↔ bit position, string notation parser ("Ah Kh Qh Jh Th")
```

## Hand Ranks

From lowest to highest:

| Rank | Notes |
|---|---|
| High Card | |
| One Pair | |
| Two Pair | |
| Three of a Kind | |
| Straight | |
| Flush | |
| Full House | |
| Four of a Kind | |
| Straight Flush | |
| Royal Flush | |
| Five of a Kind | Wild card games only — rarest achievable hand |

Five of a Kind ranks above Royal Flush. It requires wild cards and is statistically harder to achieve in any game that uses them.

## Evaluation Options

Pass an `EvaluationOptions` to `HandEvaluator` to configure game-specific rules without changing core logic. Presets are provided:

| Preset | Use |
|---|---|
| `EvaluationOptions.StandardPoker` | 5-card draw, no wilds |
| `EvaluationOptions.WildCardPoker` | 5-card draw with jokers or named wilds |
| `EvaluationOptions.TexasHoldem` | Best 5 of 7, no wilds |
| `EvaluationOptions.Rummy` | 3-card runs and sets, 7-card hand |

## Decks and Wild Rules

`StandardDeck` provides factory methods for common configurations:

```csharp
StandardDeck.Standard()               // 52 cards, no wilds
StandardDeck.WithJokers(2)            // 52 cards + 2 jokers
StandardDeck.DeucesWild()             // all 2s are wild
StandardDeck.OneEyedJacksWild()       // Jack of Spades and Jack of Hearts are wild
StandardDeck.JokersAndDeucesWild(2)   // 2 jokers + deuces wild
```

For custom wild rules, pass `wildRanks` and/or `wildCards` to the constructor directly.

## Parsing Hands

`CardConverter` parses standard card notation. Ranks are `A 2 3 4 5 6 7 8 9 T J Q K`. Suits are `c d h s`. Jokers are `J`, `JKR`, or `JOKER`.

```csharp
CardConverter converter = new CardConverter(StandardDeck.Standard());

HandState hand = converter.ParseHand("Ah Kh Qh Jh Th");   // Royal Flush
HandState wild = converter.ParseHand("Ah Kh Qh JOKER Th"); // Royal Flush with joker
```

## Combining Hands

`HandState` supports merging with `|`. This is the primary operation for Hold'em, where hole cards and community cards are built separately and combined for evaluation:

```csharp
HandState holeCards      = converter.ParseHand("Ah Kh");
HandState communityCards = converter.ParseHand("Qh Jh Th 2c 7d");
HandState full           = holeCards | communityCards;

HandEvaluator evaluator = new HandEvaluator(EvaluationOptions.TexasHoldem);
HandResult result = evaluator.Evaluate(full);
```

## Extensibility

The library is designed to support games beyond poker. `IDeckDefinition` and `EvaluationOptions` let any game plug into the bit engine without modifying core logic. Planned extensions include a Texas Hold'em equity calculator (win percentage from Monte Carlo simulation over the remaining deck) and Rummy run/set detection.

## Requirements

- .NET 10
- No external dependencies
