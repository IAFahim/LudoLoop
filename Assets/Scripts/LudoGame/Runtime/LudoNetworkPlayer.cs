using System.Collections.Generic;
using UnityEngine;

namespace Ludo
{
    /// <summary>
    /// Controller for network players (other players in online match)
    /// This player's moves are handled by the server
    /// </summary>
    public class LudoNetworkPlayer : LudoPlayerController
    {
        public override void OnTurn()
        {
            
        }

        public override void ChooseTokenFrom(List<byte> movableTokens, byte diceRoll)
        {
            // Network players don't make local decisions
            // Their moves come from the server via OnTokenMoved callback
            Debug.Log($"<color=blue>Network player {playerIndex} is choosing (waiting for server)...</color>");
        }

        public void OnTurnStart()
        {
            Debug.Log($"<color=blue>Network player {playerIndex}'s turn started</color>");
        }

        public void OnTurnEnd()
        {
            Debug.Log($"<color=blue>Network player {playerIndex}'s turn ended</color>");
        }
    }
}