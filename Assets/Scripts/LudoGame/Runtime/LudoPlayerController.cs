using System.Collections.Generic;
using UnityEngine;

namespace Ludo
{
    /// <summary>
    /// Base class for all player types (Human, AI, etc.)
    /// </summary>
    public abstract class LudoPlayerController : MonoBehaviour
    {
        [Header("Player Info")]
        public int playerIndex;
        public string playerName;
        public PlayerType playerType;
        
        [Header("References")]
        public LudoGamePlay ludoGamePlay;
        
        protected GameSession Session => ludoGamePlay.gameSession;
        public bool IsMyTurn => Session.currentPlayerIndex == playerIndex;
        
        protected virtual void OnValidate()
        {
            if (ludoGamePlay == null)
                ludoGamePlay = GetComponent<LudoGamePlay>();
        }

        /// <summary>
        /// Called when it's this player's turn to act
        /// </summary>
        public abstract void OnTurnStart();
        
        /// <summary>
        /// Called when player needs to choose which token to move
        /// </summary>
        public abstract void OnChooseToken(List<byte> movableTokens, byte diceValue);
        
        /// <summary>
        /// Executes the token move
        /// </summary>
        protected void ExecuteMove(byte tokenIndex, byte dice)
        {
            var session = Session;
            session.tokenToMove = tokenIndex;
            
            // Move token
            ludoGamePlay.MoveToken(tokenIndex, dice, out var tokenSentToBase);
            session.tokenSentToBase = tokenSentToBase;
            
            // Log capture
            if (tokenSentToBase != LudoBoard.NoTokenSentToBaseCode)
            {
                int capturedPlayer = tokenSentToBase / LudoBoard.Tokens;
                int capturedTokenOrdinal = tokenSentToBase % LudoBoard.Tokens;
                Debug.Log($"<color=red>★ Player {playerIndex} captured Player {capturedPlayer}'s token {capturedTokenOrdinal}!</color>");
            }
            
            // Check win
            if (session.CheckWinCondition(session.currentPlayerIndex))
            {
                Debug.Log($"<color=yellow>★★★ {playerName} (Player {playerIndex}) WINS! ★★★</color>");
                OnGameEnd();
                return;
            }
            
            // Handle turn passing
            if (session.ShouldPassTurn(dice))
            {
                session.EndTurn();
            }
            else
            {
                OnTurnStart();
            }
        }
        
        protected virtual void OnGameEnd()
        {
        }
    }

    public enum PlayerType
    {
        Human,
        AI,
        LocalHuman,
        NetworkHuman
    }
}