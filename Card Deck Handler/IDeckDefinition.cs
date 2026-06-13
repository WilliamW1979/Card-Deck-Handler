using System.Collections.Generic;
using Card_Deck_Handler.Core;

namespace Card_Deck_Handler.Games.Decks;

/// <summary>
/// Defines what cards exist in a given game's deck and how wild cards work.
/// Implement this for any game variant to plug into the card conversion layer.
/// </summary>
public interface IDeckDefinition
{
    /// <summary>Name of this deck variant (e.g. "Standard 52", "Canasta Double Deck").</summary>
    string Name { get; }

    /// <summary>All cards in the deck (may contain duplicates for multi-deck games).</summary>
    IReadOnlyList<Card> Cards { get; }

    /// <summary>Whether this deck includes physical jokers.</summary>
    bool HasJokers { get; }

    /// <summary>Number of jokers in this deck.</summary>
    int JokerCount { get; }

    /// <summary>
    /// Given a set of cards in a hand, computes the WildMask ulong —
    /// which of those cards are wild under this game's rules.
    /// </summary>
    ulong ComputeWildMask(ulong handCards);
}