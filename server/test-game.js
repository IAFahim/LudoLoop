// Test script for Ludo Game Server
const WebSocket = require('ws');

const SERVER_URL = 'ws://localhost:8080';

class TestClient {
    constructor(name) {
        this.name = name;
        this.ws = null;
        this.playerId = null;
    }

    connect() {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket(SERVER_URL);

            this.ws.on('open', () => {
                console.log(`[${this.name}] Connected to server`);
            });

            this.ws.on('message', (data) => {
                const message = JSON.parse(data);
                this.handleMessage(message);
            });

            this.ws.on('error', (error) => {
                console.error(`[${this.name}] Error:`, error.message);
                reject(error);
            });

            this.ws.on('close', () => {
                console.log(`[${this.name}] Disconnected`);
            });

            // Wait for connected message
            const messageHandler = (data) => {
                const message = JSON.parse(data);
                if (message.type === 'connected') {
                    this.playerId = message.payload.playerId;
                    console.log(`[${this.name}] Received player ID: ${this.playerId}`);
                    this.ws.off('message', messageHandler);
                    resolve();
                }
            };
            this.ws.on('message', messageHandler);
        });
    }

    handleMessage(message) {
        const { type, payload } = message;

        switch (type) {
            case 'queue_update':
                console.log(`[${this.name}] Queue update: ${payload.currentPlayers}/${payload.neededPlayers}`);
                break;

            case 'match_found':
                console.log(`[${this.name}] 🎮 Match found! Session: ${payload.sessionId}`);
                console.log(`[${this.name}] Players:`, payload.players.map(p => p.name).join(', '));
                break;

            case 'dice_rolled':
                console.log(`[${this.name}] 🎲 Player ${payload.playerIndex} rolled ${payload.diceValue}`);
                console.log(`[${this.name}] Valid moves: ${payload.validMoves.join(', ')}`);
                if (payload.noValidMoves) {
                    console.log(`[${this.name}] ❌ No valid moves, turn skipped`);
                }
                break;

            case 'token_moved':
                console.log(`[${this.name}] ♟️  Player ${payload.playerIndex} moved token ${payload.tokenIndex} to position ${payload.newPosition}`);
                console.log(`[${this.name}] Result: ${payload.moveResult}`);
                if (payload.hasWon) {
                    console.log(`[${this.name}] 🏆 Player ${payload.playerIndex} WON!`);
                }
                break;

            case 'game_over':
                console.log(`[${this.name}] 🏁 GAME OVER! Winner: ${payload.winnerName}`);
                break;

            case 'error':
                console.error(`[${this.name}] ❌ Error: ${payload.error}`);
                break;

            case 'player_disconnected':
                console.log(`[${this.name}] Player disconnected: ${payload.playerId}`);
                break;

            case 'player_reconnected':
                console.log(`[${this.name}] Player reconnected: ${payload.playerId}`);
                break;

            case 'player_left':
                console.log(`[${this.name}] Player left: ${payload.playerName}`);
                break;
        }
    }

    send(type, payload = {}) {
        const message = { type, payload };
        this.ws.send(JSON.stringify(message));
    }

    joinQueue(roomType = 'casual', playerCount = 2) {
        console.log(`[${this.name}] Joining queue for ${playerCount} players...`);
        this.send('join_queue', {
            playerName: this.name,
            roomType,
            playerCount
        });
    }

    rollDice(forcedValue = 0) {
        this.send('roll_dice', { forcedValue });
    }

    moveToken(tokenIndex) {
        this.send('move_token', { tokenIndex });
    }

    getState() {
        this.send('get_state');
    }

    leaveGame() {
        this.send('leave_game');
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
        }
    }
}

async function testBasicMatchmaking() {
    console.log('\n=== TEST 1: Basic Matchmaking (2 players) ===\n');

    const player1 = new TestClient('Alice');
    const player2 = new TestClient('Bob');

    await player1.connect();
    await player2.connect();

    await new Promise(resolve => setTimeout(resolve, 500));

    player1.joinQueue('casual', 2);
    await new Promise(resolve => setTimeout(resolve, 500));
    player2.joinQueue('casual', 2);

    // Wait for match to be found
    await new Promise(resolve => setTimeout(resolve, 2000));

    console.log('\n✅ Matchmaking test completed\n');
    
    player1.disconnect();
    player2.disconnect();
}

async function testGameFlow() {
    console.log('\n=== TEST 2: Complete Game Flow ===\n');

    const player1 = new TestClient('Charlie');
    const player2 = new TestClient('Diana');

    await player1.connect();
    await player2.connect();

    await new Promise(resolve => setTimeout(resolve, 500));

    player1.joinQueue('test', 2);
    player2.joinQueue('test', 2);

    // Wait for match
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Play a few turns
    console.log('\n--- Starting game turns ---\n');

    // Turn 1: Player 0 (Charlie)
    player1.rollDice(6); // Force a 6
    await new Promise(resolve => setTimeout(resolve, 500));
    player1.moveToken(0); // Move first token
    await new Promise(resolve => setTimeout(resolve, 500));

    // Player gets to roll again (rolled 6)
    player1.rollDice(4);
    await new Promise(resolve => setTimeout(resolve, 500));
    player1.moveToken(0); // Move same token
    await new Promise(resolve => setTimeout(resolve, 500));

    // Turn 2: Player 1 (Diana)
    player2.rollDice(6);
    await new Promise(resolve => setTimeout(resolve, 500));
    player2.moveToken(4); // Move first token of player 1
    await new Promise(resolve => setTimeout(resolve, 500));

    console.log('\n✅ Game flow test completed\n');

    player1.disconnect();
    player2.disconnect();
}

async function testReconnection() {
    console.log('\n=== TEST 3: Player Reconnection ===\n');

    const player1 = new TestClient('Eve');
    const player2 = new TestClient('Frank');

    await player1.connect();
    await player2.connect();

    const player1Id = player1.playerId;

    await new Promise(resolve => setTimeout(resolve, 500));

    player1.joinQueue('reconnect', 2);
    player2.joinQueue('reconnect', 2);

    await new Promise(resolve => setTimeout(resolve, 2000));

    // Start game
    player1.rollDice(6);
    await new Promise(resolve => setTimeout(resolve, 500));

    // Disconnect player 1
    console.log('\n[Eve] Disconnecting...\n');
    player1.disconnect();
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Reconnect player 1
    console.log('[Eve] Reconnecting...\n');
    const reconnectedPlayer = new TestClient('Eve (Reconnected)');
    await reconnectedPlayer.connect();
    
    // Manually set the player ID to simulate reconnection
    reconnectedPlayer.playerId = player1Id;
    reconnectedPlayer.send('reconnect');

    await new Promise(resolve => setTimeout(resolve, 2000));

    console.log('\n✅ Reconnection test completed\n');

    reconnectedPlayer.disconnect();
    player2.disconnect();
}

async function runAllTests() {
    console.log('🧪 Starting Ludo Server Tests...\n');

    try {
        await testBasicMatchmaking();
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testGameFlow();
        await new Promise(resolve => setTimeout(resolve, 2000));

        await testReconnection();
        await new Promise(resolve => setTimeout(resolve, 2000));

        console.log('\n✅ All tests completed successfully!\n');
        process.exit(0);
    } catch (error) {
        console.error('\n❌ Test failed:', error);
        process.exit(1);
    }
}

// Run tests
runAllTests();
