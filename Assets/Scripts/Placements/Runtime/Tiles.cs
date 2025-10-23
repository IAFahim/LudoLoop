using System.Collections.Generic;
using UnityEngine;

namespace Placements.Runtime
{
    public class Tiles : MonoBehaviour
    {
        public Vector3[] tiles;
        public float heightOffset;
        public ColorTiles[] groupedTiles;

        private void OnValidate()
        {
            var totalTiles = new List<Vector3>();

            foreach (var tile in groupedTiles[0].FirstTiles)
            {
                totalTiles.Add(tile.transform.position);
            }

            foreach (var colorTiles in groupedTiles[1..4])
            {
                foreach (var tile in colorTiles.EndTiles)
                {
                    totalTiles.Add(tile.transform.position);
                }

                foreach (var tile in colorTiles.FirstTiles)
                {
                    totalTiles.Add(tile.transform.position);
                }
            }

            foreach (var tile in groupedTiles[0].EndTiles)
            {
                totalTiles.Add(tile.transform.position);
            }

            for (var index = 0; index < totalTiles.Count; index++)
            {
                var tile = totalTiles[index];
                tile.y += heightOffset;
                totalTiles[index] = tile;
            }

            tiles = totalTiles.ToArray();
        }
        
        /// <summary>
        /// This now correctly uses the pre-built arrays from your Tiles/ColorTiles components.
        /// </summary>
        public Vector3 GetTileBoardPosition(sbyte boardPos, int playerIndex)
        {
            // Case 1: Token is on the HOME STRETCH (encoded as 100+)
            if (boardPos >= 100)
            {
                int homeStretchStep = (boardPos - 100) % 6;
                if (playerIndex < groupedTiles.Length)
                {
                    var finalTiles = groupedTiles[playerIndex].tileFinal;
                    if (homeStretchStep < finalTiles.Length)
                    {
                        return finalTiles[homeStretchStep].transform.position;
                    }
                }
            }
            else if (boardPos >= 0 && boardPos < tiles.Length)
            {
                return tiles[boardPos];
            }
        
            return Vector3.zero;
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (tiles == null) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            for (int i = 0; i < tiles.Length; i++)
            {
                UnityEditor.Handles.Label(tiles[i], (i + 1).ToString(), style);
            }
        }
#endif
    }
}