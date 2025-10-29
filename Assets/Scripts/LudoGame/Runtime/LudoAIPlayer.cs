using System.Collections.Generic;
using EasyButtons;
using Spawner.Spawner.Authoring;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ludo
{
    /// <summary>
    /// AI player controller - makes automatic strategic decisions
    /// </summary>
    public class LudoAIPlayer : LudoPlayerController
    {
        [Header("Strategy Weights")]
        [Range(0f, 10f)] public float captureWeight = 8f;
        [Range(0f, 10f)] public float exitBaseWeight = 7f;
        [Range(0f, 10f)] public float progressWeight = 5f;
        [Range(0f, 10f)] public float safetyWeight = 3f;
        [Range(0f, 10f)] public float reachHomeWeight = 10f;
        
        public override void OnTurnStart()
        {
            if (!IsMyTurn) return;
            
            Debug.Log($"<color=cyan>═══ {playerName} (AI)'s Turn ═══</color>");
            
            // Check consecutive sixes
            if (!Session.ConsecutiveSixLessThanThree())
            {
                Debug.Log($"<color=orange>{playerName} rolled 3 consecutive sixes - turn ended</color>");
                Session.EndTurn();
                return;
            }
        }
        

        private void ProcessTurn()
        {
            byte dice = Session.RollDice();
            
            if (dice == LudoBoard.ExitRoll && Session.HasTokensInBase(playerIndex))
            {
                Session.TryExitTokenFromBase(playerIndex);
            }
            
            // Get movable tokens
            var movableTokens = Session.GetMovableTokens(playerIndex, dice);
            
            if (movableTokens.Count == 0)
            {
                Debug.Log($"<color=yellow>{playerName} has no valid moves</color>");
                Session.EndTurn();
                return;
            }
            
            Session.currentMoveableTokens = movableTokens;
            OnChooseToken(movableTokens, dice);
        }

        public override void OnChooseToken(List<byte> movableTokens, byte diceValue)
        {
            byte bestToken = ChooseBestToken(movableTokens, diceValue);
            ExecuteMove(bestToken, diceValue);
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

            int tokenOrdinal = bestToken % LudoBoard.Tokens;
            Debug.Log($"<color=magenta>{playerName} chose token {tokenOrdinal} (score: {bestScore:F2})</color>");
            return bestToken;
        }

        private float EvaluateMove(byte tokenIndex, byte dice)
        {
            float score = 0f;

            // 1. HIGHEST PRIORITY: Reach home
            if (Session.CanReachHome(tokenIndex, dice))
            {
                score += reachHomeWeight * 100f;
                return score;
            }

            // 2. HIGH PRIORITY: Exit from base
            if (Session.board.IsAtBase(tokenIndex))
            {
                score += exitBaseWeight * 50f;
            }

            // 3. HIGH PRIORITY: Capture opponent
            if (Session.WillCaptureOpponent(tokenIndex, dice, out int captureCount))
            {
                score += captureWeight * captureCount * 30f;
            }

            // 4. MEDIUM PRIORITY: Progress
            float currentProgress = Session.GetTokenProgress(tokenIndex);
            float futureProgress = currentProgress + dice;
            score += (futureProgress / 57f) * progressWeight * 10f;

            // 5. SAFETY: Landing on safe tile
            if (Session.WillLandOnSafeTile(tokenIndex, dice))
            {
                score += safetyWeight * 15f;
            }

            // 6. SAFETY: Escaping danger
            if (Session.IsTokenInDanger(tokenIndex))
            {
                score += safetyWeight * 10f;
            }

            // 7. PENALTY: Staying in base when others are ahead
            if (Session.board.IsAtBase(tokenIndex) && Session.HasTokensOnBoard(playerIndex))
            {
                score -= 20f;
            }

            return score;
        }


        [Button("Force Take Turn (Debug)")]
        public void ForceTakeTurn()
        {
            if (!IsMyTurn)
            {
                Debug.LogWarning("Not this AI's turn!");
                return;
            }
            
            ProcessTurn();
        }
    }
}