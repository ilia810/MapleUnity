# Offline quests and the classic journal

Source inspected: `C:/HeavenClient/MapleStory-Client` working tree, 2026-09-06. Online quest packets and server scripts remain outside this milestone.

## Source findings

- `IO/UITypes/UIQuestLog.cpp` reads `UIWindow.img/Quest/list`. That child is absent in this installed NX set. The actual assets are directly under `UIWindow.img/Quest`: `backgrnd` (245×396), `backgrnd2` (305×396), and the tab/button art. Unity now composes the two native panes into an independent 550×396 journal.
- `IO/UITypes/UINpcTalk.cpp` uses `UIWindow.img/UtilDlgEx`: top (529×28), repeating center (529×20), bottom (529×58), name bar and End Chat/Next/Previous/Yes/No/OK buttons. The center must repeat at native height; stretching its dither creates stripes. Dialogue shows the linked original NPC portrait at native size, with a scrollable text area and paged source dialogue.
- `Character/QuestLog.cpp` represents quest states and kill progress; the original client exchanges NPC and quest packets with a server. The Unity offline evaluator is explicitly limited to audited data-only quests, not a replacement for arbitrary server scripts.
- `Quest.nx/Check.img/{id}/0` and `/1` hold starting and completion requirements. `Act.img` holds item/EXP/meso actions. `QuestInfo.img` supplies titles and descriptions, and `Say.img` supplies the English dialogue. Some Act nodes duplicate localized dialogue; these are text, not executable actions. `nextQuest` is a follow-up hint, never automatic acceptance.
- `UIWindow.img/QuestAlarm` supplies the translucent helper frame. It remains separate from the journal, scales to fit shorter viewports, and opens the selected tracked quest when clicked.

## Audited playable quests

| ID | Quest | Source requirements / route | Reward |
|---|---|---|---|
| 1037 | Help Hunt the Snails | Beginner, level 1–10; Sam in Dangerous Forest (50000); defeat 10 Snails after acceptance, then speak to Maria in Amherst (1000000) | 60 EXP |
| 1038 | Maria's Letter | Beginner, level 1–10, quest 1037 completed; Maria gives one letter (4031800), deliver it to Lucas in Amherst | 40 EXP, 5 Red Potions, 5 Blue Potions; letter consumed |
| 1039 | Helping Out Yoona | Beginner, level 1–10; Yoona at 1010000; defeat 10 Blue Snails and 10 Shrooms after acceptance | 100 EXP |
| 2088 | The Reason Behind the Mushroom Studies | Level 10+; Bruce in Henesys; collect 10 Mushroom Spores (4000011) and 40 Orange Mushroom Caps (4000001), then return to Bruce | 300 EXP, 25 Red Potions; materials consumed |

Henesys's nearby maps contain Shrooms (100010000) and Orange Mushrooms (100020000). NX client data has no server loot tables: the existing **explicit local drop policy** now gives Shrooms one Mushroom Spore. This is a local gameplay rule, not a claim about the original server's drop rate. Original quest requirements, item amounts and rewards are retained.

`NxQuestData` uses an allowlist and rejects unknown requirement/action fields, random rewards and job-filtered item rewards. Event, quiz, auto-start and scripted quest flows are not accidentally enabled because their basic item counts happen to parse. More quests require source audit and support for their actual behavior.

## State and interactions

- Q or Menu → Quest journal opens Available / In Progress / Completed. Inventory, skills, stats, dialogue and the journal can remain open together. Each ordinary window keeps its own position and uses the common focus/Escape stack.
- V speaks to the nearest eligible NPC; double-clicking a visible NPC selects that NPC. UI raycasts prevent clicks through windows. NPC interactions validate the actual map-spawn object, horizontal/vertical proximity, living/idle state and recipient identity. Walking away, attacking, death or changing maps closes the conversation.
- Acceptance automatically tracks an active quest when there is room. Tracking is limited to five. Collection progress reads current bag contents, while hunt counts increment only on credited player combat kills after acceptance and saturate at the requirement.
- Completion preflights both removals and grants against bag capacity and meso capacity. Repeated or re-entrant completion cannot award twice. Abandon requires a second click in the UI, clears hunt progress, and reclaims issued delivery items before reacceptance. Maria can replace a missing letter without granting an extra copy or other acceptance rewards.
- Save schema 3 records active/completed state, per-monster counts and tracking. Versions 1 and 2 load with an empty journal. Unknown IDs, duplicate entries, invalid states/counts or missing completed prerequisites reject the whole load before changing the character or map. The existing manual-save file path and atomic replacement/backup behavior are unchanged.

## Remaining work

The catalog currently contains these four quests. General scripted NPC services, repeatable/event quests, selectable/random rewards and quest map navigation need further implementation. Available/ready overhead indicators are implemented with the original animated QuestIcon assets; see [combat and interaction visuals](Classic-combat-visuals-source.md). Ambient speech bubbles and both NPC portrait layouts are now implemented; see [NPC presentation](Classic-npc-presentation-source.md) for the approach-side rule used by offline conversations. NX and reference screenshots were not edited.

Validation results and final rendered captures are recorded in `PROJECT_STATUS.md` and the classic UI comparison page.
