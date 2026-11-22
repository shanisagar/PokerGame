namespace PokerDemo.Core
{
    // One player's chips during a hand. The betting round and the hand state both use this.
    public sealed class BettingSeat
    {
        public readonly int Seat;
        public int Stack;
        public int Bet;          // chips in this betting round
        public int Committed;    // chips in the whole hand, used for pots
        public bool Folded;
        public bool AllIn;
        public bool NeedsToAct;

        public BettingSeat(int seat, int stack)
        {
            Seat = seat;
            Stack = stack;
        }

        public void Pay(int amount)
        {
            Stack -= amount;
            Bet += amount;
            Committed += amount;
            if (Stack == 0)
            {
                AllIn = true;
            }
        }

        public void ResetBet()
        {
            Bet = 0;
        }
    }
}
