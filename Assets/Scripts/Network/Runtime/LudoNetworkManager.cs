using System;
using UnityEngine;
using UnityEngine.Events;

namespace Network.Runtime
{
    /// <summary>
    /// Manages WebSocket connection to the Ludo game server
    /// and handles all network communication
    /// </summary>
    public class LudoNetworkManager : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:8080";
        [SerializeField] private string playerName = "Unity Player";
        [SerializeField] private bool autoConnect = false;
        
        [Header("Debug")]
        [SerializeField] private bool logMessages = true;
        
        // Connection state
        private SimpleWebSocket webSocket;
        private bool isConnected = false;
        
        // Player/Game state
        private string playerId;
        private string sessionId;
        private int playerIndex = -1;
        private GameStatePayload currentGameState;
        
        #region Events
        [Header("Events")]
        public UnityEvent<string> OnConnected;
        public UnityEvent<GameCreatedPayload> OnGameCreated;
        public UnityEvent<GameJoinedPayload> OnGameJoined;
        public UnityEvent<PlayerJoinedPayload> OnPlayerJoined;
        public UnityEvent<GameStartedPayload> OnGameStarted;
        public UnityEvent<DiceRolledPayload> OnDiceRolled;
        public UnityEvent<TokenMovedPayload> OnTokenMoved;
        public UnityEvent<GameStatePayload> OnGameStateUpdated;
        public UnityEvent<GameOverPayload> OnGameOver;
        public UnityEvent<string> OnPlayerLeft;
        public UnityEvent<string> OnError;
        public UnityEvent OnDisconnected;
        #endregion
        
        #region Properties
        public bool IsConnected => isConnected;
        public string PlayerId => playerId;
        public string SessionId => sessionId;
        public int PlayerIndex => playerIndex;
        public GameStatePayload CurrentGameState => currentGameState;
        public bool IsMyTurn => currentGameState?.gameState != null && 
                                 currentGameState.gameState.currentPlayer == playerIndex;
        #endregion
        
        private void Awake()
        {
            webSocket = new SimpleWebSocket();
            
            webSocket.OnOpen += HandleWebSocketOpen;
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += HandleWebSocketError;
            webSocket.OnClose += HandleWebSocketClose;
        }
        
        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }
        
        private void Update()
        {
            // Process incoming messages on the main thread
            webSocket?.ProcessMessages();
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        #region Public API
        
        /// <summary>
        /// Connect to the game server
        /// </summary>
        public void Connect()
        {
            if (isConnected)
            {
                Debug.LogWarning("Already connected to server");
                return;
            }
            
            Log($"Connecting to {serverUrl}...");
            webSocket.Connect(serverUrl);
        }
        
        /// <summary>
        /// Disconnect from the game server
        /// </summary>
        public void Disconnect()
        {
            if (webSocket != null && isConnected)
            {
                webSocket.Close();
            }
        }
        
        /// <summary>
        /// Create a new game session
        /// </summary>
        public void CreateGame(int maxPlayers = 4, string customPlayerName = null)
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            var payload = new CreateGamePayload
            {
                maxPlayers = maxPlayers,
                playerName = customPlayerName ?? playerName
            };
            
            SendMessage(MessageType.CreateGame, payload);
        }
        
        /// <summary>
        /// Join an existing game session
        /// </summary>
        public void JoinGame(string gameSessionId, string customPlayerName = null)
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            var payload = new JoinGamePayload
            {
                sessionId = gameSessionId,
                playerName = customPlayerName ?? playerName
            };
            
            SendMessage(MessageType.JoinGame, payload);
        }
        
        /// <summary>
        /// Start the game (must be in a game session)
        /// </summary>
        public void StartGame()
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new StartGamePayload { playerId = playerId };
            SendMessage(MessageType.StartGame, payload);
        }
        
        /// <summary>
        /// Roll the dice (must be your turn)
        /// </summary>
        public void RollDice(int diceValue = 0)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new RollDicePayload 
            { 
                playerId = playerId,
                diceValue = diceValue // 0 for random
            };
            
            SendMessage(MessageType.RollDice, payload);
        }
        
        /// <summary>
        /// Move a token (must have rolled dice)
        /// </summary>
        public void MoveToken(int tokenIndex)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new MoveTokenPayload
            {
                playerId = playerId,
                tokenIndex = tokenIndex
            };
            
            SendMessage(MessageType.MoveToken, payload);
        }
        
        /// <summary>
        /// Request current game state
        /// </summary>
        public void RefreshGameState()
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new GetStatePayload { playerId = playerId };
            SendMessage(MessageType.GetState, payload);
        }
        
        /// <summary>
        /// Leave the current game
        /// </summary>
        public void LeaveGame()
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new LeaveGamePayload { playerId = playerId };
            SendMessage(MessageType.LeaveGame, payload);
        }
        
        /// <summary>
        /// List available games
        /// </summary>
        public void ListGames()
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            SendMessage(MessageType.ListGames, new { });
        }
        
        #endregion
        
        #region Message Handling
        
        private void HandleWebSocketOpen()
        {
            isConnected = true;
            Log("Connected to game server");
            OnConnected?.Invoke(serverUrl);
        }
        
        private void HandleWebSocketMessage(string message)
        {
            try
            {
                Log($"Received: {message}");
                
                var networkMessage = JsonUtility.FromJson<NetworkMessage>(message);
                var messageType = ParseMessageType(networkMessage.type);
                
                switch (messageType)
                {
                    case MessageType.Connected:
                        var connectedData = JsonUtility.FromJson<ConnectedPayload>(message);
                        Log($"Server says: {connectedData?.message ?? "Connected"}");
                        break;
                        
                    case MessageType.GameCreated:
                        var gameCreated = JsonUtility.FromJson<GameCreatedPayload>(networkMessage.payload);
                        playerId = gameCreated.playerId;
                        sessionId = gameCreated.sessionId;
                        playerIndex = gameCreated.playerIndex;
                        Log($"Game created! Session: {sessionId}, Player Index: {playerIndex}");
                        OnGameCreated?.Invoke(gameCreated);
                        break;
                        
                    case MessageType.GameJoined:
                        var gameJoined = JsonUtility.FromJson<GameJoinedPayload>(networkMessage.payload);
                        playerId = gameJoined.playerId;
                        sessionId = gameJoined.sessionId;
                        playerIndex = gameJoined.playerIndex;
                        Log($"Joined game! Player Index: {playerIndex}");
                        OnGameJoined?.Invoke(gameJoined);
                        break;
                        
                    case MessageType.PlayerJoined:
                        var playerJoined = JsonUtility.FromJson<PlayerJoinedPayload>(networkMessage.payload);
                        Log($"Player {playerJoined.playerIndex} ({playerJoined.playerName}) joined");
                        OnPlayerJoined?.Invoke(playerJoined);
                        break;
                        
                    case MessageType.GameStarted:
                        var gameStarted = JsonUtility.FromJson<GameStartedPayload>(networkMessage.payload);
                        Log($"Game started! Player {gameStarted.currentPlayer} goes first");
                        OnGameStarted?.Invoke(gameStarted);
                        break;
                        
                    case MessageType.DiceRolled:
                        var diceRolled = JsonUtility.FromJson<DiceRolledPayload>(networkMessage.payload);
                        Log($"Player {diceRolled.playerIndex} rolled {diceRolled.diceValue}");
                        OnDiceRolled?.Invoke(diceRolled);
                        break;
                        
                    case MessageType.TokenMoved:
                        var tokenMoved = JsonUtility.FromJson<TokenMovedPayload>(networkMessage.payload);
                        Log($"Token {tokenMoved.tokenIndex} moved: {tokenMoved.message}");
                        OnTokenMoved?.Invoke(tokenMoved);
                        break;
                        
                    case MessageType.GameState:
                        var gameState = JsonUtility.FromJson<GameStatePayload>(networkMessage.payload);
                        currentGameState = gameState;
                        OnGameStateUpdated?.Invoke(gameState);
                        break;
                        
                    case MessageType.GameOver:
                        var gameOver = JsonUtility.FromJson<GameOverPayload>(networkMessage.payload);
                        Log($"Game Over! Winner: Player {gameOver.winnerIndex} ({gameOver.winnerName})");
                        OnGameOver?.Invoke(gameOver);
                        break;
                        
                    case MessageType.Error:
                        var error = JsonUtility.FromJson<ErrorPayload>(networkMessage.payload);
                        LogError($"Server error: {error.error}");
                        OnError?.Invoke(error.error);
                        break;
                        
                    default:
                        Log($"Unhandled message type: {networkMessage.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                LogError($"Error parsing message: {e.Message}\nMessage: {message}");
            }
        }
        
        private void HandleWebSocketError(string error)
        {
            LogError($"WebSocket error: {error}");
            OnError?.Invoke(error);
        }
        
        private void HandleWebSocketClose()
        {
            isConnected = false;
            Log("Disconnected from server");
            OnDisconnected?.Invoke();
        }
        
        private void SendMessage<T>(MessageType messageType, T payload)
        {
            try
            {
                var messageTypeStr = messageType.ToString();
                // Convert from PascalCase to snake_case
                messageTypeStr = System.Text.RegularExpressions.Regex.Replace(
                    messageTypeStr, 
                    "([a-z])([A-Z])", 
                    "$1_$2"
                ).ToLower();
                
                var payloadJson = JsonUtility.ToJson(payload);
                var message = $"{{\"type\":\"{messageTypeStr}\",\"payload\":{payloadJson}}}";
                
                Log($"Sending: {message}");
                webSocket.Send(message);
            }
            catch (Exception e)
            {
                LogError($"Error sending message: {e.Message}");
            }
        }
        
        private MessageType ParseMessageType(string type)
        {
            // Convert snake_case to PascalCase
            var parts = type.Split('_');
            var pascalCase = "";
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    pascalCase += char.ToUpper(part[0]) + part.Substring(1);
                }
            }
            
            if (Enum.TryParse<MessageType>(pascalCase, out var messageType))
            {
                return messageType;
            }
            
            return MessageType.Error;
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            if (logMessages)
            {
                Debug.Log($"[LudoNetwork] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[LudoNetwork] {message}");
        }
        
        #endregion
    }
}
