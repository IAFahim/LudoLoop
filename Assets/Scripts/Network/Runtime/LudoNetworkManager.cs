using System;
using UnityEngine;
using UnityEngine.Events;

namespace Network.Runtime
{
    // ==================== REQUEST PAYLOADS ====================
    
    [Serializable]
    public class JoinQueuePayload
    {
        public string playerName;
        public string roomType;
        public int playerCount;
    }

    [Serializable]
    public class MoveTokenPayload
    {
        public int tokenIndex;
    }

    [Serializable]
    public class RollDicePayload
    {
        public int forcedValue; // Optional: 0 for random, 1-6 for testing
    }

    // ==================== RESPONSE CLASSES ====================

    [Serializable]
    public class ConnectedResponse
    {
        public string message;
        public string playerId;
    }

    [Serializable]
    public class QueueUpdateResponse
    {
        public string queueKey;
        public string roomType;
        public int currentPlayers;
        public int neededPlayers;
    }
    

    [Serializable]
    public class GameState
    {
        public int turnCount;
        public int diceValue;
        public int consecutiveSixes;
        public int currentPlayer;
        public int playerCount;
        public int[] tokenPositions;
    }

    [Serializable]
    public class MatchFoundResponse
    {
        public string sessionId;
        public int playerCount;
        public string roomType;
        public GameState gameState;
        public PlayerInfo[] players;
    }

    [Serializable]
    public class DiceRolledResponse
    {
        public bool success;
        public string playerId;
        public int playerIndex;
        public int diceValue;
        public int[] validMoves;
        public bool noValidMoves;
        public bool turnSwitched;
        public int nextPlayer;
    }

    [Serializable]
    public class TokenMovedResponse
    {
        public bool success;
        public string playerId;
        public int playerIndex;
        public int tokenIndex;
        public string moveResult;
        public int moveResultCode;
        public int diceValue;
        public int newPosition;
        public bool hasWon;
        public bool turnSwitched;
        public int nextPlayer;
        public GameState gameState;
    }

    [Serializable]
    public class GameOverResponse
    {
        public string winnerId;
        public int winnerIndex;
        public string winnerName;
    }

    [Serializable]
    public class PlayerDisconnectedResponse
    {
        public string playerId;
    }

    [Serializable]
    public class PlayerLeftResponse
    {
        public string playerId;
        public string playerName;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
    }

    [Serializable]
    public class GameStateResponse
    {
        public bool success;
        public string sessionId;
        public int playerIndex;
        public int playerCount;
        public int currentPlayer;
        public GameState gameState;
        public PlayerInfo[] players;
        public bool isGameOver;
        public string winnerId;
    }

    // ==================== MESSAGE WRAPPER ====================

    [Serializable]
    public class NetworkMessage<T>
    {
        public string type;
        public T payload;
    }

    // ==================== NETWORK MANAGER ====================

    /// <summary>
    /// Complete Network Manager for Ludo Game - Fixed for Server v1.0
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
        
        #region Unity Events
        
        [Header("Connection Events")]
        public UnityEvent onConnected;
        public UnityEvent<string> onDisconnected;
        public UnityEvent<string> onError;
        
        [Header("Matchmaking Events")]
        public UnityEvent<QueueUpdateResponse> onQueueUpdate;
        public UnityEvent<MatchFoundResponse> onMatchFound;
        
        [Header("Game Events")]
        public UnityEvent<DiceRolledResponse> onDiceRolled;
        public UnityEvent<TokenMovedResponse> onTokenMoved;
        public UnityEvent<GameOverResponse> onGameOver;
        public UnityEvent<GameStateResponse> onGameState;
        
        [Header("Player Events")]
        public UnityEvent<PlayerDisconnectedResponse> onPlayerDisconnected;
        public UnityEvent<PlayerLeftResponse> onPlayerLeft;
        public UnityEvent<string> onPlayerReconnected;
        
        #endregion
        
        #region Properties
        
        public bool IsConnected => isConnected;
        public string PlayerId => playerId;
        public string SessionId => sessionId;
        public int PlayerIndex => playerIndex;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            webSocket = new SimpleWebSocket();
            
            webSocket.OnOpen += HandleWebSocketOpen;
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += HandleWebSocketError;
            webSocket.OnClose += HandleWebSocketClose;
        }

        private void OnDisable()
        {
            if (webSocket != null)
            {
                webSocket.OnOpen -= HandleWebSocketOpen;
                webSocket.OnMessage -= HandleWebSocketMessage;
                webSocket.OnError -= HandleWebSocketError;
                webSocket.OnClose -= HandleWebSocketClose;
            }
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
            webSocket?.ProcessMessages();
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        #endregion
        
        #region WebSocket Handlers
        
        private void HandleWebSocketOpen()
        {
            isConnected = true;
            Log("Connected to server");
            onConnected?.Invoke();
        }

        private void HandleWebSocketMessage(string message)
        {
            Log($"Received: {message}");
            
            try
            {
                // Parse message type first
                var typeWrapper = JsonUtility.FromJson<MessageTypeWrapper>(message);
                
                switch (typeWrapper.type)
                {
                    case "connected":
                        HandleConnected(message);
                        break;
                        
                    case "queue_update":
                        HandleQueueUpdate(message);
                        break;
                        
                    case "match_found":
                        HandleMatchFound(message);
                        break;
                        
                    case "dice_rolled":
                        HandleDiceRolled(message);
                        break;
                        
                    case "token_moved":
                        HandleTokenMoved(message);
                        break;
                        
                    case "game_over":
                        HandleGameOver(message);
                        break;
                        
                    case "game_state":
                        HandleGameState(message);
                        break;
                        
                    case "player_disconnected":
                        HandlePlayerDisconnected(message);
                        break;
                        
                    case "player_reconnected":
                        HandlePlayerReconnected(message);
                        break;
                        
                    case "player_left":
                        HandlePlayerLeft(message);
                        break;
                        
                    case "left_queue":
                        Log("Successfully left queue");
                        break;
                        
                    case "left_game":
                        Log("Successfully left game");
                        ClearSessionData();
                        break;
                        
                    case "reconnected":
                        Log("Successfully reconnected to game");
                        HandleGameState(message);
                        break;
                        
                    case "error":
                        HandleError(message);
                        break;
                        
                    default:
                        Log($"Unhandled message type: {typeWrapper.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                LogError($"Error parsing message: {e.Message}\n{e.StackTrace}");
            }
        }

        private void HandleConnected(string json)
        {
            var msg = ParseMessage<ConnectedResponse>(json);
            playerId = msg.payload.playerId;
            Log($"Received player ID: {playerId}");
        }

        private void HandleQueueUpdate(string json)
        {
            var msg = ParseMessage<QueueUpdateResponse>(json);
            Log($"Queue update: {msg.payload.currentPlayers}/{msg.payload.neededPlayers}");
            onQueueUpdate?.Invoke(msg.payload);
        }

        private void HandleMatchFound(string json)
        {
            var msg = ParseMessage<MatchFoundResponse>(json);
            sessionId = msg.payload.sessionId;
            
            // Find our player index
            foreach (var player in msg.payload.players)
            {
                if (player.playerId == playerId)
                {
                    playerIndex = player.playerIndex;
                    break;
                }
            }
            
            Log($"Match found! Session: {sessionId}, Player Index: {playerIndex}");
            onMatchFound?.Invoke(msg.payload);
        }

        private void HandleDiceRolled(string json)
        {
            var msg = ParseMessage<DiceRolledResponse>(json);
            Log($"Dice rolled: {msg.payload.diceValue} by player {msg.payload.playerIndex}");
            onDiceRolled?.Invoke(msg.payload);
        }

        private void HandleTokenMoved(string json)
        {
            var msg = ParseMessage<TokenMovedResponse>(json);
            Log($"Token {msg.payload.tokenIndex} moved to {msg.payload.newPosition}");
            onTokenMoved?.Invoke(msg.payload);
        }

        private void HandleGameOver(string json)
        {
            var msg = ParseMessage<GameOverResponse>(json);
            Log($"Game Over! Winner: {msg.payload.winnerName}");
            onGameOver?.Invoke(msg.payload);
        }

        private void HandleGameState(string json)
        {
            var msg = ParseMessage<GameStateResponse>(json);
            onGameState?.Invoke(msg.payload);
        }

        private void HandlePlayerDisconnected(string json)
        {
            var msg = ParseMessage<PlayerDisconnectedResponse>(json);
            Log($"Player disconnected: {msg.payload.playerId}");
            onPlayerDisconnected?.Invoke(msg.payload);
        }

        private void HandlePlayerReconnected(string json)
        {
            var msg = ParseMessage<PlayerReconnectedPayload>(json);
            Log($"Player reconnected: {msg.payload.playerId}");
            onPlayerReconnected?.Invoke(msg.payload.playerId);
        }

        private void HandlePlayerLeft(string json)
        {
            var msg = ParseMessage<PlayerLeftResponse>(json);
            Log($"Player left: {msg.payload.playerName}");
            onPlayerLeft?.Invoke(msg.payload);
        }

        private void HandleError(string json)
        {
            var msg = ParseMessage<ErrorResponse>(json);
            LogError($"Server error: {msg.payload.error}");
            onError?.Invoke(msg.payload.error);
        }

        private void HandleWebSocketError(string error)
        {
            LogError($"WebSocket error: {error}");
            onError?.Invoke(error);
        }

        private void HandleWebSocketClose()
        {
            isConnected = false;
            Log("Disconnected from server");
            onDisconnected?.Invoke("Connection closed");
        }
        
        #endregion
        
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
        /// Join matchmaking queue
        /// </summary>
        /// <param name="roomType">Room type (e.g., "casual", "ranked", "100coins")</param>
        /// <param name="playerCount">Number of players (2-4)</param>
        /// <param name="customPlayerName">Optional custom player name</param>
        public void JoinQueue(string roomType = "casual", int playerCount = 4, string customPlayerName = null)
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            var payload = new JoinQueuePayload
            {
                playerName = string.IsNullOrEmpty(customPlayerName) ? playerName : customPlayerName,
                roomType = roomType,
                playerCount = playerCount
            };
            
            SendMessage("join_queue", payload);
            Log($"Joined queue for {roomType} with {playerCount} players");
        }
        
        /// <summary>
        /// Leave the current matchmaking queue
        /// </summary>
        public void LeaveQueue()
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            SendMessage("leave_queue", new {});
        }
        
        /// <summary>
        /// Roll the dice (only on your turn)
        /// </summary>
        /// <param name="forcedValue">Optional: Force specific dice value 1-6 for testing</param>
        public void RollDice(int forcedValue = 0)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new RollDicePayload { forcedValue = forcedValue };
            SendMessage("roll_dice", payload);
        }
        
        /// <summary>
        /// Move a token
        /// </summary>
        /// <param name="tokenIndex">Token index (0-15)</param>
        public void MoveToken(int tokenIndex)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            var payload = new MoveTokenPayload { tokenIndex = tokenIndex };
            SendMessage("move_token", payload);
        }
        
        /// <summary>
        /// Request current game state
        /// </summary>
        public void GetGameState()
        {
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("Not in a game session");
                return;
            }
            
            SendMessage("get_state", new {});
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
            
            SendMessage("leave_game", new {});
        }
        
        /// <summary>
        /// Reconnect to a game after disconnection
        /// </summary>
        public void Reconnect()
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
            if (string.IsNullOrEmpty(playerId))
            {
                LogError("No player ID to reconnect with");
                return;
            }
            
            SendMessage("reconnect", new {});
        }
        
        #endregion
        
        #region Helper Methods
        
        private void SendMessage<T>(string messageType, T payload)
        {
            var message = new NetworkMessage<T>
            {
                type = messageType,
                payload = payload
            };
            
            string json = JsonUtility.ToJson(message);
            Log($"Sending: {json}");
            webSocket.Send(json);
        }
        
        private NetworkMessage<T> ParseMessage<T>(string json)
        {
            return JsonUtility.FromJson<NetworkMessage<T>>(json);
        }
        
        private void ClearSessionData()
        {
            playerId = null;
            sessionId = null;
            playerIndex = -1;
        }
        
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

    // Helper class for parsing message type
    [Serializable]
    internal class MessageTypeWrapper
    {
        public string type;
    }

    [Serializable]
    internal class PlayerReconnectedPayload
    {
        public string playerId;
    }
}
