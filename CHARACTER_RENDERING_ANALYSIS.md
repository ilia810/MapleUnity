# MapleStory Character Rendering Analysis

## Overview

After analyzing the original C++ MapleStory client source code, I've documented the exact mathematical formulas and logic used for character rendering. This information is crucial for accurately replicating the character positioning in the Unity implementation.

## Core Positioning System

### 1. Body Position (Base Reference)

The body serves as the root position for all other body parts:

```cpp
body_position = bodymap[BODY]["navel"]
```

- The "navel" attachment point is the origin (0,0) for the character
- All other body parts are positioned relative to this point

### 2. Arm Position Calculation

Arms are positioned relative to the body's navel:

```cpp
// If ARM layer exists:
arm_position = armmap["hand"] - armmap["navel"] + bodymap["navel"]

// If only ARM_OVER_HAIR layer exists:
arm_position = arm_over_hair_map["hand"] - arm_over_hair_map["navel"] + bodymap["navel"]
```

**Key insight**: The arm has its own "navel" reference point that must be aligned with the body's navel, then the hand position is calculated from there.

### 3. Head Position Calculation

The head attaches at the neck:

```cpp
head_position = bodymap["neck"] - headmap["neck"]
```

This formula aligns the head's neck point with the body's neck point.

### 4. Face Position Calculation

The face is positioned relative to the head:

```cpp
face_position = bodymap["neck"] - headmap["neck"] + headmap["brow"]
```

The face attaches at the "brow" point on the head.

### 5. Hair Position Calculation

Hair also uses the head's brow point:

```cpp
hair_position = headmap["brow"] - headmap["neck"] + bodymap["neck"]
```

## Attachment Points

Each body part has specific attachment points that are used for positioning:

### Body Attachment Points
- **navel**: Center reference point (origin)
- **neck**: Where the head connects

### Arm Attachment Points
- **navel**: Reference point that aligns with body navel
- **hand**: Where weapons and gloves attach

### Head Attachment Points
- **neck**: Connects to body's neck
- **brow**: Where face and hair attach

### Hand Attachment Points
- **handMove**: Dynamic position for hand animations

## Drawing Order (Z-Ordering)

The drawing order is critical for proper layering:

1. Hair (BELOWBODY layer)
2. Cape
3. Shield (SHIELD_BELOW_BODY)
4. Weapon (WEAPON_BELOW_BODY)
5. Hat (CAP_BELOW_BODY)
6. **Body (BODY layer)** - The main body sprite
7. Gloves (WRIST_OVER_BODY, GLOVE_OVER_BODY)
8. Shoes
9. **Arm (ARM_BELOW_HEAD)**
10. Clothing (Pants/Top or Overall)
11. Arm (ARM_BELOW_HEAD_OVER_MAIL)
12. Shield (SHIELD_OVER_HAIR)
13. Earrings
14. **Head**
15. **Hair (SHADE, DEFAULT layers)**
16. **Face**
17. Face accessories
18. Eye accessories
19. Hat/Hair (varies by cap type)
20. Weapon layers
21. Arm/Mail (order depends on weapon type)
22. Final glove/weapon/hand layers

## Sprite Origin Handling

Each sprite has an origin point that affects its final position:

```cpp
final_position = calculated_position - sprite_origin
```

The sprite origin is typically stored in the WZ/NX file data and represents the pivot point of the sprite.

## Coordinate Transformations

### Flipping (Horizontal Mirroring)
- Applied using negative X scale (-1.0)
- Affects all child positions

### Position Accumulation
```cpp
child_position = parent_position + local_offset
```

### DrawArgument Composition
The DrawArgument class accumulates transformations:
```cpp
DrawArgument faceargs = args + DrawArgument{ faceshift, false, Point<int16_t>(0, 0) };
```

## Example Calculation Flow

For a character standing at position (500, 300):

1. **Body**: Drawn at (500, 300) - body_sprite_origin
2. **Head**: 
   - shift = body_neck - head_neck
   - position = (500, 300) + shift - head_sprite_origin
3. **Face**:
   - shift = body_neck - head_neck + head_brow
   - position = (500, 300) + shift - face_sprite_origin
4. **Arm**:
   - shift = arm_hand - arm_navel + body_navel
   - position = (500, 300) + shift - arm_sprite_origin

## Implementation Guidelines for Unity

1. **Maintain the same attachment point system**: Each body part prefab should have named attachment points (navel, neck, hand, etc.)

2. **Use the exact same formulas**: Don't try to simplify or "improve" the positioning math - it needs to match exactly

3. **Respect the drawing order**: Use Unity's sorting layers or Z-positions to maintain the exact same layering

4. **Handle sprite origins correctly**: Unity sprites have pivots - these need to match the original sprite origins

5. **Test with known values**: Use specific equipment IDs and stances to compare Unity output with the original client

## Critical Implementation Details

1. **Two-handed weapon handling**: The arm drawing order changes based on weapon type
2. **Cap types affect hair visibility**: Different cap types (NONE, HEADBAND, HALFCOVER, FULLCOVER) change hair rendering
3. **Climbing stances**: Have a completely different drawing order
4. **Expression system**: Face has its own animation frames independent of body stance

## Debugging Recommendations

To verify correct implementation in Unity:

1. Log all attachment point positions
2. Log calculated shifts for each body part
3. Compare final positions with original client
4. Use visual debugging to show attachment points
5. Test with multiple stances and equipment combinations