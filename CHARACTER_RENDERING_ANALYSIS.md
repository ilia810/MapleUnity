# MapleStory C++ Client Character Rendering Analysis

## Overview
Based on my analysis of the C++ MapleStory client code, here's how character rendering works in the original implementation.

## Key Components

### 1. Attachment Points System (BodyDrawInfo.cpp)
The character rendering system uses an attachment point system where body parts are positioned relative to each other:

- **Body Origin**: The body sprite's origin is at the "navel" attachment point
- **Head Attachment**: The head is positioned using `body.neck - head.neck`
- **Face Attachment**: The face is positioned using `body.neck - head.neck + head.brow`

### 2. Character Flipping Mechanism
When a character changes direction:
- `CharLook::set_direction(bool flip)` is called
- The `flip` boolean is stored as a member variable
- When drawing, `DrawArgument` is created with this flip value
- If `flip == true`, the x-scale becomes -1.0, flipping sprites horizontally

### 3. Drawing Process (CharLook::draw)
The drawing happens in layers, from back to front:

For standing poses:
1. Hair (below body layer)
2. Cape
3. Shield/Weapon (below body)
4. **Body** (main torso sprite)
5. Arms
6. Equipment (pants, top, gloves, shoes)
7. **Head** 
8. Hair (default and overhead layers)
9. **Face** (with expression)
10. Accessories (hat, earrings, etc.)
11. Weapon (over body layers)

### 4. Position Calculations

#### Body Position
- Drawn at character position + body attachment offset
- Body attachment offset is typically (0, 0) for the navel point

#### Head Position  
```cpp
head_position = body_neck - head_neck
```
Where:
- `body_neck`: Neck position in body sprite (e.g., (-7, -31) from navel)
- `head_neck`: Neck position in head sprite (e.g., (14, 19))

#### Face Position
```cpp
face_position = body_neck - head_neck + head_brow
```
Where:
- `head_brow`: Brow position in head sprite (e.g., (13, 9))

### 5. Sprite Drawing (Animation::draw)
Each sprite is drawn with:
- **Position**: Character position + attachment offset
- **Origin**: The sprite's origin point (pivot)
- **Scale**: xscale = -1 when flipped, 1 otherwise

The final rendering position is calculated as:
```cpp
render_position = position - origin * scale
```

### 6. DrawArgument Structure
The DrawArgument class encapsulates:
- `pos`: The position to draw at
- `center`: The center point for transformations
- `xscale/yscale`: Scale factors (xscale = -1 for flipping)
- `angle`: Rotation angle
- `color/opacity`: Color and transparency

## Key Insights for Unity Implementation

1. **Attachment Point Matching**: Unity implementation must use the same attachment point calculations
2. **Flip Handling**: Use negative x-scale rather than rotating sprites
3. **Layer Ordering**: Maintain the exact same drawing order for proper overlapping
4. **Origin Points**: Sprite pivots must match the original sprite origins
5. **Position Math**: All position calculations must match exactly, including the subtraction operations

## Example Values (stand1, frame 0)
Typical attachment points from v83 data:
- body.navel: (0, 0)
- body.neck: (-7, -31) 
- head.neck: (14, 19)
- head.brow: (13, 9)

Calculated positions:
- head_position = (-7, -31) - (14, 19) = (-21, -50)
- face_position = (-21, -50) + (13, 9) = (-8, -41)

## Unity Implementation Requirements

1. **Preserve Attachment Points**: Load and use the exact attachment point data from NX files
2. **Implement Proper Flipping**: Use Transform.localScale.x = -1 for flipping
3. **Correct Pivot Points**: Set sprite pivots to match original origins
4. **Layer Management**: Use sorting layers or Z-positions to maintain draw order
5. **Exact Math**: Ensure all vector math operations match the C++ implementation

The key to accurate rendering is maintaining the exact same mathematical relationships between body parts as defined in the original attachment point system.