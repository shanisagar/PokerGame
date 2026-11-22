using PokerDemo.Core;
using PokerDemo.Network;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Keeps simple personal stats (hands played, hands won, best hand, biggest pot).
    // They are saved on this device only and are just a record, nothing is rewarded for them.
    public class StatsTracker : MonoBehaviour
    {
        private const string SaveKey = "poker.stats";

        [SerializeField] private PokerNetwork network;
        [SerializeField] private TableNetwork table;
        [SerializeField] private Text lobbyText;
        [SerializeField] private Text tableText;

        private PlayerStats stats;
        private string lastRecordedHand = "";

        private void Awake()
        {
            stats = Load();
            ShowStats();
        }

        private void Update()
        {
            TableSnapshot snapshot = table.Snapshot;
            if (!network.InRoom || snapshot == null || snapshot.handActive)
            {
                return;
            }

            SeatSnapshot me = snapshot.seats.FirstOrDefault(s => s.playerId == table.MyId);
            string handKey = network.RoomCode + ":" + snapshot.handNumber;

            // Count each finished hand once, and only if we were dealt in.
            if (me != null && me.inHand && handKey != lastRecordedHand)
            {
                lastRecordedHand = handKey;
                stats.RecordHand(me.won > 0, me.won, me.handRank);
                Save();
                ShowStats();
            }
        }

        private void ShowStats()
        {
            string text = "Hands played " + stats.handsPlayed + "    Won " + stats.handsWon
                          + "\nBest hand " + stats.BestHandName + "    Biggest pot " + stats.biggestPot;

            lobbyText.text = "Your stats\n" + text;
            tableText.text = text;
        }

        private static PlayerStats Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(SaveKey, "");
                if (json != "")
                {
                    return JsonUtility.FromJson<PlayerStats>(json);
                }
            }
            catch (System.ArgumentException)
            {
                // Unreadable save, start fresh.
            }

            return new PlayerStats();
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(stats));
            PlayerPrefs.Save();
        }
    }
}
