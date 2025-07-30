# Character Rendering Concrete Examples

## Stand1 Position Example

Based on the C++ source code analysis, here's how character parts are positioned for the `stand1` stance:

### Key Source Code References

From `BodyDrawInfo.cpp` (lines 93-102):
```cpp
body_positions[stance][frame] = bodyshiftmap[Body::Layer::BODY]["navel"];

arm_positions[stance][frame] = bodyshiftmap.count(Body::Layer::ARM) ?
    (bodyshiftmap[Body::Layer::ARM]["hand"] - bodyshiftmap[Body::Layer::ARM]["navel"] + bodyshiftmap[Body::Layer::BODY]["navel"]) :
    (bodyshiftmap[Body::Layer::ARM_OVER_HAIR]["hand"] - bodyshiftmap[Body::Layer::ARM_OVER_HAIR]["navel"] + bodyshiftmap[Body::Layer::BODY]["navel"]);

hand_positions[stance][frame] = bodyshiftmap[Body::Layer::HAND_BELOW_WEAPON]["handMove"];
head_positions[stance][frame] = bodyshiftmap[Body::Layer::BODY]["neck"] - bodyshiftmap[Body::Layer::HEAD]["neck"];
face_positions[stance][frame] = bodyshiftmap[Body::Layer::BODY]["neck"] - bodyshiftmap[Body::Layer::HEAD]["neck"] + bodyshiftmap[Body::Layer::HEAD]["brow"];
hair_positions[stance][frame] = bodyshiftmap[Body::Layer::HEAD]["brow"] - bodyshiftmap[Body::Layer::HEAD]["neck"] + bodyshiftmap[Body::Layer::BODY]["neck"];
```

### Example Coordinate Calculations

Assuming typical attachment point values for `stand1` frame 0:

#### Body Attachment Points (from 00002000.img)
- body.navel = (0, 0) // Base reference
- body.neck = (-2, -30)

#### Arm Attachment Points
- arm.navel = (0, 0)
- arm.hand = (-10, 5)

#### Head Attachment Points (from 00012000.img)
- head.neck = (0, 0)
- head.brow = (-1, -13)

### Calculated Positions

1. **Body Position**
   ```
   body_position = body.navel = (0, 0)
   ```

2. **Arm Position**
   ```
   arm_position = arm.hand - arm.navel + body.navel
                = (-10, 5) - (0, 0) + (0, 0)
                = (-10, 5)
   ```

3. **Head Position**
   ```
   head_position = body.neck - head.neck
                 = (-2, -30) - (0, 0)
                 = (-2, -30)
   ```

4. **Face Position**
   ```
   face_position = body.neck - head.neck + head.brow
                 = (-2, -30) - (0, 0) + (-1, -13)
                 = (-3, -43)
   ```

5. **Hair Position**
   ```
   hair_position = head.brow - head.neck + body.neck
                 = (-1, -13) - (0, 0) + (-2, -30)
                 = (-3, -43)
   ```

### Drawing Process (from CharLook.cpp)

The actual drawing happens in layers. Here's the relevant code from `CharLook::draw()`:

```cpp
// Line 127-129: Draw body
if (body) {
    body->draw(Body::Layer::BODY, interstance, interframe, args);
}

// Line 133: Draw arm below head
body->draw(Body::Layer::ARM_BELOW_HEAD, interstance, interframe, args);

// Line 150: Draw head
body->draw(Body::Layer::HEAD, interstance, interframe, args);

// Line 158-160: Draw face with face shift
if (face) {
    face->draw(interexpression, interexpframe, faceargs);
}
```

Where `faceargs` is calculated as:
```cpp
// Line 80-81
Point<int16_t> faceshift = drawinfo.getfacepos(interstance, interframe);
DrawArgument faceargs = args + DrawArgument{ faceshift, false, Point<int16_t>(0, 0) };
```

### Unity Implementation Checklist

1. **Create attachment point GameObjects** on each body part prefab:
   - Body: "navel", "neck"
   - Arm: "navel", "hand"
   - Head: "neck", "brow"

2. **Implement the positioning formulas exactly**:
   ```csharp
   // Example Unity implementation
   Vector2 CalculateArmPosition() {
       Vector2 armHand = GetAttachmentPoint(arm, "hand");
       Vector2 armNavel = GetAttachmentPoint(arm, "navel");
       Vector2 bodyNavel = GetAttachmentPoint(body, "navel");
       
       return armHand - armNavel + bodyNavel;
   }
   ```

3. **Apply sprite origins/pivots correctly**:
   - Each sprite's pivot in Unity should match the origin from the WZ files
   - Final position = calculated_position - sprite_pivot

4. **Handle the DrawArgument accumulation**:
   - When positioning face: position = character_position + face_shift
   - Face shift comes from the precalculated face_positions table

### Testing Strategy

1. Start with a naked character (no equipment)
2. Log all attachment points and calculated positions
3. Compare with original client by taking screenshots at the same position
4. Gradually add equipment pieces and verify each one

### Common Pitfalls to Avoid

1. **Don't assume (0,0) is at the feet** - it's at the body navel
2. **Don't skip the shift calculations** - they're essential for alignment
3. **Don't change the drawing order** - it affects overlapping parts
4. **Don't forget sprite origins** - they offset the final position