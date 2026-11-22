using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PokerDemo.Core;
using System;
using UnityEngine;

namespace PokerDemo.Network
{
    // Sends the table over Photon. The Master Client runs the real Table (shuffling, dealing,
    // rules, timer). Everyone else only sends actions to it and shows what it sends back.
    //
    // Hole cards are sent with a separate event addressed to one player, so nobody else gets them.
    // Players only trust table events that come from the Master Client.
    public class TableNetwork : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte ActionEvent = 1;      // player -> host
        private const byte RestartEvent = 2;     // player -> host, asks to start again with fresh chips
        private const byte SnapshotEvent = 10;   // host -> everyone
        private const byte HoleCardsEvent = 11;  // host -> one player
        private const byte RejectedEvent = 12;   // host -> one player

        [Serializable]
        private class ActionMessage
        {
            public int type;
            public int amount;
        }

        [Serializable]
        private class HoleCardsMessage
        {
            public int hand;
            public string[] cards;
        }

        private Table table; // only exists on the host
        private int lastSentVersion = -1;
        private int lastDealtHand;

        private string[] holeCards = new string[0];
        private int holeCardsHand = -1;
        private float snapshotTime;

        public event Action Updated;

        public TableSnapshot Snapshot { get; private set; }
        public string Notice { get; private set; } = "";
        public bool IsHost => PhotonNetwork.IsMasterClient;
        public bool CanStart => IsHost && table != null && table.CanStartGame;
        public int MyId => PhotonNetwork.LocalPlayer.ActorNumber;

        // Only valid for the hand we were dealt into.
        public string[] MyHoleCards =>
            Snapshot != null && holeCardsHand == Snapshot.handNumber ? holeCards : new string[0];

        public float TurnSecondsLeft =>
            Snapshot == null ? 0f : Mathf.Max(0f, Snapshot.turnSecondsLeft - (Time.unscaledTime - snapshotTime));

        private void Update()
        {
            if (table == null || !IsHost)
            {
                return;
            }

            table.Tick(Time.deltaTime);
            if (table.Version != lastSentVersion)
            {
                Broadcast();
            }
        }

        // Called by the host to deal the first hand. After that hands start on their own.
        // With a single player, bots join the table.
        public void StartGame()
        {
            if (CanStart)
            {
                table.StartGame();
            }
        }

        public void AddBot(BotLevel level)
        {
            if (IsHost && table != null)
            {
                table.AddBot(level);
            }
        }

        public void RemoveBots()
        {
            if (IsHost && table != null)
            {
                table.RemoveBots();
            }
        }

        // A player with no chips can ask to start again with the starting amount.
        public void RequestRestart()
        {
            if (IsHost)
            {
                HandleRestart(MyId);
            }
            else
            {
                var options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
                PhotonNetwork.RaiseEvent(RestartEvent, "restart", options, SendOptions.SendReliable);
            }
        }

        public void SendAction(PlayerAction action)
        {
            string json = JsonUtility.ToJson(new ActionMessage { type = (int)action.Type, amount = action.Amount });

            if (IsHost)
            {
                HandleAction(MyId, json);
            }
            else
            {
                var options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
                PhotonNetwork.RaiseEvent(ActionEvent, json, options, SendOptions.SendReliable);
            }
        }

        // ---- host side ----

        private void BuildTable()
        {
            table = new Table();
            lastSentVersion = -1;
            lastDealtHand = 0;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!player.IsInactive)
                {
                    table.AddPlayer(player.ActorNumber, player.NickName);
                }
            }
        }

        private void HandleRestart(int senderId)
        {
            string error;
            if (!table.Restart(senderId, out error))
            {
                Reject(senderId, error);
            }
        }

        private void HandleAction(int senderId, string json)
        {
            ActionMessage message;
            try
            {
                message = JsonUtility.FromJson<ActionMessage>(json);
            }
            catch (ArgumentException)
            {
                message = null;
            }

            // Anything that does not look like a real action is thrown away.
            if (message == null || !Enum.IsDefined(typeof(ActionType), message.type))
            {
                Reject(senderId, "That action was not understood.");
                return;
            }

            var action = new PlayerAction((ActionType)message.type, message.amount);
            string error;
            if (!table.TryAct(senderId, action, out error))
            {
                Debug.LogWarning("Rejected action from player " + senderId + ": " + error);
                Reject(senderId, error);
            }
        }

        private void Reject(int playerId, string reason)
        {
            if (playerId == MyId)
            {
                Notice = reason;
                Updated?.Invoke();
                return;
            }

            var options = new RaiseEventOptions { TargetActors = new[] { playerId } };
            PhotonNetwork.RaiseEvent(RejectedEvent, reason, options, SendOptions.SendReliable);
        }

        private void Broadcast()
        {
            lastSentVersion = table.Version;

            // Hole cards go out first, once per new hand.
            if (table.HandNumber != lastDealtHand)
            {
                lastDealtHand = table.HandNumber;
                foreach (int playerId in table.HumanIdsInHand())
                {
                    SendHoleCards(playerId);
                }
            }

            string json = JsonUtility.ToJson(table.BuildSnapshot());
            ApplySnapshot(json);
            PhotonNetwork.RaiseEvent(SnapshotEvent, json, new RaiseEventOptions(), SendOptions.SendReliable);
        }

        private void SendHoleCards(int playerId)
        {
            var message = new HoleCardsMessage { hand = table.HandNumber, cards = table.GetHoleCards(playerId) };
            string json = JsonUtility.ToJson(message);

            if (playerId == MyId)
            {
                ApplyHoleCards(json);
                return;
            }

            var options = new RaiseEventOptions { TargetActors = new[] { playerId } };
            PhotonNetwork.RaiseEvent(HoleCardsEvent, json, options, SendOptions.SendReliable);
        }

        // ---- everyone ----

        public void OnEvent(EventData photonEvent)
        {
            string data = photonEvent.CustomData as string;
            if (data == null)
            {
                return;
            }

            if (photonEvent.Code == ActionEvent)
            {
                if (IsHost && table != null)
                {
                    HandleAction(photonEvent.Sender, data);
                }

                return;
            }

            if (photonEvent.Code == RestartEvent)
            {
                if (IsHost && table != null)
                {
                    HandleRestart(photonEvent.Sender);
                }

                return;
            }

            // The rest must come from the host, otherwise another player could fake the table.
            bool fromHost = PhotonNetwork.MasterClient != null && photonEvent.Sender == PhotonNetwork.MasterClient.ActorNumber;
            if (!fromHost)
            {
                return;
            }

            if (photonEvent.Code == SnapshotEvent)
            {
                ApplySnapshot(data);
            }
            else if (photonEvent.Code == HoleCardsEvent)
            {
                ApplyHoleCards(data);
            }
            else if (photonEvent.Code == RejectedEvent)
            {
                Notice = data;
                Updated?.Invoke();
            }
        }

        private void ApplySnapshot(string json)
        {
            Snapshot = JsonUtility.FromJson<TableSnapshot>(json);
            snapshotTime = Time.unscaledTime;
            Notice = "";
            Updated?.Invoke();
        }

        private void ApplyHoleCards(string json)
        {
            HoleCardsMessage message = JsonUtility.FromJson<HoleCardsMessage>(json);
            holeCards = message.cards;
            holeCardsHand = message.hand;
            Updated?.Invoke();
        }

        private void ResetLocalState()
        {
            table = null;
            Snapshot = null;
            holeCards = new string[0];
            holeCardsHand = -1;
            Notice = "";
        }

        // ---- Photon callbacks ----

        public override void OnJoinedRoom()
        {
            // Also runs when we come back after a disconnect. A host keeps the table it already has.
            Snapshot = null;
            holeCards = new string[0];
            holeCardsHand = -1;

            if (IsHost && table == null)
            {
                BuildTable();
            }

            Updated?.Invoke();
        }

        public override void OnLeftRoom()
        {
            ResetLocalState();
            Updated?.Invoke();
        }

        // Also reached when going back to the lobby because our old table is gone.
        public override void OnJoinedLobby()
        {
            ResetLocalState();
            Updated?.Invoke();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!IsHost || table == null)
            {
                return;
            }

            if (table.GetPlayer(newPlayer.ActorNumber) != null)
            {
                // Someone who dropped is back. They get their seat, chips and cards again.
                table.PlayerReconnected(newPlayer.ActorNumber);
                SendHoleCards(newPlayer.ActorNumber);
            }
            else
            {
                table.AddPlayer(newPlayer.ActorNumber, newPlayer.NickName);
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (!IsHost || table == null)
            {
                return;
            }

            // Photon keeps a dropped player for a while (IsInactive). Only a real leave frees the seat.
            if (otherPlayer.IsInactive)
            {
                table.PlayerDisconnected(otherPlayer.ActorNumber);
            }
            else
            {
                table.RemovePlayer(otherPlayer.ActorNumber);
            }
        }

        // The table lived on the old host, so a new host can only start a fresh one.
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            ResetLocalState();
            if (IsHost)
            {
                BuildTable();
                Notice = "The host left, so the table was restarted.";
            }

            Updated?.Invoke();
        }
    }
}
