const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const GameSession = require('./gameSession');

const PORT = process.env.PORT || 8080;
const GAME_CLEANUP_INTERVAL = 300000; // 5 minutes
const INACTIVE_GAME_TIMEOUT = 1800000; // 30 minutes

class LudoGameServer {
    constructor() {
        this.wss = null;
        this.gameSessions = new Map();      // sessionId -> GameSession
        this.playerSessions = new Map();    // playerId -> sessionId
        this.playerConnections = new Map(); // playerId -> ws connection
        this.matchmakingQueues = new Map(); // queueKey -> [playerObjects]
        this.playerInQueue = new Map();     // playerId -> queueKey

        // Start periodic cleanup
        this.startCleanupTimer();
    }

    start() {
        this.wss = new WebSocket.Server({ port: PORT });
        this.wss.on('connection', this.handleConnection.bind(this));
        console.log(`🎲 Ludo Game Server running on port ${PORT}`);
        console.log(`Server ready for Unity clients`);
    }

    handleConnection(ws) {
        const playerId = uuidv4();
        ws.playerId = playerId;
        this.playerConnections.set(playerId, ws);

        console.log(`New client connected: ${playerId}`);

        ws.on('message', (message) => {
            try {
                const data = JSON.parse(message);
                this.handleMessage(ws, data, playerId);
            } catch (error) {
                console.error('Error parsing message:', error);
                this.sendError(ws, 'Invalid message format');
            }
        });

        ws.on('close', () => this.handleDisconnect(ws, playerId));
        ws.on('error', (error) => console.error('WebSocket error:', error));

        this.send(ws, {
            type: 'connected',
            payload: {
                message: 'Connected to Ludo Game Server',
                playerId: playerId
            }
        });
    }

    handleMessage(ws, data, playerId) {
        const { type, payload } = data;
        const newPayload = { ...payload, playerId };

        try {
            switch (type) {
                case 'join_queue':
                    this.handleJoinQueue(ws, newPayload);
                    break;
                case 'leave_queue':
                    this.handleLeaveQueue(ws, newPayload);
                    break;
                case 'roll_dice':
                    this.handleRollDice(ws, newPayload);
                    break;
                case 'move_token':
                    this.handleMoveToken(ws, newPayload);
                    break;
                case 'get_state':
                    this.handleGetState(ws, newPayload);
                    break;
                case 'leave_game':
                    this.handleLeaveGame(ws, newPayload);
                    break;
                case 'reconnect':
                    this.handleReconnect(ws, newPayload);
                    break;
                default:
                    this.sendError(ws, `Unknown message type: ${type}`);
            }
        } catch (error) {
            console.error(`Error handling message type ${type}:`, error);
            this.sendError(ws, 'Internal server error');
        }
    }

    // ==================== MATCHMAKING ====================

    handleJoinQueue(ws, payload) {
        const { playerId, playerName, roomType, playerCount } = payload;

        if (!playerName || !roomType || !playerCount) {
            return this.sendError(ws, 'Missing required fields: playerName, roomType, playerCount');
        }

        if (playerCount < 2 || playerCount > 4) {
            return this.sendError(ws, 'Player count must be between 2 and 4');
        }

        if (this.playerSessions.has(playerId)) {
            return this.sendError(ws, 'You are already in a game');
        }

        if (this.playerInQueue.has(playerId)) {
            return this.sendError(ws, 'You are already in a queue');
        }

        const queueKey = `${roomType}_${playerCount}`;

        if (!this.matchmakingQueues.has(queueKey)) {
            this.matchmakingQueues.set(queueKey, []);
        }

        const queue = this.matchmakingQueues.get(queueKey);
        queue.push({ playerId, playerName, ws, roomType });
        this.playerInQueue.set(playerId, queueKey);

        console.log(`Player ${playerName} (${playerId}) joined queue ${queueKey}. Queue: ${queue.length}/${playerCount}`);

        this.broadcastToQueue(queueKey, {
            type: 'queue_update',
            payload: {
                queueKey,
                roomType,
                currentPlayers: queue.length,
                neededPlayers: playerCount
            }
        });

        if (queue.length >= playerCount) {
            const playersForGame = queue.splice(0, playerCount);
            this.startMatchmadeGame(playersForGame, roomType, playerCount);
        }
    }

    handleLeaveQueue(ws, payload) {
        const { playerId } = payload;
        const queueKey = this.playerInQueue.get(playerId);

        if (!queueKey) {
            return this.sendError(ws, 'You are not in a queue');
        }

        const queue = this.matchmakingQueues.get(queueKey);
        if (queue) {
            const playerIndex = queue.findIndex(p => p.playerId === playerId);
            if (playerIndex > -1) {
                const player = queue.splice(playerIndex, 1)[0];
                this.playerInQueue.delete(playerId);

                console.log(`Player ${playerId} left queue ${queueKey}`);

                this.send(ws, { 
                    type: 'left_queue', 
                    payload: { success: true } 
                });

                const neededPlayers = parseInt(queueKey.split('_')[1], 10);
                this.broadcastToQueue(queueKey, {
                    type: 'queue_update',
                    payload: {
                        queueKey,
                        currentPlayers: queue.length,
                        neededPlayers: neededPlayers
                    }
                });
            }
        }
    }

    startMatchmadeGame(players, roomType, playerCount) {
        const sessionId = uuidv4();
        const session = new GameSession(sessionId, players);
        this.gameSessions.set(sessionId, session);

        console.log(`Game ${sessionId} created: ${playerCount} players, room type: ${roomType}`);

        for (const player of players) {
            this.playerSessions.set(player.playerId, sessionId);
            this.playerInQueue.delete(player.playerId);
        }

        session.broadcast({
            type: 'match_found',
            payload: {
                sessionId,
                playerCount,
                roomType,
                gameState: session.gameState,
                players: Array.from(session.players.entries()).map(([id, p]) => ({
                    playerId: id,
                    name: p.name,
                    playerIndex: p.playerIndex
                }))
            }
        });
    }

    broadcastToQueue(queueKey, message) {
        const queue = this.matchmakingQueues.get(queueKey);
        if (queue) {
            queue.forEach(player => {
                this.send(player.ws, message);
            });
        }
    }

    // ==================== GAME LOGIC ====================

    handleRollDice(ws, payload) {
        const { playerId, forcedValue } = payload;
        const sessionId = this.playerSessions.get(playerId);
        
        if (!sessionId) {
            return this.sendError(ws, 'You are not in a game');
        }

        const session = this.gameSessions.get(sessionId);
        if (!session) {
            return this.sendError(ws, 'Game session not found');
        }

        const result = session.rollDice(playerId, forcedValue);
        
        if (!result.success) {
            return this.sendError(ws, result.error);
        }

        session.broadcast({
            type: 'dice_rolled',
            payload: result
        });

        console.log(`Game ${sessionId}: Player ${result.playerIndex} rolled ${result.diceValue}`);
    }

    handleMoveToken(ws, payload) {
        const { playerId, tokenIndex } = payload;
        const sessionId = this.playerSessions.get(playerId);

        if (!sessionId) {
            return this.sendError(ws, 'You are not in a game');
        }

        const session = this.gameSessions.get(sessionId);
        if (!session) {
            return this.sendError(ws, 'Game session not found');
        }

        const result = session.moveToken(playerId, tokenIndex);

        if (!result.success) {
            return this.sendError(ws, result.error);
        }

        session.broadcast({
            type: 'token_moved',
            payload: result
        });

        console.log(`Game ${sessionId}: Player ${result.playerIndex} moved token ${tokenIndex}`);

        if (result.hasWon) {
            const player = session.players.get(playerId);
            session.broadcast({
                type: 'game_over',
                payload: {
                    winnerId: playerId,
                    winnerIndex: result.playerIndex,
                    winnerName: player.name
                }
            });

            console.log(`Game ${sessionId}: Player ${player.name} won!`);

            // Schedule game cleanup
            setTimeout(() => {
                this.cleanupGame(sessionId);
            }, 30000); // Clean up after 30 seconds
        }
    }

    handleGetState(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);

        if (!sessionId) {
            return this.sendError(ws, 'You are not in a game');
        }

        const session = this.gameSessions.get(sessionId);
        if (!session) {
            return this.sendError(ws, 'Game session not found');
        }

        const state = session.getGameState(playerId);

        if (!state.success) {
            return this.sendError(ws, state.error);
        }

        this.send(ws, {
            type: 'game_state',
            payload: state
        });
    }

    handleLeaveGame(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);

        if (!sessionId) {
            return this.sendError(ws, 'You are not in a game');
        }

        const session = this.gameSessions.get(sessionId);
        if (!session) {
            return this.sendError(ws, 'Game session not found');
        }

        const player = session.players.get(playerId);
        const playerName = player ? player.name : 'Unknown';

        session.removePlayer(playerId);
        this.playerSessions.delete(playerId);

        this.send(ws, {
            type: 'left_game',
            payload: { success: true }
        });

        session.broadcast({
            type: 'player_left',
            payload: {
                playerId,
                playerName
            }
        });

        console.log(`Player ${playerId} left game ${sessionId}`);

        if (session.allPlayersDisconnected() || session.players.size === 0) {
            this.cleanupGame(sessionId);
        }
    }

    handleReconnect(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);

        if (!sessionId) {
            return this.sendError(ws, 'No active game found for reconnection');
        }

        const session = this.gameSessions.get(sessionId);
        if (!session) {
            this.playerSessions.delete(playerId);
            return this.sendError(ws, 'Game session no longer exists');
        }

        const result = session.reconnectPlayer(playerId, ws);

        if (!result.success) {
            return this.sendError(ws, result.error);
        }

        this.playerConnections.set(playerId, ws);
        ws.playerId = playerId;

        this.send(ws, {
            type: 'reconnected',
            payload: result
        });

        session.broadcast({
            type: 'player_reconnected',
            payload: { playerId }
        }, playerId);

        console.log(`Player ${playerId} reconnected to game ${sessionId}`);
    }

    // ==================== CONNECTION MANAGEMENT ====================

    handleDisconnect(ws, playerId) {
        console.log(`Client disconnected: ${playerId}`);

        // Remove from queue if in one
        const queueKey = this.playerInQueue.get(playerId);
        if (queueKey) {
            const queue = this.matchmakingQueues.get(queueKey);
            if (queue) {
                const index = queue.findIndex(p => p.playerId === playerId);
                if (index > -1) {
                    queue.splice(index, 1);
                    this.playerInQueue.delete(playerId);
                    
                    const neededPlayers = parseInt(queueKey.split('_')[1], 10);
                    this.broadcastToQueue(queueKey, {
                        type: 'queue_update',
                        payload: {
                            queueKey,
                            currentPlayers: queue.length,
                            neededPlayers: neededPlayers
                        }
                    });
                }
            }
        }

        // Handle game disconnection
        const sessionId = this.playerSessions.get(playerId);
        if (sessionId) {
            const session = this.gameSessions.get(sessionId);
            if (session) {
                session.disconnectPlayer(playerId);
                session.broadcast({
                    type: 'player_disconnected',
                    payload: { playerId }
                }, playerId);

                console.log(`Player ${playerId} disconnected from game ${sessionId}`);
            }
        }

        this.playerConnections.delete(playerId);
    }

    // ==================== CLEANUP ====================

    cleanupGame(sessionId) {
        const session = this.gameSessions.get(sessionId);
        if (!session) return;

        console.log(`Cleaning up game ${sessionId}`);

        for (const [playerId] of session.players.entries()) {
            this.playerSessions.delete(playerId);
        }

        this.gameSessions.delete(sessionId);
    }

    startCleanupTimer() {
        setInterval(() => {
            const now = new Date();
            
            for (const [sessionId, session] of this.gameSessions.entries()) {
                const inactiveTime = now - session.lastActivityTime;
                
                if (session.isGameOver || session.allPlayersDisconnected()) {
                    this.cleanupGame(sessionId);
                } else if (inactiveTime > INACTIVE_GAME_TIMEOUT) {
                    console.log(`Game ${sessionId} inactive for ${Math.floor(inactiveTime / 60000)} minutes, cleaning up`);
                    this.cleanupGame(sessionId);
                }
            }
        }, GAME_CLEANUP_INTERVAL);
    }

    // ==================== UTILITY ====================

    send(ws, message) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            try {
                ws.send(JSON.stringify(message));
            } catch (error) {
                console.error('Error sending message:', error);
            }
        }
    }

    sendError(ws, error) {
        this.send(ws, {
            type: 'error',
            payload: { error }
        });
    }
}

// Start server
const server = new LudoGameServer();
server.start();

module.exports = LudoGameServer;