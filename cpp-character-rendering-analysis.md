# MapleStory C++ Client Character Rendering Analysis

## Key Concepts and Logic

### 1. **Sprite Flipping**
The C++ client handles facing direction through the `flip` parameter in `DrawArgument`:
- `facing_right` boolean is stored in the `Char` class
- When drawing, it's passed as part of `DrawArgument(position, flip)`
- The flip is implemented as `xscale = flip ? -1.0f : 1.0f` in DrawArgument constructor
- This means flipping is done by scaling the X axis by -1, NOT by changing attachment points

### 2. **Transformation Order**
From `DrawArgument::get_rectangle()` and the drawing flow:
1. **Origin adjustment**: `pos - center - origin`
2. **Scale application**: `xscale * (coordinate)`
3. **Position translation**: Final position applied
4. **The center point is preserved during scaling**

The key insight: When flipped (xscale = -1), the sprite is mirrored around its center point, not around (0,0).

### 3. **Coordinate System**
- Attachment points are calculated in `BodyDrawInfo::init()` using map data from body parts
- These positions are **relative offsets** between attachment points (e.g., neck-to-brow for face)
- The coordinate system uses these formulas:
  ```cpp
  face_positions = body["neck"] - head["neck"] + head["brow"]
  arm_positions = arm["hand"] - arm["navel"] + body["navel"]
  ```
- **Critical**: Attachment points are NOT recalculated when flipping - they remain the same

### 4. **Body Part Hierarchy**
From `CharLook::draw()`, the rendering order (non-climbing stance):
1. Hair below body layer
2. Cape, shield, weapon (below body)
3. **Body** (the main torso)
4. Arms below head
5. Pants/Top (clothing layers)
6. Head
7. **Face** (uses faceshift offset)
8. Hair (default layer)
9. Arms over hair
10. Hands and weapons over body

**Key insight**: Face gets special positioning with `faceshift` calculated from body positions.

### 5. **Special Cases**
- **Face positioning**: Uses `drawinfo.getfacepos()` to get a special offset
- **Eyes are part of Face**: Not rendered separately - they're baked into the face sprite
- **Arm positioning**: Different logic for `arm` vs `armOverHair` layers
- **Two-handed weapons**: Changes the draw order of arm vs weapon

### 6. **Animation States**
Different stances affect:
- Which body parts are visible
- The attachment point calculations (each frame has its own map data)
- Special handling for climbing (completely different draw order)
- Crouching would use specific frame data with adjusted attachment points

## Critical Differences from Unity Implementation

1. **Flipping is done via scale**, not by moving body parts or changing positions
2. **Attachment points remain constant** - they don't change with facing direction
3. **Eyes are not separate** - they're part of the face sprite
4. **Center-based scaling** - sprites flip around their center, not origin
5. **Face uses special offset calculation** separate from other body parts

## Recommendations for Unity Implementation

1. Implement flipping using `transform.localScale.x = -1` instead of moving sprites
2. Keep attachment points constant regardless of facing direction
3. Ensure face positioning uses the special face offset calculation
4. Don't render eyes separately - they should be part of the face sprite
5. Apply transformations in the correct order: origin adjustment → scale → position