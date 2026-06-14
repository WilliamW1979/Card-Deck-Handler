# Card Deck Handler

A high-performance .NET 10 card hand evaluation library built around a 64-bit integer representation of a full hand. Designed for speed — hand evaluation uses bitwise operations and population counts rather than object comparisons or sorting algorithms. Supports poker variants, Rummy, Seven Card Stud, Blackjack, and Texas Hold'em equity calculation.

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
    IDeckDefinition.cs      — interface for any deck type
    StandardDeck.cs         — 52-card deck, factory methods for common wild variants
  Conversion/
    CardConverter.cs        — Card ↔ bit position, string notation parser
  Rummy/
    RummyGroup.cs           — matched set or run with point value
    RummyHandResult.cs      — decomposition result: groups, deadwood, score
    RummyDecomposer.cs      — partition solver, minimizes deadwood across all variants
    GinRummyEvaluator.cs    — Gin Rummy rules, knocking, Gin, Big Gin
    Rummy500Evaluator.cs    — Rummy 500 rules, point scoring, variable hand sizes
  Holdem/
    HoldemPlayer.cs         — hole cards and player identity
    HoldemBoard.cs          — community cards tracked by street
    HoldemEquityResult.cs   — win percentages per player to 3 decimal places
    HoldemEquityCalculator.cs — exact enumeration or Monte Carlo, 2–10 players
  SevenCardStud/
    StudCardState.cs        — single card with face-up/down and held/discarded status
    StudPlayer.cs           — per-player card management, visible vs hole card views
    StudHandEvaluator.cs    — best 5 of 7 evaluation, winner determination
  Blackjack/
    BlackjackShoe.cs        — configurable 1–8 deck shoe with shuffle and tracking
    BlackjackHand.cs        — total, soft/hard, bust, blackjack, Five Card Charlie
    BlackjackStrategy.cs    — complete basic strategy table for all hand vs dealer combos
    BlackjackEvaluator.cs   — player vs dealer outcome, split hands, payout calculation
    BlackjackShoeTracker.cs — Hi-Lo card counting, running count, true count
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

HandState hand = converter.ParseHand("Ah Kh Qh Jh Th");    // Royal Flush
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

## Texas Hold'em Equity

Computes win percentage for each player given known hole cards and any community cards already dealt. Automatically uses exact enumeration when 1–2 cards remain, Monte Carlo (100,000 iterations) for earlier streets. Supports 2–10 players. All percentages are `decimal` to 3 decimal places and always sum to exactly `100.000`.

```csharp
CardConverter converter = new CardConverter(StandardDeck.Standard());
HoldemEquityCalculator calc = new HoldemEquityCalculator();

List<HoldemPlayer> players = new List<HoldemPlayer>
{
    new HoldemPlayer("Alice", converter.ParseHand("Ah Kh")),
    new HoldemPlayer("Bob",   converter.ParseHand("7c 7d"))
};

HoldemBoard board = HoldemBoard.FromFlop(converter.ParseHand("Qh Jh 2c"));
HoldemEquityResult result = calc.Calculate(players, board);

// Alice: 67.341%
// Bob:   32.659%
```

## Rummy

Both Gin Rummy and Rummy 500 share the same partition solver (`RummyDecomposer`) which finds the optimal grouping of cards into runs and sets to minimize deadwood. Each evaluator applies its own scoring rules on top.

```csharp
GinRummyEvaluator gin = new GinRummyEvaluator();
RummyHandResult result = gin.Evaluate(hand);

// result.IsGin        — zero deadwood, 10-card hand
// result.IsBigGin     — zero deadwood, 11-card hand
// result.CanKnock     — deadwood 10 points or less
// result.DeadwoodPoints
// result.Groups       — list of matched runs and sets
// result.Deadwood     — unmatched cards
```

```csharp
Rummy500Evaluator r500 = new Rummy500Evaluator();
RummyHandResult result = r500.Evaluate(hand);
int score = Rummy500Evaluator.RoundScore(result); // matched - deadwood
```

**Gin Rummy scoring:** Ace = 1, face cards = 10, number cards = face value. Knock at ≤ 10 deadwood. Gin bonus = 25, undercut bonus = 25, Big Gin bonus = 31.

**Rummy 500 scoring:** Ace = 15 (high) or 1 (low), face cards = 10, number cards = face value. Go out by emptying hand. First to 500 points wins.

## Seven Card Stud

Each player's cards are tracked individually with face-up/face-down visibility and held/discarded status, supporting both standard Stud and draw variants where cards can be discarded and replaced.

```csharp
StudPlayer player = new StudPlayer("Alice");
player.AddCard(new Card(Suit.Hearts,   Rank.Ace),   CardVisibility.FaceDown);
player.AddCard(new Card(Suit.Spades,   Rank.Ace),   CardVisibility.FaceDown);
player.AddCard(new Card(Suit.Clubs,    Rank.King),   CardVisibility.FaceUp);
player.AddCard(new Card(Suit.Diamonds, Rank.Queen),  CardVisibility.FaceUp);
player.AddCard(new Card(Suit.Hearts,   Rank.Jack),   CardVisibility.FaceUp);
player.AddCard(new Card(Suit.Clubs,    Rank.Ten),    CardVisibility.FaceUp);
player.AddCard(new Card(Suit.Spades,   Rank.Two),    CardVisibility.FaceDown);

// Discard and replace (draw variants)
player.DiscardCard(new Card(Suit.Spades, Rank.Two));
player.AddCard(new Card(Suit.Hearts, Rank.Ten), CardVisibility.FaceDown);

StudHandEvaluator evaluator = new StudHandEvaluator();
StudHandResult result = evaluator.Evaluate(player);

// Opponent view — only sees face-up cards
HandState visible = player.ToVisibleHandState();
```

Multiple players are ranked with `EvaluateAll`, ties are handled automatically by `GetWinners`.

## Blackjack

Configurable 1–8 deck shoe, complete basic strategy table, outcome evaluation with standard payouts, and Hi-Lo card counting simulation.

```csharp
BlackjackShoe shoe = new BlackjackShoe(deckCount: 6);
BlackjackEvaluator evaluator = new BlackjackEvaluator();
BlackjackShoeTracker tracker = new BlackjackShoeTracker(deckCount: 6);

BlackjackHand playerHand = new BlackjackHand();
BlackjackHand dealerHand = new BlackjackHand();

playerHand.AddCard(shoe.Deal());
dealerHand.AddCard(shoe.Deal());
playerHand.AddCard(shoe.Deal());
dealerHand.AddCard(shoe.Deal()); // dealer's hole card

// Basic strategy recommendation
int dealerUpValue = BlackjackHand.CardValue(dealerHand.UpCard!.Value);
BlackjackAction action = BlackjackStrategy.GetAction(playerHand, dealerUpValue);
// e.g. BlackjackAction.DoubleDown, Hit, Stand, Split

// Complete dealer hand
evaluator.PlayDealer(dealerHand, shoe, hitSoft17: false);

// Determine outcome
BlackjackOutcome outcome = evaluator.Evaluate(playerHand, dealerHand);
decimal payout = BlackjackEvaluator.GetPayout(outcome);

// Track cards for counting
tracker.TrackCards(playerHand.Cards);
tracker.TrackCards(dealerHand.Cards);
Console.WriteLine($"Running count: {tracker.RunningCount}");
Console.WriteLine($"True count: {tracker.TrueCount(shoe.RemainingCards):F3}");
```

**Five Card Charlie** (optional rule — 5 cards without busting wins automatically) is enabled by passing `enableFiveCardCharlie: true` to `BlackjackEvaluator` and each `AddCard` call.

**Hi-Lo card counting values:** Low cards (2–6) = +1, high cards (10, J, Q, K, A) = −1, neutral (7–9) = 0. True count adjusts the running count for decks remaining as a `decimal`.

## Precision

All percentage calculations use `decimal` arithmetic throughout — never `float` or `double`. Win percentages are rounded to 3 decimal places and normalized to sum to exactly `100.000` across all players.

## Requirements

- .NET 10
- No external dependencies
