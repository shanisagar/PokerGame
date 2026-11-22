using System;

namespace PokerDemo.Core
{
    // The one retention feature: a simple record of how you played.
    // Public fields in camelCase because Unity's JsonUtility needs them.
    [Serializable]
    public class PlayerStats
    {
        public int handsPlayed;
        public int handsWon;
        public int biggestPot;

        // Best hand that was shown at a showdown, as a HandRank number. -1 means none yet.
        public int bestHandRank = -1;

        public string BestHandName =>
            bestHandRank < 0 ? "-" : new HandValue((HandRank)bestHandRank).Name;

        // shownHandRank is -1 when the hand ended without a showdown.
        public void RecordHand(bool won, int chipsWon, int shownHandRank)
        {
            handsPlayed++;

            if (won)
            {
                handsWon++;
                biggestPot = Math.Max(biggestPot, chipsWon);
            }

            if (shownHandRank > bestHandRank)
            {
                bestHandRank = shownHandRank;
            }
        }
    }
}
