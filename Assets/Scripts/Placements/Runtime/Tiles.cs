using System;
using System.Collections.Generic;
using UnityEngine;

namespace Placements.Runtime
{
    public class Tiles : MonoBehaviour
    {
        public Vector3[] tiles;
        public float heightOffset;
        public GroupedTiles[] groupedTiles;

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

        private void Start()
        {
            SetColor();
        }

        public void SetColor()
        {
            for (var i = 0; i < groupedTiles.Length; i++)
            {
                var tile = groupedTiles[i];
                tile.SetColor(i);
            }
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (tiles == null) return;

            GUIStyle style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            for (int i = 0; i < tiles.Length; i++)
            {
                UnityEditor.Handles.Label(tiles[i], (i + 1).ToString(), style);
            }
        }
#endif
        public Vector3 GetMainTilePosition(int absPosition)
        {
            int index = absPosition - 1;
            if (index < 0 || index >= tiles.Length) return Vector3.zero;
            return tiles[index];
        }
    }
}