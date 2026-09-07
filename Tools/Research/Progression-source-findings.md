> Update — 2026-09-07: Magic Guard and Magic Armor now work with earned first-job SP; Magic Armor requires Guard 3. Amazon Blessing 3 unlocks Focus, and Nimble Body accepts earned SP. Disorder and Double Stab now accept earned SP too, with Disorder 3 unlocking Dark Sight. See [the thief skill milestone](Thief-skills-source.md). Iron Body now casts, but its Endure/recovery/growth prerequisite chain remains unavailable through normal SP. See [the support skill milestone](Support-skills-source.md). The original investigation below records the earlier baseline.

# Progression source findings

Research date: 2026-09-06. Read-only investigation of the Unity progression excerpts, `C:/HeavenClient/MapleStory-Client`, and its `nx` assets. No code/assets changed. Binary node paths below are precise source locations; the NX metadata was read with the existing `Tools/Research/ui_nx_inspect.py` `Nx` reader.

## Authority boundary

The C++ checkout is a client. It sends AP/SP requests and receives resulting stats/skills; it does not implement point rewards, advancement eligibility, or base HP/MP growth. `Character/Player.cpp:435-443` only plays the level effect and sets the received level. `Character/Player.cpp:455-459` only plays the job effect and changes the received job. `Net/Handlers/PlayerHandlers.cpp:90-120` reads LEVEL, JOB and other stat values from a packet; `:150-159` identifies AP/MAXHP/MAXMP among the stats that refresh UI. `Net/Handlers/Helpers/LoginParser.cpp:148-165` reads initial MAXHP/MAXMP/AP/SP from the server.

A bounded top-level directory inspection of `C:/HeavenClient` found no apparent server implementation checkout. The promising names `wz-classic-server` and `wz-private-server` contain WZ asset directories (`Skill.wz`, `Npc.wz`, `Quest.wz`, etc.), not Java/C#/JS server logic at their roots. `HeavenClientNX` is another client source tree and `gms-v92` contains the game client/WZs. This is evidence about the inspected locations, not a claim that no server exists anywhere on the machine.

## Exact client AP/SP behavior

- AP UI buttons (HP, MP, STR, DEX, INT, LUK, automatic allocation) are inactive for job ID 0: `IO/UITypes/UIStatsInfo.cpp:141-151`, refreshed at `:365-377`. `:551-560` enables their button states only while AP is positive. These are UI restrictions, not server validation.
- A click dispatches `SpendApPacket(stat)` and disables UI: `UIStatsInfo.cpp:520-524`. `Net/Packets/PlayerPackets.h:28-39` writes a time and the stat code. No allocation amount/cap rule is implemented there.
- Automatic allocation uses all currently available AP and the job's primary stat: `UIStatsInfo.cpp:430-481`; `Character/Job.cpp:235-251` chooses INT for magician, DEX for archer, LUK for rogue, DEX for gun pirate/STR for other pirate, STR otherwise.
- `Character/StatCaps.h:28-45` provides **calculated equipment-total caps**, including 999 STR/DEX/INT/LUK and 30000 HP/MP. `Character/CharStats.cpp:156-166` clamps calculated totals. This is not evidence for an AP-spending validator's base-stat cap, though 999 is a reasonable explicitly local guard.
- Beginner SP is `min(level - 1, 6)` minus the learned levels of IDs **1000, 1001, 1002 only**: `IO/UITypes/UISkillBook.cpp:801-824`; names/constants at `Character/SkillId.h:29-31`. It is computed when the beginner tab is shown, including for an advanced job. Other beginner/special skills are not debited by this formula.
- Other tabs show the server-provided shared SP field: `UISkillBook.cpp:826-829`. There is no first-job-family ledger in this client; such a ledger would be an offline practice-isolation rule.
- `UISkillBook.cpp:946-972` rejects raising a skill with no points, at its master level, or for ID 12 (ANGEL_BLESSING / asset name Blessing of the Fairy), then checks prerequisites. It first uses a learned book entry's master level, falling back to the asset master level if zero. `Data/SkillData.cpp:83` defines that asset master level as the count of level nodes. `UISkillBook.cpp:1095-1111` requires each prerequisite's learned level to meet its minimum.
- Skill lists come from the tab's ancestor job: `UISkillBook.cpp:851-878`. An invisible skill without an unlocked master level is omitted at `:873-876`. `Data/SkillData.cpp:88-96` loads numeric `req` children. A successful request sends only a time and skill ID (`Net/Packets/PlayerPackets.h:41-51`; `UISkillBook.cpp:1026-1031`).
- Job ancestry is already faithfully represented by Unity's `SourceSkillRules.JobBranches`: `Character/Job.cpp:32-49` classifies 0/beginner, hundreds/first, tens/second, last digit 1/third, otherwise fourth; `:52-69` tests `skillId / 10000` against ancestors; `:77-99` returns 0, hundreds, tens, current third or fourth-minus-one, and current fourth. It is not a general model for all later MapleStory job systems.

## First-job threshold clues, and missing rewards/growth

`nx/Quest.nx` contains these recommendation quests:

| Quest | Name in `QuestInfo.img/<id>/name` | `Check.img/<id>/0/lvmin` | Script |
|---|---|---:|---|
| 1049 | Becoming a Warrior | 10 | q1049s |
| 1050 | Becoming a Magician | 8 | q1050s |
| 1051 | Becoming a Bowman | 10 | q1051s |
| 1052 | Becoming a Thief | 10 | q1052s |
| 1053 | Becoming a Pirate | 10 | q1053s |

Each has beginner job 0, quest 1048 prerequisite, `lvmax=20`, NPC 9010000, `startscript` as listed, and `end=2009010100`. Their `Act.img/<id>` contains no scalar rewards. These are historical recommendation-quest hints, not complete/current job advancement requirements. The referenced script bodies are not in the client. They support choosing 8/10 as an offline threshold but do not prove no stat/NPC/quest requirement. No local source was found for 5 AP per level, 1 first-job SP, 3 SP per subsequent level, advancement HP/MP bonuses, or base natural HP/MP growth.

There is **partial** HP/MP growth metadata: `Skill.nx/100.img/skill/1000001/level/<n>` has `x=4*n`, `y=3*n`; `200.img/skill/2000001/level/<n>` has `x=2*n`, `y=n`. Matching `String.nx/Skill.img/1000001/h1,h10` and `2000001/h1,h10` explicitly describe x as additional growth on level-up and y as additional growth on HP/MP AP allocation. These do not supply the underlying natural growth formulas. Improved MP Recovery 2000000 has description text saying recovery depends on character and skill level, but its level data has no numeric recovery formula. C++ `Character/PassiveBuffs.cpp:93-129` has no handlers for any first-job passive.

## Real first-job skill chains and current Unity coverage

All data below comes from `nx/Skill.nx/<job>.img/skill/<id>`, especially `/req` and `/level`; names come from `nx/String.nx/Skill.img/<id>/name`. A missing req means no numeric prerequisite in this asset. All attack skills in the supported column have max level 20.

| First job | Spendable with existing handlers | Existing unsupported branches |
|---|---|---|
| 100 Swordsman | 1001004 Power Strike; 1001005 Slash Blast requires Power Strike 1 | 1000000 HP Recovery (16) -> 1000001 MaxHP Increase (10), requires Recovery 5 -> 1000002 Endure (8), requires Increase 3 -> 1001003 Iron Body (20), requires Endure 3 |
| 200 Magician | 2001004 Energy Bolt; 2001005 Magic Claw requires Energy Bolt 1 | 2000000 MP Recovery (16) -> 2000001 MaxMP Increase (10), requires Recovery 5; 2001002 Magic Guard (20) -> 2001003 Magic Armor (20), requires Guard 3 |
| 300 Archer | 3001004 Arrow Blow; 3001005 Double Shot requires Arrow Blow 1 | 3000000 Blessing of Amazon (16) -> 3000002 Eye of Amazon (8), requires Blessing 3; 3001003 Focus (20) also requires Blessing 3; 3000001 Critical Shot (20), no req |
| 400 Rogue | 4001344 Lucky Seven, no req (claw) | 4000000 Nimble Body (20) -> 4000001 Keen Eyes (8), requires Body 3; 4001002 Disorder (20) -> 4001003 Dark Sight (20), requires Disorder 3; 4001334 Double Stab (20), no req |
| 500 Pirate | 5001003 Double Shot, no req (gun) | 5000000 Bullet Time (20), 5001001 Flash Fist (20), 5001002 Sommersault Kick (20), 5001005 Dash (10), all no req |

Unity evidence: `Assets/Scripts/GameLogic/Skills/SourceSkillRules.cs:31-41` enumerates executable attacks and their weapon families. `:58-76` supports special skill 12 and several second/fourth-job passives, but no first-job passive. `Assets/Scripts/GameLogic/Skills/SkillManager.cs:106` rejects source active skills outside the supported attack/booster lists. `Assets/Scripts/GameData/NxSkillDataProvider.cs:60-66` uses the NX level count and req nodes. Source ranged-attack flags are an acknowledged extension in Unity `SourceSkillRules.cs:33-35`, not a completed C++ registry.

Beginner earned skills are 1000 Three Snails, 1001 Recovery, 1002 Nimble Feet, all max 3, no prerequisites. None currently has an executable source-data handler in Unity. Skill 12 must remain excluded from paid learning even though its passive handler exists. Special beginner skill IDs such as riding/event skills must not become payable merely because they share job 0.

## Recommended bounded offline rules

1. Keep an explicit offline progression policy. Level-only eligibility of 8 for Magician and 10 for the other four first jobs is supported by asset hints; identify the missing script policy in documentation.
2. If using 5 AP per earned level, 1 SP at first job, and 3 SP per later earned level, label these as local rules. Award through earned-EXP level transitions only; practice level jumps or job switches must not mint points. Keep first-job-family points isolated from practice kits and do not retroactively award points to old practice saves from their displayed level.
3. Restrict this pass's AP spending to STR/DEX/INT/LUK, require positive AP, and reject capped base stats before decrementing. A beginner restriction matching C++ UI is defensible; allowing beginner allocation is a deliberate local divergence.
4. Preserve the exact NX attack prerequisite chains in the supported column. Show unsupported skills with an explicit unavailable-effect reason and prevent SP spending. There is no need to allow an ineffective passive prerequisite to reach any currently implemented first-job attack.
5. Defer base natural HP/MP growth and HP/MP AP allocation rather than invent source formulas. Keep existing level-up restoration and clearly state that natural growth remains unported; block growth-passive SP spending. If playable balance demonstrably requires growth, implement a separately named/documented deterministic offline schedule after selecting it as a design decision, not as a source port.
6. Existing handlers provide a meaningful first-job attack loop for all five families, but do not support a full level-30 first-job build: Swordsman/Magician/Archer currently have only 40 spendable levels; Rogue/Pirate only 20. Accumulated SP should remain banked after available skills cap. Further progression requires additional skill handlers, not bypassed prerequisites or silently ineffective purchases.

Asset SHA-256:

- `nx/Skill.nx`: `5FB970791416065C523F6A17673D420E604226D14136FA65B401FE7C1043E3F1`
- `nx/Quest.nx`: `8581C865C5CCD119BD7128C6685BD18ECBC5D840393967195BF6746E6A41101A`
