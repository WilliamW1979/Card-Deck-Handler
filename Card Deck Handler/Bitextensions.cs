using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Card_Deck_Handler.Core;

public static class BitExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong value) => BitOperations.PopCount(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ushort value) => BitOperations.PopCount((uint)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ExtractSuit(ulong hand, Suit suit) => (ushort)((hand >> BitLayout.SuitShifts[(int)suit]) & BitLayout.SuitRankMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort CollapseToRanks(ulong hand)
    {
        ushort clubs = (ushort)(hand & BitLayout.SuitRankMask);
        ushort diamonds = (ushort)((hand >> 16) & BitLayout.SuitRankMask);
        ushort hearts = (ushort)((hand >> 32) & BitLayout.SuitRankMask);
        ushort spades = (ushort)((hand >> 48) & BitLayout.SuitRankMask);
        return (ushort)(clubs | diamonds | hearts | spades);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SuitCardCount(ulong hand, Suit suit)
    {
        ushort suitBits = ExtractSuit(hand, suit);
        suitBits &= BitLayout.SuitRankNoLowAce;
        return PopCount(suitBits);
    }

    public static (Suit suit, int count) BestFlushSuit(ulong hand)
    {
        Suit best = Suit.Clubs;
        int bestCount = 0;
        for (int i = 0; i < 4; i++)
        {
            int count = SuitCardCount(hand, (Suit)i);
            if (count > bestCount)
            {
                bestCount = count;
                best = (Suit)i;
            }
        }
        return (best, bestCount);
    }
    public static int FindStraight(ushort rankWord, int length = 5)
    {
        ushort mask = (ushort)((1 << length) - 1);
        int topStart = BitLayout.RankBitsUsed - length;
        for (int start = topStart; start >= 0; start--)
        {
            ushort window = (ushort)(mask << start);
            if ((rankWord & window) == window)
                return topStart - start;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasStraight(ushort rankWord, int length = 5) => FindStraight(rankWord, length) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountRankAcrossSuits(ulong hand, int rankBit)
    {
        ulong rankMask = BitLayout.RankAcrossSuits(rankBit);
        return PopCount(hand & rankMask);
    }

    public static List<(int rankBit, int count)> GetRankCounts(ulong hand)
    {
        List<(int rankBit, int count)> counts = new List<(int rankBit, int count)>(14);
        for (int rankBit = 1; rankBit < BitLayout.RankBitsUsed; rankBit++)
        {
            int count = CountRankAcrossSuits(hand, rankBit);
            if (rankBit == BitLayout.AceHighBit)
            {
                int lowAceCount = CountRankAcrossSuits(hand, BitLayout.AceLowBit);
                count = Math.Max(count, lowAceCount);
            }
            if (count > 0)
                counts.Add((rankBit, count));
        }
        counts.Sort((a, b) => a.count != b.count ? b.count.CompareTo(a.count) : b.rankBit.CompareTo(a.rankBit));
        return counts;
    }

    public static IEnumerable<int> SetBitPositions(ulong value)
    {
        while (value != 0)
        {
            int pos = BitOperations.TrailingZeroCount(value);
            yield return pos;
            value &= value - 1;
        }
    }
}