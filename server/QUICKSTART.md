# Ludo Game Server - Quick Start Guide

## 🎯 What's This?

A complete, production-ready WebSocket server for Unity Ludo multiplayer games with automatic matchmaking, full game logic, and player reconnection.

## 🚀 Getting Started

### 1. Install Dependencies
```bash
cd server
npm install
```

### 2. Start Server
```bash
npm start
```

Server runs on **ws://localhost:8080**

### 3. Test It Works
```bash
node test-game.js
```

You should see all tests pass ✅

## 🎮 How It Works

### Game Flow
```
1. Players connect → Receive unique Player ID
2. Join matchmaking queue → Wait for other players
3. Match found → Game starts automatically
4. Take turns → Roll dice, move tokens
5. First player to get all 4 tokens home wins!
```

### For Unity Developers

**Step 1: Connect**
```csharp
WebSocket ws = new WebSocket("ws://yourserver:8080");
ws.OnMessage += HandleMessage;
ws.Connect();
```

**Step 2: Join Queue**
```json
{
  "type": "join_queue",
  "payload": {
    "playerName": "Player1",
    "roomType": "casual",
    "playerCount": 4
  }
}
```

**Step 3: Wait for Match**
```json
// Server sends when game starts:
{
  "type": "match_found",
  "payload": {
    "sessionId": "game-id",
    "gameState": { ... },
    "players": [ ... ]
  }
}
```

**Step 4: Play Game**
```json
// On your turn, roll dice:
{"type": "roll_dice", "payload": {}}

// Then move a token:
{"type": "move_token", "payload": {"tokenIndex": 0}}
```

See **API.md** for complete documentation!

## 📁 Files

| File | Purpose |
|------|---------|
| `server.js` | Main server, matchmaking, WebSocket handling |
| `gameSession.js` | Game logic, token movement, rules |
| `API.md` | Complete API documentation |
| `README.md` | Detailed information |
| `QUICKSTART.md` | This file |
| `test-game.js` | Automated tests |
| `test-client.html` | Browser test client |

## 🎲 Game Rules Summary

- **2-4 players**, each with 4 tokens
- Roll **6 to exit** base
- **Safe tiles**: 0, 13, 26, 39
- **Evict** opponents by landing on them
- **Roll again** on: 6, eviction, or reaching home
- **Third 6 in a row**: Turn ends automatically
- **First to get all 4 tokens home wins!**

## 🔧 Configuration

Change port with environment variable:
```bash
PORT=3000 npm start
```

## 🐛 Common Issues

**"You are already in a game"**
→ Leave current game first with `leave_game`

**"Not your turn"**
→ Wait for `currentPlayer` to match your `playerIndex`

**"Invalid move"**
→ Only move tokens in the `validMoves` array

## 📚 Next Steps

1. Read **API.md** for complete WebSocket API
2. Check **README.md** for architecture details
3. Open **test-client.html** in browser to test manually
4. Integrate with your Unity project

## 💡 Quick Unity Example

```csharp
public class LudoGameClient : MonoBehaviour
{
    WebSocket ws;
    string playerId;
    
    void Start() {
        ws = new WebSocket("ws://localhost:8080");
        ws.OnMessage += OnMessage;
        ws.Connect();
    }
    
    void OnMessage(object sender, MessageEventArgs e) {
        var msg = JsonUtility.FromJson<Message>(e.Data);
        
        switch(msg.type) {
            case "connected":
                playerId = msg.payload.playerId;
                JoinQueue();
                break;
            case "match_found":
                StartGame(msg.payload);
                break;
            case "dice_rolled":
                ShowDice(msg.payload.diceValue);
                break;
            // ... handle other messages
        }
    }
    
    void JoinQueue() {
        Send("join_queue", new {
            playerName = "UnityPlayer",
            roomType = "casual",
            playerCount = 4
        });
    }
    
    public void RollDice() {
        Send("roll_dice", new {});
    }
    
    public void MoveToken(int index) {
        Send("move_token", new { tokenIndex = index });
    }
    
    void Send(string type, object payload) {
        var msg = new { type, payload };
        ws.Send(JsonUtility.ToJson(msg));
    }
}
```

## 🎉 That's It!

You now have a fully functional Ludo game server. Happy coding! 🚀

For detailed API documentation, see **API.md**
