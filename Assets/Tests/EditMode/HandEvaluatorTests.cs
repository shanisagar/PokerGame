using NUnit.Framework;
using PokerDemo.Core;
using System.Collections.Generic;

namespace PokerDemo.Tests
{
    public class HandEvaluatorTests
    {
        // Codes like "As" (ace of spades) or "Td" (ten of diamonds).
        private static Card Parse(string code)
        {
            Rank rank = (Rank)("23456789TJQKA".IndexOf(code[0]) + 2);
            Suit suit = (Suit)"cdhs".IndexOf(code[1]);
            return new Card(rank, suit);
        }

        private static HandValue Eval(params string[] codes)
        {
            var cards = new List<Card>();
            foreach (string code in codes)
            {
                cards.Add(Parse(code));
            }

            return HandEvaluator.Evaluate(cards);
        }

        [Test]
        public void RoyalFlush_IsDetected()
        {
            Assert.AreEqual(HandRank.RoyalFlush, Eval("As", "Ks", "Qs", "Js", "Ts").Rank);
        }

        [Test]
        public void StraightFlush_BeatsFourOfAKind()
        {
            HandValue straightFlush = Eval("9h", "8h", "7h", "6h", "5h");
            HandValue quads = Eval("Ac", "Ad", "Ah", "As", "2c");

            Assert.Greater(straightFlush.CompareTo(quads), 0);
        }

        [Test]
        public void FourOfAKind_BeatsFullHouse()
        {
            HandValue quads = Eval("Ac", "Ad", "Ah", "As", "2c");
            HandValue fullHouse = Eval("Kc", "Kd", "Kh", "2s", "2c");

            Assert.Greater(quads.CompareTo(fullHouse), 0);
        }

        [Test]
        public void WheelStraight_IsLowerThanSixHighStraight()
        {
            HandValue wheel = Eval("Ac", "2d", "3h", "4s", "5c");
            HandValue sixHigh = Eval("2c", "3d", "4h", "5s", "6c");

            Assert.AreEqual(HandRank.Straight, wheel.Rank);
            Assert.Less(wheel.CompareTo(sixHigh), 0);
        }

        [Test]
        public void Flush_IsCompared_ByKickersInOrder()
        {
            HandValue higher = Eval("Ah", "Jh", "8h", "5h", "3h");
            HandValue lower = Eval("Ad", "Th", "8d", "5d", "3d");

            Assert.Greater(higher.CompareTo(lower), 0);
        }

        [Test]
        public void TwoPair_UsesKickerWhenPairsMatch()
        {
            HandValue withNine = Eval("Kc", "Kd", "5h", "5s", "9c");
            HandValue withEight = Eval("Kh", "Ks", "5c", "5d", "8c");

            Assert.Greater(withNine.CompareTo(withEight), 0);
        }

        [Test]
        public void BestFiveOfSeven_IsChosen()
        {
            HandValue hand = Eval("Ah", "Kh", "Qh", "Jh", "2c", "3d", "Th");

            Assert.AreEqual(HandRank.RoyalFlush, hand.Rank);
        }

        [Test]
        public void SharedBoard_IsATie()
        {
            HandValue playerA = Eval("2c", "3d", "Ah", "Kh", "Qh", "Jh", "Th");
            HandValue playerB = Eval("4c", "5d", "Ah", "Kh", "Qh", "Jh", "Th");

            Assert.AreEqual(0, playerA.CompareTo(playerB));
        }
    }
}
