using System.Collections.Generic;
using Ludo;
using Placements.Runtime;
using UnityEngine;

public class LudoGamePlay : MonoBehaviour, ILudoBoard
{
    [Header("Game Setup")] public GameState gameState;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private Tiles tileSystem;
    [SerializeField] private GroupedTiles[] playerColorTiles; // Per player home stretches

    private void OnEnable()
    {
        if (gameState == null) return;
        playerSpawner.CreateBaseForPlayerCount(PlayerCount);
        RefreshState();
    }

    private void OnValidate()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        for (int p = 0; p < gameState.board.PlayerCount; p++)
        {
            for (int o = 0; o < LudoBoard.Tokens; o++)
            {
                int t = gameState.board.TokenIndex(p, o);
                byte logicalPos = gameState.board.TokenPositions[t];
                Vector3 worldPos;

                if (gameState.board.IsAtBase(t))
                {
                    worldPos = GetBasePosition(p, o);
                }
                else if (gameState.board.IsHome(t))
                {
                    worldPos = GetHomePosition(p, o);
                }
                else if (gameState.board.IsOnHomeStretch(t))
                {
                    int step = logicalPos - LudoBoard.HomeStart;
                    worldPos = GetHomeStretchPosition(p, step);
                }
                else // main track
                {
                    int abs = gameState.board.GetAbsolutePosition(t);
                    if (abs >= 1 && abs <= 52)
                    {
                        worldPos = GetMainTrackPosition(abs);
                    }
                    else continue;
                }

                MoveTokenToPosition(p, o, worldPos);
            }
        }
    }


    public Vector3 GetBasePosition(int playerIndex, int tokenOrdinal)
    {
        return Vector3.zero;
    }

    public Vector3 GetMainTrackPosition(int absPosition)
    {
        return Vector3.zero;
    }

    public Vector3 GetHomeStretchPosition(int playerIndex, int step)
    {
        return Vector3.zero;
    }

    public Vector3 GetHomePosition(int playerIndex, int tokenOrdinal)
    {
        return Vector3.zero;
    }

    public Vector3 GetTokenTransform(int playerIndex, int tokenOrdinal)
    {
        return Vector3.zero;
    }

    public void HighlightToken(int playerIndex, int tokenOrdinal, bool highlight)
    {
        
    }

    public void MoveTokenToPosition(int playerIndex, int tokenOrdinal, Vector3 position)
    {
       
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

    public List<int> GetMovableTokens(int playerIndex, int diceRoll) =>
        gameState.board.GetMovableTokens(playerIndex, diceRoll);

    public int GetAbsolutePosition(int tokenIndex) => gameState.board.GetAbsolutePosition(tokenIndex);

    public int ToAbsoluteMainTrack(byte relativeMainTrackTile, int playerIndex) =>
        gameState.board.ToAbsoluteMainTrack(relativeMainTrackTile, playerIndex);

    public int StartAbsoluteTile(int playerIndex) => gameState.board.StartAbsoluteTile(playerIndex);

    public int TokenIndex(int playerIndex, int tokenOrdinal) => gameState.board.TokenIndex(playerIndex, tokenOrdinal);

    public byte RelativeForAbsolute(int playerIndex, int absoluteTile) =>
        gameState.board.RelativeForAbsolute(playerIndex, absoluteTile);

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