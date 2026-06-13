namespace Card_Deck_Handler.Core;

public enum Suit : int
{
    Clubs = 0,
    Diamonds = 1,
    Hearts = 2,
    Spades = 3
}

public enum Rank : int
{
    AceLow = 0,
    Two = 1,
    Three = 2,
    Four = 3,
    Five = 4,
    Six = 5,
    Seven = 6,
    Eight = 7,
    Nine = 8,
    Ten = 9,
    Jack = 10,
    Queen = 11,
    King = 12,
    AceHigh = 13
}

public static class RankExtensions
{
    public static bool IsAce(this Rank rank) => rank == Rank.AceLow || rank == Rank.AceHigh;

    public static string ToDisplayString(this Rank rank) => rank switch
    {
        Rank.AceLow => "A",
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "T",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        Rank.AceHigh => "A",
        _ => "?"
    };
}

public static class SuitExtensions
{
    public static char ToSymbol(this Suit suit) => suit switch
    {
        Suit.Clubs => 'c',
        Suit.Diamonds => 'd',
        Suit.Hearts => 'h',
        Suit.Spades => 's',
        _ => '?'
    };

    public static char ToUnicode(this Suit suit) => suit switch
    {
        Suit.Clubs => '♣',
        Suit.Diamonds => '♦',
        Suit.Hearts => '♥',
        Suit.Spades => '♠',
        _ => '?'
    };
}