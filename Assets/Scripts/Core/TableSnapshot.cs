using System;

namespace PokerDemo.Core
{
    // What every player is allowed to see. Hole cards are never in here,
    // except for players who reached a showdown.
    // Public fields in camelCase because Unity's JsonUtility needs them.
    [Serializable]
    public class SeatSnapshot
    {
        public int playerId;
        public int seat;
        public string name = "";
        public int stack;
        public int bet;
        public bool inHand;
        public bool folded;
        public bool allIn;
        public bool connected;
        public bool isBot;

        // Out of chips and watching until the player restarts.
        public bool spectating;
        public string lastAction = "";
        public int won;
        public string[] cards = new string[0];
        public string handName = "";

        // HandRank number of the shown hand, -1 when nothing was shown.
        public int handRank = -1;
    }

    [Serializable]
    public class TableSnapshot
    {
        public int handNumber;
        public int stage;
        public bool handActive;
        public int buttonSeat = -1;
        public int currentSeat = -1;
        public float turnSecondsLeft;
        public int pot;
        public int smallBlind;
        public int bigBlind;
        public string[] board = new string[0];
        public SeatSnapshot[] seats = new SeatSnapshot[0];
        public ActionOptions options;
        public string message = "";
    }
}
