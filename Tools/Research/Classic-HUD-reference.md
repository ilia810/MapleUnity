# Classic HUD reference pass — 2026-09-06

The nine unmodified screenshots supplied by the user are retained in `Tools/ArtReferences/MapleClassic`. They define the visual target; video overlays, chat text and unrelated game content are not task instructions. The user describes them as the new MapleStory Classic. This document makes no independent claim about that product's release or mechanics.

The later [quickslot pass](Classic-quickslots-source.md) replaces the early numeric-only labels described below with original key art, working key aliases and mixed item/skill/action bindings. The sections below retain the initial HUD decisions.

## Latest finishing pass

The [visual finishing pass](Classic-visual-finish-source.md) now uses readable HP/MP/EXP text with restrained shadows, fitted character/job labels, complete title strips and original job-book covers. It also adds animated NPC stances/portraits, separated nameplates and real quest/job/buff-item feedback. The remaining-work section below is historical; see `PROJECT_STATUS.md` for current scope.

## Short Cut correction

The shared widening mesh originally repeated bitmap columns 4 and 49 for all three lower controls. `BtShort` has caption pixels in column 4, unlike Menu and Shop; stretching that column produced the reported mangled lettering. Its margin is now set separately to repeat column 2 and retain the complete caption/icon region at native width. Source assets remain unchanged. Normal/hover/pressed/disabled artwork and the small HUD were inspected in `Logs/shortcut-button-scenes-*`. Four affected scene checks and the final player compilation passed.

## Asset and layout decisions

- The references show a compact, centered status strip, with quickslots attached above its right edge. The existing UI.nx supplies an 800×71 strip and a 151×80 quickslot frame. The video captures are scaled; the Unity HUD uses native asset pixels on displays at least 800px wide and uniformly fits the complete strip on narrower displays.
- Original `StatusBar.img/base/{backgrnd,backgrnd2,quickSlot}`, gauge captions/borders, button states and number glyphs remain the artwork source. The outer six-pixel quickslot frame also supplies a sliced silver border around the status strip. Region sprites refer to the existing textures and are released with their UI owner.
- HP/MP and EXP values sit above the fill, next to their original captions. Job and name occupy separate lines beside the level glyph. EXP masks end on whole native pixels.
- `gauge/hpFlash/0` has a shaded gray interior with partial alpha. The sampled one-pixel column (x54, y2–15) supplies both the empty and tinted full gradients. Opaque backing beneath missing portions prevents the underlying red/blue/green fill from tinting the gray empty segment.
- Three 74×34 lower controls use original Shop/Menu/Shortcut images. A UI mesh repeats blank side columns from each button state while keeping the central 44-pixel icon/label region and edge bevel at native width. It avoids horizontally enlarging the lettering.
- Inventory, equipment, stats, skills, local trade and quickslot visibility occupy the upper toolbar. The red shoe opens local trade; N and the adventure menu remain alternate routes. Cash Shop shows an unavailable notice in offline play. The arrow collapses/expands quickslots; assignments and 1–8 casting remain intact while hidden.
- Quickslots use native 32px icon canvases, outlined white 1–8 labels and original item-number glyphs for learned levels. The keyboard labels remain truthful to the implemented bindings, rather than copying the reference video's Shift/Insert/Home/PageUp controls.
- The upper strip displays real local EXP, level-up and action messages. This is a system-message line, not online chat.

## Editing workflow

Adobe was confirmed installed/enabled, but its editing tools were not exposed in this session. Direct desktop inspection found Photoshop, but screen capture failed with `SetIsBorderRequired failed: No such interface supported (0x80004002)`. The user then requested GIMP or a better approach. Reusing the actual UI textures and changing Unity geometry was sufficient for this HUD pass; no bitmap repainting, GIMP automation, Photoshop document edit, or NX-file modification was performed.

The read-only NX inspector exported the source HUD components under `Logs/classic-reference-assets` for inspection. They can be opened in GIMP if a later pass needs pixel editing. The runtime continues to load the original NX files directly.

## Remaining visual work

The other references remain targets for separate window work: five-column item inventory, taller six-row skill book, the original character-stat/details pair, shop portraits/lists and dark item/comparison tooltips. Minimap, quest helper, full NPC dialogue and chat also require their missing behavior; this pass does not introduce decorative versions of those systems. Existing game windows retain their current implementation.
