namespace Card_Deck_Handler.Core;


public static class BitLayout
{
    public const int BitsPerSuit = 16;
    public const int ClubsShift = 0;
    public const int DiamondsShift = 16;
    public const int HeartsShift = 32;
    public const int SpadesShift = 48;
    public const int AceLowBit = 0;
    public const int TwoBit = 1;
    public const int ThreeBit = 2;
    public const int FourBit = 3;
    public const int FiveBit = 4;
    public const int SixBit = 5;
    public const int SevenBit = 6;
    public const int EightBit = 7;
    public const int NineBit = 8;
    public const int TenBit = 9;
    public const int JackBit = 10;
    public const int QueenBit = 11;
    public const int KingBit = 12;
    public const int AceHighBit = 13;
    public const int RankBitsUsed = 14;
    public const ushort SuitRankMask = 0x3FFF;
    public const ushort SuitRankNoLowAce = 0x3FFE;
    public const ushort AceLowMask = 1 << AceLowBit;
    public const ushort AceHighMask = 1 << AceHighBit;
    public const ulong ClubsMask = (ulong)SuitRankMask << ClubsShift;
    public const ulong DiamondsMask = (ulong)SuitRankMask << DiamondsShift;
    public const ulong HeartsMask = (ulong)SuitRankMask << HeartsShift;
    public const ulong SpadesMask = (ulong)SuitRankMask << SpadesShift;

    public static readonly ulong[] SuitMasks = [ClubsMask, DiamondsMask, HeartsMask, SpadesMask];
    public static readonly int[] SuitShifts = [ClubsShift, DiamondsShift, HeartsShift, SpadesShift];
    public static readonly ushort[] StraightMasks = [0b10_0001_1110_0000,];
    public static readonly ushort[] StraightPatterns = BuildStraightPatterns();

    private static ushort[] BuildStraightPatterns()
    {
        ushort[] patterns = new ushort[10];
        for (int i = 0; i < 10; i++)
        {
            int startBit = 9 - i;
            patterns[i] = (ushort)(0x1F << startBit);
        }
        return patterns;
    }

    public static ulong RankAcrossSuits(int rankBit)
    {
        ulong bit = 1UL << rankBit;
        return (bit << ClubsShift) | (bit << DiamondsShift) | (bit << HeartsShift) | (bit << SpadesShift);
    }
}