# MapleUnity Physics Implementation Report

## Executive Summary
The current MapleUnity implementation has a working but simplified physics system that differs significantly from the original MapleStory v83 C++ client. While basic movement and collision detection work, the physics feel and behavior do not match the authentic MapleStory experience.

## Current Implementation

### 1. Player System Architecture
- **SimplePlayerController.cs**: Unity-based physics controller using Rigidbody2D
  - Basic WASD/Arrow key movement
  - Simple jump with Space/Alt
  - Gravity scale: 2.0
  - Movement speed: 5 units/second
  - Jump force: 10 units
  - Blue rectangle sprite (30x60 pixels)

### 2. What Was Replaced
The original implementation attempted to use:
- **Player.cs** (GameLogic): Custom MapleStory physics simulation
- **PlayerView.cs** (GameView): Unity GameObject bridge
- **MapleCharacterRenderer.cs**: Complex 12+ body part rendering system
- **MaplePhysics.cs**: Constants attempting to match v83 physics

These were replaced due to:
- Platform collision detection completely broken (player fell through everything)
- Body parts rendering independently from player position
- Camera following invisible falling player instead of visible character
- Complex cross-assembly architecture issues

### 3. Current Working Features
- ✅ Player spawns at correct position
- ✅ Basic movement left/right
- ✅ Jumping and landing on platforms
- ✅ Camera follows player
- ✅ Platform collision detection
- ✅ Map generation from NX files

### 4. What's Missing vs Original MapleStory

#### Physics Differences
1. **Movement Acceleration/Deceleration**
   - Current: Instant start/stop
   - Original: Gradual acceleration with momentum

2. **Jump Mechanics**
   - Current: Simple upward force
   - Original: Complex jump states with different physics during ascent/descent

3. **Air Movement**
   - Current: Same speed as ground movement
   - Original: Different air control and momentum preservation

4. **Platform Interaction**
   - Current: Basic collision detection
   - Original: One-way platforms, slopes, different platform types

#### Rendering Differences
1. **Character Sprite**
   - Current: Blue rectangle placeholder
   - Original: Multi-layered character with equipment, animations

2. **Animation System**
   - Current: None
   - Original: State-based animations (walk, jump, stand, etc.)

## Original MapleStory v83 Physics Constants
From the attempted MaplePhysics.cs implementation:
```csharp
- Walk Speed: 1.25 units/s (125 pixels/s at 100% speed)
- Jump Speed: 5.55 units/s (555 pixels/s)
- Gravity: 20 units/s² (2000 pixels/s²)
- Max Fall Speed: 6.7 units/s (670 pixels/s)
- Walk Force: 1.4 units/s²
- Walk Drag: 80
- Slip Force: 0.05
- Slip Drag: 0.9
```

## Key Technical Challenges

### 1. Coordinate System Mismatch
- MapleStory uses pixel-based coordinates (100 pixels = 1 Unity unit)
- Y-axis handling differs between systems
- Platform data from NX files needs proper conversion

### 2. Custom Physics vs Unity Physics
- MapleStory uses frame-based custom physics simulation
- Unity uses continuous physics with Rigidbody2D
- Difficult to replicate exact feel with Unity's physics engine

### 3. Architecture Constraints
- GameLogic layer is platform-agnostic (no Unity dependencies)
- GameView layer handles Unity-specific rendering
- Cross-assembly communication complicates physics integration

## Recommendations for Accurate Physics

### Option 1: Port C++ Physics Code
- Extract physics calculations from C++ client
- Implement custom physics in Unity (bypass Rigidbody2D)
- Use FixedUpdate for frame-perfect simulation
- Maintain MapleStory's integer-based position system

### Option 2: Carefully Tuned Unity Physics
- Study C++ client frame data
- Adjust Rigidbody2D parameters to match
- Custom movement controller with acceleration curves
- Physics materials for different surface types

### Option 3: Hybrid Approach
- Use Unity physics for collision detection only
- Custom movement calculations based on C++ logic
- Interpolate between physics frames for smooth rendering

## Next Steps for Research

1. **Analyze C++ Client Physics**
   - Extract exact formulas for movement, jumping, gravity
   - Document state machine for player movement
   - Capture frame data for acceleration/deceleration curves

2. **Study Platform System**
   - Foothold/platform data structure
   - One-way platform implementation
   - Slope handling algorithms

3. **Character State Machine**
   - Movement states and transitions
   - Animation triggers and timing
   - Input buffering and responsiveness

4. **Equipment and Rendering**
   - Body part layering system
   - Equipment sprite compositing
   - Animation frame synchronization

## Files to Reference

### Current Working Implementation
- `/Assets/Scripts/GameView/SimplePlayerController.cs` - Current player controller
- `/Assets/Scripts/GameView/SimpleCameraFollow.cs` - Camera system
- `/Assets/Scripts/SceneGeneration/MapSceneGenerator.cs` - Map generation

### Original Attempted Implementation
- `/Assets/Scripts/GameLogic/Core/Player.cs` - Original physics attempt
- `/Assets/Scripts/GameLogic/MaplePhysics.cs` - Physics constants
- `/Assets/Scripts/GameView/MapleCharacterRenderer.cs` - Character rendering
- `/PLAYER_ISSUES_RESEARCH_REPORT.md` - Detailed issue documentation

### C++ Client Reference
- `C:\HeavenClient\MapleStory-Client\` - Original client source
- Movement physics likely in player/character classes
- Platform collision in foothold/physics systems

## Conclusion
While the current implementation provides functional gameplay, achieving authentic MapleStory physics requires deeper integration with the original game's physics engine. The simplified Unity physics approach works but lacks the precise feel and mechanics that MapleStory players expect. A comprehensive physics port or careful recreation of the C++ client's movement system is needed for authenticity.