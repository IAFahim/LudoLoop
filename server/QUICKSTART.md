# Quick Start Guide

## Starting the Server

```bash
cd server
npm install
npm start
```

The server will run on `ws://localhost:8080`

## Testing with Browser Client

1. Start the server (see above)
2. Open `test-client.html` in your browser
3. Click "Connect"
4. Create or join a game
5. Play!

## Testing with Node.js Client

```bash
cd server
node example-client.js
```

## Game Flow Example

### Two-Player Game

**Player 1:**
```javascript
const client1 = new LudoClient();
await client1.connect();
client1.createGame('Alice', 2);
// Note the session ID from console
```

**Player 2:**
```javascript
const client2 = new LudoClient();
await client2.connect();
client2.joinGame('SESSION_ID_HERE', 'Bob');
```

**Player 1 starts:**
```javascript
client1.startGame();
```

**Playing:**
```javascript
// Player 1's turn
client1.rollDice();
// Check console for valid moves, e.g., [0, 1]
client1.moveToken(0);

// Player 2's turn
client2.rollDice();
client2.moveToken(4); // Player 2's first token
```

## API Reference

See `README.md` for complete API documentation.

## Project Structure

```
server/
├── server.js           # Main WebSocket server
├── gameSession.js      # Game session management
├── ludoGame.js         # Core Ludo game logic
├── example-client.js   # Node.js client example
├── test-client.html    # Browser test client
├── package.json        # Dependencies
└── README.md          # Full documentation
```

## Features

✅ Full Ludo game implementation
✅ 2-4 player support
✅ Real-time multiplayer via WebSocket
✅ Player reconnection
✅ Game state serialization
✅ Complete rule enforcement (blockades, safe tiles, home stretch)
✅ Browser and Node.js clients

## Connecting from Unity

To connect from Unity, you'll need a WebSocket client library like:
- NativeWebSocket
- WebSocketSharp
- Unity's built-in WebSocket support

Example Unity integration:

```csharp
using NativeWebSocket;
using UnityEngine;
using System;

public class LudoNetworkManager : MonoBehaviour
{
    private WebSocket ws;
    
    async void Start()
    {
        ws = new WebSocket("ws://localhost:8080");
        
        ws.OnOpen += () => {
            Debug.Log("Connected to Ludo server");
            CreateGame();
        };
        
        ws.OnMessage += (bytes) => {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleMessage(message);
        };
        
        await ws.Connect();
    }
    
    void CreateGame()
    {
        var msg = new {
            type = "create_game",
            payload = new {
                playerName = "Unity Player",
                maxPlayers = 4
            }
        };
        
        ws.SendText(JsonUtility.ToJson(msg));
    }
    
    void HandleMessage(string message)
    {
        // Parse and handle server messages
        Debug.Log("Received: " + message);
    }
}
```

## Environment Variables

- `PORT`: Server port (default: 8080)

## Troubleshooting

**Server won't start:**
- Make sure port 8080 is not in use
- Try: `PORT=3000 npm start`

**Client can't connect:**
- Check server is running
- Verify WebSocket URL (ws://localhost:8080)
- Check firewall settings

**Game state issues:**
- Use `get_state` to refresh
- Check console for error messages
- Verify you're sending correct player/token indices
