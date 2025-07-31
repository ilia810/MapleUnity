# Sprite Origins and Positioning Analysis Report

## Issue Identified

The arm sprite appears at the wrong position (leg level) because **sprite renderer positions are never set correctly** in the MapleCharacterRenderer.

## Current Implementation

1. All sprite renderers are created at `localPosition = Vector3.zero` in the `CreateSpriteLayer` method
2. No code exists to position body parts at their correct offsets
3. The arm should be at Y=0.20 but is created at Y=0.00

## Expected Positions (MapleStory Standard)

Based on the MapleStory client and the expected layout:
- **Body**: Y = 0.00 (at ground level)
- **Arm**: Y = 0.20 (20 pixels up, mid-body level)
- **Head**: Y = ~0.28 (or from body's head attachment point)
- **Face**: Same as head position

## Code Analysis

In `MapleCharacterRenderer.cs`:
```csharp
// Current CreateSpriteLayer method:
layerObj.transform.localPosition = Vector3.zero; // All parts at Y=0!
```

## Missing Implementation

The code needs to:
1. Set arm position to `new Vector3(0, 0.2f, 0)` after creation
2. Read head attachment point from body sprite data
3. Position head and face at the attachment point

## Why The Pivot/Origin Data Isn't Helping

The sprite pivots are correctly loaded from NX data and properly position each sprite relative to its own center. However, the GameObjects themselves need to be positioned at different heights to match the character's anatomy.

## Solution Required

Add proper positioning after sprite renderer creation:
```csharp
// After creating sprite renderers:
armRenderer.transform.localPosition = new Vector3(0, 0.2f, 0);
// Head position from attachment point or default
headRenderer.transform.localPosition = new Vector3(0, 0.28f, 0);
faceRenderer.transform.localPosition = new Vector3(0, 0.28f, 0);
```