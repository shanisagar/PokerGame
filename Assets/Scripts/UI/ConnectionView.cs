using PokerDemo.Network;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Full screen message shown when the connection to the server is lost, with a Reconnect button.
    public class ConnectionView : MonoBehaviour
    {
        [SerializeField] private PokerNetwork network;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button reconnectButton;

        private void Awake()
        {
            reconnectButton.onClick.AddListener(network.Reconnect);
        }

        private void Update()
        {
            panel.SetActive(network.LostConnection);
            if (network.LostConnection)
            {
                messageText.text = network.ConnectionMessage;
            }
        }
    }
}
