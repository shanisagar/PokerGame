using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    public static class HandEvaluator
    {
        // Takes 5 to 7 cards and returns the best 5-card hand.
        public static HandValue Evaluate(IReadOnlyList<Card> cards)
        {
            int n = cards.Count;
            if (n < 5 || n > 7)
            {
                throw new ArgumentException("Need between 5 and 7 cards.");
            }

            HandValue best = default;
            bool hasBest = false;

            for (int mask = 0; mask < (1 << n); mask++)
            {
                if (CountBits(mask) != 5)
                {
                    continue;
                }

                Card[] five = new Card[5];
                int k = 0;
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        five[k++] = cards[i];
                    }
                }

                HandValue value = EvaluateFive(five);
                if (!hasBest || value.CompareTo(best) > 0)
                {
                    best = value;
                    hasBest = true;
                }
            }

            return best;
        }

        private static HandValue EvaluateFive(Card[] five)
        {
            int[] ranks = five.Select(c => (int)c.Rank).OrderByDescending(r => r).ToArray();

            bool isFlush = five.All(c => c.Suit == five[0].Suit);
            int straightHigh = GetStraightHigh(ranks);

            if (straightHigh > 0 && isFlush)
            {
                return straightHigh == 14
                    ? new HandValue(HandRank.RoyalFlush, 14)
                    : new HandValue(HandRank.StraightFlush, straightHigh);
            }

            // Groups sorted by how many cards share the rank, then by rank.
            // This gives the kicker order for pairs, trips, full house and quads.
            var groups = ranks
                .GroupBy(r => r)
                .Select(g => new { Rank = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Rank)
                .ToList();

            int[] groupOrder = groups.Select(g => g.Rank).ToArray();

            if (groups[0].Count == 4)
            {
                return new HandValue(HandRank.FourOfAKind, groupOrder);
            }

            if (groups[0].Count == 3 && groups[1].Count == 2)
            {
                return new HandValue(HandRank.FullHouse, groupOrder);
            }

            if (isFlush)
            {
                return new HandValue(HandRank.Flush, ranks);
            }

            if (straightHigh > 0)
            {
                return new HandValue(HandRank.Straight, straightHigh);
            }

            if (groups[0].Count == 3)
            {
                return new HandValue(HandRank.ThreeOfAKind, groupOrder);
            }

            if (groups[0].Count == 2)
            {
                return groups[1].Count == 2
                    ? new HandValue(HandRank.TwoPair, groupOrder)
                    : new HandValue(HandRank.OnePair, groupOrder);
            }

            return new HandValue(HandRank.HighCard, ranks);
        }

        // ranks must be sorted high to low. Returns the high card of the straight, or 0 if none.
        private static int GetStraightHigh(int[] ranks)
        {
            for (int i = 0; i < 4; i++)
            {
                if (ranks[i] == ranks[i + 1])
                {
                    return 0;
                }
            }

            if (ranks[0] - ranks[4] == 4)
            {
                return ranks[0];
            }

            // Ace low straight: A-2-3-4-5, which counts as five high.
            if (ranks[0] == 14 && ranks[1] == 5)
            {
                return 5;
            }

            return 0;
        }

        private static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
