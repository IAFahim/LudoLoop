# Automatic UI Setup Guide

## 🎨 One-Click UI Creation

The network module includes powerful editor scripts that automatically create and wire up the complete UI with a single click!

## Quick Setup Methods

### Method 1: Menu System (Easiest!)

#### Option A: Everything (Recommended)
```
Tools → Ludo Network → Quick Setup - Everything
```
This creates:
- ✅ Network Manager
- ✅ Network Game Bridge  
- ✅ Complete UI with all controls
- ✅ All references automatically wired up

**Result:** Fully functional network UI ready to use!

#### Option B: Manager Only
```
Tools → Ludo Network → Quick Setup - Manager Only
```
Creates just the Network Manager component.

#### Option C: Manager + Bridge
```
Tools → Ludo Network → Quick Setup - Manager + Bridge
```
Creates Network Manager and Bridge, auto-assigns OfflineLudoGame if found.

### Method 2: GameObject Menu
```
GameObject → UI → Ludo Network UI (Complete)
```
Same as "Quick Setup - Everything" but from GameObject menu.

### Method 3: Inspector Button
```
1. Select Network Manager in scene
2. Look in Inspector
3. Click "Create Complete UI" button
```

## 📦 What Gets Created

### UI Structure
```
Canvas
└── Network UI
    ├── Connection Panel (120px)
    │   ├── Server URL Input
    │   ├── Connect Button
    │   ├── Disconnect Button
    │   └── Connection Status
    │
    ├── Game Setup Panel (180px)
    │   ├── Player Name Input
    │   ├── Session ID Input
    │   ├── Create Game Button
    │   ├── Join Game Button
    │   ├── Start Game Button
    │   └── List Games Button
    │
    ├── Gameplay Panel (120px)
    │   ├── Roll Dice Button (🎲)
    │   ├── Token Index Input
    │   └── Move Token Button
    │
    ├── Info Panel (140px)
    │   ├── Game Info Text
    │   │   - Session ID
    │   │   - Player Index
    │   │   - Player Count
    │   └── Turn Info Text
    │       - Current Turn
    │       - Your Turn
    │       - Last Dice Roll
    │
    └── Messages Panel (Rest of screen)
        ├── Console Messages Title
        ├── Clear Button
        └── Scrollable Message Log
```

### Components Created

1. **Network Manager GameObject**
   - LudoNetworkManager component
   - NetworkGameBridge component

2. **Canvas** (if doesn't exist)
   - Canvas component
   - CanvasScaler
   - GraphicRaycaster

3. **Network UI GameObject**
   - NetworkGameUI component
   - All UI panels and controls
   - All references auto-wired

## 🎮 Using the Created UI

### After Creation

1. **Press Play**
2. **Click "Connect"** in Connection Panel
3. **Enter player name** (optional)
4. **Click "Create Game"** or enter Session ID and **"Join Game"**
5. **Click "Start Game"** when ready
6. **Play the game!**
   - Roll Dice when it's your turn
   - Enter token index (0-15)
   - Click Move Token

### UI Features

**Connection Panel:**
- Server URL input (default: ws://localhost:8080)
- Connect/Disconnect buttons
- Connection status indicator (green = connected, red = disconnected)

**Game Setup Panel:**
- Player name customization
- Session ID display/input
- Create/Join/Start game buttons
- List available games

**Gameplay Panel:**
- Large Roll Dice button (disabled when not your turn)
- Token index input (0-3 for player 0, 4-7 for player 1, etc.)
- Move Token button

**Info Panel:**
- Real-time game information
- Current turn indicator
- Your player index
- Last dice roll

**Messages Panel:**
- Console-style message log
- Timestamps on all messages
- Color-coded messages (green = success, red = error)
- Auto-scroll to latest message
- Clear button

## 🔧 Customization

### Modify After Creation

All created UI elements can be customized:

1. **Colors:** Select any panel/button and change Image color
2. **Layout:** Adjust RectTransform positions/sizes
3. **Text:** Modify TextMeshProUGUI components
4. **Functionality:** Extend NetworkGameUI script

### Common Customizations

**Change Button Colors:**
```csharp
// In Inspector
Select button → Image component → Color
```

**Adjust Panel Sizes:**
```csharp
// In Inspector
Select panel → RectTransform → Height
```

**Add Custom Messages:**
```csharp
// In your script
networkUI.SendMessage("Custom event happened", false);
```

## 🎨 UI Styling

The automatically created UI uses a dark theme:

**Color Palette:**
- Connection Panel: Dark Gray (0.2, 0.2, 0.2)
- Game Setup Panel: Dark Blue-Gray (0.25, 0.25, 0.35)
- Gameplay Panel: Dark Brown (0.3, 0.25, 0.2)
- Info Panel: Dark Teal (0.2, 0.3, 0.3)
- Messages Panel: Almost Black (0.15, 0.15, 0.15)

**Buttons:**
- Connect: Green (0.2, 0.7, 0.2)
- Disconnect: Red (0.7, 0.2, 0.2)
- Create: Blue (0.2, 0.5, 0.7)
- Join: Yellow (0.5, 0.5, 0.2)
- Start: Green (0.2, 0.7, 0.2)
- Roll Dice: Orange-Red (0.7, 0.3, 0.2)
- Move: Green (0.3, 0.6, 0.3)

## 📱 Responsive Design

The UI is designed to be responsive:

- Panels use anchor points for proper scaling
- Text uses TextMeshPro for sharp rendering
- Scroll view auto-expands messages
- Works in any resolution

## 🐛 Troubleshooting

**UI not appearing:**
- Check Canvas exists in scene
- Ensure Canvas render mode is Screen Space Overlay
- Check Camera if using Screen Space Camera

**Buttons not responding:**
- Verify EventSystem exists (auto-created with Canvas)
- Check GraphicRaycaster on Canvas
- Ensure buttons have Button component

**References missing:**
- Delete UI and recreate
- Use "Quick Setup - Everything" instead of manual creation
- Check Console for errors during creation

**TextMeshPro errors:**
- Import TextMeshPro essentials: Window → TextMeshPro → Import TMP Essential Resources

## 📋 Manual Setup (If Needed)

If automatic setup fails, you can manually:

1. Create Canvas
2. Add NetworkGameUI script to a GameObject
3. Create UI elements matching the structure above
4. Assign all references in Inspector

But this is not recommended - use the automatic setup!

## 🎓 Advanced: Script API

The setup scripts provide methods you can call:

```csharp
using Network.Editor;

// Create UI programmatically
NetworkUISetup.CreateNetworkUI();

// Setup just manager
NetworkQuickSetup.SetupManagerOnly();

// Setup manager + bridge
NetworkQuickSetup.SetupManagerAndBridge();
```

## ✨ Tips

1. **Run setup ONCE** - Don't create multiple UIs
2. **Start server first** - Have `server/` running before testing
3. **Use Console** - Watch messages panel for feedback
4. **Test locally** - Create game in one Unity instance, join from another
5. **Save scene** - Don't forget to save after creating UI!

## 🚀 Next Steps

After UI is created:

1. ✅ Start the server: `cd server && npm start`
2. ✅ Press Play in Unity
3. ✅ Click Connect
4. ✅ Create or join a game
5. ✅ Play!

## 📚 Related Documentation

- [Network Module README](../README.md) - API reference
- [Integration Guide](../INTEGRATION.md) - Detailed integration
- [Server Documentation](../../../server/README.md) - Server API

---

**Everything you need is created automatically! Just click and play!** 🎮✨
