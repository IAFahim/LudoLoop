using System.Collections.Generic;
using UnityEngine;

namespace Placements.Runtime
{
    public class Tiles : MonoBehaviour
    {
        public GameObject[] tiles;
        public ColorTiles[] groupedTiles;

        private void OnValidate()
        {
            var totalTiles = new List<GameObject>();

            foreach (var tile in groupedTiles[0].FirstTiles)
            {
                totalTiles.Add(tile);
            }

            foreach (var colorTiles in groupedTiles[1..4])
            {
                foreach (var tile in colorTiles.EndTiles)
                {
                    totalTiles.Add(tile);
                }

                foreach (var tile in colorTiles.FirstTiles)
                {
                    totalTiles.Add(tile);
                }
            }

            foreach (var tile in groupedTiles[0].EndTiles)
            {
                totalTiles.Add(tile);
            }

            tiles = totalTiles.ToArray();
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
                if (tiles[i] != null)
                {
                    UnityEditor.Handles.Label(tiles[i].transform.position, i.ToString(), style);
                }
            }
        }
#endif
    }
}