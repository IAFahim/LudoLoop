# Unity C# Client for Ludo Game Server

Complete Unity integration for the automatic matchmaking Ludo game server.

## Features

✅ **Automatic Matchmaking** - Just call `FindMatch()` and get matched with other players  
✅ **Flexible Team Sizes** - Matches 2-4 players automatically after 10 seconds  
✅ **Full Game Logic** - Complete Ludo game implementation  
✅ **Event-Driven** - Easy to integrate with Unity UI  
✅ **Reconnection Support** - Handle disconnects gracefully  

## Quick Start

### 1. Install WebSocket Library

Install the `NativeWebSocket` package in Unity:

```
Window → Package Manager → Add package from git URL:
https://github.com/endel/NativeWebSocket.git#upm
```

### 2. Add Scripts to Unity

Copy these files to your Unity project's `Assets/Scripts/` folder:
- `LudoClient.cs` - Main client script
- `LudoGameUI.cs` - Example UI controller (optional)

### 3. Setup Scene

1. Create an empty GameObject named "LudoClient"
2. Attach the `LudoClient` script to it
3. Configure the server URL (default: `ws://localhost:8080`)
4. Set your player name

### 4. Start Playing

```csharp
// Get reference to client
LudoClient client = FindObjectOfType<LudoClient>();

// Connect to server
client.Connect();

// Find a match (automatic matchmaking!)
client.FindMatch();

// That's it! The server will match you with other players
```

## API Reference

### Connection Methods

```csharp
client.Connect();           // Connect to server
client.Disconnect();        // Disconnect from server
bool IsConnected();         // Check connection status
```

### Matchmaking Methods

```csharp
client.FindMatch();         // Join matchmaking queue
client.LeaveQueue();        // Leave matchmaking queue
bool IsInQueue();           // Check if in queue
bool IsInGame();            // Check if in active game
```

### Game Methods

```csharp
client.RollDice();          // Roll dice (must be your turn)
client.MoveToken(int tokenIndex);  // Move a token (0-15)
client.GetGameState();      // Request current game state
client.LeaveGame();         // Leave current game
bool IsMyTurn();            // Check if it's your turn
```

### Getters

```csharp
string GetPlayerId();       // Your player ID
string GetSessionId();      // Current game session ID
int GetPlayerIndex();       // Your player index (0-3)
GameStateData GetCurrentGameState();  // Current game state
```

## Events

Subscribe to events for game updates:

```csharp
void Start()
{
    client.OnConnected += (playerId) => {
        Debug.Log($"Connected! ID: {playerId}");
    };

    client.OnMatchFound += (matchData) => {
        Debug.Log($"Match found! {matchData.playerCount} players");
        Debug.Log($"You are player {matchData.myPlayerIndex}");
    };

    client.OnDiceRolled += (data) => {
        Debug.Log($"Player {data.playerIndex} rolled {data.diceValue}");
        // Update UI to show dice result
    };

    client.OnTokenMoved += (data) => {
        Debug.Log($"Token {data.tokenIndex} moved to position {data.newPosition}");
        // Update board visualization
    };

    client.OnGameOver += (data) => {
        Debug.Log($"Winner: {data.winnerName}!");
        // Show victory screen
    };

    client.OnError += (error) => {
        Debug.LogError($"Error: {error}");
    };
}
```

### Available Events

- `OnConnected(string playerId)` - Connected to server
- `OnDisconnected()` - Disconnected from server
- `OnError(string error)` - Error occurred
- `OnQueueJoined(int playersInQueue)` - Joined matchmaking queue
- `OnMatchFound(MatchData matchData)` - Match found, game starting
- `OnDiceRolled(DiceRollData data)` - Dice rolled by any player
- `OnTokenMoved(TokenMoveData data)` - Token moved by any player
- `OnGameOver(GameOverData data)` - Game finished
- `OnPlayerDisconnected(string playerId)` - Player disconnected
- `OnPlayerReconnected(string playerId)` - Player reconnected

## Game State

The `GameStateData` object contains:

```csharp
public class GameStateData
{
    public int turnCount;           // Current turn number
    public int diceValue;           // Last dice roll (0 if not rolled)
    public int consecutiveSixes;    // Consecutive sixes rolled
    public int currentPlayer;       // Current player's turn (0-3)
    public int playerCount;         // Number of players (2-4)
    public int[] tokenPositions;    // All token positions (16 total)
}
```

### Token Positions

- `-1` = Token in base (not started)
- `0-51` = Token on main path
- `100-105` = Player 0's home stretch
- `106-111` = Player 1's home stretch
- `112-117` = Player 2's home stretch
- `118-123` = Player 3's home stretch
- `57` = Token finished (in home)

### Token Indexing

Tokens are indexed 0-15:
- Tokens 0-3: Player 0 (Red)
- Tokens 4-7: Player 1 (Blue)
- Tokens 8-11: Player 2 (Green)
- Tokens 12-15: Player 3 (Yellow)

## Example: Complete Game Flow

```csharp
using UnityEngine;

public class MyLudoGame : MonoBehaviour
{
    [SerializeField] private LudoClient client;
    
    void Start()
    {
        // Setup events
        client.OnConnected += OnConnected;
        client.OnMatchFound += OnMatchFound;
        client.OnDiceRolled += OnDiceRolled;
        client.OnGameOver += OnGameOver;
        
        // Connect and find match
        client.Connect();
    }
    
    void OnConnected(string playerId)
    {
        Debug.Log("Connected! Finding match...");
        client.FindMatch();
    }
    
    void OnMatchFound(MatchData match)
    {
        Debug.Log($"Game starting with {match.playerCount} players!");
        Debug.Log($"I am player {match.myPlayerIndex}");
    }
    
    void OnDiceRolled(DiceRollData data)
    {
        if (data.playerIndex == client.GetPlayerIndex())
        {
            // My turn! I rolled the dice
            if (!data.noValidMoves)
            {
                // Move first available token
                int myFirstToken = client.GetPlayerIndex() * 4;
                client.MoveToken(myFirstToken);
            }
        }
    }
    
    void OnGameOver(GameOverData data)
    {
        if (data.winnerIndex == client.GetPlayerIndex())
        {
            Debug.Log("I WON! 🎉");
        }
        else
        {
            Debug.Log($"Player {data.winnerIndex} won");
        }
    }
    
    void Update()
    {
        // Auto-roll dice on my turn
        if (client.IsMyTurn() && Input.GetKeyDown(KeyCode.Space))
        {
            client.RollDice();
        }
    }
}
```

## Server Configuration

The server now features:

### Automatic Matchmaking
- Players join a single queue
- No need to specify team size
- Matches 4 players instantly if available
- After 10 seconds, matches with any available players (2-4)

### Smart Team Sizing
- 4+ players in queue → Instant 4-player match
- 2-3 players waiting 10+ seconds → Match with available players
- Flexible team sizes (2v2, 3-player, or 4-player)

## Troubleshooting

### WebSocket Connection Issues

If you get connection errors:
1. Make sure the server is running (`node server.js`)
2. Check the server URL is correct
3. Try `ws://localhost:8080` for local testing

### Build Issues

For WebGL builds, add this to your code:
```csharp
#if !UNITY_WEBGL || UNITY_EDITOR
    websocket.DispatchMessageQueue();
#endif
```

This is already included in `LudoClient.cs`.

## License

MIT License - Free to use in your projects!
