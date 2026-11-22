using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    public sealed class BettingRound
    {
        private readonly List<BettingSeat> seats;
        private readonly int bigBlind;
        private int lastRaiseSize;
        private int actorIndex = -1;

        public int CurrentBet { get; private set; }

        public int MinRaiseTo => CurrentBet == 0 ? bigBlind : CurrentBet + lastRaiseSize;

        public int CurrentSeat => actorIndex < 0 ? -1 : seats[actorIndex].Seat;

        public bool IsComplete => actorIndex < 0;

        // players must be in table order. firstToAct is the seat number that acts first.
        public BettingRound(IEnumerable<BettingSeat> players, int bigBlind, int firstToAct)
        {
            seats = new List<BettingSeat>(players);
            this.bigBlind = bigBlind;
            lastRaiseSize = bigBlind;

            foreach (BettingSeat s in seats)
            {
                if (s.Bet > CurrentBet)
                {
                    CurrentBet = s.Bet;
                }
            }

            foreach (BettingSeat s in seats)
            {
                s.NeedsToAct = !s.Folded && !s.AllIn;
            }

            int start = seats.FindIndex(s => s.Seat == firstToAct);
            actorIndex = FindActor(start < 0 ? 0 : start);
        }

        public ActionOptions GetOptions(int seatNumber)
        {
            BettingSeat s = Find(seatNumber);
            if (s == null || seatNumber != CurrentSeat)
            {
                return default;
            }

            int maxTo = s.Bet + s.Stack;

            return new ActionOptions
            {
                CanFold = true,
                CanCheck = s.Bet == CurrentBet,
                CanCall = CurrentBet > s.Bet,
                CallAmount = Math.Max(0, Math.Min(CurrentBet - s.Bet, s.Stack)),
                CanRaise = s.NeedsToAct && maxTo > CurrentBet,
                MinRaiseTo = Math.Min(MinRaiseTo, maxTo),
                MaxRaiseTo = maxTo,
                CanAllIn = s.Stack > 0
            };
        }

        public bool TryApply(int seatNumber, PlayerAction action, out string error)
        {
            error = null;

            BettingSeat s = Find(seatNumber);
            if (s == null)
            {
                error = "Seat is not in this hand.";
                return false;
            }

            if (seatNumber != CurrentSeat)
            {
                error = "It is not your turn.";
                return false;
            }

            int toCall = CurrentBet - s.Bet;

            if (action.Type == ActionType.Fold)
            {
                s.Folded = true;
                s.NeedsToAct = false;
            }
            else if (action.Type == ActionType.Check)
            {
                if (toCall > 0)
                {
                    error = "You can't check, there is a bet to call.";
                    return false;
                }

                s.NeedsToAct = false;
            }
            else if (action.Type == ActionType.Call)
            {
                if (toCall <= 0)
                {
                    error = "Nothing to call. Check instead.";
                    return false;
                }

                // A call with fewer chips than needed is an all-in.
                Commit(s, Math.Min(CurrentBet, s.Bet + s.Stack));
            }
            else if (action.Type == ActionType.Raise)
            {
                int maxTo = s.Bet + s.Stack;

                if (!s.NeedsToAct)
                {
                    error = "Betting is closed to you, you can only call or fold.";
                    return false;
                }

                if (action.Amount <= CurrentBet)
                {
                    error = "A raise must be above the current bet.";
                    return false;
                }

                if (action.Amount > maxTo)
                {
                    error = "You don't have enough chips.";
                    return false;
                }

                // Going all-in for less than a full raise is allowed.
                if (action.Amount < MinRaiseTo && action.Amount != maxTo)
                {
                    error = "A raise must be at least " + MinRaiseTo + ".";
                    return false;
                }

                Commit(s, action.Amount);
            }
            else if (action.Type == ActionType.AllIn)
            {
                int allInTo = s.Bet + s.Stack;

                if (allInTo > CurrentBet && !s.NeedsToAct)
                {
                    error = "Betting is closed to you, you can only call or fold.";
                    return false;
                }

                Commit(s, allInTo);
            }
            else
            {
                error = "Unknown action.";
                return false;
            }

            actorIndex = FindActor((actorIndex + 1) % seats.Count);
            return true;
        }

        // Moves chips in and updates the bet. Only a full raise reopens betting for everyone else.
        private void Commit(BettingSeat s, int newLevel)
        {
            int raiseSize = newLevel - CurrentBet;
            s.Pay(newLevel - s.Bet);
            s.NeedsToAct = false;

            if (raiseSize <= 0)
            {
                return;
            }

            CurrentBet = newLevel;

            if (raiseSize >= lastRaiseSize)
            {
                lastRaiseSize = raiseSize;
                foreach (BettingSeat other in seats)
                {
                    if (other != s && !other.Folded && !other.AllIn)
                    {
                        other.NeedsToAct = true;
                    }
                }
            }
        }

        private int FindActor(int start)
        {
            if (seats.Count(s => !s.Folded) <= 1)
            {
                return -1;
            }

            for (int k = 0; k < seats.Count; k++)
            {
                int i = (start + k) % seats.Count;
                if (CanAct(seats[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool CanAct(BettingSeat s)
        {
            return !s.Folded && !s.AllIn && (s.NeedsToAct || s.Bet < CurrentBet);
        }

        private BettingSeat Find(int seatNumber)
        {
            return seats.Find(s => s.Seat == seatNumber);
        }
    }
}
