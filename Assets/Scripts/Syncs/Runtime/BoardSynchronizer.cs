using System.Collections.Generic;
using System.Linq;
using LudoGame.Runtime;
using Placements.Runtime;
using UnityEngine;

namespace Syncs.Runtime
{
    /// <summary>
    /// Listens for OfflineLudoGame.onBoardSync and ensures the scene visually matches the logical state.
    /// </summary>
    public class BoardSynchronizer : MonoBehaviour
    {
        [Header("References")]
        public OfflineLudoGame ludoGame;
        public PlayerSpawner playerSpawner;
        public Tiles boardLayout;

        [Tooltip("Centers of each player's base area (P0..P3).")]
        public Transform[] playerBases = new Transform[4];

        [Tooltip("Centers of each player's home/finish area (P0..P3).")]
        public Transform[] playerHomes = new Transform[4];

        [Header("Placement")]
        public float tokenSize = 0.4f;
        public float stackingOffset = 0.3f;

        private void OnEnable()
        {
            if (ludoGame != null)
                ludoGame.onBoardSync.AddListener(ApplySync);
        }

        private void OnDisable()
        {
            if (ludoGame != null)
                ludoGame.onBoardSync.RemoveListener(ApplySync);
        }

        private void ApplySync(BoardSyncEventData data)
        {
            if (data.LudoGameState.TokenPositions == null || data.LudoGameState.TokenPositions.Length == 0)
            {
                Debug.LogWarning("[BoardSynchronizer] No token data in BoardSyncEventData.");
                return;
            }

            // 1) Ensure bases/tokens exist
            EnsureBasesAndTokens(data);

            // 2) Place tokens
            RepositionTokens(data);
        }

        private void EnsureBasesAndTokens(BoardSyncEventData data)
        {
            bool needSpawn = playerSpawner.pawnBases == null ||
                             playerSpawner.pawnBases.Length != data.LudoGameState.PlayerCount ||
                             playerSpawner.pawnBases.Any(pb => pb == null);

            if (needSpawn)
            {
                var createEvent = new GameCreatedEventData
                {
                    PlayerCount = data.LudoGameState.PlayerCount,
                    StartingPlayer = data.LudoGameState.CurrentPlayer,
                    GameState = ludoGame.GameState   // ok to pass – used for context if needed
                };

                playerSpawner.CreateBase(createEvent);
                Debug.Log($"[BoardSynchronizer] Spawned {data.LudoGameState.PlayerCount} bases.");
            }
        }

        private void RepositionTokens(BoardSyncEventData data)
        {
            if (boardLayout == null || boardLayout.tiles == null || boardLayout.tiles.Length == 0)
            {
                Debug.LogError("[BoardSynchronizer] Board layout not set or empty.");
                return;
            }

            var tokensOnTile = new Dictionary<GameObject, int>();

            for (int tokenIndex = 0; tokenIndex < data.LudoGameState.TokenPositions.Length; tokenIndex++)
            {
                int playerIndex = tokenIndex / 4;
                int inTeamIndex = tokenIndex % 4;

                if (playerIndex >= data.LudoGameState.PlayerCount) continue;

                var baseComp = playerSpawner.pawnBases[playerIndex];
                if (baseComp == null || baseComp.Tokens == null || baseComp.Tokens.Length < 4)
                {
                    Debug.LogWarning($"[BoardSynchronizer] Missing tokens for player {playerIndex}.");
                    continue;
                }

                var token = baseComp.Tokens[inTeamIndex];
                if (token == null) continue;

                var pos = data.LudoGameState.TokenPositions[tokenIndex];

                // Decide a world position
                Vector3 worldPos = Vector3.zero;

                // Base
                if (pos == LudoBoard.PosBase)
                {
                    if (playerIndex < playerBases.Length && playerBases[playerIndex] != null)
                    {
                        // same layout as your visualizer so base looks stable
                        Vector3 offset = new Vector3((inTeamIndex % 2 - 0.5f), 0, (inTeamIndex / 2 - 0.5f)) * stackingOffset * 2f;
                        worldPos = playerBases[playerIndex].position + offset;
                    }
                }
                // Finished/Home
                else if (pos == LudoBoard.PosFinished)
                {
                    if (playerIndex < playerHomes.Length && playerHomes[playerIndex] != null)
                    {
                        Vector3 offset = new Vector3(inTeamIndex * stackingOffset, 0, 0);
                        worldPos = playerHomes[playerIndex].position + offset;
                    }
                }
                // Home stretch (100+)
                else if (pos >= 100)
                {
                    GameObject tile = MapHomeStretchTile(pos, playerIndex);
                    if (tile != null) worldPos = tile.transform.position;
                }
                // Main loop [0..51]
                else if (pos >= 0 && pos < boardLayout.tiles.Length)
                {
                    //TODO: WoWo
                }

                if (worldPos != Vector3.zero)
                {
                    token.transform.position = worldPos;
                }
                else
                {
                    // Fallback: keep token where it is but log once
#if UNITY_EDITOR
                    Debug.LogWarning($"[BoardSynchronizer] Unmapped position {pos} for token {tokenIndex} (P{playerIndex}).");
#endif
                }
            }
        }

        private GameObject MapHomeStretchTile(sbyte boardPos, int playerIndex)
        {
            // In your encoding: 100 + (6*color) + step(0..5). The step can be derived mod 6.
            int step = (boardPos - 100) % 6;
            if (playerIndex < boardLayout.groupedTiles.Length)
            {
                var finals = boardLayout.groupedTiles[playerIndex].tileFinal;
                if (finals != null && step >= 0 && step < finals.Length) return finals[step];
            }
            return null;
        }
    }
}
