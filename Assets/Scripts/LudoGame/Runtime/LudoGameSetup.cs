using System;
using System.Collections.Generic;
using EasyButtons;
using Spawner.Spawner.Authoring;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Ludo
{
    public class LudoGameSetup : MonoBehaviour
    {
        [Header("Game Setup")] public LudoGamePlay ludoGamePlay;

        [Header("Player Controllers")] public List<LudoPlayerController> players = new();

        [Header("Game Events")] public UnityEvent onGameStarted;


        public GameSession GameSession => ludoGamePlay.gameSession;

        [Button("Start Game")]
        public void StartGame()
        {
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

        public Delay delay;
        public bool active;
        public byte playerPickedToken;
        public bool playerPicked;

        public void ActiveGamePlay()
        {
            active = true;
            Setup4Players_1Human3AI();
        }
        

        [Button]
        public void HumanInteraction(int choose)
        {
            playerPickedToken = (byte)choose;
            playerPicked = true;
        }

        private void Update()
        {
            if (!active) return;
            if (!delay.UpdateAndReset(Time.deltaTime, 1)) return;
            ludoGamePlay.RefreshState();
            var playerIndex = GameSession.currentPlayerIndex;
            var ludoPlayerController = players[playerIndex];
            var isHuman = ludoPlayerController.playerType == PlayerType.Human;
    
            if (isHuman)
            {
                Debug.Log("Player Waiting");
                if (!playerPicked) return;  // Wait for human input
            }

            byte diceRoll = (byte)Random.Range(1, 7);
            var movableTokens = GameSession.GetMovableTokens(playerIndex, diceRoll);
    
            if (isHuman && playerPicked)
            {
                ludoPlayerController.ChooseTokenFrom(movableTokens, diceRoll);
                playerPicked = false;  // Reset for next turn
            }
            else if (!isHuman)
            {
                ludoPlayerController.ChooseTokenFrom(movableTokens, diceRoll);
            }

            Debug.Log($"<color=magenta>{playerIndex} chose token {diceRoll}</color>");
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
    }
}