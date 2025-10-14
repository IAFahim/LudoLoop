using UnityEngine;
using UnityEngine.Events;
using System;

namespace LudoGame.Runtime
{
    /// <summary>
    /// Handles game flow, turn management, and provides detailed feedback through events.
    /// </summary>
    public class OfflineLudoGame : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Game Configuration")]
        [SerializeField] private int playerCount = 4;
        [SerializeField] private bool autoAdvanceTurnOnNoMoves = true;
        
        [Header("Debug Info (Read-Only)")]
        [SerializeField] private GamePhase currentPhase = GamePhase.NotStarted;
        [SerializeField] private int currentPlayerIndex;
        [SerializeField] private byte lastDiceRoll;
        [SerializeField] private int consecutiveSixes;
        [SerializeField] private int[] validTokenIndices = Array.Empty<int>();
        [SerializeField] private bool isWaitingForTokenSelection;
        #endregion

        #region Events
        [Header("Game Events")]
        public UnityEvent<GameCreatedEventData> onGameCreated;
        public UnityEvent<TurnStartEventData> onTurnStart;
        public UnityEvent<DiceRolledEventData> onDiceRolled;
        public UnityEvent<TokenMoveEventData> onTokenMoved;
        public UnityEvent<MoveFailedEventData> onMoveFailed;
        public UnityEvent<TurnEndEventData> onTurnEnd;
        public UnityEvent<PlayerWonEventData> onPlayerWon;
        public UnityEvent<GameStateChangedEventData> onGameStateChanged;
        #endregion

        #region Private State
        private LudoGameState gameState;
        #endregion

        #region Properties
        public LudoGameState GameState => gameState;
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentPlayer => currentPlayerIndex;
        public int LastDiceRoll => lastDiceRoll;
        public bool IsWaitingForInput => isWaitingForTokenSelection;
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
            lastDiceRoll = 0;
            consecutiveSixes = 0;
            validTokenIndices = Array.Empty<int>();
            isWaitingForTokenSelection = false;
            currentPhase = GamePhase.Playing;

            // Notify listeners
            var eventData = new GameCreatedEventData
            {
                PlayerCount = numPlayers,
                StartingPlayer = currentPlayerIndex,
                GameState = gameState
            };
            
            onGameCreated?.Invoke(eventData);
            NotifyTurnStart();
            
            Debug.Log($"Game created with {numPlayers} players. Player {currentPlayerIndex} starts.");
            return true;
        }

        public bool diceProcssed;

        /// <summary>
        /// Processes a dice roll (useful for deterministic testing or network games).
        /// </summary>
        
        public void ProcessDiceRoll(byte diceValue)
        {
            diceProcssed = true;
            if (!ValidateGameState("ProcessDiceRoll")) diceProcssed = false;
            
            if (diceValue < 1 || diceValue > 6)
            {
                Debug.LogError($"Invalid dice value: {diceValue}. Must be between 1 and 6.");
                diceProcssed = false;
            }

            if (isWaitingForTokenSelection)
            {
                Debug.LogWarning("Cannot roll dice while waiting for token selection.");
                diceProcssed = false;
            }

            lastDiceRoll = diceValue;
            
            // Get valid moves for this dice roll
            validTokenIndices = LudoBoard.GetValidMoves(gameState, diceValue);
            
            var diceEventData = new DiceRolledEventData
            {
                PlayerIndex = currentPlayerIndex,
                DiceValue = diceValue,
                ValidTokenCount = validTokenIndices.Length,
                ValidTokenIndices = validTokenIndices,
                ConsecutiveSixes = gameState.ConsecutiveSixes
            };
            
            onDiceRolled?.Invoke(diceEventData);

            // Handle no valid moves
            if (validTokenIndices.Length == 0)
            {
                Debug.Log($"Player {currentPlayerIndex} rolled {diceValue} but has no valid moves.");
                
                var failEventData = new MoveFailedEventData
                {
                    PlayerIndex = currentPlayerIndex,
                    DiceValue = diceValue,
                    Result = MoveResult.InvalidNoValidMoves,
                    Message = MoveResult.InvalidNoValidMoves.ToMessage()
                };
                
                onMoveFailed?.Invoke(failEventData);

                if (autoAdvanceTurnOnNoMoves)
                {
                    AdvanceTurn(MoveResult.InvalidNoValidMoves);
                }
                
                diceProcssed = false;
            }

            // If only one valid move, could auto-select (optional)
            if (validTokenIndices.Length == 1)
            {
                Debug.Log($"Only one valid move available. Token {validTokenIndices[0]}");
            }

            isWaitingForTokenSelection = true;
            diceProcssed = false;
        }

        /// <summary>
        /// Moves the specified token using the last dice roll.
        /// </summary>
        public bool MoveToken(int tokenIndex)
        {
            if (!ValidateGameState("MoveToken")) return false;
            
            if (!isWaitingForTokenSelection)
            {
                Debug.LogWarning("No dice roll active. Roll the dice first.");
                return false;
            }

            if (lastDiceRoll == 0)
            {
                Debug.LogError("No valid dice roll to process.");
                return false;
            }

            // Validate token belongs to current player
            int tokenOwner = tokenIndex / 4;
            if (tokenOwner != currentPlayerIndex)
            {
                Debug.LogError($"Token {tokenIndex} does not belong to player {currentPlayerIndex}.");
                
                var failEventData = new MoveFailedEventData
                {
                    PlayerIndex = currentPlayerIndex,
                    TokenIndex = tokenIndex,
                    DiceValue = lastDiceRoll,
                    Result = MoveResult.InvalidNotYourToken,
                    Message = MoveResult.InvalidNotYourToken.ToMessage()
                };
                
                onMoveFailed?.Invoke(failEventData);
                return false;
            }

            // Process the move
            bool success = LudoBoard.TryProcessMove(ref gameState, tokenIndex, lastDiceRoll, out MoveResult result);
            
            isWaitingForTokenSelection = false;
            validTokenIndices = Array.Empty<int>();

            if (!success)
            {
                Debug.LogWarning($"Move failed: {result.ToMessage()}");
                
                var failEventData = new MoveFailedEventData
                {
                    PlayerIndex = currentPlayerIndex,
                    TokenIndex = tokenIndex,
                    DiceValue = lastDiceRoll,
                    Result = result,
                    Message = result.ToMessage()
                };
                
                onMoveFailed?.Invoke(failEventData);
                return false;
            }

            // Move succeeded
            Debug.Log($"Player {currentPlayerIndex} moved token {tokenIndex} with dice {lastDiceRoll}. Result: {result}");
            
            var moveEventData = new TokenMoveEventData
            {
                PlayerIndex = currentPlayerIndex,
                TokenIndex = tokenIndex,
                DiceValue = lastDiceRoll,
                Result = result,
                NewPosition = gameState.TokenPositions[tokenIndex],
                Message = result.ToMessage()
            };
            
            onTokenMoved?.Invoke(moveEventData);

            // Check for win condition
            if (LudoBoard.HasPlayerWon(gameState, currentPlayerIndex))
            {
                HandlePlayerWin(currentPlayerIndex);
                return true;
            }

            // Advance turn
            AdvanceTurn(result);
            
            return true;
        }

        /// <summary>
        /// Automatically selects and moves the first valid token (for AI or quick play).
        /// </summary>
        public bool AutoMove()
        {
            if (!ValidateGameState("AutoMove")) return false;
            
            if (!isWaitingForTokenSelection || validTokenIndices.Length == 0)
            {
                Debug.LogWarning("No valid moves available for auto-move.");
                return false;
            }

            // Select first valid token
            int selectedToken = validTokenIndices[0];
            return MoveToken(selectedToken);
        }

        /// <summary>
        /// Combined roll and auto-move for AI players.
        /// </summary>
        public bool RollAndAutoMove(byte dice)
        {
            if (dice <= 0) return false;
            
            if (!isWaitingForTokenSelection)
            {
                // No valid moves, turn already advanced
                return true;
            }

            return AutoMove();
        }
        #endregion

        #region Public API - Game State Queries
        /// <summary>
        /// Gets all valid token indices for the current player with a specific dice value.
        /// </summary>
        public int[] GetValidMovesForDice(byte diceValue)
        {
            if (!ValidateGameState("GetValidMovesForDice", false)) 
                return Array.Empty<int>();
            
            return LudoBoard.GetValidMoves(gameState, diceValue);
        }

        /// <summary>
        /// Checks if a specific player has won.
        /// </summary>
        public bool HasPlayerWon(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerCount) return false;
            return LudoBoard.HasPlayerWon(gameState, playerIndex);
        }

        /// <summary>
        /// Gets the position of a specific token.
        /// </summary>
        public sbyte GetTokenPosition(int tokenIndex)
        {
            if (tokenIndex < 0 || tokenIndex >= 16) return LudoBoard.PosBase;
            return gameState.TokenPositions[tokenIndex];
        }

        /// <summary>
        /// Gets all token positions for a specific player.
        /// </summary>
        public sbyte[] GetPlayerTokenPositions(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerCount) 
                return Array.Empty<sbyte>();
            
            int startIdx = playerIndex * 4;
            sbyte[] positions = new sbyte[4];
            Array.Copy(gameState.TokenPositions, startIdx, positions, 0, 4);
            return positions;
        }
        #endregion

        #region Public API - Serialization
        /// <summary>
        /// Serializes the current game state to a compact base64 string.
        /// </summary>
        public string SerializeGameState()
        {
            return gameState.Serialize();
        }

        /// <summary>
        /// Loads a game state from a serialized string.
        /// </summary>
        public bool LoadGameState(string serializedData)
        {
            try
            {
                gameState = LudoGameState.Deserialize(serializedData);
                currentPlayerIndex = gameState.CurrentPlayer;
                consecutiveSixes = gameState.ConsecutiveSixes;
                currentPhase = GamePhase.Playing;
                lastDiceRoll = 0;
                isWaitingForTokenSelection = false;
                validTokenIndices = Array.Empty<int>();
                
                var eventData = new GameStateChangedEventData
                {
                    GameState = gameState,
                    ChangeReason = "Game state loaded from save"
                };
                
                onGameStateChanged?.Invoke(eventData);
                NotifyTurnStart();
                
                Debug.Log("Game state loaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game state: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Private Helpers
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

        private void AdvanceTurn(MoveResult moveResult)
        {
            int previousPlayer = currentPlayerIndex;
            bool turnSwitched = LudoBoard.TryNextTurn(ref gameState, moveResult);
            
            currentPlayerIndex = gameState.CurrentPlayer;
            consecutiveSixes = gameState.ConsecutiveSixes;

            var turnEndData = new TurnEndEventData
            {
                PreviousPlayer = previousPlayer,
                NextPlayer = currentPlayerIndex,
                TurnSwitched = turnSwitched,
                MoveResult = moveResult,
                ConsecutiveSixes = consecutiveSixes
            };
            
            onTurnEnd?.Invoke(turnEndData);

            if (turnSwitched)
            {
                Debug.Log($"Turn advanced: Player {previousPlayer} -> Player {currentPlayerIndex}");
                NotifyTurnStart();
            }
            else
            {
                Debug.Log($"Player {currentPlayerIndex} rolls again!");
                NotifyTurnStart();
            }
        }

        private void NotifyTurnStart()
        {
            var turnStartData = new TurnStartEventData
            {
                PlayerIndex = currentPlayerIndex,
                ConsecutiveSixes = consecutiveSixes,
                PlayerTokenPositions = GetPlayerTokenPositions(currentPlayerIndex)
            };
            
            onTurnStart?.Invoke(turnStartData);
        }

        private void HandlePlayerWin(int winningPlayer)
        {
            currentPhase = GamePhase.Finished;
            
            var winEventData = new PlayerWonEventData
            {
                WinningPlayer = winningPlayer,
                FinalGameState = gameState
            };
            
            onPlayerWon?.Invoke(winEventData);
            
            Debug.Log($"🎉 Player {winningPlayer} has won the game!");
        }
        #endregion

        #region Debug Helpers
        /// <summary>
        /// Logs the current game state to the console.
        /// </summary>
        public void LogGameState()
        {
            Debug.Log(gameState.ToString());
            Debug.Log($"Phase: {currentPhase}, Waiting for input: {isWaitingForTokenSelection}");
        }

        /// <summary>
        /// Logs detailed information about the current turn.
        /// </summary>
        public void LogCurrentTurn()
        {
            Debug.Log($"=== Turn Info ===");
            Debug.Log($"Current Player: {currentPlayerIndex}");
            Debug.Log($"Last Dice Roll: {lastDiceRoll}");
            Debug.Log($"Consecutive Sixes: {consecutiveSixes}");
            Debug.Log($"Valid Tokens: {string.Join(", ", validTokenIndices)}");
            Debug.Log($"Waiting for Selection: {isWaitingForTokenSelection}");
        }
        #endregion
    }

    #region Event Data Classes
    [Serializable]
    public struct GameCreatedEventData
    {
        public int PlayerCount;
        public int StartingPlayer;
        public LudoGameState GameState;
    }

    [Serializable]
    public struct TurnStartEventData
    {
        public int PlayerIndex;
        public int ConsecutiveSixes;
        public sbyte[] PlayerTokenPositions;
    }

    [Serializable]
    public struct DiceRolledEventData
    {
        public int PlayerIndex;
        public byte DiceValue;
        public int ValidTokenCount;
        public int[] ValidTokenIndices;
        public int ConsecutiveSixes;
    }

    [Serializable]
    public struct TokenMoveEventData
    {
        public int PlayerIndex;
        public int TokenIndex;
        public byte DiceValue;
        public MoveResult Result;
        public sbyte NewPosition;
        public string Message;
    }

    [Serializable]
    public struct MoveFailedEventData
    {
        public int PlayerIndex;
        public int TokenIndex;
        public byte DiceValue;
        public MoveResult Result;
        public string Message;
    }

    [Serializable]
    public struct TurnEndEventData
    {
        public int PreviousPlayer;
        public int NextPlayer;
        public bool TurnSwitched;
        public MoveResult MoveResult;
        public int ConsecutiveSixes;
    }

    [Serializable]
    public struct PlayerWonEventData
    {
        public int WinningPlayer;
        public LudoGameState FinalGameState;
    }

    [Serializable]
    public struct GameStateChangedEventData
    {
        public LudoGameState GameState;
        public string ChangeReason;
    }
    #endregion

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