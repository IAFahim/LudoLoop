using UnityEngine;
using UnityEngine.InputSystem;

// 1. Add the Input System namespace

namespace Network.Runtime
{
    public class LudoTestScript : MonoBehaviour
    {
        private LudoClient client;
        private bool hasRolled = false;
        private int[] validMoves;

        private LudoControls controls; // 2. Reference to our generated controls class

        void Awake()
        {
            client = GetComponent<LudoClient>();
            controls = new LudoControls(); // 3. Create a new instance of the controls
        }
    
        void OnEnable()
        {
            // Subscribe to game client events
            client.OnConnected.AddListener(OnConnected);
            client.OnQueueJoined.AddListener(OnQueueJoined);
            client.OnMatchFound.AddListener(OnMatchFound);
            client.OnDiceRolled.AddListener(OnDiceRolled);
            client.OnTokenMoved.AddListener(OnTokenMoved);
            client.OnGameOver.AddListener(OnGameOver);
            client.OnError.AddListener(OnError);
        
            // 4. Subscribe to the 'performed' event for each input action
            controls.Player.FindMatch.performed += OnFindMatchInput;
            controls.Player.RollDice.performed += OnRollDiceInput;
            controls.Player.Leave.performed += OnLeaveInput;
            controls.Player.MoveToken.performed += OnMoveTokenInput;

            // 5. Enable the 'Player' action map
            controls.Player.Enable();
        
            Debug.Log("🔌 Connecting to server...");
            client.Connect();
        }

        private void OnDisable()
        {
            // Unsubscribe from game client events
            client.OnConnected.RemoveListener(OnConnected);
            client.OnQueueJoined.RemoveListener(OnQueueJoined);
            client.OnMatchFound.RemoveListener(OnMatchFound);
            client.OnDiceRolled.RemoveListener(OnDiceRolled);
            client.OnTokenMoved.RemoveListener(OnTokenMoved);
            client.OnGameOver.RemoveListener(OnGameOver);
            client.OnError.RemoveListener(OnError);
        
            // 6. Disable the action map to stop listening for input
            controls.Player.Disable();
        
            // It's good practice to also unsubscribe from the input events
            controls.Player.FindMatch.performed -= OnFindMatchInput;
            controls.Player.RollDice.performed -= OnRollDiceInput;
            controls.Player.Leave.performed -= OnLeaveInput;
            controls.Player.MoveToken.performed -= OnMoveTokenInput;
        }

        #region Input System Handlers

        private void OnFindMatchInput(InputAction.CallbackContext context)
        {
            Debug.Log("🔍 Finding match...");
            client.FindMatch("casual", 4);
        }

        private void OnRollDiceInput(InputAction.CallbackContext context)
        {
            if (client.IsMyTurn() && !hasRolled)
            {
                Debug.Log("🎲 Rolling dice...");
                client.RollDice();
            }
        }

        private void OnLeaveInput(InputAction.CallbackContext context)
        {
            if (client.IsInQueue())
            {
                Debug.Log("Leaving queue...");
                client.LeaveQueue();
            }
            else if (client.IsInGame())
            {
                Debug.Log("Leaving game...");
                client.LeaveGame();
            }
        }

        private void OnMoveTokenInput(InputAction.CallbackContext context)
        {
            if (!hasRolled || validMoves == null) return;
        
            // context.control.name gives us the name of the key pressed (e.g., "1", "2")
            if (int.TryParse(context.control.name, out int tokenNumber))
            {
                if (tokenNumber is >= 1 and <= 4)
                {
                    int globalIndex = client.GetPlayerIndex() * 4 + (tokenNumber - 1);
                    if (System.Array.IndexOf(validMoves, globalIndex) >= 0)
                    {
                        Debug.Log($"Moving token {tokenNumber}...");
                        client.MoveToken(globalIndex);
                    }
                    else
                    {
                        Debug.Log($"❌ Token {tokenNumber} is not a valid move!");
                    }
                }
            }
        }

        #endregion

        #region LudoClient Event Handlers
    
        private void OnConnected(string id)
        {
            Debug.Log($"✓ Connected! ID: {id}");
        }
    
        private void OnQueueJoined(int count)
        {
            Debug.Log($"⏳ In queue. Players: {count}");
        }
    
        private void OnMatchFound(MatchData match)
        {
            Debug.Log($"🎮 Match found! You are Player {match.myPlayerIndex}");
            if (client.IsMyTurn()) Debug.Log("🎲 Your turn! Press 'R' to roll");
        }

        private void OnDiceRolled(DiceRollData data)
        {
            if (data.playerIndex == client.GetPlayerIndex())
            {
                validMoves = data.validMoves;
                hasRolled = true;
            
                if (data.noValidMoves)
                {
                    Debug.Log($"🎲 Rolled {data.diceValue} - No valid moves! Turn skipped.");
                    hasRolled = false; // Reset for next turn
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
    
        private void OnTokenMoved(TokenMoveData move)
        {
            Debug.Log($"🎯 Token moved: {move.moveResult}");
            hasRolled = false;
            if (client.IsMyTurn()) Debug.Log("🎲 Your turn! Press 'R' to roll");
        }
    
        private void OnGameOver(GameOverData data)
        {
            Debug.Log($"🏆 Game Over! Winner: {data.winnerName}");
        }
    
        private void OnError(string err)
        {
            Debug.LogError($"❌ Error: {err}");
        }
    
        #endregion
    }
}