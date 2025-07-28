# Object Alignment Debug Instructions

## Unity Client Debug

### 1. Debug Object Alignment
- Open Unity Editor
- Go to menu: `MapleUnity > Debug > Debug Object Alignment`
- Click "Debug Object Alignment" button
- Check console output and `object_alignment_debug.log` file

This will show:
- Node structure for each object
- Whether origin child is found
- Origin values retrieved
- If VirtualOriginNode is being used

### 2. Debug Scene Object Placement
- Go to menu: `MapleUnity > Debug > Debug Scene Object Placement`
- Enter Map ID (default: 100000000 for Henesys)
- Click "Debug Object Placement in Scene"
- Check console output and `scene_object_placement_debug.log` file

This will show:
- Actual object positions in the map
- Origins loaded for each object
- Calculated Unity positions with offsets

## C++ Client Debug

### 1. Build with Debug Logging
The following files have been modified with debug logging:
- `Gameplay/MapleMap/Obj.cpp` - Logs object loading and positions
- `Graphics/Texture.cpp` - Logs origin detection for object textures

### 2. Run with Debug Level
Make sure `LOG_LEVEL` is set to `LOG_DEBUG` in the build configuration.

### 3. Check Console Output
Look for these debug tags:
- `[OBJ DEBUG]` - Object loading information
- `[TEXTURE DEBUG]` - Origin detection in textures
- `[OBJ DRAW]` - Final drawing positions

## What to Compare

### Key Differences to Look For:

1. **Origin Detection**
   - Unity: Check if "Has 'origin' child: true/false"
   - C++: Check "[TEXTURE DEBUG] Has origin child: true/false"

2. **Origin Values**
   - Unity: "Origin returned: (x, y)"
   - C++: "[TEXTURE DEBUG] Origin: (x, y)"

3. **Object Positions**
   - Unity: "Unity position (calculated): (x, y)"
   - C++: "[OBJ DEBUG] Position: (x, y)" and "[OBJ DRAW] Drawing object at screen pos: (x, y)"

## Expected Results

For the guide sign (Obj/guide.img/common/post/0):
- Origin should be: (91, 55)
- If Unity shows (0, 0) but C++ shows (91, 55), the VirtualOriginNode isn't working

## Troubleshooting

If origins are still (0, 0) in Unity:
1. Check if VirtualOriginNode is being created
2. Verify the node type is detected as bitmap/image
3. Check if the reflection is finding origin properties
4. Compare node structure between Unity and C++ logs