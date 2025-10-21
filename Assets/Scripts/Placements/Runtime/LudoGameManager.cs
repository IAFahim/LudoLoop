using System.Collections.Generic;
using LudoGame.Runtime;
using Network.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Placements.Runtime
{
    public class LudoGameManager : MonoBehaviour
    {
        [Header("Scene References")]
        // NOTE: You will drag this from the scene into the event slots in the Inspector
        [SerializeField] private LudoClient ludoClient; 
        [SerializeField] private string gameType="casual";
        [SerializeField] private int playerCount = 4;
        [SerializeField] private Tiles tiles;

        [Header("UI References")]
        [SerializeField] private Button findMatchButton;
        [SerializeField] private Button rollDiceButton;
        [SerializeField] private TextMeshProUGUI turnInfoText;
        [SerializeField] private TextMeshProUGUI diceRollText;
        [SerializeField] private TextMeshProUGUI winnerText;

        [Header("Game Setup")]
        [SerializeField] private TokenBase[] tokenBases; // Array of 4 empty GameObjects where tokens Rest
        
        // --- State ---
        private LudoGameState currentGameState;
        private List<Token> spawnedTokens = new List<Token>();
        private int[] validMoves = new int[0];

        #region Unity Lifecycle

        private void Start()
        {
            // Subscribe to UI button clicks (this is still best done in code)
            findMatchButton.onClick.AddListener(OnFindMatchClicked);
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);

            // Initial UI state
            ResetUI();
        }

        // NOTE: OnEnable, OnDisable, and OnDestroy have been removed because
        // event connections will now be handled in the Unity Inspector.

        #endregion

        #region UI Event Handlers

        private void OnFindMatchClicked()
        {
            // Connect if not already connected, then find a match
            if (!ludoClient.IsConnected())
            {
                ludoClient.Connect();
            }
            ludoClient.FindMatch(gameType, playerCount);
            findMatchButton.gameObject.SetActive(false);
            turnInfoText.text = "Finding match...";
        }

        private void OnRollDiceClicked()
        {
            if (IsMyTurn())
            {
                ludoClient.RollDice();
                // Prevent spamming the roll button
                rollDiceButton.interactable = false; 
            }
        }

        private void OnTokenSelected(int tokenIndex)
        {
            if (!IsMyTurn()) return;

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

        #region LudoClient Event Handlers (MUST BE PUBLIC)

        // These methods are now public so they can be assigned in the Inspector.
    
        public void HandleMatchFound(MatchData data)
        {
            Debug.Log("Match found! Setting up board.");
            winnerText.gameObject.SetActive(false);

            currentGameState = ConvertToLudoGameState(data.gameState);
        
            ClearBoard();
            SpawnTokens();
            UpdateAllTokenPositions();
            UpdateTurnUI();
        }
    
        public void HandleDiceRolled(DiceRollData data)
        {
            diceRollText.text = $"Rolled: {data.diceValue}";
            validMoves = data.validMoves;

            if (data.playerIndex == ludoClient.GetPlayerIndex() && !data.noValidMoves)
            {
                HighlightValidMoves();
            }
        }

        public void HandleTokenMoved(TokenMoveData data)
        {
            currentGameState = ConvertToLudoGameState(data.gameState);
            UpdateAllTokenPositions();
            UpdateTurnUI();
        }

        public void HandleGameOver(GameOverData data)
        {
            winnerText.text = $"{data.winnerName} has won the game!";
            winnerText.gameObject.SetActive(true);
            rollDiceButton.gameObject.SetActive(false);
            turnInfoText.text = "Game Over!";
        }

        public void HandleDisconnect()
        {
            ResetUI();
            ClearBoard();
            turnInfoText.text = "Disconnected. Find a new match?";
        }

        #endregion

        #region Game Logic & Visuals

        private void SpawnTokens()
        {
            for (int i = 0; i < currentGameState.PlayerCount; i++)
            {
                int playerIndex = i / 4;
                var tokens = tokenBases[i].Place(i);
                for (int t = 0; t < 4; t++)
                {
                    var token = tokens[i];
                    token.onTokenClicked.AddListener(OnTokenSelected);
                }
                spawnedTokens.AddRange(tokens);
            }
        }
    
        private void UpdateAllTokenPositions()
        {
            for (int i = 0; i < spawnedTokens.Count; i++)
            {
                sbyte boardPos = currentGameState.TokenPositions[i];

                int playerIndex = i / 4;
                if (boardPos == LudoBoard.PosBase)
                {
                    int playerTokenIndex = i % 4;
                    tokenBases[playerIndex].MoveTokenToBase(playerTokenIndex);
                }
                else
                {
                    var targetPosition = tiles.GetTileBoardPosition(boardPos, playerIndex);
                    spawnedTokens[i].transform.position = targetPosition;
                }
            }
        }

        private void UpdateTurnUI()
        {
            int currentPlayer = currentGameState.CurrentPlayer;
            if (currentPlayer == ludoClient.GetPlayerIndex())
            {
                turnInfoText.text = "Your Turn!";
                rollDiceButton.gameObject.SetActive(true);
                rollDiceButton.interactable = true; // Re-enable the button
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
                Seed = 0 
            };
        }

        private bool IsMyTurn()
        {
            return ludoClient.IsConnected() && currentGameState.CurrentPlayer == ludoClient.GetPlayerIndex();
        }

        #endregion
    }
}