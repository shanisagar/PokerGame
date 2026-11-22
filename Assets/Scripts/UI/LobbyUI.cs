using PokerDemo.Network;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PokerDemo.UI
{
    // Lobby screen: name, create table, join by code, list of open tables.
    // The waiting room is a placeholder until the table screen is built.
    public class LobbyUI : MonoBehaviour
    {
        private PokerNetwork network;

        private RectTransform lobbyPanel;
        private RectTransform roomPanel;
        private RectTransform roomList;
        private Text statusText;
        private Text roomTitle;
        private Text playersText;
        private InputField nameInput;
        private InputField codeInput;

        private void Awake()
        {
            network = gameObject.AddComponent<PokerNetwork>();
            BuildUI();
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

        private void BuildUI()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = UiFactory.Panel("Background", canvasObject.transform, new Color(0.04f, 0.27f, 0.18f));
            UiFactory.Stretch(background);

            BuildLobbyPanel(background);
            BuildRoomPanel(background);
        }

        private void BuildLobbyPanel(Transform parent)
        {
            lobbyPanel = UiFactory.Column("LobbyPanel", parent, 520);

            UiFactory.Label("Title", lobbyPanel, "Texas Hold'em", 44);
            statusText = UiFactory.Label("Status", lobbyPanel, "", 20);

            nameInput = UiFactory.CreateInput("NameInput", lobbyPanel, "Your name");
            nameInput.onEndEdit.AddListener(network.SetName);

            UiFactory.CreateButton("CreateButton", lobbyPanel, "Create a new table", () =>
            {
                network.SetName(nameInput.text);
                network.CreateRoom();
            });

            codeInput = UiFactory.CreateInput("CodeInput", lobbyPanel, "Table code, for example KQWR");
            codeInput.characterLimit = 4;
            UiFactory.CreateButton("JoinButton", lobbyPanel, "Join with code", () =>
            {
                network.SetName(nameInput.text);
                network.JoinRoom(codeInput.text);
            });

            UiFactory.Label("OpenTablesLabel", lobbyPanel, "Open tables", 24);
            roomList = UiFactory.Column("RoomList", lobbyPanel, 520);
        }

        private void BuildRoomPanel(Transform parent)
        {
            roomPanel = UiFactory.Column("RoomPanel", parent, 520);

            roomTitle = UiFactory.Label("RoomTitle", roomPanel, "", 40);
            playersText = UiFactory.Label("Players", roomPanel, "", 24);
            UiFactory.CreateButton("LeaveButton", roomPanel, "Leave table", network.LeaveRoom);
        }

        private void Refresh()
        {
            lobbyPanel.gameObject.SetActive(!network.InRoom);
            roomPanel.gameObject.SetActive(network.InRoom);

            statusText.text = network.Status;
            RefreshRoomList();

            if (network.InRoom)
            {
                roomTitle.text = "Table " + network.RoomCode;
                playersText.text = string.Join("\n", network.PlayerNames);
            }
        }

        private void RefreshRoomList()
        {
            foreach (Transform child in roomList)
            {
                Destroy(child.gameObject);
            }

            foreach (RoomInfo room in network.Rooms)
            {
                string code = room.Name;
                string text = code + "   " + room.PlayerCount + "/" + room.MaxPlayers + " players";

                Button button = UiFactory.CreateButton("Room " + code, roomList, text, () =>
                {
                    network.SetName(nameInput.text);
                    network.JoinRoom(code);
                });
                button.interactable = room.PlayerCount < room.MaxPlayers;
            }
        }
    }
}
