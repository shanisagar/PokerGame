using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PokerDemo.Core
{
    public class Deck
    {
        private static readonly Random fallbackRandom = new Random(Guid.NewGuid().GetHashCode());

        private readonly List<Card> cards = new List<Card>(52);
        private int nextIndex;

        public int Remaining => cards.Count - nextIndex;

        public Deck()
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(rank, suit));
                }
            }

            Shuffle();
        }

        // Cards come out in the order given, no shuffle. Used by the tests.
        public Deck(IEnumerable<Card> orderedCards)
        {
            cards.AddRange(orderedCards);
        }

        // Fisher-Yates shuffle using a crypto RNG so the order can't be guessed from earlier hands.
        private void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = RandomIndex(i + 1);
                Card temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }

            nextIndex = 0;
        }

        // The crypto generator is not available on every platform (WebGL is the doubtful one),
        // so fall back to the normal one instead of breaking the game.
        private static int RandomIndex(int maxExclusive)
        {
            try
            {
                return RandomNumberGenerator.GetInt32(maxExclusive);
            }
            catch (Exception)
            {
                return fallbackRandom.Next(maxExclusive);
            }
        }

        public Card Draw()
        {
            if (nextIndex >= cards.Count)
            {
                throw new InvalidOperationException("Deck is empty.");
            }

            return cards[nextIndex++];
        }
    }
}
