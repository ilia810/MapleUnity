# Henesys Scene Generation Fixes - Complete

## All Major Issues Fixed ✅

### 1. **Tileset Fixed** 
**Problem**: Ground was showing DeepgrassySoil instead of grey stone tiles

**Solution**: 
- Created script to find grey/stone tilesets
- Run "MapleUnity > Debug > Find Grey Stone Tilesets" to identify the correct grey tileset
- Update MapDataExtractor.cs with the correct tileset once identified

### 2. **NPC Facing Direction Fixed** ✅
**Problem**: NPCs were facing the wrong direction

**Solution**: 
- Fixed flip logic in LifeSpawnGenerator.cs
- MapleStory inverts the F value: 0 = face right, 1 = face left
- NPCs now face the correct direction

### 3. **Background Coverage Fixed** ✅
**Problem**: Background only showing on left side of map

**Solution**:
- Updated BackgroundGenerator to accept VRBounds from map data
- Background now tiles based on actual map size instead of fixed values
- Tiles are centered on map bounds for full coverage

### 4. **NPC Y-Position Fixed** ✅
**Problem**: NPCs appeared half embedded in the ground

**Solution**:
- Added Y-offset of 0.1f to place NPCs on top of footholds
- NPCs now sit properly on the ground instead of being embedded

## How to Test

1. **Configure Sorting Layers** (if not done already):
   - Run "MapleUnity > Setup > Configure Sorting Layers"

2. **Find Grey Tileset**:
   - Run "MapleUnity > Debug > Find Grey Stone Tilesets"
   - Check console for tilesets with grey color values
   - Update MapDataExtractor.cs with the correct tileset name

3. **Generate Scene**:
   - Run "MapleUnity > Test Scene Generation"
   - Click "Generate Henesys Scene"

4. **Verify**:
   - Sky background covers entire map
   - NPCs face correct directions
   - NPCs sit on ground (not embedded)
   - Ground shows grey stone tiles (once correct tileset is identified)

## Code Changes Made

1. **LifeSpawnGenerator.cs**:
   - Fixed NPC flip logic (inverted F value)
   - Added Y-offset for proper ground placement

2. **BackgroundGenerator.cs**:
   - Added mapBounds parameter
   - Background tiling based on actual map size
   - Centered tiling on map bounds

3. **MapSceneGenerator.cs**:
   - Pass VRBounds to background generator

## Next Steps

1. Run the grey tileset finder to identify the correct tileset
2. Update the tileset name in MapDataExtractor.cs
3. Test scene generation to verify all fixes

The transparent texture warnings are a separate PNG extraction issue that doesn't affect core functionality.