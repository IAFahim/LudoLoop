# Network Editor Tools

Unity Editor scripts for automatic setup and management of the Ludo Network system.

## 📦 Files

1. **NetworkUISetup.cs** - Complete UI creation system
2. **NetworkQuickSetup.cs** - Quick setup menu and inspector helpers
3. **UI_SETUP.md** - Detailed UI setup documentation
4. **Network.Editor.asmdef** - Editor assembly definition

## 🎯 Quick Access

### Main Menu Items

**Tools → Ludo Network**
- ✅ Quick Setup - Everything (Creates manager, bridge, and complete UI)
- ✅ Quick Setup - Manager Only
- ✅ Quick Setup - Manager + Bridge
- ✅ Add Example Script to Selected
- ✅ Test Server Connection (Play mode only)
- ✅ Documentation

**GameObject → UI**
- ✅ Ludo Network UI (Complete)

## 🚀 Usage

### Automatic Setup (Recommended)

```
Tools → Ludo Network → Quick Setup - Everything
```

Creates everything you need in one click:
- Network Manager
- Network Game Bridge
- Complete UI with all controls
- All references wired up

### Inspector Helpers

When you select a Network Manager in the scene, the Inspector shows:

**In Edit Mode:**
- Add Network Game Bridge button
- Create Complete UI button
- Add Example Script button

**In Play Mode:**
- Connect/Disconnect buttons
- Connection status display
- Create Game button
- Roll Dice button (when your turn)
- Player and session info

## 🎨 UI Creation

The `NetworkUISetup.CreateNetworkUI()` method creates:

### Panels
1. **Connection Panel** (120px height)
   - Server URL input
   - Connect/Disconnect buttons
   - Connection status

2. **Game Setup Panel** (180px height)
   - Player name input
   - Session ID input
   - Create/Join/Start/List buttons

3. **Gameplay Panel** (120px height)
   - Roll Dice button
   - Token index input
   - Move Token button

4. **Info Panel** (140px height)
   - Game information
   - Turn information

5. **Messages Panel** (fills remaining space)
   - Scrollable console
   - Message log with timestamps
   - Clear button

### Components
- NetworkGameUI script (auto-added and wired)
- All UI elements (buttons, inputs, text)
- Proper layout and styling
- Event handlers connected

## 🔧 Customization

All created elements can be customized after creation:

```csharp
// Find and modify
var connectionPanel = GameObject.Find("Network UI/Connection Panel");
connectionPanel.GetComponent<Image>().color = Color.blue;
```

Or use the Inspector to modify:
- Colors
- Sizes
- Positions
- Text
- Fonts

## 📋 API Reference

### NetworkUISetup

```csharp
public static class NetworkUISetup
{
    // Create complete UI setup
    [MenuItem("GameObject/UI/Ludo Network UI (Complete)")]
    public static void CreateNetworkUI();
}
```

### NetworkQuickSetup

```csharp
public static class NetworkQuickSetup
{
    // Setup everything
    [MenuItem("Tools/Ludo Network/Quick Setup - Everything")]
    public static void SetupEverything();
    
    // Setup manager only
    [MenuItem("Tools/Ludo Network/Quick Setup - Manager Only")]
    public static void SetupManagerOnly();
    
    // Setup manager and bridge
    [MenuItem("Tools/Ludo Network/Quick Setup - Manager + Bridge")]
    public static void SetupManagerAndBridge();
    
    // Add example to selected GameObject
    [MenuItem("Tools/Ludo Network/Add Example Script to Selected")]
    public static void AddExampleScript();
    
    // Test connection (Play mode only)
    [MenuItem("Tools/Ludo Network/Test Server Connection")]
    public static void TestConnection();
    
    // Open documentation
    [MenuItem("Tools/Ludo Network/Documentation")]
    public static void OpenDocumentation();
}
```

### LudoNetworkManagerEditor

Custom inspector that adds helper buttons to Network Manager component.

## 🎓 Advanced Usage

### Create UI Programmatically

```csharp
using Network.Editor;

public class MySetup
{
    [MenuItem("My Menu/Setup Network")]
    public static void Setup()
    {
        NetworkUISetup.CreateNetworkUI();
        Debug.Log("Network UI created!");
    }
}
```

### Extend the Inspector

```csharp
[CustomEditor(typeof(LudoNetworkManager))]
public class MyCustomInspector : LudoNetworkManagerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        // Add your custom buttons
        if (GUILayout.Button("My Custom Action"))
        {
            // Your code
        }
    }
}
```

## 🐛 Troubleshooting

**Menu items not appearing:**
- Ensure scripts are in Editor folder
- Check Network.Editor.asmdef exists
- Restart Unity

**UI creation fails:**
- Check TextMeshPro is imported
- Ensure no compilation errors
- Check Console for error messages

**References not wired:**
- Delete and recreate UI
- Use "Quick Setup - Everything" instead of manual
- Check NetworkGameUI component exists

## 📚 Documentation

- [UI Setup Guide](UI_SETUP.md) - Detailed UI creation guide
- [Integration Guide](../INTEGRATION.md) - Full integration guide
- [API Reference](../README.md) - Network API documentation

## ✨ Features

- ✅ One-click complete setup
- ✅ Automatic reference wiring
- ✅ Inspector helpers
- ✅ Menu system integration
- ✅ Play mode controls
- ✅ Validation and error checking
- ✅ Undo support
- ✅ Dialog confirmations

## 🎯 Best Practices

1. **Use Quick Setup** - Don't manually create components
2. **Test in Play Mode** - Use Inspector buttons to test
3. **Save Scene** - Always save after setup
4. **One UI per scene** - Don't create multiple network UIs
5. **Check Console** - Watch for creation messages

## 📝 Notes

- All created GameObjects are registered with Undo system
- UI is responsive and works at any resolution
- Components are automatically found and assigned
- Duplicate creation is prevented with checks
- All operations show success/error dialogs

---

**Everything you need to quickly setup and test the network system!** 🚀
