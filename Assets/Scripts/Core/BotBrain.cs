using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerDemo.Core
{
    public enum BotLevel
    {
        Cautious,
        Bold
    }

    // Computer opponents. A bot only looks at what a real player could see:
    // its own two cards, the board and the betting.
    public static class BotBrain
    {
        public static PlayerAction Decide(BotLevel level, IReadOnlyList<Card> hole, IReadOnlyList<Card> board,
            ActionOptions options, int pot, Random random)
        {
            double strength = Strength(hole, board);

            return level == BotLevel.Cautious
                ? DecideCautious(strength, options, pot, random)
                : DecideBold(strength, options, pot, random);
        }

        // Plays few hands, folds to big bets and only raises with a strong hand.
        private static PlayerAction DecideCautious(double strength, ActionOptions options, int pot, Random random)
        {
            strength += Noise(random, 0.05);

            if (options.CanCheck)
            {
                if (strength > 0.75 && options.CanRaise)
                {
                    return Raise(options, pot, 0.5);
                }

                return new PlayerAction(ActionType.Check);
            }

            if (strength < 0.35 + PotOdds(options, pot) * 0.8)
            {
                return new PlayerAction(ActionType.Fold);
            }

            if (strength > 0.85 && options.CanRaise)
            {
                return Raise(options, pot, 0.5);
            }

            return new PlayerAction(ActionType.Call);
        }

        // Plays many hands, bets and raises a lot, and bluffs now and then.
        private static PlayerAction DecideBold(double strength, ActionOptions options, int pot, Random random)
        {
            strength += Noise(random, 0.1);

            if (random.NextDouble() < 0.18)
            {
                strength = Math.Max(strength, 0.7); // bluff
            }

            if (options.CanCheck)
            {
                if (strength > 0.45 && options.CanRaise)
                {
                    return Raise(options, pot, 0.7);
                }

                return new PlayerAction(ActionType.Check);
            }

            if (strength < 0.2 + PotOdds(options, pot) * 0.5)
            {
                return new PlayerAction(ActionType.Fold);
            }

            if (strength > 0.6 && options.CanRaise && random.NextDouble() < 0.6)
            {
                return Raise(options, pot, 1.0);
            }

            return new PlayerAction(ActionType.Call);
        }

        // 0 means a hopeless hand, 1 means a monster.
        private static double Strength(IReadOnlyList<Card> hole, IReadOnlyList<Card> board)
        {
            if (board.Count == 0)
            {
                return PreflopStrength(hole);
            }

            HandValue value = HandEvaluator.Evaluate(hole.Concat(board).ToList());

            switch (value.Rank)
            {
                case HandRank.HighCard:
                    return 0.1 + hole.Max(c => (int)c.Rank) / 14.0 * 0.1;
                case HandRank.OnePair:
                    // A pair that is only on the board does not help much.
                    bool usesHoleCard = hole.Any(c => (int)c.Rank == value.Kickers[0]);
                    return usesHoleCard ? 0.45 + value.Kickers[0] / 14.0 * 0.15 : 0.25;
                case HandRank.TwoPair:
                    return 0.65;
                case HandRank.ThreeOfAKind:
                    return 0.75;
                case HandRank.Straight:
                    return 0.85;
                case HandRank.Flush:
                    return 0.9;
                case HandRank.FullHouse:
                    return 0.95;
                default:
                    return 0.98;
            }
        }

        private static double PreflopStrength(IReadOnlyList<Card> hole)
        {
            int high = Math.Max((int)hole[0].Rank, (int)hole[1].Rank);
            int low = Math.Min((int)hole[0].Rank, (int)hole[1].Rank);

            if (high == low)
            {
                return 0.5 + high / 14.0 * 0.45;
            }

            double strength = (high + low) / 28.0 * 0.6;
            if (hole[0].Suit == hole[1].Suit)
            {
                strength += 0.05;
            }

            if (high - low == 1)
            {
                strength += 0.04;
            }

            return strength;
        }

        // How much of the pot we would have to pay to stay in.
        private static double PotOdds(ActionOptions options, int pot)
        {
            return options.CallAmount / (double)(pot + options.CallAmount);
        }

        private static PlayerAction Raise(ActionOptions options, int pot, double potShare)
        {
            int amount = options.MinRaiseTo + (int)(pot * potShare);
            amount = Math.Min(Math.Max(amount, options.MinRaiseTo), options.MaxRaiseTo);
            return new PlayerAction(ActionType.Raise, amount);
        }

        private static double Noise(Random random, double size)
        {
            return (random.NextDouble() * 2 - 1) * size;
        }
    }
}
