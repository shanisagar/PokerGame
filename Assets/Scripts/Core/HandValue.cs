using System;

namespace PokerDemo.Core
{
    // Kickers are the tiebreak values, most important first.
    public readonly struct HandValue : IComparable<HandValue>
    {
        public readonly HandRank Rank;
        public readonly int[] Kickers;

        public HandValue(HandRank rank, params int[] kickers)
        {
            Rank = rank;
            Kickers = kickers;
        }

        public int CompareTo(HandValue other)
        {
            int result = Rank.CompareTo(other.Rank);
            if (result != 0)
            {
                return result;
            }

            for (int i = 0; i < Kickers.Length && i < other.Kickers.Length; i++)
            {
                result = Kickers[i].CompareTo(other.Kickers[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return Rank.ToString();
        }
    }
}
