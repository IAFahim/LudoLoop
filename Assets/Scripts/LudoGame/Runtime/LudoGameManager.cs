using System.Collections.Generic;
using EasyButtons;
using UnityEngine;
using UnityEngine.Events;

namespace Ludo
{
    /// <summary>
    /// Main game manager - coordinates all players and game flow
    /// </summary>
    public class LudoGameManager : MonoBehaviour
    {
        [Header("Game Setup")]
        public LudoGamePlay ludoGamePlay;
        public GameSession gameSession;
        
        [Header("Player Controllers")]
        public List<LudoPlayerController> players = new List<LudoPlayerController>();
        
        [Header("Game Events")]
        public UnityEvent<int> onGameStarted; // Player count
        public UnityEvent<int> onTurnChanged; // Current player index
        public UnityEvent<int, string> onGameEnded; // Winner index, winner name
        
        [Header("Auto Start")]
        public bool autoStartOnAwake = false;

        private void Awake()
        {
            if (autoStartOnAwake)
            {
                StartGame();
            }
        }

        private void Update()
        {
            // Check if it's time for current player to act
            if (gameSession == null || gameSession.isGameOver) return;
            
            int currentPlayerIndex = gameSession.currentPlayerIndex;
            
            if (currentPlayerIndex >= 0 && currentPlayerIndex < players.Count)
            {
                var currentPlayer = players[currentPlayerIndex];
                if (currentPlayer != null && currentPlayer.IsMyTurn)
                {
                    // Trigger turn start (only once per turn)
                    // Note: You might want to add a flag to prevent multiple calls
                }
            }
        }

        [Button("Start Game")]
        public void StartGame()
        {
            if (gameSession == null)
            {
                Debug.LogError("GameSession is not assigned!");
                return;
            }
            
            if (players.Count == 0)
            {
                Debug.LogError("No players assigned!");
                return;
            }
            
            // Reset game session
            gameSession.ReSetup();
            
            // Validate player count matches board
            if (players.Count != gameSession.board.PlayerCount)
            {
                Debug.LogWarning($"Player count mismatch! Board: {gameSession.board.PlayerCount}, Players: {players.Count}");
            }
            
            // Assign player indices
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    players[i].playerIndex = i;
                    players[i].ludoGamePlay = ludoGamePlay;
                }
            }
            
            Debug.Log($"<color=green>════════════════════════════════</color>");
            Debug.Log($"<color=green>Game Started with {players.Count} players!</color>");
            Debug.Log($"<color=green>════════════════════════════════</color>");
            
            onGameStarted?.Invoke(players.Count);
            
            // Start first player's turn
            StartPlayerTurn(0);
        }

        private void StartPlayerTurn(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= players.Count) return;
            
            gameSession.currentPlayerIndex = (byte)playerIndex;
            onTurnChanged?.Invoke(playerIndex);
            
            var player = players[playerIndex];
            if (player != null)
            {
                player.OnTurnStart();
            }
        }

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
            // Clear existing players
            foreach (var player in players)
            {
                if (player != null && player.gameObject != this.gameObject)
                {
                    Destroy(player.gameObject);
                }
            }
            players.Clear();
            
            // Create game session with correct player count
            if (gameSession != null)
            {
                gameSession.board = new LudoBoard(playerCount);
            }
            
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

        [Button("Next Turn (Manual)")]
        public void NextTurn()
        {
            if (gameSession == null || gameSession.isGameOver) return;
            
            gameSession.EndTurn();
            StartPlayerTurn(gameSession.currentPlayerIndex);
        }

        public void OnPlayerWon(int winnerIndex)
        {
            if (winnerIndex >= 0 && winnerIndex < players.Count)
            {
                string winnerName = players[winnerIndex].playerName;
                onGameEnded?.Invoke(winnerIndex, winnerName);
            }
        }
    }
}