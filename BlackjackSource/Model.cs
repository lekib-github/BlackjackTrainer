namespace BlackjackSource;
using static BasicStrategy.Action;

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
    public static void Shuffle (ref Queue<(byte Value, char Suit)> shoe)
    {
        var rng = new Random();
        var temp = shoe.ToArray();
        int n = temp.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (temp[n], temp[k]) = (temp[k], temp[n]);
        }

        shoe = new Queue<(byte Value, char Suit)>(temp);
    }

    public static Queue<(byte Value, char Suit)> ConstructShoe(int deckNumber)
    {
        (byte Value, char Suit)[] decks = { };
        for (var i = 0; i < deckNumber; ++i)
        {
            decks = decks.Concat(SortedDeck).ToArray();
        }

        var shoe = new Queue<(byte Value, char Suit)>(decks);
        Shuffle(ref shoe);
        return shoe;
    }
}

class Stats
{
    // Session Win/Loss, Basic Strategy adherence %, Avg Bet, % loss of total bet
    public int BankrollStart;
    public float BankrollCurr;
    public int TotalBet = 0;
    public int HandsPlayed = 0;
    public int CorrectAct = 0;

    public int CurrentHands;

    public Stats(int bankroll, int handNumber)
    {
        BankrollStart = bankroll;
        BankrollCurr = bankroll;
        CurrentHands = handNumber;
    }
}
class Hand
{
    private List<(byte Value, char Suit)> Cards;
    private int total;
    private int Bet;
    private bool BJ;
    public int CardCount => Cards.Count;

    public Hand(int bet, ref Stats session)
    {
        session.BankrollCurr -= bet;
        Cards = new List<(byte Value, char Suit)>();
        Bet = bet;
        total = 0;
    }
    private void AddVal((byte Value, char Suit) card)
    {
        if (card.Value != 14)
        {
            total = card.Value > 10 ? total + 10 : total + card.Value;
        }
        else
        {
            total = total + 11 > 21 ? total + 1 : total + 11;
        }
    }

    private bool Done()
    {
        if (total == 21)
        {
            BJ = CardCount == 2;
            return true;
        }
        if (total > 21)
        {
            return true;
        }

        return false;
    }

    public bool DealCard(ref Queue<(byte Value, char Suit)> shoe)
    {
        var card = shoe.Dequeue();
        Cards.Add(card);
        AddVal(card);
        return Done();
    }

    public bool Hit(ref Queue<(byte Value, char Suit)> shoe)
    {
        var card = shoe.Dequeue();
        Cards.Add(card);
        AddVal(card);
        return Done();
    }

    public bool Stand()
    {
        return true;
    }

    public bool Doubl(ref Queue<(byte Value, char Suit)> shoe, ref Stats session)
    {
        var card = shoe.Dequeue();
        Cards.Add(card);
        AddVal(card);
        session.BankrollCurr -= Bet;
        Bet += Bet;
        return true;
    }

    public Hand[] Split(ref Queue<(byte Value, char Suit)> shoe, ref Stats session)
    {
        session.BankrollCurr -= Bet;
        var newHs = new[] { new Hand(Bet, ref session), new Hand(Bet, ref session) };

        for (int i = 0; i < 2; ++i)
        {
            var splitCard = Cards[i];
            newHs[i].Cards.Add(splitCard);
            newHs[i].AddVal(splitCard);
            newHs[i].DealCard(ref shoe);
        }

        return newHs;
    }

    public bool Surrender(ref Stats session)
    {
        total = 22; // simulate bust at end of round tally (when implemented)
        session.BankrollCurr += (float) Bet / 2;
        return true;
    }
}

class BasicStrategy
{
    public enum Action
    {
        Hit, Stand, Doubl, Split, Surrender
    }
    // Basic strategy chart for 4-8 decks, dealer stand on any 17, double allowed after split,
    // surrender offered, and no count variation!! (literally a whole other dimension of complexity, 3D array).
    // details such as hit/surrender if allowed or not, should generally have low impact on EV and also basic strategy
    // differences (can be learned by heart).
    // Usage: basicStrat[handTotal][dealerCard].handState; null/empty array are impossible combinations, for example,
    // hand total being less than 4, an even number being a pair, or the dealer having a value less than 2, however,
    // they have been included so that the usage would be intuitive.
    private static readonly (Action? Hard, Action? Soft, Action? Pair)[][] BasicStrat = {
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (null,null,Split), (null,null,Split), (null,null,Split), (null,null,Split), (null,null,Split), (null,null,Split), (null,null,Hit), (null,null,Hit),(null,null,Hit),(null,null,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,Split), (Hit,null,Split), (Hit,null,Split), (Hit,null,Split), (Hit,null,Split), (Hit,null,Split), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Split), (Hit,null,Split), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null), (Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Doubl,null,Doubl), (Hit,null,Hit), (Hit,null,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Doubl,null,null), (Hit,null,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Hit,null,Split), (Hit,null,Split), (Stand,null,Split), (Stand,null,Split), (Stand,null,Split), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit), (Hit,null,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Doubl,null), (Stand,Doubl,null), (Hit,Hit,null), (Hit,Hit,null), (Hit,Hit,null), (Hit,Hit,null), (Hit,Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Hit,Split), (Stand,Hit,Split), (Stand,Hit,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Hit,Hit,Split), (Hit,Hit,Hit), (Hit,Hit,Hit), (Hit,Hit,Hit), (Hit,Hit,Hit)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Doubl,null), (Stand,Doubl,null), (Stand,Doubl,null), (Hit,Hit,null), (Hit,Hit,null), (Hit,Hit,null), (Surrender,Hit,null), (Hit,Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Hit,Split), (Stand,Hit,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Hit,Hit,Split), (Hit,Hit,Split), (Surrender,Hit,Split), (Surrender,Hit,Split), (Surrender,Hit,Split)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Hit,null), (Stand,Doubl,null), (Stand,Doubl,null), (Stand,Doubl,null), (Stand,Doubl,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Hit,null), (Stand,Hit,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Stand,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Stand,Doubl,Split), (Stand,Stand,Stand), (Stand,Stand,Split), (Stand,Hit,Split), (Stand,Hit,Stand), (Stand,Hit,Stand)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null), (Stand,Stand,null)},
        new (Action? Hard, Action? Soft, Action? Pair)[] {(null,null,null), (null,null,null), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand), (Stand,Stand,Stand)},
    };

    static void Main()
    {
    }
}