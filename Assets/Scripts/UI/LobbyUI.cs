using PokerDemo.Network;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Lobby screen: name, create table, join by code, list of open tables.
    // The layout lives in the scene (Canvas > Background > LobbyPanel), this script only
    // fills it with data and reacts to the buttons. The table screen is TableView.
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private PokerNetwork network;

        [Header("Lobby")]
        [SerializeField] private RectTransform lobbyPanel;
        [SerializeField] private RectTransform roomList;
        [SerializeField] private Text statusText;
        [SerializeField] private InputField nameInput;
        [SerializeField] private InputField codeInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;

        private void Awake()
        {
            nameInput.onEndEdit.AddListener(network.SetName);
            createButton.onClick.AddListener(OnCreateClicked);
            joinButton.onClick.AddListener(OnJoinClicked);

            network.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (network != null)
            {
                network.Changed -= Refresh;
            }
        }

        private void OnCreateClicked()
        {
            network.SetName(nameInput.text);
            network.CreateRoom();
        }

        private void OnJoinClicked()
        {
            JoinTable(codeInput.text);
        }

        private void JoinTable(string code)
        {
            network.SetName(nameInput.text);
            network.JoinRoom(code);
        }

        private void Refresh()
        {
            // Photon reports a disconnect while Play mode is shutting down, after the UI is already gone.
            if (lobbyPanel == null)
            {
                return;
            }

            lobbyPanel.gameObject.SetActive(!network.InRoom);
            statusText.text = network.Status;
            createButton.interactable = network.InLobby;
            joinButton.interactable = network.InLobby;

            if (!network.InRoom)
            {
                RefreshRoomList();
            }
        }

        // Rows are created at runtime because the list changes all the time.
        private void RefreshRoomList()
        {
            foreach (Transform row in roomList)
            {
                Destroy(row.gameObject);
            }

            foreach (RoomInfo room in network.Rooms)
            {
                string code = room.Name;
                string text = code + "   " + room.PlayerCount + "/" + room.MaxPlayers + " players";

                Button row = UiFactory.CreateButton("Room " + code, roomList, text, () => JoinTable(code));
                row.interactable = room.PlayerCount < room.MaxPlayers;
            }
        }
    }
}
