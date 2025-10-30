using EasyButtons;
using Ludo;
using Placements.Runtime;
using UnityEngine;

public class LudoGamePlay : MonoBehaviour
{
    [Header("Game Setup")] public GameSession gameSession;
    public PlayerSpawner playerSpawner;
    [SerializeField] private Tiles tileSystem;

    public GameSession Session => gameSession;
    public bool isPlaying;

    private void Start()
    {
        RefreshState();
        isPlaying = true;
    }

    private void OnValidate()
    {
        RefreshState();
    }

    [Button]
    public void RefreshState()
    {
        for (int playerIndex = 0; playerIndex < Session.board.PlayerCount; playerIndex++)
        {
            for (int tokenOrdinal = 0; tokenOrdinal < LudoBoard.Tokens; tokenOrdinal++)
            {
                int tokenIndex = Session.board.TokenIndex(playerIndex, tokenOrdinal);
                byte logicalPos = Session.board.TokenPositions[tokenIndex];
                var currentPos = GetPosition(tokenIndex, playerIndex, tokenOrdinal, logicalPos);
                MoveTokenToPosition(playerIndex, tokenOrdinal, currentPos);
            }
        }
    }

    private Vector3 GetPosition(int tokenIndex, int playerIndex, int tokenOrdinal, byte logicalPos)
    {
        Vector3 currentPos;

        if (Session.board.IsAtBase(tokenIndex))
        {
            currentPos = GetBasePosition(playerIndex, tokenOrdinal);
        }
        else if (Session.board.IsHome(tokenIndex))
        {
            currentPos = GetHomePosition(playerIndex, tokenOrdinal);
        }
        else if (Session.board.IsOnHomeStretch(tokenIndex))
        {
            int step = logicalPos - LudoBoard.HomeStart;
            currentPos = GetHomeStretchPosition(playerIndex, step);
        }
        else // main track
        {
            if (logicalPos is >= 1 and <= 52)
            {
                int abs = Session.board.GetAbsolutePosition(tokenIndex);
                currentPos = GetAbsoluteBoardPosition(abs);
            }
            else
            {
                currentPos = Vector3.zero;
            }
        }

        Debug.DrawLine(currentPos, currentPos + Vector3.up * 5, Color.black, 1);
        return currentPos;
    }

    public void EndTurn(int playerIndex)
    {
        Session.EndTurn();
    }

    public Vector3 GetBasePosition(int playerIndex, int tokenOrdinal)
    {
        var tokenBase = playerSpawner.tokenBases[playerIndex];
        var tokenBasePosition = tokenBase.tokenBasePositions[tokenOrdinal] + tokenBase.transform.position;
        return tokenBasePosition;
    }

    public Vector3 GetHomeStretchPosition(int playerIndex, int step)
    {
        var offsetPlayerIndex = gameSession.OffsetPlayerIndex(playerIndex);
        return tileSystem.groupedTiles[playerIndex].tileFinal[step].transform.position;
    }

    public Vector3 GetAbsoluteBoardPosition(int abs)
    {
        return tileSystem.tiles[abs - 1];
    }


    public void Play(int tokenIndex, int steps, out byte tokenSentToBase)
    {
        gameSession.board.MoveToken(tokenIndex, steps, out tokenSentToBase);
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
        if (!isPlaying) return;
        playerSpawner.tokenBases[playerIndex].Tokens[tokenOrdinal].transform.position = position;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}