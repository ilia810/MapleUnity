# Object Misalignment Issue - Minimal Repomix

## Problem Statement
Objects in MapleUnity (signs, houses, etc.) appear systematically lower than they should when generating Henesys (map 100000000). The offset is proportional to sprite size (sign ~30px, house ~60px). Despite multiple attempts to fix origin loading from NX data, objects still appear misaligned.

## Key Discovery from research3.txt
Origins ARE stored in the NX data at the frame level:
- Guide sign: origin at (91, 55)
- Objects have structure: `Obj/guide.img/common/post/0` where "0" is a container with origin

## Core Issue
Debug output shows:
- Tiles: "Has 'origin' child: True" - origins load successfully
- Objects: "Has 'origin' child: False" - all return origin (0,0)

## Node Structure Difference
```
TILES (working):
Tile/woodMarble.img/edD/1
  ├── origin (child node with Vector2 value)
  └── [image data in same node]

OBJECTS (broken):
Obj/guide.img/common/post/0 (container)
  ├── origin (should be here but not exposed as child)
  └── 0 (child node with actual image data)
```

## What We've Tried
1. Modified LoadSpriteAndOriginFromNode to check container level for origin
2. Added VirtualOriginNode class to create synthetic origin child nodes
3. Used reflection to extract origin from bitmap node properties
4. All attempts compile but objects remain misaligned

## Hypothesis
The reNX library (C# NX reader) may handle node properties differently than the C++ client. The origin property exists on bitmap nodes but isn't being exposed as a child node by the library, and our reflection-based approach isn't finding it.

## Next Steps Needed
1. Debug why VirtualOriginNode creation isn't working (are we finding the origin property?)
2. Examine the actual reNX library code to understand how it exposes node properties
3. Compare with C++ client's NX reading implementation
4. Consider alternative approaches like modifying the reNX library itself

## Key Files
- `SpriteLoader.cs` line 196-391: LoadSpriteAndOriginFromNode method
- `RealNxFile.cs` line 597-621: Bitmap node origin extraction attempt
- `NXDataManagerSingleton.cs` line 163-217: GetObjectSpriteWithOrigin method
- `debug_all.txt`: Shows "Has 'origin' child: False" for all objects