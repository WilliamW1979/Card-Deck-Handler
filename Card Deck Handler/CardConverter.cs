using System;
using System.Collections.Generic;
using Card_Deck_Handler.Core;
using Card_Deck_Handler.Games.Decks;

namespace Card_Deck_Handler.Games.Conversion;

public interface ICardConverter
{
    int CardToBitPosition(Card card);
    ulong CardToBitMask(Card card);
    HandState CardsToHand(IEnumerable<Card> cards, int jokers = 0);
    IReadOnlyList<Card> HandToCards(HandState hand);
    Card ParseCard(string notation);
    HandState ParseHand(string notation);
}

public sealed class CardConverter : ICardConverter
{
    private readonly IDeckDefinition _deck;

    public CardConverter(IDeckDefinition deck)
    {
        _deck = deck;
    }

    public int CardToBitPosition(Card card) => card.ToBitPosition();
    public ulong CardToBitMask(Card card) => card.ToBitMask();

    public HandState CardsToHand(IEnumerable<Card> cards, int jokers = 0)
    {
        ulong hand = 0;
        foreach (Card card in cards)
        {
            if (card.Rank == Rank.AceHigh || card.Rank == Rank.AceLow)
            {
                hand |= new Card(card.Suit, Rank.AceLow).ToBitMask();
                hand |= new Card(card.Suit, Rank.AceHigh).ToBitMask();
            }
            else
                hand |= card.ToBitMask();
        }
        ulong wildMask = _deck.ComputeWildMask(hand);
        int totalJokers = jokers + _deck.JokerCount;   // deck jokers + extra
        return new HandState(hand, wildMask, (byte)jokers);
    }

    public IReadOnlyList<Card> HandToCards(HandState hand)
    {
        List<Card> cards = new List<Card>();
        foreach (int bitPos in BitExtensions.SetBitPositions(hand.Cards))
        {
            Card card = Card.FromBitPosition(bitPos);
            if (card.Rank == Rank.AceLow) continue;
            cards.Add(card);
        }
        return cards;
    }

    public Card ParseCard(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation) || notation.Length < 2)
            throw new ArgumentException($"Invalid card notation: '{notation}'");
        string upper = notation.Trim().ToUpperInvariant();
        string rankPart = upper[..^1];
        char suitChar = upper[^1];
        Rank rank = rankPart switch
        {
            "A" => Rank.AceHigh,
            "2" => Rank.Two,
            "3" => Rank.Three,
            "4" => Rank.Four,
            "5" => Rank.Five,
            "6" => Rank.Six,
            "7" => Rank.Seven,
            "8" => Rank.Eight,
            "9" => Rank.Nine,
            "T" => Rank.Ten,
            "10" => Rank.Ten,
            "J" => Rank.Jack,
            "Q" => Rank.Queen,
            "K" => Rank.King,
            _ => throw new ArgumentException($"Unknown rank: '{rankPart}'")
        };
        Suit suit = suitChar switch
        {
            'C' => Suit.Clubs,
            'D' => Suit.Diamonds,
            'H' => Suit.Hearts,
            'S' => Suit.Spades,
            _ => throw new ArgumentException($"Unknown suit: '{suitChar}'")
        };
        return new Card(suit, rank);
    }

    public HandState ParseHand(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation))
            return HandState.Empty;
        string[] tokens = notation.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<Card> cards = new List<Card>();
        int jokers = 0;
        foreach (string token in tokens)
        {
            string t = token.Trim().ToUpperInvariant();
            if (t is "J" or "JKR" or "JOKER")
            {
                jokers++;
                continue;
            }
            cards.Add(ParseCard(token));
        }
        return CardsToHand(cards, jokers);
    }
}