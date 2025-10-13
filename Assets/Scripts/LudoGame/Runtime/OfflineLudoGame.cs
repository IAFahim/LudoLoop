using UnityEngine;
using UnityEngine.Events;

namespace LudoGame.Runtime
{
    public class OfflineLudoGame : MonoBehaviour
    {
        public LudoGameState GameState;
        public int playerCount = 4;
        [SerializeField] private LudoCreateResult ludoCreateResult;
        [SerializeField] private MoveResult moveResult;
        [SerializeField] private int currentPlayer;
        [SerializeField] private int tokenIndex;
        [SerializeField] private byte dice;

        public UnityEvent<int> onGameCreate;
        public UnityEvent<MoveResult> onNextTurn;
        public UnityEvent<MoveResult> onNextTurnFail;
        public UnityEvent<MoveResult, int> onMove;
        public UnityEvent<MoveResult> onMoveFail;

        private void Start() => CreateNew(playerCount);

        private void CreateNew(int player)
        {
            playerCount = player;
            if (LudoGameState.TryCreate(playerCount, out GameState, out ludoCreateResult))
            {
                onGameCreate?.Invoke(GameState.CurrentPlayer);
                currentPlayer = GameState.CurrentPlayer;
            }
            else
            {
                Debug.LogError($"Failed to create game: {ludoCreateResult}");
            }
        }

        /// <summary>
        /// Handles dice roll and processes the move for the specified token.
        /// </summary>
        public void OnDiceRoll(byte diceValue)
        {
            dice = diceValue;
            // Sync current player with game state
            GameState.CurrentPlayer = currentPlayer;
            
            // Try to process the move
            if (!LudoBoard.TryProcessMove(ref GameState, tokenIndex, diceValue, out moveResult))
            {
                // Move failed - invoke failure event
                onMoveFail?.Invoke(moveResult);
                
                // If no valid moves are possible, advance turn
                if (moveResult == MoveResult.InvalidNoValidMoves)
                {
                    bool turnSwitched = LudoBoard.TryNextTurn(ref GameState, moveResult);
                    currentPlayer = GameState.CurrentPlayer;
                    
                    if (turnSwitched)
                    {
                        onNextTurn?.Invoke(moveResult);
                    }
                }
                return;
            }

            // Move succeeded - invoke success event
            onMove?.Invoke(moveResult, tokenIndex);
            
            // Process turn transition
            bool switchedToNextPlayer = LudoBoard.TryNextTurn(ref GameState, moveResult);
            currentPlayer = GameState.CurrentPlayer;
            
            if (switchedToNextPlayer)
            {
                // Turn switched to next player
                onNextTurn?.Invoke(moveResult);
            }
            else
            {
                // Current player rolls again
                onNextTurn?.Invoke(moveResult);
            }
        }

        /// <summary>
        /// Handles dice roll with automatic valid token selection.
        /// Useful for AI or auto-play scenarios.
        /// </summary>
        public void OnDiceRollAuto(byte diceValue)
        {
            GameState.CurrentPlayer = currentPlayer;
            
            // Get all valid moves for this dice roll
            int[] validMoves = LudoBoard.GetValidMoves(GameState, diceValue);
            
            if (validMoves.Length == 0)
            {
                // No valid moves - invoke failure and advance turn
                moveResult = MoveResult.InvalidNoValidMoves;
                onMoveFail?.Invoke(moveResult);
                
                bool turnSwitched = LudoBoard.TryNextTurn(ref GameState, moveResult);
                currentPlayer = GameState.CurrentPlayer;
                
                if (turnSwitched)
                {
                    onNextTurn?.Invoke(moveResult);
                }
                return;
            }
            
            // Select the first valid token (or implement custom selection logic)
            tokenIndex = validMoves[0];
            
            // Process the move
            if (!LudoBoard.TryProcessMove(ref GameState, tokenIndex, diceValue, out moveResult))
            {
                onMoveFail?.Invoke(moveResult);
                return;
            }
            
            // Move succeeded
            onMove?.Invoke(moveResult, tokenIndex);
            
            // Process turn transition
            bool switchedToNextPlayer = LudoBoard.TryNextTurn(ref GameState, moveResult);
            currentPlayer = GameState.CurrentPlayer;
            
            onNextTurn?.Invoke(moveResult);
        }

        /// <summary>
        /// Gets all valid token indices for the current player with the given dice roll.
        /// </summary>
        public int[] GetValidMoves(byte diceValue)
        {
            GameState.CurrentPlayer = currentPlayer;
            return LudoBoard.GetValidMoves(GameState, diceValue);
        }

        /// <summary>
        /// Checks if the specified player has won the game.
        /// </summary>
        public bool HasPlayerWon(int playerIndex)
        {
            return LudoBoard.HasPlayerWon(GameState, playerIndex);
        }

        /// <summary>
        /// Checks if the current player has won.
        /// </summary>
        public bool HasCurrentPlayerWon()
        {
            return LudoBoard.HasPlayerWon(GameState, currentPlayer);
        }

        /// <summary>
        /// Serializes the current game state to a base64 string.
        /// </summary>
        public string SerializeGameState()
        {
            return GameState.Serialize();
        }

        /// <summary>
        /// Loads game state from a serialized base64 string.
        /// </summary>
        public void LoadGameState(string serializedData)
        {
            GameState = LudoGameState.Deserialize(serializedData);
            currentPlayer = GameState.CurrentPlayer;
        }

        public void Log()
        {
            Debug.Log(GameState.ToString());
        }
    }
}