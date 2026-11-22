using NUnit.Framework;
using PokerDemo.Core;
using System;
using System.Collections.Generic;

namespace PokerDemo.Tests
{
    public class BotBrainTests
    {
        private static Card[] Draw(Deck deck, int count)
        {
            var cards = new Card[count];
            for (int i = 0; i < count; i++)
            {
                cards[i] = deck.Draw();
            }

            return cards;
        }

        private static ActionOptions CheckOptions()
        {
            return new ActionOptions
            {
                CanFold = true, CanCheck = true, CanRaise = true, CanAllIn = true,
                MinRaiseTo = 20, MaxRaiseTo = 1000
            };
        }

        private static ActionOptions FacingBetOptions()
        {
            return new ActionOptions
            {
                CanFold = true, CanCall = true, CallAmount = 100, CanRaise = true, CanAllIn = true,
                MinRaiseTo = 200, MaxRaiseTo = 1000
            };
        }

        [Test]
        [TestCase(BotLevel.Cautious)]
        [TestCase(BotLevel.Bold)]
        public void Bot_NeverFolds_WhenCheckingIsFree(BotLevel level)
        {
            var random = new Random(1);
            for (int i = 0; i < 300; i++)
            {
                var deck = new Deck();
                Card[] hole = Draw(deck, 2);
                Card[] board = Draw(deck, i % 4 == 0 ? 0 : 3);

                PlayerAction action = BotBrain.Decide(level, hole, board, CheckOptions(), 100, random);

                Assert.AreNotEqual(ActionType.Fold, action.Type);
            }
        }

        [Test]
        [TestCase(BotLevel.Cautious)]
        [TestCase(BotLevel.Bold)]
        public void Bot_OnlyChoosesAllowedActions_AndRaisesStayInRange(BotLevel level)
        {
            var random = new Random(2);
            for (int i = 0; i < 300; i++)
            {
                var deck = new Deck();
                Card[] hole = Draw(deck, 2);
                Card[] board = Draw(deck, 5);
                ActionOptions options = FacingBetOptions();

                PlayerAction action = BotBrain.Decide(level, hole, board, options, 300, random);

                Assert.AreNotEqual(ActionType.Check, action.Type); // there is a bet to answer
                if (action.Type == ActionType.Raise)
                {
                    Assert.GreaterOrEqual(action.Amount, options.MinRaiseTo);
                    Assert.LessOrEqual(action.Amount, options.MaxRaiseTo);
                }
            }
        }

        [Test]
        public void CautiousBot_FoldsWeakHand_ToABigBet()
        {
            // Seven-two offsuit before the flop is the worst starting hand.
            var hole = new[] { new Card(Rank.Seven, Suit.Clubs), new Card(Rank.Two, Suit.Hearts) };
            var random = new Random(3);

            int folds = 0;
            for (int i = 0; i < 100; i++)
            {
                PlayerAction action = BotBrain.Decide(BotLevel.Cautious, hole, new List<Card>(), FacingBetOptions(), 150, random);
                if (action.Type == ActionType.Fold)
                {
                    folds++;
                }
            }

            Assert.AreEqual(100, folds);
        }

        [Test]
        public void BoldBot_RaisesMoreOften_ThanCautiousBot()
        {
            var hole = new[] { new Card(Rank.King, Suit.Spades), new Card(Rank.Jack, Suit.Diamonds) };
            var board = new[]
            {
                new Card(Rank.King, Suit.Hearts), new Card(Rank.Five, Suit.Clubs), new Card(Rank.Two, Suit.Diamonds)
            };

            int cautiousRaises = CountRaises(BotLevel.Cautious, hole, board);
            int boldRaises = CountRaises(BotLevel.Bold, hole, board);

            Assert.Greater(boldRaises, cautiousRaises);
        }

        private static int CountRaises(BotLevel level, Card[] hole, Card[] board)
        {
            var random = new Random(4);
            int raises = 0;
            for (int i = 0; i < 500; i++)
            {
                PlayerAction action = BotBrain.Decide(level, hole, board, CheckOptions(), 100, random);
                if (action.Type == ActionType.Raise)
                {
                    raises++;
                }
            }

            return raises;
        }
    }
}
