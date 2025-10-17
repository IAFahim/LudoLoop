// Simple test client to verify automatic matchmaking
const WebSocket = require('ws');

class TestClient {
    constructor(name) {
        this.name = name;
        this.ws = null;
        this.playerId = null;
    }

    connect() {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket('ws://localhost:8080');

            this.ws.on('open', () => {
                console.log(`[${this.name}] Connected to server`);
            });

            this.ws.on('message', (data) => {
                const message = JSON.parse(data.toString());
                this.handleMessage(message);
                
                if (message.type === 'connected') {
                    resolve();
                }
            });

            this.ws.on('error', (error) => {
                console.error(`[${this.name}] Error:`, error.message);
                reject(error);
            });

            this.ws.on('close', () => {
                console.log(`[${this.name}] Disconnected`);
            });
        });
    }

    handleMessage(message) {
        const { type, payload } = message;

        switch (type) {
            case 'connected':
                this.playerId = payload.playerId;
                console.log(`[${this.name}] Got player ID: ${this.playerId.substring(0, 8)}...`);
                break;

            case 'queue_joined':
                console.log(`[${this.name}] ✓ Joined queue. ${payload.playersInQueue} players waiting`);
                break;

            case 'match_found':
                console.log(`[${this.name}] 🎮 MATCH FOUND! ${payload.playerCount} players`);
                console.log(`[${this.name}] Session: ${payload.sessionId.substring(0, 8)}...`);
                console.log(`[${this.name}] I am player ${payload.players.find(p => p.playerId === this.playerId)?.playerIndex}`);
                break;

            case 'dice_rolled':
                console.log(`[${this.name}] 🎲 Player ${payload.playerIndex} rolled ${payload.diceValue}`);
                break;

            case 'token_moved':
                console.log(`[${this.name}] 🚀 Player ${payload.playerIndex} moved token ${payload.tokenIndex}`);
                break;

            case 'game_over':
                console.log(`[${this.name}] 🎉 GAME OVER! Winner: Player ${payload.winnerIndex} (${payload.winnerName})`);
                break;

            case 'error':
                console.error(`[${this.name}] ❌ Error: ${payload.error}`);
                break;

            default:
                // Ignore other message types for clean output
                break;
        }
    }

    send(type, payload) {
        this.ws.send(JSON.stringify({ type, payload }));
    }

    findMatch() {
        console.log(`[${this.name}] Finding match...`);
        this.send('find_match', { playerName: this.name });
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
        }
    }
}

// ==================== TEST SCENARIOS ====================

async function testInstantMatch() {
    console.log('\n═══════════════════════════════════════');
    console.log('TEST 1: Instant Match (4 Players)');
    console.log('═══════════════════════════════════════\n');

    const clients = [
        new TestClient('Alice'),
        new TestClient('Bob'),
        new TestClient('Charlie'),
        new TestClient('Diana')
    ];

    // Connect all clients
    for (const client of clients) {
        await client.connect();
        await new Promise(resolve => setTimeout(resolve, 200));
    }

    console.log('\n--- All clients connected. Finding matches... ---\n');

    // All join queue at once
    for (const client of clients) {
        client.findMatch();
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    // Wait to see match
    await new Promise(resolve => setTimeout(resolve, 3000));

    console.log('\n--- Test complete! Disconnecting... ---\n');
    for (const client of clients) {
        client.disconnect();
    }
}

async function testFlexibleMatch() {
    console.log('\n═══════════════════════════════════════');
    console.log('TEST 2: Flexible Match (2 Players, 10s wait)');
    console.log('═══════════════════════════════════════\n');

    const clients = [
        new TestClient('Player1'),
        new TestClient('Player2')
    ];

    // Connect clients
    for (const client of clients) {
        await client.connect();
        await new Promise(resolve => setTimeout(resolve, 200));
    }

    console.log('\n--- Clients connected. Finding matches... ---\n');

    // Join queue
    for (const client of clients) {
        client.findMatch();
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    console.log('--- Waiting 10 seconds for flexible matching... ---\n');

    // Wait for flexible match (10 seconds)
    await new Promise(resolve => setTimeout(resolve, 11000));

    console.log('\n--- Test complete! Disconnecting... ---\n');
    for (const client of clients) {
        client.disconnect();
    }
}

async function testStaggeredJoin() {
    console.log('\n═══════════════════════════════════════');
    console.log('TEST 3: Staggered Join (3 Players)');
    console.log('═══════════════════════════════════════\n');

    const client1 = new TestClient('Early');
    const client2 = new TestClient('Middle');
    const client3 = new TestClient('Late');

    // First player joins
    await client1.connect();
    console.log('\n--- First player finding match... ---\n');
    client1.findMatch();
    
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Second player joins
    await client2.connect();
    console.log('\n--- Second player finding match... ---\n');
    client2.findMatch();
    
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Third player joins
    await client3.connect();
    console.log('\n--- Third player finding match... ---\n');
    client3.findMatch();

    console.log('\n--- Waiting for match (should trigger after 10s from first player)... ---\n');
    
    // Wait for match
    await new Promise(resolve => setTimeout(resolve, 5000));

    console.log('\n--- Test complete! Disconnecting... ---\n');
    client1.disconnect();
    client2.disconnect();
    client3.disconnect();
}

// ==================== RUN TESTS ====================

async function runTests() {
    console.log('\n🎮 AUTOMATIC MATCHMAKING TEST SUITE 🎮\n');
    console.log('Make sure server is running: node server.js\n');

    const args = process.argv.slice(2);
    const testNum = args[0] || '1';

    try {
        switch (testNum) {
            case '1':
                await testInstantMatch();
                break;
            case '2':
                await testFlexibleMatch();
                break;
            case '3':
                await testStaggeredJoin();
                break;
            case 'all':
                await testInstantMatch();
                await new Promise(resolve => setTimeout(resolve, 2000));
                await testFlexibleMatch();
                await new Promise(resolve => setTimeout(resolve, 2000));
                await testStaggeredJoin();
                break;
            default:
                console.log('Usage: node test-matchmaking.js [1|2|3|all]');
                console.log('  1 - Test instant match (4 players)');
                console.log('  2 - Test flexible match (2 players)');
                console.log('  3 - Test staggered join (3 players)');
                console.log('  all - Run all tests');
        }

        console.log('\n✅ All tests completed!\n');
        process.exit(0);
    } catch (error) {
        console.error('\n❌ Test failed:', error.message);
        process.exit(1);
    }
}

runTests();
