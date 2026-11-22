using PokerDemo.Core;
using PokerDemo.Network;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // A calm reminder after a long stretch of play. It can be dismissed or switched off for good,
    // and it never covers the screen while it is the player's turn.
    public class BreakReminder : MonoBehaviour
    {
        private const string SaveKey = "poker.breakReminders";

        [SerializeField] private PokerNetwork network;
        [SerializeField] private TableNetwork table;
        [SerializeField] private float reminderMinutes = 60f;

        [Header("Reminder")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button okButton;
        [SerializeField] private Button turnOffButton;

        [Header("Switch in the lobby")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Text toggleLabel;

        private SessionTimer timer;
        private bool remindersOn;
        private bool waitingToShow;

        private void Awake()
        {
            timer = new SessionTimer(reminderMinutes * 60f);
            remindersOn = PlayerPrefs.GetInt(SaveKey, 1) == 1;

            okButton.onClick.AddListener(() => waitingToShow = false);
            turnOffButton.onClick.AddListener(() => SetReminders(false));
            toggleButton.onClick.AddListener(() => SetReminders(!remindersOn));
            SetReminders(remindersOn);
        }

        private void Update()
        {
            // Only time spent at a table counts.
            if (network.InRoom && remindersOn && timer.Advance(Time.unscaledDeltaTime))
            {
                waitingToShow = true;
                messageText.text = "You have been playing for about " + Mathf.RoundToInt(timer.TotalSeconds / 60f)
                                   + " minutes.\nThis could be a good moment for a short break.";
            }

            panel.SetActive(waitingToShow && remindersOn && !IsMyTurn());
            toggleButton.gameObject.SetActive(!network.InRoom);
        }

        private void SetReminders(bool on)
        {
            remindersOn = on;
            waitingToShow = waitingToShow && on;
            toggleLabel.text = "Break reminders: " + (on ? "On" : "Off");

            PlayerPrefs.SetInt(SaveKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        private bool IsMyTurn()
        {
            TableSnapshot snapshot = table.Snapshot;
            if (snapshot == null || !snapshot.handActive)
            {
                return false;
            }

            SeatSnapshot me = snapshot.seats.FirstOrDefault(s => s.playerId == table.MyId);
            return me != null && me.seat == snapshot.currentSeat;
        }
    }
}
