using NUnit.Framework;
using PokerDemo.Core;
using System;
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

        // Plays many hands with random but legal actions. Whatever happens, the chips on the table
        // must stay the same, and everything the options allow must really be accepted.
        [Test]
        public void RandomPlay_NeverCreatesOrLosesChips()
        {
            var random = new Random(7);

            for (int game = 0; game < 400; game++)
            {
                int playerCount = random.Next(2, 7);
                var seats = new List<BettingSeat>();
                for (int i = 0; i < playerCount; i++)
                {
                    seats.Add(new BettingSeat(i, random.Next(1, 60) * 5));
                }

                int chipsBefore = seats.Sum(s => s.Stack);
                var hand = new Hand(seats, random.Next(-1, playerCount), 6, 10, 20, new Deck());

                for (int turn = 0; turn < 500 && !hand.IsFinished; turn++)
                {
                    int seat = hand.CurrentSeat;
                    Assert.GreaterOrEqual(seat, 0, "an unfinished hand must have someone to act");

                    string error;
                    PlayerAction action = RandomLegalAction(hand.GetOptions(seat), random);
                    Assert.IsTrue(hand.TryAct(seat, action, out error), action.Type + " " + action.Amount + ": " + error);
                }

                Assert.IsTrue(hand.IsFinished, "hand " + game + " never finished");
                Assert.AreEqual(chipsBefore, seats.Sum(s => s.Stack), "chips changed in hand " + game);
            }
        }

        internal static PlayerAction RandomLegalAction(ActionOptions options, Random random)
        {
            var choices = new List<PlayerAction>();

            if (options.CanFold)
            {
                choices.Add(new PlayerAction(ActionType.Fold));
            }

            if (options.CanCheck)
            {
                choices.Add(new PlayerAction(ActionType.Check));
                choices.Add(new PlayerAction(ActionType.Check));
            }

            if (options.CanCall)
            {
                choices.Add(new PlayerAction(ActionType.Call));
                choices.Add(new PlayerAction(ActionType.Call));
            }

            if (options.CanRaise)
            {
                int amount = random.Next(options.MinRaiseTo, options.MaxRaiseTo + 1);
                choices.Add(new PlayerAction(ActionType.Raise, amount));
            }

            if (options.CanAllIn)
            {
                choices.Add(new PlayerAction(ActionType.AllIn));
            }

            return choices[random.Next(choices.Count)];
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
