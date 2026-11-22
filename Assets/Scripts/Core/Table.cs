using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    // One poker table: who is sitting, the current hand, the turn timer and the pause between hands.
    // It knows nothing about Photon. The network layer feeds it actions and sends out its snapshots.
    public sealed class Table
    {
        public const int MaxSeats = 6;
        public const int StartingChips = 1000;
        public const int SmallBlind = 10;
        public const int BigBlind = 20;
        public const float TurnSeconds = 30f;
        public const float NextHandDelay = 6f;

        private readonly List<TablePlayer> players = new List<TablePlayer>();
        private readonly Random random = new Random();
        private Hand hand;
        private int previousButton = -1;
        private int nextBotId = -1;
        private float turnTimeLeft;
        private float botDelay;
        private float nextHandTimer;
        private bool autoStart;
        private string message = "Waiting for players.";

        // Goes up whenever something changes, so the network layer knows when to send an update.
        public int Version { get; private set; }
        public int HandNumber { get; private set; }
        public bool HandActive => hand != null && !hand.IsFinished;
        public IReadOnlyList<TablePlayer> Players => players;

        // Enough players with chips for a normal hand.
        public bool CanStart => !HandActive && FundedCount() >= 2;

        // The host can also start with a single player, bots then fill the table.
        public bool CanStartGame => !HandActive && FundedCount() >= 1;

        // ---- seating ----

        public TablePlayer AddPlayer(int id, string name)
        {
            TablePlayer existing = GetPlayer(id);
            if (existing != null)
            {
                return existing;
            }

            int seat = FreeSeat();
            if (seat < 0)
            {
                // A full table makes room for a person by sending a bot away.
                TablePlayer bot = players.FirstOrDefault(p => p.IsBot && !InCurrentHand(p));
                if (bot == null)
                {
                    return null;
                }

                players.Remove(bot);
                seat = bot.Seat;
            }

            var player = new TablePlayer(id, name, seat, StartingChips);
            players.Add(player);
            Version++;
            return player;
        }

        public TablePlayer AddBot(BotLevel level)
        {
            int seat = FreeSeat();
            if (seat < 0)
            {
                return null;
            }

            int number = players.Count(p => p.IsBot && p.Level == level) + 1;
            var bot = new TablePlayer(nextBotId--, level + " Bot " + number, seat, StartingChips)
            {
                IsBot = true,
                Level = level
            };

            players.Add(bot);
            Version++;
            return bot;
        }

        public void RemoveBots()
        {
            foreach (TablePlayer bot in players.Where(p => p.IsBot).ToList())
            {
                RemovePlayer(bot.Id);
            }
        }

        // The player is gone for good. In the middle of a hand they are folded and removed afterwards.
        public void RemovePlayer(int id)
        {
            TablePlayer player = GetPlayer(id);
            if (player == null)
            {
                return;
            }

            if (InCurrentHand(player))
            {
                player.Connected = false;
                player.Left = true;
            }
            else
            {
                players.Remove(player);
            }

            Version++;
        }

        // The connection dropped. The seat and chips stay, and the turn timer folds them if needed.
        public void PlayerDisconnected(int id)
        {
            TablePlayer player = GetPlayer(id);
            if (player != null)
            {
                player.Connected = false;
                Version++;
            }
        }

        public void PlayerReconnected(int id)
        {
            TablePlayer player = GetPlayer(id);
            if (player != null)
            {
                player.Connected = true;
                Version++;
            }
        }

        public TablePlayer GetPlayer(int id)
        {
            return players.Find(p => p.Id == id);
        }

        // A player with no chips left can start again with the starting amount. Nothing is bought.
        public bool Restart(int playerId, out string error)
        {
            error = null;

            TablePlayer player = GetPlayer(playerId);
            if (player == null)
            {
                error = "You are not seated at this table.";
                return false;
            }

            if (InCurrentHand(player))
            {
                error = "Wait until the hand is over.";
                return false;
            }

            if (player.Stack > 0)
            {
                error = "You still have chips.";
                return false;
            }

            player.Stack = StartingChips;
            Version++;
            return true;
        }

        // ---- hands ----

        public bool StartHand()
        {
            if (!CanStart)
            {
                return false;
            }

            List<BettingSeat> seats = players
                .Where(p => p.Stack > 0 && p.Connected)
                .Select(p => new BettingSeat(p.Seat, p.Stack))
                .ToList();

            hand = new Hand(seats, previousButton, MaxSeats, SmallBlind, BigBlind, new Deck());
            previousButton = hand.ButtonSeat;
            HandNumber++;
            autoStart = true;
            message = "Hand " + HandNumber;

            foreach (TablePlayer p in players)
            {
                p.LastAction = "";
                p.Won = 0;
            }

            AfterHandChange();
            return true;
        }

        // Starts a hand. When fewer than two players have chips, one bot of each kind joins in.
        public bool StartGame()
        {
            if (!CanStartGame)
            {
                return false;
            }

            if (FundedCount() < 2)
            {
                AddBot(BotLevel.Cautious);
                AddBot(BotLevel.Bold);
            }

            return StartHand();
        }

        public bool TryAct(int playerId, PlayerAction action, out string error)
        {
            error = null;

            TablePlayer player = GetPlayer(playerId);
            if (player == null)
            {
                error = "You are not seated at this table.";
                return false;
            }

            if (!HandActive)
            {
                error = "There is no hand in progress.";
                return false;
            }

            BettingSeat seat = FindHandSeat(player);
            if (seat == null)
            {
                error = "You are not in this hand.";
                return false;
            }

            int stackBefore = seat.Stack;
            if (!hand.TryAct(player.Seat, action, out error))
            {
                return false;
            }

            player.LastAction = Describe(action, stackBefore - seat.Stack, seat.AllIn);
            AfterHandChange();
            return true;
        }

        // Call every frame with the time since the last call.
        public void Tick(float deltaSeconds)
        {
            if (hand == null)
            {
                return;
            }

            if (HandActive)
            {
                if (hand.CurrentSeat < 0)
                {
                    return;
                }

                TablePlayer current = players.Find(p => p.Seat == hand.CurrentSeat);

                if (current.Left)
                {
                    ActForCurrentPlayer(); // gone for good, no reason to wait
                    return;
                }

                if (current.IsBot)
                {
                    botDelay -= deltaSeconds;
                    if (botDelay <= 0)
                    {
                        ActForBot(current);
                    }

                    return;
                }

                turnTimeLeft -= deltaSeconds;
                if (turnTimeLeft <= 0)
                {
                    ActForCurrentPlayer();
                }
            }
            else if (autoStart)
            {
                nextHandTimer -= deltaSeconds;
                if (nextHandTimer <= 0)
                {
                    StartHand();
                }
            }
        }

        // The turn timer ran out (or the player left): check if that is free, otherwise fold.
        private void ActForCurrentPlayer()
        {
            TablePlayer player = players.Find(p => p.Seat == hand.CurrentSeat);
            bool canCheck = hand.GetOptions(player.Seat).CanCheck;
            var action = new PlayerAction(canCheck ? ActionType.Check : ActionType.Fold);

            string error;
            TryAct(player.Id, action, out error);
            player.LastAction = (canCheck ? "Check" : "Fold") + (player.Left ? " (left)" : " (time out)");
        }

        private void ActForBot(TablePlayer bot)
        {
            Card[] holeCards = hand.GetHoleCards(bot.Seat);
            ActionOptions options = hand.GetOptions(bot.Seat);
            int pot = hand.Seats.Sum(s => s.Committed);

            PlayerAction action = BotBrain.Decide(bot.Level, holeCards, hand.Board, options, pot, random);

            string error;
            if (!TryAct(bot.Id, action, out error))
            {
                // Should not happen, but a bot must never get stuck.
                TryAct(bot.Id, new PlayerAction(options.CanCheck ? ActionType.Check : ActionType.Fold), out error);
            }
        }

        // ---- what clients are told ----

        public string[] GetHoleCards(int playerId)
        {
            TablePlayer player = GetPlayer(playerId);
            if (player == null || hand == null || FindHandSeat(player) == null)
            {
                return new string[0];
            }

            return hand.GetHoleCards(player.Seat).Select(c => c.Code).ToArray();
        }

        // Real players in the hand. Bots keep their cards to themselves.
        public List<int> HumanIdsInHand()
        {
            if (hand == null)
            {
                return new List<int>();
            }

            return players.Where(p => !p.IsBot && FindHandSeat(p) != null).Select(p => p.Id).ToList();
        }

        public TableSnapshot BuildSnapshot()
        {
            var snapshot = new TableSnapshot
            {
                handNumber = HandNumber,
                handActive = HandActive,
                smallBlind = SmallBlind,
                bigBlind = BigBlind,
                turnSecondsLeft = Math.Max(0f, turnTimeLeft),
                message = message
            };

            if (hand != null)
            {
                snapshot.stage = (int)hand.Stage;
                snapshot.buttonSeat = hand.ButtonSeat;
                snapshot.pot = hand.Seats.Sum(s => s.Committed);
                snapshot.board = hand.Board.Select(c => c.Code).ToArray();
            }

            if (HandActive && hand.CurrentSeat >= 0)
            {
                snapshot.currentSeat = hand.CurrentSeat;
                snapshot.options = hand.GetOptions(hand.CurrentSeat);
            }

            snapshot.seats = players.OrderBy(p => p.Seat).Select(BuildSeatSnapshot).ToArray();
            return snapshot;
        }

        private SeatSnapshot BuildSeatSnapshot(TablePlayer player)
        {
            var seat = new SeatSnapshot
            {
                playerId = player.Id,
                seat = player.Seat,
                name = player.Name,
                stack = player.Stack,
                connected = player.Connected,
                isBot = player.IsBot,
                lastAction = player.LastAction
            };

            BettingSeat inHand = hand == null ? null : FindHandSeat(player);
            if (inHand != null)
            {
                seat.inHand = true;
                seat.bet = inHand.Bet;
                seat.folded = inHand.Folded;
                seat.allIn = inHand.AllIn;

                // Once the hand is over the player's own stack is the up to date one.
                seat.stack = HandActive ? inHand.Stack : player.Stack;
            }

            // Watching: no chips and not playing the hand that is running right now.
            seat.spectating = !player.IsBot && seat.stack == 0 && !InCurrentHand(player);

            // Cards are only shown for players who reached the showdown.
            if (hand != null && hand.IsFinished)
            {
                seat.won = player.Won;
                if (hand.ShowdownHands.TryGetValue(player.Seat, out HandValue value))
                {
                    seat.cards = hand.GetHoleCards(player.Seat).Select(c => c.Code).ToArray();
                    seat.handName = value.Name;
                    seat.handRank = (int)value.Rank;
                }
            }

            return seat;
        }

        // ---- internals ----

        private void AfterHandChange()
        {
            turnTimeLeft = TurnSeconds;
            botDelay = 0.8f + (float)random.NextDouble() * 1.2f;
            Version++;

            if (hand.IsFinished)
            {
                FinishHand();
            }
        }

        private void FinishHand()
        {
            foreach (BettingSeat s in hand.Seats)
            {
                TablePlayer player = players.Find(p => p.Seat == s.Seat);
                player.Stack = s.Stack;
                player.Won = hand.Winnings.TryGetValue(s.Seat, out int won) ? won : 0;
            }

            message = BuildResultText();
            nextHandTimer = NextHandDelay;

            // Players who left for good are removed now. Bots that lost everything start over.
            players.RemoveAll(p => p.Left);
            foreach (TablePlayer bot in players.Where(p => p.IsBot && p.Stack == 0))
            {
                bot.Stack = StartingChips;
            }
        }

        private string BuildResultText()
        {
            var parts = new List<string>();
            foreach (KeyValuePair<int, int> win in hand.Winnings)
            {
                string name = players.Find(p => p.Seat == win.Key).Name;
                string text = name + " wins " + win.Value;
                if (hand.ShowdownHands.TryGetValue(win.Key, out HandValue value))
                {
                    text += " with " + value.Name;
                }

                parts.Add(text);
            }

            return string.Join("; ", parts);
        }

        private static string Describe(PlayerAction action, int paid, bool allIn)
        {
            switch (action.Type)
            {
                case ActionType.Fold:
                    return "Fold";
                case ActionType.Check:
                    return "Check";
                case ActionType.Call:
                    return allIn ? "All-in " + paid : "Call " + paid;
                case ActionType.Raise:
                    return allIn ? "All-in " + paid : "Raise to " + action.Amount;
                default:
                    return "All-in " + paid;
            }
        }

        private int FundedCount()
        {
            return players.Count(p => p.Stack > 0 && p.Connected);
        }

        private int FreeSeat()
        {
            for (int seat = 0; seat < MaxSeats; seat++)
            {
                if (players.All(p => p.Seat != seat))
                {
                    return seat;
                }
            }

            return -1;
        }

        private bool InCurrentHand(TablePlayer player)
        {
            return HandActive && FindHandSeat(player) != null;
        }

        private BettingSeat FindHandSeat(TablePlayer player)
        {
            return hand.Seats.FirstOrDefault(s => s.Seat == player.Seat);
        }
    }
}
