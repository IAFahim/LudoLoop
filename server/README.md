# Ludo Game WebSocket Server

A Node.js WebSocket server for playing the Ludo game with multiplayer support.

## Features

- ✅ Multiplayer support (2-4 players)
- ✅ Real-time game synchronization via WebSockets
- ✅ Complete Ludo game logic (dice rolling, token movement, game rules)
- ✅ Player reconnection support
- ✅ Game session management
- ✅ Full implementation of Ludo rules (blockades, safe tiles, home stretch)

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

The server will start on port `8080` by default. You can change this by setting the `PORT` environment variable:

```bash
PORT=3000 npm start
```

## WebSocket API

### Message Format

All messages follow this structure:

```json
{
  "type": "message_type",
  "payload": { /* message-specific data */ }
}
```

### Client -> Server Messages

#### 1. Create Game

```json
{
  "type": "create_game",
  "payload": {
    "maxPlayers": 4,
    "playerName": "Player 1"
  }
}
```

**Response:**
```json
{
  "type": "game_created",
  "payload": {
    "sessionId": "uuid",
    "playerId": "uuid",
    "playerIndex": 0,
    "maxPlayers": 4,
    "gameState": { /* game state */ }
  }
}
```

#### 2. Join Game

```json
{
  "type": "join_game",
  "payload": {
    "sessionId": "uuid",
    "playerName": "Player 2"
  }
}
```

**Response:**
```json
{
  "type": "game_joined",
  "payload": {
    "sessionId": "uuid",
    "playerId": "uuid",
    "playerIndex": 1,
    "gameState": { /* game state */ }
  }
}
```

#### 3. Start Game

```json
{
  "type": "start_game",
  "payload": {
    "playerId": "uuid"
  }
}
```

**Broadcast to all players:**
```json
{
  "type": "game_started",
  "payload": {
    "playerCount": 2,
    "currentPlayer": 0,
    "gameState": { /* game state */ }
  }
}
```

#### 4. Roll Dice

```json
{
  "type": "roll_dice",
  "payload": {
    "playerId": "uuid",
    "diceValue": null  // Optional: for testing, otherwise random 1-6
  }
}
```

**Broadcast to all players:**
```json
{
  "type": "dice_rolled",
  "payload": {
    "playerId": "uuid",
    "playerIndex": 0,
    "diceValue": 4,
    "validMoves": [0, 1, 2],  // Token indices that can move
    "noValidMoves": false,
    "nextPlayer": 0
  }
}
```

#### 5. Move Token

```json
{
  "type": "move_token",
  "payload": {
    "playerId": "uuid",
    "tokenIndex": 0  // 0-15 (player 0: 0-3, player 1: 4-7, etc.)
  }
}
```

**Broadcast to all players:**
```json
{
  "type": "token_moved",
  "payload": {
    "playerId": "uuid",
    "playerIndex": 0,
    "tokenIndex": 0,
    "moveResult": "Success",
    "message": "Move successful.",
    "newPosition": 5,
    "hasWon": false,
    "turnSwitched": true,
    "nextPlayer": 1,
    "gameState": { /* updated game state */ }
  }
}
```

#### 6. Get Game State

```json
{
  "type": "get_state",
  "payload": {
    "playerId": "uuid"
  }
}
```

**Response:**
```json
{
  "type": "game_state",
  "payload": {
    "sessionId": "uuid",
    "playerIndex": 0,
    "playerCount": 2,
    "currentPlayer": 0,
    "gameState": {
      "turnCount": 5,
      "diceValue": 0,
      "consecutiveSixes": 0,
      "currentPlayer": 0,
      "tokenPositions": [-1, -1, -1, -1, ...],  // 16 positions
      "playerCount": 2
    },
    "players": [...],
    "isStarted": true,
    "winnerId": null
  }
}
```

#### 7. Leave Game

```json
{
  "type": "leave_game",
  "payload": {
    "playerId": "uuid"
  }
}
```

#### 8. Reconnect

```json
{
  "type": "reconnect",
  "payload": {
    "playerId": "uuid"
  }
}
```

#### 9. List Games

```json
{
  "type": "list_games",
  "payload": {}
}
```

**Response:**
```json
{
  "type": "games_list",
  "payload": {
    "games": [
      {
        "sessionId": "uuid",
        "playerCount": 2,
        "maxPlayers": 4,
        "isStarted": false,
        "createdAt": 1234567890
      }
    ]
  }
}
```

### Server -> Client Messages

#### Error

```json
{
  "type": "error",
  "payload": {
    "error": "Error message"
  }
}
```

#### Game Over

```json
{
  "type": "game_over",
  "payload": {
    "winnerId": "uuid",
    "winnerIndex": 0,
    "winnerName": "Player 1"
  }
}
```

## Game Rules

### Token Positions
- `-1`: Token in base (not yet entered the board)
- `0-51`: Main path positions
- `52-56`: Home stretch (100-105 in encoding)
- `57`: Finished

### Move Results
- `Success`: Normal move
- `SuccessSix`: Rolled a 6, roll again
- `SuccessRollAgain`: Token reached home, roll again
- `SuccessEvictedOpponent`: Sent opponent back to base, roll again
- `SuccessThirdSixPenalty`: Third consecutive 6, turn ends
- `InvalidNeedSixToExit`: Must roll 6 to leave base
- `InvalidOvershoot`: Would overshoot home
- `InvalidBlockedByBlockade`: Path blocked by 2+ opponent tokens
- `InvalidTokenFinished`: Token already finished
- `InvalidNotYourToken`: Not your turn/token
- `InvalidNoValidMoves`: No legal moves available

### Game Flow
1. Create or join a game
2. Wait for all players to join
3. Start the game
4. Current player rolls dice
5. Current player selects a valid token to move
6. Token moves, game rules are applied
7. Turn switches or player rolls again (on 6, eviction, or reaching home)
8. Game continues until one player gets all 4 tokens home

## Example Client

See `test-client.html` for a browser-based example client implementation.

## Architecture

- **server.js**: Main WebSocket server and message routing
- **gameSession.js**: Game session management (players, turns, state)
- **ludoGame.js**: Core Ludo game logic (board, rules, moves)

## Testing

You can test the server using the included HTML client or any WebSocket client:

```javascript
const ws = new WebSocket('ws://localhost:8080');

ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'create_game',
    payload: { maxPlayers: 4, playerName: 'Test Player' }
  }));
};

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log('Received:', data);
};
```

## License

MIT
