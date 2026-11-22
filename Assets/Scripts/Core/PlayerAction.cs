namespace PokerDemo.Core
{
    public readonly struct PlayerAction
    {
        public readonly ActionType Type;

        // Only used by Raise. It is the total bet for the round ("raise to"), not the extra chips.
        public readonly int Amount;

        public PlayerAction(ActionType type, int amount = 0)
        {
            Type = type;
            Amount = amount;
        }
    }
}
