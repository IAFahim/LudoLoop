/**
 * JavaScript implementation of the Ludo Game logic
 * Ported from C# LudoGame.cs
 */

class LudoGameState {
    constructor() {
        this.turnCount = 0;
        this.diceValue = 0;
        this.consecutiveSixes = 0;
        this.currentPlayer = 0;
        this.tokenPositions = new Array(16).fill(-1); // -1 = base
        this.playerCount = 0;
        this.seed = 0;
    }

    static tryCreate(playerCount) {
        if (playerCount < 2) {
            return { success: false, error: 'InsufficientPlayers' };
        }
        if (playerCount > 4) {
            return { success: false, error: 'TooManyPlayers' };
        }

        const state = new LudoGameState();
        state.playerCount = playerCount;
        state.currentPlayer = 0;
        state.consecutiveSixes = 0;
        state.tokenPositions = new Array(16).fill(LudoBoard.POS_BASE);

        return { success: true, state };
    }

    serialize() {
        const buffer = Buffer.alloc(26);
        
        // TurnCount (2 bytes)
        buffer.writeUInt16LE(this.turnCount, 0);
        // DiceValue (1 byte)
        buffer.writeUInt8(this.diceValue, 2);
        // ConsecutiveSixes (1 byte)
        buffer.writeUInt8(this.consecutiveSixes, 3);
        // CurrentPlayer (1 byte)
        buffer.writeUInt8(this.currentPlayer, 4);
        // PlayerCount (1 byte)
        buffer.writeUInt8(this.playerCount, 5);
        // Seed (4 bytes)
        buffer.writeInt32LE(this.seed, 6);
        // TokenPositions (16 bytes)
        for (let i = 0; i < 16; i++) {
            buffer.writeInt8(this.tokenPositions[i], 10 + i);
        }

        return buffer.toString('base64');
    }

    static deserialize(data) {
        const buffer = Buffer.from(data, 'base64');
        const state = new LudoGameState();
        
        state.turnCount = buffer.readUInt16LE(0);
        state.diceValue = buffer.readUInt8(2);
        state.consecutiveSixes = buffer.readUInt8(3);
        state.currentPlayer = buffer.readUInt8(4);
        state.playerCount = buffer.readUInt8(5);
        state.seed = buffer.readInt32LE(6);
        state.tokenPositions = [];
        for (let i = 0; i < 16; i++) {
            state.tokenPositions.push(buffer.readInt8(10 + i));
        }

        return state;
    }
}

class LudoBoard {
    static TOKENS_PER_PLAYER = 4;
    static MAIN_PATH_TILES = 52;
    static POS_BASE = -1;
    static POS_HOME_STRETCH_START = 51;
    static POS_FINISHED = 57;

    static START_OFFSETS = [0, 13, 26, 39]; // R, B, G, Y
    static SAFE_TILES = [0, 13, 26, 39];

    static getValidMoves(state, diceRoll) {
        const player = state.currentPlayer;
        const startIdx = player * this.TOKENS_PER_PLAYER;
        const validMoves = [];

        for (let i = startIdx; i < startIdx + this.TOKENS_PER_PLAYER; i++) {
            const result = this.validateMove(state, i, diceRoll);
            if (this.isSuccess(result)) {
                validMoves.push(i);
            }
        }

        return validMoves;
    }

    static tryProcessMove(state, tokenIndex, diceRoll) {
        // Check if there are any valid moves
        if (this.getValidMoves(state, diceRoll).length === 0) {
            return {
                success: false,
                result: 'InvalidNoValidMoves',
                message: 'There are no possible moves for this roll.'
            };
        }

        // Basic validation
        const basicResult = this.isMoveBasicallyValid(state, tokenIndex);
        if (!basicResult.valid) {
            return {
                success: false,
                result: basicResult.result,
                message: basicResult.message
            };
        }

        const currentPos = state.tokenPositions[tokenIndex];
        const isThirdSix = (diceRoll === 6 && state.consecutiveSixes === 2);

        let moveResult;
        if (currentPos === this.POS_BASE) {
            moveResult = this.tryMoveFromBase(state, tokenIndex, diceRoll, isThirdSix);
        } else {
            moveResult = this.tryPerformNormalMove(state, tokenIndex, diceRoll, currentPos, isThirdSix);
        }

        return moveResult;
    }

    static tryNextTurn(state, moveResult) {
        const rollAgain = moveResult === 'SuccessSix' ||
                         moveResult === 'SuccessRollAgain' ||
                         moveResult === 'SuccessEvictedOpponent';

        if (moveResult === 'SuccessThirdSixPenalty') {
            state.consecutiveSixes = 0;
            state.currentPlayer = (state.currentPlayer + 1) % state.playerCount;
            return true; // Turn switched
        }

        if (rollAgain) {
            if (moveResult === 'SuccessSix') {
                state.consecutiveSixes++;
            } else {
                state.consecutiveSixes = 0;
            }
            return false; // Current player rolls again
        } else {
            state.consecutiveSixes = 0;
            state.currentPlayer = (state.currentPlayer + 1) % state.playerCount;
            return true; // Turn switched
        }
    }

    static hasPlayerWon(state, playerIndex) {
        const startIdx = playerIndex * this.TOKENS_PER_PLAYER;
        for (let i = startIdx; i < startIdx + this.TOKENS_PER_PLAYER; i++) {
            if (state.tokenPositions[i] !== this.POS_FINISHED) {
                return false;
            }
        }
        return true;
    }

    static validateMove(state, tokenIndex, diceRoll) {
        const basicResult = this.isMoveBasicallyValid(state, tokenIndex);
        if (!basicResult.valid) {
            return basicResult.result;
        }

        const tokenColor = Math.floor(tokenIndex / this.TOKENS_PER_PLAYER);
        const currentPos = state.tokenPositions[tokenIndex];

        if (currentPos === this.POS_BASE) {
            if (diceRoll !== 6) return 'InvalidNeedSixToExit';
            
            const startGlobalPos = this.START_OFFSETS[tokenColor];
            if (this.isBlockade(state, startGlobalPos, tokenColor)) {
                return 'InvalidBlockedByBlockade';
            }
        } else {
            const relativePos = this.getRelativePosition(currentPos, tokenColor);
            const newRelativePos = relativePos + diceRoll;

            if (newRelativePos > this.POS_FINISHED) return 'InvalidOvershoot';

            if (this.isPathBlocked(state, tokenColor, relativePos, newRelativePos)) {
                return 'InvalidBlockedByBlockade';
            }
        }

        return 'Success';
    }

    static isMoveBasicallyValid(state, tokenIndex) {
        const tokenColor = Math.floor(tokenIndex / this.TOKENS_PER_PLAYER);

        if (tokenColor !== state.currentPlayer) {
            return {
                valid: false,
                result: 'InvalidNotYourToken',
                message: 'This is not your token.'
            };
        }

        if (state.tokenPositions[tokenIndex] === this.POS_FINISHED) {
            return {
                valid: false,
                result: 'InvalidTokenFinished',
                message: 'This token has already finished.'
            };
        }

        return { valid: true };
    }

    static tryMoveFromBase(state, tokenIndex, diceRoll, isThirdSix) {
        if (diceRoll !== 6) {
            return {
                success: false,
                result: 'InvalidNeedSixToExit',
                message: 'You must roll a 6 to move a token from base.'
            };
        }

        const tokenColor = Math.floor(tokenIndex / this.TOKENS_PER_PLAYER);
        const startGlobalPos = this.START_OFFSETS[tokenColor];

        if (this.isBlockade(state, startGlobalPos, tokenColor)) {
            return {
                success: false,
                result: 'InvalidBlockedByBlockade',
                message: 'Start position is blocked.'
            };
        }

        if (isThirdSix) {
            state.tokenPositions[tokenIndex] = this.START_OFFSETS[Math.floor(tokenIndex / this.TOKENS_PER_PLAYER)];
            return {
                success: true,
                result: 'SuccessThirdSixPenalty',
                message: 'Third six! Turn over.'
            };
        }

        const occupancy = this.analyzeTileOccupancy(state, startGlobalPos);
        let evicted = false;

        if (occupancy.count > 0 && occupancy.color !== tokenColor && !this.isSafeTile(startGlobalPos)) {
            this.evictTokensAt(state, startGlobalPos, occupancy.color);
            evicted = true;
        }

        state.tokenPositions[tokenIndex] = startGlobalPos;

        return {
            success: true,
            result: evicted ? 'SuccessEvictedOpponent' : 'SuccessSix',
            message: evicted ? 'Evicted opponent!' : 'Rolled a 6!'
        };
    }

    static tryPerformNormalMove(state, tokenIndex, diceRoll, currentPos, isThirdSix) {
        const tokenColor = Math.floor(tokenIndex / this.TOKENS_PER_PLAYER);
        const relativePos = this.getRelativePosition(currentPos, tokenColor);
        const newRelativePos = relativePos + diceRoll;

        if (newRelativePos > this.POS_FINISHED) {
            return {
                success: false,
                result: 'InvalidOvershoot',
                message: 'Would overshoot home.'
            };
        }

        if (this.isPathBlocked(state, tokenColor, relativePos, newRelativePos)) {
            return {
                success: false,
                result: 'InvalidBlockedByBlockade',
                message: 'Path is blocked.'
            };
        }

        if (isThirdSix) {
            state.tokenPositions[tokenIndex] = this.getBoardPositionFromRelative(newRelativePos, tokenColor);
            return {
                success: true,
                result: 'SuccessThirdSixPenalty',
                message: 'Third six! Turn over.'
            };
        }

        state.tokenPositions[tokenIndex] = this.getBoardPositionFromRelative(newRelativePos, tokenColor);
        return this.resolveLanding(state, tokenIndex, diceRoll);
    }

    static resolveLanding(state, tokenIndex, diceRoll) {
        const tokenColor = Math.floor(tokenIndex / this.TOKENS_PER_PLAYER);
        const newPos = state.tokenPositions[tokenIndex];

        if (this.getRelativePosition(newPos, tokenColor) === this.POS_FINISHED) {
            return {
                success: true,
                result: 'SuccessRollAgain',
                message: 'Token reached home!'
            };
        }

        const newGlobalPos = this.getGlobalPosition(newPos, tokenColor);
        let evicted = false;

        if (newGlobalPos !== -1) {
            const tempPos = state.tokenPositions[tokenIndex];
            state.tokenPositions[tokenIndex] = -100;
            const occupancy = this.analyzeTileOccupancy(state, newGlobalPos);
            state.tokenPositions[tokenIndex] = tempPos;

            if (occupancy.count > 0 && occupancy.color !== tokenColor && !this.isSafeTile(newGlobalPos)) {
                this.evictTokensAt(state, newGlobalPos, occupancy.color);
                evicted = true;
            }
        }

        if (evicted) {
            return {
                success: true,
                result: 'SuccessEvictedOpponent',
                message: 'Evicted opponent!'
            };
        } else {
            return {
                success: true,
                result: diceRoll === 6 ? 'SuccessSix' : 'Success',
                message: diceRoll === 6 ? 'Rolled a 6!' : 'Move successful.'
            };
        }
    }

    static isBlockade(state, globalPos, movingTokenColor) {
        let count = 0;
        let occupantColor = -1;

        for (let i = 0; i < state.playerCount * this.TOKENS_PER_PLAYER; i++) {
            const tokenColor = Math.floor(i / this.TOKENS_PER_PLAYER);
            const tokenGlobalPos = this.getGlobalPosition(state.tokenPositions[i], tokenColor);

            if (tokenGlobalPos === globalPos) {
                count++;
                occupantColor = tokenColor;
                if (count >= 2) break;
            }
        }

        return count >= 2 && occupantColor !== movingTokenColor && !this.isSafeTile(globalPos);
    }

    static isPathBlocked(state, tokenColor, startRelativePos, endRelativePos) {
        for (let relPos = startRelativePos + 1; relPos <= endRelativePos; relPos++) {
            if (relPos >= this.POS_HOME_STRETCH_START && relPos < this.POS_FINISHED) {
                continue;
            }

            if (relPos === this.POS_FINISHED) {
                continue;
            }

            const boardPos = this.getBoardPositionFromRelative(relPos, tokenColor);
            const globalPos = this.getGlobalPosition(boardPos, tokenColor);

            if (globalPos !== -1 && this.isBlockade(state, globalPos, tokenColor)) {
                return true;
            }
        }

        return false;
    }

    static analyzeTileOccupancy(state, globalPos) {
        let count = 0;
        let color = -1;

        for (let i = 0; i < state.playerCount * this.TOKENS_PER_PLAYER; i++) {
            const tokenColor = Math.floor(i / this.TOKENS_PER_PLAYER);
            if (this.getGlobalPosition(state.tokenPositions[i], tokenColor) === globalPos) {
                count++;
                color = tokenColor;
            }
        }

        return { color, count };
    }

    static evictTokensAt(state, globalPos, victimColor) {
        const startIdx = victimColor * this.TOKENS_PER_PLAYER;
        for (let i = startIdx; i < startIdx + this.TOKENS_PER_PLAYER; i++) {
            if (this.getGlobalPosition(state.tokenPositions[i], victimColor) === globalPos) {
                state.tokenPositions[i] = this.POS_BASE;
            }
        }
    }

    static getRelativePosition(boardPos, color) {
        if (boardPos < 0) return boardPos;
        if (boardPos >= 100) return this.POS_HOME_STRETCH_START + (boardPos - 100 - (6 * color));

        let relative = boardPos - this.START_OFFSETS[color];
        return (relative < 0) ? relative + this.MAIN_PATH_TILES : relative;
    }

    static getBoardPositionFromRelative(relativePos, color) {
        if (relativePos < 0) return relativePos;
        if (relativePos === this.POS_FINISHED) return this.POS_FINISHED;
        if (relativePos >= this.POS_HOME_STRETCH_START) {
            return 100 + (6 * color) + (relativePos - this.POS_HOME_STRETCH_START);
        }

        return (relativePos + this.START_OFFSETS[color]) % this.MAIN_PATH_TILES;
    }

    static getGlobalPosition(boardPos, color) {
        if (boardPos >= 100 || boardPos < 0) return -1;
        return (this.getRelativePosition(boardPos, color) + this.START_OFFSETS[color]) % this.MAIN_PATH_TILES;
    }

    static isSafeTile(globalPos) {
        return this.SAFE_TILES.includes(globalPos);
    }

    static isSuccess(result) {
        return result === 'Success' ||
               result === 'SuccessRollAgain' ||
               result === 'SuccessSix' ||
               result === 'SuccessEvictedOpponent' ||
               result === 'SuccessThirdSixPenalty';
    }
}

module.exports = { LudoGameState, LudoBoard, MoveResult: {} };
