# Background and Tile Fixes

## Issues Fixed

### 1. Sky Background Rendering Order - FIXED
**Problem**: Sky background was rendering above ground tiles

**Solution**: 
- Modified `BackgroundGenerator.cs` to set type 3 backgrounds to sorting order -500
- This ensures sky renders behind everything else
- Based on C++ client: backgrounds render first, then objects in layers 0-7, then foregrounds

### 2. Henesys Floor Tiles - FIXED  
**Problem**: Using grassySoil instead of brick tiles

**Solution**:
- Changed default tileset from "grassySoil" to "brownBrick" for Henesys
- brownBrick.img has all required variants (bsc, edD, enH0)
- Matches user's memory of Henesys having brick flooring

## Rendering Order (from back to front)

1. **Sky/Far Backgrounds** (sorting order -500)
   - Type 3 backgrounds (tiled sky)
   
2. **Regular Backgrounds** (sorting order -100 to -99)
   - Type 0, 1, 2, 4+ backgrounds
   
3. **Tiles** (sorting layer "Tiles")
   - Ground tiles (brownBrick for Henesys)
   
4. **Objects** (sorting layer "Objects") 
   - Buildings, decorations, etc.
   - Sorted by layer (0-7) and Y position
   
5. **NPCs** (sorting layer "NPCs")
   - Sorted by negative Y position
   
6. **Foregrounds** (sorting order 100+)
   - Front flag backgrounds

## Testing

Run "MapleUnity > Test Scene Generation" and verify:
- Sky blue background is behind everything
- Ground shows brown brick tiles, not grass/soil
- Objects render above tiles
- NPCs render in front of buildings

## Notes from C++ Client

- Background types:
  - 0: NORMAL (static)
  - 1: HTILED (horizontal tiling)
  - 2: VTILED (vertical tiling)  
  - 3: TILED (both directions)
  - 4-7: Parallax variants

- Tileset is determined by:
  1. Map's info/tS value (if present)
  2. Inferred from map ID/location
  3. Henesys doesn't have tS value, so we infer brownBrick