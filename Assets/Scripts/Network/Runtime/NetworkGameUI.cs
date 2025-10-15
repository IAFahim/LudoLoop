using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Network.Runtime;

namespace Network.Runtime
{
    /// <summary>
    /// Simple UI for network game controls
    /// Attach this to a Canvas in your scene
    /// </summary>
    public class NetworkGameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LudoNetworkManager networkManager;
        [SerializeField] private NetworkGameBridge gameBridge;
        
        [Header("UI - Connection")]
        [SerializeField] private TMP_InputField serverUrlInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI connectionStatus;
        
        [Header("UI - Game Setup")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField sessionIdInput;
        [SerializeField] private Button createGameButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Button startGameButton;
        
        [Header("UI - Gameplay")]
        [SerializeField] private Button rollDiceButton;
        [SerializeField] private TMP_InputField tokenIndexInput;
        [SerializeField] private Button moveTokenButton;
        [SerializeField] private TextMeshProUGUI gameInfoText;
        [SerializeField] private TextMeshProUGUI turnInfoText;
        
        [Header("UI - Messages")]
        [SerializeField] private TextMeshProUGUI messagesText;
        [SerializeField] private ScrollRect messagesScroll;
        
        private string messageLog = "";
        
        private void Start()
        {
            SetupUI();
            SubscribeToEvents();
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SetupUI()
        {
            // Connection buttons
            if (connectButton) connectButton.onClick.AddListener(OnConnectClicked);
            if (disconnectButton) disconnectButton.onClick.AddListener(OnDisconnectClicked);
            
            // Game setup buttons
            if (createGameButton) createGameButton.onClick.AddListener(OnCreateGameClicked);
            if (joinGameButton) joinGameButton.onClick.AddListener(OnJoinGameClicked);
            if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
            
            // Gameplay buttons
            if (rollDiceButton) rollDiceButton.onClick.AddListener(OnRollDiceClicked);
            if (moveTokenButton) moveTokenButton.onClick.AddListener(OnMoveTokenClicked);
        }
        
        private void SubscribeToEvents()
        {
            if (networkManager == null) return;
            
            networkManager.OnConnected.AddListener(OnConnected);
            networkManager.OnDisconnected.AddListener(OnDisconnected);
            networkManager.OnGameCreated.AddListener(OnGameCreated);
            networkManager.OnGameJoined.AddListener(OnGameJoined);
            networkManager.OnPlayerJoined.AddListener(OnPlayerJoined);
            networkManager.OnGameStarted.AddListener(OnGameStarted);
            networkManager.OnDiceRolled.AddListener(OnDiceRolled);
            networkManager.OnTokenMoved.AddListener(OnTokenMoved);
            networkManager.OnGameStateUpdated.AddListener(OnGameStateUpdated);
            networkManager.OnGameOver.AddListener(OnGameOver);
            networkManager.OnError.AddListener(OnError);
        }
        
        private void UnsubscribeFromEvents()
        {
            if (networkManager == null) return;
            
            networkManager.OnConnected.RemoveListener(OnConnected);
            networkManager.OnDisconnected.RemoveListener(OnDisconnected);
            networkManager.OnGameCreated.RemoveListener(OnGameCreated);
            networkManager.OnGameJoined.RemoveListener(OnGameJoined);
            networkManager.OnPlayerJoined.RemoveListener(OnPlayerJoined);
            networkManager.OnGameStarted.RemoveListener(OnGameStarted);
            networkManager.OnDiceRolled.RemoveListener(OnDiceRolled);
            networkManager.OnTokenMoved.RemoveListener(OnTokenMoved);
            networkManager.OnGameStateUpdated.RemoveListener(OnGameStateUpdated);
            networkManager.OnGameOver.RemoveListener(OnGameOver);
            networkManager.OnError.RemoveListener(OnError);
        }
        
        private void Update()
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (networkManager == null) return;
            
            bool connected = networkManager.IsConnected;
            bool inGame = !string.IsNullOrEmpty(networkManager.SessionId);
            bool isMyTurn = networkManager.IsMyTurn;
            
            // Connection status
            if (connectionStatus)
            {
                connectionStatus.text = connected ? "Connected" : "Disconnected";
                connectionStatus.color = connected ? Color.green : Color.red;
            }
            
            // Enable/disable buttons
            if (connectButton) connectButton.interactable = !connected;
            if (disconnectButton) disconnectButton.interactable = connected;
            if (createGameButton) createGameButton.interactable = connected && !inGame;
            if (joinGameButton) joinGameButton.interactable = connected && !inGame;
            if (startGameButton) startGameButton.interactable = inGame;
            if (rollDiceButton) rollDiceButton.interactable = inGame && isMyTurn;
            if (moveTokenButton) moveTokenButton.interactable = inGame && isMyTurn;
            
            // Game info
            if (gameInfoText && inGame)
            {
                var state = networkManager.CurrentGameState;
                gameInfoText.text = $"Session: {networkManager.SessionId?.Substring(0, 8)}...\n" +
                                   $"Player Index: {networkManager.PlayerIndex}\n" +
                                   $"Players: {state?.playerCount ?? 0}";
            }
            
            // Turn info
            if (turnInfoText && inGame)
            {
                var state = networkManager.CurrentGameState;
                if (state?.gameState != null)
                {
                    turnInfoText.text = $"Current Turn: Player {state.gameState.currentPlayer}\n" +
                                       $"Your Turn: {(isMyTurn ? "YES" : "NO")}\n" +
                                       $"Last Dice: {state.currentDiceRoll}";
                }
            }
        }
        
        #region Button Handlers
        
        private void OnConnectClicked()
        {
            if (serverUrlInput && !string.IsNullOrEmpty(serverUrlInput.text))
            {
                // Update the network manager's server URL via reflection or expose it
                AddMessage($"Connecting to server...");
            }
            networkManager.Connect();
        }
        
        private void OnDisconnectClicked()
        {
            networkManager.Disconnect();
        }
        
        private void OnCreateGameClicked()
        {
            string playerName = playerNameInput?.text ?? "Player";
            networkManager.CreateGame(4, playerName);
        }
        
        private void OnJoinGameClicked()
        {
            string sessionId = sessionIdInput?.text;
            if (string.IsNullOrEmpty(sessionId))
            {
                AddMessage("Please enter a session ID", true);
                return;
            }
            
            string playerName = playerNameInput?.text ?? "Player";
            networkManager.JoinGame(sessionId, playerName);
        }
        
        private void OnStartGameClicked()
        {
            networkManager.StartGame();
        }
        
        private void OnRollDiceClicked()
        {
            if (gameBridge)
            {
                gameBridge.RollDice();
            }
            else
            {
                networkManager.RollDice();
            }
        }
        
        private void OnMoveTokenClicked()
        {
            if (tokenIndexInput && int.TryParse(tokenIndexInput.text, out int tokenIndex))
            {
                if (gameBridge)
                {
                    gameBridge.MoveToken(tokenIndex);
                }
                else
                {
                    networkManager.MoveToken(tokenIndex);
                }
            }
            else
            {
                AddMessage("Please enter a valid token index (0-15)", true);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnConnected(string url)
        {
            AddMessage($"✅ Connected to {url}");
        }
        
        private void OnDisconnected()
        {
            AddMessage("❌ Disconnected from server");
        }
        
        private void OnGameCreated(GameCreatedPayload payload)
        {
            AddMessage($"🎮 Game created! Session: {payload.sessionId}");
            if (sessionIdInput)
            {
                sessionIdInput.text = payload.sessionId;
            }
        }
        
        private void OnGameJoined(GameJoinedPayload payload)
        {
            AddMessage($"✅ Joined game as Player {payload.playerIndex}");
        }
        
        private void OnPlayerJoined(PlayerJoinedPayload payload)
        {
            AddMessage($"👤 {payload.playerName} joined as Player {payload.playerIndex}");
        }
        
        private void OnGameStarted(GameStartedPayload payload)
        {
            AddMessage($"🎲 Game started! Player {payload.currentPlayer} goes first");
        }
        
        private void OnDiceRolled(DiceRolledPayload payload)
        {
            string msg = $"🎲 Player {payload.playerIndex} rolled {payload.diceValue}";
            if (payload.noValidMoves)
            {
                msg += " (No valid moves)";
            }
            else
            {
                msg += $" (Valid: {string.Join(", ", payload.validMoves)})";
            }
            AddMessage(msg);
        }
        
        private void OnTokenMoved(TokenMovedPayload payload)
        {
            AddMessage($"♟️ Player {payload.playerIndex} moved token {payload.tokenIndex}: {payload.message}");
            if (payload.hasWon)
            {
                AddMessage($"🎉 Player {payload.playerIndex} WON!");
            }
        }
        
        private void OnGameStateUpdated(GameStatePayload payload)
        {
            // State updated silently
        }
        
        private void OnGameOver(GameOverPayload payload)
        {
            AddMessage($"🏆 GAME OVER! Winner: Player {payload.winnerIndex} ({payload.winnerName})");
        }
        
        private void OnError(string error)
        {
            AddMessage($"❌ Error: {error}", true);
        }
        
        #endregion
        
        private void AddMessage(string message, bool isError = false)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string prefix = isError ? "<color=red>" : "";
            string suffix = isError ? "</color>" : "";
            
            messageLog += $"{prefix}[{timestamp}] {message}{suffix}\n";
            
            if (messagesText)
            {
                messagesText.text = messageLog;
            }
            
            // Auto-scroll to bottom
            if (messagesScroll)
            {
                Canvas.ForceUpdateCanvases();
                messagesScroll.verticalNormalizedPosition = 0f;
            }
        }
    }
}
