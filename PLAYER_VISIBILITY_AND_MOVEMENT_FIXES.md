# Player Visibility and Movement Fixes

## Issues Addressed

1. **Player Invisible** - Only showing "12345678" text
2. **Movement Too Slow** - Player moving at 3 units/second
3. **Compilation Errors** - Missing namespaces and references

## Fixes Applied

### 1. Player Visibility

#### Added Fallback Renderer
- Created `PlayerFallbackRenderer.cs` that displays a simple blue character sprite
- Ensures player is always visible even if MapleStory sprites fail to load
- Automatically added to player GameObject in GameManager

#### Fixed Sorting Layer
- Added "Player" sorting layer to Unity's TagManager
- This was causing sprite renderers to fail silently

#### Fixed Namespace Issues
- Added `using MapleClient.GameData;` to MapleCharacterRenderer
- Fixed NXAssetLoader reference errors

### 2. Movement Speed

Updated physics constants in `MaplePhysics.cs` to match MapleStory v83:
- **Walk Speed**: 3.0 → 1.25 units/second (125 pixels/second at 100% speed)
- **Jump Speed**: 8.0 → 5.55 units/second (555 pixels/second)
- **Gravity**: 25 → 20 units/second²
- **Max Fall Speed**: 10 → 6.7 units/second
- **Walk Drag**: 800 → 80 (reduced friction for smoother movement)

### 3. Background Tile Pool Error

Fixed `BackgroundTilePool.cs` to check `Application.isPlaying` before using `DontDestroyOnLoad`:
```csharp
if (Application.isPlaying)
{
    DontDestroyOnLoad(poolObj);
}
```

### 4. Diagnostic Tools Created

#### PlayerTextDebugger.cs
- Automatically searches for and removes "12345678" text components
- Checks Text, TextMesh components (TMPro removed due to missing reference)
- Logs all sprite renderers on the player

#### PlayerFallbackRenderer.cs
- Creates a 30x60 pixel blue character sprite
- Uses proper sorting layer ("Player")
- Provides visual feedback that player GameObject exists

## Current State

1. **Player should now be visible** as a blue character sprite (fallback renderer)
2. **Movement speed matches MapleStory v83** standards
3. **"12345678" text will be auto-removed** if it's a text component
4. **Camera properly follows player** with correct orthographic settings

## Next Steps

1. Investigate why MapleStory character sprites aren't loading
2. Find source of "12345678" text if not a text component
3. Test player physics and platform collision
4. Implement proper character sprite layering system