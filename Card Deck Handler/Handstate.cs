using Card_Deck_Handler.Core;

namespace Card_Deck_Handler.Core;

public struct HandState
{
    public ulong Cards;
    public ulong WildMask;
    public byte Jokers;

    public HandState(ulong cards, ulong wildMask = 0, byte jokers = 0)
    {
        Cards = cards;
        WildMask = wildMask;
        Jokers = jokers;
    }

    public int WildCount => BitExtensions.PopCount(Cards & WildMask) + Jokers;
    public ulong NaturalCards => Cards & ~WildMask;
    public int CardCount => BitExtensions.PopCount(Cards) + Jokers;
    public ushort GetSuitBits(Suit suit) => (ushort)((Cards >> BitLayout.SuitShifts[(int)suit]) & BitLayout.SuitRankMask);
    public ushort GetNaturalSuitBits(Suit suit) => (ushort)((NaturalCards >> BitLayout.SuitShifts[(int)suit]) & BitLayout.SuitRankMask);
    public HandState WithCard(Card card) => new(Cards | card.ToBitMask(), WildMask, Jokers);
    public HandState WithoutCard(Card card) => new(Cards & ~card.ToBitMask(), WildMask & ~card.ToBitMask(), Jokers);
    public HandState WithJoker() => new(Cards, WildMask, (byte)(Jokers + 1));
    public HandState WithWild(Card card) => new(Cards | card.ToBitMask(), WildMask | card.ToBitMask(), Jokers);
    public static HandState Merge(HandState a, HandState b) => new(a.Cards | b.Cards, a.WildMask | b.WildMask, (byte)(a.Jokers + b.Jokers));
    public static HandState operator |(HandState a, HandState b) => Merge(a, b);
    public static readonly HandState Empty = new(0, 0, 0);
    public bool IsEmpty => Cards == 0 && Jokers == 0;
}