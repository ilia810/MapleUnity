# Defensive and support skill rewrite — 2026-09-07

This milestone enables eight explicit NX skill IDs in offline play. It uses the inspected HeavenClient C++ checkout at `C:/HeavenClient/MapleStory-Client` as the client behavior reference. The source is copyright 2015–2019 Daniel Allendorf and Ryan Payton, AGPL-3.0-or-later. Server-owned rules are called out below.

## Skills and level-one data

Values come from `nx/Skill.nx/<job>.img/skill/<id>/level/1`; names, descriptions and prerequisites come from the corresponding Skill/String NX nodes.

| Skill | ID | MP | Duration | Effect |
|---|---:|---:|---:|---|
| Iron Body | 1001003 | 8 | 75 s | Weapon defense +2 |
| Magic Guard | 2001002 | 6 | 111 s | 11% contact damage transferred to MP |
| Magic Armor | 2001003 | 8 | 54 s | Weapon defense +2 |
| Rage | 1101006 | 12 | 46 s | Weapon attack +3, weapon defense −3 |
| Iron Will | 1301006 | 12 | 15 s | Weapon and magic defense +1 |
| Hyper Body | 1301007 | 20 | 10 s | Maximum HP and MP +2% |
| Meditation, Fire/Poison | 2101001 | 10 | 10 s | Magic attack +1 |
| Meditation, Ice/Lightning | 2201001 | 10 | 10 s | Magic attack +1 |

All authored ranks are loaded. Magic Guard and Magic Armor can now consume earned first-job SP. Magic Armor requires Magic Guard 3. Iron Body still requires Endure 3; its recovery/growth prerequisite chain remains unavailable through normal SP. Later job advancement and second-job SP are not silently enabled.

## Client behavior retained

- `Gameplay/Combat/Skill.cpp`, `SkillAction.cpp` and `SkillUseEffect.cpp`: all eight skills use the original `alert2` action. Use effects retain NX frames, origins, delays, alpha and scaling. Their timing runs on the existing 8 ms skill/body clocks. Costs are paid once after successful validation, and an active cast blocks another cast.
- `Character/Char.cpp::show_iron_body` and `Char::draw`: Iron Body and Magic Armor have no substitute NX burst. The view draws the complete current character a second time, growing from 1× to 2× and fading from full opacity to zero over 500 ms. Clothing, hair and equipped pieces use the live assembled pose. The effect stops during travel, death and restore and pauses with simulation.
- `Character/ActiveBuffs.cpp`, `Player.cpp::give_buff` and `CharStats.cpp`: each stat has one current buff contribution. A new contribution replaces that stat instead of accumulating edits to base stats. Other contributions from the previous source can remain. Rage's negative defense is retained. Hyper Body uses the total including equipment, adds the truncated percentage contribution and applies `StatCaps.h`'s 30,000 HP/MP caps. Equipment changes recalculate against the underlying base values.
- Skills use the existing skill book, keyboard/quickslot bindings, native icons and buff countdown display. Tooltips distinguish percentage maxima, defense/attack bonuses and Magic Guard's MP transfer.

## Explicit offline authority

The C++ client receives buff payloads and current HP/MP from its server. The local client substitutes only these documented rules:

- Skill-level `pad/pdd/mad/mdd` fields supply their corresponding stat contributions. Hyper Body supplies HP/MP percentages from `x/y`; Magic Guard supplies its transfer percentage from `x`.
- Magic Guard intercepts successful contact damage **after** the existing defense/passive reduction. It transfers `floor(damage × x / 100)` points to MP, limited to available MP; any shortage remains HP damage. Invulnerability and knockback are unchanged. Skill HP/MP costs and direct scripted `TakeDamage` calls bypass the guard. Floating damage feedback continues to report actual HP lost.
- A larger maximum grants no HP/MP. Recasting, expiry, removal and equipment changes clamp resources that exceed a reduced maximum. Death clears active buffs. Travel retains their remaining duration. Saved base maxima never include buffs; loading clears temporary buffs and clamps saved current resources to restored maxima.
- Menu → Practice supplies and skills → Defensive and support skills initially offered six job presets, now extended with Archer and Thief in the [utility skill pass](Utility-skills-source.md). These explicitly change the practice job and teach level-one buffs, retaining already higher ranks. They grant no equipment, healing, EXP or AP/SP. Repeating a selection cannot produce item rewards or lower learned ranks. Regular SP prerequisites remain separate.

## Limits

Party range/recipients, monster spells, dispel/status interactions, online buff packets and skill sounds are not implemented by this milestone. Only the local player receives these buffs. Focus and Dark Sight were subsequently added in the [utility skill pass](Utility-skills-source.md). Power Guard, teleportation, recovery/growth passives and broader class attacks remain separate work.

Meditation changes the real magic-attack stat. The previously ported C++ magic attack calculation still uses its WATK-derived bounds; this milestone does not invent a new MATK damage formula. Natural HP/MP growth and HP/MP AP allocation remain deferred as recorded in `Progression-source-findings.md`.

## Verification

New checks cover real NX costs and art for every skill, earned Magic Guard/Armor prerequisites, repeated casts, insufficient MP, wrong jobs, MP shortage spillover, invulnerability, direct-damage bypass, per-stat replacement, death/revive, percentage caps, equipment changes, expiry, travel, save/load and repeatable practice selection. Scene checks exercise real book controls, assignment, quickslot casting, the original armor effect and support presets at 640×480 as well as the normal capture size.

Final validation results and screenshots are linked from `PROJECT_STATUS.md` and the [support skill review](../ArtReferences/SupportSkills/review.html).

- **154/154 focused EditMode checks passed:** `Logs/support-skills-data-final.xml`. This includes 29 new support checks and the affected combat-stat, skill, progression and local-save coverage.
- **55/55 PlayMode scene checks passed:** `Logs/support-skills-scenes-final.xml`, including both new support scenes and the complete existing smoke selection. The initial focused scene run passed 6/7; a new test had a floating-point expected-value error, corrected to the concrete source result before the full passing run.
- All **402 C# sources** have metadata and match the isolated validation project. The original Unity editor and its unsaved scene are preserved.
- **Windows player compilation passed** with zero C# errors: `Logs/support-skills-player.log`, marker `PLAYER_COMPILATION_VALIDATION_PASSED`. The only remaining Unity process is the original editor PID 26408.
