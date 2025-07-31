# MapleUnity Character Rendering Issue Report

## Executive Summary

The MapleUnity project has persistent character rendering issues where body parts are not positioned correctly. Despite multiple attempts to fix the positioning system, the character still displays with misaligned body parts.

## Current Issues

### 1. Body Positioning Problem
- **Symptom**: The body sprite appears at the same height as the head instead of below it
- **Visual**: The body is positioned to the left of the head at approximately the same Y-coordinate
- **Expected**: The body should be at ground level (Y=0) with the head above it

### 2. Arm Positioning Problem
- **Symptom**: The left arm appears near the character's legs/feet
- **Visual**: The arm sprite is rendering at or near Y=0 (ground level)
- **Expected**: The arm should be positioned at mid-body level (approximately Y=0.20)

### 3. Face/Eyes Positioning Problem
- **Symptom**: The eyes/face are positioned too high, appearing near the hair level
- **Visual**: Face features are not properly centered within the head sprite
- **Expected**: Face should be positioned within the head bounds, properly aligned

## Technical Background

### MapleStory's Original Rendering System
Based on research of the C++ client (see `research5.txt`):

1. **Coordinate System**: 
   - MapleStory uses a top-left origin system
   - Y=0 is at the top of the screen, positive Y goes down
   - Character position represents the character's feet

2. **Sprite Origins**:
   - Each sprite has an "origin" point stored in the NX data
   - This origin is an offset from the sprite's top-left corner
   - The origin point determines where the sprite anchors to the character position

3. **Attachment Points**:
   - Body frames contain attachment points (e.g., "head", "navel", "neck")
   - These points indicate where other body parts connect
   - The head attachment point shows where the head sprite should align with the body

4. **Rendering Approach**:
   - All sprites are drawn at the same base position (character's feet)
   - Each sprite's origin determines its visual offset from that position
   - No manual positioning of individual body parts is needed

### Unity Implementation

Current implementation in `MapleCharacterRenderer.cs`:

1. **Sprite Loading**: 
   - Sprites are loaded from NX files via `NXAssetLoader`
   - Origins are extracted and converted to Unity pivots in `SpriteLoader`

2. **Pivot Conversion**:
   - MapleStory origin (top-left based) → Unity pivot (bottom-left based)
   - Current formula: `pivotY = origin.y / texture.height` (after recent fix)
   - Previously was: `pivotY = 1.0f - (origin.y / texture.height)`

3. **GameObject Hierarchy**:
   - Each body part is a separate GameObject with a SpriteRenderer
   - All parts are children of the main character GameObject
   - Parts are created at local position (0,0,0)

## What We've Tried

### Attempt 1: Manual Positioning
- Set head position based on attachment point data
- Set arm position to Y=0.20
- **Result**: Did not fix the issues

### Attempt 2: All Parts at Origin
- Kept all parts at local position (0,0,0)
- Relied on sprite pivots to handle positioning
- **Result**: Body appeared at head height, arm at leg level

### Attempt 3: Fix Pivot Calculation
- Changed pivot Y calculation from inverted to direct
- Removed `1.0f -` from the pivot Y formula
- **Result**: Issues persist

### Attempt 4: Head Attachment Point Usage
- Used head attachment point from body data
- Converted from pixels to Unity units (/100)
- **Result**: No improvement

## Current Understanding

### What We Know:
1. The NX data is being read correctly - we can access sprites and origin data
2. Sprites are being created and rendered - they appear on screen
3. The layering (z-order) is correct - parts render in proper order
4. The character faces the correct direction

### What's Unclear:
1. Whether the sprite pivot calculation is truly correct
2. If the body sprite's origin in the NX data is what we expect
3. Whether there's a fundamental misunderstanding of how MapleStory positions sprites
4. If there's an issue with Unity's coordinate system conversion

## Data Analysis Needed

To properly diagnose this issue, we need to analyze:

1. **Raw NX Data**:
   - The exact origin values for body, arm, and head sprites
   - The head attachment point coordinates
   - Any other attachment points (navel, neck) that might be relevant

2. **Sprite Creation**:
   - Verify the pivot calculations are correct
   - Check if the sprite bounds match expectations
   - Ensure texture dimensions are correct

3. **Runtime Positioning**:
   - The actual world positions of each sprite when rendered
   - The bounds of each sprite in world space
   - How the pivots affect the final positioning

## Hypothesis

The most likely causes of the issue:

1. **Incorrect Pivot Calculation**: The conversion from MapleStory's origin to Unity's pivot may still be wrong
2. **Misunderstood Coordinate System**: MapleStory might use a different approach than we think
3. **Missing Offset Data**: There might be additional offset data in the NX files we're not using
4. **Sprite Import Issue**: The way sprites are created might not preserve the correct anchor points

## Recommendations for Research

1. **Deep Dive into C++ Client**: 
   - Study the exact rendering code in the original client
   - Understand how drawinfo.cpp handles body part positioning
   - Check if there are any transformations we're missing

2. **NX Data Analysis**:
   - Extract and log all origin values for test sprites
   - Compare with expected values from documentation
   - Verify our understanding of the coordinate system

3. **Visual Debugging**:
   - Create debug visualizations showing sprite bounds
   - Display pivot points and origins
   - Show attachment point connections

4. **Alternative Approach**:
   - Consider if we need to calculate offsets differently
   - Check if Unity's SpriteRenderer has settings we're missing
   - Explore whether we need custom positioning logic

## Files to Review

- `Assets/Scripts/GameView/MapleCharacterRenderer.cs` - Main rendering logic
- `Assets/Scripts/GameView/SpriteLoader.cs` - Sprite creation and pivot calculation
- `Assets/Scripts/GameData/NX/NXAssetLoader.cs` - NX data loading
- `research5.txt` - Documentation on MapleStory's rendering system
- Original C++ client source (if available)

## Success Criteria

The character rendering will be considered fixed when:
1. Body appears at ground level (Y=0)
2. Arm appears at mid-body level
3. Head appears above body at appropriate height
4. Face/eyes are properly positioned within head
5. All animations maintain correct positioning

This issue is critical for the MapleUnity project as proper character rendering is fundamental to recreating the MapleStory experience.