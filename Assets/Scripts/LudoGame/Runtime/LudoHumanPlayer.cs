using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using UnityEngine.Events;

namespace Ludo
{
    /// <summary>
    /// Human player controller - waits for UI/input to make decisions
    /// </summary>
    public class LudoHumanPlayer : LudoPlayerController
    {
        public override void OnTurn()
        {
        }

        public override void ChooseTokenFrom(List<byte> movableTokens, byte diceValue)
        {
        }

        protected override void OnGameEnd()
        {
        }


        [Button("Select Token 0")]
        public void SelectToken0(byte tokenNumberOfPlayer, byte dice) => ExecuteMove((byte)(playerIndex * LudoBoard.Tokens + tokenNumberOfPlayer), dice);
    }
}