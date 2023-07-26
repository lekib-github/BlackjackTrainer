namespace BlackjackSource;

// Fisher-Yates algorithm implementation for Shuffle from: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
static class DeckMethods
{
    public static readonly (byte Value, char Suit)[] SortedDeck = {
        (2, 'H'), (2, 'D'), (2, 'S'), (2, 'C'), (3, 'H'), (3, 'D'), (3, 'S'), (3, 'C'), (4, 'H'), (4, 'D'), (4, 'S'), (4, 'C'),
        (5, 'H'), (5, 'D'), (5, 'S'), (5, 'C'), (6, 'H'), (6, 'D'), (6, 'S'), (6, 'C'), (7, 'H'), (7, 'D'), (7, 'S'), (7, 'C'),
        (8, 'H'), (8, 'D'), (8, 'S'), (8, 'C'), (9, 'H'), (9, 'D'), (9, 'S'), (9, 'C'), (10, 'H'), (10, 'D'), (10, 'S'), (10, 'C'),
        (11, 'H'), (11, 'D'), (11, 'S'), (11, 'C'), (12, 'H'), (12, 'D'), (12, 'S'), (12, 'C'), (13, 'H'), (13, 'D'), (13, 'S'), (13, 'C'),
        (14, 'H'), (14, 'D'), (14, 'S'), (14, 'C')
    };
    public static void Shuffle ((byte Value, char Suit)[] decks)
    {
        var rng = new Random();
        int n = decks.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (decks[n], decks[k]) = (decks[k], decks[n]);
        }
    }

    public static (byte Value, char Suit)[] ConstructDecks(int deckNumber)
    {
        (byte Value, char Suit)[] decks = { };
        for (var i = 0; i < deckNumber; ++i)
        {
            decks = decks.Concat(SortedDeck).ToArray();
        }
        Shuffle(decks);
        return decks;
    }
}

class BasicStrategy
{
    private enum Action
    {
        Hit, Stand, Doubl, Split, Surrender
    }
    // Basic strategy chart for 4-8 decks, dealer stand on any 17, double allowed after split,
    // surrender offered, and no count variation!! (literally a whole other dimension of complexity, 3D array).
    // details such as hit/surrender if allowed or not, should generally have low impact on EV.
    // Usage: basicStrat[handTotal][dealerCard].handState; null/empty array are impossible combinations, for example,
    // hand total being less than 4, an even number being a pair, or the dealer having a value less than 2, however,
    // they have been included so that the usage would be intuitive.
    private static readonly (Action? Hard, Action? Soft, Action? Pair)[][] BasicStrat = {
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (null,null,Action.Split), (null,null,Action.Split), (null,null,Action.Split), (null,null,Action.Split), (null,null,Action.Split), (null,null,Action.Split), (null,null,Action.Hit), (null,null,Action.Hit),(null,null,Action.Hit),(null,null,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null), (Action.Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Doubl,null,Action.Doubl), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Doubl,null,null), (Action.Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Hit,null,Action.Split), (Action.Hit,null,Action.Split), (Action.Stand,null,Action.Split), (Action.Stand,null,Action.Split), (Action.Stand,null,Action.Split), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit), (Action.Hit,null,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Hit,Action.Hit,Action.Split), (Action.Hit,Action.Hit,Action.Hit), (Action.Hit,Action.Hit,Action.Hit), (Action.Hit,Action.Hit,Action.Hit), (Action.Hit,Action.Hit,Action.Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null), (Action.Hit,Action.Hit,null), (Action.Surrender,Action.Hit,null), (Action.Hit,Action.Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Hit,Action.Hit,Action.Split), (Action.Hit,Action.Hit,Action.Split), (Action.Surrender,Action.Hit,Action.Split), (Action.Surrender,Action.Hit,Action.Split), (Action.Surrender,Action.Hit,Action.Split)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Doubl,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null), (Action.Stand,Action.Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Stand,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Doubl,Action.Split), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Split), (Action.Stand,Action.Hit,Action.Split), (Action.Stand,Action.Hit,Action.Stand), (Action.Stand,Action.Hit,Action.Stand)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null), (Action.Stand,Action.Stand,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand), (Action.Stand,Action.Stand,Action.Stand)},
    };

    static void Main()
    {
    }
}