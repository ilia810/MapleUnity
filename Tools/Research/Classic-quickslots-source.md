# Classic quickslots — 6 September 2026

This follows the raised two-by-four tray in the user's nine preserved screenshots. The native artwork is composed directly in Unity; no NX archive, screenshot or bitmap is repainted.

## Source and artwork

- `C:/HeavenClient/MapleStory-Client/IO/UITypes/UIStatusBar.cpp` loads `StatusBar.img/key/0..7` and maps its two rows to Shift / Insert / Home / Page Up and Ctrl / Delete / End / Page Down. Its item rendering uses total inventory counts and deliberately retains zero when exhausted. Its drop routing rejects targeted consumables.
- The existing 151×80 `StatusBar.img/base/quickSlot` has a measured 35-pixel horizontal and 34-pixel vertical pitch. Unity keeps the established 32×32 icon rectangles at (7+35×column, 7+34×row), avoiding mixed-version offsets in the C++ working tree.
- `StatusBar.img/key/0..7` provides the original outlined labels, 18–28 pixels wide and 11–13 pixels high. Labels render above the icons at native size. `Basic.img/ItemNo` supplies quantities and learned-level glyphs. Item totals align to the lower left; skill levels retain the lower-right display established in this rewrite.
- `IO/KeyAction.h` and `IO/UITypes/UIKeyConfig.cpp` identify Attack=52 and Jump=53. The original 32×32 `UIWindow.img/KeyConfig/icon/52` and `/53` provide the action tiles. Both were exported read-only and inspected under `Logs/classic-quickslot-assets`.
- The existing radial cooldown overlay now has an actual one-pixel sprite from the HUD gauge, so Unity's Filled image geometry can render it. A sprite-less Image would ignore the requested radial fill.

## Current behavior

- Both original keys and existing 1–8 aliases activate the same eight bindings. Pressing both aliases for one item in the same frame dispatches once. The tray can be hidden without disabling bindings.
- Fresh sessions start with Ctrl=Attack, Page Up=Red Potion and Page Down=Blue Potion. Zero-stock items remain visible, dimmed, with a zero count. Replenishing or moving/merging bag stacks updates the total without rebinding.
- Drag a supported recovery/stat-buff consumable from the bag onto a slot. Assignment does not remove, move or consume the bag stack. Existing inventory revision checks reject stale drags. Gear, ammunition and currently unsupported effects cannot be assigned as usable items.
- Drag a learned active skill from the book to bind it, or use its right-click assignment controls. Skill-to-slot dragging copies the binding. Dragging between quickslots swaps bindings; releasing outside preserves them. Changing a binding or map during a drag cancels/rejects it.
- Right-click a slot or click the HUD's Short Cut button to open an independent shortcut window. Select a row to assign Attack, Jump or Clear. The window follows the shared focus, drag, bounds and Escape behavior. This is a compact local settings window, not a reconstruction of a complete original keyboard editor.
- Ctrl is owned by its slot. Rebinding it to a potion/skill does not also send a basic attack. Z stays the independent attack key; Space/Alt keep jumping. Clicked actions enter the existing simulation input path and are consumed once. Keyboard actions, movement and item/skill dispatch stop while editing an InputField. Map travel and loading discard queued clicks.
- Skill job, weapon, MP, cooldown, death and movement restrictions remain enforced by SkillManager. Item effects still pass through the existing local inventory use rules. Hover cards identify the binding, key aliases and current item total/skill level.

## Persistence

Manual saves now use version 4 with explicit Empty/Skill/Item/Action records. The old `Hotbar` skill projection remains for callers and format diagnostics. Versions 1–3 still load through their existing progression and quest migrations; their first eight skill assignments are restored, with Attack placed in the Ctrl slot only when it was empty. Version 4 retains intentionally empty bindings, exhausted item bindings, and learned off-job skills.

The world validates the whole save before mutation, including binding kind/ID, maximum eight records, supported item effects, learned active skills and supported actions. Version-4 quest records still receive the version-3 quest validation. Invalid records leave character progress and the map untouched. Save-file backup behavior and file location remain unchanged.

## Scope

This pass does not add a complete keyboard remapping screen, targeted scroll effects, sitting/chair actions, item-use repeat timing, or skill macros. Those need their associated gameplay behavior. Online play remains excluded by the user's request. Fine HUD typography at scaled resolutions, broader NPC animation and further event effects remain visual work.

## Validation

The complete 36-check scene suite passed in `Logs/classic-quickslots-scenes.xml`; all 112 selected data checks passed in `Logs/classic-quickslots-data.xml`. The new data cases cover mixed JSON records, exhausted items, off-job skills, malformed bindings/quest records and old schema versions. Rendered scene checks exercise actual UI raycasts, item/skill drags, swaps, stale bag revisions, total counts, consumption, empty/refilled bindings, original labels, shortcut controls, hidden-tray keys, typing, Ctrl ownership, clicked actions and fresh-scene save/load. The focused jump check advances the existing simulation through its departure from the foothold before asserting the airborne state.

Final player compilation passed with zero C# errors in `Logs/classic-quickslots-player.log`. All 379 C# files match the isolated validation snapshot and have metadata. Current captures are `Logs/classic-quickslots-layout-{quickslots-wide,quickslots-small,shortcut-settings-small,earned-skills}.png`. The original user editor and unsaved scene were preserved.
