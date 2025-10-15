using UnityEngine;
using LudoGame.Runtime;
using Network.Runtime;

namespace Network.Runtime
{
    /// <summary>
    /// Bridges the network game state with the local LudoGame visualization
    /// </summary>
    [RequireComponent(typeof(LudoNetworkManager))]
    public class NetworkGameBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OfflineLudoGame offlineLudoGame;
        [SerializeField] private LudoNetworkManager networkManager;
        
        [Header("Settings")]
        [SerializeField] private bool syncToLocalGame = true;
        
        private void Awake()
        {
            if (networkManager == null)
                networkManager = GetComponent<LudoNetworkManager>();
        }
        
        private void OnEnable()
        {
            if (networkManager != null)
            {
                networkManager.OnGameStarted.AddListener(HandleGameStarted);
                networkManager.OnDiceRolled.AddListener(HandleDiceRolled);
                networkManager.OnTokenMoved.AddListener(HandleTokenMoved);
                networkManager.OnGameStateUpdated.AddListener(HandleGameStateUpdated);
            }
        }
        
        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnGameStarted.RemoveListener(HandleGameStarted);
                networkManager.OnDiceRolled.RemoveListener(HandleDiceRolled);
                networkManager.OnTokenMoved.RemoveListener(HandleTokenMoved);
                networkManager.OnGameStateUpdated.RemoveListener(HandleGameStateUpdated);
            }
        }
        
        private void HandleGameStarted(GameStartedPayload payload)
        {
            if (!syncToLocalGame || offlineLudoGame == null) return;
            
            Debug.Log($"[NetworkBridge] Game started with {payload.playerCount} players");
            
            // Apply the game state from server
            if (payload.gameState != null)
            {
                ApplyNetworkGameState(payload.gameState);
            }
        }
        
        private void HandleDiceRolled(DiceRolledPayload payload)
        {
            Debug.Log($"[NetworkBridge] Player {payload.playerIndex} rolled {payload.diceValue}");
            
            // If it's our turn and we're the one who rolled, process it locally
            if (payload.playerIndex == networkManager.PlayerIndex && offlineLudoGame != null)
            {
                offlineLudoGame.ProcessDiceRoll((byte)payload.diceValue);
            }
        }
        
        private void HandleTokenMoved(TokenMovedPayload payload)
        {
            if (!syncToLocalGame || offlineLudoGame == null) return;
            
            Debug.Log($"[NetworkBridge] Token {payload.tokenIndex} moved to {payload.newPosition}");
            
            // Apply the updated game state
            if (payload.gameState != null)
            {
                ApplyNetworkGameState(payload.gameState);
            }
        }
        
        private void HandleGameStateUpdated(GameStatePayload payload)
        {
            if (!syncToLocalGame || offlineLudoGame == null) return;
            
            if (payload.gameState != null)
            {
                ApplyNetworkGameState(payload.gameState);
            }
        }
        
        /// <summary>
        /// Applies network game state to the local OfflineLudoGame
        /// </summary>
        private void ApplyNetworkGameState(LudoGameStateData networkState)
        {
            // Create a LudoGameState from network data
            var localState = new LudoGameState
            {
                TurnCount = (ushort)networkState.turnCount,
                DiceValue = networkState.diceValue,
                ConsecutiveSixes = networkState.consecutiveSixes,
                CurrentPlayer = networkState.currentPlayer,
                PlayerCount = networkState.playerCount,
                TokenPositions = new sbyte[16]
            };
            
            // Copy token positions
            for (int i = 0; i < 16 && i < networkState.tokenPositions.Length; i++)
            {
                localState.TokenPositions[i] = (sbyte)networkState.tokenPositions[i];
            }
            
            // Serialize and load into the offline game
            string serialized = localState.Serialize();
            offlineLudoGame.LoadGameState(serialized);
            
            Debug.Log($"[NetworkBridge] Applied network state: Player {localState.CurrentPlayer}'s turn");
        }
        
        /// <summary>
        /// Helper method to automatically play as network client
        /// Call this when it's your turn and you want to roll dice
        /// </summary>
        public void RollDice()
        {
            if (networkManager.IsMyTurn)
            {
                networkManager.RollDice();
            }
            else
            {
                Debug.LogWarning("Not your turn!");
            }
        }
        
        /// <summary>
        /// Helper method to move a token
        /// </summary>
        public void MoveToken(int tokenIndex)
        {
            if (networkManager.IsMyTurn)
            {
                networkManager.MoveToken(tokenIndex);
            }
            else
            {
                Debug.LogWarning("Not your turn!");
            }
        }
    }
}
