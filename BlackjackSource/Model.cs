namespace BlackjackSource;

using static Console;
using static BasicStrategy.Action;
using static Hand.States;

internal static class DeckMethods
{
    private static readonly (byte Value, char Suit)[] SortedDeck = {
        (2, 'H'), (2, 'D'), (2, 'S'), (2, 'C'), (3, 'H'), (3, 'D'), (3, 'S'), (3, 'C'), (4, 'H'), (4, 'D'), (4, 'S'), (4, 'C'),
        (5, 'H'), (5, 'D'), (5, 'S'), (5, 'C'), (6, 'H'), (6, 'D'), (6, 'S'), (6, 'C'), (7, 'H'), (7, 'D'), (7, 'S'), (7, 'C'),
        (8, 'H'), (8, 'D'), (8, 'S'), (8, 'C'), (9, 'H'), (9, 'D'), (9, 'S'), (9, 'C'), (10, 'H'), (10, 'D'), (10, 'S'), (10, 'C'),
        (11, 'H'), (11, 'D'), (11, 'S'), (11, 'C'), (12, 'H'), (12, 'D'), (12, 'S'), (12, 'C'), (13, 'H'), (13, 'D'), (13, 'S'), (13, 'C'),
        (14, 'H'), (14, 'D'), (14, 'S'), (14, 'C')
    };

    // Fisher-Yates algorithm implementation for Shuffle from: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
    private static void Shuffle (ref Queue<(byte Value, char Suit)> shoe)
    {
        var rng = new Random();
        var temp = shoe.ToArray();
        var n = temp.Length;
        while (n > 1)
        {
            var k = rng.Next(n--);
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

internal class Stats
{
    // Session Win/Loss, Basic Strategy adherence %, Avg Bet, % loss of total bet
    public readonly int BankrollStart;
    public float BankrollCurr;
    public int TotalBet;
    public int HandsPlayed;
    public int CorrectAct;
    public int TotalAct;

    public readonly int ActiveHands;

    public Stats(int bankroll, int handNumber)
    {
        BankrollStart = bankroll;
        BankrollCurr = bankroll;
        ActiveHands = handNumber;
    }
}

internal class Hand
{
    public enum States
    {
        Hard, Soft, Pair, Blackjack
    }
    public readonly List<(byte Value, char Suit)> Cards;
    public int Total;
    public int Bet;
    public bool BJ;
    public States State;
    public int CardCount => Cards.Count;

    public Hand(int bet, ref Stats session)
    {
        session.BankrollCurr -= bet;
        Cards = new List<(byte Value, char Suit)>();
        Bet = bet;
        Total = 0;
    }
    private void RecalculateTotal()
    {
        Total = 0;
        var count = 0;
        var softFlag = false;
        foreach (var card in Cards)
        {
            if (card.Value != 14)
            {
                Total = card.Value > 10 ? Total + 10 : Total + card.Value;
                continue;
            }

            ++count;
        }

        for (; count > 0; --count)
        {
            if (Total + 11 > 21)
            {
                Total += 1;
                continue;
            }

            softFlag = true;
            Total += 11;
        }

        if (Cards.Count == 2 && Cards[0].Value == Cards[1].Value)
        {
            State = Pair;
            return;
        }
        if (softFlag)
        {
            State = Soft;
            return;
        }
        State = Hard;
    }

    private bool Done()
    {
        switch (Total)
        {
            case 21:
                BJ = CardCount == 2;
                if (BJ) State = Blackjack;
                return true;
            case > 21:
                return true;
            default:
                return false;
        }
    }

    public BasicStrategy.Action? OptimalAct(int dealerCardVal)
    {
        switch (State)
        {
            case Pair when Cards[0].Value == 14: // always split aces, not encoded in BasicStrat[]
                return BasicStrategy.Action.Split;
            case Pair:
                return BasicStrategy.BasicStrat[Total][dealerCardVal].Pair; // no ambiguity for double, pair => exactly 2 cards
            case Hard when BasicStrategy.BasicStrat[Total][dealerCardVal].Hard == BasicStrategy.Action.Doubl:
                return CardCount == 2 ? BasicStrategy.Action.Doubl : BasicStrategy.Action.Hit;
            case Hard when BasicStrategy.BasicStrat[Total][dealerCardVal].Hard == BasicStrategy.Action.Surrender:
                return CardCount == 2 ? BasicStrategy.Action.Surrender : BasicStrategy.Action.Hit;
            case Hard:
                return BasicStrategy.BasicStrat[Total][dealerCardVal].Hard;
            case Soft when BasicStrategy.BasicStrat[Total][dealerCardVal].Soft == BasicStrategy.Action.Doubl:
                return CardCount == 2 ? BasicStrategy.Action.Doubl : BasicStrategy.Action.Hit;
            case Soft:
                return BasicStrategy.BasicStrat[Total][dealerCardVal].Soft;
        }

        return null;
    }

    // Method for Stand not implemented, action transference handled in a loop in main.
    public bool Hit(ref Queue<(byte Value, char Suit)> shoe)
    {
        var card = shoe.Dequeue();
        Cards.Add(card);
        RecalculateTotal();
        return Done();
    }

    public bool Doubl(ref Queue<(byte Value, char Suit)> shoe, ref Stats session)
    {
        Hit(ref shoe);
        session.BankrollCurr -= Bet;
        Bet += Bet;
        return true;
    }

    public Hand[] Split(ref Queue<(byte Value, char Suit)> shoe, ref Stats session)
    {
        session.BankrollCurr -= Bet;
        var newHs = new[] { new Hand(Bet, ref session), new Hand(Bet, ref session) };

        for (var i = 0; i < 2; ++i)
        {
            var splitCard = Cards[i];
            newHs[i].Cards.Add(splitCard);
            newHs[i].RecalculateTotal();
            newHs[i].Hit(ref shoe);
        }

        return newHs;
    }

    public bool Surrender(ref Stats session)
    {
        Total = 22; // Simulates bust for Tally
        session.BankrollCurr += (float) Bet / 2;
        return true;
    }
}

internal static class BasicStrategy
{
    private static Stats? Session;
    private static Queue<(byte, char)>? Shoe;
    private static List<Hand>? Hands;
    private static Queue<Hand>? Turn;
    public enum Action
    {
        Hit,
        Stand,
        Doubl,
        Surrender,
        Split
    }

    // Basic strategy chart for 4-8 decks, dealer stand on any 17, double allowed after split,
    // surrender offered, and no count variation!! (literally a whole other dimension of complexity, 3D array).
    // details such as hit/surrender if allowed or not, should generally have low impact on EV and also basic strategy
    // differences (can be learned by heart).
    // Usage: basicStrat[handTotal][dealerCard].handState; null/empty array are impossible combinations, for example,
    // hand total being less than 4, an even number being a pair, or the dealer having a value less than 2, however,
    // they have been included so that the usage would be intuitive.
    public static readonly (Action? Hard, Action? Soft, Action? Pair)[][] BasicStrat =
    {
        new (Action? Hard, Action? Soft, Action? Pair)[] { },
        new (Action? Hard, Action? Soft, Action? Pair)[] { },
        new (Action? Hard, Action? Soft, Action? Pair)[] { },
        new (Action? Hard, Action? Soft, Action? Pair)[] { },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (null, null, Split), (null, null, Split), (null, null, Split),
            (null, null, Split), (null, null, Split), (null, null, Split), (null, null, Hit), (null, null, Hit),
            (null, null, Hit), (null, null, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null),
            (Hit, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null),
            (Hit, null, null), (Hit, null, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, Split), (Hit, null, Split), (Hit, null, Split),
            (Hit, null, Split), (Hit, null, Split), (Hit, null, Split), (Hit, null, Hit), (Hit, null, Hit),
            (Hit, null, Hit), (Hit, null, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null),
            (Hit, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null),
            (Hit, null, null), (Hit, null, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, Hit), (Hit, null, Hit), (Hit, null, Hit),
            (Hit, null, Split), (Hit, null, Split), (Hit, null, Hit), (Hit, null, Hit), (Hit, null, Hit),
            (Hit, null, Hit), (Hit, null, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, null), (Doubl, null, null), (Doubl, null, null),
            (Doubl, null, null), (Doubl, null, null), (Hit, null, null), (Hit, null, null), (Hit, null, null),
            (Hit, null, null), (Hit, null, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Doubl, null, Doubl), (Doubl, null, Doubl), (Doubl, null, Doubl),
            (Doubl, null, Doubl), (Doubl, null, Doubl), (Doubl, null, Doubl), (Doubl, null, Doubl),
            (Doubl, null, Doubl), (Hit, null, Hit), (Hit, null, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Doubl, null, null), (Doubl, null, null), (Doubl, null, null),
            (Doubl, null, null), (Doubl, null, null), (Doubl, null, null), (Doubl, null, null), (Doubl, null, null),
            (Doubl, null, null), (Hit, null, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Hit, null, Split), (Hit, null, Split), (Stand, null, Split),
            (Stand, null, Split), (Stand, null, Split), (Hit, null, Hit), (Hit, null, Hit), (Hit, null, Hit),
            (Hit, null, Hit), (Hit, null, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Hit, null), (Stand, Hit, null), (Stand, Hit, null),
            (Stand, Doubl, null), (Stand, Doubl, null), (Hit, Hit, null), (Hit, Hit, null), (Hit, Hit, null),
            (Hit, Hit, null), (Hit, Hit, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Hit, Split), (Stand, Hit, Split), (Stand, Hit, Split),
            (Stand, Doubl, Split), (Stand, Doubl, Split), (Hit, Hit, Split), (Hit, Hit, Hit), (Hit, Hit, Hit),
            (Hit, Hit, Hit), (Hit, Hit, Hit)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Hit, null), (Stand, Hit, null), (Stand, Doubl, null),
            (Stand, Doubl, null), (Stand, Doubl, null), (Hit, Hit, null), (Hit, Hit, null), (Hit, Hit, null),
            (Surrender, Hit, null), (Hit, Hit, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Hit, Split), (Stand, Hit, Split), (Stand, Doubl, Split),
            (Stand, Doubl, Split), (Stand, Doubl, Split), (Hit, Hit, Split), (Hit, Hit, Split), (Surrender, Hit, Split),
            (Surrender, Hit, Split), (Surrender, Hit, Split)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Hit, null), (Stand, Doubl, null), (Stand, Doubl, null),
            (Stand, Doubl, null), (Stand, Doubl, null), (Stand, Hit, null), (Stand, Hit, null), (Stand, Hit, null),
            (Stand, Hit, null), (Stand, Hit, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Stand, Split), (Stand, Doubl, Split), (Stand, Doubl, Split),
            (Stand, Doubl, Split), (Stand, Doubl, Split), (Stand, Stand, Stand), (Stand, Stand, Split),
            (Stand, Hit, Split), (Stand, Hit, Stand), (Stand, Hit, Stand)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Stand, null), (Stand, Stand, null), (Stand, Stand, null),
            (Stand, Stand, null), (Stand, Stand, null), (Stand, Stand, null), (Stand, Stand, null),
            (Stand, Stand, null), (Stand, Stand, null), (Stand, Stand, null)
        },
        new (Action? Hard, Action? Soft, Action? Pair)[]
        {
            (null, null, null), (null, null, null), (Stand, Stand, Stand), (Stand, Stand, Stand), (Stand, Stand, Stand),
            (Stand, Stand, Stand), (Stand, Stand, Stand), (Stand, Stand, Stand), (Stand, Stand, Stand),
            (Stand, Stand, Stand), (Stand, Stand, Stand), (Stand, Stand, Stand)
        },
    };

    private static void Draw()
    {
        Clear();
        var acting = Turn!.Peek();
        var dealer = Hands![^1];
        for (var i = 0; i < Hands.Count / 2 + 1; ++i)
        {
            Write("    ");
        }
        Write($"Dealer showing: {dealer.Cards[0].Value} ");
        if (Turn.Count == 1)
        {
            for (var i = 1; i < dealer.CardCount; ++i)
            {
                Write($"{dealer.Cards[i].Value} ");
            }
        }
        WriteLine("\n\nYour Hand cards (Total State : Bet):");

        foreach (var hand in Hands.TakeWhile(hand => hand != Hands[^1]))
        {
            if (hand == acting)
            {
                Write("Acting >");
            }

            foreach (var card in hand.Cards)
            {
                Write($"{card.Value} ");
            }

            Write($"({hand.Total} {hand.State} : {hand.Bet})");

            Write("    ");
        }
        WriteLine($"\nBankroll: {Session!.BankrollCurr}");
    }

    private static void DrawStats()
    {
        var net = Session!.BankrollCurr - Session.BankrollStart;

        Clear();
        WriteLine($"Net: {net}");
        WriteLine($"Basic strategy adherence: {Session.CorrectAct*100.00/Session.TotalAct}%");
        WriteLine($"Average bet: {(float)Session.TotalBet/Session.HandsPlayed}");
        WriteLine($"Win/loss per hand: {net/Session.HandsPlayed}");

        Write("Place new bets? Yes/New (session): ");
        if (ReadLine()!.ToUpper() != "YES") Main();
    }

    private static void BetAndDeal()
    {
        for (var i = 1; i <= Session!.ActiveHands; ++i)
        {
            WriteLine($"Bankroll: {Session.BankrollCurr}\nBet for hand {i}");
            var bet = int.Parse(ReadLine()!);
            while (Session.BankrollCurr - bet < 0)
            {
                Clear();
                WriteLine($"Bankroll: {Session.BankrollCurr}\nBet for hand {i} greater than available bankroll, try again");
                bet = int.Parse(ReadLine()!);
            }
            Hands!.Add(new Hand(bet, ref Session));
            Session.TotalBet += Hands[^1].Bet;
            ++Session.HandsPlayed;
            Clear();
        }

        Hands!.Add(new Hand(0, ref Session));

        for (var i = 0; i < 2; ++i)
        {
            Hands[^1].Hit(ref Shoe!);

            foreach (var hand in Hands.TakeWhile(hand => hand != Hands[^1]))
            {
                hand.Hit(ref Shoe);
            }
        }
    }

    private static void Tally()
    {
        foreach (var hand in Hands!)
        {
            var dealer = Hands[^1];
            if (hand.Bet <= 0) continue;
            if (hand.BJ)
            {
                Session!.BankrollCurr += !dealer.BJ ? 5 / 2 * hand.Bet : hand.Bet;
            }
            else if ((dealer.Total < hand.Total || dealer.Total > 21) && hand.Total < 22)
            {
                Session!.BankrollCurr += 2 * hand.Bet;
            }
            else if (dealer.Total == hand.Total && hand.Total < 22)
            {
                Session!.BankrollCurr += hand.Bet;
            }
        }
    }

    private static bool Play(Hand acting, Action act)
    {
        switch (act)
        {
            case Split:
            {
                var deq = Turn!.Dequeue();
                var tempTurn = Turn.ToList();
                var splits = acting.Split(ref Shoe!, ref Session!);

                Turn.Clear();
                Turn.Enqueue(deq); Turn.Enqueue(splits[0]); Turn.Enqueue(splits[1]);
                foreach (var hand in tempTurn)
                {
                    Turn.Enqueue(hand);
                }

                var tempHands = Hands!.ToArray();
                Hands.Clear();
                for (var i = 0; i < tempHands.Length - Turn.Count; ++i)
                {
                    Hands.Add(tempHands[i]);
                }
                foreach (var hand in Turn.Where(hand => hand != deq))
                {
                    Hands.Add(hand);
                }

                return true;
            }
            case Hit:
                return acting.Hit(ref Shoe!);
            case Doubl:
                return acting.Doubl(ref Shoe!, ref Session!);
            case Surrender:
                return acting.Surrender(ref Session!);
            case Stand:
                return true;
            default:
                return false;
        }
    }
    private static void Main()
    {
        Clear();
        WriteLine("Enter starting bankroll and number of hands to play");
        Session = new Stats(int.Parse(ReadLine()!), int.Parse(ReadLine()!));
        Clear();
        Shoe = DeckMethods.ConstructShoe(8);

        while (Session.BankrollCurr > 0)
        {
            Clear();
            Hands = new List<Hand>();

            BetAndDeal();

            Turn = new Queue<Hand>(Hands);

            while (Turn.Count > 1)
            {
                var acting = Turn.Peek();
                var dealer = Hands[^1];
                var done = false;

                while(!done){
                    Draw();
                    if (acting.BJ) break;

                    Write("Write action: ");
                    Enum.TryParse(ReadLine()!, out Action act);

                    var dealerCardVal = dealer.Cards[0].Value == 14 ? 11 :
                        dealer.Cards[0].Value < 10 ? dealer.Cards[0].Value : 10;
                    var optimal = acting.OptimalAct(dealerCardVal);

                    if (act == optimal) ++Session.CorrectAct;
                    ++Session.TotalAct;

                    done = Play(acting, act);
                }

                Turn.Dequeue();
            }

            while (Turn.Peek().Total < 17) // Dealer behaviour
            {
                Turn.Peek().Hit(ref Shoe);
                Draw();
                Thread.Sleep(750);
            }

            Tally();
            Draw();

            Write("\nPlace new bets? Yes/Stats: ");
            var answer = ReadLine()!;

            if (answer.ToUpper() != "STATS") continue; //Session Win/Loss, Basic Strategy adherence %, Avg Bet, % loss of total bet
            DrawStats();
        }
    }
}
