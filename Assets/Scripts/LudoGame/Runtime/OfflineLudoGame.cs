using UnityEngine;

namespace LudoGame.Runtime
{
    [CreateAssetMenu(fileName = "Offline.Ludo.asset", menuName = "LudoGame/Offline", order = 0)]
    public class OfflineLudoGame : ScriptableObject
    {
        public LudoGameState GameState;
        public int currentPlayer = 0;
        public int playerCount = 4;
        public MoveResult moveResult;
        public int tokenIndex = 0;

        private void OnEnable()
        {
            LudoGameState.TryCreate(4, out GameState, out var _);
        }

        public void Reset()
        {
            LudoGameState.TryCreate(4, out GameState, out var _);
        }

        public void OnDiceRoll(byte diceValue)
        {
            GameState.CurrentPlayer = currentPlayer;
            if (LudoBoard.TryProcessMove(ref GameState, tokenIndex, diceValue, out moveResult))
            {
                LudoBoard.NextTurn(ref GameState, moveResult);
            }
        }

        [ContextMenu("Log")]
        public void Log()
        {
            Debug.Log(GameState.ToString());
        }
    }
}