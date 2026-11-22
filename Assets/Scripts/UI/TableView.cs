using PokerDemo.Core;
using PokerDemo.Network;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // The table screen. The layout is in the scene (Canvas > Background > TablePanel),
    // this script fills it from the snapshot the host sends and turns button clicks into actions.
    public class TableView : MonoBehaviour
    {
        private static readonly string[] StageNames = { "Pre-flop", "Flop", "Turn", "River", "Showdown" };

        [Header("Network")]
        [SerializeField] private PokerNetwork network;
        [SerializeField] private TableNetwork table;

        [Header("Table")]
        [SerializeField] private GameObject tablePanel;
        [SerializeField] private Text tableCodeText;
        [SerializeField] private Text potText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text noticeText;
        [SerializeField] private CardView[] boardCards;
        [SerializeField] private SeatView[] seats;   // slot 0 is always the local player
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;

        [Header("Host and spectator buttons")]
        [SerializeField] private Button addCautiousButton;
        [SerializeField] private Button addBoldButton;
        [SerializeField] private Button removeBotsButton;
        [SerializeField] private Button restartButton;

        [Header("Actions")]
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkButton;
        [SerializeField] private Button callButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private Button allInButton;
        [SerializeField] private Button halfPotButton;
        [SerializeField] private Button potButton;
        [SerializeField] private Text callLabel;
        [SerializeField] private Text raiseLabel;
        [SerializeField] private InputField raiseInput;
        [SerializeField] private Text raiseHintText;

        private bool wasMyTurn;

        private void Awake()
        {
            startButton.onClick.AddListener(table.StartGame);
            leaveButton.onClick.AddListener(network.LeaveRoom);
            addCautiousButton.onClick.AddListener(() => table.AddBot(BotLevel.Cautious));
            addBoldButton.onClick.AddListener(() => table.AddBot(BotLevel.Bold));
            removeBotsButton.onClick.AddListener(table.RemoveBots);
            restartButton.onClick.AddListener(table.RequestRestart);

            foldButton.onClick.AddListener(() => SendAction(ActionType.Fold));
            checkButton.onClick.AddListener(() => SendAction(ActionType.Check));
            callButton.onClick.AddListener(() => SendAction(ActionType.Call));
            allInButton.onClick.AddListener(() => SendAction(ActionType.AllIn));
            raiseButton.onClick.AddListener(OnRaiseClicked);
            halfPotButton.onClick.AddListener(() => SetQuickRaise(0.5f));
            potButton.onClick.AddListener(() => SetQuickRaise(1f));
        }

        private void Update()
        {
            tablePanel.SetActive(network.InRoom);
            if (!network.InRoom)
            {
                wasMyTurn = false;
                return;
            }

            tableCodeText.text = "Table " + network.RoomCode;
            noticeText.text = table.Notice;

            TableSnapshot snapshot = table.Snapshot;
            if (snapshot == null)
            {
                messageText.text = "Waiting for the host...";
                potText.text = "";
                turnText.text = "";
                HideSeatsAndBoard();
                SetActionsEnabled(false);
                SetSideButtons(null, null);
                return;
            }

            SeatSnapshot me = snapshot.seats.FirstOrDefault(s => s.playerId == table.MyId);
            DrawBoard(snapshot);
            DrawSeats(snapshot, me);
            DrawTexts(snapshot, me);
            DrawActions(snapshot, me);
            SetSideButtons(snapshot, me);
        }

        // Start and bot buttons are for the host, restart is for a player who ran out of chips.
        private void SetSideButtons(TableSnapshot snapshot, SeatSnapshot me)
        {
            bool spectating = me != null && me.spectating;
            bool host = table.IsHost && snapshot != null;

            startButton.gameObject.SetActive(table.CanStart && !spectating);
            addCautiousButton.gameObject.SetActive(host && snapshot.seats.Length < seats.Length);
            addBoldButton.gameObject.SetActive(host && snapshot.seats.Length < seats.Length);
            removeBotsButton.gameObject.SetActive(host && snapshot.seats.Any(s => s.isBot));
            restartButton.gameObject.SetActive(spectating);
        }

        private void DrawBoard(TableSnapshot snapshot)
        {
            for (int i = 0; i < boardCards.Length; i++)
            {
                if (i < snapshot.board.Length)
                {
                    boardCards[i].ShowCard(snapshot.board[i]);
                }
                else
                {
                    boardCards[i].ShowEmpty();
                }
            }
        }

        // The local player is always drawn at the bottom, the other seats follow clockwise.
        private void DrawSeats(TableSnapshot snapshot, SeatSnapshot me)
        {
            int mySeat = me != null ? me.seat : 0;
            string[] myCards = table.MyHoleCards;

            for (int slot = 0; slot < seats.Length; slot++)
            {
                int seatNumber = (slot + mySeat) % seats.Length;
                SeatSnapshot seat = snapshot.seats.FirstOrDefault(s => s.seat == seatNumber);

                if (seat == null)
                {
                    seats[slot].Clear();
                    continue;
                }

                bool isTurn = snapshot.handActive && seat.seat == snapshot.currentSeat;
                bool isDealer = snapshot.handNumber > 0 && seat.seat == snapshot.buttonSeat;
                seats[slot].Show(seat, seat == me, isTurn, isDealer, myCards);
            }
        }

        private void DrawTexts(TableSnapshot snapshot, SeatSnapshot me)
        {
            messageText.text = snapshot.message;
            potText.text = snapshot.handNumber == 0 ? "" : StageNames[snapshot.stage] + "     Pot " + snapshot.pot;

            SeatSnapshot current = snapshot.seats.FirstOrDefault(s => s.seat == snapshot.currentSeat);
            if (snapshot.handActive && current != null)
            {
                string who = current.playerId == table.MyId ? "YOUR TURN" : current.name + "'s turn";
                turnText.text = who + "   " + Mathf.CeilToInt(table.TurnSecondsLeft) + "s";
            }
            else if (me != null && me.spectating)
            {
                turnText.text = "Out of chips - watching";
            }
            else if (snapshot.handActive && me != null && !me.inHand)
            {
                turnText.text = "Waiting for the next hand";
            }
            else
            {
                turnText.text = "";
            }
        }

        private void DrawActions(TableSnapshot snapshot, SeatSnapshot me)
        {
            bool myTurn = me != null && snapshot.handActive && snapshot.currentSeat == me.seat;
            ActionOptions options = snapshot.options;

            if (!myTurn)
            {
                SetActionsEnabled(false);
                wasMyTurn = false;
                return;
            }

            foldButton.interactable = options.CanFold;
            checkButton.interactable = options.CanCheck;
            callButton.interactable = options.CanCall;
            raiseButton.interactable = options.CanRaise;
            halfPotButton.interactable = options.CanRaise;
            potButton.interactable = options.CanRaise;
            raiseInput.interactable = options.CanRaise;
            allInButton.interactable = options.CanAllIn;

            bool callIsAllIn = options.CallAmount >= me.stack;
            callLabel.text = !options.CanCall ? "Call" : (callIsAllIn ? "All-in " : "Call ") + options.CallAmount;
            raiseLabel.text = snapshot.seats.Any(s => s.bet > 0) ? "Raise to" : "Bet";
            raiseHintText.text = options.CanRaise ? "Min " + options.MinRaiseTo + "   Max " + options.MaxRaiseTo : "";

            // Start each turn with the smallest legal raise filled in.
            if (!wasMyTurn && options.CanRaise)
            {
                raiseInput.text = options.MinRaiseTo.ToString();
            }

            wasMyTurn = true;
        }

        private void SetActionsEnabled(bool enabled)
        {
            foldButton.interactable = enabled;
            checkButton.interactable = enabled;
            callButton.interactable = enabled;
            raiseButton.interactable = enabled;
            halfPotButton.interactable = enabled;
            potButton.interactable = enabled;
            raiseInput.interactable = enabled;
            allInButton.interactable = enabled;
            raiseHintText.text = "";
            callLabel.text = "Call";
        }

        private void HideSeatsAndBoard()
        {
            foreach (SeatView seat in seats)
            {
                seat.Clear();
            }

            foreach (CardView card in boardCards)
            {
                card.ShowEmpty();
            }
        }

        private void SendAction(ActionType type)
        {
            table.SendAction(new PlayerAction(type));
        }

        // The amount is kept inside the allowed range here, and the host checks it again.
        private void OnRaiseClicked()
        {
            ActionOptions options = table.Snapshot.options;

            int amount;
            if (!int.TryParse(raiseInput.text, out amount))
            {
                amount = options.MinRaiseTo;
            }

            amount = Mathf.Clamp(amount, options.MinRaiseTo, options.MaxRaiseTo);
            raiseInput.text = amount.ToString();
            table.SendAction(new PlayerAction(ActionType.Raise, amount));
        }

        // Fills in a raise of a share of the pot, counting the call we would have to make first.
        // The player still presses the raise button, so the amount can be changed.
        private void SetQuickRaise(float potShare)
        {
            TableSnapshot snapshot = table.Snapshot;
            ActionOptions options = snapshot.options;
            SeatSnapshot me = snapshot.seats.First(s => s.playerId == table.MyId);

            int currentBet = snapshot.seats.Max(s => s.bet);
            int potAfterCall = snapshot.pot + (currentBet - me.bet);
            int amount = currentBet + Mathf.RoundToInt(potAfterCall * potShare);

            raiseInput.text = Mathf.Clamp(amount, options.MinRaiseTo, options.MaxRaiseTo).ToString();
        }
    }
}
