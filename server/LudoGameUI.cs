using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example usage of LudoClient - simple UI controller
/// Attach this to a GameObject with UI elements
/// </summary>
public class LudoGameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LudoClient client;
    
    [Header("UI Elements")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text gameInfoText;
    [SerializeField] private Text diceResultText;
    
    [Header("Token Buttons (16 total)")]
    [SerializeField] private Button[] tokenButtons = new Button[16];

    private void Start()
    {
        // Setup UI
        if (connectButton) connectButton.onClick.AddListener(OnConnectClicked);
        if (findMatchButton) findMatchButton.onClick.AddListener(OnFindMatchClicked);
        if (rollDiceButton) rollDiceButton.onClick.AddListener(OnRollDiceClicked);

        // Setup token buttons
        for (int i = 0; i < tokenButtons.Length; i++)
        {
            if (tokenButtons[i] != null)
            {
                int index = i; // Capture for closure
                tokenButtons[i].onClick.AddListener(() => OnTokenClicked(index));
            }
        }

        // Subscribe to client events
        if (client != null)
        {
            client.OnConnected += OnConnected;
            client.OnDisconnected += OnDisconnected;
            client.OnError += OnError;
            client.OnQueueJoined += OnQueueJoined;
            client.OnMatchFound += OnMatchFound;
            client.OnDiceRolled += OnDiceRolled;
            client.OnTokenMoved += OnTokenMoved;
            client.OnGameOver += OnGameOver;
        }

        UpdateUI();
    }

    // ==================== UI CALLBACKS ====================

    private void OnConnectClicked()
    {
        if (client.IsConnected())
        {
            client.Disconnect();
        }
        else
        {
            client.Connect();
        }
    }

    private void OnFindMatchClicked()
    {
        if (client.IsInQueue())
        {
            client.LeaveQueue();
        }
        else
        {
            client.FindMatch();
        }
    }

    private void OnRollDiceClicked()
    {
        if (client.IsMyTurn())
        {
            client.RollDice();
        }
    }

    private void OnTokenClicked(int tokenIndex)
    {
        if (client.IsInGame() && client.IsMyTurn())
        {
            client.MoveToken(tokenIndex);
        }
    }

    // ==================== CLIENT EVENT HANDLERS ====================

    private void OnConnected(string playerId)
    {
        UpdateStatus($"Connected! ID: {playerId.Substring(0, 8)}...");
        UpdateUI();
    }

    private void OnDisconnected()
    {
        UpdateStatus("Disconnected");
        UpdateUI();
    }

    private void OnError(string error)
    {
        UpdateStatus($"Error: {error}");
    }

    private void OnQueueJoined(int playersInQueue)
    {
        UpdateStatus($"Searching for match... ({playersInQueue} in queue)");
        UpdateUI();
    }

    private void OnMatchFound(MatchData matchData)
    {
        UpdateStatus($"Match found! {matchData.playerCount} players");
        UpdateGameInfo($"You are Player {matchData.myPlayerIndex}\nWaiting for turn...");
        UpdateUI();
    }

    private void OnDiceRolled(DiceRollData data)
    {
        if (diceResultText)
        {
            diceResultText.text = $"Player {data.playerIndex} rolled: {data.diceValue}";
        }

        if (data.noValidMoves)
        {
            UpdateStatus("No valid moves! Turn skipped.");
        }
        else if (data.playerIndex == client.GetPlayerIndex())
        {
            UpdateStatus($"You rolled {data.diceValue}! Select a token to move.");
        }
        
        UpdateUI();
    }

    private void OnTokenMoved(TokenMoveData data)
    {
        UpdateStatus($"Player {data.playerIndex} moved token {data.tokenIndex}");
        
        if (data.hasWon)
        {
            UpdateStatus($"Player {data.playerIndex} is about to win!");
        }
        
        UpdateGameInfo(GetGameStateInfo());
        UpdateUI();
    }

    private void OnGameOver(GameOverData data)
    {
        UpdateStatus($"Game Over! {data.winnerName} (Player {data.winnerIndex}) wins! 🎉");
        UpdateUI();
    }

    // ==================== UI UPDATES ====================

    private void UpdateUI()
    {
        // Update button states
        if (connectButton)
        {
            var btnText = connectButton.GetComponentInChildren<Text>();
            if (btnText)
            {
                btnText.text = client.IsConnected() ? "Disconnect" : "Connect";
            }
            connectButton.interactable = true;
        }

        if (findMatchButton)
        {
            var btnText = findMatchButton.GetComponentInChildren<Text>();
            if (btnText)
            {
                btnText.text = client.IsInQueue() ? "Leave Queue" : "Find Match";
            }
            findMatchButton.interactable = client.IsConnected() && !client.IsInGame();
        }

        if (rollDiceButton)
        {
            rollDiceButton.interactable = client.IsInGame() && client.IsMyTurn();
            var btnText = rollDiceButton.GetComponentInChildren<Text>();
            if (btnText)
            {
                btnText.text = client.IsMyTurn() ? "Roll Dice (Your Turn)" : "Roll Dice";
            }
        }

        // Update token buttons
        var gameState = client.GetCurrentGameState();
        for (int i = 0; i < tokenButtons.Length; i++)
        {
            if (tokenButtons[i] != null)
            {
                // Only enable tokens belonging to current player
                int tokenPlayer = i / 4;
                bool isMyToken = tokenPlayer == client.GetPlayerIndex();
                bool canMove = client.IsInGame() && client.IsMyTurn() && isMyToken;
                
                tokenButtons[i].interactable = canMove;

                // Update token position display
                var btnText = tokenButtons[i].GetComponentInChildren<Text>();
                if (btnText && gameState != null && i < gameState.tokenPositions.Length)
                {
                    int pos = gameState.tokenPositions[i];
                    string posText = pos == -1 ? "Base" : pos == 57 ? "Home" : pos.ToString();
                    btnText.text = $"T{i}\n{posText}";
                }
            }
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText)
        {
            statusText.text = message;
        }
        Debug.Log($"[Status] {message}");
    }

    private void UpdateGameInfo(string info)
    {
        if (gameInfoText)
        {
            gameInfoText.text = info;
        }
    }

    private string GetGameStateInfo()
    {
        var state = client.GetCurrentGameState();
        if (state == null) return "No game";

        return $"Turn: {state.turnCount}\n" +
               $"Current Player: {state.currentPlayer}\n" +
               $"Your Index: {client.GetPlayerIndex()}\n" +
               $"Players: {state.playerCount}";
    }
}
