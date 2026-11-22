namespace PokerDemo.Core
{
    // Chips a seat has put in during one hand.
    public readonly struct Contribution
    {
        public readonly int Seat;
        public readonly int Committed;
        public readonly bool Folded;

        public Contribution(int seat, int committed, bool folded)
        {
            Seat = seat;
            Committed = committed;
            Folded = folded;
        }
    }
}
