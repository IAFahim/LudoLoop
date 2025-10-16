// gameSession.js
const { v4: uuidv4 } = require('uuid');

// --- Direct Port of C# LudoBoard Constants ---
const LudoBoard = {
    TokensPerPlayer: 4,
    MainPathTiles: 52,
    PosBase: -1,
    PosHomeStretchStart: 51,
    PosFinished: 57,
    StartOffsets: [0, 13, 26, 39], // R, B, G, Y
    SafeTiles: [0, 13, 26, 39]
};

// --- Direct Port of C# MoveResult Enum ---
const MoveResult = {
    Success: 0,
    SuccessRollAgain: 1,
    SuccessSix: 2,
    SuccessEvictedOpponent: 3,
    SuccessThirdSixPenalty: 4,
    InvalidTokenFinished: 5,
    InvalidNeedSixToExit: 6,
    InvalidOvershoot: 7,
    InvalidNotYourToken: 8,
    InvalidNoValidMoves: 9,
    InvalidBlockedByBlockade: 10
};

const MoveResultNames = {
    0: 'Success',
    1: 'SuccessRollAgain',
    2: 'SuccessSix',
    3: 'SuccessEvictedOpponent',
    4: 'SuccessThirdSixPenalty',
    5: 'InvalidTokenFinished',
    6: 'InvalidNeedSixToExit',
    7: 'InvalidOvershoot',
    8: 'InvalidNotYourToken',
    9: 'InvalidNoValidMoves',
    10: 'InvalidBlockedByBlockade'
};

// ================================================
// LUDO LOGIC - Ported from C# LudoBoard class
// ================================================

/**
 * Creates a new game state object.
 */
function createGameState(playerCount) {
    if (playerCount < 2 || playerCount > 4) {
        return null;
    }
    const tokenPositions = new Array(16).fill(LudoBoard.PosBase);
    return {
        turnCount: 0,
        diceValue: 0,
        consecutiveSixes: 0,
        currentPlayer: 0,
        playerCount: playerCount,
        tokenPositions: tokenPositions,
    };
}

/**
 * Gets all valid token moves for a given roll.
 */
function getValidMoves(state, diceRoll) {
    const player = state.currentPlayer;
    const startIdx = player * LudoBoard.TokensPerPlayer;
    const validMoves = [];

    for (let i = startIdx; i < startIdx + LudoBoard.TokensPerPlayer; i++) {
        const result = validateMove(state, i, diceRoll);
        if (result <= MoveResult.SuccessThirdSixPenalty) { // Is a success state
            validMoves.push(i);
        }
    }
    return validMoves;
}

/**
 * Processes a move, mutating the state.
 */
function tryProcessMove(state, tokenIndex, diceRoll) {
    if (getValidMoves(state, diceRoll).length === 0) {
        return { success: false, result: MoveResult.InvalidNoValidMoves };
    }

    const basicValidation = isMoveBasicallyValid(state, tokenIndex);
    if (!basicValidation.success) {
        return { success: false, result: basicValidation.result };
    }

    const currentPos = state.tokenPositions[tokenIndex];
    const isThirdSix = (diceRoll === 6 && state.consecutiveSixes === 2);

    let result;
    if (currentPos === LudoBoard.PosBase) {
        result = tryMoveFromBase(state, tokenIndex, diceRoll, isThirdSix);
    } else {
        result = tryPerformNormalMove(state, tokenIndex, diceRoll, currentPos, isThirdSix);
    }

    return { success: result <= MoveResult.SuccessThirdSixPenalty, result: result };
}

/**
 * Determines the next player based on move result, mutating state.
 * Returns true if turn switched.
 */
function tryNextTurn(state, moveResult) {
    const rollAgain = moveResult === MoveResult.SuccessSix ||
        moveResult === MoveResult.SuccessRollAgain ||
        moveResult === MoveResult.SuccessEvictedOpponent;

    if (moveResult === MoveResult.SuccessThirdSixPenalty || moveResult === MoveResult.InvalidNoValidMoves) {
        state.consecutiveSixes = 0;
        state.currentPlayer = (state.currentPlayer + 1) % state.playerCount;
        return true;
    }

    if (rollAgain) {
        state.consecutiveSixes = (moveResult === MoveResult.SuccessSix) ? state.consecutiveSixes + 1 : 0;
        return false;
    } else {
        state.consecutiveSixes = 0;
        state.currentPlayer = (state.currentPlayer + 1) % state.playerCount;
        return true;
    }
}

/**
 * Checks if a player has all tokens in the finished position.
 */
function hasPlayerWon(state, playerIndex) {
    const startIdx = playerIndex * LudoBoard.TokensPerPlayer;
    for (let i = startIdx; i < startIdx + LudoBoard.TokensPerPlayer; i++) {
        if (state.tokenPositions[i] !== LudoBoard.PosFinished) return false;
    }
    return true;
}

// --- All Helper Functions ---

function validateMove(state, tokenIndex, diceRoll) {
    const basicValidation = isMoveBasicallyValid(state, tokenIndex);
    if (!basicValidation.success) return basicValidation.result;

    const tokenColor = Math.floor(tokenIndex / LudoBoard.TokensPerPlayer);
    const currentPos = state.tokenPositions[tokenIndex];

    if (currentPos === LudoBoard.PosBase) {
        if (diceRoll !== 6) return MoveResult.InvalidNeedSixToExit;
        const startGlobalPos = LudoBoard.StartOffsets[tokenColor];
        if (isBlockade(state, startGlobalPos, tokenColor)) return MoveResult.InvalidBlockedByBlockade;
    } else {
        const relativePos = getRelativePosition(currentPos, tokenColor);
        const newRelativePos = relativePos + diceRoll;
        if (newRelativePos > LudoBoard.PosFinished) return MoveResult.InvalidOvershoot;
        if (isPathBlocked(state, tokenColor, relativePos, newRelativePos)) return MoveResult.InvalidBlockedByBlockade;
    }
    return MoveResult.Success;
}

function isBlockade(state, globalPos, movingTokenColor) {
    const { color, count } = analyzeTileOccupancy(state, globalPos);
    return count >= 2 && color !== movingTokenColor && !LudoBoard.SafeTiles.includes(globalPos);
}

function isPathBlocked(state, tokenColor, startRelativePos, endRelativePos) {
    for (let relPos = startRelativePos + 1; relPos <= endRelativePos; relPos++) {
        if (relPos >= LudoBoard.PosHomeStretchStart) continue;
        const boardPos = getBoardPositionFromRelative(relPos, tokenColor);
        const globalPos = getGlobalPosition(boardPos, tokenColor);
        if (globalPos !== -1 && isBlockade(state, globalPos, tokenColor)) return true;
    }
    return false;
}

function analyzeTileOccupancy(state, globalPos) {
    let occupantCount = 0;
    let occupantColor = -1;
    for (let i = 0; i < state.playerCount * LudoBoard.TokensPerPlayer; i++) {
        const tokenColor = Math.floor(i / LudoBoard.TokensPerPlayer);
        if (getGlobalPosition(state.tokenPositions[i], tokenColor) === globalPos) {
            occupantCount++;
            occupantColor = tokenColor;
        }
    }
    return { color: occupantColor, count: occupantCount };
}

function isMoveBasicallyValid(state, tokenIndex) {
    const tokenColor = Math.floor(tokenIndex / LudoBoard.TokensPerPlayer);
    if (tokenColor !== state.currentPlayer) return { success: false, result: MoveResult.InvalidNotYourToken };
    if (state.tokenPositions[tokenIndex] === LudoBoard.PosFinished) return { success: false, result: MoveResult.InvalidTokenFinished };
    return { success: true };
}

function tryMoveFromBase(state, tokenIndex, diceRoll, isThirdSix) {
    if (diceRoll !== 6) return MoveResult.InvalidNeedSixToExit;
    const tokenColor = Math.floor(tokenIndex / LudoBoard.TokensPerPlayer);
    const startGlobalPos = LudoBoard.StartOffsets[tokenColor];

    if (isBlockade(state, startGlobalPos, tokenColor)) return MoveResult.InvalidBlockedByBlockade;

    state.tokenPositions[tokenIndex] = startGlobalPos;
    if (isThirdSix) return MoveResult.SuccessThirdSixPenalty;

    const { color, count } = analyzeTileOccupancy(state, startGlobalPos);
    let evicted = false;
    if (count > 0 && color !== tokenColor && !LudoBoard.SafeTiles.includes(startGlobalPos)) {
        evictTokensAt(state, startGlobalPos, color);
        evicted = true;
    }
    return evicted ? MoveResult.SuccessEvictedOpponent : MoveResult.SuccessSix;
}

function tryPerformNormalMove(state, tokenIndex, diceRoll, currentPos, isThirdSix) {
    const tokenColor = Math.floor(tokenIndex / LudoBoard.TokensPerPlayer);
    const relativePos = getRelativePosition(currentPos, tokenColor);
    const newRelativePos = relativePos + diceRoll;

    if (newRelativePos > LudoBoard.PosFinished) return MoveResult.InvalidOvershoot;
    if (isPathBlocked(state, tokenColor, relativePos, newRelativePos)) return MoveResult.InvalidBlockedByBlockade;

    state.tokenPositions[tokenIndex] = getBoardPositionFromRelative(newRelativePos, tokenColor);

    if (isThirdSix) return MoveResult.SuccessThirdSixPenalty;
    return resolveLanding(state, tokenIndex, diceRoll);
}

function resolveLanding(state, tokenIndex, diceRoll) {
    const tokenColor = Math.floor(tokenIndex / LudoBoard.TokensPerPlayer);
    const newPos = state.tokenPositions[tokenIndex];

    if (getRelativePosition(newPos, tokenColor) === LudoBoard.PosFinished) return MoveResult.SuccessRollAgain;

    const newGlobalPos = getGlobalPosition(newPos, tokenColor);
    let evicted = false;

    if (newGlobalPos !== -1 && !LudoBoard.SafeTiles.includes(newGlobalPos)) {
        state.tokenPositions[tokenIndex] = -100;
        const { color, count } = analyzeTileOccupancy(state, newGlobalPos);
        state.tokenPositions[tokenIndex] = newPos;

        if (count > 0 && color !== tokenColor) {
            evictTokensAt(state, newGlobalPos, color);
            evicted = true;
        }
    }
    if (evicted) return MoveResult.SuccessEvictedOpponent;
    return diceRoll === 6 ? MoveResult.SuccessSix : MoveResult.Success;
}

function evictTokensAt(state, globalPos, victimColor) {
    const startIdx = victimColor * LudoBoard.TokensPerPlayer;
    for (let i = startIdx; i < startIdx + LudoBoard.TokensPerPlayer; i++) {
        if (getGlobalPosition(state.tokenPositions[i], victimColor) === globalPos) {
            state.tokenPositions[i] = LudoBoard.PosBase;
        }
    }
}

function getRelativePosition(boardPos, color) {
    if (boardPos < 0) return boardPos;
    if (boardPos >= 100) return LudoBoard.PosHomeStretchStart + (boardPos - 100 - (6 * color));
    const relative = boardPos - LudoBoard.StartOffsets[color];
    return (relative < 0) ? relative + LudoBoard.MainPathTiles : relative;
}

function getBoardPositionFromRelative(relativePos, color) {
    if (relativePos < 0) return relativePos;
    if (relativePos === LudoBoard.PosFinished) return LudoBoard.PosFinished;
    if (relativePos >= LudoBoard.PosHomeStretchStart) return (100 + (6 * color) + (relativePos - LudoBoard.PosHomeStretchStart));
    return ((relativePos + LudoBoard.StartOffsets[color]) % LudoBoard.MainPathTiles);
}

function getGlobalPosition(boardPos, color) {
    if (boardPos >= 100 || boardPos < 0) return -1;
    return (getRelativePosition(boardPos, color) + LudoBoard.StartOffsets[color]) % LudoBoard.MainPathTiles;
}

// ======================================================
// GAME SESSION CLASS - Manages players and game state
// ======================================================
class GameSession {
    constructor(sessionId, players) {
        this.sessionId = sessionId;
        this.players = new Map(); // playerId -> { name, ws, playerIndex, connected, isAFK }
        this.gameState = createGameState(players.length);
        this.createdAt = new Date();
        this.lastActivityTime = new Date();
        this.winnerId = null;
        this.isGameOver = false;

        players.forEach((player, index) => {
            this.players.set(player.playerId, {
                name: player.playerName,
                ws: player.ws,
                playerIndex: index,
                connected: true,
                isAFK: false
            });
        });

        console.log(`GameSession ${sessionId} created with ${players.length} players`);
    }

    /**
     * Simulates a dice roll and determines the consequences.
     */
    rollDice(playerId, forcedValue = 0) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: "Player not in this game." };
        }

        if (player.playerIndex !== this.gameState.currentPlayer) {
            return { success: false, error: "Not your turn." };
        }

        if (this.isGameOver) {
            return { success: false, error: "Game is over." };
        }

        const diceValue = (forcedValue > 0 && forcedValue <= 6) ? forcedValue : Math.floor(Math.random() * 6) + 1;
        this.gameState.diceValue = diceValue;
        this.lastActivityTime = new Date();

        const validMoves = getValidMoves(this.gameState, diceValue);

        let turnSwitched = false;
        if (validMoves.length === 0) {
            turnSwitched = tryNextTurn(this.gameState, MoveResult.InvalidNoValidMoves);
            this.gameState.diceValue = 0;
        }

        return {
            success: true,
            playerId,
            playerIndex: player.playerIndex,
            diceValue,
            validMoves,
            noValidMoves: validMoves.length === 0,
            turnSwitched,
            nextPlayer: this.gameState.currentPlayer
        };
    }

    /**
     * Attempts to move a token.
     */
    moveToken(playerId, tokenIndex) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: "Player not in this game." };
        }

        if (player.playerIndex !== this.gameState.currentPlayer) {
            return { success: false, error: "Not your turn." };
        }

        if (this.isGameOver) {
            return { success: false, error: "Game is over." };
        }

        if (this.gameState.diceValue === 0) {
            return { success: false, error: "You must roll the dice first." };
        }

        const moveOutcome = tryProcessMove(this.gameState, tokenIndex, this.gameState.diceValue);

        if (!moveOutcome.success) {
            return { 
                success: false, 
                error: `Invalid move: ${MoveResultNames[moveOutcome.result]}`
            };
        }

        const currentPlayerBeforeMove = this.gameState.currentPlayer;
        const hasWon = hasPlayerWon(this.gameState, currentPlayerBeforeMove);
        
        if (hasWon) {
            this.winnerId = playerId;
            this.isGameOver = true;
        }

        const turnSwitched = tryNextTurn(this.gameState, moveOutcome.result);
        
        this.gameState.turnCount++;
        const diceValueForResponse = this.gameState.diceValue;
        this.gameState.diceValue = 0;
        this.lastActivityTime = new Date();

        return {
            success: true,
            playerId,
            playerIndex: player.playerIndex,
            tokenIndex,
            moveResult: MoveResultNames[moveOutcome.result],
            moveResultCode: moveOutcome.result,
            diceValue: diceValueForResponse,
            newPosition: this.gameState.tokenPositions[tokenIndex],
            hasWon,
            turnSwitched,
            nextPlayer: this.gameState.currentPlayer,
            gameState: this.gameState
        };
    }

    /**
     * Get current game state for a player
     */
    getGameState(playerId) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: "Player not in this game." };
        }

        return {
            success: true,
            sessionId: this.sessionId,
            playerIndex: player.playerIndex,
            playerCount: this.gameState.playerCount,
            currentPlayer: this.gameState.currentPlayer,
            gameState: this.gameState,
            players: Array.from(this.players.entries()).map(([id, p]) => ({
                playerId: id,
                name: p.name,
                playerIndex: p.playerIndex,
                connected: p.connected,
                isAFK: p.isAFK
            })),
            isGameOver: this.isGameOver,
            winnerId: this.winnerId
        };
    }

    /**
     * Disconnect a player
     */
    disconnectPlayer(playerId) {
        const player = this.players.get(playerId);
        if (player) {
            player.connected = false;
            player.ws = null;
            console.log(`Player ${playerId} disconnected from game ${this.sessionId}`);
        }
    }

    /**
     * Reconnect a player
     */
    reconnectPlayer(playerId, ws) {
        const player = this.players.get(playerId);
        if (!player) {
            return { success: false, error: "Player not found in this game." };
        }

        player.connected = true;
        player.ws = ws;
        player.isAFK = false;
        this.lastActivityTime = new Date();

        console.log(`Player ${playerId} reconnected to game ${this.sessionId}`);

        return {
            success: true,
            sessionId: this.sessionId,
            playerIndex: player.playerIndex,
            gameState: this.gameState,
            players: Array.from(this.players.entries()).map(([id, p]) => ({
                playerId: id,
                name: p.name,
                playerIndex: p.playerIndex,
                connected: p.connected
            }))
        };
    }

    /**
     * Remove a player from the game
     */
    removePlayer(playerId) {
        const player = this.players.get(playerId);
        if (player) {
            this.players.delete(playerId);
            console.log(`Player ${playerId} removed from game ${this.sessionId}`);
            return true;
        }
        return false;
    }

    /**
     * Check if all players are disconnected
     */
    allPlayersDisconnected() {
        for (const player of this.players.values()) {
            if (player.connected) return false;
        }
        return true;
    }

    /**
     * Broadcast message to all players, optionally excluding one
     */
    broadcast(message, excludePlayerId = null) {
        for (const [playerId, player] of this.players.entries()) {
            if (playerId === excludePlayerId) continue;
            if (player.ws && player.ws.readyState === 1) { // WebSocket.OPEN
                try {
                    player.ws.send(JSON.stringify(message));
                } catch (error) {
                    console.error(`Error broadcasting to player ${playerId}:`, error);
                }
            }
        }
    }

    /**
     * Send message to a specific player
     */
    sendToPlayer(playerId, message) {
        const player = this.players.get(playerId);
        if (player && player.ws && player.ws.readyState === 1) {
            try {
                player.ws.send(JSON.stringify(message));
            } catch (error) {
                console.error(`Error sending to player ${playerId}:`, error);
            }
        }
    }
}

module.exports = GameSession;