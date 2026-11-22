using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PokerDemo.Network
{
    // Wraps Photon for the lobby: connect, create a table, join by code or from the list.
    public class PokerNetwork : MonoBehaviourPunCallbacks
    {
        public const int MaxPlayers = 6;

        private readonly Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();

        public event Action Changed;

        public string Status { get; private set; } = "Connecting to server...";
        public bool InLobby => PhotonNetwork.InLobby;
        public bool InRoom => PhotonNetwork.InRoom;
        public string RoomCode => PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "";
        public IEnumerable<RoomInfo> Rooms => rooms.Values;

        public IEnumerable<string> PlayerNames =>
            PhotonNetwork.PlayerList.Select(p => p.NickName + (p.IsLocal ? " (you)" : ""));

        private void Start()
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.GameVersion = "1";
            PhotonNetwork.NickName = "Player" + UnityEngine.Random.Range(100, 1000);
            PhotonNetwork.ConnectUsingSettings();
        }

        public void SetName(string playerName)
        {
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                PhotonNetwork.NickName = playerName.Trim();
            }
        }

        public void CreateRoom()
        {
            var options = new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = true, IsOpen = true };
            PhotonNetwork.CreateRoom(MakeCode(), options);
            Status = "Creating table...";
            Notify();
        }

        public void JoinRoom(string code)
        {
            code = code.Trim().ToUpper();
            if (code.Length == 0)
            {
                Status = "Type a table code first.";
                Notify();
                return;
            }

            PhotonNetwork.JoinRoom(code);
            Status = "Joining table " + code + "...";
            Notify();
        }

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        // Four letters, no I or O so the code is easy to read out loud.
        private static string MakeCode()
        {
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            var chars = new char[4];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = letters[UnityEngine.Random.Range(0, letters.Length)];
            }

            return new string(chars);
        }

        private void Notify()
        {
            Changed?.Invoke();
        }

        public override void OnConnectedToMaster()
        {
            // Also runs again after leaving a room, which puts us back in the lobby.
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            rooms.Clear();
            Status = "Create a table or pick one from the list.";
            Notify();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList || !info.IsOpen || !info.IsVisible || info.PlayerCount == 0)
                {
                    rooms.Remove(info.Name);
                }
                else
                {
                    rooms[info.Name] = info;
                }
            }

            Notify();
        }

        public override void OnJoinedRoom()
        {
            Status = "Waiting for players.";
            Notify();
        }

        public override void OnLeftRoom()
        {
            Notify();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (returnCode == ErrorCode.GameIdAlreadyExists)
            {
                CreateRoom(); // the random code was taken, try another one
                return;
            }

            Status = "Could not create table: " + message;
            Notify();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Status = "Could not join table: " + message;
            Notify();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Notify();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Notify();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            rooms.Clear();
            Status = "Lost connection to the server (" + cause + ").";
            Notify();
        }
    }
}
