using System;
using System.Text;

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

        // "TwoPair" becomes "Two Pair".
        public string Name
        {
            get
            {
                string text = Rank.ToString();
                var result = new StringBuilder();
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && char.IsUpper(text[i]))
                    {
                        result.Append(' ');
                    }

                    result.Append(text[i]);
                }

                return result.ToString();
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
