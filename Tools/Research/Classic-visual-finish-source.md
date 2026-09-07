# Classic visual finishing pass — 2026-09-06

## Source and reference

Behavioral source: `C:/HeavenClient/MapleStory-Client`, inspected working-tree `Gameplay/MapleMap/Npc.cpp` and `Character/CharEffect.cpp`. The nine preserved user screenshots in `Tools/ArtReferences/MapleClassic` remain the appearance reference. Original NX bitmaps were reused without modification.

## HUD and windows

- HP/MP numbers now use readable 11-pixel text with a shallow shadow. Job/name labels use 12 pixels, reducing to 10 for long values. EXP uses 9–11 pixels according to length. The original level digits, gauge caps, button icons and quickslot geometry stay native. The preceding Short Cut safe-margin correction covers normal, hover, pressed and disabled art.
- Window title regions retain the complete 14-pixel strip. Custom action buttons have a shallow highlight/shade and 8–11-pixel fitting labels, retaining their normal hover/press behavior and caller colors.
- The skill-book header uses the gold/gray artwork from `UIWindow.img/Skill/backgrnd`, extending only an empty center column. Its real job-book icon comes from `Skill.nx/<job D3>.img/info/icon` (26×30 pixels). The displayed tier chooses the corresponding job. The authored scroll arrow states are `Basic.img/VScr/enabled/{prev,next}{0,1}` and `disabled/{prev,next}`; this NX set has no third pressed bitmap.

## Nameplates

Unobstructed labels remain two pixels below the projected feet. Actual text widths and one/two-line heights drive placement. The local player has priority, followed by stable NPC and recent-hit monster order. Collisions move lower-priority plates down in 18-pixel rows, up to 108 pixels, with small gaps. Plates are clamped to the viewport; crowded labels that cannot fit within that bound are hidden. These are local readability choices; the C++ source does not implement this avoidance policy. Labels remain upright and never intercept input.

## NPC animation and portraits

`NxNpcAnimations` follows `info/link` with cycle/missing-link protection while the original NPC ID continues to select its name, service, quests and speech. Only root stances containing a zero frame qualify: NPC 1012108 also has `condition1/22557` and `condition2/22557` quest data, which must not be loaded as pictures.

`ClassicNpcAnimator` starts at `stand`, loads the authored frame delays and origins, and changes stance at loop boundaries. C++ chooses a random next stance; the Unity offline presentation uses a stable per-NPC sequence. NPCs with one authored frame remain still. This plays existing gestures and expressions and does not introduce walking physics. Bruce's first say frame lasts 1000 ms; subsequent say frames begin after that hold.

Active dialogue or a visible ambient bubble requests `say`/`talk` where available. Every world frame applies its own origin to the sprite child, retaining the spawn's feet and facing on its parent. The dialogue portrait follows the same frame without world flipping. A single fitting scale and origin are computed from all of the NPC's frame bounds, preventing animation-dependent resizing, bobbing or clipping.

Quest-conditional NPC art and scripted movement still require their corresponding gameplay implementation. This pass does not infer quest-condition behavior from numeric asset children.

## Event and item effects

- `GameWorld.FirstJobAdvanced` fires after the successful first-job inventory/reward transaction; it plays `BasicEff.img/JobChanged` (13×100 ms), behind the actor as indicated by its z metadata.
- `GameWorld.QuestCompleted` fires only after the actual reward transaction; it plays `BasicEff.img/QuestClear` (11×100 ms). This original asset is a green particle burst, not a text banner. Choosing it for the offline completion event is explicit local presentation; no server quest packet behavior is implied.
- `Player.ItemUsed` fires after successful consumption. Stat-buff consumables play `BasicEff.img/Buff`; skill casts retain their existing skill-specific art. Ordinary recovery items do not receive an invented generic effect.
- Existing `BasicEff.img/LevelUp` continues to play independently. A quest reward can trigger both level-up and quest-clear tracks. Each kind restarts only itself, expires at its authored duration, follows the current actor/foothold layer, and clears on travel or restoring progress.
- Job and quest completion also enter the bounded fading event-message stack. Failed actions, repeated completed quests, direct practice job changes and loading saves do not replay celebration events.

## Validation

The focused nine-scene run passed in `Logs/visual-finish-layout.xml`. The first exploratory run exposed the numbered quest-condition import edge case; it was corrected in the data filter, with regression coverage. New checks also cover linked NPC art, anchored animated portraits, overlapping labels at two resolutions, successful/failed event transactions, simultaneous effects and travel/load cleanup.

Final validation: **38/38 scene checks**, then **6/6 affected scene rechecks** after the final spacing adjustment, **115/115 data checks**, and player compilation with zero C# errors. Artifacts: `Logs/visual-finish-scenes.xml`, `Logs/visual-finish-final.xml`, `Logs/visual-finish-data.xml`, and `Logs/visual-finish-player.log`. All 383 C# files match the isolated snapshot and have metadata. Further details are in `PROJECT_STATUS.md`. Validation runs in `Temp/CodexValidation`; the original editor and its scene remain open.

## Remaining scope

This pass addresses each of the five outstanding visual categories. Exact pixel matching still needs side-by-side captures of the same map, character and UI state; the supplied video screenshots have different resolution and version details. Further class-specific actor/skill effects, conditional NPC art and scripted movement follow their gameplay ports. A full keyboard editor, more usable item behaviors, additional quests and scripted services remain rewrite work. Online play is excluded.
