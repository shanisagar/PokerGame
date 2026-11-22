using NUnit.Framework;
using PokerDemo.Core;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Tests
{
    public class HandTests
    {
        private static Card Parse(string code)
        {
            Rank rank = (Rank)("23456789TJQKA".IndexOf(code[0]) + 2);
            Suit suit = (Suit)"cdhs".IndexOf(code[1]);
            return new Card(rank, suit);
        }

        // Cards are dealt in this order: one hole card to each seat, then the second hole card
        // to each seat, then burn + flop (3), burn + turn, burn + river.
        private static Deck DeckFrom(params string[] codes)
        {
            return new Deck(codes.Select(Parse).ToList());
        }

        private static List<BettingSeat> Seats(params int[] stacks)
        {
            var list = new List<BettingSeat>();
            for (int i = 0; i < stacks.Length; i++)
            {
                list.Add(new BettingSeat(i, stacks[i]));
            }

            return list;
        }

        private static void Act(Hand hand, int seat, ActionType type)
        {
            string error;
            Assert.IsTrue(hand.TryAct(seat, new PlayerAction(type), out error), error);
        }

        [Test]
        public void Blinds_AreAllPosted_AndPlayerAfterBigBlindActsFirst()
        {
            var seats = Seats(1000, 1000, 1000);
            var hand = new Hand(seats, -1, 6, 5, 10, DeckFrom("As", "Qh", "7c", "Ad", "Qd", "7d"));

            Assert.AreEqual(0, hand.ButtonSeat);
            Assert.AreEqual(5, seats[1].Bet);
            Assert.AreEqual(10, seats[2].Bet);
            Assert.AreEqual(0, hand.CurrentSeat);
            Assert.AreEqual(HandStage.PreFlop, hand.Stage);
        }

        [Test]
        public void Button_MovesToTheNextPlayerEachHand()
        {
            var seats = Seats(1000, 1000, 1000);
            var hand = new Hand(seats, 0, 6, 5, 10, DeckFrom("As", "Qh", "7c", "Ad", "Qd", "7d"));

            Assert.AreEqual(1, hand.ButtonSeat);
        }

        [Test]
        public void HeadsUp_ButtonIsSmallBlind_AndFoldGivesPotToOther()
        {
            var seats = Seats(1000, 1000);
            var hand = new Hand(seats, -1, 6, 5, 10, DeckFrom("Ac", "Kc", "Ad", "Kd"));

            Assert.AreEqual(0, hand.CurrentSeat);
            Act(hand, 0, ActionType.Fold);

            Assert.IsTrue(hand.IsFinished);
            Assert.AreEqual(15, hand.Winnings[1]);
            Assert.AreEqual(995, seats[0].Stack);
            Assert.AreEqual(1005, seats[1].Stack);
        }

        [Test]
        public void FullHand_GoesThroughEveryStage_AndBestHandWins()
        {
            var seats = Seats(1000, 1000);
            var hand = new Hand(seats, -1, 6, 5, 10, DeckFrom(
                "Ac", "Kc", "Ad", "Kd",
                "Th", "2c", "7d", "9h",
                "Ts", "Jc",
                "Td", "8h"));

            Act(hand, 0, ActionType.Call);
            Act(hand, 1, ActionType.Check);
            Assert.AreEqual(HandStage.Flop, hand.Stage);
            Assert.AreEqual(3, hand.Board.Count);

            Act(hand, 1, ActionType.Check);
            Act(hand, 0, ActionType.Check);
            Assert.AreEqual(HandStage.Turn, hand.Stage);

            Act(hand, 1, ActionType.Check);
            Act(hand, 0, ActionType.Check);
            Assert.AreEqual(HandStage.River, hand.Stage);

            Act(hand, 1, ActionType.Check);
            Act(hand, 0, ActionType.Check);

            Assert.IsTrue(hand.IsFinished);
            Assert.AreEqual(HandStage.Showdown, hand.Stage);
            Assert.AreEqual(20, hand.Winnings[0]);
            Assert.AreEqual(1010, seats[0].Stack);
            Assert.AreEqual(990, seats[1].Stack);
        }

        [Test]
        public void AllIn_WithDifferentStacks_PaysMainAndSidePot()
        {
            var seats = Seats(50, 200, 200);
            var hand = new Hand(seats, -1, 6, 5, 10, DeckFrom(
                "As", "Qh", "7c", "Ad", "Qd", "7d",
                "6h", "2c", "5d", "9h",
                "3s", "Js",
                "4s", "Kc"));

            Act(hand, 0, ActionType.AllIn);
            Act(hand, 1, ActionType.AllIn);
            Act(hand, 2, ActionType.Call);

            // Nobody can bet any more, so the board is dealt and the hand is paid out.
            Assert.IsTrue(hand.IsFinished);
            Assert.AreEqual(5, hand.Board.Count);

            // Seat 0 has aces and only covers the main pot. Seat 1 has queens and takes the side pot.
            Assert.AreEqual(150, hand.Winnings[0]);
            Assert.AreEqual(300, hand.Winnings[1]);
            Assert.IsFalse(hand.Winnings.ContainsKey(2));
            Assert.AreEqual(150, seats[0].Stack);
            Assert.AreEqual(300, seats[1].Stack);
            Assert.AreEqual(0, seats[2].Stack);
        }
    }
}
