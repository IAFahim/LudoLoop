using EasyButtons;
using UnityEngine;

namespace Ludo
{
    public class GameSession : ScriptableObject
    {
        public string gameType;
        public LudoBoard board;
        public int currentPlayerIndex;
        public byte diceValue;
        

        [ContextMenu("ReSetup")]
        [Button]
        public void ReSetup()
        {
            board = new LudoBoard(board.PlayerCount);
        }
    }
}