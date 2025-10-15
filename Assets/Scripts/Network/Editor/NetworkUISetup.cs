using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Network.Runtime;

namespace Network.Editor
{
    /// <summary>
    /// Editor utility to automatically create and setup the Network UI
    /// Menu: GameObject > UI > Ludo Network UI (Complete)
    /// </summary>
    public static class NetworkUISetup
    {
        [MenuItem("GameObject/UI/Ludo Network UI (Complete)", false, 10)]
        public static void CreateNetworkUI()
        {
            // Create or get canvas
            Canvas canvas = CreateCanvas();
            
            // Create network manager if doesn't exist
            LudoNetworkManager networkManager = CreateNetworkManager();
            NetworkGameBridge gameBridge = CreateGameBridge(networkManager);
            
            // Create UI structure
            GameObject uiRoot = CreateUIRoot(canvas);
            
            // Create all panels
            GameObject connectionPanel = CreateConnectionPanel(uiRoot);
            GameObject gameSetupPanel = CreateGameSetupPanel(uiRoot);
            GameObject gameplayPanel = CreateGameplayPanel(uiRoot);
            GameObject infoPanel = CreateInfoPanel(uiRoot);
            GameObject messagesPanel = CreateMessagesPanel(uiRoot);
            
            // Add and configure NetworkGameUI component
            NetworkGameUI networkUI = uiRoot.AddComponent<NetworkGameUI>();
            WireUpNetworkUI(networkUI, networkManager, gameBridge, 
                connectionPanel, gameSetupPanel, gameplayPanel, infoPanel, messagesPanel);
            
            // Select the created UI
            Selection.activeGameObject = uiRoot;
            
            Debug.Log("[NetworkUISetup] ✅ Network UI created successfully! Check the Inspector to verify all references.");
            EditorUtility.DisplayDialog("Success", 
                "Network UI created successfully!\n\n" +
                "Components created:\n" +
                "- Canvas with Network UI\n" +
                "- Network Manager\n" +
                "- Network Game Bridge\n" +
                "- Complete UI layout\n\n" +
                "All references have been automatically wired up.",
                "OK");
        }
        
        private static Canvas CreateCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                Debug.Log("[NetworkUISetup] Created new Canvas");
            }
            
            return canvas;
        }
        
        private static LudoNetworkManager CreateNetworkManager()
        {
            LudoNetworkManager manager = Object.FindObjectOfType<LudoNetworkManager>();
            
            if (manager == null)
            {
                GameObject managerObj = new GameObject("Network Manager");
                manager = managerObj.AddComponent<LudoNetworkManager>();
                
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Network Manager");
                Debug.Log("[NetworkUISetup] Created Network Manager");
            }
            
            return manager;
        }
        
        private static NetworkGameBridge CreateGameBridge(LudoNetworkManager manager)
        {
            NetworkGameBridge bridge = manager.GetComponent<NetworkGameBridge>();
            
            if (bridge == null)
            {
                bridge = manager.gameObject.AddComponent<NetworkGameBridge>();
                Undo.RegisterCreatedObjectUndo(bridge, "Create Network Game Bridge");
                Debug.Log("[NetworkUISetup] Created Network Game Bridge");
            }
            
            return bridge;
        }
        
        private static GameObject CreateUIRoot(Canvas canvas)
        {
            GameObject root = new GameObject("Network UI");
            root.transform.SetParent(canvas.transform, false);
            
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            Undo.RegisterCreatedObjectUndo(root, "Create Network UI Root");
            
            return root;
        }
        
        private static GameObject CreateConnectionPanel(GameObject parent)
        {
            GameObject panel = CreatePanel(parent, "Connection Panel", new Vector2(10, -10), new Vector2(-10, -10), 
                new Vector2(0, 1), new Vector2(1, 1), new Color(0.2f, 0.2f, 0.2f, 0.9f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 120);
            
            // Title
            CreateText(panel, "Connection Title", "Network Connection", 18, TextAlignmentOptions.Left, 
                new Vector2(10, -10), new Vector2(-10, -40));
            
            // Server URL Input
            GameObject urlLabel = CreateText(panel, "URL Label", "Server URL:", 14, TextAlignmentOptions.Left,
                new Vector2(10, -50), new Vector2(150, -70));
            GameObject urlInput = CreateInputField(panel, "Server URL Input", "ws://localhost:8080",
                new Vector2(160, -50), new Vector2(-10, -70));
            
            // Buttons
            GameObject connectBtn = CreateButton(panel, "Connect Button", "Connect", new Color(0.2f, 0.7f, 0.2f),
                new Vector2(10, -80), new Vector2(150, -110));
            GameObject disconnectBtn = CreateButton(panel, "Disconnect Button", "Disconnect", new Color(0.7f, 0.2f, 0.2f),
                new Vector2(160, -80), new Vector2(300, -110));
            
            // Status Text
            GameObject statusText = CreateText(panel, "Connection Status", "Disconnected", 14, TextAlignmentOptions.Right,
                new Vector2(-200, -80), new Vector2(-10, -110));
            TMP_Text statusTMP = statusText.GetComponent<TMP_Text>();
            statusTMP.color = Color.red;
            
            return panel;
        }
        
        private static GameObject CreateGameSetupPanel(GameObject parent)
        {
            GameObject panel = CreatePanel(parent, "Game Setup Panel", new Vector2(10, -140), new Vector2(-10, -140),
                new Vector2(0, 1), new Vector2(1, 1), new Color(0.25f, 0.25f, 0.35f, 0.9f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 180);
            
            // Title
            CreateText(panel, "Setup Title", "Game Setup", 18, TextAlignmentOptions.Left,
                new Vector2(10, -10), new Vector2(-10, -40));
            
            // Player Name Input
            CreateText(panel, "Name Label", "Player Name:", 14, TextAlignmentOptions.Left,
                new Vector2(10, -50), new Vector2(150, -70));
            GameObject nameInput = CreateInputField(panel, "Player Name Input", "Unity Player",
                new Vector2(160, -50), new Vector2(-10, -70));
            
            // Session ID Input
            CreateText(panel, "Session Label", "Session ID:", 14, TextAlignmentOptions.Left,
                new Vector2(10, -80), new Vector2(150, -100));
            GameObject sessionInput = CreateInputField(panel, "Session ID Input", "",
                new Vector2(160, -80), new Vector2(-10, -100));
            
            // Buttons row 1
            GameObject createBtn = CreateButton(panel, "Create Game Button", "Create Game", new Color(0.2f, 0.5f, 0.7f),
                new Vector2(10, -110), new Vector2(200, -140));
            GameObject joinBtn = CreateButton(panel, "Join Game Button", "Join Game", new Color(0.5f, 0.5f, 0.2f),
                new Vector2(210, -110), new Vector2(400, -140));
            
            // Buttons row 2
            GameObject startBtn = CreateButton(panel, "Start Game Button", "Start Game", new Color(0.2f, 0.7f, 0.2f),
                new Vector2(10, -150), new Vector2(200, -170));
            GameObject listBtn = CreateButton(panel, "List Games Button", "List Games", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(210, -150), new Vector2(400, -170));
            
            return panel;
        }
        
        private static GameObject CreateGameplayPanel(GameObject parent)
        {
            GameObject panel = CreatePanel(parent, "Gameplay Panel", new Vector2(10, -330), new Vector2(-10, -330),
                new Vector2(0, 1), new Vector2(1, 1), new Color(0.3f, 0.25f, 0.2f, 0.9f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 120);
            
            // Title
            CreateText(panel, "Gameplay Title", "Gameplay", 18, TextAlignmentOptions.Left,
                new Vector2(10, -10), new Vector2(-10, -40));
            
            // Roll Dice Button
            GameObject rollBtn = CreateButton(panel, "Roll Dice Button", "🎲 Roll Dice", new Color(0.7f, 0.3f, 0.2f),
                new Vector2(10, -50), new Vector2(200, -90));
            TMP_Text rollText = rollBtn.GetComponentInChildren<TMP_Text>();
            rollText.fontSize = 20;
            
            // Token Selection
            CreateText(panel, "Token Label", "Token Index:", 14, TextAlignmentOptions.Left,
                new Vector2(210, -50), new Vector2(340, -70));
            GameObject tokenInput = CreateInputField(panel, "Token Index Input", "0",
                new Vector2(210, -75), new Vector2(340, -95));
            
            // Move Token Button
            GameObject moveBtn = CreateButton(panel, "Move Token Button", "Move Token", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(350, -50), new Vector2(540, -90));
            
            return panel;
        }
        
        private static GameObject CreateInfoPanel(GameObject parent)
        {
            GameObject panel = CreatePanel(parent, "Info Panel", new Vector2(10, -460), new Vector2(-10, -460),
                new Vector2(0, 1), new Vector2(1, 1), new Color(0.2f, 0.3f, 0.3f, 0.9f));
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 140);
            
            // Title
            CreateText(panel, "Info Title", "Game Information", 18, TextAlignmentOptions.Left,
                new Vector2(10, -10), new Vector2(-10, -40));
            
            // Game Info Text
            GameObject gameInfo = CreateText(panel, "Game Info Text", 
                "Session: Not connected\nPlayer Index: -\nPlayers: 0", 
                14, TextAlignmentOptions.Left,
                new Vector2(10, -50), new Vector2(250, -110));
            
            // Turn Info Text
            GameObject turnInfo = CreateText(panel, "Turn Info Text",
                "Current Turn: -\nYour Turn: NO\nLast Dice: 0",
                14, TextAlignmentOptions.Left,
                new Vector2(260, -50), new Vector2(-10, -110));
            
            return panel;
        }
        
        private static GameObject CreateMessagesPanel(GameObject parent)
        {
            GameObject panel = CreatePanel(parent, "Messages Panel", new Vector2(10, -610), new Vector2(-10, 10),
                new Vector2(0, 0), new Vector2(1, 1), new Color(0.15f, 0.15f, 0.15f, 0.95f));
            
            // Title
            CreateText(panel, "Messages Title", "Console Messages", 18, TextAlignmentOptions.Left,
                new Vector2(10, -10), new Vector2(-10, -40));
            
            // Clear Button
            GameObject clearBtn = CreateButton(panel, "Clear Button", "Clear", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(-110, -10), new Vector2(-10, -35));
            
            // Scroll View
            GameObject scrollView = new GameObject("Messages ScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            
            RectTransform scrollRT = scrollView.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 1);
            scrollRT.offsetMin = new Vector2(10, 10);
            scrollRT.offsetMax = new Vector2(-10, -45);
            
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollView.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlHeight = false;
            
            // Messages Text
            GameObject messagesText = new GameObject("Messages Text");
            messagesText.transform.SetParent(content.transform, false);
            RectTransform msgRT = messagesText.AddComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0, 1);
            msgRT.anchorMax = new Vector2(1, 1);
            msgRT.pivot = new Vector2(0, 1);
            
            TMP_Text tmp = messagesText.AddComponent<TextMeshProUGUI>();
            tmp.text = "[00:00:00] Network UI ready. Click Connect to start.\n";
            tmp.fontSize = 12;
            tmp.color = Color.green;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.richText = true;
            tmp.enableWordWrapping = true;
            
            // Configure scroll rect
            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = null;
            
            return panel;
        }
        
        private static GameObject CreatePanel(GameObject parent, string name, Vector2 offsetMin, Vector2 offsetMax,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            Image img = panel.AddComponent<Image>();
            img.color = color;
            
            return panel;
        }
        
        private static GameObject CreateText(GameObject parent, string name, string text, int fontSize,
            TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);
            
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            
            return textObj;
        }
        
        private static GameObject CreateInputField(GameObject parent, string name, string placeholder,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent.transform, false);
            
            RectTransform rt = inputObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            Image img = inputObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            
            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform taRT = textArea.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(5, 2);
            taRT.offsetMax = new Vector2(-5, -2);
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRT = placeholderObj.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.sizeDelta = Vector2.zero;
            
            TMP_Text phText = placeholderObj.AddComponent<TextMeshProUGUI>();
            phText.text = placeholder;
            phText.fontSize = 14;
            phText.color = new Color(1f, 1f, 1f, 0.5f);
            phText.alignment = TextAlignmentOptions.Left;
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform tRT = textObj.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.sizeDelta = Vector2.zero;
            
            TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "";
            tmpText.fontSize = 14;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Left;
            
            inputField.textViewport = taRT;
            inputField.textComponent = tmpText;
            inputField.placeholder = phText;
            inputField.text = placeholder;
            
            return inputObj;
        }
        
        private static GameObject CreateButton(GameObject parent, string name, string text, Color color,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);
            
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            
            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            ColorBlock colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;
            
            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            
            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            
            return btnObj;
        }
        
        private static void WireUpNetworkUI(NetworkGameUI networkUI, LudoNetworkManager networkManager,
            NetworkGameBridge gameBridge, GameObject connectionPanel, GameObject gameSetupPanel,
            GameObject gameplayPanel, GameObject infoPanel, GameObject messagesPanel)
        {
            // Use SerializedObject to set private fields
            SerializedObject so = new SerializedObject(networkUI);
            
            // Network references
            so.FindProperty("networkManager").objectReferenceValue = networkManager;
            so.FindProperty("gameBridge").objectReferenceValue = gameBridge;
            
            // Connection Panel
            so.FindProperty("serverUrlInput").objectReferenceValue = 
                connectionPanel.transform.Find("Server URL Input")?.GetComponent<TMP_InputField>();
            so.FindProperty("connectButton").objectReferenceValue = 
                connectionPanel.transform.Find("Connect Button")?.GetComponent<Button>();
            so.FindProperty("disconnectButton").objectReferenceValue = 
                connectionPanel.transform.Find("Disconnect Button")?.GetComponent<Button>();
            so.FindProperty("connectionStatus").objectReferenceValue = 
                connectionPanel.transform.Find("Connection Status")?.GetComponent<TextMeshProUGUI>();
            
            // Game Setup Panel
            so.FindProperty("playerNameInput").objectReferenceValue = 
                gameSetupPanel.transform.Find("Player Name Input")?.GetComponent<TMP_InputField>();
            so.FindProperty("sessionIdInput").objectReferenceValue = 
                gameSetupPanel.transform.Find("Session ID Input")?.GetComponent<TMP_InputField>();
            so.FindProperty("createGameButton").objectReferenceValue = 
                gameSetupPanel.transform.Find("Create Game Button")?.GetComponent<Button>();
            so.FindProperty("joinGameButton").objectReferenceValue = 
                gameSetupPanel.transform.Find("Join Game Button")?.GetComponent<Button>();
            so.FindProperty("startGameButton").objectReferenceValue = 
                gameSetupPanel.transform.Find("Start Game Button")?.GetComponent<Button>();
            
            // Gameplay Panel
            so.FindProperty("rollDiceButton").objectReferenceValue = 
                gameplayPanel.transform.Find("Roll Dice Button")?.GetComponent<Button>();
            so.FindProperty("tokenIndexInput").objectReferenceValue = 
                gameplayPanel.transform.Find("Token Index Input")?.GetComponent<TMP_InputField>();
            so.FindProperty("moveTokenButton").objectReferenceValue = 
                gameplayPanel.transform.Find("Move Token Button")?.GetComponent<Button>();
            
            // Info Panel
            so.FindProperty("gameInfoText").objectReferenceValue = 
                infoPanel.transform.Find("Game Info Text")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("turnInfoText").objectReferenceValue = 
                infoPanel.transform.Find("Turn Info Text")?.GetComponent<TextMeshProUGUI>();
            
            // Messages Panel
            GameObject scrollView = messagesPanel.transform.Find("Messages ScrollView")?.gameObject;
            if (scrollView != null)
            {
                so.FindProperty("messagesScroll").objectReferenceValue = scrollView.GetComponent<ScrollRect>();
                
                GameObject content = scrollView.transform.Find("Viewport/Content")?.gameObject;
                if (content != null)
                {
                    so.FindProperty("messagesText").objectReferenceValue = 
                        content.transform.Find("Messages Text")?.GetComponent<TextMeshProUGUI>();
                }
            }
            
            so.ApplyModifiedProperties();
            
            Debug.Log("[NetworkUISetup] All UI references wired up successfully!");
        }
    }
}
