using static System.Console;
using static System.Random;

// Fisher-Yates algorithm implementation for Shuffle from: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
static class DeckMethods
{
    public static readonly byte[] sortedDeck = new byte[]
    {
        2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10,
        11, 11, 11, 11, 12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14
    };
    public static void Shuffle (byte[] decks)
    {
        var rng = new Random();
        int n = decks.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (decks[n], decks[k]) = (decks[k], decks[n]);
        }
    }

    public static byte[] ConstructDecks(int deckNumber)
    {
        byte[] decks = Array.Empty<byte>();
        for (var i = 0; i < deckNumber; ++i)
        {
            decks = decks.Concat(sortedDeck).ToArray();
        }
        Shuffle(decks);
        return decks;
    }
}

class Program
{
    static void Main()
    {
    }
}     
