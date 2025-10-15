using UnityEngine;
using Network.Runtime;
using LudoGame.Runtime;

namespace Network.Runtime
{
    /// <summary>
    /// Example script showing how to set up a complete networked Ludo game
    /// Attach this to a GameObject in your scene
    /// </summary>
    public class NetworkGameExample : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:8080";
        [SerializeField] private string playerName = "Unity Player";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private bool autoCreateGame = false;
        
        [Header("Game Settings")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private bool autoStartGame = false;
        
        [Header("References (Auto-created if null)")]
        [SerializeField] private LudoNetworkManager networkManager;
        [SerializeField] private NetworkGameBridge gameBridge;
        [SerializeField] private OfflineLudoGame offlineLudoGame;
        
        [Header("Status (Read-Only)")]
        [SerializeField] private bool connected = false;
        [SerializeField] private bool inGame = false;
        [SerializeField] private string currentSessionId;
        [SerializeField] private int myPlayerIndex = -1;
        
        private void Awake()
        {
            SetupComponents();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            
            if (autoConnect)
            {
                networkManager.Connect();
            }
        }
        
        private void SetupComponents()
        {
            // Find or create network manager
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<LudoNetworkManager>();
                if (networkManager == null)
                {
                    var go = new GameObject("Network Manager");
                    networkManager = go.AddComponent<LudoNetworkManager>();
                    Debug.Log("Created LudoNetworkManager");
                }
            }
            
            // Find or create game bridge
            if (gameBridge == null)
            {
                gameBridge = FindObjectOfType<NetworkGameBridge>();
                if (gameBridge == null && networkManager != null)
                {
                    gameBridge = networkManager.gameObject.AddComponent<NetworkGameBridge>();
                    Debug.Log("Created NetworkGameBridge");
                }
            }
            
            // Find offline game (should exist in scene)
            if (offlineLudoGame == null)
            {
                offlineLudoGame = FindObjectOfType<OfflineLudoGame>();
            }
            
            // Link components
            if (gameBridge != null && offlineLudoGame != null)
            {
                // Use reflection to set the private field (or make it public in NetworkGameBridge)
                var field = typeof(NetworkGameBridge).GetField("offlineLudoGame", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(gameBridge, offlineLudoGame);
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            if (networkManager == null) return;
            
            networkManager.OnConnected.AddListener(OnConnected);
            networkManager.OnDisconnected.AddListener(OnDisconnected);
            networkManager.OnGameCreated.AddListener(OnGameCreated);
            networkManager.OnGameJoined.AddListener(OnGameJoined);
            networkManager.OnGameStarted.AddListener(OnGameStarted);
            networkManager.OnDiceRolled.AddListener(OnDiceRolled);
            networkManager.OnTokenMoved.AddListener(OnTokenMoved);
            networkManager.OnGameOver.AddListener(OnGameOver);
            networkManager.OnError.AddListener(OnNetworkError);
        }
        
        private void Update()
        {
            // Update status
            connected = networkManager != null && networkManager.IsConnected;
            inGame = !string.IsNullOrEmpty(networkManager?.SessionId);
            currentSessionId = networkManager?.SessionId;
            myPlayerIndex = networkManager?.PlayerIndex ?? -1;
        }
        
        #region Event Handlers
        
        private void OnConnected(string url)
        {
            Debug.Log($"[NetworkExample] Connected to {url}");
            
            if (autoCreateGame)
            {
                networkManager.CreateGame(maxPlayers, playerName);
            }
        }
        
        private void OnDisconnected()
        {
            Debug.Log("[NetworkExample] Disconnected from server");
        }
        
        private void OnGameCreated(GameCreatedPayload payload)
        {
            Debug.Log($"[NetworkExample] Game created! Session ID: {payload.sessionId}");
            Debug.Log($"[NetworkExample] You are Player {payload.playerIndex}");
            Debug.Log($"[NetworkExample] Share this session ID with other players to join");
            
            if (autoStartGame && payload.maxPlayers == 1)
            {
                // Single player game, start immediately
                networkManager.StartGame();
            }
        }
        
        private void OnGameJoined(GameJoinedPayload payload)
        {
            Debug.Log($"[NetworkExample] Joined game as Player {payload.playerIndex}");
        }
        
        private void OnGameStarted(GameStartedPayload payload)
        {
            Debug.Log($"[NetworkExample] Game started with {payload.playerCount} players!");
            Debug.Log($"[NetworkExample] Player {payload.currentPlayer} goes first");
            
            if (payload.currentPlayer == myPlayerIndex)
            {
                Debug.Log("[NetworkExample] It's your turn! Roll the dice.");
            }
        }
        
        private void OnDiceRolled(DiceRolledPayload payload)
        {
            if (payload.playerIndex == myPlayerIndex)
            {
                Debug.Log($"[NetworkExample] You rolled {payload.diceValue}");
                
                if (payload.noValidMoves)
                {
                    Debug.Log("[NetworkExample] No valid moves. Turn skipped.");
                }
                else
                {
                    Debug.Log($"[NetworkExample] Valid moves: {string.Join(", ", payload.validMoves)}");
                    Debug.Log($"[NetworkExample] Select a token to move (call MoveToken)");
                }
            }
            else
            {
                Debug.Log($"[NetworkExample] Player {payload.playerIndex} rolled {payload.diceValue}");
            }
        }
        
        private void OnTokenMoved(TokenMovedPayload payload)
        {
            string who = payload.playerIndex == myPlayerIndex ? "You" : $"Player {payload.playerIndex}";
            Debug.Log($"[NetworkExample] {who} moved token {payload.tokenIndex}: {payload.message}");
            
            if (payload.hasWon)
            {
                if (payload.playerIndex == myPlayerIndex)
                {
                    Debug.Log("[NetworkExample] 🎉 YOU WON! 🎉");
                }
                else
                {
                    Debug.Log($"[NetworkExample] Player {payload.playerIndex} won!");
                }
            }
            else if (networkManager.IsMyTurn)
            {
                Debug.Log("[NetworkExample] It's your turn! Roll the dice.");
            }
        }
        
        private void OnGameOver(GameOverPayload payload)
        {
            Debug.Log($"[NetworkExample] GAME OVER! Winner: {payload.winnerName} (Player {payload.winnerIndex})");
        }
        
        private void OnNetworkError(string error)
        {
            Debug.LogError($"[NetworkExample] Error: {error}");
        }
        
        #endregion
        
        #region Public Methods (Can be called from UI or other scripts)
        
        public void ConnectToServer()
        {
            if (networkManager != null)
            {
                networkManager.Connect();
            }
        }
        
        public void CreateNewGame()
        {
            if (networkManager != null)
            {
                networkManager.CreateGame(maxPlayers, playerName);
            }
        }
        
        public void JoinGameBySessionId(string sessionId)
        {
            if (networkManager != null)
            {
                networkManager.JoinGame(sessionId, playerName);
            }
        }
        
        public void StartGameNow()
        {
            if (networkManager != null)
            {
                networkManager.StartGame();
            }
        }
        
        public void RollDiceNow()
        {
            if (gameBridge != null)
            {
                gameBridge.RollDice();
            }
            else if (networkManager != null)
            {
                networkManager.RollDice();
            }
        }
        
        public void MoveTokenByIndex(int tokenIndex)
        {
            if (gameBridge != null)
            {
                gameBridge.MoveToken(tokenIndex);
            }
            else if (networkManager != null)
            {
                networkManager.MoveToken(tokenIndex);
            }
        }
        
        #endregion
    }
}
