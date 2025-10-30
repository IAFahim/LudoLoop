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
        
        public override void OnTurn()
        {
            if (!IsMyTurn) return;
            
            Debug.Log($"<color=cyan>═══ {playerName} (AI)'s Turn ═══</color>");
            ProcessTurn();
            if (!Session.ConsecutiveSixLessThanThree())
            {
                Debug.Log($"<color=orange>{playerName} rolled 3 consecutive sixes - turn ended</color>");
                
                return;
            }
        }
        
        public override void ChooseTokenFrom(List<byte> movableTokens, byte diceValue)
        {
            if (movableTokens.Count == 0)
            {
                EndTurn();
                return;
            }
            byte bestToken = ChooseBestToken(movableTokens, diceValue);
            ExecuteMove(bestToken, diceValue);
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
            ChooseTokenFrom(movableTokens, dice);
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
    }
}