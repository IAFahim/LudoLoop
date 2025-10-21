using System;
using System.Linq;
using LudoGame.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Placements.Runtime
{
    public class PlayerSpawner : MonoBehaviour
    {
        [FormerlySerializedAs("pawnBasePrefab")] public TokenBase tokenBasePrefab;
        public Vector3[] pawnBasePositions;
        public TokenBase[] pawnBases;

        private void OnValidate()
        {
            // Find all PawnBase objects in the scene, including inactive ones
            var allPawnBases = FindObjectsByType<TokenBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Sort by name
            var sortedPawnBases = allPawnBases.OrderBy(pb => pb.name).ToArray();

            // Assign positions from sorted PawnBases to pawnBasePositions
            pawnBasePositions = new Vector3[sortedPawnBases.Length];
            for (int i = 0; i < sortedPawnBases.Length; i++)
            {
                pawnBasePositions[i] = sortedPawnBases[i].transform.position;
            }
        }

        public void CreateBase(LudoGameState ludoGameState)
        {
            // Ensure pawnBases array is initialized
            var playerCount = ludoGameState.PlayerCount;
            pawnBases = new TokenBase[playerCount];

            // Instantiate PawnBase prefabs at the sorted positions
            for (int i = 0; i < playerCount; i++)
            {
                var pawnBase = Instantiate(tokenBasePrefab, pawnBasePositions[i], Quaternion.identity);
                pawnBase.Place(i);
                pawnBases[i] = pawnBase;
            }
        }

        public void MovePawn()
        {
            
        }
    }
}