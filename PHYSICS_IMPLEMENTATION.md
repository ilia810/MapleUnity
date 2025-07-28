# MapleStory v83 Physics Implementation in MapleUnity

## Overview
This document describes the authentic MapleStory v83 physics implementation completed for MapleUnity. The physics system has been implemented in the GameLogic layer to ensure platform independence and accurate simulation of the original game's movement mechanics.

## Implemented Features

### 1. Horizontal Movement System
- **Walk Speed**: 125 units/second (1.25 Unity units/s) at 100% speed stat
- **Acceleration**: 1000 units/second² (10 Unity units/s²)
- **Deceleration/Friction**: 1000 units/second² (10 Unity units/s²) - only applies on ground
- **Air Control**: 80% of ground acceleration when airborne
- **No Air Friction**: Momentum is preserved while jumping

Key implementation details:
- Movement uses gradual acceleration instead of instant velocity changes
- Character takes ~125ms to reach full speed from standstill
- Character takes ~125ms to stop from full speed
- Direction changes mid-air are possible but require overcoming existing momentum

### 2. Jump Mechanics
- **Initial Jump Velocity**: 555 units/second (5.55 Unity units/s) at 100% jump stat
- **Jump Height**: Approximately 0.77 units
- **Jump Duration**: ~0.82 seconds (0.27s to peak, 0.55s to fall)
- **Jump Key Behavior**: Must release and re-press to jump again (no bunny hopping)
- **Immediate Landing Jump**: Can jump immediately upon landing

Key implementation details:
- Jump velocity is fixed at the moment of jumping (no variable height)
- Jump key state is tracked to prevent continuous jumping
- `Player.ReleaseJump()` method must be called when jump key is released

### 3. Gravity System
- **Gravity Acceleration**: 2000 units/second² (20 Unity units/s²)
- **Terminal Velocity**: 750 units/second (7.5 Unity units/s)
- **Crisp Physics**: No floaty feeling - gravity creates authentic MapleStory fall curves

### 4. State Machine
The player state machine correctly handles transitions between:
- **Standing**: Default idle state when on ground
- **Walking**: Active when moving horizontally on ground
- **Jumping**: Active during entire airborne period (both ascending and descending)
- **Climbing**: Special state for ladder/rope movement (gravity disabled)
- **Crouching**: Prevents movement and jumping

State transitions are automatic based on physics state and input.

## Code Architecture

### Core Classes Modified

1. **MaplePhysics.cs**
   - Updated physics constants to match MapleStory v83 exactly
   - Added proper acceleration methods with ground/air distinction
   - Friction only applies when grounded

2. **Player.cs**
   - Implemented `UpdateHorizontalVelocity()` with proper acceleration/friction
   - Added jump key tracking with `jumpKeyPressed` flag
   - Updated `UpdatePhysics()` to apply horizontal physics every frame
   - Added `ReleaseJump()` method for jump key management

### Key Methods

```csharp
// Apply movement with acceleration
player.MoveLeft(true/false);
player.MoveRight(true/false);

// Jump with proper key tracking
player.Jump();        // Press jump key
player.ReleaseJump(); // Release jump key (required for next jump)

// Physics update (called at 60 FPS)
player.UpdatePhysics(1f/60f, mapData);
```

## Integration with Unity

The physics system is designed to work with Unity's fixed timestep:

1. Set Unity's Fixed Timestep to 0.01667 (60 FPS) in Time settings
2. Call `player.UpdatePhysics(Time.fixedDeltaTime, mapData)` in FixedUpdate
3. Apply the resulting position to the GameObject transform
4. Handle input in Update() but apply it through the Player methods

Example Unity integration:
```csharp
void Update()
{
    // Handle input
    if (Input.GetKeyDown(KeyCode.Space))
        player.Jump();
    if (Input.GetKeyUp(KeyCode.Space))
        player.ReleaseJump();
        
    player.MoveLeft(Input.GetKey(KeyCode.LeftArrow));
    player.MoveRight(Input.GetKey(KeyCode.RightArrow));
}

void FixedUpdate()
{
    // Update physics
    player.UpdatePhysics(Time.fixedDeltaTime, currentMapData);
    
    // Apply position to Unity transform
    transform.position = new Vector3(player.Position.X, player.Position.Y, 0);
}
```

## Testing

Comprehensive unit tests have been added to verify:
- Acceleration and deceleration rates
- Jump velocities and trajectories
- Gravity application and terminal velocity
- State machine transitions
- Air control behavior

Tests can be found in:
- `Assets/Scripts/Tests/GameLogic/PlayerTests.cs` (basic physics tests)
- `Assets/Scripts/Tests/GameLogic/PlayerMovementPhysicsTests.cs` (comprehensive physics validation)

## Platform Collision
The existing platform collision system continues to work with the new physics:
- Platforms are checked when falling (velocity.Y <= 0)
- One-way platforms work correctly (can jump through from below)
- Landing detection snaps player to platform surface
- Slope support via GetYAtX() interpolation

## Next Steps
With authentic physics now implemented, the following features can be built on this foundation:
- Double jump and flash jump skills
- Knockback physics (already defined in MaplePhysics)
- Ice/slippery surface physics
- Swimming physics (constants already defined)
- Flying mount physics

The physics system is now ready for integration with the Unity GameView layer while maintaining the authentic MapleStory v83 feel that players expect.