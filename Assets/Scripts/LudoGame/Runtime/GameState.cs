using UnityEngine;

namespace Ludo
{
    public class GameState : ScriptableObject
    {
        public string gameType;
        public LudoBoard board;
        public int currentPlayerIndex;
        public byte diceValue;
    }
}