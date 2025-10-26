using System.Collections.Generic;
using Ludo;
using LudoGame.Runtime;
using Placements.Runtime;
using UnityEngine;

public class LudoGamePlay : MonoBehaviour, ILudoBoard
    {
        [Header("Game Setup")] public GameState gameState;
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private Tiles tileSystem;
        [SerializeField] private ColorTiles[] playerColorTiles; // Per player home stretches

        private TokenBase[] bases;
        private HomeBase[] homes;

        private IPositionProvider positionProvider;
        private ITokenProvider tokenProvider;

        private void OnEnable()
        {
            if (gameState == null) return;
            int playerCount = bases?.Length ?? 2; // Default to 2 if not set
            if (playerCount < 2 || playerCount > 4) playerCount = 2;

            if (gameState.board.TokenPositions == null || gameState.board.PlayerCount != playerCount)
            {
                gameState.board = new LudoBoard(playerCount);
            }
            gameState.currentPlayerIndex = 0;
            gameState.diceValue = 0;

            if (playerSpawner != null)
            {
                playerSpawner.SetupPlayers(gameState.board.PlayerCount);
                bases = playerSpawner.pawnBases;
                homes = playerSpawner.homes;
            }

            positionProvider = new UnityPositionProvider(bases, tileSystem, playerColorTiles, homes);
            tokenProvider = new UnityTokenProvider(bases);

            RefreshState();
        }

        private void OnValidate()
        {
            RefreshState();
        }

        public void RefreshState()
        {
            if (gameState == null || positionProvider == null || tokenProvider == null) return;
            if (bases == null || bases.Length != gameState.board.PlayerCount) return;
            if (homes == null || homes.Length != gameState.board.PlayerCount) return;
            if (playerColorTiles == null || playerColorTiles.Length != gameState.board.PlayerCount) return;
            if (tileSystem == null || tileSystem.tiles == null || tileSystem.tiles.Length != 52) return;

            for (int p = 0; p < gameState.board.PlayerCount; p++)
            {
                for (int o = 0; o < LudoBoard.Tokens; o++)
                {
                    int t = gameState.board.TokenIndex(p, o);
                    byte logicalPos = gameState.board.TokenPositions[t];
                    Vector3 worldPos;

                    if (gameState.board.IsAtBase(t))
                    {
                        worldPos = positionProvider.GetBasePosition(p, o);
                    }
                    else if (gameState.board.IsHome(t))
                    {
                        worldPos = positionProvider.GetHomePosition(p, o);
                    }
                    else if (gameState.board.IsOnHomeStretch(t))
                    {
                        int step = logicalPos - LudoBoard.HomeStart;
                        worldPos = positionProvider.GetHomeStretchPosition(p, step);
                    }
                    else // main track
                    {
                        int abs = gameState.board.GetAbsolutePosition(t);
                        if (abs >= 1 && abs <= 52)
                        {
                            worldPos = positionProvider.GetMainTrackPosition(abs);
                        }
                        else continue;
                    }

                    tokenProvider.MoveTokenToPosition(p, o, worldPos);
                }
            }
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        #region ILudoBoard Implementation

        public byte[] TokenPositions => gameState.board.TokenPositions;

        public int PlayerCount => gameState.board.PlayerCount;

        public bool IsAtBase(int t) => gameState.board.IsAtBase(t);

        public bool IsOnMainTrack(int t) => gameState.board.IsOnMainTrack(t);

        public bool IsOnHomeStretch(int t) => gameState.board.IsOnHomeStretch(t);

        public bool IsHome(int t) => gameState.board.IsHome(t);

        public bool IsOnSafeTile(int t) => gameState.board.IsOnSafeTile(t);

        public bool HasWon(int playerIndex) => gameState.board.HasWon(playerIndex);

        public void MoveToken(int tokenIndex, int steps)
        {
            gameState.board.MoveToken(tokenIndex, steps);
            RefreshState();
        }

        public void GetOutOfBase(int tokenIndex)
        {
            gameState.board.GetOutOfBase(tokenIndex);
            RefreshState();
        }

        public List<int> GetMovableTokens(int playerIndex, int diceRoll) => gameState.board.GetMovableTokens(playerIndex, diceRoll);

        public int GetAbsolutePosition(int tokenIndex) => gameState.board.GetAbsolutePosition(tokenIndex);

        public int ToAbsoluteMainTrack(byte relativeMainTrackTile, int playerIndex) => gameState.board.ToAbsoluteMainTrack(relativeMainTrackTile, playerIndex);

        public int StartAbsoluteTile(int playerIndex) => gameState.board.StartAbsoluteTile(playerIndex);

        public int TokenIndex(int playerIndex, int tokenOrdinal) => gameState.board.TokenIndex(playerIndex, tokenOrdinal);

        public byte RelativeForAbsolute(int playerIndex, int absoluteTile) => gameState.board.RelativeForAbsolute(playerIndex, absoluteTile);

        public void DebugSetTokenAtRelative(int playerIndex, int tokenOrdinal, int relative)
        {
            gameState.board.DebugSetTokenAtRelative(playerIndex, tokenOrdinal, relative);
            RefreshState();
        }

        public void DebugSetTokenAtAbsolute(int playerIndex, int tokenOrdinal, int absoluteTile)
        {
            gameState.board.DebugSetTokenAtAbsolute(playerIndex, tokenOrdinal, absoluteTile);
            RefreshState();
        }

        public void DebugMakeBlockadeAtAbsolute(int ownerPlayerIndex, int absoluteTile)
        {
            gameState.board.DebugMakeBlockadeAtAbsolute(ownerPlayerIndex, absoluteTile);
            RefreshState();
        }

        #endregion
    }