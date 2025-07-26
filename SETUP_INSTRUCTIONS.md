# MapleUnity Setup Instructions

## Prerequisites
1. Unity 2021.3 or newer
2. reNX.dll placed in `Assets/Plugins/` folder ✓
3. MapleStory NX files in `C:\HeavenClient\MapleStory-Client\nx\` ✓

## Unity Project Settings
1. **API Compatibility Level**
   - Edit > Project Settings > Player > Configuration
   - Set "Api Compatibility Level" to ".NET Framework" or ".NET 4.x"

2. **Create Sorting Layers** (in order):
   - Background
   - Platform
   - Objects
   - Player
   - Foreground
   - UI

## Setting Up the Scene

1. **Create GameManager**
   - Create empty GameObject named "GameManager"
   - Add GameManager component
   - Set "Use Networking" to false for testing

2. **Setup Camera**
   - Main Camera should have CameraController component
   - Set camera to Orthographic
   - Set Size to 5 (adjust as needed)

3. **Create UI Canvas** (optional)
   - Add Canvas for UI elements
   - Add StatusBar, InventoryView, SkillBar components

## Running the Game

1. Play the scene
2. The game should:
   - Load Henesys (map 100000000) by default
   - Display actual MapleStory backgrounds, tiles, and objects
   - Show character with proper sprites (if Character.nx loads correctly)
   - Allow movement with arrow keys and jumping with Alt/Space

## Troubleshooting

### "NX file not found" errors
- Verify NX files exist in `C:\HeavenClient\MapleStory-Client\nx\`
- Check file names match (Map.nx, Character.nx, etc.)

### No sprites showing
- Check Unity console for sprite loading errors
- Verify reNX.dll is in Plugins folder
- Ensure API Compatibility Level is set correctly

### Character appears as colored square
- Character.nx may not be loading properly
- Check CharacterDataProvider implementation
- Verify sprite conversion from Bitmap to Unity Sprite

### Map appears empty
- Map.nx structure might be different than expected
- Check MapInfoImpl parsing logic
- Debug by logging what nodes are found in the map

## Next Steps

1. Implement sprite loading from NX files (currently returns null)
2. Add proper movement physics matching MapleStory
3. Implement UI with MapleStory graphics
4. Add sound and music playback
5. Connect to v83 server for multiplayer