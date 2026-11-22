using PokerDemo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // One seat around the table: name, chips, bet, last action and the two cards.
    public class SeatView : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.08f, 0.12f, 0.14f, 0.85f);
        private static readonly Color TurnColor = new Color(0.75f, 0.6f, 0.1f, 0.95f);
        private static readonly Color FoldedColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
        private static readonly Color WinnerColor = new Color(0.1f, 0.5f, 0.2f, 0.95f);

        [SerializeField] private Image background;
        [SerializeField] private Text nameText;
        [SerializeField] private Text chipsText;
        [SerializeField] private Text betText;
        [SerializeField] private Text actionText;
        [SerializeField] private Text resultText;
        [SerializeField] private CardView firstCard;
        [SerializeField] private CardView secondCard;
        [SerializeField] private GameObject dealerMarker;

        // myCards are only filled in on the seat of the local player.
        public void Show(SeatSnapshot seat, bool isMe, bool isTurn, bool isDealer, string[] myCards)
        {
            gameObject.SetActive(true);

            nameText.text = seat.name + (isMe ? " (you)" : "") + (seat.connected ? "" : " (offline)");
            chipsText.text = "Chips " + seat.stack;
            betText.text = seat.bet > 0 ? "Bet " + seat.bet : "";
            actionText.text = seat.spectating ? "Out of chips" : (seat.folded ? "Folded" : seat.lastAction);
            dealerMarker.SetActive(isDealer);

            ShowCards(seat, isMe, myCards);
            ShowResult(seat);

            if (seat.won > 0)
            {
                background.color = WinnerColor;
            }
            else if (isTurn)
            {
                background.color = TurnColor;
            }
            else
            {
                background.color = seat.folded ? FoldedColor : NormalColor;
            }
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }

        private void ShowCards(SeatSnapshot seat, bool isMe, string[] myCards)
        {
            if (seat.cards.Length == 2)
            {
                // The hand reached a showdown, so everyone gets to see these.
                firstCard.ShowCard(seat.cards[0]);
                secondCard.ShowCard(seat.cards[1]);
            }
            else if (!seat.inHand || seat.folded)
            {
                firstCard.Hide();
                secondCard.Hide();
            }
            else if (isMe && myCards.Length == 2)
            {
                firstCard.ShowCard(myCards[0]);
                secondCard.ShowCard(myCards[1]);
            }
            else
            {
                firstCard.ShowBack();
                secondCard.ShowBack();
            }
        }

        private void ShowResult(SeatSnapshot seat)
        {
            if (seat.won > 0)
            {
                resultText.text = "Won " + seat.won + (seat.handName != "" ? "\n" + seat.handName : "");
            }
            else
            {
                resultText.text = seat.handName;
            }
        }
    }
}
