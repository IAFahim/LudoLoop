using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

/// <summary>
/// Complete Unity client for Ludo Game Server with automatic matchmaking.
/// Drop this script into your Unity project and call FindMatch() to start playing.
/// </summary>
public class LudoClient : MonoBehaviour
{
    [Header("Server Configuration")]
    [SerializeField] private string serverUrl = "ws://localhost:8080";
    [SerializeField] private string playerName = "Player";

    [Header("Events")]
    public Action<string> OnConnected;
    public Action OnDisconnected;
    public Action<string> OnError;
    public Action<int> OnQueueJoined; // Players in queue
    public Action<MatchData> OnMatchFound;
    public Action<DiceRollData> OnDiceRolled;
    public Action<TokenMoveData> OnTokenMoved;
    public Action<GameOverData> OnGameOver;
    public Action<string> OnPlayerDisconnected;
    public Action<string> OnPlayerReconnected;

    private WebSocket websocket;
    private string myPlayerId;
    private string currentSessionId;
    private int myPlayerIndex = -1;
    private GameStateData currentGameState;
    private bool isInQueue = false;
    private bool isInGame = false;

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Connect to the game server
    /// </summary>
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

            websocket.OnOpen += () =>
            {
                Debug.Log("Connected to Ludo Game Server");
            };

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

    /// <summary>
    /// Disconnect from the server
    /// </summary>
    public async void Disconnect()
    {
        if (websocket != null)
        {
            await websocket.Close();
            websocket = null;
        }
    }

    /// <summary>
    /// Find a match - automatically joins queue and matches you with other players
    /// </summary>
    public void FindMatch()
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

        SendMessage(new
        {
            type = "find_match",
            payload = new { playerName = playerName }
        });

        isInQueue = true;
        Debug.Log("Finding match...");
    }

    /// <summary>
    /// Leave the matchmaking queue
    /// </summary>
    public void LeaveQueue()
    {
        if (!isInQueue)
        {
            Debug.LogWarning("Not in queue");
            return;
        }

        SendMessage(new
        {
            type = "leave_queue",
            payload = new { }
        });

        isInQueue = false;
    }

    /// <summary>
    /// Roll the dice (must be your turn)
    /// </summary>
    public void RollDice()
    {
        if (!isInGame)
        {
            Debug.LogWarning("Not in a game");
            return;
        }

        SendMessage(new
        {
            type = "roll_dice",
            payload = new { playerId = myPlayerId }
        });
    }

    /// <summary>
    /// Move a token (must have rolled dice first)
    /// </summary>
    /// <param name="tokenIndex">Token index (0-15, where 0-3 is player 0, 4-7 is player 1, etc.)</param>
    public void MoveToken(int tokenIndex)
    {
        if (!isInGame)
        {
            Debug.LogWarning("Not in a game");
            return;
        }

        SendMessage(new
        {
            type = "move_token",
            payload = new { playerId = myPlayerId, tokenIndex = tokenIndex }
        });
    }

    /// <summary>
    /// Request current game state
    /// </summary>
    public void GetGameState()
    {
        if (!isInGame)
        {
            Debug.LogWarning("Not in a game");
            return;
        }

        SendMessage(new
        {
            type = "get_state",
            payload = new { playerId = myPlayerId }
        });
    }

    /// <summary>
    /// Leave the current game
    /// </summary>
    public void LeaveGame()
    {
        if (!isInGame)
        {
            Debug.LogWarning("Not in a game");
            return;
        }

        SendMessage(new
        {
            type = "leave_game",
            payload = new { playerId = myPlayerId }
        });

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
            var message = JsonUtility.FromJson<ServerMessage>(json);
            
            switch (message.type)
            {
                case "connected":
                    HandleConnected(message.payload);
                    break;

                case "queue_joined":
                    HandleQueueJoined(message.payload);
                    break;

                case "left_queue":
                    HandleLeftQueue(message.payload);
                    break;

                case "match_found":
                    HandleMatchFound(message.payload);
                    break;

                case "dice_rolled":
                    HandleDiceRolled(message.payload);
                    break;

                case "token_moved":
                    HandleTokenMoved(message.payload);
                    break;

                case "game_state":
                    HandleGameState(message.payload);
                    break;

                case "game_over":
                    HandleGameOver(message.payload);
                    break;

                case "player_disconnected":
                    HandlePlayerDisconnected(message.payload);
                    break;

                case "player_reconnected":
                    HandlePlayerReconnected(message.payload);
                    break;

                case "error":
                    HandleError(message.payload);
                    break;

                default:
                    Debug.LogWarning("Unknown message type: " + message.type);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling message: " + e.Message + "\nJSON: " + json);
        }
    }

    private void HandleConnected(string payload)
    {
        var data = JsonUtility.FromJson<ConnectedPayload>(payload);
        myPlayerId = data.playerId;
        Debug.Log($"Connected! Player ID: {myPlayerId}");
        OnConnected?.Invoke(myPlayerId);
    }

    private void HandleQueueJoined(string payload)
    {
        var data = JsonUtility.FromJson<QueueJoinedPayload>(payload);
        Debug.Log($"Joined queue. Players waiting: {data.playersInQueue}");
        OnQueueJoined?.Invoke(data.playersInQueue);
    }

    private void HandleLeftQueue(string payload)
    {
        isInQueue = false;
        Debug.Log("Left matchmaking queue");
    }

    private void HandleMatchFound(string payload)
    {
        var data = JsonUtility.FromJson<MatchFoundPayload>(payload);
        
        isInQueue = false;
        isInGame = true;
        currentSessionId = data.sessionId;
        currentGameState = data.gameState;

        // Find our player index
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

    private void HandleDiceRolled(string payload)
    {
        var data = JsonUtility.FromJson<DiceRollPayload>(payload);
        
        Debug.Log($"Player {data.playerIndex} rolled {data.diceValue}");
        
        OnDiceRolled?.Invoke(new DiceRollData
        {
            playerIndex = data.playerIndex,
            diceValue = data.diceValue,
            validMoves = data.validMoves,
            noValidMoves = data.noValidMoves
        });
    }

    private void HandleTokenMoved(string payload)
    {
        var data = JsonUtility.FromJson<TokenMovePayload>(payload);
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

    private void HandleGameState(string payload)
    {
        var data = JsonUtility.FromJson<GameStatePayload>(payload);
        currentGameState = data.gameState;
        Debug.Log($"Game state updated. Current player: {data.currentPlayer}");
    }

    private void HandleGameOver(string payload)
    {
        var data = JsonUtility.FromJson<GameOverPayload>(payload);
        isInGame = false;
        
        Debug.Log($"Game Over! Winner: {data.winnerName} (Player {data.winnerIndex})");
        
        OnGameOver?.Invoke(new GameOverData
        {
            winnerId = data.winnerId,
            winnerIndex = data.winnerIndex,
            winnerName = data.winnerName
        });
    }

    private void HandlePlayerDisconnected(string payload)
    {
        var data = JsonUtility.FromJson<PlayerEventPayload>(payload);
        Debug.Log($"Player disconnected: {data.playerId}");
        OnPlayerDisconnected?.Invoke(data.playerId);
    }

    private void HandlePlayerReconnected(string payload)
    {
        var data = JsonUtility.FromJson<PlayerEventPayload>(payload);
        Debug.Log($"Player reconnected: {data.playerId}");
        OnPlayerReconnected?.Invoke(data.playerId);
    }

    private void HandleError(string payload)
    {
        var data = JsonUtility.FromJson<ErrorPayload>(payload);
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

// ==================== DATA STRUCTURES ====================

[Serializable]
public class ServerMessage
{
    public string type;
    public string payload;
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
public class ErrorPayload
{
    public string error;
}

// Event Data Classes
public class MatchData
{
    public string sessionId;
    public int playerCount;
    public int myPlayerIndex;
    public PlayerInfo[] players;
    public GameStateData gameState;
}

public class DiceRollData
{
    public int playerIndex;
    public int diceValue;
    public int[] validMoves;
    public bool noValidMoves;
}

public class TokenMoveData
{
    public int playerIndex;
    public int tokenIndex;
    public string moveResult;
    public int newPosition;
    public bool hasWon;
    public GameStateData gameState;
}

public class GameOverData
{
    public string winnerId;
    public int winnerIndex;
    public string winnerName;
}
