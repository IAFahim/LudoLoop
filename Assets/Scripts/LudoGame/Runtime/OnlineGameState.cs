using UnityEngine;

namespace Ludo
{
    
    [CreateAssetMenu(fileName = "Online State", menuName = "Game/OnlineState", order = 1)]
    public class OnlineGameState : GameState
    {
        [Header("PlayerIndex")]
        public int localMainPlayerIndex;
        public string[] otherPlayerId;
    }
}