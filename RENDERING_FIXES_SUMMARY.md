# MapleUnity Rendering Fixes Summary

## Issues Fixed

### 1. Sorting Layer Configuration
- **Fixed**: Updated all generators to use proper sorting layers instead of "Default"
- **Background**: Uses "Background" layer for backgrounds, "Foreground" for foregrounds
- **Tiles**: Uses "Tiles" sorting layer
- **Objects**: Uses "Objects" sorting layer
- **NPCs**: Already using "NPCs" sorting layer correctly

### 2. Background Tiling Optimization
- **Created**: `BackgroundTilePool.cs` - Optimized object pooling system
- **Fixed**: ViewportBackgroundLayer now only updates when camera moves significantly (0.5 units threshold)
- **Fixed**: Tiles are pooled and reused instead of constantly destroyed/recreated
- **Result**: Eliminates constant enable/disable cycles that cause flickering

### 3. Sorting Order Fixes
- **Fixed**: Removed +500 offset from tile sorting orders (now uses +256)
- **Fixed**: All sprites now stay at Z=0 position to prevent Z-fighting
- **Fixed**: Objects no longer use Z position for depth (-objData.Z * 0.01f removed)

### 4. Camera Configuration
- **Fixed**: Camera orthographic size set to 3.84 for MapleStory's 1024x768 viewport
- **Fixed**: Near/far clip planes set to -100/100 for proper 2D rendering
- **Fixed**: Camera Y offset removed (was 2, now 0) for accurate positioning
- **Added**: Light blue background color for sky

### 5. Additional Improvements
- **Created**: `RenderingConfiguration.cs` - Centralized rendering settings
- **Created**: `RenderingDebugWindow.cs` - Editor tool to monitor rendering issues
- **Optimized**: Background manager only tracks significant camera movements

## Key Changes by File

### BackgroundGenerator.cs
- Uses proper "Background"/"Foreground" sorting layers
- Implements update threshold to prevent constant updates
- Integrates with BackgroundTilePool for efficient tile management

### TileGenerator.cs
- Uses "Tiles" sorting layer
- Reduced tile offset from +500 to +256
- Maintains proper depth ordering with objects

### ObjectGenerator.cs
- Uses "Objects" sorting layer
- Removed Z-position manipulation (all sprites at Z=0)
- Maintains sorting order based on layer and z values only

### CameraController.cs
- Proper orthographic size (3.84) for 1024x768 viewport
- Configured clip planes for 2D rendering
- Removed Y offset for accurate positioning

## Usage

The rendering system now follows this hierarchy:
1. **Background** (sortingOrder: -1000 + layer*10)
2. **Tiles** (sortingOrder: layer*1000 + z + 256)
3. **Objects** (sortingOrder: layer*1000 + z)
4. **NPCs** (sortingOrder: based on Y position)
5. **Foreground** (sortingOrder: 10000 + layer*10)

All flickering should be eliminated with proper pooling and update thresholds.