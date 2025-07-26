# Henesys Tile Rendering Issue Summary

## Issue Description
The Unity implementation of MapleStory client is rendering Henesys (map 100000000) with incorrect tiles. The user reports:
- Ground tiles seem "somewhat right" but upper and lower parts of the tile map don't sit right
- Some platforms have "half stone half grass tile which is wrong"
- Overall rendering is "better but still not 100%" compared to the C++ client

## Key Discoveries

### 1. Tileset is Per-Layer, Not Per-Map
Through debugging the C++ client, we discovered that MapleStory stores tiles in numbered layers (0-7), and each layer has its own tileset defined in `layer/info/tS`.

For Henesys:
- Layer 0: tS='woodMarble' (236 tiles)
- Layer 1: tS='woodMarble' (623 tiles)
- Layers 2-7: empty tS (0 tiles)

### 2. The C++ Client Tile Loading Process
```cpp
// MapTilesObjs.cpp
auto tileset = src["info"]["tS"] + ".img";  // Appends .img to tS value

// Tile.cpp
nl::node dsrc = nl::nx::Map["Tile"][actualTileset][src["u"]][src["no"]];
```

### 3. woodMarble Tileset Contents
The woodMarble tileset contains:
- bsc (basic): 5 tiles (0-4)
- edD (edge down): 2 tiles (0-1)
- edU (edge up): 2 tiles (0-1)
- enH0/enH1 (end horizontal): 4 tiles each (0-3)
- enV0/enV1 (end vertical): 2 tiles each (0-1)
- slLU (slope left up): 1 tile

Tiles are 90x60 pixels in size.

## Changes Implemented

### 1. Fixed Layer-Based Tile Extraction
**File: MapDataExtractor.cs**
- Changed from looking for tiles at map level to iterating through layers 0-7
- Each layer now correctly reads its own `info/tS` value
- Tiles are extracted with their layer number stored

### 2. Fixed Tile Sorting Order
**File: TileGenerator.cs**
- Updated sorting order calculation to account for layers
- Each layer gets 1000 sorting order units
- Within each layer, Z value provides fine-grained sorting

### 3. Fixed Empty Tileset Handling
**File: NXDataManagerSingleton.cs**
- Handle empty tileset names (which become ".img" in C++ client)
- Added proper tileset path construction

### 4. Added Debug Tools
- **AnalyzeHenesysTileUsage.cs**: Shows which tile variants are used in each layer
- **PreviewWoodMarbleTiles.cs**: Visual preview of tileset contents
- **CheckTileOrigins.cs**: Checks tile origin points for alignment
- **DebugTileAlignment.cs**: Checks and fixes tile grid alignment

## Remaining Issues

### 1. Visual Mismatch
The "woodMarble" tileset name suggests it contains mixed wood and marble/stone textures. The user reports seeing "half stone half grass" tiles on platforms, which might be:
- Correct tiles but visually unexpected (woodMarble might intentionally mix materials)
- Tile alignment issues causing overlap
- Wrong tile selection (incorrect `no` values)
- Missing tile origin/pivot handling

### 2. Possible Root Causes
1. **Tile Origins**: MapleStory tiles may have specific origin points that affect alignment. We added code to read origins but haven't fully verified if they're being applied correctly.

2. **Tile Grid Alignment**: Tiles might need to snap to a specific grid (e.g., 30x30 pixels) for proper alignment.

3. **Wrong Tileset**: Although the C++ debug shows woodMarble is correct, the visual appearance suggests it might not be the right tileset for Henesys's stone/grey appearance.

## Debug Information Available
- C++ client debug logs show tile loading process
- Unity console shows tile extraction and loading
- Scene contains tiles with names like "Tile_L0_woodMarble_bsc_4"
- Transparent texture warnings are normal (decorative overlays)

## Next Steps for Researcher
1. Compare screenshots between C++ client and Unity to identify exact visual differences
2. Verify if woodMarble is the correct tileset (name suggests mixed materials)
3. Check if tile origins/pivots are being applied correctly for alignment
4. Investigate if there's a tile grid system that needs to be enforced
5. Check if tile selection logic (variant/no) matches C++ client exactly

## Code References
- Tile extraction: `MapDataExtractor.cs:251-287`
- Tile rendering: `TileGenerator.cs:51-121`
- Tile sprite loading: `NXDataManagerSingleton.cs:618-714`
- C++ tile loading: `MapTilesObjs.cpp:23-53`, `Tile.cpp:28-49`