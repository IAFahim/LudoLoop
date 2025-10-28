using EasyButtons;
using UnityEngine;

namespace Ludo
{
    public class LudoAIPlayer : MonoBehaviour
    {
        public LudoGamePlay ludoGamePlay;

        private void OnValidate()
        {
            ludoGamePlay = GetComponent<LudoGamePlay>();
        }

        [Button]
        public void Play()
        {
            var gameSession = ludoGamePlay.gameSession;
            var consecutiveSixLessThenThree = gameSession.ConsecutiveSixLessThanThree();
            if (!consecutiveSixLessThenThree)
            {
                PassPlayer();
                return;
            }
            var dice = gameSession.RollDice();
            Debug.Log(dice);
            var movableTokens = ludoGamePlay.GetMovableTokens(gameSession.currentPlayerIndex, dice);
            if (movableTokens.Count == 0)
            {
                PassPlayer();
            }
            else
            {
                gameSession.currentMoveableTokens = movableTokens;
                if (movableTokens.Count == 1)
                {
                    var tokenToMove = movableTokens[0];
                    Perform(gameSession, tokenToMove, dice);
                }
                else
                {
                    DoRandom();
                }
            }
        }

        private void Perform(GameSession gameSession, byte tokenToMove, byte dice)
        {
            gameSession.tokenToMove = tokenToMove;
            ludoGamePlay.MoveToken(tokenToMove, dice, out var tokenSentToBase);
            gameSession.tokenSentToBase = tokenSentToBase;
        }

        private void PassPlayer()
        {
            var gameSession = ludoGamePlay.gameSession;
            gameSession.EndTurn();
            gameSession.currentPlayerIndex =
                (byte)((gameSession.currentPlayerIndex + 1) % gameSession.board.PlayerCount);
        }

        public void Selected()
        {
        }

        public void DoRandom()
        {
            var gameSession = ludoGamePlay.gameSession;
            var movableTokens = gameSession.currentMoveableTokens;

            if (movableTokens == null || movableTokens.Count == 0) return;

            int randomIndex = Random.Range(0, movableTokens.Count);
            byte tokenToMove = movableTokens[randomIndex];
            Perform(gameSession, tokenToMove, gameSession.diceValue);
        }
    }
}