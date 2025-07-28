# Platform Collision System Implementation

## Summary
Implemented Phase 3 of the MapleStory v83 physics plan: a custom foothold/platform detection system with authentic collision physics.

## Key Changes to `Assets/Scripts/GameLogic/Core/Player.cs`:

### 1. **Fixed GetPlatformBelow() Method**
- Previously always returned null due to incorrect Y comparison logic
- Now properly searches for platforms below the player
- Uses pixel coordinates internally (multiplies by 100)
- Finds the closest platform below the player within 100 pixels (1 unit)
- Filters platforms by type (Normal and OneWay only)
- Returns the highest platform below the player

### 2. **Improved Platform Collision Detection**
- Only checks for collisions when falling (velocity.Y <= 0)
- Uses player's bottom position for accurate foot placement
- Implements line intersection test: checks if player crosses platform between frames
- Snaps player to platform surface when landing
- Triggers landing events and state changes

### 3. **One-way Platform Support**
- Added `droppingThroughPlatform` flag and timer
- `DropThroughPlatform()` method for down+jump input
- Ignores one-way platforms when dropping through (300ms timer)
- Allows jumping through platforms from below (only collides when falling)

### 4. **Slope Handling**
- Uses Platform.GetYAtX() for linear interpolation on slopes
- Continuously adjusts Y position when grounded to stay on slopes
- Smooth transitions between connected platforms

### 5. **Edge Behavior**
- Detects when player walks off platform edges
- Transitions to falling state when no platform below
- No artificial sliding - player sticks to edges until moving off

### 6. **Additional Helper Methods**
- `IsOnOneWayPlatform(MapData)` - Check if on a one-way platform
- `GetCurrentPlatform(MapData)` - Get the platform player is standing on

## Technical Details

### Platform Detection Algorithm:
```
1. Convert player position to pixels (multiply by 100)
2. Get player's bottom Y position (center Y - height/2)
3. For each platform:
   - Check if player X is within platform X range
   - Calculate platform Y at player's X position
   - Find closest platform below player (positive distance)
4. Return platform if within 100 pixels (1 unit)
```

### Collision Resolution:
```
1. Check if falling (velocity.Y <= 0)
2. Get platform below new position
3. Check if player crossed platform:
   - Previous bottom Y >= platform Y
   - New bottom Y <= platform Y
4. If collision:
   - Snap player Y to platform + half height
   - Set velocity.Y to 0
   - Mark as grounded
```

## Testing
- Created comprehensive test suite in `PlatformCollisionTests.cs`
- Tests cover: basic collision, slopes, one-way platforms, edges, multiple platforms
- Created debug tool `TestPlatformCollision.cs` for manual testing in Unity Editor

## Notes
- All collision math uses MapleStory's pixel coordinates internally
- No Unity physics used - pure mathematical line intersection
- Follows TDD principles with failing tests written first
- Maintains platform independence in GameLogic layer