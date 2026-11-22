using NUnit.Framework;
using PokerDemo.Core;
using System.Collections.Generic;

namespace PokerDemo.Tests
{
    public class BettingRoundTests
    {
        private static List<BettingSeat> Seats(params int[] stacks)
        {
            var list = new List<BettingSeat>();
            for (int i = 0; i < stacks.Length; i++)
            {
                list.Add(new BettingSeat(i, stacks[i]));
            }

            return list;
        }

        [Test]
        public void Check_IsRejected_WhenThereIsABetToCall()
        {
            var seats = Seats(1000, 1000, 1000);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Raise, 100), out error));
            Assert.AreEqual(1, round.CurrentSeat);
            Assert.IsFalse(round.TryApply(1, new PlayerAction(ActionType.Check), out error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void MinRaise_MustBeAtLeastThePreviousRaise()
        {
            var seats = Seats(1000, 1000, 1000);
            seats[1].Pay(5);
            seats[2].Pay(10);
            var round = new BettingRound(seats, 10, 0);
            string error;

            // Raise by 20 on top of the big blind, so the next raise must be at least 20 more.
            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Raise, 30), out error));
            Assert.AreEqual(50, round.MinRaiseTo);
            Assert.IsFalse(round.TryApply(1, new PlayerAction(ActionType.Raise, 40), out error));
            Assert.IsTrue(round.TryApply(1, new PlayerAction(ActionType.Raise, 50), out error));
        }

        [Test]
        public void ShortStack_CallingWithFewerChips_BecomesAllIn()
        {
            var seats = Seats(1000, 50, 1000);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Raise, 200), out error));
            Assert.IsTrue(round.TryApply(1, new PlayerAction(ActionType.Call), out error));

            Assert.IsTrue(seats[1].AllIn);
            Assert.AreEqual(0, seats[1].Stack);
            Assert.AreEqual(50, seats[1].Bet);
        }

        [Test]
        public void ShortAllIn_DoesNotReopenRaising_ForPlayersWhoAlreadyActed()
        {
            var seats = Seats(1000, 1000, 150);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Raise, 100), out error));
            Assert.IsTrue(round.TryApply(1, new PlayerAction(ActionType.Call), out error));
            Assert.IsTrue(round.TryApply(2, new PlayerAction(ActionType.AllIn), out error));

            Assert.AreEqual(0, round.CurrentSeat);
            Assert.IsFalse(round.GetOptions(0).CanRaise);
            Assert.IsFalse(round.TryApply(0, new PlayerAction(ActionType.Raise, 300), out error));

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Call), out error));
            Assert.IsTrue(round.TryApply(1, new PlayerAction(ActionType.Call), out error));
            Assert.IsTrue(round.IsComplete);
        }

        [Test]
        public void Fold_LeavesOnePlayer_AndEndsTheRound()
        {
            var seats = Seats(1000, 1000);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.Fold), out error));

            Assert.IsTrue(round.IsComplete);
        }

        [Test]
        public void AllIn_PutsWholeStackIn()
        {
            var seats = Seats(500, 1000, 1000);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsTrue(round.TryApply(0, new PlayerAction(ActionType.AllIn), out error));

            Assert.AreEqual(0, seats[0].Stack);
            Assert.AreEqual(500, seats[0].Bet);
            Assert.IsTrue(seats[0].AllIn);
        }

        [Test]
        public void Raise_AboveStack_IsRejected()
        {
            var seats = Seats(100, 1000);
            var round = new BettingRound(seats, 10, 0);
            string error;

            Assert.IsFalse(round.TryApply(0, new PlayerAction(ActionType.Raise, 200), out error));
        }
    }
}
