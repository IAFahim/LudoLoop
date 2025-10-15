using UnityEngine;
using UnityEditor;
using Network.Runtime;
using LudoGame.Runtime;
using MessageType = UnityEditor.MessageType;

namespace Network.Editor
{
    /// <summary>
    /// Quick setup menu for Ludo Network components
    /// Provides multiple quick-setup options
    /// </summary>
    public static class NetworkQuickSetup
    {
        [MenuItem("Tools/Ludo Network/Quick Setup - Everything", false, 1)]
        public static void SetupEverything()
        {
            if (EditorUtility.DisplayDialog("Setup Everything",
                "This will create:\n\n" +
                "1. Network Manager\n" +
                "2. Network Game Bridge\n" +
                "3. Complete UI with all controls\n" +
                "4. Wire up all references\n\n" +
                "Continue?", "Yes", "Cancel"))
            {
                NetworkUISetup.CreateNetworkUI();
            }
        }
        
        [MenuItem("Tools/Ludo Network/Quick Setup - Manager Only", false, 2)]
        public static void SetupManagerOnly()
        {
            LudoNetworkManager manager = Object.FindObjectOfType<LudoNetworkManager>();
            
            if (manager != null)
            {
                EditorUtility.DisplayDialog("Already Exists",
                    "Network Manager already exists in the scene!",
                    "OK");
                Selection.activeGameObject = manager.gameObject;
                return;
            }
            
            GameObject managerObj = new GameObject("Network Manager");
            manager = managerObj.AddComponent<LudoNetworkManager>();
            
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Network Manager");
            Selection.activeGameObject = managerObj;
            
            EditorUtility.DisplayDialog("Success",
                "Network Manager created!\n\n" +
                "Next steps:\n" +
                "1. Configure Server URL in Inspector\n" +
                "2. Add Network Game Bridge component\n" +
                "3. Create UI or use script to control",
                "OK");
        }
        
        [MenuItem("Tools/Ludo Network/Quick Setup - Manager + Bridge", false, 3)]
        public static void SetupManagerAndBridge()
        {
            LudoNetworkManager manager = Object.FindObjectOfType<LudoNetworkManager>();
            
            if (manager == null)
            {
                GameObject managerObj = new GameObject("Network Manager");
                manager = managerObj.AddComponent<LudoNetworkManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Network Manager");
            }
            
            NetworkGameBridge bridge = manager.GetComponent<NetworkGameBridge>();
            if (bridge == null)
            {
                bridge = manager.gameObject.AddComponent<NetworkGameBridge>();
                Undo.RegisterCreatedObjectUndo(bridge, "Create Network Game Bridge");
            }
            
            // Try to find and assign OfflineLudoGame
            OfflineLudoGame offlineGame = Object.FindObjectOfType<OfflineLudoGame>();
            if (offlineGame != null)
            {
                SerializedObject so = new SerializedObject(bridge);
                so.FindProperty("offlineLudoGame").objectReferenceValue = offlineGame;
                so.ApplyModifiedProperties();
            }
            
            Selection.activeGameObject = manager.gameObject;
            
            string message = "Network Manager and Bridge created!";
            if (offlineGame != null)
            {
                message += "\n\nOfflineLudoGame automatically assigned.";
            }
            else
            {
                message += "\n\nNote: OfflineLudoGame not found in scene.\nYou'll need to assign it manually in the Inspector.";
            }
            
            EditorUtility.DisplayDialog("Success", message, "OK");
        }
        
        [MenuItem("Tools/Ludo Network/Add Example Script to Selected", false, 20)]
        public static void AddExampleScript()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select a GameObject first!",
                    "OK");
                return;
            }
            
            NetworkGameExample example = Selection.activeGameObject.AddComponent<NetworkGameExample>();
            Undo.RegisterCreatedObjectUndo(example, "Add Network Game Example");
            
            EditorUtility.DisplayDialog("Success",
                "NetworkGameExample added!\n\n" +
                "This script will:\n" +
                "- Auto-setup components if missing\n" +
                "- Handle all network events\n" +
                "- Provide example implementation\n\n" +
                "Check the Inspector to configure settings.",
                "OK");
        }
        
        [MenuItem("Tools/Ludo Network/Documentation", false, 100)]
        public static void OpenDocumentation()
        {
            string readmePath = "Assets/Scripts/Network/README.md";
            string integrationPath = "Assets/Scripts/Network/INTEGRATION.md";
            
            if (System.IO.File.Exists(readmePath))
            {
                System.Diagnostics.Process.Start(readmePath);
            }
            
            if (System.IO.File.Exists(integrationPath))
            {
                System.Diagnostics.Process.Start(integrationPath);
            }
        }
        
        [MenuItem("Tools/Ludo Network/Test Server Connection", false, 101)]
        public static void TestConnection()
        {
            LudoNetworkManager manager = Object.FindObjectOfType<LudoNetworkManager>();
            
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Not Found",
                    "No Network Manager in scene!\n\n" +
                    "Use 'Quick Setup - Everything' to create one.",
                    "OK");
                return;
            }
            
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Not Playing",
                    "Enter Play Mode to test the connection!",
                    "OK");
                return;
            }
            
            if (manager.IsConnected)
            {
                EditorUtility.DisplayDialog("Connected",
                    $"Already connected!\n\n" +
                    $"Player ID: {manager.PlayerId}\n" +
                    $"Session: {manager.SessionId ?? "Not in game"}",
                    "OK");
            }
            else
            {
                manager.Connect();
                EditorUtility.DisplayDialog("Connecting",
                    "Attempting to connect...\n\n" +
                    "Check the Console for connection status.",
                    "OK");
            }
        }
        
        // Validation
        [MenuItem("Tools/Ludo Network/Add Example Script to Selected", true)]
        public static bool ValidateAddExampleScript()
        {
            return Selection.activeGameObject != null;
        }
        
        [MenuItem("Tools/Ludo Network/Test Server Connection", true)]
        public static bool ValidateTestConnection()
        {
            return Application.isPlaying;
        }
    }
    
    /// <summary>
    /// Custom inspector for Network Manager with quick actions
    /// </summary>
    [CustomEditor(typeof(LudoNetworkManager))]
    public class LudoNetworkManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            LudoNetworkManager manager = (LudoNetworkManager)target;
            
            if (Application.isPlaying)
            {
                // Runtime controls
                EditorGUILayout.BeginHorizontal();
                
                GUI.enabled = !manager.IsConnected;
                if (GUILayout.Button("Connect", GUILayout.Height(30)))
                {
                    manager.Connect();
                }
                
                GUI.enabled = manager.IsConnected;
                if (GUILayout.Button("Disconnect", GUILayout.Height(30)))
                {
                    manager.Disconnect();
                }
                
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                
                if (manager.IsConnected)
                {
                    EditorGUILayout.HelpBox(
                        $"Connected!\n" +
                        $"Player ID: {manager.PlayerId}\n" +
                        $"Session: {manager.SessionId ?? "Not in game"}\n" +
                        $"Player Index: {manager.PlayerIndex}",
                        MessageType.Info);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.enabled = string.IsNullOrEmpty(manager.SessionId);
                    if (GUILayout.Button("Create Game"))
                    {
                        manager.CreateGame(4, "Inspector Player");
                    }
                    
                    GUI.enabled = !string.IsNullOrEmpty(manager.SessionId) && manager.IsMyTurn;
                    if (GUILayout.Button("Roll Dice"))
                    {
                        manager.RollDice();
                    }
                    
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Not connected to server", MessageType.Warning);
                }
            }
            else
            {
                // Editor-time setup
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Setup Helpers", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Add Network Game Bridge", GUILayout.Height(25)))
                {
                    if (manager.GetComponent<NetworkGameBridge>() == null)
                    {
                        Undo.AddComponent<NetworkGameBridge>(manager.gameObject);
                        EditorUtility.DisplayDialog("Success", "Network Game Bridge added!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Already Exists", 
                            "Network Game Bridge already exists on this GameObject!", "OK");
                    }
                }
                
                if (GUILayout.Button("Create Complete UI", GUILayout.Height(25)))
                {
                    NetworkUISetup.CreateNetworkUI();
                }
                
                if (GUILayout.Button("Add Example Script", GUILayout.Height(25)))
                {
                    if (manager.GetComponent<NetworkGameExample>() == null)
                    {
                        Undo.AddComponent<NetworkGameExample>(manager.gameObject);
                        EditorUtility.DisplayDialog("Success", "NetworkGameExample added!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Already Exists",
                            "NetworkGameExample already exists on this GameObject!", "OK");
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to test connection and gameplay.",
                    MessageType.Info);
            }
        }
    }
}
