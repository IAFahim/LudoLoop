using System;
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
        public byte currentToken;
        
        [Header("References")]
        public LudoGamePlay ludoGamePlay;
        protected GameSession Session => ludoGamePlay.gameSession;
        public bool IsMyTurn => Session.currentPlayerIndex == playerIndex;
        public abstract void OnTurn();
        
        public abstract void ChooseTokenFrom(List<byte> movableTokens, byte diceValue);
        
        protected void ExecuteMove(byte tokenIndex, byte dice)
        {
            currentToken = tokenIndex;
            Session.tokenToMove = tokenIndex;
            
            // Move token
            ludoGamePlay.Play(tokenIndex, dice, out var tokenSentToBase);
            Session.tokenSentToBase = tokenSentToBase;
            
            // Log capture
            if (tokenSentToBase != LudoBoard.NoTokenSentToBaseCode)
            {
                int capturedPlayer = tokenSentToBase / LudoBoard.Tokens;
                int capturedTokenOrdinal = tokenSentToBase % LudoBoard.Tokens;
                Debug.Log($"<color=red>★ Player {playerIndex} captured Player {capturedPlayer}'s token {capturedTokenOrdinal}!</color>");
            }
            
            // Check win
            if (Session.CheckWinCondition(Session.currentPlayerIndex))
            {
                Debug.Log($"<color=yellow>★★★ {playerName} (Player {playerIndex}) WINS! ★★★</color>");
                OnGameEnd();
                return;
            }
            
            if (Session.ShouldPassTurn(dice))
            {
                EndTurn();
                return;
            }

            OnTurn();
        }


        protected virtual void EndTurn()
        {
            
            ludoGamePlay.EndTurn(playerIndex);
        }
        

        protected virtual void OnGameEnd()
        {
            ludoGamePlay.EndTurn(playerIndex);
        }
    }

    public enum PlayerType
    {
        Human,
        AI,
        LocalHuman,
        NetworkPlayer
    }
}