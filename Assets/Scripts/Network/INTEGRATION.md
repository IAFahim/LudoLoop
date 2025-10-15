# Unity WebSocket Integration Guide

Complete guide to integrate the LudoLoop Unity game with the multiplayer WebSocket server.

## 📋 Prerequisites

1. Unity 2020.3 or later
2. Node.js server running (see `/server` folder)
3. TextMeshPro package (for UI)

## 🚀 Quick Setup (1 Minute!) - AUTOMATIC

### One-Click Setup (Recommended)

The easiest way to get started:

```
Unity Menu → Tools → Ludo Network → Quick Setup - Everything
```

This automatically creates:
- ✅ Network Manager with all settings
- ✅ Network Game Bridge (auto-wired to OfflineLudoGame)
- ✅ Complete UI with all buttons and controls
- ✅ All references automatically connected

**That's it!** Now just:
1. Start server: `cd server && npm start`
2. Press Play in Unity
3. Click Connect → Create Game → Start!

### Alternative: GameObject Menu

```
Unity Menu → GameObject → UI → Ludo Network UI (Complete)
```

Same as Quick Setup - Everything.

### Manual Setup (Not Recommended)

If you prefer manual setup:

1. **Start the Server**
   ```bash
   cd server
   npm install
   npm start
   ```

2. **Add Network Manager to Scene**
   - Create Empty GameObject
   - Add component: `Ludo Network Manager`
   - Configure Server URL: `ws://localhost:8080`

3. **Add Game Bridge**
   - Add component: `Network Game Bridge`
   - Assign OfflineLudoGame reference

4. **Create UI** (or use the automatic setup above!)

**See [Editor/UI_SETUP.md](Editor/UI_SETUP.md) for complete UI setup documentation.**

## 🎮 Integration Methods

### Method 1: Using NetworkGameExample (Easiest)

Perfect for quick testing and learning.

```csharp
// Add to a GameObject in your scene
1. Create GameObject
2. Add Component: NetworkGameExample
3. Configure settings in Inspector
4. Press Play
```

The example script will:
- Auto-setup all components
- Connect to server
- Create game (if configured)
- Log all events to Console

### Method 2: Custom Integration (Recommended)

For production games with custom UI and logic.

```csharp
using Network.Runtime;
using LudoGame.Runtime;

public class MyLudoController : MonoBehaviour
{
    [SerializeField] private LudoNetworkManager network;
    [SerializeField] private NetworkGameBridge bridge;
    
    void Start()
    {
        // Subscribe to events
        network.OnGameStarted.AddListener(OnGameStarted);
        network.OnDiceRolled.AddListener(OnDiceRolled);
        
        // Connect
        network.Connect();
    }
    
    void OnGameStarted(GameStartedPayload payload)
    {
        // Game started, show UI, etc.
        UpdateUI();
    }
    
    void OnDiceRolled(DiceRolledPayload payload)
    {
        if (payload.playerIndex == network.PlayerIndex)
        {
            // Your turn, show valid moves
            ShowValidMoves(payload.validMoves);
        }
    }
    
    public void OnPlayerClicksRollDice()
    {
        bridge.RollDice(); // Safe, checks if it's your turn
    }
    
    public void OnPlayerClicksToken(int tokenIndex)
    {
        bridge.MoveToken(tokenIndex);
    }
}
```

### Method 3: With NetworkGameUI (Full UI)

Complete UI solution with buttons and console.

1. Create Canvas: `GameObject > UI > Canvas`
2. Add Script: `NetworkGameUI` to Canvas
3. Assign references in Inspector
4. Create UI elements (see UI Setup below)

## 🎨 UI Setup

### Creating the UI

```
Canvas
├── Connection Panel
│   ├── Server URL Input
│   ├── Connect Button
│   └── Status Text
├── Game Setup Panel
│   ├── Player Name Input
│   ├── Session ID Input
│   ├── Create Game Button
│   ├── Join Game Button
│   └── Start Game Button
├── Gameplay Panel
│   ├── Roll Dice Button
│   ├── Token Index Input
│   ├── Move Token Button
│   └── Info Text
└── Messages Panel
    └── Messages ScrollView
        └── Messages Text
```

### Assigning UI References

In `NetworkGameUI` Inspector:
- Drag each UI element to its corresponding field
- Assign Network Manager reference
- Assign Network Game Bridge reference

## 🔄 Game Flow

### Complete Flow Example

```csharp
public class CompleteLudoFlow : MonoBehaviour
{
    private LudoNetworkManager network;
    private NetworkGameBridge bridge;
    
    void Start()
    {
        network = GetComponent<LudoNetworkManager>();
        bridge = GetComponent<NetworkGameBridge>();
        
        SetupEvents();
        network.Connect();
    }
    
    void SetupEvents()
    {
        network.OnConnected.AddListener(url => {
            Debug.Log("Connected! Creating game...");
            network.CreateGame(4, "Player 1");
        });
        
        network.OnGameCreated.AddListener(payload => {
            Debug.Log($"Game created: {payload.sessionId}");
            DisplaySessionIdForSharing(payload.sessionId);
        });
        
        network.OnPlayerJoined.AddListener(payload => {
            Debug.Log($"{payload.playerName} joined!");
            UpdatePlayerList();
        });
        
        network.OnGameStarted.AddListener(payload => {
            Debug.Log("Game started!");
            if (network.IsMyTurn) {
                EnableRollDiceButton();
            }
        });
        
        network.OnDiceRolled.AddListener(payload => {
            if (payload.playerIndex == network.PlayerIndex) {
                if (payload.validMoves.Length > 0) {
                    ShowTokenSelection(payload.validMoves);
                } else {
                    ShowMessage("No valid moves!");
                }
            }
        });
        
        network.OnTokenMoved.AddListener(payload => {
            PlayMoveAnimation(payload.tokenIndex, payload.newPosition);
            
            if (payload.hasWon) {
                if (payload.playerIndex == network.PlayerIndex) {
                    ShowVictoryScreen();
                } else {
                    ShowDefeatScreen(payload.winnerName);
                }
            } else if (network.IsMyTurn) {
                EnableRollDiceButton();
            }
        });
    }
    
    // UI Callbacks
    public void OnRollDiceButtonClicked()
    {
        bridge.RollDice();
        DisableRollDiceButton();
    }
    
    public void OnTokenClicked(int tokenIndex)
    {
        bridge.MoveToken(tokenIndex);
        HideTokenSelection();
    }
    
    // Helper methods (implement these based on your UI)
    void DisplaySessionIdForSharing(string sessionId) { }
    void UpdatePlayerList() { }
    void EnableRollDiceButton() { }
    void DisableRollDiceButton() { }
    void ShowTokenSelection(int[] validMoves) { }
    void HideTokenSelection() { }
    void ShowMessage(string msg) { }
    void PlayMoveAnimation(int token, int pos) { }
    void ShowVictoryScreen() { }
    void ShowDefeatScreen(string winner) { }
}
```

## 🎯 Common Patterns

### Pattern 1: Auto-Play Bot

```csharp
public class BotPlayer : MonoBehaviour
{
    private LudoNetworkManager network;
    
    void Start()
    {
        network = GetComponent<LudoNetworkManager>();
        network.OnDiceRolled.AddListener(AutoPlay);
    }
    
    void AutoPlay(DiceRolledPayload payload)
    {
        if (payload.playerIndex == network.PlayerIndex && payload.validMoves.Length > 0)
        {
            // Wait 1 second then move first valid token
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

### Pattern 2: Turn Indicator

```csharp
public class TurnIndicator : MonoBehaviour
{
    [SerializeField] private GameObject yourTurnPanel;
    private LudoNetworkManager network;
    
    void Update()
    {
        if (network != null)
        {
            yourTurnPanel.SetActive(network.IsMyTurn);
        }
    }
}
```

### Pattern 3: Session ID Sharing

```csharp
public class SessionSharer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sessionIdText;
    [SerializeField] private Button copyButton;
    
    void Start()
    {
        var network = GetComponent<LudoNetworkManager>();
        network.OnGameCreated.AddListener(payload => {
            sessionIdText.text = payload.sessionId;
            copyButton.onClick.AddListener(() => {
                GUIUtility.systemCopyBuffer = payload.sessionId;
            });
        });
    }
}
```

## 🔧 Advanced Integration

### Custom Message Handling

```csharp
// Extend LudoNetworkManager for custom messages
public class ExtendedNetworkManager : LudoNetworkManager
{
    // Override or extend message handling
    protected override void HandleCustomMessage(string type, string payload)
    {
        switch(type)
        {
            case "custom_event":
                // Handle your custom event
                break;
        }
    }
}
```

### State Synchronization

The `NetworkGameBridge` automatically syncs network state to `OfflineLudoGame`, which triggers:
- `onBoardSync` event
- Visual updates via `BoardSynchronizer`
- Token positioning

To customize:
```csharp
public class CustomBridge : NetworkGameBridge
{
    protected override void HandleTokenMoved(TokenMovedPayload payload)
    {
        base.HandleTokenMoved(payload);
        
        // Your custom logic
        PlayCustomAnimation(payload.tokenIndex);
    }
}
```

## 📱 Platform-Specific Notes

### WebGL Build

1. Use NativeWebSocket library instead of SimpleWebSocket
2. Add JavaScript bridge to index.html (see Network/README.md)
3. Test in browser with `npm start` running

### Mobile (iOS/Android)

- Works out of the box with System.Net.WebSockets
- Test on device, not just editor
- Consider using secure WebSocket (wss://) for production

### Desktop

- Fully supported
- Fastest performance
- Best for development

## 🐛 Debugging

### Enable Detailed Logging

In `LudoNetworkManager` Inspector:
- ☑️ Log Messages

This will log all WebSocket traffic to Console.

### Common Issues

**"Not connected to server"**
- Check server is running: `cd server && npm start`
- Verify Server URL in Inspector
- Check Console for connection errors

**"Not your turn"**
- Check `network.IsMyTurn` before actions
- Subscribe to `OnDiceRolled` to know when it's your turn

**State not syncing**
- Ensure `NetworkGameBridge` has `OfflineLudoGame` reference
- Check "Sync To Local Game" is enabled
- Verify `BoardSynchronizer` is in scene

**UI not updating**
- Subscribe to events, don't poll properties
- Update UI in event handlers
- Use `UnityEvent.AddListener()` in OnEnable/Start

## 🎓 Best Practices

1. **Always use events** - Don't poll `IsMyTurn` every frame
2. **Check state before actions** - Use `IsConnected`, `IsMyTurn`, etc.
3. **Handle disconnections** - Subscribe to `OnDisconnected` and show reconnect UI
4. **Validate moves locally** - Even though server validates, pre-check for better UX
5. **Add delays** - Use coroutines for realistic pacing (dice roll → move)
6. **Test with multiple clients** - Build and run multiple instances

## 📦 Example Scene Hierarchy

```
Scene
├── Network Manager (LudoNetworkManager, NetworkGameBridge)
├── Ludo Game (OfflineLudoGame, BoardSynchronizer, etc.)
├── UI
│   └── Canvas (NetworkGameUI)
│       ├── Connection Panel
│       ├── Game Panel
│       └── Messages Panel
└── Game Controller (Your custom script)
```

## 🚢 Production Checklist

- [ ] Replace SimpleWebSocket with NativeWebSocket
- [ ] Use secure WebSocket (wss://) for production server
- [ ] Add player authentication
- [ ] Implement reconnection logic
- [ ] Add error recovery
- [ ] Test on all target platforms
- [ ] Add loading states and transitions
- [ ] Implement proper UI/UX
- [ ] Add sound effects for network events
- [ ] Test with poor network conditions

## 📚 Further Reading

- [Network Module README](README.md) - Complete API reference
- [Server Documentation](../../server/README.md) - Server API
- [WebSocket Protocol](../../server/README.md#websocket-api) - Message formats
