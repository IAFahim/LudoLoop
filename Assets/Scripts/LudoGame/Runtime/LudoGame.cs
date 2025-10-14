using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace LudoGame.Runtime
{
    public struct LudoGameState
    {
        //newly added
        public ushort TurnCount;
        public int DiceValue;
        public int ConsecutiveSixes;
        public int CurrentPlayer;
        public sbyte[] TokenPositions;
        public int PlayerCount;
        public int Seed;

        /// <summary>
        /// Buffer Layout (26 bytes total):
        /// buffer[0-1] - TurnCount (ushort)
        /// buffer[2] - DiceValue (byte)
        /// buffer[3] - ConsecutiveSixes (byte)
        /// buffer[4] - CurrentPlayer (byte)
        /// buffer[5] - PlayerCount (byte)
        /// buffer[6-9] - Seed (int)
        /// buffer[10-25] - TokenPositions (16 bytes)
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            // Buffer size: 10 bytes for metadata + 16 bytes for token positions = 26 bytes
            byte[] buffer = new byte[10 + 16];

            // Store TurnCount as 2 bytes (ushort) at index 0-1
            byte[] turnCountBytes = BitConverter.GetBytes(TurnCount);
            Buffer.BlockCopy(turnCountBytes, 0, buffer, 0, 2);

            buffer[2] = (byte)DiceValue;
            buffer[3] = (byte)ConsecutiveSixes;
            buffer[4] = (byte)CurrentPlayer;
            buffer[5] = (byte)PlayerCount;

            // Store Seed as 4 bytes (int) at index 6-9
            byte[] seedBytes = BitConverter.GetBytes(Seed);
            Buffer.BlockCopy(seedBytes, 0, buffer, 6, 4);

            // Token positions start at index 10
            Buffer.BlockCopy(TokenPositions, 0, buffer, 10, 16);

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
                TurnCount = BitConverter.ToUInt16(buffer, 0),
                DiceValue = buffer[2],
                ConsecutiveSixes = buffer[3],
                CurrentPlayer = buffer[4],
                PlayerCount = buffer[5],
                Seed = BitConverter.ToInt32(buffer, 6),
                TokenPositions = new sbyte[16]
            };

            Buffer.BlockCopy(buffer, 10, state.TokenPositions, 0, 16);

            return state;
        }
        
        public void StartNewTurn(ref LudoGameState state)
        {
            state.TurnCount++;
            state.Seed = GenerateRandomSeed(); 
            state.DiceValue = 0;
        }
        
        private int GenerateRandomSeed()
        {
            return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        }


        /// <summary>
        /// Creates a new game state.
        /// </summary>
        public static bool TryCreate(int playerCount, out LudoGameState ludoGameState,
            out LudoCreateResult ludoCreateResult)
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

    public enum LudoCreateResult
    {
        Success,
        InsufficientPlayers,
        TooManyPlayers
    }

    /// <summary>
    /// Represents the result of a token move attempt.
    /// </summary>
    public enum MoveResult
    {
        // --- Success States ---
        Success = 0, // Move successful, turn ends
        SuccessRollAgain = 1, // Move successful AND token reached home (roll again)
        SuccessSix = 2, // Move successful, rolled 6 (roll again)
        SuccessEvictedOpponent = 3, // Move successful AND an opponent's token was evicted
        SuccessThirdSixPenalty = 4, // Move successful, but turn is forfeited due to 3 sixes in a row

        // --- Invalid States ---
        InvalidTokenFinished = 5, // Token already at home
        InvalidNeedSixToExit = 6, // Must roll 6 to exit base
        InvalidOvershoot = 7, // Would overshoot home
        InvalidNotYourToken = 8, // Token doesn't belong to current player
        InvalidNoValidMoves = 9, // Dice roll is valid, but no token can legally move
        InvalidBlockedByBlockade = 10 // Path is blocked by a blockade (2+ tokens on same tile)
    }

    /// <summary>
    /// Extension method to provide human-readable descriptions for MoveResult values.
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
                case MoveResult.InvalidBlockedByBlockade:
                    return "Invalid move: Path is blocked by a blockade.";

                // Default case to catch any unhandled enum values
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), "Unhandled MoveResult.");
            }
        }

        /// <summary>
        /// Determines if the MoveResult is a success state.
        /// </summary>
        public static bool IsSuccess(this MoveResult result)
        {
            // Success states are grouped with values from 0 to 4.
            return result <= MoveResult.SuccessThirdSixPenalty;
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
        /// processes a move and returns true on success and false on failure.
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

        /// <summary>
        /// Attempts to switch to the next turn based on the move result.
        /// Returns true if the turn switches to the next player, false if the current player rolls again.
        /// </summary>
        public static bool TryNextTurn(ref LudoGameState state, in MoveResult moveResult)
        {
            bool rollAgain = moveResult == MoveResult.SuccessSix ||
                             moveResult == MoveResult.SuccessRollAgain ||
                             moveResult == MoveResult.SuccessEvictedOpponent;

            if (moveResult == MoveResult.SuccessThirdSixPenalty)
            {
                state.ConsecutiveSixes = 0; // Reset counter
                state.CurrentPlayer = (state.CurrentPlayer + 1) % state.PlayerCount;
                return true; // Turn switched to next player
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

                return false; // Current player rolls again
            }
            else
            {
                state.ConsecutiveSixes = 0;
                state.CurrentPlayer = (state.CurrentPlayer + 1) % state.PlayerCount;
                return true; // Turn switched to next player
            }
        }

        /// <summary>
        /// To ensure that every move returned is 100% legal according to all game rules.
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
                if (!result.IsSuccess()) continue;
                validMoves.Add(i);
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
        /// This helper method performs a complete, read-only validation of a potential move.
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

                // Check if start position is blocked
                int startGlobalPos = StartOffsets[tokenColor];
                if (IsBlockade(state, startGlobalPos, tokenColor))
                {
                    return MoveResult.InvalidBlockedByBlockade;
                }
            }
            // 3. Handle logic for a normal move on the board.
            else
            {
                int relativePos = GetRelativePosition(currentPos, tokenColor);
                sbyte newRelativePos = (sbyte)(relativePos + diceRoll);

                // Check if the move would overshoot the home position.
                if (newRelativePos > PosFinished) return MoveResult.InvalidOvershoot;

                // Check if path is blocked by blockades
                if (IsPathBlocked(state, tokenColor, relativePos, newRelativePos))
                {
                    return MoveResult.InvalidBlockedByBlockade;
                }
            }

            // If all checks pass, the move is valid.
            return MoveResult.Success;
        }

        /// <summary>
        /// Checks if there's a blockade (2+ tokens of the same color) at the given global position.
        /// </summary>
        private static bool IsBlockade(LudoGameState state, int globalPos, int movingTokenColor)
        {
            int count = 0;
            int occupantColor = -1;

            for (int i = 0; i < state.PlayerCount * TokensPerPlayer; i++)
            {
                int tokenColor = i / TokensPerPlayer;
                int tokenGlobalPos = GetGlobalPosition(state.TokenPositions[i], tokenColor);

                if (tokenGlobalPos == globalPos)
                {
                    count++;
                    occupantColor = tokenColor;

                    // Early exit if we found 2+ tokens
                    if (count >= 2) break;
                }
            }

            // A blockade exists if there are 2+ tokens of a different color on this tile
            // (Safe tiles cannot be blockaded in traditional Ludo)
            return count >= 2 && occupantColor != movingTokenColor && !IsSafeTile(globalPos);
        }

        /// <summary>
        /// Checks if the path from startRelativePos to endRelativePos is blocked by any blockades.
        /// </summary>
        private static bool IsPathBlocked(LudoGameState state, int tokenColor, int startRelativePos, int endRelativePos)
        {
            // Check each position along the path (exclusive of start, inclusive of end)
            for (int relPos = startRelativePos + 1; relPos <= endRelativePos; relPos++)
            {
                // Get the global position for this relative position
                int globalPos = -1;

                // If we're in the home stretch (>= 51), there are no blockades possible
                if (relPos >= PosHomeStretchStart && relPos < PosFinished)
                {
                    continue; // Home stretch can't be blocked
                }

                // If we've reached the finished position, no need to check
                if (relPos == PosFinished)
                {
                    continue;
                }

                // Get the board position and convert to global
                sbyte boardPos = GetBoardPositionFromRelative(relPos, tokenColor);
                globalPos = GetGlobalPosition(boardPos, tokenColor);

                // If this is a valid global position, check for blockades
                if (globalPos != -1 && IsBlockade(state, globalPos, tokenColor))
                {
                    return true; // Path is blocked
                }
            }

            return false; // Path is clear
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

            if (tokenColor != state.CurrentPlayer)
            {
                result = MoveResult.InvalidNotYourToken;
                return false;
            }

            if (state.TokenPositions[tokenIndex] == PosFinished)
            {
                result = MoveResult.InvalidTokenFinished;
                return false;
            }

            result = MoveResult.Success; // Default to success if no basic invalidations are found
            return true;
        }

        /// <summary>
        /// Changed return type from bool to void for consistency. It now sets the out result and returns.
        /// </summary>
        private static void TryMoveFromBase(ref LudoGameState state, int tokenIndex, int diceRoll, bool isThirdSix,
            out MoveResult result)
        {
            if (diceRoll != 6)
            {
                result = MoveResult.InvalidNeedSixToExit;
                return;
            }

            int tokenColor = tokenIndex / TokensPerPlayer;
            int startGlobalPos = StartOffsets[tokenColor];

            // Check for blockade at start position
            if (IsBlockade(state, startGlobalPos, tokenColor))
            {
                result = MoveResult.InvalidBlockedByBlockade;
                return;
            }

            // Handle the third six penalty
            if (isThirdSix)
            {
                // The token is moved out, but the turn ends immediately.
                state.TokenPositions[tokenIndex] = (sbyte)StartOffsets[tokenIndex / TokensPerPlayer];
                result = MoveResult.SuccessThirdSixPenalty;
                return;
            }

            (int occupantColor, int occupantCount) = AnalyzeTileOccupancy(state, startGlobalPos);

            bool evicted = false;
            if (occupantCount > 0 && occupantColor != tokenColor && !IsSafeTile(startGlobalPos))
            {
                EvictTokensAt(ref state, startGlobalPos, occupantColor);
                evicted = true;
            }

            state.TokenPositions[tokenIndex] = (sbyte)startGlobalPos;

            // Set result based on whether an eviction occurred.
            result = evicted ? MoveResult.SuccessEvictedOpponent : MoveResult.SuccessSix;
        }

        private static void TryPerformNormalMove(ref LudoGameState state, int tokenIndex, int diceRoll,
            sbyte currentPos, bool isThirdSix, out MoveResult result)
        {
            int tokenColor = tokenIndex / TokensPerPlayer;
            int relativePos = GetRelativePosition(currentPos, tokenColor);
            sbyte newRelativePos = (sbyte)(relativePos + diceRoll);

            if (newRelativePos > PosFinished)
            {
                result = MoveResult.InvalidOvershoot;
                return;
            }

            // Check if path is blocked
            if (IsPathBlocked(state, tokenColor, relativePos, newRelativePos))
            {
                result = MoveResult.InvalidBlockedByBlockade;
                return;
            }

            // If it's the third six, the move is made, but the turn ends.
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
            if (relativePos >= PosHomeStretchStart)
                return (sbyte)(100 + (6 * color) + (relativePos - PosHomeStretchStart));

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