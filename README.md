# Multiplayer Texas Hold'em (sample)

A working prototype of multiplayer Texas Hold'em for 2 to 6 players, built in Unity with Photon PUN 2.
Everything is played with virtual chips. Each player starts with 1,000 chips. There is no real money,
no purchases and no loot boxes of any kind.

- **Playable build:** _add link here_
- **Video (full hand, 3 players):** _add link here_
- **Platform / layout:** desktop, mouse. The UI is laid out for a 16:9 screen (1280x720 reference).
  It is not designed for phones.

## Contents

1. [How to run](#how-to-run)
2. [How to test multiplayer](#how-to-test-multiplayer)
3. [How to play](#how-to-play)
4. [What is implemented](#what-is-implemented)
5. [Design decisions](#design-decisions)
6. [Edge cases](#edge-cases)
7. [Responsible design](#responsible-design)
8. [Automated tests](#automated-tests)
9. [Known issues and what was left out](#known-issues-and-what-was-left-out)

## How to run

**Requirements:** Unity **6000.3.22f1** (URP 2D template, new Input System). Photon PUN 2 is already in the project.

**In the editor**

1. Open the project folder in Unity and wait for the first import to finish.
2. Open `Assets/Scenes/Main.unity` and press Play.

**Standalone build**

1. `File > Build Profiles`, pick Windows (or your platform) and make sure `Main` is in the scene list.
2. Build and run.

**WebGL build**

1. `File > Build Profiles > Web`, add `Main`, then Build.
2. Upload the output folder to any static host (itch.io, GitHub Pages, ...). Photon connects over secure WebSocket.
3. Host the page over **https**, and prefer compression set to *Disabled* (or *Gzip* if your host sends the right headers).
4. The browser tab of the **table host** (the player who created the table) must stay in the foreground.
   Browsers slow down background tabs, which would stall the turn timer for everyone.

**Photon App ID.** The App ID of the author's Photon app is stored in
`Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`, so the game connects out of the box.
To use your own app, create a free **PUN** app on the Photon dashboard and paste its App ID there
(`Window > Photon Unity Networking > Highlight Server Settings`).

## How to test multiplayer

You need at least two running copies of the game. Any of these work:

- the editor **plus** a standalone build on the same computer,
- two standalone builds,
- a build on a second computer or device (any network, the traffic goes through Photon's cloud).

Steps:

1. In the first copy type a name and press **Create a new table**. A 4 letter table code appears at the top.
2. In the second copy either type the code and press **Join with code**, or click the table in the **Open tables** list.
3. The creator (the host) presses **Start hand**. After that, hands follow one another automatically.
4. Join with a third copy to see a three player hand.

**Playing alone:** create a table and press **Start hand** with nobody else there. One Cautious and one Bold bot
join so there is someone to play against. The host can also add or remove bots with the buttons next to
**Start hand**.

**Things worth trying**

| What | How |
|---|---|
| Hidden cards | Look at each window. Only your own two cards are face up. |
| Turn timer | Let your turn run out (30 seconds). You check if it is free, otherwise you fold. |
| Disconnect and return | In a build, switch off the network for about 20 seconds, switch it on and press **Reconnect**. |
| Leaving mid-hand | Close one copy during a hand. That player is folded at once. |
| Joining mid-hand | Join while a hand is running. You watch until the next hand. |
| Running out of chips | Lose all your chips. You become a spectator and get a **Restart with 1,000 chips** button. |
| Side pots | Let players with different chip counts go all-in. The result text and winnings show each pot. |

## How to play

- The table shows each player's name, chips, current bet and last action. A gold seat means it is that player's turn.
  The **D** marks the dealer. Your seat is always at the bottom.
- When it is your turn, the buttons that are not allowed are greyed out.
  **Fold**, **Check**, **Call N**, **Bet / Raise to**, **All-in**.
- For a raise, type the amount in the box or press **1/2 Pot** or **Pot** to fill in a pot sized raise.
  The hint under the box shows the smallest and largest amount you can use. Then press **Raise to**.
- When a hand ends, the winner's seat turns green and shows the amount won and the winning hand.
  Players who reached the showdown show their cards.
- Blinds are 10 and 20. The minimum bet is the big blind, and a raise must be at least as large as the previous one.

## What is implemented

| Area | Status |
|---|---|
| Create / join a table by code or from a list, 2 to 6 players, different devices | Done |
| Server-side shuffling, dealing and winner decision (see the note on trust below) | Done |
| Private hole cards, public state for everyone | Done |
| 30 second turn timer, auto check or fold | Done |
| Join and leave mid-hand, reconnect | Done |
| Blinds, moving dealer button, Pre-flop / Flop / Turn / River / Showdown | Done |
| Fold, Check, Call, Raise, All-in with disallowed actions disabled | Done |
| Hand ranking from Royal Flush to High Card, tied hands | Done |
| Side pots and pot distribution | Done |
| Quick raise buttons (half pot, pot) and custom amount within limits | Done |
| Call for more than your stack becomes all-in | Done |
| Computer opponents, two levels | Done |
| One retention feature (personal stats) and a break reminder with an off switch | Done |
| Automated tests for ranking, pots, betting and the table | Done |
| In-table chat, sounds, dealing animations | Not done |

## Design decisions

**Photon PUN 2 as the network layer.** It gives rooms, a lobby, reconnection and relayed events without running our
own server. The trade-off is that Photon's cloud only relays messages. It does not run our game code.

**The Master Client is the authority.** The player who created the table (Photon's "Master Client") runs the real
game: shuffling, dealing, rules, timers and bots. Everyone else, including the host's own UI, only sends *requests*
("I want to raise to 120") and draws what the host sends back. A request is rejected if it is not that player's turn,
is not allowed by the rules, or is not understood. The sender of every request comes from Photon, not from the message,
so a player cannot act as someone else. Clients also ignore table updates that do not come from the host.

**Hidden cards.** The public snapshot that goes to everyone never contains hole cards. Each player's two cards go out
in a separate event addressed to that player only. Cards of players who fold are never shown. At a showdown only the
players who reached it are revealed.

**Game logic is separate from UI and networking.** All rules live in `Assets/Scripts/Core`, a plain C# assembly with
no Unity or Photon references:

| File | Role |
|---|---|
| `Card`, `Deck` | Cards and the shuffled deck |
| `HandEvaluator`, `HandValue`, `HandRank` | Best five of seven cards and comparing hands |
| `PotCalculator`, `Pot`, `Contribution` | Main and side pots, splitting, uncalled bets |
| `BettingRound`, `BettingSeat`, `PlayerAction` | Whose turn it is and which actions are legal |
| `Hand` | One hand from blinds to payout |
| `Table`, `TablePlayer`, `TableSnapshot` | Seats, timer, bots, reconnecting, what clients may see |
| `BotBrain` | The computer opponents |
| `PlayerStats`, `SessionTimer` | The retention feature and the break timer |

`Assets/Scripts/Network` holds the Photon code (`PokerNetwork` for the lobby, `TableNetwork` for the table).
`Assets/Scripts/UI` holds the scripts that fill the scene's UI (`LobbyUI`, `TableView`, `SeatView`, ...).
All screens are real objects in the `Main` scene hierarchy (`Canvas > Background > ...`).

**Snapshots instead of many small messages.** After every change the host sends the whole public table state as one
small JSON message. It is simple, and a player who reconnects just waits for the next one.

**Odd chips.** When a pot cannot be split evenly, the whole remainder goes to the winner closest to the button
(first seat clockwise after it), as the task describes.

## Edge cases

| Case | What happens |
|---|---|
| Split pot | Pot divided equally. The remainder goes to the tied winner closest to the button. |
| Several all-ins for different amounts | Main pot and side pots are built per amount. Each goes to the best hand among the players eligible for it. A bet nobody could match goes back to the bettor. |
| Not enough chips to call | The call becomes an all-in for the rest of the chips. |
| Out of chips | The player becomes a spectator and can press **Restart with 1,000 chips**. Nothing is sold. |
| Disconnect mid-hand | Their turn timer folds them (or checks if free). The seat and chips are kept for **2 minutes**. Pressing **Reconnect** puts them back with their chips and cards. |
| Server connection lost | A full-screen message explains what happened and offers **Reconnect**. |
| Cheating attempts | Actions out of turn, illegal amounts, unknown actions, forged senders and fake table updates are rejected. |

**What happens to chips when the connection is lost.** Chips already bet in the running hand stay in the pot and are
played out normally. The disconnected player is folded when their turn comes, so those chips go to whoever wins.
Chips that were not bet stay with the player's seat for 2 minutes. If the player returns in time, they keep them.
If they do not, the seat is released and the chips are gone. Chips are virtual and are not saved between sessions.
If the **host** loses the connection, the table lives on their machine, so another player becomes host and the table
**restarts** with fresh 1,000 chip stacks (see known issues).

## Responsible design

- **Virtual chips only.** No purchases, no ads, no loot boxes. Running out of chips offers a free restart, never a shop.
- **One retention feature: personal stats.** Hands played, hands won, best hand shown and biggest pot won.
  They are kept on your own device (browser or PC) and only describe your play. They are not ranked, not shared,
  and nothing is unlocked or rewarded for reaching them.
- **No pressure mechanics.** No streaks, no daily rewards, no countdowns, no "you are about to lose" prompts.
- **Break reminder.** After about an hour of play at a table (time at the lobby does not count), a calm message
  suggests a short break. **OK** dismisses it. **Turn off reminders** switches it off for good, and the lobby has a
  **Break reminders: On / Off** button to change that later. The message never appears while it is your turn,
  so it cannot make you miss an action. The interval is the `Reminder Minutes` field on `GameManager > BreakReminder`.

## Automated tests

Open `Window > General > Test Runner`, choose **EditMode** and run all. The tests cover:

- hand ranking, kickers, the ace-low straight, ties and best five of seven,
- main and side pots, folded chips, uncalled bets, the odd chip rule,
- betting rules: minimum raise, short all-in not reopening betting, check and call rules,
- a full hand and all-in hands through every stage,
- the table: hidden cards in snapshots, wrong-turn actions, timeouts, reconnecting, leaving, restarting, bots,
- bot behaviour (legal actions only, never folding when checking is free, the two levels differing),
- stats and the break timer.

There are **no automated tests for Photon networking or the UI**. Those need the manual steps above.

## Known issues and what was left out

- **The host is trusted.** Photon only relays messages, so the game runs on the host's computer. A modified host
  could see the deck or change results. A real release would run the same `Core` code on a dedicated server
  (the `Core` assembly has no Unity dependency, so it could be moved as it is).
- **Host leaving restarts the table.** The table state is not stored anywhere else, so when the host drops for good,
  the new host starts a fresh table.
- **Reconnecting works within one session.** The 2 minute window relies on a player id created when the game starts.
  Closing and restarting the game gives a new identity.
- **Reconnecting is the least covered part.** It relies on Photon's rejoin feature and can only be exercised with a
  real network interruption, so it has no automated test.
- **WebGL and the shuffle.** The deck uses the system's secure random generator. On platforms where it is missing
  (WebGL may be one), it falls back to a normal random generator, which is fine for a sample but not for real stakes.
  The WebGL build has not been tested as thoroughly as the desktop one.
- **Layout.** Seat positions are fixed for a 16:9 screen. Very narrow or portrait windows will look cramped.
  The UI uses Unity's legacy `Text`, so the suit symbols depend on the default font.
- **Bots are simple.** They estimate hand strength and react to bets. They do not count outs, read opponents or
  think about position. The two levels are Cautious (tight, rarely raises) and Bold (loose, raises and bluffs).
- **Stats can be edited.** They are stored in the browser or PC settings and counted on the player's own device.
  That is acceptable for a personal record with no rewards attached.
- **Not done:** in-table chat, sound effects and dealing animations (the optional extras).
- **Photon free plan.** The free plan limits the number of players connected at once, which is plenty for an
  evaluation but not for a public launch.
