using EasyButtons;
using Ludo;
using Placements.Runtime;
using UnityEngine;
using System.Collections;

public class LudoGamePlay : MonoBehaviour
{
    [Header("Game Setup")] 
    public GameSession gameSession;
    public PlayerSpawner playerSpawner;
    [SerializeField] private Tiles tileSystem;

    [Header("Movement Settings")]
    [SerializeField] private bool useInstantMovement = false;
    [SerializeField] private float movementDelay = 0.2f;

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
                
                // Use instant positioning for refresh (setup)
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
        // TODO: Implement highlighting logic
    }

    /// <summary>
    /// Moves token smoothly to position using PID controller
    /// </summary>
    public void MoveTokenToPosition(int playerIndex, int tokenOrdinal, Vector3 position)
    {
        if (!isPlaying) return;
        
        var token = playerSpawner.tokenBases[playerIndex].Tokens[tokenOrdinal];
        var movementController = token.GetComponent<TokenMovementController>();
        
        if (movementController == null)
        {
            Debug.LogWarning($"Token {token.name} missing TokenMovementController, adding one...");
            movementController = token.gameObject.AddComponent<TokenMovementController>();
        }

        if (useInstantMovement)
        {
            movementController.TeleportToPosition(position);
        }
        else
        {
            movementController.SetTargetPosition(position);
        }
    }

    /// <summary>
    /// Instantly moves token to position (used for initialization)
    /// </summary>
    public void MoveTokenToPositionInstant(int playerIndex, int tokenOrdinal, Vector3 position)
    {
        var token = playerSpawner.tokenBases[playerIndex].Tokens[tokenOrdinal];
        var movementController = token.GetComponent<TokenMovementController>();
        
        if (movementController == null)
        {
            movementController = token.gameObject.AddComponent<TokenMovementController>();
        }
        
        movementController.TeleportToPosition(position);
    }

    /// <summary>
    /// Moves token along a path of positions (for multi-step moves)
    /// </summary>
    public void MoveTokenAlongPath(int playerIndex, int tokenOrdinal, Vector3[] waypoints)
    {
        if (!isPlaying || waypoints == null || waypoints.Length == 0) return;
        
        StartCoroutine(MoveAlongPathCoroutine(playerIndex, tokenOrdinal, waypoints));
    }

    private IEnumerator MoveAlongPathCoroutine(int playerIndex, int tokenOrdinal, Vector3[] waypoints)
    {
        var token = playerSpawner.tokenBases[playerIndex].Tokens[tokenOrdinal];
        var movementController = token.GetComponent<TokenMovementController>();
        
        if (movementController == null)
        {
            Debug.LogError("TokenMovementController not found!");
            yield break;
        }

        foreach (var waypoint in waypoints)
        {
            movementController.SetTargetPosition(waypoint);
            
            // Wait until token reaches the waypoint
            yield return new WaitUntil(() => movementController.HasReachedTarget);
            
            // Small delay between waypoints
            yield return new WaitForSeconds(movementDelay);
        }
    }

    /// <summary>
    /// Gets the path from current position to target position
    /// </summary>
    public Vector3[] GetMovementPath(int playerIndex, int tokenOrdinal, int steps)
    {
        int tokenIndex = Session.board.TokenIndex(playerIndex, tokenOrdinal);
        byte currentLogicalPos = Session.board.TokenPositions[tokenIndex];
        
        Vector3[] path = new Vector3[steps];
        
        for (int i = 0; i < steps; i++)
        {
            byte nextLogicalPos = (byte)(currentLogicalPos + i + 1);
            path[i] = GetPosition(tokenIndex, playerIndex, tokenOrdinal, nextLogicalPos);
        }
        
        return path;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}