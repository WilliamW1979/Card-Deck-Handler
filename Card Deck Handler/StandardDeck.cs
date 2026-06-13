using System;
using System.Collections.Generic;
using Card_Deck_Handler.Core;
using Card_Deck_Handler.Games.Decks;

namespace Card_Deck_Handler.Games.Decks;


public sealed class StandardDeck : IDeckDefinition
{
    private readonly List<Card> _cards;
    private readonly ulong _wildCardMask;

    public string Name { get; }
    public IReadOnlyList<Card> Cards => _cards;
    public bool HasJokers { get; }
    public int JokerCount { get; }

    public StandardDeck(int jokerCount = 0, IEnumerable<Rank>? wildRanks = null, IEnumerable<Card>? wildCards = null)
    {
        JokerCount = jokerCount;
        HasJokers = jokerCount > 0;
        _cards = [];
        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                if (rank == Rank.AceLow) continue;
                _cards.Add(new Card(suit, rank));
            }
        }
        _wildCardMask = 0;
        if (wildRanks is not null)
        {
            foreach (Rank rank in wildRanks)
            {
                int rankBit = (int)rank;
                _wildCardMask |= BitLayout.RankAcrossSuits(rankBit);
                if (rank == Rank.AceHigh)
                    _wildCardMask |= BitLayout.RankAcrossSuits(BitLayout.AceLowBit);
            }
        }
        if (wildCards is not null)
        {
            foreach (Card card in wildCards)
                _wildCardMask |= card.ToBitMask();
        }
        Name = BuildName(jokerCount, wildRanks, wildCards);
    }

    public ulong ComputeWildMask(ulong handCards) => handCards & _wildCardMask;
    public static StandardDeck Standard() => new();
    public static StandardDeck WithJokers(int count = 2) => new(jokerCount: count);
    public static StandardDeck DeucesWild() => new(wildRanks: [Rank.Two]);
    public static StandardDeck OneEyedJacksWild() => new(wildCards: [new Card(Suit.Spades, Rank.Jack), new Card(Suit.Hearts, Rank.Jack)]);
    public static StandardDeck JokersAndDeucesWild(int jokerCount = 2) => new(jokerCount: jokerCount, wildRanks: [Rank.Two]);

    private static string BuildName(int jokerCount, IEnumerable<Rank>? wildRanks, IEnumerable<Card>? wildCards)
    {
        List<string> parts = new List<string> { "Standard 52" };
        if (jokerCount > 0) parts.Add($"{jokerCount} Joker(s)");
        if (wildRanks is not null)
        {
            string rankList = string.Join(", ", wildRanks);
            if (!string.IsNullOrEmpty(rankList)) parts.Add($"Wild Ranks: {rankList}");
        }
        if (wildCards is not null)
        {
            string cardList = string.Join(", ", wildCards);
            if (!string.IsNullOrEmpty(cardList)) parts.Add($"Wild Cards: {cardList}");
        }
        return string.Join(" | ", parts);
    }
}