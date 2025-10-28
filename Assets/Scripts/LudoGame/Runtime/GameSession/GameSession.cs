using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

namespace Ludo
{
    [CreateAssetMenu(fileName = "NewGameSession", menuName = "Ludo/Game Session")]
    public class GameSession : ScriptableObject
    {
        [Header("Game Configuration")]
        [Tooltip("Type of the game (e.g., Classic, Custom).")]
        public string gameType;

        [Tooltip("Reference to the Ludo board.")]
        public LudoBoard board;

        [Header("Player State")]
        [Tooltip("Index of the current player (0-3).")]
        [Range(0, 3)]
        public byte currentPlayerIndex;

        [Tooltip("Value of the last dice roll (1-6).")]
        [Range(1, 6)]
        public byte diceValue;

        [Tooltip("Number of consecutive sixes rolled.")]
        public byte consecutiveSixCount;

        [Tooltip("If true, all players can interact with the board this turn.")]
        public bool othersCanInteractWithBoard;

        [Tooltip("Index of the main player (0-3).")]
        [Range(0, 3)]
        public byte mainPlayerIndex;

        [Header("Token State")]
        [Tooltip("List of tokens that can be moved this turn.")]
        public List<byte> currentMoveableTokens;

        [Tooltip("Index of the token to move (0-3).")]
        [Range(0, 3)]
        public byte tokenToMove;

        [Tooltip("New position of the token after movement (0-56).")]
        [Range(0, 56)]
        public byte tokenNewPosition;

        [Tooltip("Index of the token sent back to base (0-3).")]
        [Range(0, 3)]
        public byte tokenSentToBase;

        [Button]
        [Tooltip("Re-initialize the board with the current player count.")]
        public void ReSetup()
        {
            board = new LudoBoard(board.PlayerCount);
        }

        [Button]
        [Tooltip("Simulate a dice roll (for testing).")]
        public byte RollDice()
        {
            byte value = (byte) Random.Range(1, 7); // Replace with Random.Range(1, 7) in production.
            diceValue = value;
            if (diceValue == 6) consecutiveSixCount++;
            return value;
        }

        [Button]
        [Tooltip("Check if consecutive sixes are less than 3.")]
        public bool ConsecutiveSixLessThanThree() => consecutiveSixCount < 3;

        [Button]
        [Tooltip("Check if the given player is the main player.")]
        public bool IsMainPlayer(byte playerIndex) => mainPlayerIndex == playerIndex;

        [Button]
        [Tooltip("End the current turn and reset consecutive sixes.")]
        public void EndTurn() => consecutiveSixCount = 0;

        [Button]
        [Tooltip("Check if the given player can interact this turn.")]
        public bool HavePermissionToInteractThisTurn(byte playerIndex)
        {
            return othersCanInteractWithBoard || IsMainPlayer(playerIndex);
        }
    }
}
