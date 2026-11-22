using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    public enum HandStage
    {
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown
    }

    // One hand of Texas Hold'em, from the blinds to paying out the pots.
    public sealed class Hand
    {
        private readonly List<BettingSeat> seats;
        private readonly Dictionary<int, Card[]> holeCards = new Dictionary<int, Card[]>();
        private readonly List<Card> board = new List<Card>();
        private readonly Deck deck;
        private readonly int tableSize;
        private readonly int bigBlind;
        private readonly int firstPostflopSeat;
        private BettingRound betting;

        public int ButtonSeat { get; }
        public HandStage Stage { get; private set; } = HandStage.PreFlop;
        public bool IsFinished { get; private set; }
        public IReadOnlyList<Card> Board => board;
        public IReadOnlyList<BettingSeat> Seats => seats;
        public int CurrentSeat => betting == null ? -1 : betting.CurrentSeat;

        // Filled in when the hand ends.
        public Dictionary<int, HandValue> ShowdownHands { get; } = new Dictionary<int, HandValue>();
        public Dictionary<int, int> Winnings { get; private set; } = new Dictionary<int, int>();

        // players are the seats that have chips. The button moves to the next player after previousButtonSeat.
        public Hand(IEnumerable<BettingSeat> players, int previousButtonSeat, int tableSize,
            int smallBlind, int bigBlind, Deck deck)
        {
            seats = players.OrderBy(s => s.Seat).ToList();
            if (seats.Count < 2)
            {
                throw new ArgumentException("A hand needs at least two players.");
            }

            this.tableSize = tableSize;
            this.bigBlind = bigBlind;
            this.deck = deck;

            ButtonSeat = NextSeat(previousButtonSeat);

            int smallBlindSeat;
            int bigBlindSeat;
            int firstPreflopSeat;

            if (seats.Count == 2)
            {
                // Heads up, the button posts the small blind and acts first before the flop.
                smallBlindSeat = ButtonSeat;
                bigBlindSeat = NextSeat(smallBlindSeat);
                firstPreflopSeat = smallBlindSeat;
                firstPostflopSeat = bigBlindSeat;
            }
            else
            {
                smallBlindSeat = NextSeat(ButtonSeat);
                bigBlindSeat = NextSeat(smallBlindSeat);
                firstPreflopSeat = NextSeat(bigBlindSeat);
                firstPostflopSeat = NextSeat(ButtonSeat);
            }

            BettingSeat smallBlindPlayer = Find(smallBlindSeat);
            BettingSeat bigBlindPlayer = Find(bigBlindSeat);
            smallBlindPlayer.Pay(Math.Min(smallBlind, smallBlindPlayer.Stack));
            bigBlindPlayer.Pay(Math.Min(bigBlind, bigBlindPlayer.Stack));

            foreach (BettingSeat s in seats)
            {
                holeCards[s.Seat] = new Card[2];
            }

            for (int round = 0; round < 2; round++)
            {
                foreach (BettingSeat s in seats)
                {
                    holeCards[s.Seat][round] = deck.Draw();
                }
            }

            betting = new BettingRound(seats, bigBlind, firstPreflopSeat);
            Settle();
        }

        public Card[] GetHoleCards(int seat)
        {
            return (Card[])holeCards[seat].Clone();
        }

        public ActionOptions GetOptions(int seat)
        {
            return betting == null ? default : betting.GetOptions(seat);
        }

        public bool TryAct(int seat, PlayerAction action, out string error)
        {
            error = null;

            if (IsFinished || betting == null)
            {
                error = "No betting is open right now.";
                return false;
            }

            if (!betting.TryApply(seat, action, out error))
            {
                return false;
            }

            Settle();
            return true;
        }

        // Moves the hand forward whenever a betting round is over.
        private void Settle()
        {
            while (betting == null || betting.IsComplete)
            {
                betting = null;

                if (seats.Count(s => !s.Folded) == 1)
                {
                    Payout();
                    return;
                }

                // If nobody can bet any more, the rest of the board is dealt without betting.
                int canBet = seats.Count(s => !s.Folded && !s.AllIn);
                if (Stage == HandStage.River || canBet < 2)
                {
                    while (board.Count < 5)
                    {
                        DealNextStreet();
                    }

                    Stage = HandStage.Showdown;
                    FindShowdownHands();
                    Payout();
                    return;
                }

                DealNextStreet();
                betting = new BettingRound(seats, bigBlind, firstPostflopSeat);
            }
        }

        private void DealNextStreet()
        {
            foreach (BettingSeat s in seats)
            {
                s.ResetBet();
            }

            deck.Draw(); // burn card

            int count = board.Count == 0 ? 3 : 1;
            for (int i = 0; i < count; i++)
            {
                board.Add(deck.Draw());
            }

            if (board.Count == 3)
            {
                Stage = HandStage.Flop;
            }
            else if (board.Count == 4)
            {
                Stage = HandStage.Turn;
            }
            else
            {
                Stage = HandStage.River;
            }
        }

        private void FindShowdownHands()
        {
            foreach (BettingSeat s in seats.Where(p => !p.Folded))
            {
                var cards = new List<Card>(holeCards[s.Seat]);
                cards.AddRange(board);
                ShowdownHands[s.Seat] = HandEvaluator.Evaluate(cards);
            }
        }

        // Also used when everyone else folded, then there are no hands to compare.
        private void Payout()
        {
            var contributions = seats
                .Select(s => new Contribution(s.Seat, s.Committed, s.Folded))
                .ToList();

            List<Pot> pots = PotCalculator.Build(contributions);
            Winnings = PotCalculator.Distribute(pots, ShowdownHands, ButtonSeat, tableSize);

            foreach (KeyValuePair<int, int> win in Winnings)
            {
                Find(win.Key).Stack += win.Value;
            }

            IsFinished = true;
        }

        // First seat after the given seat number, wrapping around.
        private int NextSeat(int afterSeat)
        {
            foreach (BettingSeat s in seats)
            {
                if (s.Seat > afterSeat)
                {
                    return s.Seat;
                }
            }

            return seats[0].Seat;
        }

        private BettingSeat Find(int seat)
        {
            return seats.Find(s => s.Seat == seat);
        }
    }
}
