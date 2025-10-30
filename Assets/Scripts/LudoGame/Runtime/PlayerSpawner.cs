using Ludo;
using UnityEngine;

namespace Placements.Runtime
{
    public class PlayerSpawner : MonoBehaviour
    {
        public TokenBase[] tokenBases;
        
        public void CreateBaseForPlayerCount(GameSession session, int playerCount, PlayerType[] playerTypes)
        {
            // Instantiate PawnBase prefabs at the sorted positions
            if (playerCount == 2)
            {
                tokenBases[1].gameObject.SetActive(false);
                tokenBases[3].gameObject.SetActive(false);
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                var pawnBase = tokenBases[i];
                if (i >= playerCount)
                {
                    pawnBase.gameObject.SetActive(false);
                }
            }
            
        }
    }
}