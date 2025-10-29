using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

namespace Ludo
{
    [CreateAssetMenu(fileName = "NewGameSession", menuName = "Ludo/Game Session")]
    public class GameSession : ScriptableObject
    {
        [Header("Game Configuration")] 
        [Tooltip("Type of the game (e.g., Classic, Custom).")]
        public string gameType;

        [Tooltip("Reference to the Ludo board.")]
        public LudoBoard board;

        [Header("Player State")] 
        [Tooltip("Index of the current player (0-3).")] 
        [Range(0, 3)]
        public byte currentPlayerIndex;

        [Tooltip("Value of the last dice roll (1-6).")] 
        [Range(1, 6)]
        public byte diceValue;

        [Tooltip("Number of consecutive sixes rolled.")]
        public byte consecutiveSixCount;

        [Tooltip("If true, all players can interact with the board this turn.")]
        public bool othersCanInteractWithBoard;

        [Tooltip("Index of the main player (0-3).")] 
        [Range(0, 3)]
        public byte mainPlayerIndex;

        [Header("Token State")] 
        [Tooltip("List of tokens that can be moved this turn.")]
        public List<byte> currentMoveableTokens;

        [Tooltip("Index of the token to move (0-3).")] 
        [Range(0, 3)]
        public byte tokenToMove;

        [Tooltip("New position of the token after movement (0-56).")] 
        [Range(0, 56)]
        public byte tokenNewPosition;

        [Tooltip("Index of the token sent back to base (0-3).")] 
        [Range(0, 3)]
        public byte tokenSentToBase;

        [Header("Game State")]
        public bool isGameOver;
        public int winnerPlayerIndex = -1;

        // ========================================
        // GAME SETUP
        // ========================================
        
        [Button]
        public void ReSetup()
        {
            board = new LudoBoard(board.PlayerCount);
            currentPlayerIndex = 0;
            consecutiveSixCount = 0;
            diceValue = 0;
            isGameOver = false;
            winnerPlayerIndex = -1;
            currentMoveableTokens?.Clear();
            tokenSentToBase = LudoBoard.NoTokenSentToBaseCode;
        }

        // ========================================
        // DICE MANAGEMENT
        // ========================================
        
        [Button]
        public byte RollDice()
        {
            byte value = (byte)Random.Range(1, 7);
            diceValue = value;
            if (diceValue == 6) 
            {
                consecutiveSixCount++;
            }
            return value;
        }

        public bool ConsecutiveSixLessThanThree() => consecutiveSixCount < 3;

        public void ResetConsecutiveSix()
        {
            consecutiveSixCount = 0;
        }

        // ========================================
        // TURN MANAGEMENT
        // ========================================
        
        public void EndTurn()
        {
            ResetConsecutiveSix();
            currentPlayerIndex = (byte)((currentPlayerIndex + 1) % board.PlayerCount);
            currentMoveableTokens?.Clear();
            tokenSentToBase = LudoBoard.NoTokenSentToBaseCode;
        }

        public bool ShouldPassTurn(byte dice)
        {
            // Pass turn if not a six OR rolled 3 consecutive sixes
            return dice != 6 || !ConsecutiveSixLessThanThree();
        }

        // ========================================
        // TOKEN MANAGEMENT
        // ========================================
        
        public bool TryExitTokenFromBase(int playerIndex)
        {
            int startToken = playerIndex * LudoBoard.Tokens;
            
            for (int i = 0; i < LudoBoard.Tokens; i++)
            {
                int tokenIndex = startToken + i;
                if (board.IsAtBase(tokenIndex))
                {
                    // Check if start position is blocked
                    int absStart = board.StartAbsoluteTile(playerIndex);
                    if (IsTileBlockedByOpponent(absStart, playerIndex))
                    {
                        return false;
                    }
                    
                    board.GetOutOfBase(tokenIndex);
                    return true;
                }
            }
            
            return false; // No tokens in base
        }

        public List<byte> GetMovableTokens(int playerIndex, int diceRoll)
        {
            return board.GetMovableTokens(playerIndex, diceRoll);
        }

        public bool HasTokensInBase(int playerIndex)
        {
            int startToken = playerIndex * LudoBoard.Tokens;
            
            for (int i = 0; i < LudoBoard.Tokens; i++)
            {
                int tokenIndex = startToken + i;
                if (board.IsAtBase(tokenIndex))
                    return true;
            }
            
            return false;
        }

        public bool HasTokensOnBoard(int playerIndex)
        {
            int startToken = playerIndex * LudoBoard.Tokens;
            
            for (int i = 0; i < LudoBoard.Tokens; i++)
            {
                int tokenIndex = startToken + i;
                if (!board.IsAtBase(tokenIndex) && !board.IsHome(tokenIndex))
                    return true;
            }
            
            return false;
        }

        // ========================================
        // WIN CONDITION
        // ========================================
        
        public bool CheckWinCondition(int playerIndex)
        {
            if (board.HasWon(playerIndex))
            {
                isGameOver = true;
                winnerPlayerIndex = playerIndex;
                return true;
            }
            return false;
        }

        // ========================================
        // BOARD QUERIES FOR AI
        // ========================================
        
        public bool CanReachHome(byte tokenIndex, byte dice)
        {
            byte currentPos = board.TokenPositions[tokenIndex];
            
            if (board.IsOnHomeStretch(tokenIndex))
            {
                int targetPos = currentPos + dice;
                return targetPos == LudoBoard.Home;
            }
            
            if (board.IsOnMainTrack(tokenIndex))
            {
                int targetPos = currentPos + dice;
                int stepsIntoHome = targetPos - LudoBoard.MainEnd;
                if (stepsIntoHome > 0)
                {
                    int homeTarget = LudoBoard.HomeStart + stepsIntoHome - 1;
                    return homeTarget == LudoBoard.Home;
                }
            }
            
            return false;
        }

        public bool WillCaptureOpponent(byte tokenIndex, byte dice, out int captureCount)
        {
            captureCount = 0;
            int playerIndex = tokenIndex / LudoBoard.Tokens;
            
            // Calculate landing position
            byte currentPos = board.TokenPositions[tokenIndex];
            byte landingPos;
            
            if (board.IsAtBase(tokenIndex) && dice == LudoBoard.ExitRoll)
            {
                landingPos = LudoBoard.MainStart;
            }
            else
            {
                landingPos = (byte)(currentPos + dice);
            }

            // Only check captures on main track
            if (landingPos < LudoBoard.MainStart || landingPos > LudoBoard.MainEnd)
                return false;

            // Calculate absolute position
            int landingAbs = board.ToAbsoluteMainTrack(landingPos, playerIndex);
            
            // Can't capture on safe tiles
            if (board.Contains(LudoBoard.SafeAbsoluteTiles, (byte)landingAbs))
                return false;

            // Count opponent tokens at landing position
            for (int i = 0; i < board.TokenPositions.Length; i++)
            {
                int opponentPlayer = i / LudoBoard.Tokens;
                
                // Skip own tokens
                if (opponentPlayer == playerIndex)
                    continue;
                
                // For 2-player game, skip player 1 (only 0 and 2 play)
                if (board.PlayerCount == 2 && opponentPlayer == 1)
                    continue;
                
                if (board.IsOnMainTrack(i))
                {
                    int opponentAbs = board.GetAbsolutePosition(i);
                    if (opponentAbs == landingAbs)
                    {
                        captureCount++;
                    }
                }
            }

            return captureCount > 0;
        }

        public bool IsTokenInDanger(byte tokenIndex)
        {
            int playerIndex = tokenIndex / LudoBoard.Tokens;
            
            if (!board.IsOnMainTrack(tokenIndex))
                return false;
            
            if (board.IsOnSafeTile(tokenIndex))
                return false;

            int myAbs = board.GetAbsolutePosition(tokenIndex);

            // Check if any opponent is within striking distance (1-6 tiles behind)
            for (int i = 0; i < board.TokenPositions.Length; i++)
            {
                int opponentPlayer = i / LudoBoard.Tokens;
                
                if (opponentPlayer == playerIndex)
                    continue;
                
                // For 2-player game
                if (board.PlayerCount == 2 && opponentPlayer == 1)
                    continue;

                if (!board.IsOnMainTrack(i))
                    continue;

                int opponentAbs = board.GetAbsolutePosition(i);
                
                // Calculate distance (considering circular track)
                int distance = (myAbs - opponentAbs + LudoBoard.MainEnd) % LudoBoard.MainEnd;
                
                if (distance > 0 && distance <= 6)
                    return true;
            }

            return false;
        }

        public bool WillLandOnSafeTile(byte tokenIndex, byte dice)
        {
            int playerIndex = tokenIndex / LudoBoard.Tokens;
            byte currentPos = board.TokenPositions[tokenIndex];
            byte landingPos;
            
            if (board.IsAtBase(tokenIndex) && dice == LudoBoard.ExitRoll)
            {
                landingPos = LudoBoard.MainStart;
            }
            else
            {
                landingPos = (byte)(currentPos + dice);
            }

            // Home stretch is always safe
            if (landingPos > LudoBoard.MainEnd && landingPos < LudoBoard.Home)
                return true;

            // Check if landing on safe tile on main track
            if (landingPos >= LudoBoard.MainStart && landingPos <= LudoBoard.MainEnd)
            {
                int landingAbs = board.ToAbsoluteMainTrack(landingPos, playerIndex);
                return board.Contains(LudoBoard.SafeAbsoluteTiles, (byte)landingAbs);
            }

            return false;
        }

        public float GetTokenProgress(byte tokenIndex)
        {
            byte currentPos = board.TokenPositions[tokenIndex];
            
            if (board.IsAtBase(tokenIndex))
                return 0f;
            
            if (board.IsHome(tokenIndex))
                return 57f;
            
            if (board.IsOnMainTrack(tokenIndex))
                return currentPos;
            
            if (board.IsOnHomeStretch(tokenIndex))
                return LudoBoard.MainEnd + (currentPos - LudoBoard.HomeStart);
            
            return 0f;
        }

        // ========================================
        // UTILITY
        // ========================================
        
        private bool IsTileBlockedByOpponent(int absolutePosition, int movingPlayer)
        {
            for (int opp = 0; opp < board.PlayerCount; opp++)
            {
                if (opp == movingPlayer) continue;
                if (board.PlayerCount == 2 && opp == 1) continue; // Skip player 1 in 2-player

                int start = opp * LudoBoard.Tokens;
                int count = 0;
                
                for (int i = 0; i < LudoBoard.Tokens; i++)
                {
                    int t = start + i;
                    if (board.IsOnMainTrack(t) && board.GetAbsolutePosition(t) == absolutePosition)
                    {
                        count++;
                        if (count >= 2) return true; // Blockade
                    }
                }
            }
            return false;
        }

        public bool IsMainPlayer(byte playerIndex) => mainPlayerIndex == playerIndex;

        public bool HavePermissionToInteractThisTurn(byte playerIndex)
        {
            return othersCanInteractWithBoard || IsMainPlayer(playerIndex);
        }

        public int OffsetPlayerIndex(int playerIndex)
        {
            if (board.PlayerCount == 2 && playerIndex == 1) 
                return 2;
            return playerIndex;
        }
    }
}