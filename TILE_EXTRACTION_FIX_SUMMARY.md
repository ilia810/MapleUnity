# Tile Extraction Fix Summary

## Issues Fixed

### 1. Tiles Not Being Extracted
**Problem**: The debug output showed that tiles existed in the map data (859 tiles across layers 0 and 1) but none were being extracted because they lacked the "tS" (tileSet) property.

**Solution**: 
- Added `GetDefaultTileSet()` method to determine the default tile set for a map
- Modified `ExtractTilesFromNode()` to accept an optional `defaultTileSet` parameter
- When a tile lacks the "tS" property, we now use the default tile set
- The default tile set is determined by:
  1. Checking map info for a tileSet property
  2. Looking for the first tile that has a tS property to use as default
  3. Inferring based on map ID (e.g., "wood" for Henesys area)

### 2. Background Type 3 Not Handled
**Problem**: The blue 256x256 squares were background type 3 (sky/solid color backgrounds) that weren't being tiled properly.

**Solution**:
- Added handling for background type 3 in `BackgroundGenerator`
- Created `CreateFullScreenTiledBackground()` method to tile these backgrounds across the entire visible area
- Type 3 backgrounds are now properly tiled to cover the full screen

## Files Modified

1. **MapDataExtractor.cs**:
   - Added `GetDefaultTileSet()` method
   - Modified `ExtractTilesFromNode()` to use default tile set when "tS" is missing
   - Enhanced debug logging to show skipped tiles

2. **BackgroundGenerator.cs**:
   - Added type 3 background handling
   - Created `CreateFullScreenTiledBackground()` method
   - Added comments explaining background types

3. **TestTileExtraction.cs** (new):
   - Created test script to verify tile extraction works correctly

## Testing

To test the fixes, use the Unity Editor menu:
1. MapleUnity > Test > Tile Extraction Fix
2. Click "Test Henesys Tile Extraction"

This will show:
- Number of tiles extracted
- Unique tile sets found
- First 10 tiles with sprite loading status
- Distribution of tiles by tileSet

## Expected Results

For Henesys (map 100000000), you should now see:
- ~859 tiles extracted (using "wood" as default tileSet)
- Blue sky background properly tiled across the screen
- Ground tiles rendered correctly

## Notes

- Henesys uses "wood" tiles for its ground/platforms
- The grassySoil background is a 256x256 blue square that should be tiled as the sky
- NPCs should now appear in front of buildings due to previous sorting fixes