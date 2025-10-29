using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

namespace Ludo
{
    [CreateAssetMenu(fileName = "NewGameSession", menuName = "Ludo/Game Session")]
    public class GameSession : ScriptableObject
    {
        [Header("Game Configuration")] [Tooltip("Type of the game (e.g., Classic, Custom).")]
        public string gameType;

        [Tooltip("Reference to the Ludo board.")]
        public LudoBoard board;

        [Header("Player State")] [Tooltip("Index of the current player (0-3).")] [Range(0, 3)]
        public byte currentPlayerIndex;

        [Tooltip("Value of the last dice roll (1-6).")] [Range(1, 6)]
        public byte diceValue;

        [Tooltip("Number of consecutive sixes rolled.")]
        public byte consecutiveSixCount;

        [Tooltip("If true, all players can interact with the board this turn.")]
        public bool othersCanInteractWithBoard;

        [Tooltip("Index of the main player (0-3).")] [Range(0, 3)]
        public byte mainPlayerIndex;

        [Header("Token State")] [Tooltip("List of tokens that can be moved this turn.")]
        public List<byte> currentMoveableTokens;

        [Tooltip("Index of the token to move (0-3).")] [Range(0, 3)]
        public byte tokenToMove;

        [Tooltip("New position of the token after movement (0-56).")] [Range(0, 56)]
        public byte tokenNewPosition;

        [Tooltip("Index of the token sent back to base (0-3).")] [Range(0, 3)]
        public byte tokenSentToBase;

        [Button]
        public void ReSetup()
        {
            board = new LudoBoard(board.PlayerCount);
        }

        [Button]
        public byte RollDice()
        {
            byte value = (byte)Random.Range(1, 7);
            diceValue = value;
            if (diceValue == 6) consecutiveSixCount++;
            return value;
        }

        public bool ConsecutiveSixLessThanThree() => consecutiveSixCount < 3;

        public bool IsMainPlayer(byte playerIndex) => mainPlayerIndex == playerIndex;

        public void EndTurn()
        {
            ResetConsecutiveSix();
            currentPlayerIndex = (byte)((currentPlayerIndex + 1) % board.PlayerCount);
        }

        public void ResetConsecutiveSix()
        {
            consecutiveSixCount = 0;
            diceValue = 0;
        }

    public bool HavePermissionToInteractThisTurn(byte playerIndex)
        {
            return othersCanInteractWithBoard || IsMainPlayer(playerIndex);
        }

        public int OffsetPlayerIndex(int playerIndex)
        {
            if (board.PlayerCount == 2 && playerIndex == 1) return 2;
            return playerIndex;
        }
    }
}