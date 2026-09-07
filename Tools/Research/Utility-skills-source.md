# Archer/thief utility skills

Implemented 2026-09-07. HeavenClient is behavioral evidence, not an architectural template. Preserve supported gameplay intent and NX content, investigate contradictions, and prefer small reusable Unity implementations. Do not reproduce placeholder behavior or build speculative infrastructure solely to resemble or optimize the C++ code.

## Evidence and content

The original NX metadata is recorded in [Utility-skills-nx.json](Utility-skills-nx.json). Skill paths use `Skill.nx/<job>.img/skill/<id>` and text uses `String.nx/Skill.img/<id>`.

| Skill | Ranks | Original behavior | Prerequisite |
|---|---:|---|---|
| The Blessing of Amazon, 3000000 | 16 | Passive accuracy `x`: +1 through +16 | None |
| Focus, 3001003 | 20 | Temporary accuracy `acc` and avoidability `eva`: +1 through +20 | Amazon Blessing 3 |
| Nimble Body, 4000000 | 20 | Passive accuracy `x` and avoidability `y`: +1 through +20 | None |
| Dark Sight, 4001003 | 20 | Concealment `x=1`, with movement `speed`: −30 through 0 | Disorder 3 |

Focus costs 8 MP for 70 seconds at rank 1 and 16 MP for 300 seconds at rank 20. It uses the original `alert2` action and NX effect. Its rank-20 accuracy happens to be stored as a string in this NX export; the ordinary numeric loader handles it without a skill-specific exception.

Dark Sight costs 24 MP for 10 seconds at rank 1 and 5 MP for 200 seconds at rank 20. Its `darksight` character action aliases `alert` frame 0 for 100 ms. The original NX casting burst and icon are used.

The C++ evidence is incomplete: `Character/Player.cpp::is_invincible()` checks DARKSIGHT, but `Character/Char.cpp::draw()` does not supply a complete concealment visual, and `Gameplay/Combat/Skill.cpp` does not supply the full local attack restriction. The NX description explicitly prohibits attacking and being attacked. Unity implements those intentions for the currently supported contact-combat path instead of duplicating the gaps.

## Small extensions to existing systems

- `ClassicSkillCatalog` contains the four original definitions. Existing normalized passive values and the `stat-buff` effect do the work. Runtime behavior has no conditions on these four IDs.
- Accuracy buffs feed both physical and magic attack accuracy. Equipment/passive/buff accuracy caps before derived DEX/LUK accuracy is added, preserving the existing source order. Avoidability now includes its active stat contribution.
- `BuffType.Hide=1` enables concealment for any source or custom skill. It prevents contact checks and new basic/skill attacks, including the lower-level animation entry points. Failed attacks spend no HP, MP, cooldown or ammunition. It leaves the ordinary two-second invulnerability timer independent.
- Concealed character layers use 35% opacity. Names, HUD, and casting bursts remain legible. The existing renderer applies the state to assembled body/equipment layers and armor echoes, without a second character renderer or a new effect hierarchy.
- Timed buff expiry, travel, save restoration and death use existing ownership rules: travel retains concealment, while load/death clear it. Direct scripted damage bypasses contact protection. A projectile launched before concealment retains its normal impact behavior; concealment blocks new attacks. Existing movement consumes the rank's Speed contribution.
- Buff icons accept right-click cancellation through SkillManager. Cancelling a source removes only stat contributions still owned by that source. For example, a newer Speed Potion survives cancelling Dark Sight. Cancellation does not refund resources or change learned ranks/cooldowns.
- NX `acc`/`eva` consumables are now supported too, including Sniper Potion (+5 accuracy, 300 seconds) and Dexterity Potion (+5 avoidability, 180 seconds). They share stat replacement and cancellation. Item buffs are fully validated before consumption.

## Explicit local evasion rule

The C++ `Character/CharStats.cpp::calculate_damage()` labels its defense calculation as a placeholder and does not use player avoidability. Carrying that over would leave Focus/Nimble Body's evasion as a display-only stat.

For local contact checks, reuse the existing outgoing accuracy rule with the roles reversed:

```
delta = max(0, playerLevel - monsterLevel)
hitChance = clamp(monsterAccuracy / ((1.84 + 0.07 * delta) * playerAvoidability + 1), 0.01, 1)
```

This is a documented local gameplay choice, not a claim that HeavenClient or an original server used this formula for player evasion. Monster accuracy is already imported from NX. With no player avoidability or missing/nonpositive monster accuracy, keep the previous always-hit fallback. A contact that misses creates the native incoming MISS feedback and the existing two-second contact grace period; it spends no HP/MP and causes no knockback. Successful contact damage still uses the existing defense/Guard calculation. The contact RNG is injectable for deterministic boundary tests.

Outgoing accuracy behavior is unchanged; both directions call `AccuracyRules`, a small calculation rather than a new combat service or hierarchy. Exact server-era contact balance, monster spells and the broader defense formula remain future work.

## Try it

Restart Play after compilation; do not regenerate the map. An archer can spend earned SP on Amazon Blessing 3, then Focus. A thief can spend earned SP on Nimble Body. Both passive descriptions show their flat bonuses.

For immediate casting checks, use **Menu → Practice supplies and skills → Defensive and support skills → Archer: Focus / Thief: Dark Sight**. Presets teach rank 1 without supplying items, healing, EXP or AP/SP, and preserve higher learned ranks. Cast via the book or its assigned quickslot. Right-click the active buff icon to cancel it.

Disorder was subsequently implemented in the [thief skill pass](Thief-skills-source.md). Normal earned SP can now unlock Dark Sight after Disorder 3, while the practice preset remains available for immediate testing. Other utility skills, later-job progression, online play and previously deferred unrelated tests remain separate.

Custom skills can use `stat-buff` with Accuracy/Avoidability contributions or `Hide=1`, plus an optional Speed contribution. No custom skill ID needs special handling.

## Verification

- **282/282 focused EditMode checks passed:** `Logs/utility-skills-data-final.xml`. This includes 21 new utility checks, shared-framework/support/passive/progression/combat coverage and the player-death regressions. A null asset provider exposed a constructor regression in the preceding framework pass; the effect-registry lookup now handles it. Two formerly unsupported potions moved into the positive asset tests because their mechanics are now implemented.
- **61/61 PlayMode scene checks passed:** `Logs/utility-skills-scenes-final.xml`, including all 58 previous cases and three new utility scenes. The new scenes exercise earned Amazon/Focus prerequisites, real book/quickslot actions, right-click cancellation through the EventSystem, both skill and item buffs, all assembled concealed character layers, travel/load, the 640×480 picker and native incoming MISS feedback. The first focused run passed 6/7; the archer test needed to wait for the normal book refresh before reading its changed passive tooltip. The full run passed after that test timing correction.
- **Windows player compilation passed with zero C# errors:** `Logs/utility-skills-player-clean.log`, marker `PLAYER_COMPILATION_VALIDATION_PASSED`. The preceding compile regenerated its cached input graph after the new accuracy source file was added.
- All **415 C# files and their metadata** match the isolated validation snapshot. All **12 links** in the [captured review](../ArtReferences/UtilitySkills/review.html) resolve. Focus, concealed/cancelled Dark Sight, incoming MISS and the small practice picker were visually inspected.
- The original Unity editor (PID 26408) and unsaved scene are preserved. No map regeneration or external image editing is required. Online play and the previously deferred unrelated failures remain excluded.
