using System.Collections.Generic;
using EasyButtons;
using Spawner.Spawner.Authoring;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ludo
{
    /// <summary>
    /// AI player controller - handles AI decision making and timing only.
    /// Game logic is delegated to GameSession and LudoGamePlay.
    /// </summary>
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
            
            var session = ludoGamePlay.gameSession;
            if (session.isGameOver) return;
            
            if (delay.UpdateAndReset(Time.deltaTime, turnLockDuration))
            {
                PlayTurn();
            }
        }

        [Button]
        public void PlayTurn()
        {
            var session = ludoGamePlay.gameSession;
            
            // Check if rolled 3 consecutive sixes
            if (!session.ConsecutiveSixLessThanThree())
            {
                Debug.Log($"<color=orange>Player {session.currentPlayerIndex} rolled 3 consecutive sixes - turn ended</color>");
                session.EndTurn();
                return;
            }

            // Roll dice
            byte dice = session.RollDice();
            Debug.Log($"<color=cyan>Player {session.currentPlayerIndex} rolled: {dice}</color>");
            
            // Try to exit from base on rolling 6
            if (dice == LudoBoard.ExitRoll && session.HasTokensInBase(session.currentPlayerIndex))
            {
                if (session.TryExitTokenFromBase(session.currentPlayerIndex))
                {
                    Debug.Log($"<color=green>Player {session.currentPlayerIndex} exited token from base</color>");
                }
            }
            
            // Get movable tokens
            var movableTokens = session.GetMovableTokens(session.currentPlayerIndex, dice);
            
            if (movableTokens.Count == 0)
            {
                Debug.Log($"<color=yellow>Player {session.currentPlayerIndex} has no valid moves</color>");
                session.EndTurn();
                return;
            }

            // Choose and move token
            session.currentMoveableTokens = movableTokens;
            byte bestToken = ChooseBestToken(movableTokens, dice);
            MoveToken(bestToken, dice);
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
            var session = ludoGamePlay.gameSession;
            float score = 0f;

            // 1. HIGHEST PRIORITY: Reach home
            if (session.CanReachHome(tokenIndex, dice))
            {
                score += reachHomeWeight * 100f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Can reach HOME (+{reachHomeWeight * 100f})");
                return score; // This is the best possible move
            }

            // 2. HIGH PRIORITY: Exit from base
            if (session.board.IsAtBase(tokenIndex))
            {
                score += exitBaseWeight * 50f;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Exits base (+{exitBaseWeight * 50f})");
            }

            // 3. HIGH PRIORITY: Capture opponent
            if (session.WillCaptureOpponent(tokenIndex, dice, out int captureCount))
            {
                float captureScore = captureWeight * captureCount * 30f;
                score += captureScore;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: CAPTURE {captureCount} opponent(s) (+{captureScore})");
            }

            // 4. MEDIUM PRIORITY: Progress
            float currentProgress = session.GetTokenProgress(tokenIndex);
            float futureProgress = currentProgress + dice;
            float progressScore = (futureProgress / 57f) * progressWeight * 10f;
            score += progressScore;

            // 5. SAFETY: Landing on safe tile
            if (session.WillLandOnSafeTile(tokenIndex, dice))
            {
                float safeScore = safetyWeight * 15f;
                score += safeScore;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Lands on SAFE tile (+{safeScore})");
            }

            // 6. SAFETY: Escaping danger
            if (session.IsTokenInDanger(tokenIndex))
            {
                float escapeScore = safetyWeight * 10f;
                score += escapeScore;
                Debug.Log($"  Token {tokenIndex % LudoBoard.Tokens}: Escapes danger (+{escapeScore})");
            }

            // 7. PENALTY: Leaving base when others are ahead
            if (session.board.IsAtBase(tokenIndex) && session.HasTokensOnBoard(session.currentPlayerIndex))
            {
                score -= 20f;
            }

            return score;
        }

        private void MoveToken(byte tokenIndex, byte dice)
        {
            var session = ludoGamePlay.gameSession;
            session.tokenToMove = tokenIndex;
            
            // Execute move through gameplay
            ludoGamePlay.MoveToken(tokenIndex, dice, out var tokenSentToBase);
            session.tokenSentToBase = tokenSentToBase;
            
            // Log capture
            if (tokenSentToBase != LudoBoard.NoTokenSentToBaseCode)
            {
                int capturedPlayer = tokenSentToBase / LudoBoard.Tokens;
                int capturedTokenOrdinal = tokenSentToBase % LudoBoard.Tokens;
                Debug.Log($"<color=red>★ CAPTURED! Player {capturedPlayer}'s token {capturedTokenOrdinal} sent to base!</color>");
            }
            
            // Check win condition
            if (session.CheckWinCondition(session.currentPlayerIndex))
            {
                Debug.Log($"<color=yellow>★★★ GAME OVER! Player {session.currentPlayerIndex} WINS! ★★★</color>");
                autoPlay = false;
                return;
            }
            
            // Handle turn passing
            if (session.ShouldPassTurn(dice))
            {
                session.EndTurn();
                Debug.Log($"<color=white>════ Turn passed to Player {session.currentPlayerIndex} ════</color>");
            }
            else
            {
                Debug.Log($"<color=lime>Player {session.currentPlayerIndex} rolled a 6 - gets another turn!</color>");
            }
        }

        // Optional: Random strategy for comparison/testing
        [Button("Use Random Strategy")]
        public void UseRandomStrategy()
        {
            var session = ludoGamePlay.gameSession;
            var movableTokens = session.currentMoveableTokens;

            if (movableTokens == null || movableTokens.Count == 0) 
            {
                Debug.LogWarning("No movable tokens available");
                return;
            }

            int randomIndex = Random.Range(0, movableTokens.Count);
            byte tokenToMove = movableTokens[randomIndex];
            
            Debug.Log($"<color=grey>Random: Player {session.currentPlayerIndex} chose token {tokenToMove % LudoBoard.Tokens}</color>");
            MoveToken(tokenToMove, session.diceValue);
        }
    }
}