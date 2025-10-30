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
                Vector3 worldPos;

                if (Session.board.IsAtBase(tokenIndex))
                {
                    worldPos = GetBasePosition(playerIndex, tokenOrdinal);
                }
                else if (Session.board.IsHome(tokenIndex))
                {
                    worldPos = GetHomePosition(playerIndex, tokenOrdinal);
                }
                else if (Session.board.IsOnHomeStretch(tokenIndex))
                {
                    int step = logicalPos - LudoBoard.HomeStart;
                    worldPos = GetHomeStretchPosition(playerIndex, step);
                }
                else // main track
                {
                    if (logicalPos is >= 1 and <= 52)
                    {
                        int abs = Session.board.GetAbsolutePosition(tokenIndex);
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

    public void EndTurn(int playerIndex)
    {
        Session.EndTurn();
    }

    public Vector3 GetBasePosition(int playerIndex, int tokenOrdinal)
    {
        var tokenBase = playerSpawner.tokenBases[playerIndex];
        var tokenBasePosition = tokenBase.tokenBasePositions[tokenOrdinal];
        if (isPlaying)
        {
            tokenBase.Tokens[tokenOrdinal].transform.position = tokenBasePosition;
        }
        return tokenBasePosition;
    }

    public Vector3 GetHomeStretchPosition(int playerIndex, int step)
    {
        var offsetPlayerIndex = gameSession.OffsetPlayerIndex(playerIndex);
        return tileSystem.groupedTiles[playerIndex].tileFinal[step].transform.position;
    }

    public Vector3 GetAbsoluteBoardPosition(int abs) => tileSystem.tiles[abs - 1];


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
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}