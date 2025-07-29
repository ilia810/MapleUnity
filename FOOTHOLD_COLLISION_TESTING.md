# Foothold Collision Testing Documentation

## Overview
This document describes the comprehensive testing infrastructure for the foothold collision system in MapleUnity.

## Test Components Created

### 1. Integration Tests (`FootholdCollisionIntegrationTests.cs`)
Comprehensive tests that validate the entire collision pipeline from NX data loading to player physics.

**Key Test Scenarios:**
- Player spawns at correct ground height (not floating)
- Walking on platforms without falling through
- Falling when walking off edges
- Jumping and landing mechanics
- Slope walking functionality
- Gap handling between platforms
- Coordinate conversion accuracy
- Multiple foothold connections

### 2. Edge Case Tests (`FootholdCollisionEdgeCaseTests.cs`)
Tests for unusual scenarios and performance validation.

**Edge Cases Covered:**
- Vertical walls
- Extreme slopes (near-vertical)
- Zero-length footholds
- Concave foothold arrangements (V-shapes)
- Overlapping footholds at different heights
- Small gaps between footholds
- High-speed collisions
- Teleportation handling
- Performance with many footholds (100, 1000, 10000)
- Moving platforms (future support)
- NaN/Infinity handling
- State consistency across frames

### 3. Debug Tools

#### Editor Window Tool (`FootholdCollisionDebugTool.cs`)
An editor window for real-time testing and debugging.

**Features:**
- Initialize test environment in editor
- Manual player controls (move, jump, drop)
- Real-time physics simulation
- Debug information display
- Test scenario automation
- Foothold data export
- Performance monitoring

**Test Scenarios:**
- Walk Test: Automated walking on flat surface
- Jump Test: Jump and land cycle
- Slope Test: Walking on slopes
- Edge Test: Walking off platform edges
- Multi-Jump: Double jump testing
- Stress Test: Random movements for stability

#### Runtime Debugger (`FootholdCollisionDebugger.cs`)
A component that can be added to the scene for runtime visualization.

**Visualizations:**
- Foothold lines with IDs
- Player collision box
- Ground detection rays
- Velocity vectors
- State color indicators
- Distance to ground measurements
- Performance statistics

#### Scene Visualizer (`FootholdDebugVisualizer.cs`)
Existing component enhanced for collision debugging.

**Features:**
- Foothold rendering with colors by type
- Connection visualization
- Layer information
- ID labels

### 4. Test Scene Creator (`CreateFootholdTestScene.cs`)
Editor tool to create a dedicated test scene.

**Creates:**
- Configured GameManager
- Debug tools pre-attached
- Visual markers for test areas
- Height indicators
- Platform type examples
- Instructions UI

## Usage Guide

### Running Integration Tests
1. Open Test Runner (Window > General > Test Runner)
2. Navigate to MapleClient.Tests.Integration
3. Run FootholdCollisionIntegrationTests
4. Check for all green passes

### Using Debug Tools

#### Editor Testing:
1. Open Window > MapleUnity > Debug > Foothold Collision Debugger
2. Set Map ID (default: 100000000 - Henesys)
3. Click "Initialize Test"
4. Use controls or run automated scenarios
5. Monitor debug information panel

#### Runtime Testing:
1. Create test scene: Window > MapleUnity > Test > Create Foothold Test Scene
2. Enter play mode
3. Use arrow keys/WASD to move
4. Space to jump
5. Observe debug visualizations

#### Manual Scene Setup:
1. Add FootholdCollisionDebugger component to any GameObject
2. Assign GameManager reference
3. Configure visualization options
4. Enter play mode to see debug info

### Common Issues and Solutions

**Issue: Player floating above ground**
- Check foothold Y coordinates
- Verify coordinate conversion (Unity vs Maple)
- Ensure player height constant is correct (0.6 units)

**Issue: Player falling through platforms**
- Check foothold connectivity
- Verify foothold service is loaded with data
- Check physics timestep consistency

**Issue: Inconsistent grounding on slopes**
- Verify slope calculations
- Check ground detection tolerance
- Ensure proper Y adjustment for slopes

### Performance Guidelines
- Keep active footholds under 1000 for 60 FPS
- Use area queries to limit foothold checks
- Cache foothold references when possible
- Profile with stress test scenario

## Test Data
Test maps use these standard configurations:
- Flat platform: Y=2 (Unity) / Y=-200 (Maple)
- Player height: 0.6 units (60 pixels)
- Ground detection offset: 1 pixel sink
- Physics timestep: 16.67ms (60 FPS)

## Debugging Workflow
1. Create test scene
2. Enable all debug visualizations
3. Run specific test scenario
4. Monitor debug panel for anomalies
5. Export collision report if issues found
6. Use frame-by-frame stepping in editor tool
7. Check integration test for regression

## Future Enhancements
- Network lag simulation
- Multi-player collision testing
- Moving platform support
- Foothold modification runtime tests
- Collision event recording/playback