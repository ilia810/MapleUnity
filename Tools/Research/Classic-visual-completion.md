# Classic offline visual completion — 2026-09-07

The visual checklist for the currently implemented offline client is complete. The review covers all nine supplied reference scenes, the original keyboard, scrollbars, simultaneous windows, supported actor poses and existing combat/progression effects. This is an implementation using the local NX pack; it is not a pixel-identical copy of a different MapleStory release or a claim that the C++ gameplay rewrite is complete.

Open [the comparison gallery](../ArtReferences/MapleClassic/review.html). It leads with the nine matching maps and current captures, followed by equipment close-ups, long-content checks and the earlier functional UI comparisons. The initial audit remains historical.

## Final corrections

- Inventory tab captions now stay white and use their native dimensions in both selected and inactive states. Those bitmaps have different widths; changing only the sprite previously stretched the lettering. Skill-tier captions use the same centered native-size placement. Text-based native tabs use white lettering with a subtle shadow.
- Skill names and book titles fit their frames and retain their full source strings for hover details. The Ice & Lightning and Fire & Poison books have concise headers. Explorer job names now include third/fourth jobs for display; this does not unlock their gameplay.
- Character stats use original bitmap label regions wherever this NX pack supplies the matching label. The three newer captions use condensed full text, including an unclipped CRIT. DAMAGE. The job row has separate family and job lines. Long names, stats and HUD health values end with an ellipsis instead of crossing into adjacent cells.
- NPC dialogue is 529×211 instead of 529×246. Six native middle strips plus a five-pixel strip finish the frame without stretching its dither. Text stays within a 128-pixel viewport; both portrait orientations keep independent name-bar placement. Long NPC menus now scroll too, and the action footer remains reachable at 640×480.
- Headbands preserve the overhead hair; half-cover hats use `backHairBelowCap` on ladders/ropes, and full-cover helmets hide the back-hair layer. Long-hair below-body parts are rendered behind clothing/capes. The three source cap types follow HeavenClient; later NX masks containing `Hb`/`Hd` also suppress back hair.
- Face accessories, glasses and earrings can be equipped and saved in their existing equipment slots. They attach to the brow/head instead of the navel and use the correct draw order. Face accessories handle the unnumbered default layer and numbered expression layers, and update when the expression changes while the body is still. Face/eye accessories disappear on rear-facing climbing poses. Shop portraits use the same composition.
- Small authored interiors retain black margins even when their backgrounds tile across the camera. `ClassicInteriorMask` frames rooms up to the classic 800×600 aperture, responds to camera aspect changes, and clears on travel to larger maps. It does not shrink or cover the HUD/windows.
- Compact minimaps use a 140-pixel minimum and no longer reserve space for hidden location text. Full and collapsed modes still make room for their displayed names.

## Reference matrix

| Reference | Matching local map | Visuals checked | State qualification |
|---|---|---|---|
| 1 — combat HUD | 200010300, Stairway to the Sky II | Lower platforms, Lucidas, ladder, background trees/clouds, compact minimap, centered HUD and raised 2×4 quickslots | Equipment, HP and active buffs depend on the character. Existing combat tests independently cover hit/miss digits, bars, buffs and effects. |
| 2 — skill book | 110020001, Lorang Lorang Lorang | Shell/palm map, native tab states, six skill rows, Ice & Lightning book, scrollbar, compact minimap | The book displays source skill data; learned levels and supported actions follow offline progression. |
| 3 — right dialogue | 101000000, Ellinia | Betty portrait, compact mirrored frame, normal text/buttons, world layering | Reference-like text is a read-only test fixture. Betty's referenced delivery chain is not implemented. |
| 4 — shop | 200000002, Orbis Department Store | Original paired frames, Edel portrait, equipped player, item/price rows and black room margins | The preview uses Luna's working catalog. Orbis transactions are not enabled. |
| 5 — bag | 200000000, Orbis | Native category captions, 5×6 cells, item icons, quantities, meso footer, full minimap | Contents and NPC populations come from the local save/NX pack. |
| 6 — ordinary tooltip | 200080700, Orbis Tower 15F | Original tower/lighting, scroll icon and description, translucent tooltip, window layering | Item hover is real; the test chooses the return scroll explicitly. |
| 7 — comparison | 211000101, El Nath Weapon Store | Rumi portrait, paired equipment cards, requirements, stat rows, black margins | The shop is a read-only skin preview. Comparison uses real loaded equipment data and the equipped shoes. |
| 8 — left dialogue | 101000300, Ellinia Station | Cherry portrait on the left, correctly oriented frame/text, station scenery and speech bubbles | Dialogue text is a read-only fixture. Boat scheduling, ticket service and departure clock belong to the unported transport script. |
| 9 — stats | 102040001, Northern Top of Construction Site | Native stat labels, two-line job, attached details, unclipped captions, platforms and monsters | Values reflect the offline formulas; the screenshot's fame and extra AP controls are not implemented gameplay. |

## Other completed visual surfaces

| Surface | Coverage |
|---|---|
| Original Short Cut / keyboard | Native independent keyboard, draggable physical-key assignments, palette, swaps, expressions/movement/window commands, save migration, 640×480 bounds. |
| Scrollbars | Original VScr4 tracks, fixed-size thumb, arrow/hover/pressed/disabled states, wheel scrolling, and owner-window lifetime. |
| Window management | Bag, equipment, skills, stats, journal and keyboard can coexist, move independently, click to front and close in focus order. |
| Quest journal/helper | Original assets, available/in-progress/completed tabs, objectives, tracking, per-entry remove, draggable helper, AUTO, collapsed/empty headers and preference persistence. |
| HUD/messages | Native controls and safe Short Cut stretch margins, HP/MP/EXP gauges, mixed quickslot tiles, quantities/cooldowns, notice strip, resizable scrolling history and disabled envelope. |
| Navigation and world labels | Native minimap modes, live markers, atlas, measured nameplates, overlap handling, NPC speech and original cursor states. |
| Actors/actions | Supported clothing/weapon grips, hats/accessories, walking/jump/climbing/swimming poses, faces, authored NPC animations, standing portraits, projectiles, afterimages, hits/misses and progression/buff effects. |

## Source and implementation evidence

Original art remains in `C:/HeavenClient/MapleStory-Client/nx`. The final native caption/label inventory is exported read-only to `Logs/visual-completion-assets`; dialogue strips are in `Logs/dialogue-frame-assets`. User reference PNGs are unchanged. No generated replacement artwork or Photoshop/GIMP dependency was added.

Attachment and hat rules were checked against `Character/Look/Clothing.cpp`, `CharEquips.cpp` and `CharLook.cpp`; job names against `Character/Job.cpp`. The source headers credit Daniel Allendorf and Ryan Payton and specify AGPL-3.0-or-later. `NxEquipmentFrames` retains its attribution; the renderer identifies the adapted draw rules.

The final scene tests exercise actual equipment acceptance, accessory save/load, expression changes, front/rear hat layers, standing portrait parity, real NX map loading, native tab dimensions/colors, long stat/name content, and scrolling dialogue. Shop/text fixtures are confined to the test assembly. They do not alter NPC catalogs or enable services in normal play.

## Validation

- **53/53 PlayMode scene checks passed:** `Logs/visual-completion-scenes.xml`.
- **5/5 affected scene checks passed again** after the compact-minimap adjustment: `Logs/visual-completion-final.xml`.
- **105/105 focused data checks passed:** equipment, inventory slots, local save/load/economy and character progression, in `Logs/visual-completion-data.xml`. The deferred unrelated suite was not run.
- **Windows player compilation passed, with zero C# errors:** `Logs/visual-completion-player-final.log`, containing `PLAYER_COMPILATION_VALIDATION_PASSED:`.
- All **398 C# source files** have metadata and match the isolated `Temp/CodexValidation` snapshot. The review contains **336 valid local file references**.
- The only remaining Unity editor is the original PID **26408**. Its unsaved scene was preserved.
- Final reference-map and equipment close-up captures use `Logs/visual-completion-final-*`; other current captures use `Logs/visual-completion-scenes-*`. Earlier focused/v2/v3/v4 runs document intermediate corrections, not final results.

The captions, portraits, dialogue, minimap and interior-margin captures were inspected directly, including all nine reference states, all six front/rear hat views, equipment comparisons and 640×480 long-dialogue handling.

## Limits and remaining rewrite work

The local NX release has different map decoration, NPC populations, minimap canvases, text/art dither and some frame details from the supplied newer Classic release. The exact reference character outfit is not the local saved character. The source client's default top `1042399` is absent from this pack; a real equipped shirt renders correctly. No substitute outfit is silently granted.

Additional class actions and their effects, conditional quest NPC appearances, more quests, transport/boat/clock scripts, additional shop/service catalogs, and fame/HP-MP/bulk-AP gameplay remain rewrite work. Deco and unavailable online features do not have fabricated functioning controls. Their presentation must be completed together with their actual behavior and available source data. Online play and the previously deferred unrelated failing tests remain outside this milestone.

To test the completed visuals, allow Unity to compile and restart Play in the current scene. Map regeneration is unnecessary. Use I/E/K/C/Q for the main windows and journal, Short Cut for the keyboard, Tab for minimap modes, and N near Luna in Henesys General Store for real trading. Defaults may differ if the keyboard has been rebound. The comparison gallery also includes the reference-map previews that are not currently reachable through implemented NPC services.

## Subsequent gameplay visuals — 2026-09-07

The [support skill rewrite](Support-skills-source.md) adds the original cast effects and icons for eight defensive/support skills, including the C++ character echo used by Iron Body and Magic Armor. Its [scene review](../ArtReferences/SupportSkills/review.html) is separate from the nine-reference UI comparison. These skills now have implemented offline behavior; broader class actions and the other limits above remain.
