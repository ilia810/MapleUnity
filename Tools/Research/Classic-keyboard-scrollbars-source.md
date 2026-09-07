# Native keyboard window and scrollbar artwork — 2026-09-06

> The initial eight-key editing limit below is superseded by [complete keyboard remapping](Classic-full-keyboard-source.md). Every displayed key is now editable and saved in version 5; the scrollbar findings remain current.

## Evidence

The requested keyboard silhouette is present in the local `C:/HeavenClient/MapleStory-Client/nx/UI.nx`: `UIWindow.img/KeyConfig/backgrnd` is 629×373 with origin (314,186). It contains the complete keyboard, title, button strip and 54 action cells. The nine supplied reference images do not show this keyboard editor; its artwork comes directly from NX. The small `UIWindow.img/ShortCut/backgrnd` is a navigation menu, not this keyboard.

The C++ working tree's `IO/UITypes/UIKeyConfig.cpp`, `IO/KeyConfig.h` and `IO/KeyAction.h` establish the original keyboard/action relationship. Its current constructor offsets and button paths mix client versions, so Unity uses the actual bitmap's top-left key positions and `Bt*` children. The original assets and user reference images are unchanged. Read-only exports are under `Logs/keyboard-assets` and `Logs/scrollbar-variants`.

## Keyboard composition and controls

- The complete native background renders once at 629×373. It is not stretched to invent a frame. The normal independent `ClassicWindow` focus, drag, close and viewport-fit behavior applies. The bag, skills and other windows can remain open.
- The eight existing Shift / Ins / Home / PgUp / Ctrl / Del / End / PgDn bindings appear on their physical keys. Number keys 1–8 display and edit those same assignments. Native `KeyConfig/key` glyphs overlay original action, item and skill icons. Large modifier keys retain their full hit areas; icons retain 32×32 dimensions.
- Click a key and then a palette icon, or drag from the bag, skill book, palette, another key or the tray. Slot-to-slot drops swap the assignments; bag/skill/palette drops copy the binding without consuming or moving items. Existing validation rejects unsupported items, unlearned/passive skills and stale drags. Closing the keyboard cancels a drag originating there.
- Right-click clears one key. CLEAR KEY clears the selected key, native CLEAR ALL clears the eight bindings, and native DEFAULT restores the existing fresh-scene defaults (Ctrl Attack, PgUp Red Potion, PgDn Blue Potion). These edits apply immediately, as in the previous shortcut list. OK and × close the window. A Cancel button is deliberately not presented because this is not a staged transaction.
- The palette contains Attack, Jump, learned active skills and supported owned/bound consumables, with 54 cells per page. Mouse-wheel and arrow paging retain native cell dimensions. Changes use the existing eight-binding save schema and input dispatch; no save migration is needed.
- Fixed movement, expression, inventory, equipment, skills, quest, map, NPC and shop controls are displayed as a guide. They cannot be rebound by clicking or dropping in this pass. The original background also contains its authored Esc/Tab/Scroll Lock captions. Arbitrary remapping of every keyboard key remains separate input/rewrite work; this implementation does not pretend fixed keys are editable.

## Scrollbar correction

The preceding implementation used `Basic.img/VScr`, a plain fill in place of its track, and a vertically stretched thumb. The rounded blue reference style is available in `Basic.img/VScr4`:

| Piece | Native dimensions |
| --- | --- |
| enabled/disabled base | 15×13 |
| prev0/prev1/next0/next1 and disabled arrows | 15×13 |
| thumb0/thumb1 | 15×25 |

Shared controls now tile the original base and retain the thumb's 15×25 pixels, including its real pointer hit area. Normal, highlighted/pressed and disabled states come from NX. The skill book, bag, quest list/story, dialogue story and both shop columns share this implementation. Bag and text scrollbars have arrow controls; controls that have no overflow show disabled artwork or use their existing auto-hide policy.

`IO/Components/Slider.cpp` confirms the source renders fixed-size thumbs and repeating track pieces. Its current callers select the older thin `LINE_CYAN` variant; choosing VScr4 here follows the user's supplied visual references rather than copying those mixed-version style selections. Unity retains its current list and ScrollRect behavior. `ClassicScrollbarSkin` reapplies the native handle size after canvas layout so ScrollRect's proportional content ratio cannot stretch the bitmap or its hit area.

## Remaining scope

Selecting a different set of eight tray keys, configurable quest-helper AUTO, remaining class/action art and matched-map/character comparisons remain separate work. Online play stays excluded and the older unrelated failing suite stays deferred.

## Validation

45/45 scene regression checks passed in `Logs/keyboard-scrollbars-scenes.xml`. After the final cached skill-icon change, 8/8 affected cases passed in `Logs/keyboard-scrollbars-verified.xml`, covering real raycast/drag routing, simultaneous windows, keyboard title dragging, slot/number-alias synchronization, non-consuming item assignment, default/clear actions, rejected stale drags, drag cleanup on close, native scrollbar dimensions, disabled states, shop dragging and quest-story arrow scrolling. Captures were inspected at 1366×768 and 640×480.

Final Windows player compilation passed with zero C# errors in `Logs/keyboard-scrollbars-player-final.log`. All 389 C# files have metadata and match the isolated snapshot. The review contains 242 valid local links; final keyboard/scrollbar images use `Logs/keyboard-scrollbars-verified-*`. Only the original Unity editor (PID 26408) remains open. The previous unrelated failing tests were not rerun or changed.
