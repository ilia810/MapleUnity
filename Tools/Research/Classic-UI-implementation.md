# Classic window implementation — 7 September 2026

This implements the existing-window portion of [the nine-image audit](Classic-UI-audit.md), plus independent window behavior, buff icons, local message history, the black interior surround, minimaps, the regional atlas and world nameplates. It does not yet recreate every interface element shown in the references. Original NX files and reference screenshots remain unchanged; Unity composes native frame edges, sprites, icons and controls around the new layouts.

## Implemented

- **Independent windows:** bag, equipment, skills, character stats, shop and local menu can remain open together. Clicking a window raises it. The Menu command (Escape by default) closes one frontmost window. Each ordinary window has an initial placement and retains its dragged position in normalized screen coordinates across sessions. Bounds reserve the HUD and quickslots; small displays scale each window uniformly. Item/skill action popups dismiss when clicking elsewhere.
- **Inventory:** compact 211×289 frame, five columns and six rows; 30 addressable slots per category. Expand switches to ten columns and three rows. Gather packs gaps; Sort orders the current category by item ID. Existing sparse saved slots keep their addresses. Equipment has its own 175×291 window. Unsupported Deco is visibly disabled.
- **Items:** live hover descriptions in translucent navy cards, enlarged original icons, colored level/attribute requirements and class eligibility, enhancement-slot metadata and side-by-side equipped comparisons. Equipment cards use a dedicated requirement block, a single measured row of class names, and title wrapping. Tooltips clear when their window closes. Double-click uses/equips/removes an item. Right-click opens the same exact-slot action. Dragging still moves, swaps and combines stacks through the existing revision and capacity checks.
- **Skills:** a 196×370 book with six visible rows, job-tier tabs, a job header, plain learned levels, per-row SP buttons and scrolling. Hover shows costs and prerequisites. Right-click offers casting, SP spending and quickslot assignment. Practice controls moved to the local menu.
- **Character stats:** a 186×282 main panel with pink identity/vital rows, green attributes, small AP controls and an AP footer. A 187×221 detail panel attaches at the right and shares its bottom edge. Values use real equipment/buff stats; ammo status remains in its footer. Fame is unmodeled and shown as a dash. First-job advancement remains a separate local menu action.
- **Shop:** a centered 463×378 pair of panes with six visible rows each, independent scrolling, category tabs, orange selection and original Buy/Sell/Leave buttons. Selecting a row does not trade; the action button performs the transaction. Selling retains the selected bag slot. The player portrait composes current equipment in the weapon-specific standing pose without changing the actor. The original rounded shop frame, selection and coin art are used, and both columns have working arrow and wheel scrolling. Quantity is selected with ×1/×10. Trading still requires proximity to the real local shop NPC.
- **Other screen differences:** active buffs appear at the top right with original icons and timers driven by simulation time. The local message history appears above the left HUD, keeps 50 messages, wraps and colors real EXP/pickup/system events, and preserves the reading position as new messages arrive. Original All-tag, arrow, collapse/expand and resize-edge artwork matches the message strip; long strip text and floating notices use an ellipsis. Dragging the top edge resizes history, while a real scrollbar retains its native thumb. The envelope stays disabled in local play. [Reference mapping and behavior](Classic-HUD-messages-source.md). Small interiors have a black surround, matching `Graphics/GraphicsGL.cpp` in the C++ source.

- **Navigation:** original minimap frames, regional names and marks, native preview images, player/NPC/visible-portal markers and marker tooltips. Oversized previews follow the player without resampling. Collapsed, compact and expanded states share a draggable HUD panel; maps without a preview and maps hiding it use only the location strip. The WORLD button opens the original regional atlas with current-location and town/field markers, hover names, authored neighboring-region links, Overview and My location. This atlas is an independent draggable window and never teleports the player.
- **World names:** player names, NPC names/service titles and monster names/levels appear at their rendered feet. Monster names share the two-second recent-hit health feedback instead of covering every idle monster. They stay upright during sprite flips, honor NPC `hideName`, follow the final camera boundary adjustment and clean up on death or map changes. The minimap and both map-data name APIs now read the actual regional string groups. [Source notes and coordinate conventions](Classic-navigation-source.md).

- **Quests and conversations:** Q opens the original two-pane quest journal, with Available / In Progress / Completed tabs, tracking and confirmed abandonment. The separate quest helper reads current bag contents and credited hunt counts. The native AUTO button toggles automatic tracking of future acceptances and remembers the UI preference. Orange/gray states show ON/OFF. An empty 0/5 header keeps AUTO reachable while untracked active quests exist. The reference-style blue close hides the helper without clearing tracking; the journal footer can reopen it. [AUTO assets and behavior](Classic-quest-auto-source.md). Native minimize/expand controls preserve tracking; each quest has a close icon that untracks without abandoning. The header can be dragged, retains its preferred position through resizing, and reserves room for buffs and the HUD. Journal scrolling controls disable when the list fits. [Helper artwork and placement](Classic-quest-helper-source.md). V or double-click speaks to a nearby NPC, using original portraits, paged dialogue and quest acceptance/reward buttons. Conversations close on leaving range or changing maps. Four audited source quests work offline and persist in schema-5 manual saves (schema 3 introduced quest records). [Catalog and source notes](Classic-quests-source.md).

- **Combat and interaction art:** outgoing orange, critical pink and incoming violet damage digits now use the original Effect.nx artwork, including MISS, multi-hit rows and critical bursts. Monster HP uses the exact source 50×10 frame and two-tone green fill, at a native screen size above the authored head. Damage stays behind the interface. An animated glove cursor uses authored hotspots and reflects clickable/grabbable controls and NPCs. Available/ready quest bubbles follow actual quest eligibility and support nearby click-to-talk. [Source paths, behavior and scope](Classic-combat-visuals-source.md).

- **NPC presentation and event feedback:** authored ambient NPC lines appear in the original dark-red-on-white speech bubbles, staggered and bounded to avoid crowding. Dialogue supports left and right portraits according to the NPC's side when approached; only the frame mirrors, keeping text and controls readable. Short pages hide their scrollbar. Original level-up frames play around the actor, while EXP and pickups enter a fading lower-right stack above the HUD. [Source findings and local presentation choices](Classic-npc-presentation-source.md).

- **Quickslots:** original Shift/Ins/Home/PgUp and Ctrl/Del/End/PgDn label art, live item totals, original Attack/Jump tiles and 1–8 aliases. Drag supported potions from the bag or learned active skills from the book, and drag between slots to swap. Right-click a slot or click Short Cut for the independent original NX keyboard window. Every physical key can be rebound, including movement, window commands and expressions; drag from its action palette, bag, book or tray. Occupied keys swap. Number aliases follow the tray until individually edited. Full bindings persist in version-5 saves, with migration from versions 1–4. [Complete keyboard controls and migration](Classic-full-keyboard-source.md). [Scrollbar source notes](Classic-keyboard-scrollbars-source.md). [Source, migration and controls](Classic-quickslots-source.md).

## Trying it

The keys below are defaults; Short Cut now remaps every keyboard key. Let Unity finish compiling, then restart Play mode in the existing map scene; UI is built at runtime, so map regeneration is unnecessary. Use **I**, **E**, **K** and **C** to open the four main windows together. Move their title bars, click exposed parts to raise them, and press Escape repeatedly to close one at a time. Open **Menu → Practice supplies and skills** for the existing practice kits, or **Menu → First job advancement** for earned progression. Press **N** near Luna to open the shop. Use **Tab** or minimap **− / +** to change minimap size. **M** or **WORLD** toggles the atlas; hover a point for its map names, click a region link to browse, and use **My location** to return to your current region. The minimap title bar is draggable, and its placement survives map changes within the current play session.

## Visual completion and rewrite boundaries

The visual checklist for implemented offline systems is complete, including comparison in all nine reference maps. The final pass corrects native caption color/dimensions, stat labels and job rows, compact dialogue and long-content scrolling, hat/hair/accessory composition, small-interior margins and compact minimap width. Face accessories, glasses and earrings now equip and persist through local saves. See the [completion matrix and validation](Classic-visual-completion.md) and [current gallery](../ArtReferences/MapleClassic/review.html).

Differences tied to the older NX release, the character's actual gear/stats, unavailable default top artwork and unported class/NPC/transport services are documented explicitly in that matrix. Those services and their conditional visuals belong to the remaining gameplay rewrite. The two remote-shop and dialogue reference previews are read-only test fixtures. Real trading remains at Luna; online play remains outside scope.

## Quest helper header validation

**17/17 quest integration checks passed** (`Logs/quest-auto-data.xml`). **All 11 affected UI scenarios are verified:** 10/11 passed in `Logs/quest-auto-final.xml`; the message-history case hit an exact floating-point size assertion, corrected to a 0.001-pixel tolerance, and both history cases passed in `Logs/quest-auto-message-final.xml`. The AUTO, header, hide/reopen and small-display captures were inspected. Final Windows player compilation passed with **zero C# errors** (`Logs/quest-auto-player-final.log`). All **396 C# sources** have metadata and match the isolated snapshot; the review has **281 valid local links**. The original Unity editor and scene are preserved.

[Assets, local behavior and controls](Classic-quest-auto-source.md).

## HUD message validation

- 2/2 existing UI cases: `Logs/hud-messages-existing.xml`.
- 8/8 initial focused history/UI/keyboard/cursor cases: `Logs/hud-messages-focused.xml`.
- 49/49 broader scene cases: `Logs/hud-messages-scenes.xml`.
- 8/8 affected history/UI/keyboard/quickslot cases after the final envelope render fix: `Logs/hud-messages-final.xml`; final wide and small captures inspected.
- Final Windows player compilation passed with zero C# errors: `Logs/hud-messages-player-verified.log`.
- All 396 C# sources have metadata and match the isolated snapshot. The updated review has 264 valid local links.
- [Reference mapping, behavior and limits](Classic-HUD-messages-source.md).

## Complete keyboard validation

- 91/91 focused data/save cases: `Logs/full-keyboard-model.xml`.
- 7/7 focused scene cases: `Logs/full-keyboard-focused.xml`; large and small keyboard captures inspected.
- All 47 scene cases verified: 46/47 in `Logs/full-keyboard-scenes.xml`, plus the corrected swimming hint expectation and passing rerun in `Logs/full-keyboard-swimming.xml`.
- Final Windows player compilation passed with zero C# errors: `Logs/full-keyboard-player-final.log`.
- All 394 C# sources have metadata and match the isolated snapshot. The review has 247 valid local links.
- [Behavior, source composition and migration](Classic-full-keyboard-source.md).

## Earlier keyboard and scrollbar validation

- 45/45 scene checks: `Logs/keyboard-scrollbars-scenes.xml`.
- 8/8 affected checks after the final cached-icon change: `Logs/keyboard-scrollbars-verified.xml`.
- Final Windows player compilation passed with zero C# errors: `Logs/keyboard-scrollbars-player-final.log`.
- All 389 C# sources have metadata and match the isolated snapshot. The updated review has 242 valid local links and includes fresh keyboard and scrollbar captures at both display sizes. The original editor and scene remain open.

## Quest helper validation

- 42/42 scene regression checks: `Logs/quest-helper-scenes.xml`.
- 4/4 affected helper/cursor cases after the final grab affordance change: `Logs/quest-helper-cursor-final.xml`.
- Final Windows player compilation passed with zero C# errors: `Logs/quest-helper-player-final.log`.
- All 386 C# sources have metadata and match the isolated snapshot. Final helper captures use `Logs/quest-helper-cursor-final-*`; other current UI captures use `Logs/quest-helper-scenes-*`. The review has 229 valid local file links. Restart Play; no map regeneration is needed.

## Shop and tooltip validation

- 40/40 scene checks: `Logs/shop-tooltip-scenes.xml`.
- 5/5 affected scenes rechecked after the final description-text correction: `Logs/shop-tooltip-text-final.xml`.
- Player compilation passed with zero C# errors: `Logs/shop-tooltip-player.log`.
- All 384 C# sources have metadata and match the isolated snapshot. Final shop/tooltip captures use `Logs/shop-tooltip-text-final-*`; other current interface captures use `Logs/shop-tooltip-scenes-*`. The review has 212 valid local file links. The original editor and unsaved scene remain open; restart Play without regenerating the map.

## Visual finishing validation

- 38/38 scene regression checks: `Logs/visual-finish-scenes.xml`.
- 6/6 affected NPC/navigation/effect scenes rechecked after final spacing adjustment: `Logs/visual-finish-final.xml`.
- 115/115 data checks: `Logs/visual-finish-data.xml`.
- Player compilation passed without C# errors: `Logs/visual-finish-player.log`.
- All 383 C# files match the isolated snapshot and have metadata. Final HUD/windows use `Logs/visual-finish-scenes-*`; final NPC/nameplate/effect captures use `Logs/visual-finish-final-*`. The visual review's 213 links resolve. Restart Play; no map regeneration is needed.

## Short Cut correction validation

- 4/4 affected HUD/quickslot scene checks passed: `Logs/shortcut-button-scenes.xml`.
- Final player compilation passed with zero C# errors: `Logs/shortcut-button-player.log`.
- Inspected normal, hover, pressed, disabled and small-screen captures. The shortcut button now stretches a blue margin outside its lettering; original assets and control placement are preserved.

## Quickslot validation

- 36/36 scene checks: `Logs/classic-quickslots-scenes.xml`.
- 112/112 data checks, including 15 new binding/save cases: `Logs/classic-quickslots-data.xml`.
- Five focused HUD/progression/quickslot checks: `Logs/classic-quickslots-layout.xml`.
- Final Windows player compilation passed with zero C# errors: `Logs/classic-quickslots-player.log`.
- All 379 C# files match the isolated snapshot and have metadata. Inspected 1366×768 and 640×480 captures use `Logs/classic-quickslots-layout-*`; the updated review links are valid.

## NPC presentation validation

- 34/34 scene checks passed: `Logs/classic-npc-scenes.xml`.
- After the final bubble/nameplate layering correction, 6/6 affected NPC, quest and navigation checks passed again: `Logs/classic-npc-final.xml`.
- 97/97 data checks passed: `Logs/classic-npc-data.xml`.
- Final Windows player compilation passed without C# errors: `Logs/classic-npc-player.log`.
- All 376 C# sources match the validation snapshot and have metadata. Final captures use `Logs/classic-npc-final-*` at 1366×768 and 640×480.

## Combat and interaction validation

- 30 existing scene checks passed in `Logs/classic-combat-scenes.xml`; both new visual checks passed after correcting their cursor-delay assumption in `Logs/classic-combat-final.xml` (32 distinct passing checks across the runs).
- 93/93 data regression checks passed: `Logs/classic-combat-data.xml`.
- Final Windows player compilation passed with zero C# errors: `Logs/classic-combat-player.log`.
- Final combat/marker captures use `Logs/classic-combat-final-*`, including 1366×768 and 640×480 combat layouts.
- All 370 C# sources match the isolated snapshot and have metadata. The user's existing editor and unsaved scene are preserved.

## Quest validation

- 93/93 focused data checks: `Logs/quests-regression-data.xml`.
- 30/30 scene checks: `Logs/quests-regression-scenes.xml`, including 1366×768 and 640×480 quest/dialogue captures.
- Final Windows player compilation: `Logs/quests-player-clean.log`, passed without C# errors.
- All 364 C# sources match the validation snapshot.

## Navigation validation

- **40/40 focused source-data and offline-loop checks passed**, `Logs/classic-navigation-final-data.xml`, covering real region strings, preview coordinates, hidden/absent previews, atlas selection, live foothold preservation and existing offline transactions/saves.
- The broad scene run passed **27/28** checks and exposed a nameplate camera/canvas regression (`Logs/classic-navigation-final-scenes.xml`). After fixing it, **7/7 affected scene checks passed**, including navigation, map transitions, recovery, monsters and independent Classic windows (`Logs/classic-navigation-labels.xml`). Final capture checks advance the simulation before photographing the hunting map.
- After matching the recent-hit monster-name lifetime, **4/4 navigation, monster and player-combat scene checks passed**, including timer expiry, death cleanup and the final rendered captures (`Logs/classic-navigation-combat-labels.xml`).
- Final navigation captures and the current visual review use `Logs/classic-navigation-combat-labels-*`.
- Final Windows player script compilation passed without C# errors (`Logs/classic-navigation-player-final.log`). All **356 C# files** match the isolated validation snapshot and have metadata. The existing Unity editor and unsaved scene were preserved.

## Earlier window validation

- Focused inventory, exact-slot transactions, save compatibility and progression: **104/104 passed**, `Logs/classic-windows-inventory.xml`.
- Scene checks: **26/26 passed**, `Logs/classic-windows-final-scenes.xml`; **13/13 affected scenes rechecked after final visual fixes**, `Logs/classic-windows-final-layout.xml`.
- Player script compilation passed without C# errors, `Logs/classic-windows-final-player.log`. All 347 main C# files match the isolated validation snapshot. Current captures at 1366×768, 1280×720 and 640×480 were visually reviewed.
- The user's existing Unity editor and unsaved scene are preserved. Validation runs in `Temp/CodexValidation`.
