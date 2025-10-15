using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LudoGame.Runtime
{
    /// <summary>
    /// Handles game flow, turn management, and provides detailed feedback through events.
    /// </summary>
    public class SyncLudoGame : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Game Configuration")]
        [SerializeField] private int playerCount = 4;
        
        [Header("Debug Info (Read-Only)")]
        [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
        [SerializeField] private int currentPlayerIndex;
        [SerializeField] private int[] validTokenIndices = Array.Empty<int>();
        #endregion

        #region Events
        [Header("Game Events")]
        public UnityEvent<LudoGameState> onGameCreated;
        public UnityEvent<LudoGameState> onTurnStart;
        public UnityEvent<LudoGameState> onTokenMoved;
        public UnityEvent<LudoGameState> onMoveFailed;
        public UnityEvent<LudoGameState> onTurnEnd;
        public UnityEvent<LudoGameState> onPlayerWon;
        public UnityEvent<LudoGameState> onGameStateChanged;
        #endregion
        
        [Header("Sync Events")]
        public UnityEvent<BoardSyncEventData> onBoardSync;

        #region Private State
        private LudoGameState gameState;
        #endregion

        #region Properties
        public LudoGameState GameState => gameState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentPlayer => currentPlayerIndex;
        public int[] ValidTokens => validTokenIndices;
        public bool IsGameActive => currentPhase == GamePhase.Playing;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            CreateNewGame(playerCount);
        }
        #endregion

        #region Public API - Game Management
        /// <summary>
        /// Creates a new game with the specified number of players.
        /// </summary>
        public bool CreateNewGame(int numPlayers)
        {
            if (currentPhase == GamePhase.Playing)
            {
                Debug.LogWarning("Cannot create new game while a game is in progress.");
                return false;
            }

            if (!LudoGameState.TryCreate(numPlayers, out gameState, out LudoCreateResult createResult))
            {
                Debug.LogError($"Failed to create game: {createResult}");
                currentPhase = GamePhase.Error;
                return false;
            }

            // Initialize state
            playerCount = numPlayers;
            currentPlayerIndex = gameState.CurrentPlayer;
            validTokenIndices = Array.Empty<int>();
            currentPhase = GamePhase.Playing;

                
            Debug.Log($"Game created with {numPlayers} players. Player {currentPlayerIndex} starts.");
            return true;
        }

        /// <summary>
        /// Processes a dice roll (useful for deterministic testing or network games).
        /// </summary>
        public void ProcessDiceRoll(byte diceValue)
        {
            if (!ValidateGameState("ProcessDiceRoll")) return;
            
        }
        
        #endregion

        
        private bool ValidateGameState(string operation, bool logError = true)
        {
            if (currentPhase != GamePhase.Playing)
            {
                if (logError)
                    Debug.LogError($"Cannot {operation}: Game is not active.");
                return false;
            }
            return true;
        }
    }

    
    
    #region Enums
    public enum GamePhase
    {
        NotStarted,
        Playing,
        Finished,
        Error
    }
    #endregion
}