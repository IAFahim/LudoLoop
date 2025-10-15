# LudoLoop

A complete Ludo game implementation with Unity client and Node.js multiplayer server.

## 🎮 Project Structure

### Unity Game (Assets/Scripts)
The Unity-based Ludo game with complete game logic, visualization, and offline play.

**Core Components:**
- `LudoGame/` - Core game state and logic
- `DiceRoll/` - Dice rolling mechanics
- `Placements/` - Board and token placement
- `Syncs/` - Game state synchronization
- `Visualizers/` - Visual representation

### WebSocket Server (server/)
A Node.js WebSocket server for real-time multiplayer gameplay.

**Features:**
- ✅ Real-time multiplayer (2-4 players)
- ✅ Game session management
- ✅ Player reconnection support
- ✅ Complete Ludo rules implementation
- ✅ Browser and Node.js clients

## 🚀 Quick Start

### Running the Multiplayer Server

```bash
cd server
npm install
npm start
```

Server runs on `ws://localhost:8080` by default.

### Testing with Browser Client

1. Start the server
2. Open `server/test-client.html` in your browser
3. Create or join a game
4. Play!

### Testing with Node.js

```bash
cd server
node example-client.js
```

## 📚 Documentation

- [Server Documentation](server/README.md) - Complete WebSocket API reference
- [Quick Start Guide](server/QUICKSTART.md) - Get started quickly
- [Unity Scripts](Assets/Scripts/) - Unity game implementation

## 🎲 Game Features

### Complete Ludo Rules
- Safe tiles protection
- Blockades (2+ tokens block opponents)
- Home stretch and finishing
- Three consecutive sixes penalty
- Token eviction (send opponents back to base)
- Roll again on: six, reaching home, or evicting opponent

### Multiplayer Support
- 2-4 players per game
- Real-time synchronization
- Player disconnection/reconnection handling
- Turn-based gameplay
- Game state persistence

## 🛠️ Technology Stack

**Unity Client:**
- C# / Unity 3D
- Custom assembly definitions
- Event-driven architecture

**Server:**
- Node.js
- WebSocket (ws library)
- Pure JavaScript implementation

## 📖 API Example

```javascript
// Connect to server
const ws = new WebSocket('ws://localhost:8080');

// Create a game
ws.send(JSON.stringify({
  type: 'create_game',
  payload: { playerName: 'Alice', maxPlayers: 4 }
}));

// Roll dice
ws.send(JSON.stringify({
  type: 'roll_dice',
  payload: { playerId: 'your-player-id' }
}));

// Move token
ws.send(JSON.stringify({
  type: 'move_token',
  payload: { playerId: 'your-player-id', tokenIndex: 0 }
}));
```

## 🔌 Connecting Unity to Server

To connect the Unity game to the multiplayer server, you'll need a WebSocket client library:

**Quick Start:**

1. **Add Network Manager to your scene:**
   ```
   GameObject > Create Empty > Add Component > Ludo Network Manager
   ```

2. **Configure in Inspector:**
   - Server URL: `ws://localhost:8080`
   - Player Name: Your name
   - Auto Connect: ☑️

3. **Add Game Bridge:**
   ```
   Add Component > Network Game Bridge
   Assign OfflineLudoGame reference
   ```

4. **Press Play!**

**Complete Integration Guide:**
See [Assets/Scripts/Network/INTEGRATION.md](Assets/Scripts/Network/INTEGRATION.md) for detailed setup instructions.

**Example Usage:**

```csharp
using Network.Runtime;

public class MyController : MonoBehaviour
{
    private LudoNetworkManager network;
    
    void Start()
    {
        network = GetComponent<LudoNetworkManager>();
        network.OnGameStarted.AddListener(OnGameStarted);
        network.Connect();
    }
    
    void OnGameStarted(GameStartedPayload payload)
    {
        Debug.Log($"Game started! {payload.playerCount} players");
    }
    
    public void RollDice()
    {
        if (network.IsMyTurn) {
            network.RollDice();
        }
    }
}
```

**Network Module Documentation:**
- [Network API Reference](Assets/Scripts/Network/README.md)
- [Integration Guide](Assets/Scripts/Network/INTEGRATION.md)
- [Example Scripts](Assets/Scripts/Network/Runtime/)

### Unity Network Components

The Unity integration includes:

- ✅ **LudoNetworkManager** - WebSocket connection and message handling
- ✅ **NetworkGameBridge** - Syncs network state to local game visualization  
- ✅ **NetworkGameUI** - Ready-to-use UI with buttons and console
- ✅ **NetworkGameExample** - Complete example implementation
- ✅ **SimpleWebSocket** - Cross-platform WebSocket wrapper
- ✅ Full event system for all game state changes
- ✅ Automatic state synchronization
- ✅ Thread-safe message processing

**Recommended Libraries:**
- [NativeWebSocket](https://github.com/endel/NativeWebSocket) - Best for production
- [WebSocketSharp](https://github.com/sta/websocket-sharp) - Desktop/mobile only

## 🎯 Game State

The game state is serialized to a compact base64 string (26 bytes):

- **TurnCount** (2 bytes)
- **DiceValue** (1 byte)
- **ConsecutiveSixes** (1 byte)
- **CurrentPlayer** (1 byte)
- **PlayerCount** (1 byte)
- **Seed** (4 bytes)
- **TokenPositions** (16 bytes) - All player tokens

This enables efficient synchronization and state persistence.

## 📦 Files

```
LudoLoop/
├── Assets/Scripts/          # Unity game implementation
│   ├── LudoGame/           # Core game logic
│   ├── DiceRoll/           # Dice mechanics
│   ├── Placements/         # Board layout
│   └── Syncs/              # State synchronization
├── server/                  # Node.js multiplayer server
│   ├── server.js           # WebSocket server
│   ├── gameSession.js      # Session management
│   ├── ludoGame.js         # Game logic (JS port)
│   ├── example-client.js   # Example Node.js client
│   ├── test-client.html    # Browser test client
│   ├── README.md           # Server documentation
│   └── QUICKSTART.md       # Quick start guide
└── README.md               # This file
```

## 🤝 Contributing

This is a complete implementation of the Ludo board game. Feel free to extend it with:
- Additional game modes
- AI players
- Tournament systems
- Spectator mode
- Chat functionality

## 📄 License

See [LICENSE.md](LICENSE.md)
