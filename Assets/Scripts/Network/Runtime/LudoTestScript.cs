
using Network.Runtime.Network.Runtime;
using UnityEngine;

public class LudoTestScript : MonoBehaviour
{
    private LudoClient client;
    private bool hasRolled = false;
    private int[] validMoves;

    void OnEnable()
    {
        client = GetComponent<LudoClient>();
        
        // Subscribe to events
        client.OnConnected.AddListener(OnConnected);
        client.OnQueueJoined.AddListener(OnQueueJoined);
        client.OnMatchFound.AddListener(AddListener);
        client.OnDiceRolled.AddListener(OnDiceRolled);
        client.OnTokenMoved.AddListener(OnTokenMove);
        client.OnGameOver.AddListener(OnGameOver);
        client.OnError.AddListener(OnError);
        Debug.Log("🔌 Connecting to server...");
        client.Connect();
    }

    private void OnError(string err)
    {
        Debug.LogError($"❌ Error: {err}");
    }

    private void OnGameOver(GameOverData data)
    {
        Debug.Log($"🏆 Game Over! Winner: {data.winnerName}");
    }

    private void OnTokenMove(TokenMoveData move)
    {
        Debug.Log($"🎯 Token moved: {move.moveResult}");
        hasRolled = false;
        if (client.IsMyTurn()) Debug.Log("🎲 Your turn! Press roll");
    }

    private void AddListener(MatchData match)
    {
        Debug.Log($"🎮 Match found! You are Player {match.myPlayerIndex}");
    }

    private void OnConnected(string id)
    {
        Debug.Log($"✓ Connected! ID: {id}");
        client.FindMatch();
    }

    private void OnQueueJoined(int count)
    {
        Debug.Log($"⏳ In queue. Players: {count}");
    }


    void OnDiceRolled(DiceRollData data)
    {
        if (data.playerIndex == client.GetPlayerIndex())
        {
            validMoves = data.validMoves;
            hasRolled = true;
            
            if (data.noValidMoves)
            {
                Debug.Log($"🎲 Rolled {data.diceValue} - No valid moves!");
                hasRolled = false;
            }
            else
            {
                Debug.Log($"🎲 Rolled {data.diceValue}! Valid tokens: {string.Join(", ", data.validMoves)}");
                Debug.Log("Press 1-4 to move your token");
            }
        }
        else
        {
            Debug.Log($"Player {data.playerIndex} rolled {data.diceValue}");
        }
    }

    [ContextMenu("RollDice")]
    public void RollDice()
    {
        client.RollDice();
    }

    // void Update()
    // {
    //     // Keyboard controls
    //     if (Input.GetKeyDown(KeyCode.F))
    //     {
    //         Debug.Log("🔍 Finding match...");
    //         client.FindMatch("casual", 4);
    //     }
    //     
    //     if (Input.GetKeyDown(KeyCode.R) && client.IsMyTurn() && !hasRolled)
    //     {
    //         Debug.Log("🎲 Rolling dice...");
    //         client.RollDice();
    //     }
    //     
    //     // Move tokens (keys 1-4)
    //     if (hasRolled && validMoves != null)
    //     {
    //         for (int i = 1; i <= 4; i++)
    //         {
    //             if (Input.GetKeyDown(KeyCode.Alpha0 + i))
    //             {
    //                 int globalIndex = client.GetPlayerIndex() * 4 + (i - 1);
    //                 if (System.Array.IndexOf(validMoves, globalIndex) >= 0)
    //                 {
    //                     Debug.Log($"Moving token {i}...");
    //                     client.MoveToken(globalIndex);
    //                 }
    //                 else
    //                 {
    //                     Debug.Log($"❌ Token {i} is not valid!");
    //                 }
    //             }
    //         }
    //     }
    //     
    //     if (Input.GetKeyDown(KeyCode.L))
    //     {
    //         if (client.IsInQueue())
    //         {
    //             Debug.Log("Leaving queue...");
    //             client.LeaveQueue();
    //         }
    //         else if (client.IsInGame())
    //         {
    //             Debug.Log("Leaving game...");
    //             client.LeaveGame();
    //         }
    //     }
    // }
}