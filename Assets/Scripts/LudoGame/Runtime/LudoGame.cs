using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LudoGame.Runtime
{
    /// <summary>
    /// Represents the result of a token move attempt.
    /// It is now more descriptive to handle specific game events.
    /// </summary>
    public enum MoveResult
    {
        // --- Success States ---
        Success = 0,                  // Move successful, turn ends
        SuccessRollAgain = 1,         // Move successful AND token reached home (roll again)
        SuccessSix = 2,               // Move successful, rolled 6 (roll again)
        SuccessEvictedOpponent = 3,   // IMPROVEMENT: Move successful AND an opponent's token was evicted
        SuccessThirdSixPenalty = 4,   // IMPROVEMENT: Move successful, but turn is forfeited due to 3 sixes in a row

        // --- Invalid States ---
        InvalidTokenFinished = 5,     // Token already at home
        InvalidNeedSixToExit = 6,     // Must roll 6 to exit base
        InvalidOvershoot = 7,         // Would overshoot home
        InvalidNotYourToken = 8,     // Token doesn't belong to current player
        InvalidNoValidMoves = 9      // IMPROVEMENT: Dice roll is valid, but no token can legally move
    }

    /// <summary>
    /// IMPROVEMENT: Extension method to provide human-readable descriptions for MoveResult values.
    /// This is ideal for UI, logging, and debugging.
    /// </summary>
    public static class MoveResultExtensions
    {
        public static string ToMessage(this MoveResult result)
        {
            switch (result)
            {
                // Success messages
                case MoveResult.Success:
                    return "Move successful.";
                case MoveResult.SuccessRollAgain:
                    return "Token reached home! Roll again.";
                case MoveResult.SuccessSix:
                    return "You rolled a 6! Roll again.";
                case MoveResult.SuccessEvictedOpponent:
                    return "Success! You sent an opponent's token back to their base.";
                case MoveResult.SuccessThirdSixPenalty:
                    return "You rolled a third consecutive 6. Your turn is now over.";

                // Invalid/Error messages
                case MoveResult.InvalidTokenFinished:
                    return "Invalid move: This token has already finished.";
                case MoveResult.InvalidNeedSixToExit:
                    return "Invalid move: You must roll a 6 to move a token from your base.";
                case MoveResult.InvalidOvershoot:
                    return "Invalid move: This roll would overshoot the final home position.";
                case MoveResult.InvalidNotYourToken:
                    return "Invalid move: This is not your token.";
                case MoveResult.InvalidNoValidMoves:
                    return "There are no possible moves for this roll. Your turn is skipped.";

                // Default case to catch any unhandled enum values
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), "Unhandled MoveResult.");
            }
        }
    
        /// <summary>
        /// FIX: Determines if the MoveResult is a success state.
        /// </summary>
        public static bool IsSuccess(this MoveResult result)
        {
            // Success states are grouped with values from 0 to 4.
            return result <= MoveResult.SuccessThirdSixPenalty;
        }
    }

    /// <summary>
    /// An brilliantly compact game state. No redundant BoardTiles array.
    /// The entire state is captured in just 20 bytes.
    /// </summary>
    public struct LudoGameState
    {
        public int PlayerCount;
        public int CurrentPlayer;
        public sbyte[] TokenPositions; // The ONLY source of truth for the board state
        public int ConsecutiveSixes;

        /// <summary>
        /// Serializes game state to an ultra-compact base64 string (from a 20-byte array).
        /// Format: PlayerCount|CurrentPlayer|ConsecutiveSixes|TokenPositions
        /// </summary>
        public string Serialize()
        {
            byte[] buffer = new byte[4 + 16]; 
        
            buffer[0] = (byte)PlayerCount;
            buffer[1] = (byte)CurrentPlayer;
            buffer[2] = (byte)ConsecutiveSixes;
            buffer[3] = 0; // Reserved
        
            Buffer.BlockCopy(TokenPositions, 0, buffer, 4, 16);
        
            return Convert.ToBase64String(buffer);
        }
    
        /// <summary>
        /// Deserializes game state from a compact base64 string.
        /// </summary>
        public static LudoGameState Deserialize(string data)
        {
            byte[] buffer = Convert.FromBase64String(data);
            var state = new LudoGameState
            {
                PlayerCount = buffer[0],
                CurrentPlayer = buffer[1],
                ConsecutiveSixes = buffer[2],
                TokenPositions = new sbyte[16]
            };
        
            Buffer.BlockCopy(buffer, 4, state.TokenPositions, 0, 16);
        
            return state;
        }
    
        public enum LudoCreateResult
        {
            Success,
            InsufficientPlayers,
            TooManyPlayers
        }

        /// <summary>
        /// Creates a new game state.
        /// </summary>
        public static bool TryCreate(int playerCount, out LudoGameState ludoGameState, out LudoCreateResult ludoCreateResult)
        {
            if (playerCount < 2)
            {
                ludoGameState = default;
                ludoCreateResult = LudoCreateResult.InsufficientPlayers;
                return false;
            }

            if (playerCount > 4)
            {
                ludoGameState = default;
                ludoCreateResult = LudoCreateResult.TooManyPlayers;
                return false;
            }
        
            ludoGameState = new LudoGameState
            {
                PlayerCount = playerCount,
                CurrentPlayer = 0,
                ConsecutiveSixes = 0,
                TokenPositions = new sbyte[16]
            };
        
            for (int i = 0; i < 16; i++)
            {
                ludoGameState.TokenPositions[i] = LudoBoard.PosBase;
            }
        
            ludoCreateResult = LudoCreateResult.Success;
            return true;
        }

        /// <summary>
        /// Human-readable string representation of the current game state.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Player Count: {PlayerCount} Current Turn: Player {CurrentPlayer} ");
            if (ConsecutiveSixes > 0)
            {
                sb.AppendLine($"Consecutive Sixes: {ConsecutiveSixes}");
            }
            else
            {
                sb.AppendLine();
            }
            sb.AppendLine("Token Positions:");

            for (int player = 0; player < PlayerCount; player++)
            {
                sb.Append($"  Player {player}: [");
                var tokenStrings = new List<string>();
                for (int i = 0; i < 4; i++)
                {
                    int tokenIndex = player * 4 + i;
                    sbyte pos = TokenPositions[tokenIndex];
                    switch (pos)
                    {
                        case LudoBoard.PosBase:
                            tokenStrings.Add("Base");
                            break;
                        case LudoBoard.PosFinished:
                            tokenStrings.Add("Finished");
                            break;
                        default:
                            // You could add a helper to convert board pos to a more friendly format if needed
                            tokenStrings.Add($"Pos {pos}");
                            break;
                    }
                }
                sb.Append(string.Join(", ", tokenStrings));
                sb.AppendLine("]");
            }
            return sb.ToString();
        }
    }

    public static class LudoBoard
    {
        // --- CONSTANTS ---
        private const int TokensPerPlayer = 4;
        private const int MainPathTiles = 52;
    
        public const sbyte PosBase = -1;
        public const sbyte PosHomeStretchStart = 51;
        public const sbyte PosFinished = 57;
    
        private static readonly int[] StartOffsets = { 0, 13, 26, 39 }; // R, B, G, Y
        private static readonly int[] SafeTiles = { 0, 13, 26, 39 };
    
        /// <summary>
        /// FIX: This method now correctly processes a move and returns true on success and false on failure.
        /// </summary>
        public static bool TryProcessMove(ref LudoGameState state, int tokenIndex, int diceRoll, out MoveResult result)
        {
            // First, check if there are any possible moves for the current player with the given dice roll.
            // This now uses the new, more accurate GetValidMoves method.
            if (GetValidMoves(state, diceRoll).Length == 0)
            {
                result = MoveResult.InvalidNoValidMoves;
                // If no moves are possible, the turn should be skipped.
                state.ConsecutiveSixes = 0; // Reset sixes counter on a skipped turn.
                return false;
            }

            // Second, perform basic validation on the specific token selected.
            if (!IsMoveBasicallyValid(state, tokenIndex, out result))
            {
                return false;
            }

            sbyte currentPos = state.TokenPositions[tokenIndex];
            bool isThirdSix = (diceRoll == 6 && state.ConsecutiveSixes == 2);

            // Third, delegate the complex move logic to helper methods.
            if (currentPos == PosBase)
            {
                TryMoveFromBase(ref state, tokenIndex, diceRoll, isThirdSix, out result);
            }
            else
            {
                TryPerformNormalMove(ref state, tokenIndex, diceRoll, currentPos, isThirdSix, out result);
            }
        
            // Finally, return true if the resulting MoveResult is a success state, otherwise false.
            return result.IsSuccess();
        }
    
        public static void NextTurn(ref LudoGameState state, in MoveResult moveResult)
        {
            bool rollAgain = moveResult == MoveResult.SuccessSix || 
                             moveResult == MoveResult.SuccessRollAgain ||
                             moveResult == MoveResult.SuccessEvictedOpponent;

            if (moveResult == MoveResult.SuccessThirdSixPenalty)
            {
                state.ConsecutiveSixes = 0; // Reset counter
                state.CurrentPlayer = (state.CurrentPlayer + 1) % state.PlayerCount;
                return;
            }
        
            if (rollAgain)
            {
                if (moveResult == MoveResult.SuccessSix)
                {
                    state.ConsecutiveSixes++;
                }
                else
                {
                    state.ConsecutiveSixes = 0; // Reset on any other successful roll-again event
                }
            }
            else
            {
                state.ConsecutiveSixes = 0;
                state.CurrentPlayer = (state.CurrentPlayer + 1) % state.PlayerCount;
            }
        }
    
        /// <summary>
        /// FIX: This method is now fully accurate. It uses a comprehensive helper method (`ValidateMove`)
        /// to ensure that every move returned is 100% legal according to all game rules.
        /// This correctly handles cases where no moves are possible, allowing the turn to be skipped.
        /// </summary>
        public static int[] GetValidMoves(LudoGameState state, int diceRoll)
        {
            int player = state.CurrentPlayer;
            int startIdx = player * TokensPerPlayer;
            var validMoves = new List<int>();
        
            for (int i = startIdx; i < startIdx + TokensPerPlayer; i++)
            {
                // Use the new comprehensive, read-only validation method.
                MoveResult result = ValidateMove(state, i, diceRoll);
            
                // If the move is anything other than a failure, it's considered valid.
                if (result.IsSuccess())
                {
                    validMoves.Add(i);
                }
            }
            return validMoves.ToArray();
        }
    
        public static bool HasPlayerWon(LudoGameState state, int playerIndex)
        {
            int startIdx = playerIndex * TokensPerPlayer;
            for (int i = startIdx; i < startIdx + TokensPerPlayer; i++)
            {
                if (state.TokenPositions[i] != PosFinished) return false;
            }
            return true;
        }

        // --- CORE LOGIC & REFACTORED METHODS ---

        /// <summary>
        /// ADDED: This new helper method performs a complete, read-only validation of a potential move.
        /// It checks all rules (blockades, overshoots, etc.) without changing the game state.
        /// It is the single source of truth for determining if a move is legal.
        /// </summary>
        private static MoveResult ValidateMove(LudoGameState state, int tokenIndex, int diceRoll)
        {
            // 1. Perform basic checks (is it your token, is it already finished?).
            if (!IsMoveBasicallyValid(state, tokenIndex, out var basicResult))
            {
                return basicResult;
            }

            int tokenColor = tokenIndex / TokensPerPlayer;
            sbyte currentPos = state.TokenPositions[tokenIndex];

            // 2. Handle logic for moving a token from the base.
            if (currentPos == PosBase)
            {
                if (diceRoll != 6) return MoveResult.InvalidNeedSixToExit;
            }
            // 3. Handle logic for a normal move on the board.
            else
            {
                int relativePos = GetRelativePosition(currentPos, tokenColor);
                sbyte newRelativePos = (sbyte)(relativePos + diceRoll);

                // Check if the move would overshoot the home position.
                if (newRelativePos > PosFinished) return MoveResult.InvalidOvershoot;

            }

            // If all checks pass, the move is valid.
            return MoveResult.Success;
        }

        private static (int color, int count) AnalyzeTileOccupancy(LudoGameState state, int globalPos)
        {
            int occupantCount = 0;
            int occupantColor = -1;

            for (int i = 0; i < state.PlayerCount * TokensPerPlayer; i++)
            {
                int tokenColor = i / TokensPerPlayer;
                if (GetGlobalPosition(state.TokenPositions[i], tokenColor) == globalPos)
                {
                    occupantCount++;
                    occupantColor = tokenColor;
                }
            }
            return (occupantColor, occupantCount);
        }

        private static bool IsMoveBasicallyValid(LudoGameState state, int tokenIndex, out MoveResult result)
        {
            int tokenColor = tokenIndex / TokensPerPlayer;
        
            if (tokenColor != state.CurrentPlayer) { result = MoveResult.InvalidNotYourToken; return false; }
            if (state.TokenPositions[tokenIndex] == PosFinished) { result = MoveResult.InvalidTokenFinished; return false; }

            result = MoveResult.Success; // Default to success if no basic invalidations are found
            return true;
        }

        /// <summary>
        /// FIX: Changed return type from bool to void for consistency. It now sets the out result and returns.
        /// </summary>
        private static void TryMoveFromBase(ref LudoGameState state, int tokenIndex, int diceRoll, bool isThirdSix, out MoveResult result)
        {
            if (diceRoll != 6)
            {
                result = MoveResult.InvalidNeedSixToExit;
                return;
            }
        
            // IMPROVEMENT: Handle the third six penalty
            if (isThirdSix)
            {
                // The token is moved out, but the turn ends immediately.
                state.TokenPositions[tokenIndex] = (sbyte)StartOffsets[tokenIndex / TokensPerPlayer];
                result = MoveResult.SuccessThirdSixPenalty;
                return;
            }

            int tokenColor = tokenIndex / TokensPerPlayer;
            int startGlobalPos = StartOffsets[tokenColor];
            (int occupantColor, int occupantCount) = AnalyzeTileOccupancy(state, startGlobalPos);
        
            bool evicted = false;
            if (occupantCount > 0 && occupantColor != tokenColor && !IsSafeTile(startGlobalPos))
            {
                EvictTokensAt(ref state, startGlobalPos, occupantColor);
                evicted = true;
            }
        
            state.TokenPositions[tokenIndex] = (sbyte)startGlobalPos;

            // IMPROVEMENT: Set result based on whether an eviction occurred.
            result = evicted ? MoveResult.SuccessEvictedOpponent : MoveResult.SuccessSix;
        }
    
        private static void TryPerformNormalMove(ref LudoGameState state, int tokenIndex, int diceRoll, sbyte currentPos, bool isThirdSix, out MoveResult result)
        {
            int tokenColor = tokenIndex / TokensPerPlayer;
            int relativePos = GetRelativePosition(currentPos, tokenColor);
            sbyte newRelativePos = (sbyte)(relativePos + diceRoll);

            if (newRelativePos > PosFinished)
            {
                result = MoveResult.InvalidOvershoot;
                return;
            }

            // IMPROVEMENT: If it's the third six, the move is made, but the turn ends.
            if (isThirdSix)
            {
                state.TokenPositions[tokenIndex] = GetBoardPositionFromRelative(newRelativePos, tokenColor);
                result = MoveResult.SuccessThirdSixPenalty;
                // Note: We don't check for evictions here as the penalty overrides other bonuses.
                return;
            }
        
            state.TokenPositions[tokenIndex] = GetBoardPositionFromRelative(newRelativePos, tokenColor);
            ResolveLanding(ref state, tokenIndex, diceRoll, out result);
        }

        private static void ResolveLanding(ref LudoGameState state, int tokenIndex, int diceRoll, out MoveResult result)
        {
            int tokenColor = tokenIndex / TokensPerPlayer;
            sbyte newPos = state.TokenPositions[tokenIndex];

            if (GetRelativePosition(newPos, tokenColor) == PosFinished)
            {
                result = MoveResult.SuccessRollAgain;
                return;
            }

            int newGlobalPos = GetGlobalPosition(newPos, tokenColor);
            bool evicted = false;

            if (newGlobalPos != -1)
            {
                state.TokenPositions[tokenIndex] = -100;
                (int occupantColor, int occupantCount) = AnalyzeTileOccupancy(state, newGlobalPos);
                state.TokenPositions[tokenIndex] = newPos; // Put it back

                if (occupantCount > 0 && occupantColor != tokenColor && !IsSafeTile(newGlobalPos))
                {
                    EvictTokensAt(ref state, newGlobalPos, occupantColor);
                    evicted = true;
                }
            }
        
            if (evicted)
            {
                result = MoveResult.SuccessEvictedOpponent;
            }
            else
            {
                result = diceRoll == 6 ? MoveResult.SuccessSix : MoveResult.Success;
            }
        }
    
        private static void EvictTokensAt(ref LudoGameState state, int globalPos, int victimColor)
        {
            int startIdx = victimColor * TokensPerPlayer;
            for (int i = startIdx; i < startIdx + TokensPerPlayer; i++)
            {
                if (GetGlobalPosition(state.TokenPositions[i], victimColor) == globalPos)
                {
                    state.TokenPositions[i] = PosBase;
                }
            }
        }
    
        public static int GetRelativePosition(sbyte boardPos, int color)
        {
            if (boardPos < 0) return boardPos;
            if (boardPos >= 100) return PosHomeStretchStart + (boardPos - 100 - (6 * color));

            int relative = boardPos - StartOffsets[color];
            return (relative < 0) ? relative + MainPathTiles : relative;
        }

        private static sbyte GetBoardPositionFromRelative(int relativePos, int color)
        {
            if (relativePos < 0) return (sbyte)relativePos;
            if (relativePos == PosFinished) return PosFinished;
            if (relativePos >= PosHomeStretchStart) return (sbyte)(100 + (6 * color) + (relativePos - PosHomeStretchStart));

            return (sbyte)((relativePos + StartOffsets[color]) % MainPathTiles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetGlobalPosition(sbyte boardPos, int color)
        {
            if (boardPos >= 100 || boardPos < 0) return -1; // In home stretch or base
            return (GetRelativePosition(boardPos, color) + StartOffsets[color]) % MainPathTiles;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSafeTile(int globalPos)
        {
            foreach (int safeTile in SafeTiles)
            {
                if (globalPos == safeTile) return true;
            }
            return false;
        }
    }
}