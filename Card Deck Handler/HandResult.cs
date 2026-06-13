using System;
using System.Collections.Generic;

namespace Card_Deck_Handler.Evaluation;

public enum HandRank : int
{
    None = 0,
    HighCard = 1,
    OnePair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10,
    FiveOfAKind = 11
}

public sealed class HandResult : IComparable<HandResult>
{
    public HandRank Rank { get; }
    public int PrimaryRankBit { get; }
    public int SecondaryRankBit { get; }
    public IReadOnlyList<int> Kickers { get; }
    public Core.Suit? FlushSuit { get; }
    public int WildsUsed { get; }

    public HandResult(HandRank rank, int primaryRankBit = 0, int secondaryRankBit = 0, IReadOnlyList<int>? kickers = null, Core.Suit? flushSuit = null, int wildsUsed = 0)
    {
        Rank = rank;
        PrimaryRankBit = primaryRankBit;
        SecondaryRankBit = secondaryRankBit;
        Kickers = kickers ?? [];
        FlushSuit = flushSuit;
        WildsUsed = wildsUsed;
    }

    public int CompareTo(HandResult? other)
    {
        if (other is null) return 1;
        int cmp = Rank.CompareTo(other.Rank);
        if (cmp != 0) return cmp;
        cmp = PrimaryRankBit.CompareTo(other.PrimaryRankBit);
        if (cmp != 0) return cmp;
        cmp = SecondaryRankBit.CompareTo(other.SecondaryRankBit);
        if (cmp != 0) return cmp;
        int kickerCount = Math.Min(Kickers.Count, other.Kickers.Count);
        for (int i = 0; i < kickerCount; i++)
        {
            cmp = Kickers[i].CompareTo(other.Kickers[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    public static bool operator >(HandResult a, HandResult b) => a.CompareTo(b) > 0;
    public static bool operator <(HandResult a, HandResult b) => a.CompareTo(b) < 0;
    public static bool operator >=(HandResult a, HandResult b) => a.CompareTo(b) >= 0;
    public static bool operator <=(HandResult a, HandResult b) => a.CompareTo(b) <= 0;

    public override string ToString()
    {
        string wild = WildsUsed > 0 ? $" ({WildsUsed} wild)" : string.Empty;
        return $"{Rank}{wild}";
    }

    public static HandResult None => new(HandRank.None);
}