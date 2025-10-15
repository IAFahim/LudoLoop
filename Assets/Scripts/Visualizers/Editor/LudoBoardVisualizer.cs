using System.Collections.Generic;
using LudoGame.Runtime;
using Placements.Runtime;
using UnityEditor;
using UnityEngine;

namespace Visualizers.Editor
{
    public class LudoBoardVisualizer : MonoBehaviour
    {
        [Header("Game Logic Source")]
        public SyncLudoGame ludoGame;

        [Header("Physical Board Definition")]
        [Tooltip("The single 'Tiles' component that contains the final, flattened 'tiles' array.")]
        public Tiles boardLayout;
    
        [Tooltip("Transforms representing the center of each player's base area.")]
        public Transform[] playerBases = new Transform[4];
    
        [Tooltip("Transforms representing the center of each player's finished/home area.")]
        public Transform[] playerHomes = new Transform[4];
    
        [Header("Gizmo Settings")]
        public float tokenSize = 0.4f;
        public float stackingOffset = 0.3f;

        private void OnDrawGizmos()
        {
            // --- 1. Safety Checks ---
            if (ludoGame == null || boardLayout == null || ludoGame.GameState.TokenPositions == null) return;

            // --- 2. Initialization ---
            var gameState = ludoGame.GameState;
            var playerColors = new[] { Color.red, Color.blue, Color.green, Color.yellow };
            var tokensOnTile = new Dictionary<Vector3, int>();

            // --- 3. Main Token Drawing Loop ---
            for (int i = 0; i < gameState.TokenPositions.Length; i++)
            {
                int playerIndex = i / 4;
                if (playerIndex >= gameState.PlayerCount) continue;

                sbyte boardPos = gameState.TokenPositions[i];
                Vector3 finalTokenPosition = Vector3.zero;

                // --- 4. Determine Token Position ---
                if (boardPos == LudoBoard.PosBase)
                {
                    if (playerIndex < playerBases.Length && playerBases[playerIndex] != null)
                    {
                        int tokenIndexInTeam = i % 4;
                        Vector3 offset = new Vector3((tokenIndexInTeam % 2 - 0.5f), 0, (tokenIndexInTeam / 2 - 0.5f)) * stackingOffset * 2;
                        finalTokenPosition = playerBases[playerIndex].position + offset;
                    }
                }
                else if (boardPos == LudoBoard.PosFinished)
                {
                    if (playerIndex < playerHomes.Length && playerHomes[playerIndex] != null)
                    {
                        int tokenIndexInTeam = i % 4;
                        Vector3 offset = new Vector3(tokenIndexInTeam * stackingOffset, 0, 0);
                        finalTokenPosition = playerHomes[playerIndex].position + offset;
                    }
                }
                else // Token is on a board tile
                {
                    var targetTile = GetTileBoardPosition(boardPos, playerIndex);
                    if (targetTile != Vector3.zero)
                    {
                        tokensOnTile.TryGetValue(targetTile, out int count);
                        Vector3 offset = new Vector3(count * stackingOffset, 0, 0);
                        finalTokenPosition = targetTile + offset;
                        tokensOnTile[targetTile] = count + 1;
                    }
                }
            
                // --- 5. Draw the Gizmo ---
                if (finalTokenPosition != Vector3.zero)
                {
                    Gizmos.color = playerColors[playerIndex];
                    Gizmos.DrawSphere(finalTokenPosition, tokenSize / 2);
#if UNITY_EDITOR
                    Handles.color = Color.white;
                    Handles.Label(finalTokenPosition + Vector3.up * 0.3f, $"P{playerIndex}");
#endif
                }
            }
        }

        /// <summary>
        /// **THE CORRECTED MAPPING LOGIC**
        /// This now correctly uses the pre-built arrays from your Tiles/ColorTiles components.
        /// </summary>
        private Vector3 GetTileBoardPosition(sbyte boardPos, int playerIndex)
        {
            // Case 1: Token is on the HOME STRETCH (encoded as 100+)
            if (boardPos >= 100)
            {
                int homeStretchStep = (boardPos - 100) % 6;
                if (playerIndex < boardLayout.groupedTiles.Length)
                {
                    var finalTiles = boardLayout.groupedTiles[playerIndex].tileFinal;
                    if (homeStretchStep < finalTiles.Length)
                    {
                        return finalTiles[homeStretchStep].transform.position;
                    }
                }
            }
            // Case 2: Token is on the MAIN PATH (0-51)
            else if (boardPos >= 0 && boardPos < boardLayout.tiles.Length)
            {
                // The magic is here: We directly use the flattened 'tiles' array.
                // The logical boardPos is the same as the array index. No calculation needed!
                return boardLayout.tiles[boardPos];
            }
        
            return Vector3.zero;
        }
    }
}