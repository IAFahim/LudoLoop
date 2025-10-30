using System;
using System.Collections.Generic;
using EasyButtons;
using Spawner.Spawner.Authoring;
using UnityEngine;
using UnityEngine.Events;
using Network.Runtime;
using TMPro;

namespace Ludo
{
    /// <summary>
    /// Network-enabled Ludo game setup that integrates with LudoNetworkManager
    /// </summary>
    public class LudoNetworkGameSetup : MonoBehaviour
    {
        [Header("Game Setup")] public LudoGamePlay ludoGamePlay;
        public LudoNetworkManager networkManager;

        [Header("Player Controllers")] public List<LudoPlayerController> players = new();

        [Header("Game Mode")] public bool isOnlineMode = false;
        public bool isLocalMode = true;

        [Header("Game Events")] public UnityEvent onGameStarted;
        public UnityEvent onMatchFound;
        public UnityEvent onGameOver;

        [Header("Online Game State")] public int myPlayerIndex = -1;
        public string myPlayerId;
        public List<byte> validMoves;
        private int lastDiceRoll;

        [Header("Local Game State")] public Delay delay;
        public bool active;
        public byte playerPickedToken;
        public bool playerPicked;

        public GameSession GameSession => ludoGamePlay.gameSession;

        public TMP_Text Text;
        private void Awake()
        {
            LudoNetworkManager.Text = Text;
        }

        private void Start()
        {
            if (isOnlineMode && networkManager != null)
            {
                SetupNetworkCallbacks();
            }

            ConnectToServer();
        }

        #region Network Setup

        private void SetupNetworkCallbacks()
        {
            networkManager.onConnected.AddListener(OnNetworkConnected);
            networkManager.onMatchFound.AddListener(OnMatchFound);
            networkManager.onDiceRolled.AddListener(OnDiceRolled);
            networkManager.onTokenMoved.AddListener(OnTokenMoved);
            networkManager.onGameOver.AddListener(OnNetworkGameOver);
            networkManager.onError.AddListener(OnNetworkError);
        }

        [Button("Connect to Server")]
        public void ConnectToServer()
        {
            if (networkManager != null)
            {
                isOnlineMode = true;
                isLocalMode = false;
                networkManager.Connect();
            }
        }

        [Button("Join 2 Player Queue")]
        public void JoinQueue2Players()
        {
            JoinQueue(2);
        }

        [Button("Join 4 Player Queue")]
        public void JoinQueue4Players()
        {
            JoinQueue(4);
        }

        public void JoinQueue(int playerCount, string queueType = "casual", string playerName = null)
        {
            if (networkManager != null && networkManager.IsConnected)
            {
                string name = playerName ?? $"Player_{UnityEngine.Random.Range(1000, 9999)}";
                myPlayerId = name;
                networkManager.JoinQueue(queueType, playerCount, name);
                Debug.Log($"<color=cyan>Joining queue for {playerCount} players...</color>");
            }
            else
            {
                Debug.LogError("Not connected to server!");
            }
        }

        #endregion

        #region Network Callbacks

        private void OnNetworkConnected()
        {
            Debug.Log("<color=green>Connected to server!</color>");
            myPlayerId = networkManager.PlayerId;
            JoinQueue2Players();
        }

        private void OnMatchFound(MatchFoundResponse response)
        {
            Debug.Log($"<color=green>Match found with {response.playerCount} players!</color>");

            // Find my player index
            foreach (var player in response.players)
            {
                if (player.name == myPlayerId)
                {
                    myPlayerIndex = player.playerIndex;
                    Debug.Log($"<color=yellow>I am player {myPlayerIndex}</color>");
                    break;
                }
            }

            // Setup game with network players
            SetupNetworkGame(response);

            // Sync initial game state
            SyncGameStateFromServer(response.gameState);

            onMatchFound?.Invoke();

            // Check if it's my turn
            if (response.gameState.currentPlayer == myPlayerIndex)
            {
                Debug.Log("<color=yellow>It's my turn! Ready to roll dice.</color>");
            }
        }

        private void OnDiceRolled(DiceRolledResponse response)
        {
            Debug.Log($"<color=cyan>Player {response.playerIndex} rolled {response.diceValue}</color>");

            lastDiceRoll = response.diceValue;


            // Visual dice animation could go here
            // ShowDiceRoll(response.diceValue);

            if (response.playerIndex == myPlayerIndex)
            {
                var bytes = new List<byte>(response.validMoves.Length);
                foreach (var moves in response.validMoves)
                {
                    bytes.Add((byte)moves);
                }

                validMoves = bytes;

                if (response.noValidMoves)
                {
                    Debug.Log("<color=red>No valid moves! Turn will be skipped.</color>");
                    // Auto-skip turn after delay
                    Invoke(nameof(EndMyTurn), 2f);
                    return;
                }

                Debug.Log($"<color=green>Valid moves: {string.Join(", ", validMoves)}</color>");

                // Get the current player controller
                var playerController = players[myPlayerIndex];

                if (playerController.playerType == PlayerType.Human ||
                    playerController.playerType == PlayerType.LocalHuman)
                {
                    // Wait for human input
                    HighlightValidTokens(validMoves);
                }
                else
                {
                    // AI automatically chooses
                    playerController.ChooseTokenFrom(validMoves, (byte)response.diceValue);
                }


                foreach (var validMove in validMoves)
                {
                    MoveTokenNetwork(validMove);
                }
            }
        }

        private void OnTokenMoved(TokenMovedResponse response)
        {
            Debug.Log($"<color=magenta>Player {response.playerIndex} moved token {response.tokenIndex}</color>");
            Debug.Log($"Move result: {response.moveResult}");

            // Sync game state
            SyncGameStateFromServer(response.gameState);

            // Move the token visually
            int tokenOrdinal = response.tokenIndex % LudoBoard.Tokens;
            byte newLogicalPosition = (byte)response.newPosition;

            // Get visual position and move token smoothly
            Vector3 targetPosition = GetPositionFromLogical(response.tokenIndex, newLogicalPosition);
            ludoGamePlay.MoveTokenToPosition(response.playerIndex, tokenOrdinal, targetPosition);

            // Check if game continues or winner
            if (response.hasWon)
            {
                Debug.Log($"<color=gold>Player {response.playerIndex} has won!</color>");
            }
            else if (response.nextPlayer == myPlayerIndex)
            {
                Debug.Log("<color=yellow>It's my turn! Ready to roll dice.</color>");
            }
            else
            {
            }
        }

        private void OnNetworkGameOver(GameOverResponse response)
        {
            Debug.Log($"<color=gold>GAME OVER! Winner: {response.winnerName} (Player {response.winnerIndex})</color>");

            if (response.winnerIndex == myPlayerIndex)
            {
                Debug.Log("<color=gold>🎉 YOU WON! 🎉</color>");
            }

            onGameOver?.Invoke();
        }

        private void OnNetworkError(string error)
        {
            Debug.LogWarning($"<color=red>Network error: {error}</color>");
        }

        #endregion

        #region Network Game Actions

        [Button("Roll Dice (Network)")]
        public void RollDiceNetwork()
        {
            networkManager.RollDice();
        }

        public void MoveTokenNetwork(int tokenIndex)
        {
            networkManager.MoveToken(tokenIndex);
        }

        public void LeaveNetworkGame()
        {
            if (networkManager != null)
            {
                networkManager.LeaveGame();
                isOnlineMode = false;
                ResetGame();
            }
        }

        #endregion

        #region Network Game Helpers

        private void SetupNetworkGame(MatchFoundResponse response)
        {
            // Clear existing players
            foreach (var player in players)
            {
                if (player != null && player.gameObject != this.gameObject)
                {
                    Destroy(player.gameObject);
                }
            }

            players.Clear();

            // Create player types based on network info
            PlayerType[] playerTypes = new PlayerType[response.playerCount];

            for (int i = 0; i < response.playerCount; i++)
            {
                // My player is human, others are network players
                if (i == myPlayerIndex)
                {
                    playerTypes[i] = PlayerType.LocalHuman;
                }
                else
                {
                    playerTypes[i] = PlayerType.NetworkPlayer;
                }
            }

            // Setup the game
            SetupGame(response.playerCount, playerTypes);
        }

        private void SyncGameStateFromServer(GameState serverState)
        {
            if (serverState == null) return;
            for (var i = 0; i < serverState.tokenPositions.Length; i++)
            {
                var serverStateTokenPosition = serverState.tokenPositions[i];
                var stateTokenPosition = (byte)(serverStateTokenPosition + 1);
                ludoGamePlay.gameSession.board.TokenPositions[i] = stateTokenPosition;
            }

            ludoGamePlay.RefreshState();

            // Sync current player
            // Note: Server manages turn order
        }

        private Vector3 GetPositionFromLogical(int tokenIndex, byte logicalPosition)
        {
            int playerIndex = tokenIndex / LudoBoard.Tokens;
            int tokenOrdinal = tokenIndex % LudoBoard.Tokens;

            if (GameSession.board.IsAtBase(tokenIndex))
            {
                return ludoGamePlay.GetBasePosition(playerIndex, tokenOrdinal);
            }
            else if (GameSession.board.IsHome(tokenIndex))
            {
                return ludoGamePlay.GetHomePosition(playerIndex, tokenOrdinal);
            }
            else if (GameSession.board.IsOnHomeStretch(tokenIndex))
            {
                int step = logicalPosition - LudoBoard.HomeStart;
                return ludoGamePlay.GetHomeStretchPosition(playerIndex, step);
            }
            else if (logicalPosition is >= 1 and <= 52)
            {
                int abs = GameSession.board.GetAbsolutePosition(tokenIndex);
                return ludoGamePlay.GetAbsoluteBoardPosition(abs);
            }

            return Vector3.zero;
        }

        private void HighlightValidTokens(List<byte> tokenIndices)
        {
            // Highlight only tokens that can move
            foreach (int tokenIndex in tokenIndices)
            {
                int playerIndex = tokenIndex / LudoBoard.Tokens;
                int tokenOrdinal = tokenIndex % LudoBoard.Tokens;
                ludoGamePlay.HighlightToken(playerIndex, tokenOrdinal, true);
            }
        }

        private void EndMyTurn()
        {
        }

        #endregion

        #region Local Game (Original Implementation)

        [Button("Start Local Game")]
        public void StartLocalGame()
        {
            isLocalMode = true;
            isOnlineMode = false;
            active = false;

            ludoGamePlay.gameSession.ReSetup();

            if (players.Count != ludoGamePlay.gameSession.board.PlayerCount)
            {
                Debug.LogWarning(
                    $"Player count mismatch! Board: {ludoGamePlay.gameSession.board.PlayerCount}, Players: {players.Count}");
            }

            for (int i = 0; i < players.Count; i++)
            {
                players[i].playerIndex = i;
                players[i].ludoGamePlay = ludoGamePlay;
            }

            onGameStarted?.Invoke();
        }

        public void ActiveGamePlay()
        {
            active = true;
            Setup4Players_1Human3AI();
        }

        [Button]
        public void HumanInteraction(int choose)
        {
            if (isLocalMode)
            {
                playerPickedToken = (byte)choose;
                playerPicked = true;
            }
            else if (isOnlineMode)
            {
                // Use network move
                foreach (var validMove in validMoves)
                {
                    MoveTokenNetwork(validMove);
                }
            }
        }

        private void Update()
        {
            // Only run local game loop if in local mode
            if (!isLocalMode || !active) return;

            if (!delay.UpdateAndReset(Time.deltaTime, 1)) return;

            ludoGamePlay.RefreshState();
            var playerIndex = GameSession.currentPlayerIndex;
            var ludoPlayerController = players[playerIndex];
            var isHuman = ludoPlayerController.playerType == PlayerType.Human;

            if (isHuman)
            {
                Debug.Log("Player Waiting");
                if (!playerPicked) return;
            }

            byte diceRoll = (byte)UnityEngine.Random.Range(1, 7);
            var movableTokens = GameSession.GetMovableTokens(playerIndex, diceRoll);

            if (isHuman && playerPicked)
            {
                ludoPlayerController.ChooseTokenFrom(movableTokens, diceRoll);
                playerPicked = false;
            }
            else if (!isHuman)
            {
                ludoPlayerController.ChooseTokenFrom(movableTokens, diceRoll);
            }

            Debug.Log($"<color=magenta>{playerIndex} chose token {diceRoll}</color>");
        }

        #endregion

        #region Setup Methods

        [Button("Setup 2 Players (Human vs AI)")]
        public void Setup2Players_HumanVsAI()
        {
            SetupGame(2, new[] { PlayerType.Human, PlayerType.AI });
        }

        [Button("Setup 2 Players (Both Human)")]
        public void Setup2Players_BothHuman()
        {
            SetupGame(2, new[] { PlayerType.Human, PlayerType.Human });
        }

        [Button("Setup 4 Players (1 Human, 3 AI)")]
        public void Setup4Players_1Human3AI()
        {
            SetupGame(4, new[] { PlayerType.Human, PlayerType.AI, PlayerType.AI, PlayerType.AI });
        }

        [Button("Setup 4 Players (All Human)")]
        public void Setup4Players_AllHuman()
        {
            SetupGame(4, new[] { PlayerType.Human, PlayerType.Human, PlayerType.Human, PlayerType.Human });
        }

        [Button("Setup 4 Players (All AI)")]
        public void Setup4Players_AllAI()
        {
            SetupGame(4, new[] { PlayerType.AI, PlayerType.AI, PlayerType.AI, PlayerType.AI });
        }

        private void SetupGame(int playerCount, PlayerType[] playerTypes)
        {
            ludoGamePlay.playerSpawner.CreateBaseForPlayerCount(GameSession, playerCount, playerTypes);

            // Clear existing players
            foreach (var player in players)
            {
                if (player != null && player.gameObject != this.gameObject)
                {
                    Destroy(player.gameObject);
                }
            }

            players.Clear();

            // Create player controllers
            for (int i = 0; i < playerCount; i++)
            {
                GameObject playerObj = new GameObject($"Player_{i}_{playerTypes[i]}");
                playerObj.transform.SetParent(transform);

                LudoPlayerController controller = null;

                switch (playerTypes[i])
                {
                    case PlayerType.Human:
                    case PlayerType.LocalHuman:
                        var humanPlayer = playerObj.AddComponent<LudoHumanPlayer>();
                        humanPlayer.playerName = $"Player {i + 1}";
                        controller = humanPlayer;
                        break;

                    case PlayerType.AI:
                        var aiPlayer = playerObj.AddComponent<LudoAIPlayer>();
                        aiPlayer.playerName = $"AI {i + 1}";
                        controller = aiPlayer;
                        break;

                    case PlayerType.NetworkPlayer:
                        var networkPlayer = playerObj.AddComponent<LudoNetworkPlayer>();
                        networkPlayer.playerName = $"Network Player {i + 1}";
                        controller = networkPlayer;
                        break;
                }

                if (controller != null)
                {
                    controller.playerIndex = i;
                    controller.playerType = playerTypes[i];
                    controller.ludoGamePlay = ludoGamePlay;
                    players.Add(controller);
                }
            }

            Debug.Log($"<color=cyan>Setup complete: {playerCount} players</color>");
        }

        private void ResetGame()
        {
            active = false;
            myPlayerIndex = -1;
            validMoves = null;

            foreach (var player in players)
            {
                if (player != null && player.gameObject != this.gameObject)
                {
                    Destroy(player.gameObject);
                }
            }

            players.Clear();
        }

        #endregion
    }
}