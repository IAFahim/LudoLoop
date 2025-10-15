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
        
        #region Events
        [Header("Events")]
        public UnityEvent onConnected;
        public UnityEvent<string> onMassage;
        public UnityEvent<string> onError;
        public UnityEvent onClose;
        #endregion
        
        #region Properties
        public bool IsConnected => isConnected;
        public string PlayerId => playerId;
        public string SessionId => sessionId;
        public int PlayerIndex => playerIndex;
        #endregion
        
        private void OnEnable()
        {
            webSocket = new SimpleWebSocket();
            
            webSocket.OnOpen += HandleWebSocketOpen;
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += HandleWebSocketError;
            webSocket.OnClose += HandleWebSocketClose;
        }

        private void HandleWebSocketOpen()
        {
            
        }

        private void HandleWebSocketMessage(string obj)
        {
            
        }

        private void HandleWebSocketError(string obj)
        {
            
        }

        private void HandleWebSocketClose()
        {
            
        }

        private void OnDisable()
        {
            webSocket.OnOpen -= HandleWebSocketOpen;
            webSocket.OnMessage -= HandleWebSocketMessage;
            webSocket.OnError -= HandleWebSocketError;
            webSocket.OnClose -= HandleWebSocketClose;
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
        /// Join an existing game session
        /// </summary>
        public void JoinGame(string gameSessionId, string customPlayerName = null)
        {
            if (!isConnected)
            {
                LogError("Not connected to server");
                return;
            }
            
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
        }
        
        
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
