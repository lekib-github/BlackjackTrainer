using Gdk;
using Gtk;

namespace BlackjackSource;

internal class BST : Gtk.Window
{
    public BST(BasicStrategy game) : base("Basic Strategy Trainer")
    {
        Maximize();

        var toolbarBox = new Box(Orientation.Horizontal,0);
        var statButton = new Button("Show Stats");
        toolbarBox.PackStart(statButton, false, false, 50);
        var mainMenuButton = new Button("Main Menu");
        mainMenuButton.Sensitive = false; // to implement
        toolbarBox.PackEnd(mainMenuButton, false, false, 50);

        var statBox = new Box(Orientation.Vertical,0);
        statBox.MarginTop = 10;
        statBox.MarginStart = 10;
        statBox.MarginEnd = 100;
        var net = game.BankrollCurr - game.BankrollStart;
        var statNet = new Label($"Net: {net}");
        var statAdherence = new Label($"Basic strategy adherence: {game.CorrectAct * 100.00 / game.TotalAct}%");
        var statAvgBet = new Label($"Average bet: {(float)game.TotalBet / game.HandsPlayed}");
        var statWinLoss = new Label($"Win/loss per hand: {net / game.HandsPlayed}");
        statBox.PackStart(statNet, false,false,10);
        statBox.PackStart(statAdherence, false, false, 10);
        statBox.PackStart(statAvgBet,false,false,10);
        statBox.PackStart(statWinLoss,false,false,10);

        var dealerGrid = new Grid();
        dealerGrid.Attach(new Label("Hand"),0,0,1,1);
        dealerGrid.Attach(new Label("Deck left"),1,0,1,1);

        var handsGrid = new Grid();
        var actionGrid = new Grid();

        var dealerStats = new Box(Orientation.Horizontal,20);
        dealerStats.Add(statBox);
        dealerStats.Add(new Separator(Orientation.Vertical));
        dealerStats.Add(dealerGrid);

        var container = new Box(Orientation.Vertical, 10);
        container.Add(toolbarBox);
        container.Add(new Separator(Orientation.Horizontal));
        container.Add(dealerStats);
        container.Add(new Separator(Orientation.Horizontal));
        container.Add(handsGrid);
        container.Add(actionGrid);
        Add(container);
        statButton.Clicked += ShowStats;

        void ShowStats(object? sender, EventArgs args)
        {
            statNet.Visible = !statNet.Visible;
            statAdherence.Visible = !statAdherence.Visible;
            statAvgBet.Visible = !statAvgBet.Visible;
            statWinLoss.Visible = !statWinLoss.Visible;
        }

        AddEvents((int) EventMask.ButtonPressMask);
    }
}

internal class SessionRules : Gtk.Window
{
    private BasicStrategy Game;

    public SessionRules() : base("Basic Strategy Trainer")
    {
        Game = new BasicStrategy();

        var bankrollEntry = new Entry();
        bankrollEntry.MarginEnd = 50;

        var handNumberCombo = new ComboBoxText();
        for (var i = 1; i < 7; i++)
        {
            handNumberCombo.AppendText($"{i}");
        }

        var confirmButton = new Button("Confirm");
        confirmButton.Clicked += GetSessionParams;
        confirmButton.MarginTop = 50;

        Grid grid = new Grid();
        grid.Halign = Align.Center;
        grid.Valign = Align.Center;

        grid.Attach(new Label("Enter bankroll (integer)"), 0, 0, 1, 1);
        grid.Attach(bankrollEntry,1,0,1,1);
        grid.Attach(new Label("Enter number of hands to play (1-6)"), 3, 0, 1, 1);
        grid.Attach(handNumberCombo, 4,0,1,1);

        grid.Attach(confirmButton, 4, 1, 1, 1);
        Add(grid);

        void GetSessionParams(object? sender, EventArgs args)
        {
            Game.SessionInit(int.Parse(bankrollEntry.Text), int.Parse(handNumberCombo.ActiveText));
            var main = new BST(Game);
            Destroy();
            main.ShowAll();
        }
    }

    protected override bool OnDeleteEvent(Event e) {
        Application.Quit();
        return true;
    }
}

internal static class Driver {
    private static void Main() {
        Application.Init();
        var sessionRules = new SessionRules();
        sessionRules.ShowAll();
        Application.Run();
    }
}