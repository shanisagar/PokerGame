namespace PokerDemo.Core
{
    // What the current player is allowed to do. The UI and bots both read this.
    public struct ActionOptions
    {
        public bool CanFold;
        public bool CanCheck;
        public bool CanCall;
        public int CallAmount;
        public bool CanRaise;
        public int MinRaiseTo;
        public int MaxRaiseTo;
        public bool CanAllIn;
    }
}
