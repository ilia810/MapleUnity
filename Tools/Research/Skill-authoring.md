# Skill definitions and authoring

Implemented 2026-09-07. Original NX skills and custom Unity skills now share one cast pipeline. The original C++ behavior remains the reference for imported skills; new definitions choose their behavior explicitly.

## Add a skill in Unity

1. In the Project window, choose **Create → MapleUnity → Skill**. Give it a unique ID of **100000000 or higher**, a display name and an explicit **Job Id** (for example, `200` for magician). The ID does not determine its job. `Inherit Job` makes it available to later jobs in that branch.
2. Choose **Execution**: `Attack`, `Self`, or `Passive`. Select an attack family, damage policy, targeting and weapon/ammunition requirements for attacks. For self casts, add `stat-buff`, `recovery`, or both to **Cast Effects**. For a passive, leave **Cast Effects** empty and enter its passive values.
3. Add one **Ranks** entry per level, in order starting at level 1. Costs and cooldowns are per rank; all duration fields use milliseconds. Enter buffs, recovery, hit count, target count, damage, and attack area as appropriate. Prerequisites are skill IDs and required ranks.
4. Set **Action** to a valid original character action (`alert2` works for the supplied examples). An empty action allows an instant self cast or the equipped weapon's normal attack. A missing named action is rejected before spending resources.
5. Supply an icon and optional use/hit/projectile animation frames as Unity Sprites. Each frame has a duration, alpha and scale. Import sprites at 100 pixels per unit with the desired pivot; use Point filtering and no compression for matching pixel art. Alternatively set **Source Presentation Skill Id** to borrow an original skill's icon/effects. Unity sprite fields override borrowed art; borrowing art never changes gameplay, cost or job.
6. Put enabled assets beneath **Assets/Resources/Skills/**, then restart Play. They load into the same skill catalog, book, keyboard bindings, quickslots and save format as original skills. Use earned first-job SP to learn eligible definitions. Invalid definitions produce a diagnostic and do not enter the catalog.

Keep custom IDs stable after distributing saves. Removing an asset makes a save containing that unknown skill fail the existing restore validation; it is not silently replaced by another skill. Keep IDs unique across all Resources folders. Example IDs `100000001` and `100000002` are reserved by the samples.

## Supplied examples

`Assets/SkillExamples/ArcBolt.asset` is a magician attack with two projectile hits, 18 base magic damage per hit, an 8 MP cost and a one-second cooldown. It borrows Magic Bolt presentation but uses explicit authored targeting and damage.

`Assets/SkillExamples/FieldFocus.asset` costs 4 MP, restores 12 HP and grants 7 magic attack for ten seconds, with a two-second cooldown. It composes two existing caster effects.

These assets are deliberately outside Resources, so they do not change the ordinary roster. Move an example into `Assets/Resources/Skills/` to enable it, or duplicate it and give the copy a new ID. **MapleUnity → Skills → Create Example Assets** recreates missing samples without overwriting existing ones.

## Combat and state rules

- `ForwardArea` requires a feet-relative rectangle in source pixels: X points right, Y points down, and the area is authored facing left. `(-220, -90, 0, 20)` reaches 220 pixels forward; facing right mirrors it. `RangePercent` scales the forward reach. `SourceWeapon` uses the existing weapon/source targeting fallback.
- `WeaponPhysical` applies the rank's damage percentage to the player's physical attack range. `FixedPhysical` and `FixedMagic` use its damage as base damage, followed by the existing accuracy, level, defense and critical rules. `SourceClient` preserves the inspected C++ calculation, including its known handling of magic attack; it does not invent a new MATK formula.
- Attack preparation captures targets and damage; Combat delivers hits at the action marker or projectile impact. `SkillUseResult.Damage` is zero because it is not an immediate damage event. HP/MP, ammunition and cooldown are charged once after successful preparation, including an empty cast. An invalid effect, missing action, insufficient resource or incompatible job/weapon fails before payment.
- Attack ranks can enable **Apply Target Debuff** for timed negative physical attack/defense changes, with a duration, chance and optional repeat-source rejection. The effect attempts once per monster per cast after a landed nonlethal hit. **Target Status** supplies its animation; **Source Presentation Skill Id** can borrow Disorder's original art. `SkillDamagePolicy.None` makes a direct, non-damaging target-effect attempt; it requires a valid debuff, AttackCount 1 and no ammunition/projectile. See [target weakening and original thief skills](Thief-skills-source.md).
- `stat-buff` also supports Accuracy, Avoidability and `Hide=1`. Hide blocks contact and new attacks and makes the character translucent; an optional Speed contribution controls its movement penalty. Buff icons can be right-clicked to cancel. See [utility skill rules](Utility-skills-source.md) for the explicit local evasion policy.
- `stat-buff` contributes only supported buff types. Player owns all buff timers, replacement and expiry. Matching stat types replace each other; unrelated contributions persist. Travel retains buffs; death and save restoration clear them. Skill stores learned rank and cooldown only.
- Recovery combines flat amounts and percentages of the current maximum, then caps at the maximum. It runs after costs, so it cannot fund an otherwise unaffordable cast.
- Passive flat values add. Optional mastery/damage overrides keep the established source ordering by skill ID; the last applicable override wins. `-1` leaves an optional value unchanged. A threshold can restrict a passive to low HP. This is not an additive mastery or percentage stacking system.

## Extend the implementation

`SkillInfo` is the shared normalized definition. `SkillBehavior` contains executable capabilities. `ClassicSkillCatalog` is the single original-ID-to-capability import table, including raw NX buff-field and passive conversions. Unknown NX skills remain visible but unavailable, preserving SP and cast resources.

To enable another original skill that uses existing behavior, add its descriptor in `ClassicSkillCatalog`; do not add skill-ID conditions to SkillManager, Combat, Player or UI. Check its original prerequisites, rank fields, action, effects and weapon constraints against the C++ source/NX data.

For another reusable **caster effect**, implement `ISkillCastEffect` with a unique key. `Validate` must be side-effect-free and check every required rank parameter. `Apply` must use the already-validated values, apply once, and not spend cast costs again. Numeric authoring parameters are available through `LevelData.EffectParameters`. Register the handler in the shared `NXDataManager.SkillEffects` registry **before `Initialize()` loads Resources**, or include it in `SkillCastEffects` construction. Catalog validation and SkillManager use that same registry. Runtime `SkillCatalog.TryRegister` also uses it.

Effects in this registry currently run on the caster at cast time. Physical enemy weakening uses the attack rank's separate target-effect definition and resolves at impact. New summons, party targeting, channels, persistent areas, additional enemy statuses, reflection and other world/target mechanics still need their own runtime implementation and validation; selecting an ID or borrowing art does not implement them. Later-job progression and online authority also remain separate rewrite work. This change provides extensible definitions and a shared runtime for the supported mechanics, not automatic coverage of every original skill.

## NPC name priority

`ClassicWorldLabels` reserves NPC names and service labels at their own feet first. NPC plates never participate in overlap displacement; player and monster labels move downward or hide when no room remains. Camera movement and viewport clamping still determine their screen projection. A scene regression crosses the player from both directions and verifies the NPC's plate remains fixed and unobscured by the player plate.

## Validation evidence

- `Logs/skill-framework-data-final.xml`: 249/249 focused EditMode tests, including 18 new framework cases and both deserialized examples.
- `Logs/skill-framework-scenes-final.xml`: 58/58 scene checks, including custom book/SP/quickslot/save/use-art, custom projectile/hit-art, and NPC crossing priority.
- `Logs/skill-framework-player-clean.log`: successful Windows player script compilation, zero C# errors, with editor/test assemblies excluded.
- [Captured review](../ArtReferences/SkillFramework/review.html). All validation ran in `Temp/CodexValidation`, preserving the user's editor and scene.
