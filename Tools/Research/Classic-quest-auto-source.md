# Quest helper AUTO control — 2026-09-07

## Reference and original artwork

The user's reference images 3–9 show an orange AUTO control immediately before the helper's minimize and close controls. The original `UIWindow.img/QuestAlarm/BtAuto` assets are present in the local `C:/HeavenClient/MapleStory-Client/nx/UI.nx`: normal, mouseOver, pressed and disabled are all 21×12 pixels. Read-only exports and dimensions are retained in `Logs/quest-auto-assets`.

The helper now places that button at (168, 3) within its existing 223-pixel frame, followed by minimize at (191, 3) and the original blue `Basic.img/BtClose` at (207, 3). The title ends before these controls. The normal orange artwork represents ON; the gray disabled artwork represents OFF while the button remains clickable and retains its original hover/pressed states. No bitmap is painted, stretched or replaced.

The screenshots establish the appearance, not the precise automatic-selection algorithm. The inspected C++ checkout has no separate QuestAlarm implementation. The local behavior below extends the existing auto-on-accept rule; it does not claim parity with undocumented Classic server behavior.

## Local behavior

- AUTO defaults to ON. Newly accepted quests enter the helper when a tracking slot is available; the existing five-quest limit remains.
- Turning AUTO off affects future acceptances. Existing tracked quests stay tracked, and manually untracked quests are not re-added by toggling, inventory updates or hunt progress.
- Manual Track/Untrack remains available in the journal in either mode. Tracking never changes quest acceptance, objectives, rewards or abandonment.
- When active quests exist but none are tracked, the native collapsed 20-pixel header stays available with `Quest Helper (0/5)`. AUTO and close remain clickable; expansion is disabled until a quest is tracked. The helper disappears when no active quests remain.
- The blue close button hides the helper without clearing tracking. The journal footer has Show helper / Hide helper controls; manually tracking a quest also reopens the helper. Hidden state lasts for the current session and survives map travel. Quest rows still open their journal entry when clicked.
- AUTO remains available while deliberately collapsed. The user's preference is stored in `ClassicUI.QuestHelper.AutoTrackAccepted` alongside the existing local UI preferences. Character-save tracking records retain their existing meaning and schema. Loading character progress does not reset this UI preference.
- Batch validation does not read or write the user's UI preferences. Online party, chat and network quest behavior remain excluded.

## Validation

Focused quest integration checks cover automatic versus manual acceptance, preserved manual tracking, credited hunt progress, save restoration and accepting again after changing AUTO. Scene checks exercise the real controls, native on/off sprites and dimensions, title/control separation, collapsed and empty headers, actual NPC acceptance with AUTO off, manual tracking, travel, hide/reopen through the journal, manual tracking while hidden and independent windows.

**17/17 quest integration checks passed** (`Logs/quest-auto-data.xml`). **All 11 affected UI scenarios are verified:** 10/11 passed in `Logs/quest-auto-final.xml`; the message-history case hit an exact floating-point size assertion, corrected to a 0.001-pixel tolerance, and both history cases passed in `Logs/quest-auto-message-final.xml`. The AUTO, header, hide/reopen and small-display captures were inspected. Final Windows player compilation passed with **zero C# errors** (`Logs/quest-auto-player-final.log`). All **396 C# sources** have metadata and match the isolated snapshot; the review has **281 valid local links**. The original Unity editor and scene are preserved.

The original older failing suite was left alone. The corrected message-history assertion belongs to the focused HUD checks introduced in the preceding visual pass; runtime scrollbar geometry was unchanged.
