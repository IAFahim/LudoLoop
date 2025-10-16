using UnityEngine;
using UnityEditor;
using LudoGame.Runtime; // Your namespace

[CustomEditor(typeof(LudoGameManager))]
public class LudoGameManagerEditor : Editor
{
    // Values stored by the editor GUI
    private int playerCount = 4;
    private int specificDiceRoll = 1;
    private int tokenIndexToMove = 0;

    public override void OnInspectorGUI()
    {
        // Get a reference to the script we are inspecting
        LudoGameManager manager = (LudoGameManager)target;

        EditorGUILayout.LabelField("Ludo Game Controller", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- SECTION 1: Game Setup ---
        EditorGUILayout.LabelField("Game Setup", EditorStyles.centeredGreyMiniLabel);
        playerCount = EditorGUILayout.IntSlider("Player Count", playerCount, 2, 4);
        if (GUILayout.Button("Start New Game"))
        {
            manager.StartNewGame(playerCount);
        }

        EditorGUILayout.Space(20);

        // --- SECTION 2: Game Controls (only show if game is running) ---
        if (manager.currentPhase != LudoGameManager.GamePhase.PreGame && manager.currentPhase != LudoGameManager.GamePhase.GameOver)
        {
            EditorGUILayout.LabelField("Game Controls", EditorStyles.centeredGreyMiniLabel);

            // --- Dice Rolling ---
            GUI.enabled = (manager.currentPhase == LudoGameManager.GamePhase.WaitingForRoll);
            if (GUILayout.Button("Roll Dice (Random)"))
            {
                manager.RollDice();
            }
            specificDiceRoll = EditorGUILayout.IntSlider("Specific Roll", specificDiceRoll, 1, 6);
            if (GUILayout.Button("Roll Specific Value"))
            {
                manager.RollDice(specificDiceRoll);
            }
            GUI.enabled = true; // Re-enable GUI for next elements
            
            EditorGUILayout.Space();

            // --- Token Moving ---
            GUI.enabled = (manager.currentPhase == LudoGameManager.GamePhase.WaitingForMove);
            tokenIndexToMove = EditorGUILayout.IntField("Token Index to Move", tokenIndexToMove);
            if (GUILayout.Button("Move Selected Token"))
            {
                manager.AttemptMove(tokenIndexToMove);
            }
            GUI.enabled = true; // Re-enable GUI
        }
        
        EditorGUILayout.Space(20);
        
        // --- SECTION 3: Game State Display ---
        EditorGUILayout.LabelField("Current Game State", EditorStyles.boldLabel);
        if (manager.currentPhase == LudoGameManager.GamePhase.PreGame)
        {
            EditorGUILayout.HelpBox("No game in progress. Click 'Start New Game' to begin.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Game Phase:", manager.currentPhase.ToString());
            EditorGUILayout.LabelField("Current Player:", manager.gameState.CurrentPlayer.ToString());

            if (manager.currentPhase == LudoGameManager.GamePhase.WaitingForMove)
            {
                 EditorGUILayout.HelpBox($"Player {manager.gameState.CurrentPlayer} rolled a {manager.lastDiceRoll}. " +
                                         $"Valid moves are for tokens: [{string.Join(", ", manager.validMoves)}]", MessageType.Info);
            }
            
            if (manager.currentPhase == LudoGameManager.GamePhase.GameOver)
            {
                 EditorGUILayout.HelpBox($"Game Over! Player {manager.gameState.CurrentPlayer} won!", MessageType.Warning);
            }

            // Display token positions
            for (int player = 0; player < manager.gameState.PlayerCount; player++)
            {
                string positions = "";
                for (int token = 0; token < 4; token++)
                {
                    int index = player * 4 + token;
                    positions += $"T{index}: {manager.gameState.TokenPositions[index]}  ";
                }
                EditorGUILayout.LabelField($"Player {player} Tokens:", positions);
            }
        }
    }
}