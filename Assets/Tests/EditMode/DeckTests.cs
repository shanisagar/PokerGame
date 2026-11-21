using NUnit.Framework;
using PokerDemo.Core;
using System;
using System.Collections.Generic;

namespace PokerDemo.Tests
{
    public class DeckTests
    {
        [Test]
        public void NewDeck_HasFiftyTwoUniqueCards()
        {
            var deck = new Deck();
            var seen = new HashSet<Card>();

            while (deck.Remaining > 0)
            {
                seen.Add(deck.Draw());
            }

            Assert.AreEqual(52, seen.Count);
        }

        [Test]
        public void Draw_ThrowsWhenDeckIsEmpty()
        {
            var deck = new Deck();
            for (int i = 0; i < 52; i++)
            {
                deck.Draw();
            }

            Assert.Throws<InvalidOperationException>(() => deck.Draw());
        }
    }
}
