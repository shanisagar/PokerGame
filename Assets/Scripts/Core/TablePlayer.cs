namespace PokerDemo.Core
{
    // Someone sitting at the table. Id is the Photon actor number, bots get negative ids.
    public sealed class TablePlayer
    {
        public readonly int Id;
        public readonly string Name;
        public readonly int Seat;
        public int Stack;
        public bool IsBot;
        public BotLevel Level;

        // False while the player is disconnected. The seat and chips are kept so they can come back.
        public bool Connected = true;

        // The player left for good. They are removed once the current hand is over.
        public bool Left;

        public string LastAction = "";
        public int Won;

        public TablePlayer(int id, string name, int seat, int stack)
        {
            Id = id;
            Name = name;
            Seat = seat;
            Stack = stack;
        }
    }
}
