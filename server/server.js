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
        this.matchmakingQueue = [];         // All players in queue
        this.playerInQueue = new Map();     // playerId -> queue object
        this.QUEUE_TIMEOUT = 10000;         // 10 seconds to match

        // Start periodic cleanup and matchmaking
        this.startCleanupTimer();
        this.startMatchmakingTimer();
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
                case 'find_match':
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
        const { playerId, playerName } = payload;

        if (!playerName) {
            return this.sendError(ws, 'Missing required field: playerName');
        }

        if (this.playerSessions.has(playerId)) {
            return this.sendError(ws, 'You are already in a game');
        }

        if (this.playerInQueue.has(playerId)) {
            return this.sendError(ws, 'You are already in a queue');
        }

        const queueObject = {
            playerId,
            playerName,
            ws,
            joinedAt: Date.now()
        };

        this.matchmakingQueue.push(queueObject);
        this.playerInQueue.set(playerId, queueObject);

        console.log(`Player ${playerName} (${playerId}) joined matchmaking queue. Queue size: ${this.matchmakingQueue.length}`);

        this.send(ws, {
            type: 'queue_joined',
            payload: {
                playersInQueue: this.matchmakingQueue.length,
                message: 'Searching for match...'
            }
        });

        // Try immediate matchmaking
        this.tryMatchmaking();
    }

    handleLeaveQueue(ws, payload) {
        const { playerId } = payload;
        const queueObject = this.playerInQueue.get(playerId);

        if (!queueObject) {
            return this.sendError(ws, 'You are not in a queue');
        }

        const index = this.matchmakingQueue.findIndex(p => p.playerId === playerId);
        if (index > -1) {
            this.matchmakingQueue.splice(index, 1);
            this.playerInQueue.delete(playerId);

            console.log(`Player ${playerId} left queue. Queue size: ${this.matchmakingQueue.length}`);

            this.send(ws, { 
                type: 'left_queue', 
                payload: { success: true } 
            });
        }
    }

    startMatchmakingTimer() {
        setInterval(() => {
            this.tryMatchmaking();
        }, 2000); // Check every 2 seconds
    }

    tryMatchmaking() {
        if (this.matchmakingQueue.length < 2) {
            return; // Need at least 2 players
        }

        const now = Date.now();
        
        // Sort by wait time (oldest first)
        this.matchmakingQueue.sort((a, b) => a.joinedAt - b.joinedAt);

        // Find players who have been waiting for timeout
        const waitingPlayers = this.matchmakingQueue.filter(p => (now - p.joinedAt) >= this.QUEUE_TIMEOUT);

        // If we have enough players waiting, create a game with available players
        if (waitingPlayers.length >= 2) {
            // Determine team size based on available players
            let playerCount = Math.min(waitingPlayers.length, 4);
            
            // Create game with available players
            const playersForGame = this.matchmakingQueue.splice(0, playerCount);
            
            // Remove from queue tracking
            playersForGame.forEach(p => this.playerInQueue.delete(p.playerId));
            
            this.startMatchmadeGame(playersForGame);
        }
        // If we have 4 or more players total, instant match
        else if (this.matchmakingQueue.length >= 4) {
            const playersForGame = this.matchmakingQueue.splice(0, 4);
            playersForGame.forEach(p => this.playerInQueue.delete(p.playerId));
            this.startMatchmadeGame(playersForGame);
        }
    }

    startMatchmadeGame(players) {
        const sessionId = uuidv4();
        const session = new GameSession(sessionId, players);
        this.gameSessions.set(sessionId, session);

        console.log(`Game ${sessionId} created: ${players.length} players (auto-matched)`);

        for (const player of players) {
            this.playerSessions.set(player.playerId, sessionId);
        }

        session.broadcast({
            type: 'match_found',
            payload: {
                sessionId,
                playerCount: players.length,
                gameState: session.gameState,
                players: Array.from(session.players.entries()).map(([id, p]) => ({
                    playerId: id,
                    name: p.name,
                    playerIndex: p.playerIndex
                }))
            }
        });
    }

    broadcastToQueue(message) {
        this.matchmakingQueue.forEach(player => {
            this.send(player.ws, message);
        });
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
        const queueObject = this.playerInQueue.get(playerId);
        if (queueObject) {
            const index = this.matchmakingQueue.findIndex(p => p.playerId === playerId);
            if (index > -1) {
                this.matchmakingQueue.splice(index, 1);
                this.playerInQueue.delete(playerId);
                console.log(`Player ${playerId} removed from queue. Queue size: ${this.matchmakingQueue.length}`);
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