# Classic combat overlays, cursor and quest markers — 6 September 2026

Source: the working tree at `C:/HeavenClient/MapleStory-Client`, plus the installed NX files and the user's unchanged Classic screenshots. This is a presentation milestone; combat rolls, quest eligibility and rewards continue to use the existing offline simulation.

## Damage artwork

`Gameplay/Combat/DamageNumber.cpp`, `Gameplay/Combat/Combat.cpp::place_numbers`, `Character/Char.cpp::show_damage` and `IO/Components/Charset.cpp` provide the reference. `Effect.nx` is now loaded by the NX data manager.

| Feedback | First digit | Following digits / MISS |
|---|---|---|
| Normal outgoing | `BasicEff.img/NoRed1` | `BasicEff.img/NoRed0` |
| Critical outgoing | `BasicEff.img/NoCri1` | `BasicEff.img/NoCri0`; MISS uses NoRed0 |
| Incoming | `BasicEff.img/NoViolet1` | `BasicEff.img/NoViolet0` |

`ClassicDamageNumber` composes native bitmap dimensions and origins. The source advances are 24, 20, 22, 22, 24, 23, 24, 22, 24, 24 for digits 0–9. The first normal digit adds 2 pixels; critical digits add 8 for the first and 4 for subsequent digits. Subsequent advances average adjacent digits, with alternating ±2-pixel vertical staggering. Critical feedback includes the available `NoCri1/effect` burst. Literal CRIT text is removed; semantic Text components remain disabled beneath the artwork, with readable fallback if source art is unavailable.

The visible digits are horizontally centered over the target. This deliberately fixes the inspected C++ draw line that subtracts `(0, shift)` despite calculating shift from the number's width. Critical bursts are also an explicit presentation addition using existing source artwork; the inspected Charset draw path does not render that sibling. A 32-pixel clearance keeps outgoing numbers above the HP frame. Additional lines use 30 pixels for normal and 36 for critical hits. Fade follows `clamp(1.5 − age × 2)`, with 31.25 pixels/second upward movement and removal at 0.75 seconds. Horizontal monster tracking honors head X and facing; rising Y remains independent.

Damage renders in a separate world-overlay layer below the minimap, HUD and windows. Direct canvas-pixel projection runs after the final camera clamp and again before canvas rendering, keeping native size through camera zoom and capture resolution changes. Map changes, revival and teleport clear pending numbers.

## Monster HP

`Graphics/Geometry.cpp::MobHpBar::draw` uses geometry rather than a bitmap. Unity reproduces the 50×10 black frame with inset white edges: top/bottom 48×1, side edges 1×6, bright green fill at (3,3) with height 3, and dark green fill at (3,6) with height 1. Fill width truncates to 44 × truncated HP percentage / 100. For example, 8/15 HP becomes 53%, then 23 fill pixels.

`ClassicMonsterHealthView` replaces the old stretched world-space rectangles. It anchors the frame 30 pixels above the monster's authored head, follows the interpolated view position and facing, and shares the existing two-second recent-hit lifetime with monster names. Death and map travel remove the old frames. These graphics do not receive pointer input.

## Glove cursor

`IO/Cursor.cpp` and `UI.nx/Basic.img/Cursor` supply idle (0), clickable (1), grabbable (5), grabbing (11) and clicking (12) frames. `ClassicSpriteFrames` retains the authored origin and per-frame delay; the cursor origin is its pointer hotspot.

`ClassicCursorView` draws a software cursor above UI without intercepting raycasts. Enabled buttons and NPC bodies show the clickable glove; occupied inventory cells, title bars and scrollbars show the grab glove. Pressed states retain grab/click feedback. The top UI hit takes precedence over world NPC hit testing. Disabled controls do not advertise interaction. Cursor animation restarts when the state changes. After 15 seconds without movement or button activity the glove hides, as in the source. Leaving the game viewport, losing focus, disabling or destroying the component restores the prior system-cursor visibility. Batch rendering suppresses the machine's pointer; capture checks explicitly draw it at a known location.

NPC clicks and hover share `ClassicNpcDialogue.NpcAtScreen`, while actual conversation still validates the selected current-map spawn and proximity.

## Quest markers

`UIWindow.img/QuestIcon/0` supplies the animated available lightbulb bubble; `/1` supplies the ready-to-turn-in hand bubble. Both use their original dimensions, delays and origins. Other numbered QuestIcon entries include text labels and are not indiscriminately treated as overhead markers.

The inspected C++ NPC view does not implement the offline quest-state mapping. `ClassicQuestIndicators` is a Unity presentation addition backed by the existing four-quest evaluator: ready-to-turn-in takes priority over available, and incomplete or completed quests do not show an overhead marker. Level/job/prerequisite and material/kill changes update the result. The marker follows the top of the actual NPC sprite, stays below windows, and offers a tooltip and click-to-talk through the normal proximity guard. It never accepts or completes a quest automatically. Map changes remove old markers.

## Scope and validation

Original NX archives and reference images are unchanged. No Photoshop/GIMP edits or generated substitute art were required. The subsequent [NPC presentation milestone](Classic-npc-presentation-source.md) adds ambient speech, both dialogue orientations, level-up art and lower-right notices. Finer HUD/quickslot/font matching and additional source effects remain. The existing four-quest catalog is unchanged; online play stays excluded.

`ClassicCombatVisualSceneSmokeTests` checks real NX glyph families, disabled text fallback, nonblocking graphics, HP geometry/projection at 1366×768 and 640×480, multi-hit spacing, lifetime/map cleanup, marker eligibility and state transitions, click-to-talk, cursor hotspots/animation and UI-first hit testing. Existing melee/magic/ranged, NPC, inventory and navigation scene tests continue to exercise the real gameplay paths. Final result files are listed in `PROJECT_STATUS.md`.
