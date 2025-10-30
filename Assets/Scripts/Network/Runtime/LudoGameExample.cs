using EasyButtons;
using UnityEngine;
using Network.Runtime;

/// <summary>
/// Example usage of LudoNetworkManager
/// Attach this to a GameObject in your scene
/// </summary>
public class LudoGameExample : MonoBehaviour
{
    [SerializeField] private LudoNetworkManager networkManager;
    
    private GameState currentGameState;
    private int myPlayerIndex = -1;
    private int[] validMoves;
    
    private void Start()
    {
        // Subscribe to events
        networkManager.onConnected.AddListener(OnConnected);
        networkManager.onMatchFound.AddListener(OnMatchFound);
        networkManager.onDiceRolled.AddListener(OnDiceRolled);
        networkManager.onTokenMoved.AddListener(OnTokenMoved);
        networkManager.onGameOver.AddListener(OnGameOver);
        networkManager.onError.AddListener(OnError);
        
        // Connect to server
        networkManager.Connect();
    }
    
    private void OnConnected()
    {
        Debug.Log("Connected! Joining queue...");
        
        // Join a queue for 4 players in casual mode
        networkManager.JoinQueue("casual", 4, "MyUnityPlayer");
    }
    
    private void OnMatchFound(MatchFoundResponse response)
    {
        Debug.Log($"Match found with {response.playerCount} players!");
        
        // Store initial game state
        currentGameState = response.gameState;
        
        // Find my player index
        foreach (var player in response.players)
        {
            if (player.playerId == networkManager.PlayerId)
            {
                myPlayerIndex = player.playerIndex;
                Debug.Log($"I am player {myPlayerIndex}");
                break;
            }
        }
        
        // Check if it's my turn
        if (currentGameState.currentPlayer == myPlayerIndex)
        {
            Debug.Log("It's my turn! Rolling dice...");
            networkManager.RollDice();
        }
    }
    
    private void OnDiceRolled(DiceRolledResponse response)
    {
        Debug.Log($"Player {response.playerIndex} rolled {response.diceValue}");
        
        // If it's my turn
        if (response.playerIndex == myPlayerIndex)
        {
            validMoves = response.validMoves;
            
            if (response.noValidMoves)
            {
                Debug.Log("No valid moves! Turn skipped.");
                return;
            }
            
            Debug.Log($"Valid moves: {string.Join(", ", validMoves)}");
            
            // For this example, just move the first valid token
            if (validMoves.Length > 0)
            {
                int tokenToMove = validMoves[0];
                Debug.Log($"Moving token {tokenToMove}");
                networkManager.MoveToken(tokenToMove);
            }
        }
    }
    
    private void OnTokenMoved(TokenMovedResponse response)
    {
        Debug.Log($"Player {response.playerIndex} moved token {response.tokenIndex} to position {response.newPosition}");
        Debug.Log($"Move result: {response.moveResult}");
        
        // Update game state
        currentGameState = response.gameState;
        
        // Check if it's now my turn
        if (response.nextPlayer == myPlayerIndex && !response.hasWon)
        {
            Debug.Log("It's my turn again! Rolling dice...");
            // Small delay to see what happened
            Invoke(nameof(RollMyDice), 1f);
        }
    }
    
    [Button]
    private void RollMyDice()
    {
        networkManager.RollDice();
    }
    
    private void OnGameOver(GameOverResponse response)
    {
        Debug.Log($"GAME OVER! Winner: {response.winnerName} (Player {response.winnerIndex})");
        
        if (response.winnerIndex == myPlayerIndex)
        {
            Debug.Log("🎉 I WON! 🎉");
        }
        else
        {
            Debug.Log("Better luck next time!");
        }
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"Server error: {error}");
    }
    
    // Public methods you can call from UI buttons
    
    public void ManualJoinQueue()
    {
        networkManager.JoinQueue("casual", 4);
    }
    
    public void ManualLeaveQueue()
    {
        networkManager.LeaveQueue();
    }
    
    public void ManualRollDice()
    {
        networkManager.RollDice();
    }
    
    public void ManualMoveToken(int tokenIndex)
    {
        networkManager.MoveToken(tokenIndex);
    }
    
    public void ManualLeaveGame()
    {
        networkManager.LeaveGame();
    }
    
    public void ManualGetState()
    {
        networkManager.GetGameState();
    }
}
