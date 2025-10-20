using System;
using LudoGame.Runtime;
using UnityEngine;

namespace Placements.Runtime
{
    public class LudoGamePlacement : MonoBehaviour
    { 
        public LudoGameState gameState;
        
        public bool isOnline;
        public int selfID;
        public Tiles tiles;

        public Action OnSelfPick;
        public sbyte boardPosition;
        public int playerIndex;

        
        [ContextMenu("Play")]
        public Vector3 GetTileBoardPosition()
        {
            var tileBoardPosition = tiles.GetTileBoardPosition(boardPosition, playerIndex);
            Debug.Log(tileBoardPosition);
            return tileBoardPosition;
        }


        private void OnDrawGizmosSelected()
        {
            var tileBoardPosition = GetTileBoardPosition();
            Gizmos.DrawSphere(tileBoardPosition, 1);
        }
    }
}