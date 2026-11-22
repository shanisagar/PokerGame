using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // One card slot. Can show a face, a card back, an empty outline, or nothing.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        // code looks like "As" or "Td", the same short form the server sends.
        public void ShowCard(string code)
        {
            gameObject.SetActive(true);

            string rank = code[0] == 'T' ? "10" : code[0].ToString();
            bool isRed = code[1] == 'h' || code[1] == 'd';

            label.text = rank + SuitSymbol(code[1]);
            label.color = isRed ? new Color(0.8f, 0.1f, 0.1f) : Color.black;
            background.color = Color.white;
        }

        public void ShowBack()
        {
            gameObject.SetActive(true);
            label.text = "";
            background.color = new Color(0.15f, 0.25f, 0.6f);
        }

        public void ShowEmpty()
        {
            gameObject.SetActive(true);
            label.text = "";
            background.color = new Color(1f, 1f, 1f, 0.12f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string SuitSymbol(char suit)
        {
            switch (suit)
            {
                case 'c': return "♣";
                case 'd': return "♦";
                case 'h': return "♥";
                default: return "♠";
            }
        }
    }
}
