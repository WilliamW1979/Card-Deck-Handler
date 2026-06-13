using System;
using System.Collections.Generic;
using Card_Deck_Handler.Core;

namespace Card_Deck_Handler.Evaluation;

public sealed class HandEvaluator
{
    private readonly EvaluationOptions _options;

    public HandEvaluator(EvaluationOptions? options = null)
    {
        _options = options ?? EvaluationOptions.StandardPoker;
    }

    public HandResult Evaluate(HandState hand)
    {
        int wildCount = hand.WildCount;
        HandResult? result;
        if (_options.AllowFiveOfAKind)
        {
            result = TryFiveOfAKind(hand, wildCount);
            if (result is not null) return result;
        }
        result = TryRoyalFlush(hand, wildCount);
        if (result is not null) return result;
        result = TryStraightFlush(hand, wildCount);
        if (result is not null) return result;
        result = TryFourOfAKind(hand, wildCount);
        if (result is not null) return result;
        result = TryFullHouse(hand, wildCount);
        if (result is not null) return result;
        result = TryFlush(hand, wildCount);
        if (result is not null) return result;
        result = TryStraight(hand, wildCount);
        if (result is not null) return result;
        result = TryThreeOfAKind(hand, wildCount);
        if (result is not null) return result;
        result = TryTwoPair(hand, wildCount);
        if (result is not null) return result;
        result = TryOnePair(hand, wildCount);
        if (result is not null) return result;
        return MakeHighCard(hand);
    }

    private HandResult? TryFiveOfAKind(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        if (wildCount >= 5)
            return new HandResult(HandRank.FiveOfAKind,
                primaryRankBit: BitLayout.AceHighBit, wildsUsed: 5);
        foreach (var (rankBit, count) in rankCounts)
        {
            if (count + wildCount >= 5)
            {
                int wildsUsed = Math.Max(0, 5 - count);
                return new HandResult(HandRank.FiveOfAKind,
                    primaryRankBit: rankBit, wildsUsed: wildsUsed);
            }
        }
        return null;
    }
    private HandResult? TryRoyalFlush(HandState hand, int wildCount)
    {
        const ushort royalPattern = (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13);
        for (int s = 0; s < 4; s++)
        {
            ushort suitBits = BitExtensions.ExtractSuit(hand.NaturalCards, (Suit)s);
            ushort present = (ushort)(suitBits & royalPattern);
            int missing = BitExtensions.PopCount((ushort)(royalPattern & ~present));
            if (missing <= wildCount)
                return new HandResult(HandRank.RoyalFlush, primaryRankBit: BitLayout.AceHighBit, flushSuit: (Suit)s, wildsUsed: missing);
        }
        return null;
    }

    private HandResult? TryStraightFlush(HandState hand, int wildCount)
    {
        int len = _options.StraightMinLength;
        for (int s = 0; s < 4; s++)
        {
            ushort suitBits = BitExtensions.ExtractSuit(hand.NaturalCards, (Suit)s);
            (bool found, int highBit, int wildsUsed) result = FindBestStraightWithWilds(suitBits, wildCount, len);
            if (result.found)
                return new HandResult(HandRank.StraightFlush, primaryRankBit: result.highBit, flushSuit: (Suit)s, wildsUsed: result.wildsUsed);
        }

        return null;
    }

    private HandResult? TryFourOfAKind(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        foreach (var (rankBit, count) in rankCounts)
        {
            if (count + wildCount >= 4)
            {
                int wildsUsed = Math.Max(0, 4 - count);
                int kicker = GetTopKicker(hand.NaturalCards, rankBit);
                return new HandResult(HandRank.FourOfAKind, primaryRankBit: rankBit, kickers: kicker >= 0 ? [kicker] : [], wildsUsed: wildsUsed);
            }
        }
        return null;
    }

    private HandResult? TryFullHouse(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        int remainingWilds = wildCount;
        int threeRank = -1;
        int wildsFor3 = 0;
        foreach ((int rankBit, int count) in rankCounts)
        {
            if (count >= 3)
            {
                threeRank = rankBit;
                wildsFor3 = 0;
                break;
            }
            if (count + remainingWilds >= 3 && threeRank < 0)
            {
                threeRank = rankBit;
                wildsFor3 = 3 - count;
            }
        }
        if (threeRank < 0) return null;
        remainingWilds -= wildsFor3;
        foreach ((int rankBit, int count) in rankCounts)
        {
            if (rankBit == threeRank) continue;
            if (count >= 2 || count + remainingWilds >= 2)
            {
                int wildsFor2 = Math.Max(0, 2 - count);
                return new HandResult(HandRank.FullHouse, primaryRankBit: threeRank, secondaryRankBit: rankBit, wildsUsed: wildsFor3 + wildsFor2);
            }
        }
        return null;
    }

    private HandResult? TryFlush(HandState hand, int wildCount)
    {
        int minCards = _options.FlushMinCards;
        for (int s = 0; s < 4; s++)
        {
            ushort suitBits = BitExtensions.ExtractSuit(hand.NaturalCards, (Suit)s);
            ushort countBits = (ushort)(suitBits & BitLayout.SuitRankNoLowAce);
            int natural = BitExtensions.PopCount(countBits);
            if (natural + wildCount >= minCards)
            {
                int wildsUsed = Math.Max(0, minCards - natural);
                ushort fullBits = (ushort)(suitBits | BuildWildFillBits(suitBits, wildsUsed));
                int highBit = HighBit(fullBits);
                IReadOnlyList<int> kickers = GetTopKickers(fullBits, highBit, 4);
                return new HandResult(HandRank.Flush, primaryRankBit: highBit, kickers: kickers, flushSuit: (Suit)s, wildsUsed: wildsUsed);
            }
        }
        return null;
    }

    private HandResult? TryStraight(HandState hand, int wildCount)
    {
        ushort rankWord = BitExtensions.CollapseToRanks(hand.NaturalCards);
        (bool found, int highBit, int wildsUsed) result = FindBestStraightWithWilds(rankWord, wildCount, _options.StraightMinLength);
        if (!result.found) return null;
        return new HandResult(HandRank.Straight, primaryRankBit: result.highBit, wildsUsed: result.wildsUsed);
    }

    private HandResult? TryThreeOfAKind(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        foreach ((int rankBit, int count) in rankCounts)
        {
            if (count + wildCount >= 3)
            {
                int wildsUsed = Math.Max(0, 3 - count);
                IReadOnlyList<int> kickers = GetTopKickers(hand.NaturalCards, rankBit, 2);
                return new HandResult(HandRank.ThreeOfAKind, primaryRankBit: rankBit, kickers: kickers, wildsUsed: wildsUsed);
            }
        }
        return null;
    }

    private HandResult? TryTwoPair(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        int firstPair = -1;
        int secondPair = -1;
        int wildsUsed = 0;
        int wildsLeft = wildCount;
        foreach ((int rankBit, int count) in rankCounts)
        {
            if (count >= 2)
            {
                if (firstPair < 0) firstPair = rankBit;
                else if (secondPair < 0) { secondPair = rankBit; break; }
            }
            else if (count + wildsLeft >= 2 && firstPair < 0)
            {
                firstPair = rankBit;
                wildsUsed += 2 - count;
                wildsLeft -= 2 - count;
            }
        }
        if (firstPair < 0 || secondPair < 0) return null;
        int kicker = GetTopKicker(hand.NaturalCards, firstPair, secondPair);
        return new HandResult(HandRank.TwoPair, primaryRankBit: firstPair, secondaryRankBit: secondPair, kickers: kicker >= 0 ? [kicker] : [], wildsUsed: wildsUsed);
    }

    private HandResult? TryOnePair(HandState hand, int wildCount)
    {
        List<(int rankBit, int count)> rankCounts = BitExtensions.GetRankCounts(hand.NaturalCards);
        foreach ((int rankBit, int count) in rankCounts)
        {
            if (count >= 2 || (wildCount >= 1 && count >= 1))
            {
                int wildsUsed = count >= 2 ? 0 : 1;
                IReadOnlyList<int> kickers = GetTopKickers(hand.NaturalCards, rankBit, 3);
                return new HandResult(HandRank.OnePair, primaryRankBit: rankBit, kickers: kickers, wildsUsed: wildsUsed);
            }
        }
        if (wildCount >= 2)
            return new HandResult(HandRank.OnePair, primaryRankBit: BitLayout.AceHighBit, wildsUsed: 2);
        return null;
    }

    private HandResult MakeHighCard(HandState hand)
    {
        ushort rankWord = BitExtensions.CollapseToRanks(hand.Cards);
        rankWord &= BitLayout.SuitRankNoLowAce;
        int high = HighBit(rankWord);
        IReadOnlyList<int> kickers = GetTopKickers(hand.Cards, high, 4);
        return new HandResult(HandRank.HighCard, primaryRankBit: high, kickers: kickers);
    }

    private (bool found, int highBit, int wildsUsed) FindBestStraightWithWilds(ushort rankBits, int wildCount, int length)
    {
        ushort slideMask = (ushort)((1 << length) - 1);
        int topStart = BitLayout.RankBitsUsed - length;
        for (int start = topStart; start >= 0; start--)
        {
            ushort window = (ushort)(slideMask << start);
            ushort present = (ushort)(rankBits & window);
            int missing = BitExtensions.PopCount((ushort)(window & ~present));
            if (missing <= wildCount)
            {
                int highBit = start + length - 1;
                return (true, highBit, missing);
            }
        }
        return (false, 0, 0);
    }

    private static int GetTopKicker(ulong hand, params int[] excludeRanks)
    {
        ushort rankWord = BitExtensions.CollapseToRanks(hand);
        rankWord &= BitLayout.SuitRankNoLowAce;
        for (int bit = BitLayout.AceHighBit; bit >= 1; bit--)
        {
            if (Array.IndexOf(excludeRanks, bit) >= 0) continue;
            if ((rankWord & (1 << bit)) != 0) return bit;
        }
        return -1;
    }

    private static IReadOnlyList<int> GetTopKickers(ulong hand, int excludeRank, int count)
    {
        List<int> result = new List<int>(count);
        ushort rankWord = BitExtensions.CollapseToRanks(hand);
        rankWord &= BitLayout.SuitRankNoLowAce;
        for (int bit = BitLayout.AceHighBit; bit >= 1 && result.Count < count; bit--)
        {
            if (bit == excludeRank) continue;
            if ((rankWord & (1 << bit)) != 0) result.Add(bit);
        }
        return result;
    }

    private static IReadOnlyList<int> GetTopKickers(ushort suitBits, int excludeRank, int count)
    {
        List<int> result = new List<int>(count);
        ushort bits = (ushort)(suitBits & BitLayout.SuitRankNoLowAce);
        for (int bit = BitLayout.AceHighBit; bit >= 1 && result.Count < count; bit--)
        {
            if (bit == excludeRank) continue;
            if ((bits & (1 << bit)) != 0) result.Add(bit);
        }
        return result;
    }

    private static int HighBit(ushort bits)
    {
        if (bits == 0) return 0;
        return 15 - System.Numerics.BitOperations.LeadingZeroCount(bits);
    }

    private static ushort BuildWildFillBits(ushort existing, int wildsToPlace)
    {
        ushort fill = 0;
        int placed = 0;
        for (int bit = BitLayout.AceHighBit; bit >= 1 && placed < wildsToPlace; bit--)
        {
            if ((existing & (1 << bit)) == 0)
            {
                fill |= (ushort)(1 << bit);
                placed++;
            }
        }
        return fill;
    }
}