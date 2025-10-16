# Ludo Game Server - WebSocket API Documentation

## Connection

Connect to the WebSocket server:
```
ws://localhost:8080
```

Upon connection, you will receive:
```json
{
  "type": "connected",
  "payload": {
    "message": "Connected to Ludo Game Server",
    "playerId": "uuid-string"
  }
}
```

**Save the `playerId` - you'll need it for all subsequent requests!**

---

## Message Format

All messages follow this structure:

```json
{
  "type": "message_type",
  "payload": { /* message-specific data */ }
}
```

---

## Client → Server Messages

### 1. Join Matchmaking Queue

Join a queue to find a match with other players.

**Request:**
```json
{
  "type": "join_queue",
  "payload": {
    "playerName": "YourName",
    "roomType": "casual",
    "playerCount": 4
  }
}
```

**Parameters:**
- `playerName` (string): Your display name
- `roomType` (string): Room type (e.g., "casual", "ranked", "100coins")
- `playerCount` (number): 2, 3, or 4 players

**Response (broadcast to all in queue):**
```json
{
  "type": "queue_update",
  "payload": {
    "queueKey": "casual_4",
    "roomType": "casual",
    "currentPlayers": 2,
    "neededPlayers": 4
  }
}
```

**When queue is full:**
```json
{
  "type": "match_found",
  "payload": {
    "sessionId": "uuid",
    "playerCount": 4,
    "roomType": "casual",
    "gameState": {
      "turnCount": 0,
      "diceValue": 0,
      "consecutiveSixes": 0,
      "currentPlayer": 0,
      "playerCount": 4,
      "tokenPositions": [-1, -1, ..., -1]
    },
    "players": [
      {
        "playerId": "uuid",
        "name": "Player1",
        "playerIndex": 0
      },
      ...
    ]
  }
}
```

---

### 2. Leave Queue

Leave the matchmaking queue before a game starts.

**Request:**
```json
{
  "type": "leave_queue",
  "payload": {}
}
```

**Response:**
```json
{
  "type": "left_queue",
  "payload": {
    "success": true
  }
}
```

---

### 3. Roll Dice

Roll the dice on your turn.

**Request:**
```json
{
  "type": "roll_dice",
  "payload": {
    "forcedValue": 0
  }
}
```

**Parameters:**
- `forcedValue` (optional, number): Force a specific dice value 1-6 (for testing). Use 0 or omit for random.

**Response (broadcast to all players):**
```json
{
  "type": "dice_rolled",
  "payload": {
    "success": true,
    "playerId": "uuid",
    "playerIndex": 0,
    "diceValue": 4,
    "validMoves": [0, 1, 2],
    "noValidMoves": false,
    "turnSwitched": false,
    "nextPlayer": 0
  }
}
```

**Fields:**
- `validMoves`: Array of token indices (0-15) that can be moved
- `noValidMoves`: If true, turn automatically passes
- `turnSwitched`: If true, it's now another player's turn

---

### 4. Move Token

Move a token after rolling the dice.

**Request:**
```json
{
  "type": "move_token",
  "payload": {
    "tokenIndex": 0
  }
}
```

**Parameters:**
- `tokenIndex` (number): 0-15
  - Player 0 (Red): tokens 0-3
  - Player 1 (Blue): tokens 4-7
  - Player 2 (Green): tokens 8-11
  - Player 3 (Yellow): tokens 12-15

**Response (broadcast to all players):**
```json
{
  "type": "token_moved",
  "payload": {
    "success": true,
    "playerId": "uuid",
    "playerIndex": 0,
    "tokenIndex": 0,
    "moveResult": "Success",
    "moveResultCode": 0,
    "diceValue": 4,
    "newPosition": 4,
    "hasWon": false,
    "turnSwitched": true,
    "nextPlayer": 1,
    "gameState": { /* updated game state */ }
  }
}
```

**Move Results:**
- `Success` (0): Normal move
- `SuccessRollAgain` (1): Token reached home
- `SuccessSix` (2): Rolled a 6
- `SuccessEvictedOpponent` (3): Sent opponent back
- `SuccessThirdSixPenalty` (4): Third consecutive 6

**When a player wins:**
```json
{
  "type": "game_over",
  "payload": {
    "winnerId": "uuid",
    "winnerIndex": 0,
    "winnerName": "Player1"
  }
}
```

---

### 5. Get Game State

Request the current game state.

**Request:**
```json
{
  "type": "get_state",
  "payload": {}
}
```

**Response:**
```json
{
  "type": "game_state",
  "payload": {
    "success": true,
    "sessionId": "uuid",
    "playerIndex": 0,
    "playerCount": 4,
    "currentPlayer": 0,
    "gameState": {
      "turnCount": 5,
      "diceValue": 0,
      "consecutiveSixes": 0,
      "currentPlayer": 0,
      "playerCount": 4,
      "tokenPositions": [-1, 0, 13, 26, ...]
    },
    "players": [
      {
        "playerId": "uuid",
        "name": "Player1",
        "playerIndex": 0,
        "connected": true,
        "isAFK": false
      },
      ...
    ],
    "isGameOver": false,
    "winnerId": null
  }
}
```

---

### 6. Leave Game

Leave the current game.

**Request:**
```json
{
  "type": "leave_game",
  "payload": {}
}
```

**Response:**
```json
{
  "type": "left_game",
  "payload": {
    "success": true
  }
}
```

**Broadcast to other players:**
```json
{
  "type": "player_left",
  "payload": {
    "playerId": "uuid",
    "playerName": "Player1"
  }
}
```

---

### 7. Reconnect

Reconnect to a game after disconnection.

**Request:**
```json
{
  "type": "reconnect",
  "payload": {}
}
```

**Response:**
```json
{
  "type": "reconnected",
  "payload": {
    "success": true,
    "sessionId": "uuid",
    "playerIndex": 0,
    "gameState": { /* current game state */ },
    "players": [ /* current players */ ]
  }
}
```

**Broadcast to other players:**
```json
{
  "type": "player_reconnected",
  "payload": {
    "playerId": "uuid"
  }
}
```

---

## Server → Client Messages

### Error Message

Sent when an error occurs:

```json
{
  "type": "error",
  "payload": {
    "error": "Error message"
  }
}
```

### Player Disconnected

Sent when a player disconnects:

```json
{
  "type": "player_disconnected",
  "payload": {
    "playerId": "uuid"
  }
}
```

---

## Game State Reference

### Token Positions

Each token can be in one of these positions:

- `-1`: In base (not yet on board)
- `0-51`: On main path
- `100-105`, `106-111`, `112-117`, `118-123`: Home stretch (for players 0-3)
- `57`: Finished (home)

### Player Index to Color

- `0`: Red
- `1`: Blue
- `2`: Green
- `3`: Yellow

### Start Positions

- Red: 0
- Blue: 13
- Green: 26
- Yellow: 39

### Safe Tiles

Tiles 0, 13, 26, 39 are safe (no eviction)

---

## Unity Integration Example (C#)

```csharp
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class LudoClient : MonoBehaviour
{
    private WebSocket ws;
    private string playerId;
    
    void Start()
    {
        ws = new WebSocket("ws://localhost:8080");
        
        ws.OnMessage += (sender, e) =>
        {
            var data = JObject.Parse(e.Data);
            string type = data["type"].ToString();
            var payload = data["payload"];
            
            switch(type)
            {
                case "connected":
                    playerId = payload["playerId"].ToString();
                    Debug.Log("Connected with ID: " + playerId);
                    JoinQueue();
                    break;
                    
                case "match_found":
                    HandleMatchFound(payload);
                    break;
                    
                case "dice_rolled":
                    HandleDiceRolled(payload);
                    break;
                    
                case "token_moved":
                    HandleTokenMoved(payload);
                    break;
                    
                case "game_over":
                    HandleGameOver(payload);
                    break;
                    
                case "error":
                    Debug.LogError("Server error: " + payload["error"]);
                    break;
            }
        };
        
        ws.Connect();
    }
    
    void JoinQueue()
    {
        var msg = new
        {
            type = "join_queue",
            payload = new
            {
                playerName = "UnityPlayer",
                roomType = "casual",
                playerCount = 4
            }
        };
        ws.Send(JsonConvert.SerializeObject(msg));
    }
    
    public void RollDice()
    {
        var msg = new
        {
            type = "roll_dice",
            payload = new { }
        };
        ws.Send(JsonConvert.SerializeObject(msg));
    }
    
    public void MoveToken(int tokenIndex)
    {
        var msg = new
        {
            type = "move_token",
            payload = new { tokenIndex }
        };
        ws.Send(JsonConvert.SerializeObject(msg));
    }
    
    void HandleMatchFound(JToken payload)
    {
        Debug.Log("Match found! Starting game...");
        // Initialize game board with payload data
    }
    
    void HandleDiceRolled(JToken payload)
    {
        int diceValue = payload["diceValue"].ToObject<int>();
        var validMoves = payload["validMoves"].ToObject<int[]>();
        Debug.Log($"Dice: {diceValue}, Valid moves: {string.Join(",", validMoves)}");
    }
    
    void HandleTokenMoved(JToken payload)
    {
        // Update game board
    }
    
    void HandleGameOver(JToken payload)
    {
        string winnerName = payload["winnerName"].ToString();
        Debug.Log($"{winnerName} wins!");
    }
    
    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}
```

---

## Testing with wscat

Install wscat:
```bash
npm install -g wscat
```

Connect and test:
```bash
wscat -c ws://localhost:8080

# After connection, send:
{"type":"join_queue","payload":{"playerName":"Test","roomType":"casual","playerCount":2}}

# On your turn:
{"type":"roll_dice","payload":{}}
{"type":"move_token","payload":{"tokenIndex":0}}
```

---

## Error Messages

Common error messages:

- `"Invalid message format"` - JSON parsing failed
- `"Unknown message type: X"` - Invalid message type
- `"Missing required fields: X"` - Required parameters missing
- `"Player count must be between 2 and 4"` - Invalid playerCount
- `"You are already in a game"` - Cannot join queue while in game
- `"You are already in a queue"` - Already waiting for match
- `"You are not in a queue"` - Cannot leave queue (not in one)
- `"You are not in a game"` - Not in an active game
- `"Not your turn"` - Action attempted when not your turn
- `"You must roll the dice first"` - Tried to move without rolling
- `"Game is over"` - Tried to play after game ended
- `"Invalid move: X"` - Move validation failed

---

## Server Configuration

Environment variables:

- `PORT` - Server port (default: 8080)

Server automatically cleans up:
- Finished games after 30 seconds
- Inactive games after 30 minutes
- Disconnected games immediately
