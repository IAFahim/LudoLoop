using System;
using System.Collections.Generic;

namespace Network.Runtime
{
    /// <summary>
    /// Message types for client-server communication
    /// </summary>
    public enum MessageType
    {
        // Client -> Server
        CreateGame,
        JoinGame,
        StartGame,
        RollDice,
        MoveToken,
        GetState,
        LeaveGame,
        Reconnect,
        ListGames,
        
        // Server -> Client
        Connected,
        GameCreated,
        GameJoined,
        PlayerJoined,
        GameStarted,
        DiceRolled,
        TokenMoved,
        GameState,
        GameOver,
        PlayerLeft,
        PlayerDisconnected,
        PlayerReconnected,
        Reconnected,
        LeftGame,
        GamesList,
        Error
    }

    [Serializable]
    public class NetworkMessage
    {
        public string type;
        public string payload; // JSON string of the payload
    }

    [Serializable]
    public class CreateGamePayload
    {
        public int maxPlayers = 4;
        public string playerName;
    }

    [Serializable]
    public class JoinGamePayload
    {
        public string sessionId;
        public string playerName;
    }

    [Serializable]
    public class StartGamePayload
    {
        public string playerId;
    }

    [Serializable]
    public class RollDicePayload
    {
        public string playerId;
        public int diceValue; // Optional: 0 for random
    }

    [Serializable]
    public class MoveTokenPayload
    {
        public string playerId;
        public int tokenIndex;
    }

    [Serializable]
    public class GetStatePayload
    {
        public string playerId;
    }

    [Serializable]
    public class LeaveGamePayload
    {
        public string playerId;
    }

    [Serializable]
    public class ReconnectPayload
    {
        public string playerId;
    }

    // Response payloads
    [Serializable]
    public class GameCreatedPayload
    {
        public string sessionId;
        public string playerId;
        public int playerIndex;
        public int maxPlayers;
        public GameStatePayload gameState;
    }

    [Serializable]
    public class GameJoinedPayload
    {
        public string sessionId;
        public string playerId;
        public int playerIndex;
        public GameStatePayload gameState;
    }

    [Serializable]
    public class PlayerJoinedPayload
    {
        public string playerId;
        public int playerIndex;
        public string playerName;
        public int playerCount;
    }

    [Serializable]
    public class GameStartedPayload
    {
        public int playerCount;
        public int currentPlayer;
        public LudoGameStateData gameState;
    }

    [Serializable]
    public class DiceRolledPayload
    {
        public string playerId;
        public int playerIndex;
        public int diceValue;
        public int[] validMoves;
        public bool noValidMoves;
        public int nextPlayer;
    }

    [Serializable]
    public class TokenMovedPayload
    {
        public string playerId;
        public int playerIndex;
        public int tokenIndex;
        public string moveResult;
        public string message;
        public int newPosition;
        public bool hasWon;
        public bool turnSwitched;
        public int nextPlayer;
        public LudoGameStateData gameState;
    }

    [Serializable]
    public class GameStatePayload
    {
        public string sessionId;
        public int playerIndex;
        public int playerCount;
        public int currentPlayer;
        public LudoGameStateData gameState;
        public PlayerInfo[] players;
        public bool isStarted;
        public string winnerId;
        public bool isWaitingForMove;
        public int currentDiceRoll;
    }

    [Serializable]
    public class GameOverPayload
    {
        public string winnerId;
        public int winnerIndex;
        public string winnerName;
    }

    [Serializable]
    public class ErrorPayload
    {
        public string error;
    }

    [Serializable]
    public class GamesListPayload
    {
        public GameInfo[] games;
    }

    [Serializable]
    public class GameInfo
    {
        public string sessionId;
        public int playerCount;
        public int maxPlayers;
        public bool isStarted;
        public long createdAt;
    }

    [Serializable]
    public class PlayerInfo
    {
        public string playerId;
        public int playerIndex;
        public string name;
        public bool connected;
    }

    [Serializable]
    public class LudoGameStateData
    {
        public int turnCount;
        public int diceValue;
        public int consecutiveSixes;
        public int currentPlayer;
        public int[] tokenPositions;
        public int playerCount;
    }

    [Serializable]
    public class ConnectedPayload
    {
        public string message;
    }
}
