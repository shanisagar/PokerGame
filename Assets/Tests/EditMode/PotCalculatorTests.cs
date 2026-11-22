using NUnit.Framework;
using PokerDemo.Core;
using System.Collections.Generic;

namespace PokerDemo.Tests
{
    public class PotCalculatorTests
    {
        [Test]
        public void SameBets_MakeOneMainPot()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 100, false),
                new Contribution(1, 100, false),
                new Contribution(2, 100, false)
            };

            List<Pot> pots = PotCalculator.Build(contributions);

            Assert.AreEqual(1, pots.Count);
            Assert.AreEqual(300, pots[0].Amount);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, pots[0].EligibleSeats);
        }

        [Test]
        public void AllIn_ShortStack_GetsSidePot()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 50, false),
                new Contribution(1, 150, false),
                new Contribution(2, 150, false)
            };

            List<Pot> pots = PotCalculator.Build(contributions);

            Assert.AreEqual(2, pots.Count);
            Assert.AreEqual(150, pots[0].Amount);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, pots[0].EligibleSeats);
            Assert.AreEqual(200, pots[1].Amount);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, pots[1].EligibleSeats);
        }

        [Test]
        public void FoldedChips_StayInPot_ButFoldedSeatIsNotEligible()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 30, true),
                new Contribution(1, 100, false),
                new Contribution(2, 100, false)
            };

            List<Pot> pots = PotCalculator.Build(contributions);

            Assert.AreEqual(230, pots[0].Amount + pots[1].Amount);
            CollectionAssert.DoesNotContain(pots[0].EligibleSeats, 0);
            CollectionAssert.DoesNotContain(pots[1].EligibleSeats, 0);
        }

        [Test]
        public void UncalledBet_IsReturnedToBettor()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 200, false),
                new Contribution(1, 50, true)
            };

            List<Pot> pots = PotCalculator.Build(contributions);
            Dictionary<int, int> winnings = PotCalculator.Distribute(
                pots, new Dictionary<int, HandValue>(), 0, 2);

            Assert.AreEqual(250, winnings[0]);
            Assert.IsFalse(winnings.ContainsKey(1));
        }

        [Test]
        public void SidePot_GoesToBestHandInThatPot()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 50, false),
                new Contribution(1, 150, false),
                new Contribution(2, 150, false)
            };
            var hands = new Dictionary<int, HandValue>
            {
                { 0, new HandValue(HandRank.Flush, 14) },
                { 1, new HandValue(HandRank.OnePair, 10) },
                { 2, new HandValue(HandRank.TwoPair, 10, 5) }
            };

            List<Pot> pots = PotCalculator.Build(contributions);
            Dictionary<int, int> winnings = PotCalculator.Distribute(pots, hands, 0, 3);

            Assert.AreEqual(150, winnings[0]);
            Assert.AreEqual(200, winnings[2]);
            Assert.IsFalse(winnings.ContainsKey(1));
        }

        [Test]
        public void OddChip_GoesToWinnerClosestToButton()
        {
            var contributions = new List<Contribution>
            {
                new Contribution(0, 1, true),
                new Contribution(1, 50, false),
                new Contribution(2, 50, false)
            };
            var hands = new Dictionary<int, HandValue>
            {
                { 1, new HandValue(HandRank.OnePair, 10) },
                { 2, new HandValue(HandRank.OnePair, 10) }
            };

            List<Pot> pots = PotCalculator.Build(contributions);
            Dictionary<int, int> winnings = PotCalculator.Distribute(pots, hands, 0, 3);

            Assert.AreEqual(51, winnings[1]);
            Assert.AreEqual(50, winnings[2]);
        }
    }
}
