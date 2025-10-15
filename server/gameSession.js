const { LudoGameState, LudoBoard, MoveResult } = require('./ludoGame');

/**
 * Represents a multiplayer Ludo game session
 */
class GameSession {
    constructor(sessionId, maxPlayers = 4) {
        this.sessionId = sessionId;
        this.maxPlayers = maxPlayers;
        this.players = new Map(); // playerId -> { ws, playerIndex, name }
        this.gameState = null;
        this.currentDiceRoll = 0;
        this.isWaitingForMove = false;
        this.createdAt = Date.now();
        this.startedAt = null;
        this.winnerId = null;
    }

    /**
     * Add a player to the game session
     */
    addPlayer(playerId, ws, playerName = null) {
        if (this.players.size >= this.maxPlayers) {
            return { success: false, error: 'Game is full' };
        }

        if (this.players.has(playerId)) {
            return { success: false, error: 'Player already in game' };
        }

        const playerIndex = this.players.size;
        this.players.set(playerId, {
            ws,
            playerIndex,
            name: playerName || `Player ${playerIndex}`,
            connected: true
        });

        return { success: true, playerIndex };
    }

    /**
     * Remove a player from the game
     */
    removePlayer(playerId) {
        return this.players.delete(playerId);
    }

    /**
     * Mark player as disconnected
     */
    disconnectPlayer(playerId) {
        const player = this.players.get(playerId);
        if (player) {
            player.connected = false;
        }
    }

    /**
     * Reconnect a player
     */
    reconnectPlayer(playerId, ws) {
        const player = this.players.get(playerId);
        if (player) {
            player.ws = ws;
            player.connected = true;
            return true;
        }
        return false;
    }

    /**
     * Start the game
     */
    startGame() {
        if (this.players.size < 2) {
            return { success: false, error: 'Need at least 2 players' };
        }

        const result = LudoGameState.tryCreate(this.players.size);
        if (!result.success) {
            return { success: false, error: result.error };
        }

        this.gameState = result.state;
        this.startedAt = Date.now();
        return { success: true };
    }

    /**
     * Process a dice roll
     */
    rollDice(playerId, diceValue = null) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: 'Player not in game' };
        }

        if (!this.gameState) {
            return { success: false, error: 'Game not started' };
        }

        if (player.playerIndex !== this.gameState.currentPlayer) {
            return { success: false, error: 'Not your turn' };
        }

        if (this.isWaitingForMove) {
            return { success: false, error: 'Already waiting for move' };
        }

        // Use provided dice value or generate random 1-6
        const roll = diceValue || Math.floor(Math.random() * 6) + 1;
        this.currentDiceRoll = roll;

        // Get valid moves
        const validMoves = LudoBoard.getValidMoves(this.gameState, roll);

        if (validMoves.length === 0) {
            // No valid moves, skip turn
            this.gameState.consecutiveSixes = 0;
            this.gameState.currentPlayer = (this.gameState.currentPlayer + 1) % this.players.size;
            this.currentDiceRoll = 0;
            return {
                success: true,
                diceValue: roll,
                validMoves: [],
                noValidMoves: true,
                nextPlayer: this.gameState.currentPlayer
            };
        }

        this.isWaitingForMove = true;
        return {
            success: true,
            diceValue: roll,
            validMoves,
            noValidMoves: false
        };
    }

    /**
     * Move a token
     */
    moveToken(playerId, tokenIndex) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: 'Player not in game' };
        }

        if (!this.isWaitingForMove) {
            return { success: false, error: 'No dice roll active' };
        }

        if (player.playerIndex !== this.gameState.currentPlayer) {
            return { success: false, error: 'Not your turn' };
        }

        const result = LudoBoard.tryProcessMove(
            this.gameState,
            tokenIndex,
            this.currentDiceRoll
        );

        this.isWaitingForMove = false;

        if (!result.success) {
            return {
                success: false,
                error: result.message,
                moveResult: result.result
            };
        }

        // Check for win
        const hasWon = LudoBoard.hasPlayerWon(this.gameState, player.playerIndex);
        if (hasWon) {
            this.winnerId = playerId;
        }

        // Advance turn
        const turnSwitched = LudoBoard.tryNextTurn(this.gameState, result.result);

        this.currentDiceRoll = 0;

        return {
            success: true,
            moveResult: result.result,
            message: result.message,
            newPosition: this.gameState.tokenPositions[tokenIndex],
            hasWon,
            turnSwitched,
            nextPlayer: this.gameState.currentPlayer
        };
    }

    /**
     * Get game state for a specific player
     */
    getGameStateForPlayer(playerId) {
        const player = this.players.get(playerId);
        if (!player) {
            return null;
        }

        return {
            sessionId: this.sessionId,
            playerIndex: player.playerIndex,
            playerCount: this.players.size,
            currentPlayer: this.gameState?.currentPlayer,
            gameState: this.gameState ? {
                turnCount: this.gameState.turnCount,
                diceValue: this.gameState.diceValue,
                consecutiveSixes: this.gameState.consecutiveSixes,
                currentPlayer: this.gameState.currentPlayer,
                tokenPositions: this.gameState.tokenPositions,
                playerCount: this.gameState.playerCount
            } : null,
            players: Array.from(this.players.entries()).map(([id, p]) => ({
                playerId: id,
                playerIndex: p.playerIndex,
                name: p.name,
                connected: p.connected
            })),
            isStarted: this.gameState !== null,
            winnerId: this.winnerId,
            isWaitingForMove: this.isWaitingForMove,
            currentDiceRoll: this.currentDiceRoll
        };
    }

    /**
     * Broadcast message to all players
     */
    broadcast(message, excludePlayerId = null) {
        for (const [playerId, player] of this.players) {
            if (playerId !== excludePlayerId && player.connected && player.ws.readyState === 1) {
                player.ws.send(JSON.stringify(message));
            }
        }
    }

    /**
     * Send message to specific player
     */
    sendToPlayer(playerId, message) {
        const player = this.players.get(playerId);
        if (player && player.connected && player.ws.readyState === 1) {
            player.ws.send(JSON.stringify(message));
        }
    }

    /**
     * Get serialized game state
     */
    serialize() {
        if (!this.gameState) return null;
        return LudoGameState.serialize(this.gameState);
    }

    /**
     * Load from serialized state
     */
    deserialize(data) {
        this.gameState = LudoGameState.deserialize(data);
    }
}

module.exports = GameSession;
