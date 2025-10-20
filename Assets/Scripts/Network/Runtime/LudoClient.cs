using System;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

// Step 1: Add the Unity Events namespace

namespace Network.Runtime
{
    /// <summary>
    /// Complete Unity client for Ludo Game Server with automatic matchmaking.
    /// Drop this script into your Unity project and call FindMatch() to start playing.
    /// </summary>
    public class LudoClient : MonoBehaviour
    {
        [Header("Server Configuration")] [SerializeField]
        private string serverUrl = "ws://localhost:8080";

        [SerializeField] private string playerName = "Player";

        [Header("Events")]
        // Step 2: Convert Actions to UnityEvents
        public UnityEvent<string> OnConnected;

        public UnityEvent OnDisconnected;
        public UnityEvent<string> OnError;
        public UnityEvent<int> OnQueueJoined;
        public UnityEvent<string> OnPlayerLeft;
        public UnityEvent<string> OnPlayerDisconnected;
        public UnityEvent<string> OnPlayerReconnected;

        // Step 3: Use custom UnityEvent subclasses for complex data types
        public MatchDataEvent OnMatchFound;
        public DiceRollDataEvent OnDiceRolled;
        public TokenMoveDataEvent OnTokenMoved;
        public GameOverDataEvent OnGameOver;

        private WebSocket websocket;
        private string myPlayerId;
        private string currentSessionId;
        private int myPlayerIndex = -1;
        private GameStateData currentGameState;
        private bool isInQueue = false;
        private bool isInGame = false;

        // ==================== PUBLIC API ====================

        public async void Connect()
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                Debug.LogWarning("Already connected to server");
                return;
            }

            try
            {
                websocket = new WebSocket(serverUrl);

                websocket.OnOpen += () => { Debug.Log("Connected to Ludo Game Server"); };

                websocket.OnError += (e) =>
                {
                    Debug.LogError("WebSocket Error: " + e);
                    OnError?.Invoke(e);
                };

                websocket.OnClose += (e) =>
                {
                    Debug.Log("Disconnected from server");
                    OnDisconnected?.Invoke();
                    isInQueue = false;
                    isInGame = false;
                };

                websocket.OnMessage += (bytes) =>
                {
                    var message = System.Text.Encoding.UTF8.GetString(bytes);
                    HandleMessage(message);
                };

                await websocket.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to connect: " + e.Message);
                OnError?.Invoke(e.Message);
            }
        }

        public async void Disconnect()
        {
            if (websocket != null)
            {
                await websocket.Close();
                websocket = null;
            }
        }

        public void FindMatch(string roomType = "casual", int playerCount = 4)
        {
            if (isInGame)
            {
                Debug.LogWarning("Already in a game");
                return;
            }

            if (isInQueue)
            {
                Debug.LogWarning("Already in queue");
                return;
            }

            var requestPayload = new JoinQueueRequest
                { playerName = this.playerName, roomType = roomType, playerCount = playerCount };
            var message = new ClientMessage<JoinQueueRequest> { type = "join_queue", payload = requestPayload };
            SendMessage(message);
            isInQueue = true;
            Debug.Log($"Finding match... ({roomType}, {playerCount} players)");
        }

        public void LeaveQueue()
        {
            if (!isInQueue)
            {
                Debug.LogWarning("Not in queue");
                return;
            }

            var message = new ClientMessage<EmptyPayload> { type = "leave_queue", payload = new EmptyPayload() };
            SendMessage(message);
            isInQueue = false;
        }

        public void RollDice(int forcedValue = 0)
        {
            if (!isInGame)
            {
                Debug.LogWarning("Not in a game");
                return;
            }

            var requestPayload = new RollDiceRequest { forcedValue = forcedValue };
            var message = new ClientMessage<RollDiceRequest> { type = "roll_dice", payload = requestPayload };
            SendMessage(message);
        }

        public void MoveToken(int tokenIndex)
        {
            if (!isInGame)
            {
                Debug.LogWarning("Not in a game");
                return;
            }

            var requestPayload = new MoveTokenRequest { tokenIndex = tokenIndex };
            var message = new ClientMessage<MoveTokenRequest> { type = "move_token", payload = requestPayload };
            SendMessage(message);
        }

        public void GetGameState()
        {
            if (!isInGame)
            {
                Debug.LogWarning("Not in a game");
                return;
            }

            var message = new ClientMessage<EmptyPayload> { type = "get_state", payload = new EmptyPayload() };
            SendMessage(message);
        }

        public void LeaveGame()
        {
            if (!isInGame)
            {
                Debug.LogWarning("Not in a game");
                return;
            }

            var message = new ClientMessage<EmptyPayload> { type = "leave_game", payload = new EmptyPayload() };
            SendMessage(message);
            isInGame = false;
        }

        // ==================== GETTERS ====================

        public string GetPlayerId() => myPlayerId;
        public string GetSessionId() => currentSessionId;
        public int GetPlayerIndex() => myPlayerIndex;
        public GameStateData GetCurrentGameState() => currentGameState;
        public bool IsConnected() => websocket != null && websocket.State == WebSocketState.Open;
        public bool IsInQueue() => isInQueue;
        public bool IsInGame() => isInGame;
        public bool IsMyTurn() => currentGameState != null && currentGameState.currentPlayer == myPlayerIndex;

        // ==================== MESSAGE HANDLING ====================

        private void HandleMessage(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<MessageWrapper>(json);
                string payloadJson = ExtractPayload(json);

                switch (wrapper.type)
                {
                    case "connected": HandleConnected(payloadJson); break;
                    case "queue_joined": HandleQueueJoined(payloadJson); break;
                    case "left_queue": HandleLeftQueue(payloadJson); break;
                    case "player_left": HandlePlayerLeft(payloadJson); break;
                    case "match_found": HandleMatchFound(payloadJson); break;
                    case "dice_rolled": HandleDiceRolled(payloadJson); break;
                    case "token_moved": HandleTokenMoved(payloadJson); break;
                    case "game_state": HandleGameState(payloadJson); break;
                    case "game_over": HandleGameOver(payloadJson); break;
                    case "player_disconnected": HandlePlayerDisconnected(payloadJson); break;
                    case "player_reconnected": HandlePlayerReconnected(payloadJson); break;
                    case "error": HandleError(payloadJson); break;
                    default: Debug.LogWarning("Unknown message type: " + wrapper.type); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error handling message: " + e.Message + "\nJSON: " + json);
            }
        }

        private string ExtractPayload(string json)
        {
            int payloadIndex = json.IndexOf("\"payload\":");
            if (payloadIndex == -1) return "{}";
            int startIndex = json.IndexOf('{', payloadIndex);
            if (startIndex == -1) startIndex = json.IndexOf('[', payloadIndex);
            if (startIndex == -1) return "{}";

            char startChar = json[startIndex];
            char endChar = (startChar == '{') ? '}' : ']';
            int balance = 0;

            for (int i = startIndex; i < json.Length; i++)
            {
                if (json[i] == startChar) balance++;
                else if (json[i] == endChar) balance--;

                if (balance == 0)
                {
                    return json.Substring(startIndex, i - startIndex + 1);
                }
            }

            return "{}";
        }

        private void HandleConnected(string payloadJson)
        {
            var data = JsonUtility.FromJson<ConnectedPayload>(payloadJson);
            myPlayerId = data.playerId;
            Debug.Log($"Connected! Player ID: {myPlayerId}");
            OnConnected?.Invoke(myPlayerId);
        }

        private void HandleQueueJoined(string payloadJson)
        {
            var data = JsonUtility.FromJson<QueueJoinedPayload>(payloadJson);
            Debug.Log($"Joined queue. Players waiting: {data.playersInQueue}");
            OnQueueJoined?.Invoke(data.playersInQueue);
        }

        private void HandleLeftQueue(string payloadJson)
        {
            isInQueue = false;
            Debug.Log("Left matchmaking queue");
        }

        private void HandleMatchFound(string payloadJson)
        {
            var data = JsonUtility.FromJson<MatchFoundPayload>(payloadJson);

            isInQueue = false;
            isInGame = true;
            currentSessionId = data.sessionId;
            currentGameState = data.gameState;

            foreach (var player in data.players)
            {
                if (player.playerId == myPlayerId)
                {
                    myPlayerIndex = player.playerIndex;
                    break;
                }
            }

            Debug.Log($"Match found! {data.playerCount} players. You are player {myPlayerIndex}");

            OnMatchFound?.Invoke(new MatchData
            {
                sessionId = data.sessionId,
                playerCount = data.playerCount,
                myPlayerIndex = myPlayerIndex,
                players = data.players,
                gameState = data.gameState
            });
        }

        private void HandleDiceRolled(string payloadJson)
        {
            var data = JsonUtility.FromJson<DiceRollPayload>(payloadJson);
            Debug.Log($"Player {data.playerIndex} rolled {data.diceValue}");
            OnDiceRolled?.Invoke(new DiceRollData
            {
                playerIndex = data.playerIndex,
                diceValue = data.diceValue,
                validMoves = data.validMoves,
                noValidMoves = data.noValidMoves
            });
        }

        private void HandleTokenMoved(string payloadJson)
        {
            var data = JsonUtility.FromJson<TokenMovePayload>(payloadJson);
            currentGameState = data.gameState;
            Debug.Log($"Player {data.playerIndex} moved token {data.tokenIndex}: {data.moveResult}");
            OnTokenMoved?.Invoke(new TokenMoveData
            {
                playerIndex = data.playerIndex,
                tokenIndex = data.tokenIndex,
                moveResult = data.moveResult,
                newPosition = data.newPosition,
                hasWon = data.hasWon,
                gameState = data.gameState
            });
        }

        private void HandleGameState(string payloadJson)
        {
            var data = JsonUtility.FromJson<GameStatePayload>(payloadJson);
            currentGameState = data.gameState;
            Debug.Log($"Game state updated. Current player: {data.currentPlayer}");
        }

        private void HandleGameOver(string payloadJson)
        {
            var data = JsonUtility.FromJson<GameOverPayload>(payloadJson);
            isInGame = false;
            Debug.Log($"Game Over! Winner: {data.winnerName} (Player {data.winnerIndex})");
            OnGameOver?.Invoke(new GameOverData
            {
                winnerId = data.winnerId,
                winnerIndex = data.winnerIndex,
                winnerName = data.winnerName
            });
        }

        private void HandlePlayerDisconnected(string payloadJson)
        {
            var data = JsonUtility.FromJson<PlayerEventPayload>(payloadJson);
            Debug.Log($"Player disconnected: {data.playerId}");
            OnPlayerDisconnected?.Invoke(data.playerId);
        }

        private void HandlePlayerReconnected(string payloadJson)
        {
            var data = JsonUtility.FromJson<PlayerEventPayload>(payloadJson);
            Debug.Log($"Player reconnected: {data.playerId}");
            OnPlayerReconnected?.Invoke(data.playerId);
        }

        private void HandlePlayerLeft(string payloadJson)
        {
            var data = JsonUtility.FromJson<PlayerLeftPayload>(payloadJson);
            Debug.Log($"Player left: {data.playerName}");
            OnPlayerLeft?.Invoke(data.playerName);
        }

        private void HandleError(string payloadJson)
        {
            var data = JsonUtility.FromJson<ErrorPayload>(payloadJson);
            Debug.LogError($"Server error: {data.error}");
            OnError?.Invoke(data.error);
        }

        // ==================== UNITY LIFECYCLE ====================

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null)
            {
                websocket.DispatchMessageQueue();
            }
#endif
        }

        private void OnDestroy()
        {
            if (websocket != null)
            {
                websocket.Close();
            }
        }

        private async void OnApplicationQuit()
        {
            if (websocket != null)
            {
                await websocket.Close();
            }
        }

        // ==================== UTILITIES ====================

        private void SendMessage(object message)
        {
            if (websocket == null || websocket.State != WebSocketState.Open)
            {
                Debug.LogError("Not connected to server");
                return;
            }

            string json = JsonUtility.ToJson(message);
            websocket.SendText(json);
        }
    }

// ==================== DATA STRUCTURES (for receiving from server) ====================

    [Serializable]
    public class MessageWrapper
    {
        public string type;
    }

    [Serializable]
    public class ConnectedPayload
    {
        public string playerId;
        public string message;
    }

    [Serializable]
    public class QueueJoinedPayload
    {
        public int playersInQueue;
        public string message;
    }

    [Serializable]
    public class MatchFoundPayload
    {
        public string sessionId;
        public int playerCount;
        public PlayerInfo[] players;
        public GameStateData gameState;
    }

    [Serializable]
    public class PlayerInfo
    {
        public string playerId;
        public string name;
        public int playerIndex;
    }

    [Serializable]
    public class GameStateData
    {
        public int turnCount;
        public int diceValue;
        public int consecutiveSixes;
        public int currentPlayer;
        public int playerCount;
        public int[] tokenPositions;
    }

    [Serializable]
    public class DiceRollPayload
    {
        public string playerId;
        public int playerIndex;
        public int diceValue;
        public int[] validMoves;
        public bool noValidMoves;
    }

    [Serializable]
    public class TokenMovePayload
    {
        public string playerId;
        public int playerIndex;
        public int tokenIndex;
        public string moveResult;
        public int newPosition;
        public bool hasWon;
        public GameStateData gameState;
    }

    [Serializable]
    public class GameStatePayload
    {
        public string sessionId;
        public int playerIndex;
        public int currentPlayer;
        public GameStateData gameState;
    }

    [Serializable]
    public class GameOverPayload
    {
        public string winnerId;
        public int winnerIndex;
        public string winnerName;
    }

    [Serializable]
    public class PlayerEventPayload
    {
        public string playerId;
    }

    [Serializable]
    public class PlayerLeftPayload
    {
        public string playerId;
        public string playerName;
    }

    [Serializable]
    public class ErrorPayload
    {
        public string error;
    }

// ==================== REQUEST DATA STRUCTURES (for sending to server) ====================

    [Serializable]
    public class ClientMessage<T>
    {
        public string type;
        public T payload;
    }

    [Serializable]
    public class EmptyPayload
    {
    }

    [Serializable]
    public class JoinQueueRequest
    {
        public string playerName;
        public string roomType;
        public int playerCount;
    }

    [Serializable]
    public class RollDiceRequest
    {
        public int forcedValue;
    }

    [Serializable]
    public class MoveTokenRequest
    {
        public int tokenIndex;
    }

// ==================== EVENT DATA CLASSES (for Unity Events) ====================

    // Step 4: Make the data classes serializable so Unity can handle them
    [Serializable]
    public class MatchData
    {
        public string sessionId;
        public int playerCount;
        public int myPlayerIndex;
        public PlayerInfo[] players;
        public GameStateData gameState;
    }

    [Serializable]
    public class DiceRollData
    {
        public int playerIndex;
        public int diceValue;
        public int[] validMoves;
        public bool noValidMoves;
    }

    [Serializable]
    public class TokenMoveData
    {
        public int playerIndex;
        public int tokenIndex;
        public string moveResult;
        public int newPosition;
        public bool hasWon;
        public GameStateData gameState;
    }

    [Serializable]
    public class GameOverData
    {
        public string winnerId;
        public int winnerIndex;
        public string winnerName;
    }

    // Step 5: Create serializable UnityEvent subclasses for the custom data types
    [Serializable]
    public class MatchDataEvent : UnityEvent<MatchData>
    {
    }

    [Serializable]
    public class DiceRollDataEvent : UnityEvent<DiceRollData>
    {
    }

    [Serializable]
    public class TokenMoveDataEvent : UnityEvent<TokenMoveData>
    {
    }

    [Serializable]
    public class GameOverDataEvent : UnityEvent<GameOverData>
    {
    }
}