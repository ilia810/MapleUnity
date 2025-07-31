# Character Rendering C++ Formulas Implementation

## Summary
The MapleCharacterRenderer has been updated to match the exact character rendering formulas from the original C++ MapleStory client.

## Implemented Formulas

### 1. Body Positioning
**C++ Formula**: Body's navel is positioned at (0,0)
```csharp
// Move body so its navel is at (0,0)
Vector3 bodyOffset = new Vector3(
    -bodyNavel.x / 100f,  // Negative to move navel TO origin
    bodyNavel.y / 100f,   // Positive Y because MapleStory Y goes down
    0
);
bodyRenderer.transform.localPosition = bodyOffset;
```

### 2. Arm Positioning
**C++ Formula**: `shift = body.navel - arm.navel`
```csharp
// Aligns arm's navel (shoulder) to body's navel
Vector3 armPosition = new Vector3(
    (bodyNavel.x - armNavel.x) / 100f,
    -(bodyNavel.y - armNavel.y) / 100f, // Flip Y for Unity
    0
);
armRenderer.transform.localPosition = armPosition;
```

### 3. Head Positioning
**C++ Formula**: `headPos = body.neck - head.neck`
```csharp
// Aligns head's neck to body's neck
Vector3 headPosition = new Vector3(
    (bodyNeck.x - headNeck.x) / 100f,
    -(bodyNeck.y - headNeck.y) / 100f, // Flip Y for Unity
    0
);
```

### 4. Face Positioning
**C++ Formula**: `facePos = body.neck - head.neck + head.brow`
```csharp
// Face is positioned at head position + head's brow offset
Vector3 facePosition = headPosition + new Vector3(
    headBrow.x / 100f,
    -headBrow.y / 100f,  // Flip Y for Unity
    0
);
```

### 5. Hair Positioning
Uses the same formula as face (positioned at head's brow point).

## Key Changes Made

1. **Updated ApplyAttachmentOffsets()**: Now implements the exact C++ formulas instead of approximations
2. **Body Origin Correction**: The body sprite is now offset so its navel attachment point is at (0,0)
3. **Proper Coordinate Conversion**: Correctly handles MapleStory's top-down Y axis vs Unity's bottom-up Y axis
4. **Comprehensive Debug Logging**: Added detailed logging to verify each formula's application

## Testing Status

- ✅ Formulas correctly implemented in code
- ✅ Coordinate system conversion handled properly
- ✅ Unit conversion (pixels to Unity units) implemented
- ⚠️ Runtime testing pending due to compilation errors in test scripts
- ⚠️ Actual sprite alignment needs visual verification once tests can run

## Next Steps

1. Fix compilation errors in test scripts to enable runtime testing
2. Visually verify character parts align correctly in-game
3. Test with different animations (walk, jump, etc.) to ensure formulas work across all states
4. Fine-tune if any visual adjustments are needed

## Implementation Location
`Assets/Scripts/GameView/MapleCharacterRenderer.cs` - Lines 367-473 (ApplyAttachmentOffsets method)