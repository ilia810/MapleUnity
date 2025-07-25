# MapleStory Unity Client - Manual Testing Checklist

## Pre-Testing Setup
- [ ] Build the project in Unity
- [ ] Ensure no compilation errors
- [ ] Run all unit tests (Edit Mode)
- [ ] Run all integration tests (Play Mode)

## Movement & Physics Testing
- [ ] **Basic Movement**
  - [ ] Move left with A/Left Arrow
  - [ ] Move right with D/Right Arrow
  - [ ] Character faces correct direction
  - [ ] Movement stops when key released
  - [ ] Can change direction while moving

- [ ] **Jumping**
  - [ ] Jump with Space/Left Alt
  - [ ] Jump reaches appropriate height
  - [ ] Cannot double jump
  - [ ] Landing on platforms works correctly
  - [ ] Can jump while moving (maintains horizontal velocity)

- [ ] **Crouching**
  - [ ] Crouch with Down Arrow while grounded
  - [ ] Visual changes (character becomes shorter)
  - [ ] Cannot move while crouching
  - [ ] Cannot jump while crouching
  - [ ] Stand up when releasing Down

- [ ] **Ladder Climbing**
  - [ ] Enter ladder with Up Arrow when near
  - [ ] Climb up/down with arrow keys
  - [ ] Cannot go beyond ladder bounds
  - [ ] Jump off ladder with Space
  - [ ] Exit ladder at top/bottom

## Combat Testing
- [ ] **Basic Attack**
  - [ ] Attack with Ctrl/Z
  - [ ] Attack has proper range
  - [ ] Monsters take damage
  - [ ] Monsters die when HP reaches 0
  - [ ] Dead monsters disappear

- [ ] **Monster Behavior**
  - [ ] Monsters spawn at correct locations
  - [ ] Monster HP bars display correctly
  - [ ] Multiple monsters can be attacked

## Inventory & Items Testing
- [ ] **Item Drops**
  - [ ] Items drop when monsters die
  - [ ] Dropped items visible on ground
  - [ ] Items expire after time

- [ ] **Item Pickup**
  - [ ] Auto-pickup when near items
  - [ ] Items added to inventory
  - [ ] Pickup notification appears

- [ ] **Inventory UI**
  - [ ] Toggle with I key
  - [ ] Shows correct item counts
  - [ ] Updates when items added/removed

## UI Testing
- [ ] **Status Bars**
  - [ ] HP bar displays and updates
  - [ ] MP bar displays and updates
  - [ ] Level display is correct

- [ ] **Experience Bar**
  - [ ] Displays at bottom of screen
  - [ ] Shows EXP percentage

- [ ] **Skill Menu**
  - [ ] Toggle with K key
  - [ ] Shows available skills
  - [ ] Close button works

## Map & Portal Testing
- [ ] **Map Loading**
  - [ ] Initial map loads correctly
  - [ ] Platforms render properly
  - [ ] Ladders visible
  - [ ] Portals visible and animated

- [ ] **Portal Usage**
  - [ ] Stand at portal and press Up
  - [ ] Transitions to new map
  - [ ] Player positioned correctly in new map
  - [ ] Old map objects cleaned up

## Edge Cases & Error Testing
- [ ] **Boundary Testing**
  - [ ] Cannot move beyond map boundaries
  - [ ] Cannot fall through solid platforms
  - [ ] Cannot climb non-existent ladders

- [ ] **State Conflicts**
  - [ ] Cannot crouch while jumping
  - [ ] Cannot attack while climbing
  - [ ] State transitions work smoothly

- [ ] **Performance**
  - [ ] Steady frame rate (30+ FPS)
  - [ ] No memory leaks over time
  - [ ] Smooth gameplay with multiple monsters

## Known Issues to Verify Fixed
- [ ] Jump input works reliably
- [ ] Movement direction changes work while holding keys
- [ ] Inventory window displays content (not empty)
- [ ] Font errors resolved
- [ ] Players don't float on spawn
- [ ] Continuous jumping works as expected

## Regression Testing
After any code changes:
- [ ] Re-run unit tests
- [ ] Test affected features
- [ ] Verify no new issues introduced

## Notes Section
Record any issues found:
```
Date: 
Tester:
Issue Description:
Steps to Reproduce:
Expected vs Actual:
```