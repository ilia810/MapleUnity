# Classic minimap, regional atlas and world labels — 6 September 2026

## Behavioral and asset references

- `C:/HeavenClient/MapleStory-Client/IO/UITypes/UIMiniMap.cpp`: map canvas metadata (`centerX`, `centerY`, `mag`); marker projection; static portal filtering (`pt == 2`); and the region mark from `MapHelper.img/mark`. This file mixes UI versions. Unity composes the pieces that actually exist in the local NX archive instead of copying its hardcoded frame dimensions.
- `Gameplay/MapleMap/Npc.cpp`: labels use the original NPC ID's `String/Npc.img/<id>/name` and `func`, while linked appearance metadata controls `hideName`.
- `Gameplay/MapleMap/Mob.cpp`: `draw` only shows the name while `showhp` is active; `show_hp` enables it for 2000 ms. Unity nameplates share the existing monster health-feedback timer, disappear on death, and do not label every idle monster.
- `Character/Char.cpp`: player name at the actor's feet. The Unity simulation stores the player center, so its foot anchor subtracts `Player.Height / 2`.
- `Map.nx/WorldMap`: each atlas has `BaseImg/0`, `MapList/*/{mapNo,spot,type}`, `MapLink/*/{link,toolTip}`, and optional `info/parentMap`. Atlas selection follows the deepest parent chain containing the current map; counting spots is incorrect because the global overview contains towns but fewer points than their regional atlases.

## Confirmed local data

- `String.nx/Map.img` groups IDs below region keys such as `victoria`, `ossyria` and `maple`. Both the game map loader and map data provider now use the same resolver. `mapName` and `streetName` are separate values.
- Henesys (`100000000`) has a **465×86** minimap bitmap, `centerX=1068`, `centerY=661`, `mag=4`, and map mark `Henesys`. One minimap pixel therefore represents **16 source pixels**.
- Projection is `(sourceX + centerX, sourceY + centerY) / 2^mag`. Unity feet positions first convert to `(worldX*100, -worldY*100)`.
- Henesys Department Store (`100000102`) has **no minimap canvas**. It uses a collapsed location strip and never borrows the prior map's preview or markers. Maps with `info/hideMinimap` also suppress preview and markers.
- `UIWindow.img/MiniMap` supplies `MaxMap`, `MinMap`, `Min`, `title` and `BtMap`. The local archive's plus/minus buttons are under `UIWindow.img/SoftKeyboard/Bt/0`. Expanded frame: 6 px side edges, 72 px top, 15 px bottom. Compact frame: 29 px top, 14 px bottom.
- `MapHelper.img/minimap` supplies the player, NPC and portal dots. Previews remain at native pixels; oversized previews crop around the player and clamp to their edges.
- Henesys belongs to `WorldMap010.img`; its town point is `(-28,142)` relative to the 640×470 base image's `(320,235)` origin. Original `MapHelper.img/worldMap` sprites provide town/field points and the current-location arrow.

## Integration decisions

- The minimap is a persistent HUD panel. Its title bar drags it, plus/minus or **Tab** change its display state, and **WORLD / M** toggle an ordinary independent atlas window. Atlas links browse neighboring regions, **Overview** follows the authored parent, and **My location** returns to the current region. Browsing has no travel side effect.
- Player, monster and NPC views register `WorldLabelAnchor` components. Canvas nameplates use real names, services and levels; they do not inherit flipped sprite transforms. NPC marker positions use the grounded scene anchors. Disabled/destroyed actors and dead monsters remove their labels; old-map markers clear immediately on travel.
- Labels project after `CameraBounds` (execution order 900). Projecting after the follow camera but before this final clamp caused visible drift at map boundaries. `ClassicWorldLabels` uses order 1100 and refreshes projection before canvas rendering. It maps screen pixels directly to the canvas local rectangle, because a camera-space canvas can still have its previous world transform after a camera clamp or resize. The rendered scene test verifies labels at the edge of the hunting map.
- The original NX files and screenshot references are unchanged. This work adds no online identities, quest markers, fast travel or server behavior.

## Checks and captures

Focused source-data and scene checks live in `ClassicNavigationIntegrationTests` and `ClassicNavigationSceneSmokeTests`. Final results and remaining UI work are recorded in `Classic-UI-implementation.md` and the top milestone of `PROJECT_STATUS.md`.
