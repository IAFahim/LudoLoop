# 🎮 Automatic Matchmaking Ludo Game - Quick Start

## What Changed? 🎉

The game now features **automatic matchmaking**! No more manual room creation or joining.

### Before (Old Way) ❌
```
1. Click "Create Game" 
2. Copy session ID
3. Send to friend
4. Friend clicks "Join Game" and pastes ID
5. Click "Start Game"
6. Finally play...
```

### Now (New Way) ✅
```
1. Click "Find Match"
2. Play! 🎮
```

## How It Works

### Smart Matchmaking System

1. **Join Queue** - Click "Find Match" and you're in the queue
2. **Instant Match** - If 4+ players are waiting, instant match!
3. **Flexible Teams** - After 10 seconds, matches you with available players (2-4)
4. **Auto Start** - Game starts immediately when match is found

### Examples

**Scenario 1: Lots of Players**
- 4 players in queue → Instant 4-player match! 🎯

**Scenario 2: Few Players**
- 2 players waiting 10 seconds → 2v2 match starts 👥
- 3 players waiting 10 seconds → 3-player match starts 👥👤

**Scenario 3: Growing Queue**
- You join (1 player)
- Friend joins after 5 seconds (2 players)
- Another joins at 11 seconds (3 players) → Match starts with 3 players! 🎮

## Quick Start - Web Client

### 1. Start Server
```bash
cd server
npm install
node server.js
```

Server starts on `http://localhost:8080`

### 2. Open Browser
```
Open index.html in your browser
OR
Navigate to http://localhost:8080/index.html (if serving)
```

### 3. Play!
1. Enter your name
2. Click "Connect"
3. Click "Find Match" 
4. Wait for match (up to 10 seconds)
5. Play Ludo!

### 4. Test with Multiple Players
Open multiple browser tabs to simulate multiple players.

## Quick Start - Unity Client

### 1. Install WebSocket Package
```
Unity → Window → Package Manager → Add from git URL:
https://github.com/endel/NativeWebSocket.git#upm
```

### 2. Add Client Script
1. Copy `LudoClient.cs` to `Assets/Scripts/`
2. Create GameObject, attach `LudoClient` script
3. Set server URL: `ws://localhost:8080`
4. Set your player name

### 3. Use in Your Code
```csharp
LudoClient client = FindObjectOfType<LudoClient>();

// Connect and find match
client.Connect();
client.FindMatch();

// That's it! You'll be matched automatically
```

See `UNITY_INTEGRATION.md` for complete Unity guide.

## API Changes

### Old API (Removed) ❌
- `create_game` - No longer needed
- `join_game` - No longer needed  
- `start_game` - No longer needed
- `list_games` - No longer needed
- `roomType` parameter - No longer needed
- `playerCount` parameter - No longer needed

### New API ✅
- `find_match` - Join matchmaking queue
- `leave_queue` - Leave matchmaking queue

All other game APIs remain the same:
- `roll_dice` - Roll dice
- `move_token` - Move token
- `get_state` - Get game state
- `leave_game` - Leave game
- `reconnect` - Reconnect to game

## Configuration

Edit `server.js` to adjust matchmaking:

```javascript
const QUEUE_TIMEOUT = 10000;  // 10 seconds (default)
```

Change to:
- `5000` = 5 seconds (faster matches, smaller teams)
- `15000` = 15 seconds (slower matches, larger teams)
- `30000` = 30 seconds (even more patient matching)

## Testing Locally

### Test with Multiple Clients

**Option 1: Multiple Browser Tabs**
1. Start server
2. Open `index.html` in 4 different tabs
3. Click "Find Match" in each
4. All 4 will be matched instantly!

**Option 2: Mix Web + Unity**
1. Start server
2. Open web client in browser
3. Run Unity client
4. Both find match at same time
5. They'll be matched together!

**Option 3: Different Names**
1. Player 1 types "Alice", clicks Find Match
2. Player 2 types "Bob", clicks Find Match  
3. Wait 10 seconds
4. 2v2 match starts!

## Game Flow

```
Player connects
     ↓
Player clicks "Find Match"
     ↓
[Matchmaking Queue]
     ↓
     ├─→ 4+ players? → Instant 4-player match
     ↓
     └─→ Wait 10 seconds → Match with available (2-4 players)
     ↓
[Match Found!]
     ↓
Game automatically starts
     ↓
Player 0's turn → Roll dice → Move token
     ↓
Player 1's turn → Roll dice → Move token
     ↓
...continue until winner...
     ↓
[Game Over]
```

## Troubleshooting

### "Not connected to server"
- Make sure server is running: `node server.js`
- Check server URL is correct: `ws://localhost:8080`

### "Stuck in queue"
- Need at least 2 players total
- Wait 10 seconds for flexible matching
- Try opening another browser tab to test

### "Match found but game doesn't start"
- This shouldn't happen! Match automatically starts now
- Check browser console for errors
- Refresh and try again

### Server crashes or errors
```bash
# Stop server
Ctrl+C

# Restart server
node server.js
```

## Features

✅ **Automatic Matchmaking** - No manual room management  
✅ **Flexible Team Sizes** - 2v2, 3-player, or 4-player matches  
✅ **Smart Timing** - Instant match for 4+ players, 10s timeout for smaller matches  
✅ **Reconnection** - Disconnect and reconnect to same game  
✅ **Full Ludo Logic** - Complete game rules implementation  
✅ **Unity Ready** - Complete C# client included  
✅ **Web Client** - Test in browser immediately  

## Files

### Server Files
- `server.js` - Main server with matchmaking
- `gameSession.js` - Game logic and state management
- `package.json` - Dependencies

### Web Client
- `index.html` - Web-based test client

### Unity Client
- `LudoClient.cs` - Complete Unity client script
- `LudoGameUI.cs` - Example UI controller
- `UNITY_INTEGRATION.md` - Unity integration guide

## Support

Need help? Check:
1. `UNITY_INTEGRATION.md` - Complete Unity guide
2. `API.md` - Full API documentation
3. Server console logs for errors
4. Browser console for client errors

## What's Next?

1. **Customize UI** - Build your own Ludo board visualization
2. **Add AI** - Implement computer players
3. **Leaderboards** - Track wins/losses
4. **Tournaments** - Multi-round competitions
5. **Custom Rules** - Modify game logic in `gameSession.js`

Have fun! 🎲🎮
