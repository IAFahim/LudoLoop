# Ludo Game WebSocket Server

A production-ready Node.js WebSocket server for Unity Ludo multiplayer game.

## Features

- ✅ **Automatic Matchmaking** - Queue-based system for 2-4 players
- ✅ **Real-time Multiplayer** - WebSocket communication for instant updates
- ✅ **Complete Ludo Logic** - Full game rules implementation (blockades, safe tiles, eviction)
- ✅ **Player Reconnection** - Seamless reconnection after disconnect
- ✅ **Game Management** - Automatic cleanup of finished/inactive games
- ✅ **Error Handling** - Comprehensive validation and error messages
- ✅ **Unity Ready** - Designed for Unity C# integration

## Installation

```bash
cd server
npm install
```

## Running the Server

```bash
npm start
```

For development with auto-reload:

```bash
npm run dev
```

The server runs on port `8080` by default. Change with environment variable:

```bash
PORT=3000 npm start
```

## Quick Start

### 1. Connect to Server

Connect your Unity client to `ws://localhost:8080`

### 2. Receive Player ID

Upon connection, you'll receive:
```json
{
  "type": "connected",
  "payload": {
    "playerId": "your-unique-id"
  }
}
```

### 3. Join Matchmaking

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

### 4. Play the Game

Once matched, take turns rolling dice and moving tokens!

See **[API.md](API.md)** for complete API documentation.

## WebSocket API Overview

### Main Game Flow

1. **Join Queue** → Wait for players → **Match Found**
2. **Roll Dice** → Get valid moves
3. **Move Token** → Update game state
4. Repeat until someone wins → **Game Over**

### Core Messages

| Message Type | Direction | Purpose |
|-------------|-----------|---------|
| `join_queue` | Client → Server | Join matchmaking |
| `leave_queue` | Client → Server | Leave matchmaking |
| `roll_dice` | Client → Server | Roll dice on your turn |
| `move_token` | Client → Server | Move a token |
| `get_state` | Client → Server | Get current game state |
| `leave_game` | Client → Server | Exit current game |
| `reconnect` | Client → Server | Reconnect after disconnect |
| `match_found` | Server → Client | Game starting |
| `dice_rolled` | Server → Client | Dice roll result |
| `token_moved` | Server → Client | Token moved |
| `game_over` | Server → Client | Game finished |
| `error` | Server → Client | Error occurred |

## Game Rules

### Token Positions
- `-1`: Token in base (hasn't entered board yet)
- `0-51`: Main circular path
- `100-123`: Home stretch (varies by player color)
- `57`: Finished (token reached home)

### Player Colors & Starting Positions
- Player 0 (Red): Start at position 0
- Player 1 (Blue): Start at position 13
- Player 2 (Green): Start at position 26
- Player 3 (Yellow): Start at position 39

### Safe Tiles
Positions 0, 13, 26, 39 are safe - tokens can't be evicted here.

### Special Rules
- **Roll 6 to Exit**: Must roll a 6 to move token from base to board
- **Roll Again**: Get another turn when you roll 6, evict opponent, or reach home
- **Third Six Penalty**: Rolling 6 three times in a row ends your turn
- **Blockade**: 2+ tokens of same color block opponents (except on safe tiles)
- **Eviction**: Landing on opponent's token sends it back to base
- **Exact Finish**: Must roll exact number to reach home (no overshoot)

### Move Result Codes
- `Success` (0): Normal move
- `SuccessRollAgain` (1): Token reached home
- `SuccessSix` (2): Rolled a 6
- `SuccessEvictedOpponent` (3): Sent opponent home
- `SuccessThirdSixPenalty` (4): Third 6 in a row
- `InvalidNeedSixToExit` (6): Need 6 to exit base
- `InvalidOvershoot` (7): Can't overshoot home
- `InvalidBlockedByBlockade` (10): Path blocked

## Testing

### Browser Test Client
Open `test-client.html` in a browser to test the server.

### Command Line Test
```bash
npm install -g wscat
wscat -c ws://localhost:8080

# Join queue:
{"type":"join_queue","payload":{"playerName":"Test","roomType":"casual","playerCount":2}}

# Roll dice:
{"type":"roll_dice","payload":{}}

# Move token:
{"type":"move_token","payload":{"tokenIndex":0}}
```

## Architecture

```
server/
├── server.js         - WebSocket server, matchmaking, message routing
├── gameSession.js    - Game session management, Ludo logic, player state
├── API.md           - Complete WebSocket API documentation
├── README.md        - This file
└── test-client.html - Browser-based test client
```

### Key Components

**LudoGameServer** (server.js)
- WebSocket connection handling
- Matchmaking queue management
- Message routing and validation
- Game session lifecycle management
- Automatic cleanup of inactive games

**GameSession** (gameSession.js)
- Complete Ludo game logic (ported from C#)
- Token movement validation
- Turn management
- Player reconnection handling
- Game state synchronization

## Unity Integration

See `API.md` for complete Unity C# integration examples including:
- WebSocket connection setup
- Message serialization
- Event handling
- Game state management

## Production Deployment

### Environment Variables
```bash
PORT=8080  # WebSocket server port
```

### Recommended Setup
- Use **PM2** for process management
- Deploy behind **nginx** with WebSocket proxy
- Use **SSL/TLS** for production (wss://)
- Configure firewall to allow WebSocket connections

### PM2 Example
```bash
npm install -g pm2
pm2 start server.js --name ludo-server
pm2 save
pm2 startup
```

## Troubleshooting

**"You are already in a game"**
- Player ID is already in an active game
- Use `leave_game` first or `reconnect` if disconnected

**"Not your turn"**
- Wait for `dice_rolled` with your playerIndex
- Check `gameState.currentPlayer` matches your playerIndex

**"Invalid move"**
- Only use token indices from `validMoves` array
- Roll dice before moving

**Connection Issues**
- Verify server is running on correct port
- Check firewall/network settings
- Ensure WebSocket support in client

## License

MIT
