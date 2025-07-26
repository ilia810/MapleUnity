# Analysis of Remaining Tile Issues

## Observed Problems

1. **Bottom parts (enH1) are separated from tiles above**
   - Example: `Tile_L1_woodMarble_enH1_3`
   - These are horizontal end pieces that should connect seamlessly

2. **Base tiles (bsc) have incorrect sorting**
   - Example: `Tile_L1_woodMarble_bsc_1` with sorting order -29672
   - This is hiding grass edges that should be on top

## Root Cause Analysis

### 1. Origin Application is Inverted

In the C++ client:
```cpp
// DrawArgument.h line 134
Point<int16_t> rlt = pos - center - origin;
```

The origin is SUBTRACTED from the position. This means:
- A tile with origin (0, 30) renders 30 pixels HIGHER than its position
- A tile with origin (45, 0) renders 45 pixels to the LEFT of its position

Our implementation creates sprites with the origin as the pivot, but this doesn't produce the same result as subtracting the origin from the draw position.

### 2. Sorting Order Calculation Issues

The sorting order -29672 suggests our calculation is producing negative values, which shouldn't happen. Let's trace this:

For Layer 1, bsc variant:
- layerPriority = (7 - 1) * 1000000 = 6,000,000
- If z = 0 and zM = 0, then actualZ = 0
- depthPriority = 0 * 1000 = 0
- tiebreaker = -Y

If we're getting -29672, the Y position must be around 6,029,672, which is impossibly large. This suggests:
1. Our Y values are not being read correctly, OR
2. The coordinate system is different than expected

### 3. Layer and Z-Value Misunderstanding

The C++ client uses:
- `std::multimap<uint8_t, Tile>` where the key is the z value
- Tiles are drawn in the order they appear in the multimap
- Within the same z value, insertion order is preserved

Our implementation may be over-complicating this with layer priorities.

## Key Differences to Fix

1. **Origin Handling**: We need to subtract origin from position, not use it as a pivot
2. **Sorting Simplification**: Use only the actual z value for sorting within a layer
3. **Coordinate System**: Verify our coordinate reading and conversion

## The Real Issue

Looking at the C++ code more carefully:
- Tiles store their position directly
- Origin is subtracted during drawing
- Z values are used directly for sorting (no complex calculations)
- Layers are drawn in order (0-7), with tiles within each layer sorted by z

Our implementation has oversimplified the pivot system but overcomplicated the sorting system.