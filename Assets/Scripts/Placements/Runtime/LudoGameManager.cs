// LudoGameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LudoGame.Runtime;
using Network.Runtime.Network.Runtime; // Your game logic namespace
using Placements.Runtime; // Your Tiles namespace
using TMPro; // Use TextMeshPro for UI text

public class LudoGameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private LudoClient ludoClient;
    [SerializeField] private Tiles tiles;
    [SerializeField] private GameObject tokenPrefab;

    [Header("UI References")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private TextMeshProUGUI turnInfoText;
    [SerializeField] private TextMeshProUGUI diceRollText;
    [SerializeField] private TextMeshProUGUI winnerText;

    [Header("Game Setup")]
    [SerializeField] private Transform[] playerBaseParents; // Array of 4 empty GameObjects where tokens start

    // --- State ---
    private LudoGameState currentGameState;
    private List<TokenController> spawnedTokens = new List<TokenController>();
    private int[] validMoves = new int[0];

    #region Unity Lifecycle

    private void Start()
    {
        // Subscribe to client events
        ludoClient.OnMatchFound += HandleMatchFound;
        ludoClient.OnDiceRolled += HandleDiceRolled;
        ludoClient.OnTokenMoved += HandleTokenMoved;
        ludoClient.OnGameOver += HandleGameOver;
        ludoClient.OnDisconnected += HandleDisconnect;

        // Subscribe to UI button clicks
        findMatchButton.onClick.AddListener(OnFindMatchClicked);
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);

        // Initial UI state
        ResetUI();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (ludoClient != null)
        {
            ludoClient.OnMatchFound -= HandleMatchFound;
            ludoClient.OnDiceRolled -= HandleDiceRolled;
            ludoClient.OnTokenMoved -= HandleTokenMoved;
            ludoClient.OnGameOver -= HandleGameOver;
            ludoClient.OnDisconnected -= HandleDisconnect;
        }
    }

    #endregion

    #region UI Event Handlers

    private void OnFindMatchClicked()
    {
        // Connect if not already connected, then find a match
        if (!ludoClient.IsConnected())
        {
            ludoClient.Connect();
        }
        ludoClient.FindMatch("casual", 4);
        findMatchButton.gameObject.SetActive(false);
        turnInfoText.text = "Finding match...";
    }

    private void OnRollDiceClicked()
    {
        if (IsMyTurn())
        {
            ludoClient.RollDice();
            rollDiceButton.interactable = false; // Prevent double clicks
        }
    }

    private void OnTokenSelected(int tokenIndex)
    {
        if (!IsMyTurn()) return;

        // Check if the selected token is one of the valid moves
        bool isValid = false;
        foreach (int validTokenIndex in validMoves)
        {
            if (validTokenIndex == tokenIndex)
            {
                isValid = true;
                break;
            }
        }

        if (isValid)
        {
            ludoClient.MoveToken(tokenIndex);
            ClearHighlights();
            validMoves = new int[0]; // Clear valid moves after selection
        }
        else
        {
            Debug.Log($"Token {tokenIndex} is not a valid move.");
        }
    }

    #endregion

    #region LudoClient Event Handlers

    private void HandleMatchFound(MatchData data)
    {
        Debug.Log("Match found! Setting up board.");
        winnerText.gameObject.SetActive(false);

        // Convert server's GameStateData to our LudoGameState struct
        currentGameState = ConvertToLudoGameState(data.gameState);
        
        ClearBoard();
        SpawnTokens();
        UpdateAllTokenPositions();
        UpdateTurnUI();
    }
    
    private void HandleDiceRolled(DiceRollData data)
    {
        diceRollText.text = $"Rolled: {data.diceValue}";
        validMoves = data.validMoves;

        // If it's our turn and we have moves, highlight them
        if (data.playerIndex == ludoClient.GetPlayerIndex() && !data.noValidMoves)
        {
            HighlightValidMoves();
        }
        // The server will automatically advance the turn if there are no valid moves.
    }

    private void HandleTokenMoved(TokenMoveData data)
    {
        // The server sends the new, authoritative game state.
        currentGameState = ConvertToLudoGameState(data.gameState);

        // Visually update the board
        UpdateAllTokenPositions();
        
        // Update whose turn it is now
        UpdateTurnUI();
    }

    private void HandleGameOver(GameOverData data)
    {
        winnerText.text = $"{data.winnerName} has won the game!";
        winnerText.gameObject.SetActive(true);
        rollDiceButton.gameObject.SetActive(false);
        turnInfoText.text = "Game Over!";
    }

    private void HandleDisconnect()
    {
        ResetUI();
        ClearBoard();
        turnInfoText.text = "Disconnected. Find a new match?";
    }

    #endregion

    #region Game Logic & Visuals

    private void SpawnTokens()
    {
        for (int i = 0; i < currentGameState.PlayerCount * 4; i++)
        {
            int playerIndex = i / 4;
            GameObject tokenGO = Instantiate(tokenPrefab, playerBaseParents[playerIndex]);
            tokenGO.name = $"Token_{i}";

            TokenController controller = tokenGO.GetComponent<TokenController>();
            controller.TokenIndex = i;
            controller.OnTokenClicked += OnTokenSelected;
            
            spawnedTokens.Add(controller);
            // You might want to set the token's color here
        }
    }
    
    private void UpdateAllTokenPositions()
    {
        for (int i = 0; i < spawnedTokens.Count; i++)
        {
            sbyte boardPos = currentGameState.TokenPositions[i];
            int playerIndex = i / 4;

            Vector3 targetPosition;
            if (boardPos == LudoBoard.PosBase)
            {
                // Place it back in its designated home base slot
                targetPosition = playerBaseParents[playerIndex].position;
            }
            else
            {
                // Get the world position from the Tiles script
                targetPosition = tiles.GetTileBoardPosition(boardPos, playerIndex);
            }
            
            // For simplicity, we teleport. You could use a Coroutine for smooth movement.
            spawnedTokens[i].transform.position = targetPosition;
        }
    }

    private void UpdateTurnUI()
    {
        int currentPlayer = currentGameState.CurrentPlayer;
        if (currentPlayer == ludoClient.GetPlayerIndex())
        {
            turnInfoText.text = "Your Turn!";
            rollDiceButton.gameObject.SetActive(true);
            rollDiceButton.interactable = true;
        }
        else
        {
            turnInfoText.text = $"Player {currentPlayer}'s Turn";
            rollDiceButton.gameObject.SetActive(false);
        }
        diceRollText.text = ""; // Clear dice roll text at start of new turn
    }

    private void HighlightValidMoves()
    {
        ClearHighlights();
        foreach (int tokenIndex in validMoves)
        {
            if (tokenIndex >= 0 && tokenIndex < spawnedTokens.Count)
            {
                spawnedTokens[tokenIndex].SetHighlight(true);
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var token in spawnedTokens)
        {
            token.SetHighlight(false);
        }
    }

    private void ClearBoard()
    {
        foreach (var token in spawnedTokens)
        {
            Destroy(token.gameObject);
        }
        spawnedTokens.Clear();
    }

    private void ResetUI()
    {
        findMatchButton.gameObject.SetActive(true);
        rollDiceButton.gameObject.SetActive(false);
        winnerText.gameObject.SetActive(false);
        turnInfoText.text = "Welcome to Ludo!";
        diceRollText.text = "";
    }

    #endregion
    
    #region Utility
    
    private LudoGameState ConvertToLudoGameState(GameStateData serverState)
    {
        // Convert int[] to sbyte[]
        sbyte[] tokenPositionsSbyte = new sbyte[serverState.tokenPositions.Length];
        for(int i = 0; i < serverState.tokenPositions.Length; i++)
        {
            tokenPositionsSbyte[i] = (sbyte)serverState.tokenPositions[i];
        }

        return new LudoGameState
        {
            TurnCount = (ushort)serverState.turnCount,
            DiceValue = serverState.diceValue,
            ConsecutiveSixes = serverState.consecutiveSixes,
            CurrentPlayer = serverState.currentPlayer,
            PlayerCount = serverState.playerCount,
            TokenPositions = tokenPositionsSbyte,
            Seed = 0 // Seed is not synchronized in this example, managed by server
        };
    }

    private bool IsMyTurn()
    {
        return ludoClient.IsConnected() && currentGameState.CurrentPlayer == ludoClient.GetPlayerIndex();
    }

    #endregion
}