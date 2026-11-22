using NUnit.Framework;
using PokerDemo.Core;

namespace PokerDemo.Tests
{
    public class RetentionTests
    {
        [Test]
        public void Stats_CountHandsAndWins()
        {
            var stats = new PlayerStats();

            stats.RecordHand(true, 120, -1);
            stats.RecordHand(false, 0, -1);
            stats.RecordHand(true, 80, -1);

            Assert.AreEqual(3, stats.handsPlayed);
            Assert.AreEqual(2, stats.handsWon);
            Assert.AreEqual(120, stats.biggestPot);
        }

        [Test]
        public void Stats_KeepTheBestShownHand()
        {
            var stats = new PlayerStats();
            Assert.AreEqual("-", stats.BestHandName);

            stats.RecordHand(true, 50, (int)HandRank.Flush);
            stats.RecordHand(true, 50, (int)HandRank.OnePair);
            stats.RecordHand(false, 0, -1);

            Assert.AreEqual("Flush", stats.BestHandName);
        }

        [Test]
        public void Stats_BestHand_UsesReadableNames()
        {
            var stats = new PlayerStats();
            stats.RecordHand(true, 50, (int)HandRank.FullHouse);

            Assert.AreEqual("Full House", stats.BestHandName);
        }

        [Test]
        public void SessionTimer_DoesNotFire_BeforeTheInterval()
        {
            var timer = new SessionTimer(3600f);

            Assert.IsFalse(timer.Advance(3599f));
        }

        [Test]
        public void SessionTimer_FiresOncePerInterval()
        {
            var timer = new SessionTimer(3600f);

            Assert.IsTrue(timer.Advance(3600f));
            Assert.IsFalse(timer.Advance(10f));
            Assert.IsFalse(timer.Advance(3000f));
            Assert.IsTrue(timer.Advance(600f));
        }
    }
}
