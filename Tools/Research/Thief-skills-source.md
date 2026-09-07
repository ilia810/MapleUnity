# Disorder, Double Stab and reusable monster weakening

Implemented 2026-09-07. Original data is recorded in [Thief-skills-nx.json](Thief-skills-nx.json). This pass extends the shared skill runtime; it does not reproduce the C++ class hierarchy or assume its incomplete skill flags implement every NX skill.

## Original data and local decisions

| Skill | Original rank data | Execution |
|---|---|---|
| Disorder, 4001002 | 20 ranks; MP 5 → 10, duration 7 → 60 seconds, `x`/`y` −1 → −20 | Non-damaging close-range weapon action; weaken one eligible monster's physical attack and defense |
| Double Stab, 4001334 | 20 ranks; MP 8 → 14, two hits, 98% → 140% per hit, required weapon 33 (dagger) | Existing weapon swing, target selection, delayed hits and damage path |

Disorder has no NX damage field, authored action, projectile or player use effect. Its `mob` animation contains ten original frames, with 100 ms default delays and `pos=3`. Its text explicitly disallows reapplying the effect to an already affected monster. The import uses those facts: no damage is invented, it uses the ordinary equipped-weapon action/reach, and its monster animation loops at the feet anchor until expiry. Double Stab's original `CharLevel` hit art (10/15/20/25) follows the existing source boundary-selection behavior. Its rank-17 description says MP −1, but the numeric `mpCon` field is 13; numeric gameplay metadata remains authoritative.

The inspected C++ `Data/SkillData.cpp::flags_of` omits these skills. `Gameplay/Combat/Skill.cpp` supplies reusable regular-action, hit-count and artwork behavior, but its absence of a local monster weakening runtime does not prevent implementation from NX evidence. No per-ID condition was added to Combat, Player, the view or the cast manager.

The local rules are explicit:

- Disorder uses the existing attack accuracy calculation to determine whether its close-range attempt lands. The status begins at the action's hit marker, after all cast checks and MP payment. A miss uses the normal MISS feedback. Success changes no HP and creates no damage, knockback, EXP or kill event.
- Repeated Disorder skips monsters already holding Disorder. It can select another eligible monster within reach. Empty/repeated swipes still pay the ordinary cast cost but cannot refresh the active status. Eligibility is checked again at impact.
- The current monster AI patrols and has no spell/attack windup or player-aggro state. The description's ongoing-attack/aggro interruption therefore has no additional supported action to cancel. It is not simulated with a fake stun. Monster attack AI, boss/status immunity rules and broader enemy statuses remain separate work.
- Physical weakening has one slot per monster. Another source replaces the complete effect; contributions do not stack. Effective physical attack and defense clamp at zero. Shared NX templates and other monsters remain unchanged. Magic attack/defense are unaffected.
- Status time and animation advance on the monster's existing 8 ms simulation ticks. A pause freezes both. Expiry/death clear the effect; map changes/load replace world monsters and never persist their temporary statuses.
- Damage attacks can carry the same weakening definition. They attempt application after a landed, nonlethal hit, at most once per monster per cast. Missed lines can be followed by a successful line; multiple successful lines do not reroll the proc. Existing damage is captured at attack preparation, so a strike cannot retroactively increase its own already-calculated hits.

## Implementation

`ClassicSkillCatalog` adds the two original definitions. `NxSkillDataProvider` maps Disorder's `x/y/time/prop` into `MonsterDebuffDefinition` and loads its shared `mob` frames once for all ranks. `SkillDamagePolicy.None` describes a non-damaging attack with a target effect; validation limits that mode to direct, single-attempt attacks with no ammunition or projectile.

`MonsterDebuffDefinition` carries two stat changes, duration, chance, repeat-source policy and optional presentation. `MonsterDebuffs` owns the active state and expiration. It uses one physical-weakening slot rather than a speculative general status framework.

Combat checks effective monster attack for contact damage and effective physical defense for ordinary/skill physical damage. Damage rolls take an optional defense override, leaving source templates intact and the magic-defense path unchanged. The effect definition is copied when preparing the swing. Target membership, death and cancelled swings retain the existing impact guards. Effect probability uses a separate injectable RNG, preserving the existing damage RNG sequence for damage attacks.

`MonsterView` adds one status renderer alongside its hit renderer, so debuff art and a Double Stab impact can coexist. It samples original frames from simulation time. The skill tooltip shows enemy stat changes, duration and repeat behavior; Disorder is labelled as causing no damage.

## Try it and progression

Restart Play; no map regeneration is required.

- **Normal earned progression:** advance to Thief, learn Disorder 3, then spend one SP on Dark Sight. Double Stab is available independently and requires an equipped dagger to cast. Both skills use the ordinary book, key bindings and save records.
- **Immediate practice:** Menu → Practice supplies and skills → **Thief dagger practice**. It supplies a Razor (1332005) and ten Blue Potions once, teaches rank-one Disorder/Double Stab/Dark Sight, and assigns the three quickslots. Equip the Razor from the bag. The original dagger requirement is level 5 with no attribute/job requirements; the kit raises only the practice level if needed and grants no AP/SP or healing.
- Repeating the kit retains higher ranks and grants no extra items, including after saving/loading. It uses the existing weapon-kit claim set. The serialized `RangedKits` field keeps its historical name for save compatibility and now also accepts the dagger category 133. Existing ranged preset APIs remain compatible.

## Custom skill authoring

An attack rank can enable **Apply Target Debuff**, set negative **Target Physical Attack Change / Target Physical Defense Change**, duration in milliseconds, chance, and optionally reject reapplication from the same source. **Target Status** accepts Unity Sprite animation frames; a borrowed original Disorder presentation can supply its `mob` art too.

Use an existing damage policy to combine damage and weakening. Use **None** for a direct non-damaging effect, with one attack attempt, no projectile/ammunition, and a valid target debuff. Invalid definitions are rejected before catalog registration or resource spending. New kinds of status behavior still require an implementation; no fields silently claim to support poison, stun, reflection or summons.

## Validation

- **304/304 focused EditMode checks passed:** `Logs/thief-skills-data-final.xml`. This includes 22 new checks for original data/art, earned prerequisites, delayed/no-damage status application, two-hit delivery, cost guards, misses/cancelled swings, per-monster isolation, replacement/expiry/death, effective physical/contact stats, magic-defense isolation, custom effect validation/probability and saved kit claims. All 282 previous focused checks also passed.
- **63/63 PlayMode scene checks passed:** `Logs/thief-skills-scenes-final.xml`. Both new scenes passed independently beforehand. They exercise original skill-book/SP controls, the normal Disorder 3 → Dark Sight unlock, quickslot casts, the small practice picker, native status and hit art together, simulation pause/expiry and travel/load.
- **Final Windows player script compilation passed with zero C# errors:** `Logs/thief-skills-player-clean.log`, marker `PLAYER_COMPILATION_VALIDATION_PASSED`. The preceding compile refreshed Unity's cached input graph after adding the new definition and monster sources.
- All **419 C# sources and their metadata** match `Temp/CodexValidation`. All **12 links** in the [captured review](../ArtReferences/ThiefSkills/review.html) resolve. The book, Disorder/Double Stab art and 640×480 practice picker were visually inspected. The original Unity editor (PID 26408) and unsaved scene are preserved.
- The first focused data attempt failed in its common fixture setup because the initially selected dagger had unmet NX level/job requirements. The kit and fixture now use the level-5 Razor with no required attributes/job. No equipment requirement was bypassed to make the test pass. The previously deferred unrelated tests remain excluded.
