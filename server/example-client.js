// Simple example of how to use the Ludo Game WebSocket client

const WebSocket = require('ws');

class LudoClient {
    constructor(serverUrl = 'ws://localhost:8080') {
        this.serverUrl = serverUrl;
        this.ws = null;
        this.playerId = null;
        this.sessionId = null;
        this.playerIndex = null;
    }

    connect() {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket(this.serverUrl);

            this.ws.on('open', () => {
                console.log('✅ Connected to server');
                resolve();
            });

            this.ws.on('message', (data) => {
                const message = JSON.parse(data);
                this.handleMessage(message);
            });

            this.ws.on('error', (error) => {
                console.error('❌ WebSocket error:', error);
                reject(error);
            });

            this.ws.on('close', () => {
                console.log('🔌 Disconnected from server');
            });
        });
    }

    send(type, payload) {
        this.ws.send(JSON.stringify({ type, payload }));
    }

    handleMessage(message) {
        const { type, payload } = message;

        console.log(`\n📨 Received: ${type}`);

        switch (type) {
            case 'connected':
                console.log(payload?.message || 'Connected to server');
                break;

            case 'game_created':
                this.playerId = payload.playerId;
                this.sessionId = payload.sessionId;
                this.playerIndex = payload.playerIndex;
                console.log(`🎮 Game created!`);
                console.log(`   Session ID: ${this.sessionId}`);
                console.log(`   Player ID: ${this.playerId}`);
                console.log(`   Your index: ${this.playerIndex}`);
                break;

            case 'game_joined':
                this.playerId = payload.playerId;
                this.sessionId = payload.sessionId;
                this.playerIndex = payload.playerIndex;
                console.log(`✅ Joined game!`);
                console.log(`   Your index: ${this.playerIndex}`);
                break;

            case 'player_joined':
                console.log(`👤 Player ${payload.playerIndex} joined (${payload.playerName})`);
                console.log(`   Total players: ${payload.playerCount}`);
                break;

            case 'game_started':
                console.log(`🎲 Game started!`);
                console.log(`   Player ${payload.currentPlayer} goes first`);
                break;

            case 'dice_rolled':
                console.log(`🎲 Player ${payload.playerIndex} rolled ${payload.diceValue}`);
                if (payload.noValidMoves) {
                    console.log(`   ⚠️  No valid moves! Turn skipped.`);
                } else {
                    console.log(`   Valid moves: [${payload.validMoves.join(', ')}]`);
                }
                break;

            case 'token_moved':
                console.log(`♟️  Player ${payload.playerIndex} moved token ${payload.tokenIndex}`);
                console.log(`   Result: ${payload.message}`);
                console.log(`   New position: ${payload.newPosition}`);
                if (payload.hasWon) {
                    console.log(`   🎉 Player ${payload.playerIndex} WON!`);
                }
                if (!payload.turnSwitched) {
                    console.log(`   🔄 Player ${payload.playerIndex} rolls again!`);
                } else {
                    console.log(`   ➡️  Next player: ${payload.nextPlayer}`);
                }
                break;

            case 'game_over':
                console.log(`\n🏆 GAME OVER! 🏆`);
                console.log(`   Winner: Player ${payload.winnerIndex} (${payload.winnerName})`);
                break;

            case 'error':
                console.log(`❌ Error: ${payload.error}`);
                break;

            default:
                console.log(`   Data:`, payload);
        }
    }

    createGame(playerName = 'Bot', maxPlayers = 4) {
        this.send('create_game', { playerName, maxPlayers });
    }

    joinGame(sessionId, playerName = 'Bot') {
        this.send('join_game', { sessionId, playerName });
    }

    startGame() {
        this.send('start_game', { playerId: this.playerId });
    }

    rollDice() {
        this.send('roll_dice', { playerId: this.playerId });
    }

    moveToken(tokenIndex) {
        this.send('move_token', { playerId: this.playerId, tokenIndex });
    }

    getState() {
        this.send('get_state', { playerId: this.playerId });
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
        }
    }
}

// Example usage
async function example() {
    const client = new LudoClient();
    
    await client.connect();
    
    // Create a game
    client.createGame('Example Player', 2);
    
    // Wait for other players to join...
    // client.startGame();
    
    // Roll dice
    // client.rollDice();
    
    // Move token
    // client.moveToken(0);
}

// Run example if this file is executed directly
if (require.main === module) {
    example().catch(console.error);
}

module.exports = LudoClient;
