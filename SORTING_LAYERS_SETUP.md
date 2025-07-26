# Unity Sorting Layers Setup

For the MapleUnity project to render correctly, you need to configure the following sorting layers in Unity:

## Required Sorting Layers (in order from back to front):

1. **Background** - Far backgrounds, sky, etc.
2. **Tiles** - Ground tiles and platforms
3. **Objects** - Map objects, decorations, buildings
4. **NPCs** - Non-player characters
5. **Player** - Player character
6. **Foreground** - Objects that appear in front of everything

## How to Set Up:

1. Go to Edit → Project Settings → Tags and Layers
2. Under "Sorting Layers", add the layers listed above in the exact order
3. Make sure "Default" remains at the top of the list

## Layer Usage:

- **Background**: Used for background images that scroll at different speeds (parallax)
- **Tiles**: Used for ground tiles that make up the terrain
- **Objects**: Used for map decorations, buildings, and interactive objects
- **NPCs**: Used for NPCs to ensure they appear above objects but below the player
- **Player**: Used for the player character
- **Foreground**: Used for objects that should appear in front of everything else

## Note:
The system automatically assigns sorting orders within each layer based on Y position and Z order values from the map data.