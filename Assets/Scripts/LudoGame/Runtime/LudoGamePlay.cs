using System.Collections.Generic;
using EasyButtons;
using Ludo;
using Placements.Runtime;
using UnityEngine;

public class LudoGamePlay : MonoBehaviour, ILudoBoard
{
    [Header("Game Setup")] public GameSession gameSession;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private Tiles tileSystem;
    

    private void Start()
    {
        if (gameSession == null) return;
        playerSpawner.CreateBaseForPlayerCount(PlayerCount);
        RefreshState();
    }

    private void OnValidate()
    {
        RefreshState();
    }

    [Button]
    public void RefreshState()
    {
        for (int playerIndex = 0; playerIndex < gameSession.board.PlayerCount; playerIndex++)
        {
            for (int tokenOrdinal = 0; tokenOrdinal < LudoBoard.Tokens; tokenOrdinal++)
            {
                int tokenIndex = gameSession.board.TokenIndex(playerIndex, tokenOrdinal);
                byte logicalPos = gameSession.board.TokenPositions[tokenIndex];
                Vector3 worldPos;

                if (gameSession.board.IsAtBase(tokenIndex))
                {
                    worldPos = GetBasePosition(playerIndex);
                }
                else if (gameSession.board.IsHome(tokenIndex))
                {
                    worldPos = GetHomePosition(playerIndex, tokenOrdinal);
                }
                else if (gameSession.board.IsOnHomeStretch(tokenIndex))
                {
                    int step = logicalPos - LudoBoard.HomeStart;
                    worldPos = GetHomeStretchPosition(playerIndex, step);
                }
                else // main track
                {
                    if (logicalPos is >= 1 and <= 52)
                    {
                        int abs = gameSession.board.GetAbsolutePosition(tokenIndex);
                        worldPos = GetAbsoluteBoardPosition(abs);
                    }
                    else
                    {
                        worldPos = Vector3.zero;
                    }
                }

                Debug.DrawLine(worldPos, worldPos + Vector3.up * 5, Color.black, 1);
                MoveTokenToPosition(playerIndex, tokenOrdinal, worldPos);
            }
        }
    }

    

    public Vector3 GetBasePosition(int playerIndex)
    {
        var offsetPlayerIndex = gameSession.OffsetPlayerIndex(playerIndex);
        return playerSpawner.pawnBasePositions[offsetPlayerIndex];
    }

    public Vector3 GetHomeStretchPosition(int playerIndex, int step)
    {
        var offsetPlayerIndex = gameSession.OffsetPlayerIndex(playerIndex);
        return tileSystem.groupedTiles[offsetPlayerIndex].tileFinal[step].transform.position;
    }

    public Vector3 GetAbsoluteBoardPosition(int abs) => tileSystem.tiles[abs - 1];

    [Button]
    public void Predict(int playerIndex, byte dice)
    {
        var movableTokens = gameSession.board.GetMovableTokens(playerIndex, dice);
        foreach (var token in movableTokens)
        {
            Debug.Log(token);
        }
    }


    [Button]
    public void Play(int tokenIndex, int steps)
    {
        gameSession.board.MoveToken(tokenIndex, steps, out var tokenSentToBase);
    }
    

    public Vector3 GetHomePosition(int playerIndex, int tokenOrdinal)
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

    public byte[] TokenPositions => gameSession.board.TokenPositions;

    public int PlayerCount => gameSession.board.PlayerCount;

    public bool IsAtBase(int t) => gameSession.board.IsAtBase(t);

    public bool IsOnMainTrack(int t) => gameSession.board.IsOnMainTrack(t);

    public bool IsOnHomeStretch(int t) => gameSession.board.IsOnHomeStretch(t);

    public bool IsHome(int t) => gameSession.board.IsHome(t);

    public bool IsOnSafeTile(int t) => gameSession.board.IsOnSafeTile(t);

    public bool HasWon(int playerIndex) => gameSession.board.HasWon(playerIndex);

    public void MoveToken(int tokenIndex, int steps, out byte tokenSentToBase)
    {
        gameSession.board.MoveToken(tokenIndex, steps, out tokenSentToBase);
        RefreshState();
    }

    public void GetOutOfBase(int tokenIndex)
    {
        gameSession.board.GetOutOfBase(tokenIndex);
        RefreshState();
    }

    public List<byte> GetMovableTokens(int playerIndex, int diceRoll) => gameSession.board.GetMovableTokens(playerIndex, diceRoll);

    public int GetAbsolutePosition(int tokenIndex) => gameSession.board.GetAbsolutePosition(tokenIndex);

    public int ToAbsoluteMainTrack(byte relativeMainTrackTile, int playerIndex) =>
        gameSession.board.ToAbsoluteMainTrack(relativeMainTrackTile, playerIndex);

    public int StartAbsoluteTile(int playerIndex) => gameSession.board.StartAbsoluteTile(playerIndex);

    public int TokenIndex(int playerIndex, int tokenOrdinal) => gameSession.board.TokenIndex(playerIndex, tokenOrdinal);

    public byte RelativeForAbsolute(int playerIndex, int absoluteTile) =>
        gameSession.board.RelativeForAbsolute(playerIndex, absoluteTile);

    public void DebugSetTokenAtRelative(int playerIndex, int tokenOrdinal, int relative)
    {
        gameSession.board.DebugSetTokenAtRelative(playerIndex, tokenOrdinal, relative);
        RefreshState();
    }

    public void DebugSetTokenAtAbsolute(int playerIndex, int tokenOrdinal, int absoluteTile)
    {
        gameSession.board.DebugSetTokenAtAbsolute(playerIndex, tokenOrdinal, absoluteTile);
        RefreshState();
    }

    public void DebugMakeBlockadeAtAbsolute(int ownerPlayerIndex, int absoluteTile)
    {
        gameSession.board.DebugMakeBlockadeAtAbsolute(ownerPlayerIndex, absoluteTile);
        RefreshState();
    }

    #endregion
}