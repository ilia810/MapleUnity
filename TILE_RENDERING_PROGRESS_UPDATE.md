# Tile Rendering Progress Update - Henesys Map

## Summary of Progress

The tile rendering for Henesys (map 100000000) has significantly improved. The user reports they can now "see almost all assets" which is a major improvement from the initial state where tiles were missing or incorrectly rendered.

## What We Fixed

### 1. Tile Origin/Pivot Point Reading
- **Issue**: All tile origins were reading as (0,0) instead of their actual values
- **Root Cause**: The NX library stores origin data as Point objects, not as separate x/y child nodes
- **Solution**: Updated `SpriteLoader.GetOrigin()` to read the Value property directly as Vector2
- **Code Location**: `Assets/Scripts/GameData/NX/SpriteLoader.cs` lines 296-341

### 2. Tile Origin Offset Application
- **Implementation**: Modified `TileGenerator.LoadTileSprite()` to apply origin offsets to tile positions
- **Formula**: 
  ```csharp
  float offsetX = (centerX - origin.x) / 100f;
  float offsetY = (origin.y - centerY) / 100f;
  renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
  ```
- **Code Location**: `Assets/Scripts/SceneGeneration/TileGenerator.cs`

### 3. Tile Data Structure
- **Added**: OriginX and OriginY fields to TileData class
- **Purpose**: Store origin information from map data extraction
- **Code Location**: `Assets/Scripts/SceneGeneration/MapDataExtractor.cs`

## Remaining Issues

### 1. Layer Order Problems
- Some tiles appear in front of or behind other tiles incorrectly
- Current sorting order calculation:
  ```csharp
  int layerOrder = 7 - tile.Layer;
  int baseOrder = layerOrder * 10000;
  int yOrder = Mathf.RoundToInt(-tile.Y);
  renderer.sortingOrder = baseOrder + yOrder;
  ```
- May need more sophisticated sorting logic

### 2. Some Tiles Don't Sit Right
- Despite origin offset fixes, some tiles still have alignment issues
- Possible causes:
  - Z-ordering conflicts
  - Additional offset data we're not reading
  - Tile flip/rotation properties not being applied
  - Special tile types that need different handling

## Technical Details

### Layer System
- MapleStory uses 8 layers (0-7) for tiles
- Each layer can have its own tileset (tS property)
- Henesys uses "woodMarble" tileset for layers 0-1

### Tile Variants
- bsc: Basic tiles
- edD/edU: Edge down/up
- enH0/enH1: End horizontal variants
- enV0/enV1: End vertical variants
- slLU/slRU/etc: Slope variants

### Origin Data Structure
The C++ client reads origins like this:
```cpp
origin = final_node["origin"];  // Returns Point<int16_t>
```

Our implementation now correctly reads this as:
```csharp
var value = originNode.Value;
if (value is Vector2 vec2) {
    return vec2;
}
```

## Debug Tools Created
1. `DebugTileOriginData.cs` - Inspects NX node structure
2. `DebugOriginDataStructure.cs` - Checks origin data reading
3. `TestTileOriginFix.cs` - Tests origin offset calculations
4. `DebugTileAlignment.cs` - Checks tile alignment in scene
5. `DebugTileOrigins.cs` - Shows tiles with origin offsets

## Next Steps for Researcher

1. **Investigate Layer Ordering**
   - The current formula may not match the C++ client's exact behavior
   - Check if there are additional sorting properties we're missing
   - Look for z-index or zM properties on tiles

2. **Tile Positioning**
   - Some tiles still don't align properly despite origin fixes
   - Check for additional positioning data:
     - Flip/mirror properties
     - Rotation data
     - Special offset calculations for certain tile types

3. **Compare with C++ Client**
   - Add more debug logging to C++ client to understand exact positioning
   - Focus on edge tiles (edD, edU, enH0, etc.) as these seem most affected

4. **Test Other Maps**
   - Check if issues are specific to Henesys or affect other maps
   - woodMarble tileset might have unique properties

## Files Modified
- `Assets/Scripts/GameData/NX/SpriteLoader.cs`
- `Assets/Scripts/SceneGeneration/TileGenerator.cs`
- `Assets/Scripts/SceneGeneration/MapDataExtractor.cs`
- `Assets/Scripts/GameData/NXDataManagerSingleton.cs`
- Various debug tools in `Assets/Scripts/Editor/`

## Repository
All changes have been committed and pushed to: https://github.com/ilia810/MapleUnity.git