# Quest helper controls and placement — 2026-09-06

## Evidence and scope

User reference images 3, 5, 7, 8 and 9 show a translucent quest helper, compact title controls, per-quest close icons and counts preceding objective names. The inspected C++ working tree at `C:/HeavenClient/MapleStory-Client` contains a mixed-version `UIQuestLog` implementation and no separate `UIQuestAlarm` class. Consequently this pass combines the user's visible reference with original UI.nx assets and the existing offline quest model; it does not claim the screenshots establish every original interaction.

## Original artwork and readable content

- The helper uses `UIWindow.img/QuestAlarm/backgrndmax`, `backgrndcenter`, `backgrndbottom` and the distinct 223×20 `backgrndmin` when collapsed.
- Header controls use native 12×12 `Basic.img/BtMin` / `BtMax` and `QuestAlarm/BtQ` artwork. The Q icon opens the journal. Per-quest removal uses the original 14×14 outlined `Basic.img/BtClose2`, including hover and pressed states.
- Titles and objectives use 12-pixel type. Each title leaves room for its close control. Objective counts precede names; return-to-NPC text appears when completion requirements are met. Counts still come from current inventory and credited hunt progress.
- Removal calls the existing `OfflineQuestLog.SetTracked(false)` and never abandons the quest or removes issued items. The journal's Track button restores the helper entry. Collapsing preserves tracking; Q and journal windows remain independent.
- The journal list has original VScroller arrows; a list that fits hides its thumb and disables scrolling controls. Story scrolling retains its existing independent viewport.

## Placement and buff coexistence

The helper header is draggable and uses the glove cursor’s grabbable/dragging states. Its preferred top-left position is stored in normalized canvas coordinates and is separate from temporary screen clamps. Returning from a smaller viewport restores the preferred position. Deliberate drag completion stores the visible position using the existing `ClassicUI.*` preference convention; batch validation neither reads nor writes the user's saved UI preferences.

The helper reserves the bottom HUD/quickslot area and the top band occupied by active buffs. With no buffs, its default top margin is 16 pixels. It uniformly scales only when the viewport cannot contain all tracked content. Buffs now expose their actual occupied size, use stable source-ID ordering, and wrap within the available width rather than extending beyond the screen. The deterministic ID ordering and wrapping are local layout choices; C++ `UIBuffList` uses an unordered icon map and one row. Expiry uses the existing simulation clock and the original buff state is unchanged.

## Validation

42/42 scene regression cases passed (`Logs/quest-helper-scenes.xml`); 4/4 helper/cursor cases passed again after the final grab affordance change (`Logs/quest-helper-cursor-final.xml`). Final Windows player compilation passed with zero C# errors (`Logs/quest-helper-player-final.log`). All 386 C# sources have metadata and match the isolated snapshot. The review has 229 valid local file links; final helper captures use `Logs/quest-helper-cursor-final-*`. The original editor and unsaved scene were preserved. Coverage includes native collapse/expand states, untracking without abandonment, retracking, independent open windows, multiple tracked quests, live inventory counts, active buffs, drag hit routing, small-screen bounds, resize round trips and map travel/save restoration.

Follow-up on 2026-09-07: the [original AUTO control and local preference](Classic-quest-auto-source.md) are now implemented, including an accessible empty header. The validation results above describe the earlier helper milestone. Online party and chat features remain excluded. Original NX files and reference images are unchanged.
