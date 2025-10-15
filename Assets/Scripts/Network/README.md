# Unity Network Module for LudoLoop

This module provides WebSocket connectivity to play LudoLoop multiplayer games with the Node.js server.

## 📦 Components

### Core Scripts

1. **LudoNetworkManager.cs**
   - Main WebSocket connection manager
   - Handles all client-server communication
   - Exposes Unity Events for game state changes
   - Thread-safe message processing

2. **NetworkGameBridge.cs**
   - Bridges network game state with OfflineLudoGame
   - Automatically syncs server state to local visualization
   - Provides helper methods for gameplay

3. **SimpleWebSocket.cs**
   - Cross-platform WebSocket wrapper
   - Works on WebGL and native platforms
   - Falls back to System.Net.WebSockets on desktop
   - Handles message queuing for Unity main thread

4. **NetworkMessages.cs**
   - All message type definitions
   - Serializable payload classes
   - Type-safe message handling

5. **NetworkGameUI.cs**
   - Ready-to-use UI controller
   - Handles connection, game creation, and gameplay
   - Message console for debugging

## 🚀 Quick Start

### Setup

1. **Add to Scene:**
   ```
   Create GameObject → Add Component → Ludo Network Manager
   ```

2. **Configure:**
   - Set Server URL (default: ws://localhost:8080)
   - Set Player Name
   - Enable Auto Connect (optional)

3. **Add Bridge (Optional):**
   ```
   Add Component → Network Game Bridge
   Assign OfflineLudoGame reference
   ```

4. **Add UI (Optional):**
   ```
   Create Canvas → Add Component → Network Game UI
   Assign Network Manager reference
   Configure UI elements in Inspector
   ```

### Programmatic Usage

```csharp
using Network.Runtime;

public class MyGameController : MonoBehaviour
{
    private LudoNetworkManager networkManager;
    
    void Start()
    {
        networkManager = GetComponent<LudoNetworkManager>();
        
        // Subscribe to events
        networkManager.OnConnected.AddListener(OnConnected);
        networkManager.OnGameCreated.AddListener(OnGameCreated);
        networkManager.OnDiceRolled.AddListener(OnDiceRolled);
        
        // Connect
        networkManager.Connect();
    }
    
    void OnConnected(string url)
    {
        Debug.Log("Connected!");
        networkManager.CreateGame(4, "My Player");
    }
    
    void OnGameCreated(GameCreatedPayload payload)
    {
        Debug.Log($"Game created: {payload.sessionId}");
        // Share session ID with other players
    }
    
    void OnDiceRolled(DiceRolledPayload payload)
    {
        if (payload.playerIndex == networkManager.PlayerIndex)
        {
            // It's our turn, we rolled
            if (payload.validMoves.Length > 0)
            {
                // Move first available token
                networkManager.MoveToken(payload.validMoves[0]);
            }
        }
    }
}
```

## 🎮 API Reference

### LudoNetworkManager

#### Properties
```csharp
bool IsConnected { get; }           // Is connected to server
string PlayerId { get; }            // Your player ID
string SessionId { get; }           // Current game session ID
int PlayerIndex { get; }            // Your player index (0-3)
bool IsMyTurn { get; }             // Is it your turn?
GameStatePayload CurrentGameState { get; } // Current game state
```

#### Methods

**Connection**
```csharp
void Connect()                      // Connect to server
void Disconnect()                   // Disconnect from server
```

**Game Management**
```csharp
void CreateGame(int maxPlayers = 4, string playerName = null)
void JoinGame(string sessionId, string playerName = null)
void StartGame()
void LeaveGame()
void ListGames()
```

**Gameplay**
```csharp
void RollDice(int diceValue = 0)   // Roll dice (0 = random)
void MoveToken(int tokenIndex)     // Move token (0-15)
void RefreshGameState()            // Request state update
```

#### Events

```csharp
UnityEvent<string> OnConnected
UnityEvent<GameCreatedPayload> OnGameCreated
UnityEvent<GameJoinedPayload> OnGameJoined
UnityEvent<PlayerJoinedPayload> OnPlayerJoined
UnityEvent<GameStartedPayload> OnGameStarted
UnityEvent<DiceRolledPayload> OnDiceRolled
UnityEvent<TokenMovedPayload> OnTokenMoved
UnityEvent<GameStatePayload> OnGameStateUpdated
UnityEvent<GameOverPayload> OnGameOver
UnityEvent<string> OnPlayerLeft
UnityEvent<string> OnError
UnityEvent OnDisconnected
```

### NetworkGameBridge

Automatically syncs network state to OfflineLudoGame for visualization.

```csharp
void RollDice()                    // Roll if it's your turn
void MoveToken(int tokenIndex)    // Move token if it's your turn
```

## 🎯 Usage Examples

### Example 1: Simple Bot Player

```csharp
public class BotPlayer : MonoBehaviour
{
    private LudoNetworkManager network;
    
    void Start()
    {
        network = GetComponent<LudoNetworkManager>();
        network.OnDiceRolled.AddListener(OnDiceRolled);
        network.Connect();
        StartCoroutine(AutoCreateGame());
    }
    
    IEnumerator AutoCreateGame()
    {
        yield return new WaitUntil(() => network.IsConnected);
        network.CreateGame(2, "Bot");
    }
    
    void OnDiceRolled(DiceRolledPayload payload)
    {
        if (payload.playerIndex == network.PlayerIndex && payload.validMoves.Length > 0)
        {
            // Auto-move first valid token after 1 second
            StartCoroutine(AutoMove(payload.validMoves[0]));
        }
    }
    
    IEnumerator AutoMove(int tokenIndex)
    {
        yield return new WaitForSeconds(1f);
        network.MoveToken(tokenIndex);
    }
}
```

### Example 2: Two Player Local Test

**Player 1 (Host):**
```csharp
void Start()
{
    network.OnGameCreated.AddListener(payload => {
        Debug.Log($"Share this: {payload.sessionId}");
        sessionIdText.text = payload.sessionId;
    });
    
    network.Connect();
}

void OnCreateButtonClicked()
{
    network.CreateGame(2, "Player 1");
}

void OnStartButtonClicked()
{
    network.StartGame();
}
```

**Player 2 (Join):**
```csharp
void OnJoinButtonClicked()
{
    string sessionId = sessionIdInput.text;
    network.Connect();
    network.OnConnected.AddListener(_ => {
        network.JoinGame(sessionId, "Player 2");
    });
}
```

### Example 3: Complete Game Flow

```csharp
public class GameFlowExample : MonoBehaviour
{
    private LudoNetworkManager network;
    private NetworkGameBridge bridge;
    
    void Start()
    {
        network = GetComponent<LudoNetworkManager>();
        bridge = GetComponent<NetworkGameBridge>();
        
        // Subscribe to all events
        network.OnConnected.AddListener(HandleConnected);
        network.OnGameStarted.AddListener(HandleGameStarted);
        network.OnDiceRolled.AddListener(HandleDiceRolled);
        network.OnTokenMoved.AddListener(HandleTokenMoved);
        network.OnGameOver.AddListener(HandleGameOver);
        
        network.Connect();
    }
    
    void HandleConnected(string url)
    {
        Debug.Log("Connected! Creating game...");
        network.CreateGame(4, "Unity Player");
    }
    
    void HandleGameStarted(GameStartedPayload payload)
    {
        Debug.Log($"Game started! {payload.playerCount} players");
        
        if (payload.currentPlayer == network.PlayerIndex)
        {
            RollDiceWhenReady();
        }
    }
    
    void HandleDiceRolled(DiceRolledPayload payload)
    {
        if (payload.playerIndex == network.PlayerIndex)
        {
            if (payload.validMoves.Length > 0)
            {
                // Let player choose or auto-select
                SelectAndMoveToken(payload.validMoves);
            }
        }
    }
    
    void HandleTokenMoved(TokenMovedPayload payload)
    {
        Debug.Log($"Token moved: {payload.message}");
        
        // Check if it's our turn again
        if (network.IsMyTurn && !payload.hasWon)
        {
            RollDiceWhenReady();
        }
    }
    
    void HandleGameOver(GameOverPayload payload)
    {
        Debug.Log($"Winner: {payload.winnerName}");
        
        if (payload.winnerIndex == network.PlayerIndex)
        {
            Debug.Log("You won! 🎉");
        }
    }
    
    void RollDiceWhenReady()
    {
        // Add delay for realism
        StartCoroutine(DelayedRoll());
    }
    
    IEnumerator DelayedRoll()
    {
        yield return new WaitForSeconds(1f);
        bridge.RollDice();
    }
    
    void SelectAndMoveToken(int[] validMoves)
    {
        // Simple: move first valid token
        // Advanced: Let player choose via UI
        StartCoroutine(DelayedMove(validMoves[0]));
    }
    
    IEnumerator DelayedMove(int tokenIndex)
    {
        yield return new WaitForSeconds(0.5f);
        bridge.MoveToken(tokenIndex);
    }
}
```

## 🔧 Platform Support

### Desktop (Windows/Mac/Linux)
✅ Fully supported using System.Net.WebSockets

### WebGL
⚠️ Requires JavaScript bridge (see WebGL section below)

### Mobile (iOS/Android)
✅ Supported via System.Net.WebSockets

### Alternative: Use Third-Party WebSocket Libraries

For production use, consider:

1. **NativeWebSocket** (Recommended)
   - https://github.com/endel/NativeWebSocket
   - Works on all platforms including WebGL
   - Simple API

2. **WebSocketSharp**
   - https://github.com/sta/websocket-sharp
   - Desktop/mobile only
   - More features

Replace `SimpleWebSocket` with your chosen library.

## 🌐 WebGL Support

For WebGL builds, add this JavaScript to your index.html:

```javascript
// In your index.html, before Unity loader
<script>
var websocketInstances = {};
var websocketIdCounter = 0;

function WebSocketConnect(url) {
    var ws = new WebSocket(url);
    var id = websocketIdCounter++;
    websocketInstances[id] = ws;
    
    ws.onopen = function() {
        gameInstance.SendMessage('NetworkManager', 'OnWebSocketOpen');
    };
    
    ws.onmessage = function(event) {
        gameInstance.SendMessage('NetworkManager', 'OnWebSocketMessage', event.data);
    };
    
    ws.onerror = function(error) {
        gameInstance.SendMessage('NetworkManager', 'OnWebSocketError', error.toString());
    };
    
    ws.onclose = function() {
        gameInstance.SendMessage('NetworkManager', 'OnWebSocketClose');
    };
    
    return id;
}

function WebSocketSend(id, message) {
    if (websocketInstances[id]) {
        websocketInstances[id].send(message);
    }
}

function WebSocketClose(id) {
    if (websocketInstances[id]) {
        websocketInstances[id].close();
        delete websocketInstances[id];
    }
}
</script>
```

## 🐛 Troubleshooting

**Can't connect to server:**
- Check server is running (`cd server && npm start`)
- Verify server URL (ws://localhost:8080)
- Check firewall settings

**Messages not received:**
- Ensure `Update()` is being called (LudoNetworkManager processes messages there)
- Check console for WebSocket errors

**State not syncing:**
- Verify NetworkGameBridge has OfflineLudoGame reference
- Check "Sync To Local Game" is enabled

**WebGL not working:**
- Add JavaScript bridge to index.html
- Use NativeWebSocket library instead

## 📝 Notes

- Token indices: Player 0 (0-3), Player 1 (4-7), Player 2 (8-11), Player 3 (12-15)
- Always check `IsMyTurn` before rolling dice or moving tokens
- Use events instead of polling for better performance
- The bridge automatically syncs visualization with network state

## 🎓 Advanced: Custom WebSocket Implementation

To use your own WebSocket library, replace `SimpleWebSocket`:

```csharp
// In LudoNetworkManager.cs, replace:
private SimpleWebSocket webSocket;

// With your implementation:
private YourWebSocketClass webSocket;
```

Ensure it implements:
- `Connect(string url)` method
- `Send(string message)` method
- `Close()` method
- `OnOpen`, `OnMessage`, `OnError`, `OnClose` events
- `ProcessMessages()` for thread-safe Unity integration
