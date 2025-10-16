using UnityEngine;
using LudoGame.Runtime; // Your namespace
using System.Collections.Generic;
using System.Linq;

public class LudoGameManager : MonoBehaviour
{
    // The current state of the game. Making it public allows the editor script to access it.
    [Header("Game State Data")]
    public LudoGameState gameState;

    // The current phase of the turn (e.g., waiting for roll, waiting for move).
    public GamePhase currentPhase { get; private set; }

    // Stores the last dice roll value for the current turn.
    public int lastDiceRoll { get; private set; }

    // A list of tokens that can be legally moved with the current dice roll.
    public List<int> validMoves { get; private set; }

    public enum GamePhase
    {
        PreGame,
        WaitingForRoll,
        WaitingForMove,
        GameOver
    }

    private void Awake()
    {
        currentPhase = GamePhase.PreGame;
        validMoves = new List<int>();
    }

    /// <summary>
    /// Starts a new game with the specified number of players.
    /// This is called by the Editor script.
    /// </summary>
    public void StartNewGame(int playerCount)
    {
        if (LudoGameState.TryCreate(playerCount, out gameState, out var result))
        {
            currentPhase = GamePhase.WaitingForRoll;
            lastDiceRoll = 0;
            validMoves.Clear();
            Debug.Log($"<color=green>New Ludo Game Started with {playerCount} players!</color>\n{gameState}");
        }
        else
        {
            Debug.LogError($"Failed to create game: {result}");
        }
    }

    /// <summary>
    /// Simulates a dice roll for the current player.
    /// Can accept a specific value for testing purposes.
    /// </summary>
    public void RollDice(int specificValue = 0)
    {
        if (currentPhase != GamePhase.WaitingForRoll)
        {
            Debug.LogWarning("Cannot roll dice. Not in the WaitingForRoll phase.");
            return;
        }

        // Use the specific value if provided (for testing), otherwise roll a random die.
        lastDiceRoll = (specificValue > 0 && specificValue <= 6) ? specificValue : Random.Range(1, 7);
        gameState.DiceValue = lastDiceRoll;

        Debug.Log($"Player {gameState.CurrentPlayer} rolled a <color=yellow>{lastDiceRoll}</color>");

        // After rolling, check for valid moves.
        validMoves = LudoBoard.GetValidMoves(gameState, lastDiceRoll).ToList();

        if (validMoves.Count > 0)
        {
            // If there are moves, wait for the player to select one.
            currentPhase = GamePhase.WaitingForMove;
            Debug.Log($"Valid moves for this roll: [{string.Join(", ", validMoves)}]");
        }
        else
        {
            // If there are no valid moves, the turn is automatically skipped.
            Debug.LogWarning("No valid moves for this roll. Skipping turn.");
            EndTurn(MoveResult.InvalidNoValidMoves); // Pass a non-successful result to switch turn
        }
    }

    /// <summary>
    /// Attempts to move a specific token for the current player.
    /// </summary>
    public void AttemptMove(int tokenIndex)
    {
        if (currentPhase != GamePhase.WaitingForMove)
        {
            Debug.LogWarning("Cannot move token. Not in the WaitingForMove phase.");
            return;
        }
        
        // Optional: Check if the chosen token is in the list of valid moves.
        if (!validMoves.Contains(tokenIndex))
        {
            Debug.LogError($"Invalid Move: Token {tokenIndex} cannot be moved with a roll of {lastDiceRoll}. " +
                           $"Choose from: [{string.Join(", ", validMoves)}]");
            return;
        }

        Debug.Log($"Attempting to move token {tokenIndex}...");

        // The core logic call!
        if (LudoBoard.TryProcessMove(ref gameState, tokenIndex, lastDiceRoll, out MoveResult result))
        {
            Debug.Log($"<color=cyan>Move Success!</color> Result: {result.ToMessage()}\n{gameState}");
            EndTurn(result);
        }
        else
        {
            Debug.LogError($"<color=red>Move Failed!</color> Result: {result.ToMessage()}");
        }
    }

    /// <summary>
    /// Finalizes the turn after a move has been made (or skipped).
    /// </summary>
    private void EndTurn(MoveResult moveResult)
    {
        // Check for a winner before switching turns
        if (LudoBoard.HasPlayerWon(gameState, gameState.CurrentPlayer))
        {
            currentPhase = GamePhase.GameOver;
            Debug.Log($"<color=magenta>GAME OVER! Player {gameState.CurrentPlayer} has won!</color>");
            return;
        }

        // Use the game logic to determine if the turn switches or if the player rolls again.
        bool turnSwitched = LudoBoard.TryNextTurn(ref gameState, in moveResult);

        if (turnSwitched)
        {
            Debug.Log($"Turn ended. Next player is <color=white>Player {gameState.CurrentPlayer}</color>");
        }
        else
        {
            Debug.Log($"Player {gameState.CurrentPlayer} gets to roll again!");
        }

        // Reset for the next action.
        currentPhase = GamePhase.WaitingForRoll;
        lastDiceRoll = 0;
        validMoves.Clear();
        gameState.StartNewTurn(ref gameState);
    }
}