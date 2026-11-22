using System.Collections.Generic;

namespace PokerDemo.Core
{
    public sealed class Pot
    {
        public int Amount;
        public readonly List<int> EligibleSeats = new List<int>();
    }
}
