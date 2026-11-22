using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PokerDemo.Core
{
    public class Deck
    {
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
        public void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                Card temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }

            nextIndex = 0;
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
