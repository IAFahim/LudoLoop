const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const GameSession = require('./gameSession');

const PORT = process.env.PORT || 8080;

class LudoGameServer {
    constructor() {
        this.wss = null;
        this.gameSessions = new Map(); // sessionId -> GameSession
        this.playerSessions = new Map(); // playerId -> sessionId
        this.playerConnections = new Map(); // playerId -> ws
    }

    start() {
        this.wss = new WebSocket.Server({ port: PORT });

        this.wss.on('connection', (ws) => {
            console.log('New client connected');
            
            ws.on('message', (message) => {
                try {
                    const data = JSON.parse(message);
                    this.handleMessage(ws, data);
                } catch (error) {
                    console.error('Error parsing message:', error);
                    this.sendError(ws, 'Invalid message format');
                }
            });

            ws.on('close', () => {
                this.handleDisconnect(ws);
            });

            ws.on('error', (error) => {
                console.error('WebSocket error:', error);
            });

            // Send welcome message
            this.send(ws, {
                type: 'connected',
                message: 'Connected to Ludo Game Server'
            });
        });

        console.log(`🎲 Ludo Game Server running on port ${PORT}`);
    }

    handleMessage(ws, data) {
        const { type, payload } = data;

        switch (type) {
            case 'create_game':
                this.handleCreateGame(ws, payload);
                break;
            case 'join_game':
                this.handleJoinGame(ws, payload);
                break;
            case 'start_game':
                this.handleStartGame(ws, payload);
                break;
            case 'roll_dice':
                this.handleRollDice(ws, payload);
                break;
            case 'move_token':
                this.handleMoveToken(ws, payload);
                break;
            case 'get_state':
                this.handleGetState(ws, payload);
                break;
            case 'leave_game':
                this.handleLeaveGame(ws, payload);
                break;
            case 'reconnect':
                this.handleReconnect(ws, payload);
                break;
            case 'list_games':
                this.handleListGames(ws);
                break;
            default:
                this.sendError(ws, `Unknown message type: ${type}`);
        }
    }

    handleCreateGame(ws, payload) {
        const { maxPlayers = 4, playerName } = payload;
        const sessionId = uuidv4();
        const playerId = uuidv4();

        const session = new GameSession(sessionId, maxPlayers);
        const result = session.addPlayer(playerId, ws, playerName);

        if (!result.success) {
            this.sendError(ws, result.error);
            return;
        }

        this.gameSessions.set(sessionId, session);
        this.playerSessions.set(playerId, sessionId);
        this.playerConnections.set(playerId, ws);

        this.send(ws, {
            type: 'game_created',
            payload: {
                sessionId,
                playerId,
                playerIndex: result.playerIndex,
                maxPlayers,
                gameState: session.getGameStateForPlayer(playerId)
            }
        });

        console.log(`Game created: ${sessionId} by player ${playerId}`);
    }

    handleJoinGame(ws, payload) {
        const { sessionId, playerName } = payload;
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Game not found');
            return;
        }

        const playerId = uuidv4();
        const result = session.addPlayer(playerId, ws, playerName);

        if (!result.success) {
            this.sendError(ws, result.error);
            return;
        }

        this.playerSessions.set(playerId, sessionId);
        this.playerConnections.set(playerId, ws);

        // Notify new player
        this.send(ws, {
            type: 'game_joined',
            payload: {
                sessionId,
                playerId,
                playerIndex: result.playerIndex,
                gameState: session.getGameStateForPlayer(playerId)
            }
        });

        // Notify all players
        session.broadcast({
            type: 'player_joined',
            payload: {
                playerId,
                playerIndex: result.playerIndex,
                playerName,
                playerCount: session.players.size
            }
        });

        console.log(`Player ${playerId} joined game ${sessionId}`);
    }

    handleStartGame(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Not in a game');
            return;
        }

        const result = session.startGame();
        if (!result.success) {
            this.sendError(ws, result.error);
            return;
        }

        // Notify all players
        session.broadcast({
            type: 'game_started',
            payload: {
                playerCount: session.players.size,
                currentPlayer: session.gameState.currentPlayer,
                gameState: session.gameState
            }
        });

        console.log(`Game started: ${sessionId}`);
    }

    handleRollDice(ws, payload) {
        const { playerId, diceValue } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Not in a game');
            return;
        }

        const result = session.rollDice(playerId, diceValue);
        if (!result.success) {
            this.sendError(ws, result.error);
            return;
        }

        // Notify all players
        session.broadcast({
            type: 'dice_rolled',
            payload: {
                playerId,
                playerIndex: session.players.get(playerId).playerIndex,
                diceValue: result.diceValue,
                validMoves: result.validMoves,
                noValidMoves: result.noValidMoves,
                nextPlayer: result.nextPlayer
            }
        });

        console.log(`Player ${playerId} rolled ${result.diceValue}`);
    }

    handleMoveToken(ws, payload) {
        const { playerId, tokenIndex } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Not in a game');
            return;
        }

        const result = session.moveToken(playerId, tokenIndex);
        if (!result.success) {
            this.sendError(ws, result.error);
            return;
        }

        // Notify all players
        session.broadcast({
            type: 'token_moved',
            payload: {
                playerId,
                playerIndex: session.players.get(playerId).playerIndex,
                tokenIndex,
                moveResult: result.moveResult,
                message: result.message,
                newPosition: result.newPosition,
                hasWon: result.hasWon,
                turnSwitched: result.turnSwitched,
                nextPlayer: result.nextPlayer,
                gameState: session.gameState
            }
        });

        if (result.hasWon) {
            session.broadcast({
                type: 'game_over',
                payload: {
                    winnerId: playerId,
                    winnerIndex: session.players.get(playerId).playerIndex,
                    winnerName: session.players.get(playerId).name
                }
            });
            console.log(`Player ${playerId} won the game!`);
        }

        console.log(`Player ${playerId} moved token ${tokenIndex}`);
    }

    handleGetState(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Not in a game');
            return;
        }

        this.send(ws, {
            type: 'game_state',
            payload: session.getGameStateForPlayer(playerId)
        });
    }

    handleLeaveGame(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Not in a game');
            return;
        }

        session.removePlayer(playerId);
        this.playerSessions.delete(playerId);
        this.playerConnections.delete(playerId);

        // Notify remaining players
        session.broadcast({
            type: 'player_left',
            payload: {
                playerId,
                playerCount: session.players.size
            }
        });

        // Delete empty sessions
        if (session.players.size === 0) {
            this.gameSessions.delete(sessionId);
            console.log(`Game ${sessionId} deleted (no players)`);
        }

        this.send(ws, {
            type: 'left_game',
            payload: { sessionId }
        });

        console.log(`Player ${playerId} left game ${sessionId}`);
    }

    handleReconnect(ws, payload) {
        const { playerId } = payload;
        const sessionId = this.playerSessions.get(playerId);
        const session = this.gameSessions.get(sessionId);

        if (!session) {
            this.sendError(ws, 'Session not found');
            return;
        }

        if (session.reconnectPlayer(playerId, ws)) {
            this.playerConnections.set(playerId, ws);

            this.send(ws, {
                type: 'reconnected',
                payload: {
                    sessionId,
                    playerId,
                    gameState: session.getGameStateForPlayer(playerId)
                }
            });

            session.broadcast({
                type: 'player_reconnected',
                payload: { playerId }
            }, playerId);

            console.log(`Player ${playerId} reconnected to game ${sessionId}`);
        } else {
            this.sendError(ws, 'Failed to reconnect');
        }
    }

    handleListGames(ws) {
        const games = [];
        for (const [sessionId, session] of this.gameSessions) {
            if (!session.gameState || session.players.size < session.maxPlayers) {
                games.push({
                    sessionId,
                    playerCount: session.players.size,
                    maxPlayers: session.maxPlayers,
                    isStarted: session.gameState !== null,
                    createdAt: session.createdAt
                });
            }
        }

        this.send(ws, {
            type: 'games_list',
            payload: { games }
        });
    }

    handleDisconnect(ws) {
        // Find player by websocket
        for (const [playerId, playerWs] of this.playerConnections) {
            if (playerWs === ws) {
                const sessionId = this.playerSessions.get(playerId);
                const session = this.gameSessions.get(sessionId);

                if (session) {
                    session.disconnectPlayer(playerId);
                    session.broadcast({
                        type: 'player_disconnected',
                        payload: { playerId }
                    }, playerId);

                    console.log(`Player ${playerId} disconnected from game ${sessionId}`);
                }

                this.playerConnections.delete(playerId);
                break;
            }
        }
    }

    send(ws, message) {
        if (ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify(message));
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
