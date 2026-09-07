# Complete keyboard remapping — 2026-09-06

The user's request extends the native keyboard editor to every displayed key. `GameWorld.Keyboard` now owns one map used by the keyboard window, eight-cell HUD tray, player input and UI commands. This supersedes the fixed-control limit in the previous keyboard/scrollbar pass.

## Editing and input

Every physical key in `ClassicKeyboardLayout` is selectable, draggable, clearable and a drop target. Dragging a bound key to an empty key moves its assignment; dragging to an occupied key swaps both assignments. The source and destination resolve before either changes. Palette, bag and skill-book drops copy assignments without consuming an item or casting a skill. Revision checks reject drags made stale by another edit, restore or reset. Closing the keyboard or changing maps cancels its active drag.

The palette contains all implemented window commands, movement directions, Attack, Jump, seven expressions, NPC talk, the nearby shop, screenshot, keyboard settings and tray visibility, plus learned active skills and supported owned/bound consumables. Right-click / CLEAR KEY clears one physical key; CLEAR ALL clears the entire keyboard. DEFAULT restores movement, commands, expressions, Attack, Jump, potion shortcuts and number aliases. The mouse-operated Default and close buttons remain available even after clearing all controls. Changes apply immediately; OK closes the independent window.

The eight HUD positions continue to represent Shift / Ins / Home / PgUp / Ctrl / Del / End / PgDn. Number keys 1–8 initially reference those positions for compatibility. Editing or dragging a number key materializes its current assignment and makes that key independent; other untouched number aliases continue to follow their tray keys. The alias rule is explained in the window help and hover text. Duplicate bindings pressed in one frame dispatch a consumable, skill or window command only once.

`UnityInputProvider` reads movement, jumping, attacks and expressions from this map. `SkillBar` dispatches window and interaction commands. The old per-window hard-coded checks have been removed, including the previous Escape fallback when the full binding dispatcher exists. The remapped Menu command dismisses only the frontmost window, then opens the local menu when no ordinary window remains. Window hotkeys continue to toggle independently. Typing blocks game bindings, and automatic Unity button navigation/submission is disabled while this gameplay UI owns input so a remapped Return/Space/arrow key cannot also submit or navigate a selected button. The diagnostic F9 fallback yields to any assignment on that key.

## NX composition

`UIWindow.img/KeyConfig/backgrnd` remains the 629×373 source background. Native `KeyConfig/key` glyphs overlay native `KeyConfig/icon` tiles and original item/skill icons. Additional physical keys without a separate NX glyph use a compact text label. Movement/talk/shop/screenshot commands without a matching native action tile use short text captions.

To let the baked Esc / Tab / Scroll Lock action captions move too, every keycap is composed from the original unassigned number-key region: top-left rectangle (319,66,32,32), with a three-pixel sliced border. A blank interior strip at (342,68,1,12) clears the sample key's baked label before the correct glyph and live assignment are drawn. Wider keycaps preserve the native bevel; icons remain 32×32. These are runtime texture regions, with owned Sprite wrappers released on destruction; the NX bitmap is unchanged. The previous VScr4 scrollbar correction is retained.

The source comparison remains `C:/HeavenClient/MapleStory-Client/IO/UITypes/UIKeyConfig.cpp`, `IO/KeyConfig.h` and `IO/KeyAction.h`. Persisted physical/action IDs follow source values where available; additional implemented Unity controls use unused explicit IDs. Layout uses the actual supplied NX bitmap because the current C++ working tree mixes client versions.

## Persistence

Local progress version 5 contains the complete keyboard map. Keys, record counts, unique physical-key ownership, kinds, action IDs, supported consumables, learned active skills and the restricted number-reference structure are validated before any character/map/binding mutation. Invalid data leaves the session unchanged. Saved records and runtime bindings are copied so callers cannot mutate the map without advancing its revision.

Versions 1–3 migrate their learned active skill hotbar and retain the prior default Ctrl Attack behavior. Version 4 preserves its eight mixed bindings, including deliberately empty Ctrl. Both receive the remaining default commands and number aliases. Intentionally cleared version-5 keyboards load empty and are never repopulated by scene initialization. Saving is still the existing explicit Menu → Save progress action; changes survive travel immediately and survive a new session after saving.

## Validation

Focused data/save checks: 91/91 passed (`Logs/full-keyboard-model.xml`), including all physical-key/action combinations, move/swap behavior, reference detachment, duplicate dispatch, copy isolation, malformed-save rejection, empty-map round trips and versions 1–4 migration.

Focused scene checks: 7/7 passed (`Logs/full-keyboard-focused.xml`), including real raycast drag events, old-key inactivity/new-key activation, movement/Attack/Jump/expressions, independent windows, Escape remapping, number detachment, typing suppression, clear/defaults, travel and disk restore into a freshly loaded scene. Wide and small screenshots were visually inspected. The broader scene run passed 46/47 cases (`Logs/full-keyboard-scenes.xml`); its only failure expected the now-obsolete literal WASD hint. After updating that expectation, the complete swimming case passed (`Logs/full-keyboard-swimming.xml`), verifying all 47 scene cases. Final Windows player compilation passed with zero C# errors (`Logs/full-keyboard-player-final.log`). All 394 C# sources have metadata and match the isolated snapshot; the updated review has 247 valid local links. The original Unity editor and scene were preserved.

Online play, choosing a different set of eight HUD tray keys, and unrelated pre-existing failing tests remain outside this change.
