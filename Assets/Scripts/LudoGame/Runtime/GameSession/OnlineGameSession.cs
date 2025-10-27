using UnityEngine;

namespace Ludo
{
    
    [CreateAssetMenu(fileName = "Online Session", menuName = "Game/Online Session", order = 1)]
    public class OnlineGameSession : GameSession
    {
        [Header("PlayerIndex")]
        public int localMainPlayerIndex;
        public string[] otherPlayerId;
    }
}