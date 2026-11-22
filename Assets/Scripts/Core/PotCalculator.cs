using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    public static class PotCalculator
    {
        // Splits the chips from a hand into a main pot and side pots.
        // A new pot starts each time a seat's total commitment is reached.
        public static List<Pot> Build(IReadOnlyList<Contribution> contributions)
        {
            var pots = new List<Pot>();
            int previousLevel = 0;

            var levels = contributions
                .Where(c => c.Committed > 0)
                .Select(c => c.Committed)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            foreach (int level in levels)
            {
                int amount = 0;
                foreach (Contribution c in contributions)
                {
                    amount += Math.Max(0, Math.Min(c.Committed, level) - previousLevel);
                }

                var eligible = contributions
                    .Where(c => !c.Folded && c.Committed >= level)
                    .Select(c => c.Seat)
                    .ToList();

                if (eligible.Count == 0 && pots.Count > 0)
                {
                    // Only folded players reached this level, so the chips stay in the pot below.
                    pots[pots.Count - 1].Amount += amount;
                }
                else
                {
                    var pot = new Pot { Amount = amount };
                    pot.EligibleSeats.AddRange(eligible);
                    pots.Add(pot);
                }

                previousLevel = level;
            }

            return pots;
        }

        // Returns the chips each seat wins. A pot with one eligible seat is an uncalled bet
        // and goes back to that seat, so hands are only needed when two or more seats are left.
        public static Dictionary<int, int> Distribute(
            IReadOnlyList<Pot> pots,
            IReadOnlyDictionary<int, HandValue> hands,
            int buttonSeat,
            int seatCount)
        {
            var winnings = new Dictionary<int, int>();

            foreach (Pot pot in pots)
            {
                if (pot.EligibleSeats.Count == 0)
                {
                    continue;
                }

                List<int> winners = FindWinners(pot.EligibleSeats, hands);

                // The odd chip goes to the winner closest to the button, going clockwise.
                winners.Sort((a, b) =>
                    SeatDistance(a, buttonSeat, seatCount).CompareTo(SeatDistance(b, buttonSeat, seatCount)));

                int share = pot.Amount / winners.Count;
                int remainder = pot.Amount % winners.Count;

                for (int i = 0; i < winners.Count; i++)
                {
                    int chips = share + (i == 0 ? remainder : 0);
                    if (!winnings.ContainsKey(winners[i]))
                    {
                        winnings[winners[i]] = 0;
                    }

                    winnings[winners[i]] += chips;
                }
            }

            return winnings;
        }

        private static List<int> FindWinners(List<int> eligible, IReadOnlyDictionary<int, HandValue> hands)
        {
            var winners = new List<int>();
            if (eligible.Count == 1)
            {
                winners.Add(eligible[0]);
                return winners;
            }

            HandValue best = default;
            foreach (int seat in eligible)
            {
                HandValue value = hands[seat];
                if (winners.Count == 0 || value.CompareTo(best) > 0)
                {
                    best = value;
                    winners.Clear();
                    winners.Add(seat);
                }
                else if (value.CompareTo(best) == 0)
                {
                    winners.Add(seat);
                }
            }

            return winners;
        }

        // Seat right after the button is 0, the button itself is the largest.
        private static int SeatDistance(int seat, int buttonSeat, int seatCount)
        {
            return (seat - buttonSeat - 1 + seatCount) % seatCount;
        }
    }
}
