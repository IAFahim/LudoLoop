using System.Linq;
using UnityEngine;

namespace Placements.Runtime
{
    public class PlayerSpawner : MonoBehaviour
    {
        public TokenBase tokenBasePrefab;
        public Vector3[] pawnBasePositions;
        public TokenBase[] pawnBases;

        private void OnValidate()
        {
            // Find all PawnBase objects in the scene, including inactive ones
            pawnBases = FindObjectsByType<TokenBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Sort by name
            var sortedPawnBases = pawnBases.OrderBy(pb => pb.name).ToArray();

            // Assign positions from sorted PawnBases to pawnBasePositions
            pawnBasePositions = new Vector3[sortedPawnBases.Length];
            for (int i = 0; i < sortedPawnBases.Length; i++)
            {
                pawnBasePositions[i] = sortedPawnBases[i].transform.position;
            }
        }

        public void CreateBaseForPlayerCount(int playerCount)
        {
            pawnBases = new TokenBase[playerCount];

            // Instantiate PawnBase prefabs at the sorted positions
            for (int i = 0; i < playerCount; i++)
            {
                var pawnBase = Instantiate(tokenBasePrefab, pawnBasePositions[i], Quaternion.identity);
                pawnBase.Place(i);
                pawnBases[i] = pawnBase;
            }
        }
        
    }
}