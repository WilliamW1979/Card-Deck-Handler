using System;

namespace Card_Deck_Handler.Core;

public readonly struct Card : IEquatable<Card>
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public bool IsAce => Rank.IsAce();

    public int ToBitPosition() => BitLayout.SuitShifts[(int)Suit] + (int)Rank;

    public ulong ToBitMask() => 1UL << ToBitPosition();

    public static Card FromBitPosition(int bitPosition)
    {
        int suitIndex = bitPosition / BitLayout.BitsPerSuit;
        int rankBit = bitPosition % BitLayout.BitsPerSuit;
        return new Card((Suit)suitIndex, (Rank)rankBit);
    }

    public override string ToString() => $"{Rank.ToDisplayString()}{Suit.ToSymbol()}";

    public string ToUnicodeString() => $"{Rank.ToDisplayString()}{Suit.ToUnicode()}";

    public bool Equals(Card other) => Suit == other.Suit && Rank == other.Rank;
    public override bool Equals(object? obj) => obj is Card c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(Suit, Rank);

    public static bool operator ==(Card left, Card right) => left.Equals(right);
    public static bool operator !=(Card left, Card right) => !left.Equals(right);
}