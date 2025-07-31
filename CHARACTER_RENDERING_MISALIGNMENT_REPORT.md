# Character Rendering Misalignment Report

## Executive Summary

Despite implementing the correct C++ client formulas and scale-based flipping, character body parts remain largely misaligned in the Unity implementation. The facing direction now works better (using transform.scale.x = -1), but appears inverted. This report outlines what we understand about the issue and what specific data needs to be logged in both clients to identify the root cause.

## Current State of Implementation

### What We've Implemented Correctly

1. **Scale-based Flipping**
   - Changed from `sprite.flipX` to `transform.localScale.x = -1`
   - Matches C++ client's `xscale = -1` approach
   - Preserves attachment points during flipping

2. **Attachment Point Formulas**
   - Body: navel at origin (0,0)
   - Head: `body.neck - head.neck`
   - Face: `body.neck - head.neck + head.brow`
   - Arm: `body.navel - arm.navel`
   - These formulas are directly from C++ client analysis

3. **Coordinate System Conversion**
   - Y-axis flip implemented (MapleStory Y-down vs Unity Y-up)
   - Division by 100 for unit conversion

### Current Issues

1. **Body Parts Still Misaligned**
   - Despite correct formulas, parts don't align properly
   - Suggests deeper issue with coordinate interpretation or sprite setup

2. **Inverted Facing Direction**
   - Character faces opposite direction than expected
   - Indicates potential issue with default sprite orientation or flip logic

## Hypotheses for Root Causes

### 1. Sprite Origin/Pivot Interpretation
- **Question**: Are we interpreting sprite origins correctly?
- **Possibility**: The pivot calculation in `SpriteLoader.cs` might be incorrect
- **Current formula**: `pivotY = 1.0f - (origin.y / texture.height)`

### 2. Attachment Point Reference Frame
- **Question**: What coordinate system are attachment points defined in?
- **Possibility**: Attachment points might be relative to different reference frames in C++ vs Unity

### 3. Sprite Default Orientation
- **Question**: Do MapleStory sprites face left or right by default?
- **Possibility**: We assume right-facing, but they might be left-facing

### 4. Parent-Child Transform Accumulation
- **Question**: Are transforms accumulating incorrectly?
- **Possibility**: Local vs world space confusion in the hierarchy

### 5. Sprite Bounds vs Texture Bounds
- **Question**: Are attachment points relative to sprite bounds or texture bounds?
- **Possibility**: Mismatch in reference dimensions

## Required Logging for Root Cause Analysis

### C++ Client Logging Requirements

```cpp
// For each body part during rendering:
1. Sprite/Texture Information:
   - Texture dimensions (width, height)
   - Origin point (x, y)
   - Default facing direction of the sprite
   
2. Attachment Points:
   - Raw attachment point values from WZ files
   - Coordinate system (top-left origin? bottom-left?)
   - Whether points are affected by flip state
   
3. Rendering Calculations:
   - Pre-transform position
   - Post-transform position
   - Applied scale (especially xscale)
   - Final screen coordinates
   
4. For Standing Animation Frame 0:
   - Body: texture info, origin, navel attachment point
   - Head: texture info, origin, neck attachment point
   - Face: texture info, origin, brow attachment point
   - Arm: texture info, origin, navel attachment point
   
5. Transformation Order:
   - Exact order of operations (scale, translate, rotate)
   - When attachment offsets are applied
```

### Unity Client Logging Requirements

```csharp
// For each body part during rendering:
1. Sprite Information:
   - Texture dimensions
   - Sprite.pivot (normalized and pixel values)
   - Sprite.bounds
   - Sprite.rect
   
2. Attachment Points:
   - Raw values loaded from NX files
   - Values after any processing
   
3. Transform Information:
   - transform.localPosition (before and after positioning)
   - transform.localScale
   - Parent transform state
   - World position vs local position
   
4. For Standing Animation Frame 0:
   - Same body parts as C++ logging
   - Include GameObject hierarchy structure
   
5. Calculation Steps:
   - Show each step of position calculation
   - Include intermediate values
```

## Specific Test Case

To ensure comparable data, both clients should log the following scenario:

1. **Character Setup**:
   - Skin ID: 0 (light/default)
   - Face ID: 20000
   - Hair ID: 30000
   - No equipment
   - Standing animation, frame 0

2. **States to Log**:
   - Initial load (facing right)
   - After flipping to face left
   - After flipping back to face right

## Key Questions for Researcher

1. **Coordinate System Details**
   - What is the exact coordinate system for attachment points in WZ files?
   - How does the C++ client handle the origin point in relation to attachment points?
   - Are attachment points in texture space or sprite space?

2. **Sprite Setup**
   - How does the C++ client create the sprite equivalent from texture + origin?
   - What is the relationship between origin and the sprite's pivot/anchor?
   - Which direction do sprites face by default in the original data?

3. **Transformation Pipeline**
   - What is the exact transformation order in the C++ client?
   - When are attachment point offsets applied relative to scaling?
   - How does parent-child transformation work in the original client?

4. **Flipping Behavior**
   - What exactly changes when xscale = -1 in the C++ client?
   - Do attachment points get modified or just the rendering?
   - Why might facing direction appear inverted?

## Expected Outcomes

With the logged data from both clients, we should be able to identify:

1. **Exact coordinate system mismatches**
2. **Sprite pivot/origin calculation errors**
3. **Transform order differences**
4. **Reference frame confusion**
5. **Why facing direction appears inverted**

## Implementation Files to Reference

### Unity Files
- `Assets/Scripts/GameView/SpriteLoader.cs` (pivot calculation)
- `Assets/Scripts/GameView/MapleCharacterRenderer.cs` (positioning logic)
- `Assets/Scripts/GameData/NX/NXAssetLoader.cs` (data loading)

### C++ Files (for researcher)
- `BodyDrawInfo.cpp` (rendering logic)
- `Body.cpp` (body part management)
- `Sprite.cpp` (sprite creation)
- Character rendering components

## Conclusion

The misalignment issue appears to be fundamental to how we're interpreting the MapleStory sprite and attachment point system. We need detailed comparative logging to understand:

1. The exact coordinate systems in use
2. How sprite origins relate to pivots/anchors
3. The precise transformation pipeline
4. Why facing direction is inverted

This data will allow us to correct our implementation to exactly match the C++ client's rendering behavior.