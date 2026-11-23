using NUnit.Framework;
using PokerDemo.Core;
using System;
using System.Linq;

namespace PokerDemo.Tests
{
    public class TableTests
    {
        private static Table TableWith(int playerCount)
        {
            var table = new Table();
            for (int i = 0; i < playerCount; i++)
            {
                table.AddPlayer(100 + i, "Player" + i);
            }

            return table;
        }

        [Test]
        public void CannotStart_WithOnePlayer()
        {
            Table table = TableWith(1);

            Assert.IsFalse(table.CanStart);
            Assert.IsFalse(table.StartHand());
        }

        [Test]
        public void StartHand_PostsBlinds_AndDealsHoleCards()
        {
            Table table = TableWith(3);

            Assert.IsTrue(table.StartHand());
            TableSnapshot snapshot = table.BuildSnapshot();

            Assert.IsTrue(snapshot.handActive);
            Assert.AreEqual(Table.SmallBlind + Table.BigBlind, snapshot.pot);
            Assert.AreEqual(2, table.GetHoleCards(100).Length);
        }

        [Test]
        public void Snapshot_NeverContainsHoleCards_DuringTheHand()
        {
            Table table = TableWith(3);
            table.StartHand();

            TableSnapshot snapshot = table.BuildSnapshot();

            Assert.IsTrue(snapshot.seats.All(s => s.cards.Length == 0));
        }

        [Test]
        public void Action_FromPlayerWhoseTurnItIsNot_IsRejected()
        {
            Table table = TableWith(3);
            table.StartHand();

            int currentSeat = table.BuildSnapshot().currentSeat;
            TablePlayer other = table.Players.First(p => p.Seat != currentSeat);

            string error;
            bool accepted = table.TryAct(other.Id, new PlayerAction(ActionType.Fold), out error);

            Assert.IsFalse(accepted);
            Assert.IsNotNull(error);
        }

        [Test]
        public void TurnTimeout_FoldsPlayerFacingABet()
        {
            Table table = TableWith(2);
            table.StartHand();

            table.Tick(Table.TurnSeconds + 1f);

            TableSnapshot snapshot = table.BuildSnapshot();
            Assert.IsFalse(snapshot.handActive);
            StringAssert.Contains("wins", snapshot.message);
        }

        [Test]
        public void NextHand_StartsOnItsOwn_AndButtonMoves()
        {
            Table table = TableWith(2);
            table.StartHand();
            int firstButton = table.BuildSnapshot().buttonSeat;

            table.Tick(Table.TurnSeconds + 1f);
            table.Tick(Table.NextHandDelay + 1f);

            TableSnapshot snapshot = table.BuildSnapshot();
            Assert.AreEqual(2, table.HandNumber);
            Assert.IsTrue(snapshot.handActive);
            Assert.AreNotEqual(firstButton, snapshot.buttonSeat);
        }

        private static int CurrentPlayerId(Table table)
        {
            return table.Players.First(p => p.Seat == table.BuildSnapshot().currentSeat).Id;
        }

        [Test]
        public void DisconnectedPlayer_IsFoldedByTheTimer_AndKeepsSeatAndChips()
        {
            Table table = TableWith(3);
            table.StartHand();

            int currentId = CurrentPlayerId(table);
            int seat = table.GetPlayer(currentId).Seat;
            table.PlayerDisconnected(currentId);
            table.Tick(Table.TurnSeconds + 1f);

            SeatSnapshot snapshot = table.BuildSnapshot().seats.First(s => s.playerId == currentId);
            Assert.IsTrue(snapshot.folded);
            Assert.IsFalse(snapshot.connected);
            Assert.AreEqual(seat, snapshot.seat);
        }

        [Test]
        public void PlayerWhoReconnects_GetsTheSameSeatAndChipsBack()
        {
            Table table = TableWith(2);
            table.StartHand();
            table.Tick(Table.TurnSeconds + 1f); // someone folds, the hand ends

            TablePlayer player = table.GetPlayer(100);
            int seat = player.Seat;
            int chips = player.Stack;

            table.PlayerDisconnected(100);
            table.PlayerReconnected(100);

            Assert.IsTrue(table.GetPlayer(100).Connected);
            Assert.AreEqual(seat, table.GetPlayer(100).Seat);
            Assert.AreEqual(chips, table.GetPlayer(100).Stack);
        }

        [Test]
        public void PlayerWhoLeavesForGood_IsFoldedAtOnce_AndRemovedAfterTheHand()
        {
            Table table = TableWith(3);
            table.StartHand();

            int currentId = CurrentPlayerId(table);
            table.RemovePlayer(currentId);
            Assert.IsNotNull(table.GetPlayer(currentId)); // still seated until the hand is over

            table.Tick(0.1f); // no waiting for the timer

            Assert.IsTrue(table.BuildSnapshot().seats.First(s => s.playerId == currentId).folded);
        }

        [Test]
        public void PlayerWithNoChips_IsSpectating_AndCanRestartWithStartingChips()
        {
            Table table = TableWith(2);
            table.StartHand();
            table.Tick(Table.TurnSeconds + 1f); // hand over, now between hands

            TablePlayer player = table.GetPlayer(101);
            player.Stack = 0;

            Assert.IsTrue(table.BuildSnapshot().seats.First(s => s.playerId == 101).spectating);

            string error;
            Assert.IsTrue(table.Restart(101, out error));
            Assert.AreEqual(Table.StartingChips, player.Stack);
        }

        [Test]
        public void Restart_IsRefused_WhileThePlayerStillHasChips()
        {
            Table table = TableWith(2);

            string error;
            Assert.IsFalse(table.Restart(100, out error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void StartGame_WithOnePlayer_FillsTheTableWithBots()
        {
            Table table = TableWith(1);

            Assert.IsFalse(table.CanStart);
            Assert.IsTrue(table.CanStartGame);
            Assert.IsTrue(table.StartGame());

            Assert.AreEqual(3, table.Players.Count);
            Assert.AreEqual(2, table.Players.Count(p => p.IsBot));
        }

        [Test]
        public void Bots_PlayAHandOnTheirOwn()
        {
            Table table = TableWith(1);
            table.StartGame();

            // The human folds whenever it is their turn, the bots must finish the hand themselves.
            for (int i = 0; i < 500 && table.HandNumber == 1 && table.BuildSnapshot().handActive; i++)
            {
                TableSnapshot snapshot = table.BuildSnapshot();
                TablePlayer current = table.Players.First(p => p.Seat == snapshot.currentSeat);
                if (!current.IsBot)
                {
                    string error;
                    table.TryAct(current.Id, new PlayerAction(ActionType.Fold), out error);
                }

                table.Tick(1f);
            }

            Assert.IsFalse(table.BuildSnapshot().handActive);
        }

        [Test]
        public void FullTable_SendsABotAway_ToSeatANewPlayer()
        {
            Table table = new Table();
            table.AddPlayer(100, "Human");
            for (int i = 0; i < 5; i++)
            {
                table.AddBot(BotLevel.Cautious);
            }

            TablePlayer newPlayer = table.AddPlayer(101, "Newcomer");

            Assert.IsNotNull(newPlayer);
            Assert.AreEqual(4, table.Players.Count(p => p.IsBot));
        }

        // A random human against two bots for many hands, including going broke and restarting.
        [Test]
        public void Table_KeepsPlaying_WithBotsAndARandomHuman()
        {
            Table table = TableWith(1);
            table.StartGame();
            var random = new Random(11);

            for (int i = 0; i < 20000 && table.HandNumber < 40; i++)
            {
                TableSnapshot snapshot = table.BuildSnapshot();
                string error;

                if (snapshot.handActive)
                {
                    TablePlayer current = table.Players.First(p => p.Seat == snapshot.currentSeat);
                    if (!current.IsBot)
                    {
                        PlayerAction action = HandTests.RandomLegalAction(snapshot.options, random);
                        Assert.IsTrue(table.TryAct(current.Id, action, out error), action.Type + ": " + error);
                    }
                }
                else if (snapshot.seats.First(s => s.playerId == 100).spectating)
                {
                    Assert.IsTrue(table.Restart(100, out error), error);
                }

                table.Tick(1f);
            }

            Assert.GreaterOrEqual(table.HandNumber, 40);
        }

        [Test]
        public void HumanIdsInHand_LeavesOutBots()
        {
            Table table = TableWith(1);
            table.StartGame();

            Assert.AreEqual(1, table.HumanIdsInHand().Count);
        }
    }
}
