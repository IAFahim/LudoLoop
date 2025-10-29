using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using UnityEngine.Events;

namespace Ludo
{
    /// <summary>
    /// Human player controller - waits for UI/input to make decisions
    /// </summary>
    public class LudoHumanPlayer : LudoPlayerController
    {
        [Header("Human Player Settings")]
        public bool autoRollDice = false;
        public float autoRollDelay = 0.5f;
        
        [Header("UI Events")]
        public UnityEvent<int> onTurnStarted; // Player index
        public UnityEvent<byte> onDiceRolled; // Dice value
        public UnityEvent<List<byte>, byte> onTokenSelectionRequired; // Movable tokens, dice value
        public UnityEvent<int> onNoMovesAvailable; // Player index
        
        private float autoRollTimer;
        private bool waitingForDiceRoll;
        private bool waitingForTokenSelection;
        private List<byte> currentMovableTokens;
        private byte currentDiceValue;

        private void Update()
        {
            if (!IsMyTurn) return;
            
            // Auto roll dice after delay
            if (waitingForDiceRoll && autoRollDice)
            {
                autoRollTimer += Time.deltaTime;
                if (autoRollTimer >= autoRollDelay)
                {
                    RollDice();
                }
            }
        }

        [Button]
        public override void OnTurnStart()
        {
            if (!IsMyTurn) return;
            
            Debug.Log($"<color=cyan>═══ {playerName}'s Turn ═══</color>");
            onTurnStarted?.Invoke(playerIndex);
            
            // Check consecutive sixes
            if (!Session.ConsecutiveSixLessThanThree())
            {
                Debug.Log($"<color=orange>{playerName} rolled 3 consecutive sixes - turn ended</color>");
                Session.EndTurn();
                return;
            }
            
            // Wait for dice roll
            waitingForDiceRoll = true;
            autoRollTimer = 0f;
        }

        [Button("Roll Dice")]
        public void RollDice()
        {
            if (!IsMyTurn || !waitingForDiceRoll) return;
            
            waitingForDiceRoll = false;
            
            byte dice = Session.RollDice();
            Debug.Log($"<color=yellow>{playerName} rolled: {dice}</color>");
            onDiceRolled?.Invoke(dice);
            
            // Try exit from base
            if (dice == LudoBoard.ExitRoll && Session.HasTokensInBase(playerIndex))
            {
                if (Session.TryExitTokenFromBase(playerIndex))
                {
                    Debug.Log($"<color=green>{playerName} exited a token from base</color>");
                }
            }
            
            // Get movable tokens
            var movableTokens = Session.GetMovableTokens(playerIndex, dice);
            
            if (movableTokens.Count == 0)
            {
                Debug.Log($"<color=yellow>{playerName} has no valid moves</color>");
                onNoMovesAvailable?.Invoke(playerIndex);
                Session.EndTurn();
                return;
            }
            
            // Store for token selection
            currentMovableTokens = movableTokens;
            currentDiceValue = dice;
            Session.currentMoveableTokens = movableTokens;
            
            // If only one token can move, auto-select it
            if (movableTokens.Count == 1)
            {
                SelectToken(movableTokens[0]);
            }
            else
            {
                OnChooseToken(movableTokens, dice);
            }
        }

        public override void OnChooseToken(List<byte> movableTokens, byte diceValue)
        {
            Debug.Log($"<color=magenta>{playerName} needs to choose a token to move</color>");
            waitingForTokenSelection = true;
            onTokenSelectionRequired?.Invoke(movableTokens, diceValue);
        }

        /// <summary>
        /// Called by UI when player selects a token
        /// </summary>
        [Button("Select Token 0")]
        public void SelectToken0() => SelectToken((byte)(playerIndex * LudoBoard.Tokens + 0));
        
        [Button("Select Token 1")]
        public void SelectToken1() => SelectToken((byte)(playerIndex * LudoBoard.Tokens + 1));
        
        [Button("Select Token 2")]
        public void SelectToken2() => SelectToken((byte)(playerIndex * LudoBoard.Tokens + 2));
        
        [Button("Select Token 3")]
        public void SelectToken3() => SelectToken((byte)(playerIndex * LudoBoard.Tokens + 3));

        public void SelectToken(byte tokenIndex)
        {
            if (!IsMyTurn || !waitingForTokenSelection) return;
            
            // Validate token is in movable list
            if (currentMovableTokens == null || !currentMovableTokens.Contains(tokenIndex))
            {
                Debug.LogWarning($"Token {tokenIndex} is not movable!");
                return;
            }
            
            waitingForTokenSelection = false;
            
            int tokenOrdinal = tokenIndex % LudoBoard.Tokens;
            Debug.Log($"<color=lime>{playerName} selected token {tokenOrdinal}</color>");
            
            ExecuteMove(tokenIndex, currentDiceValue);
        }

        protected override void OnGameEnd()
        {
            waitingForDiceRoll = false;
            waitingForTokenSelection = false;
        }
    }
}