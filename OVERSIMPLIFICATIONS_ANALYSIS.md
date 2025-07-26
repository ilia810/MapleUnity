# Analysis of Oversimplifications in Tile Implementation

After reviewing the C++ client code and comparing it with our Unity implementation, I've identified several areas where we've oversimplified the implementation:

## 1. **Layer-Specific Tileset Handling**

### C++ Client:
```cpp
// MapTilesObjs.cpp line 25
auto tileset = src["info"]["tS"] + ".img";

// But then for each tile (line 48-49):
Tile tile{ tilenode, tileset };
// The Tile constructor checks if tile has its own tS
```

### Our Implementation:
We correctly read layer-specific tilesets but may not handle tile-specific tileset overrides properly.

**Issue**: Individual tiles can override the layer's tileset with their own `tS` property. The C++ client handles this in the Tile constructor.

## 2. **Z-Value Handling**

### C++ Client (Tile.cpp lines 75-78):
```cpp
z = dsrc["z"];
if (z == 0)
    z = dsrc["zM"];
```

### Our Implementation:
We treat `z` and `zM` as separate values and combine them, but C++ uses `zM` only if `z` is 0.

**Issue**: We're using `zM * 1000 + z * 100` but the C++ client uses zM as a fallback, not an addition.

## 3. **Multimap Sorting vs Unity SortingOrder**

### C++ Client:
```cpp
// Uses std::multimap<uint8_t, Tile> tiles
tiles.emplace(z, std::move(tile));
```
The multimap automatically sorts by the key (z value) and allows multiple tiles with the same z.

### Our Implementation:
We calculate a single sortingOrder value, which might cause conflicts when tiles have identical sorting values.

**Issue**: The C++ client's multimap preserves insertion order for tiles with the same z value, but Unity's sorting might not maintain this order consistently.

## 4. **Tile Drawing Position**

### C++ Client (Tile.cpp line 83):
```cpp
texture.draw(pos + viewpos);
```
The position is directly used without any coordinate conversion.

### Our Implementation:
```csharp
Vector3 position = CoordinateConverter.ToUnityPosition(tileData.X, tileData.Y, 0);
```

**Issue**: We might be applying unnecessary coordinate conversions. MapleStory's coordinate system might be simpler than we're treating it.

## 5. **Origin/Pivot Handling**

### C++ Client:
The texture draws using the origin directly as part of the DrawArgument calculation.

### Our Implementation:
We're applying the origin offset manually after creating the sprite with a pivot:
```csharp
float offsetX = (centerX - origin.x) / 100f;
float offsetY = (origin.y - centerY) / 100f;
renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
```

**Issue**: We might be double-applying the origin - once as sprite pivot and again as local position offset.

## 6. **Per-Layer Object Handling**

### C++ Client:
```cpp
// MapTilesObjs has both tiles AND objects per layer
std::multimap<uint8_t, Tile> tiles;
std::multimap<uint8_t, Obj> objs;
```

### Our Implementation:
We only handle tiles, not considering that each layer can have both tiles and objects that need to be sorted together.

## 7. **Texture Origin in Draw**

### C++ Client (DrawArgument.h):
```cpp
Rectangle<int16_t> get_rectangle(Point<int16_t> origin, Point<int16_t> dimensions) const
{
    Point<int16_t> rlt = pos - center - origin;
    // ... calculates rectangle considering origin
}
```

The origin is subtracted from the position during drawing.

### Our Implementation:
We add the origin offset to the local position, which might be the opposite of what's needed.

## Recommended Fixes:

1. **Fix Z-Value Logic**: Change to use zM only when z is 0
2. **Simplify Origin Handling**: Either use pivot OR local offset, not both
3. **Review Coordinate Conversion**: The C++ client doesn't seem to do complex conversions
4. **Handle Tile-Specific Tilesets**: Check each tile for its own tS override
5. **Consider Object Sorting**: Tiles and objects on the same layer need to sort together
6. **Fix Origin Application**: Origin should probably be subtracted, not added

## Most Critical Issue:
The origin/pivot handling is likely the main cause of alignment issues. We're applying it twice and possibly in the wrong direction.