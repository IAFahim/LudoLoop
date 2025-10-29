using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Spawner.Spawner.Authoring;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ludo
{
    public class LudoAIPlayer : MonoBehaviour
    {
        [Header("References")]
        public LudoGamePlay ludoGamePlay;
        
        [Header("AI Settings")]
        public float turnLockDuration = 1f;
        public Delay delay;
        public bool autoPlay = true;
        
        [Header("Strategy Weights")]
        [Range(0f, 10f)] public float captureWeight = 8f;
        [Range(0f, 10f)] public float exitBaseWeight = 7f;
        [Range(0f, 10f)] public float progressWeight = 5f;
        [Range(0f, 10f)] public float safetyWeight = 3f;
        [Range(0f, 10f)] public float reachHomeWeight = 10f;

        private void OnValidate()
        {
            if (ludoGamePlay == null)
                ludoGamePlay = GetComponent<LudoGamePlay>();
        }

        private void Update()
        {
            if (!autoPlay) return;
            
            if (delay.UpdateAndReset(Time.deltaTime, turnLockDuration))
            {
                Play();
            }
        }

        [Button]
        public void Play()
        {
            var gameSession = ludoGamePlay.gameSession;
            
            // Check if player rolled 3 consecutive sixes
            if (!gameSession.ConsecutiveSixLessThanThree())
            {
                Debug.Log($"<color=orange>Player {gameSession.currentPlayerIndex} rolled 3 consecutive sixes - turn ended</color>");
                PassPlayer();
                return;
            }

            // Roll the dice
            var dice = gameSession.RollDice();
            Debug.Log($"<color=cyan>Player {gameSession.currentPlayerIndex} rolled: {dice}</color>");
            
            // Try to exit from base if rolled a 6
            if (dice == LudoBoard.ExitRoll)
            {
                TryExitTokensFromBase(gameSession);
            }
            
            // Get movable tokens
            var movableTokens = ludoGamePlay.GetMovableTokens(gameSession.currentPlayerIndex, dice);
            
            if (movableTokens.Count == 0)
            {
                Debug.Log($"<color=yellow>Player {gameSession.currentPlayerIndex} has no valid moves</color>");
                PassPlayer();
            }
            else
            {
                gameSession.currentMoveableTokens = movableTokens;
                
                // Use smart AI to choose the best token
                byte bestToken = ChooseBestToken(movableTokens, dice);
                Perform(gameSession, bestToken, dice);
            }
        }

        private void TryExitTokensFromBase(GameSession gameSession)
        {
            int playerIndex = gameSession.currentPlayerIndex;
            int startToken = playerIndex * LudoBoard.Tokens;
            
            // Try to exit tokens from base
            for (int i = 0; i < LudoBoard.Tokens; i++)
            {
                int tokenIndex = startToken + i;
                if (gameSession.board.IsAtBase(tokenIndex))
                {
                    ludoGamePlay.GetOutOfBase(tokenIndex);
                    Debug.Log($"<color=green>Player {playerIndex} exited token {i} from base</color>");
                    break; // Exit one token per turn
                }
            }
        }

        private byte ChooseBestToken(List<byte> movableTokens, byte dice)
        {
            if (movableTokens.Count == 1)
                return movableTokens[0];

            float bestScore = float.MinValue;
            byte bestToken = movableTokens[0];

            foreach (byte token in movableTokens)
            {
                float score = EvaluateMove(token, dice);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestToken = token;
                }
            }

            Debug.Log($"<color=magenta>AI chose token {bestToken % LudoBoard.Tokens} with score: {bestScore:F2}</color>");
            return bestToken;
        }

        private float EvaluateMove(byte tokenIndex, byte dice)
        {
            float score = 0f;
            var board = ludoGamePlay.gameSession.board;
            int playerIndex = ludoGamePlay.gameSession.currentPlayerIndex;

            // 1. PRIORITY: Can reach home
            if (CanReachHome(tokenIndex, dice))
            {
                score += reachHomeWeight * 100f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Can reach HOME (+{reachHomeWeight * 100f})");
            }

            // 2. HIGH PRIORITY: Exit from base
            if (board.IsAtBase(tokenIndex))
            {
                score += exitBaseWeight * 50f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Exits base (+{exitBaseWeight * 50f})");
            }

            // 3. HIGH PRIORITY: Can capture opponent
            if (CanCaptureOpponent(tokenIndex, dice, out int captureCount))
            {
                score += captureWeight * captureCount * 30f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Can CAPTURE ({captureCount} opponents) (+{captureWeight * captureCount * 30f})");
            }

            // 4. MEDIUM PRIORITY: Progress toward home
            float progressScore = CalculateProgressScore(tokenIndex, dice);
            score += progressScore;

            // 5. SAFETY: Move to safe tile or avoid danger
            float safetyScore = CalculateSafetyScore(tokenIndex, dice);
            score += safetyScore;

            // 6. PENALTY: Staying at base when others are advanced
            if (board.IsAtBase(tokenIndex) && HasTokensOnBoard(playerIndex))
            {
                score -= 20f;
            }

            return score;
        }

        private bool CanReachHome(byte tokenIndex, byte dice)
        {
            var board = ludoGamePlay.gameSession.board;
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

        private bool CanCaptureOpponent(byte tokenIndex, byte dice, out int captureCount)
        {
            captureCount = 0;
            var board = ludoGamePlay.gameSession.board;
            int playerIndex = ludoGamePlay.gameSession.currentPlayerIndex;
            
            // Simulate the move
            byte currentPos = board.TokenPositions[tokenIndex];
            if (board.IsAtBase(tokenIndex) && dice == LudoBoard.ExitRoll)
            {
                currentPos = LudoBoard.MainStart;
            }
            else
            {
                currentPos += dice;
            }

            // Check if landing position would be on main track (not safe)
            if (currentPos < LudoBoard.MainStart || currentPos > LudoBoard.MainEnd)
                return false;

            // Calculate absolute position
            int landingAbs = board.ToAbsoluteMainTrack(currentPos, playerIndex);
            
            // Check if it's a safe tile
            if (board.Contains(LudoBoard.SafeAbsoluteTiles, (byte)landingAbs))
                return false;

            // Count opponent tokens at this position
            for (int i = 0; i < board.TokenPositions.Length; i++)
            {
                if (board.PlayerCount == 2)
                {
                    int opponent = i / LudoBoard.Tokens;
                    if (opponent == playerIndex || (playerIndex == 0 && opponent == 1) || (playerIndex == 2 && opponent == 1))
                        continue;
                }
                else
                {
                    if (i / LudoBoard.Tokens == playerIndex)
                        continue;
                }
                
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

        private float CalculateProgressScore(byte tokenIndex, byte dice)
        {
            var board = ludoGamePlay.gameSession.board;
            byte currentPos = board.TokenPositions[tokenIndex];
            
            float baseProgress = 0f;
            
            // Calculate how far the token will be from home
            if (board.IsAtBase(tokenIndex))
            {
                baseProgress = 1f; // Just starting
            }
            else if (board.IsOnMainTrack(tokenIndex))
            {
                baseProgress = currentPos + dice;
            }
            else if (board.IsOnHomeStretch(tokenIndex))
            {
                baseProgress = 52 + (currentPos - LudoBoard.HomeStart) + dice;
            }

            float normalizedProgress = baseProgress / 57f; // 57 is max (home position)
            return progressWeight * normalizedProgress * 10f;
        }

        private float CalculateSafetyScore(byte tokenIndex, byte dice)
        {
            var board = ludoGamePlay.gameSession.board;
            int playerIndex = ludoGamePlay.gameSession.currentPlayerIndex;
            float safetyScore = 0f;
            
            // Simulate landing position
            byte currentPos = board.TokenPositions[tokenIndex];
            byte landingPos = (byte)(currentPos + dice);
            
            if (board.IsAtBase(tokenIndex) && dice == LudoBoard.ExitRoll)
            {
                landingPos = LudoBoard.MainStart;
            }

            // Moving to safe tile is good
            if (landingPos >= LudoBoard.MainStart && landingPos <= LudoBoard.MainEnd)
            {
                int landingAbs = board.ToAbsoluteMainTrack(landingPos, playerIndex);
                if (board.Contains(LudoBoard.SafeAbsoluteTiles, (byte)landingAbs))
                {
                    safetyScore += safetyWeight * 15f;
                    Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Lands on SAFE tile (+{safetyWeight * 15f})");
                }
            }

            // Home stretch is always safe
            if (landingPos > LudoBoard.MainEnd && landingPos < LudoBoard.Home)
            {
                safetyScore += safetyWeight * 20f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Enters HOME STRETCH (+{safetyWeight * 20f})");
            }

            // Check if current position is in danger
            if (IsInDanger(tokenIndex))
            {
                safetyScore += safetyWeight * 10f; // Moving out of danger
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Escapes danger (+{safetyWeight * 10f})");
            }

            return safetyScore;
        }

        private bool IsInDanger(byte tokenIndex)
        {
            var board = ludoGamePlay.gameSession.board;
            int playerIndex = ludoGamePlay.gameSession.currentPlayerIndex;
            
            if (!board.IsOnMainTrack(tokenIndex))
                return false;
            
            if (board.IsOnSafeTile(tokenIndex))
                return false;

            int myAbs = board.GetAbsolutePosition(tokenIndex);

            // Check if any opponent is within striking distance (6 tiles behind)
            for (int i = 0; i < board.TokenPositions.Length; i++)
            {
                if (i / LudoBoard.Tokens == playerIndex)
                    continue;

                if (!board.IsOnMainTrack(i))
                    continue;

                int opponentAbs = board.GetAbsolutePosition(i);
                int distance = (myAbs - opponentAbs + 52) % 52;
                
                if (distance > 0 && distance <= 6)
                    return true;
            }

            return false;
        }

        private bool HasTokensOnBoard(int playerIndex)
        {
            var board = ludoGamePlay.gameSession.board;
            int startToken = playerIndex * LudoBoard.Tokens;
            
            for (int i = 0; i < LudoBoard.Tokens; i++)
            {
                int tokenIndex = startToken + i;
                if (!board.IsAtBase(tokenIndex))
                    return true;
            }
            
            return false;
        }

        private void Perform(GameSession gameSession, byte tokenToMove, byte dice)
        {
            gameSession.tokenToMove = tokenToMove;
            
            // Move the token
            ludoGamePlay.MoveToken(tokenToMove, dice, out var tokenSentToBase);
            gameSession.tokenSentToBase = tokenSentToBase;
            
            if (tokenSentToBase != LudoBoard.NoTokenSentToBaseCode)
            {
                Debug.Log($"<color=red>CAPTURED! Token {tokenSentToBase} sent back to base!</color>");
            }
            
            // Check if player won
            if (gameSession.board.HasWon(gameSession.currentPlayerIndex))
            {
                Debug.Log($"<color=yellow>★★★ Player {gameSession.currentPlayerIndex} has WON! ★★★</color>");
                // Handle win condition - you might want to stop the game or show UI
                autoPlay = false;
                return;
            }
            
            // End turn if not a six OR if rolled 3 consecutive sixes
            if (dice != 6 || !gameSession.ConsecutiveSixLessThanThree())
            {
                PassPlayer();
            }
            else
            {
                Debug.Log($"<color=lime>Player {gameSession.currentPlayerIndex} rolled a 6 - gets another turn!</color>");
            }
        }

        private void PassPlayer()
        {
            var gameSession = ludoGamePlay.gameSession;
            gameSession.EndTurn();
            Debug.Log($"<color=white>════ Turn passed to Player {gameSession.currentPlayerIndex} ════</color>");
        }

        // Manual random fallback if needed
        [Button("Use Random Strategy")]
        public void DoRandom()
        {
            var gameSession = ludoGamePlay.gameSession;
            var movableTokens = gameSession.currentMoveableTokens;

            if (movableTokens == null || movableTokens.Count == 0) return;

            int randomIndex = Random.Range(0, movableTokens.Count);
            byte tokenToMove = movableTokens[randomIndex];
            
            Debug.Log($"<color=grey>Player {gameSession.currentPlayerIndex} randomly chose token {tokenToMove}</color>");
            Perform(gameSession, tokenToMove, gameSession.diceValue);
        }
    }
}