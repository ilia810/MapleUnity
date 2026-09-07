# Ranged skill registry gap
The next Unity milestone is first-job ranged skills: Arrow Blow (3001004), Double Shot (3001005), Lucky Seven (4001344), Double Shot gun (5001003). HeavenClient is the behavioral source, but its SkillData::flags_of stops at F/P mage. Need establish the exact generic mechanics and explicit extension needed to enable these IDs, without claiming these flows already work end-to-end in C++. Investigate weapon restrictions, bulletCount vs bulletConsume, prone/degenerate handling, action markers per damage line, source stat damage formulas (especially Lucky Seven), and whether minimal safe enablement is feasible. NX values are being probed separately by the primary agent. Existing Unity ammo and magic flows are verified (493 focused checks /15 scenes). Do not edit production files.


## C:/HeavenClient/MapleStory-Client/Data/SkillData.cpp
```cpp
	int32_t SkillData::flags_of(int32_t id) const
	{
		static const std::unordered_map<int32_t, int32_t> skill_flags =
		{
			// Beginner
			{ SkillId::THREE_SNAILS, ATTACK },
			// Warrior
			{ SkillId::POWER_STRIKE, ATTACK },
			{ SkillId::SLASH_BLAST, ATTACK },
			// Fighter
			// Page
			// Crusader
			{ SkillId::SWORD_PANIC, ATTACK },
			{ SkillId::AXE_PANIC, ATTACK },
			{ SkillId::SWORD_COMA, ATTACK },
			{ SkillId::AXE_COMA, ATTACK },
			// Hero
			{ SkillId::RUSH_HERO, ATTACK },
			{ SkillId::BRANDISH, ATTACK },
			// Page
			// White Knight
			{ SkillId::CHARGE, ATTACK },
			// Paladin
			{ SkillId::RUSH_PALADIN, ATTACK },
			{ SkillId::BLAST, ATTACK },
			{ SkillId::HEAVENS_HAMMER, ATTACK },
			// Spearman
			// Dragon Knight
			{ SkillId::DRAGON_BUSTER, ATTACK },
			{ SkillId::DRAGON_FURY, ATTACK },
			{ SkillId::PA_BUSTER, ATTACK },
			{ SkillId::PA_FURY, ATTACK },
			{ SkillId::SACRIFICE, ATTACK },
			{ SkillId::DRAGONS_ROAR, ATTACK },
			// Dark Knight
			{ SkillId::RUSH_DK, ATTACK },
			// Mage
			{ SkillId::ENERGY_BOLT, ATTACK | RANGED },
			{ SkillId::MAGIC_CLAW, ATTACK | RANGED },
			// F/P Mage
			{ SkillId::SLOW_FP, ATTACK },
			{ SkillId::FIRE_ARROW, ATTACK | RANGED },
			{ SkillId::POISON_BREATH, ATTACK | RANGED },
			// F/P ArchMage
			{ SkillId::EXPLOSION, ATTACK },
			{ SkillId::POISON_BREATH, ATTACK },
			{ SkillId::SEAL_FP, ATTACK },
			{ SkillId::ELEMENT_COMPOSITION_FP, ATTACK | RANGED },
			// TODO: Blank?
			{ SkillId::FIRE_DEMON, ATTACK },
			{ SkillId::PARALYZE, ATTACK | RANGED },
			{ SkillId::METEOR_SHOWER, ATTACK }
		};

		auto iter = skill_flags.find(id);

		if (iter == skill_flags.end())
			return NONE;

		return iter->second;
	}


```

## C:/HeavenClient/MapleStory-Client/Gameplay/Combat/Skill.cpp
```cpp
	void Skill::apply_stats(const Char& user, Attack& attack) const
	{
		attack.skill = skillid;

		int32_t level = user.get_skilllevel(skillid);
		const SkillData::Stats stats = SkillData::get(skillid).get_stats(level);

		if (stats.fixdamage)
		{
			attack.fixdamage = stats.fixdamage;
			attack.damagetype = Attack::DMG_FIXED;
		}
		else if (stats.matk)
		{
			attack.matk += stats.matk;
			attack.damagetype = Attack::DMG_MAGIC;
		}
		else
		{
			attack.mindamage *= stats.damage;
			attack.maxdamage *= stats.damage;
			attack.damagetype = Attack::DMG_WEAPON;
		}

		attack.critical += stats.critical;
		attack.ignoredef += stats.ignoredef;
		attack.mobcount = stats.mobcount;
		attack.hrange = stats.hrange;

		switch (attack.type)
		{
		case Attack::RANGED:
			attack.hitcount = stats.bulletcount;
			break;
		default:
			attack.hitcount = stats.attackcount;
			break;
		}

		if (!stats.range.empty())
			attack.range = stats.range;

		if (projectile && !attack.bullet)
		{
			switch (skillid)
			{
			case SkillId::THREE_SNAILS:
				switch (level)
				{
				case 1:
					attack.bullet = 4000019;
					break;
				case 2:
					attack.bullet = 4000000;
					break;
				case 3:
					attack.bullet = 4000016;
					break;
				}
				break;
			default:
				attack.bullet = skillid;
				break;
			}
		}

		if (overregular)
		{
			attack.stance = user.get_look().get_stance();

			if (attack.type == Attack::CLOSE && !projectile)
				attack.range = user.get_afterimage().get_range();
		}
	}


```

## C:/HeavenClient/MapleStory-Client/Gameplay/Combat/Skill.cpp
```cpp
	SpecialMove::ForbidReason Skill::can_use(int32_t level, Weapon::Type weapon, const Job& job, uint16_t hp, uint16_t mp, uint16_t bullets) const
	{
		if (level <= 0 || level > SkillData::get(skillid).get_masterlevel())
			return FBR_OTHER;

		if (job.can_use(skillid) == false)
			return FBR_OTHER;

		const SkillData::Stats stats = SkillData::get(skillid).get_stats(level);

		if (hp <= stats.hpcost)
			return FBR_HPCOST;

		if (mp < stats.mpcost)
			return FBR_MPCOST;

		Weapon::Type reqweapon = SkillData::get(skillid).get_required_weapon();

		if (weapon != reqweapon && reqweapon != Weapon::NONE)
			return FBR_WEAPONTYPE;

		switch (weapon)
		{
		case Weapon::BOW:
		case Weapon::CROSSBOW:
		case Weapon::CLAW:
		case Weapon::GUN:
			return (bullets >= stats.bulletcost) ? FBR_NONE : FBR_BULLETCOST;
		default:
			return FBR_NONE;
		}
	}
}
```

## C:/HeavenClient/MapleStory-Client/Character/Player.cpp
```cpp
	Attack Player::prepare_attack(bool skill) const
	{
		Attack::Type attacktype;
		bool degenerate;

		if (state == Char::State::PRONE)
		{
			degenerate = true;
			attacktype = Attack::Type::CLOSE;
		}
		else
		{
			Weapon::Type weapontype;
			weapontype = get_weapontype();

			switch (weapontype)
			{
				case Weapon::Type::BOW:
				case Weapon::Type::CROSSBOW:
				case Weapon::Type::CLAW:
				case Weapon::Type::GUN:
				{
					degenerate = !inventory.has_projectile();
					attacktype = degenerate ? Attack::Type::CLOSE : Attack::Type::RANGED;
					break;
				}
				case Weapon::Type::WAND:
				case Weapon::Type::STAFF:
				{
					degenerate = !skill;
					attacktype = degenerate ? Attack::Type::CLOSE : Attack::Type::MAGIC;
					break;
				}
				default:
				{
					attacktype = Attack::Type::CLOSE;
					degenerate = false;
					break;
				}
			}
		}

		Attack attack;
		attack.type = attacktype;
		attack.mindamage = stats.get_mindamage();
		attack.maxdamage = stats.get_maxdamage();

		if (degenerate)
		{
			attack.mindamage /= 10;
			attack.maxdamage /= 10;
		}

		attack.critical = stats.get_critical();
		attack.ignoredef = stats.get_ignoredef();
		attack.accuracy = stats.get_total(EquipStat::Id::ACC);
		attack.playerlevel = stats.get_stat(MapleStat::Id::LEVEL);
		attack.range = stats.get_range();
		attack.bullet = inventory.get_bulletid();
		attack.origin = get_position();
		attack.toleft = !facing_right;
		attack.speed = get_integer_attackspeed();

		return attack;
	}


```

Other relevant files: Gameplay/Combat/SkillAction.cpp, SkillBullet.cpp, Combat.cpp::apply_result, Character/Look/BodyDrawInfo.h, Character/CharStats.cpp, Character/Char.cpp::get_attackdelay; Unity Assets/Scripts/GameLogic/Core/Combat.cs, Skills/SourceSkillRules.cs, Assets/Scripts/GameData/NxSkillDataProvider.cs.


---

# Ranged skill source findings

Research against `C:/HeavenClient/MapleStory-Client`, including the working-tree changes, for Arrow Blow 3001004, bow/crossbow Double Shot 3001005, Lucky Seven 4001344, and gun Double Shot 5001003. NX observations are recorded in `Logs/ranged-skill-probe.txt`. No production files were changed for this investigation.

## Scope of the extension

All four IDs are missing from `SkillData::flags_of` (`Data/SkillData.cpp:100`), and their named constants are missing from `Character/SkillId.h`. The current C++ client does not enter its attack branch for them: `Combat::apply_move` (`Gameplay/Combat/Combat.cpp:97`) tests `move.is_attack()`. Its generic mechanics can be reused, but enabling these IDs must be described as an explicit extension of an incomplete registry, not demonstrated end-to-end source behavior. The `RANGED` flag is currently unused outside the registry; attack type is selected from the equipped weapon by `Player::prepare_attack`.

The smallest controlled Unity extension is to add exactly these IDs to supported attacks and explicitly scope them to the intended families: 145/146 for the two archer skills, 147 for Lucky Seven, and 149 for gun Double Shot. The bow skills have no NX `weapon` property, and the gun property is literally `weapon ` with a trailing space, so the original C++ loader imposes no weapon restriction on those three. Lucky Seven's `weapon=47` is loaded as 147 and is source-restricted to claws. The strict source test is equality with `Weapon::by_value(100 + weapon)`, except `NONE` accepts any weapon (`Data/SkillData.cpp:77`, `Gameplay/Combat/Skill.cpp:294`, `Character/Inventory/Weapon.cpp:26`). Document the family restrictions as the small local enablement policy rather than silently changing generic source parsing for every skill.

## Entry rules and costs

`Player::can_use` (`Character/Player.cpp:261`) rejects **all skills while prone** before examining skill-specific costs. Thus the public Unity skill path should reject crouching with no action or cost. The `/10` degenerate damage and alternate attack-count branch in `prepare_attack`/`apply_stats` are only relevant if those helpers are invoked directly; they are not permission to cast a ranged skill prone.

`Skill::can_use` checks positive skill level not exceeding the source level count, job ancestry, `HP > hpCon`, `MP >= mpCon`, required weapon, and, for a ranged weapon, selected-stack count >= bullet cost (`Gameplay/Combat/Skill.cpp:278`). Job ancestry is `Job::can_use` / `is_sub_job` / `get_subjob` (`Character/Job.cpp:48,62,73`). Existing learned-skill and cooldown handling should remain in the public path.

Per-level `bulletCount` defaults to 1 and `bulletConsume` defaults to that bullet count (`Data/SkillData.cpp:58`). Source `Inventory::recalc_stats` selects the first positive compatible USE slot; it does not skip a short stack to find a larger one, and `get_bulletcount` returns that one stack. Therefore a first selected stack of 1 blocks a cost-2 skill even with 100 compatible rounds later. The four probed skills have costs 1,2,2,2 respectively. The source client waits for server inventory updates; local consumption is the already documented offline policy. Pay exactly `bulletConsume` only after successful preparation; capture damage and ammo selection first to preserve the final rounds' WATK bonus. Connected counts remain server-owned.

## Hit count, damage, and art

`Skill::apply_stats` (`Gameplay/Combat/Skill.cpp:176`) multiplies prepared minimum and maximum by NX `damage/100` for physical skills. `Attack` stores these as doubles. Each line is rolled independently through `Mob::calculate_damage` / `next_damage` (`Gameplay/MapleMap/Mob.cpp:716,761`) with normal physical defense, accuracy, and critical behavior. There is **no Lucky Seven exception or special formula** anywhere in the inspected source. Do not introduce a different LUK-only damage formula under a source-parity claim. Its generic source route uses the ordinary claw/job bounds from `CharStats::close_totalstats` and `Job::get_primary/get_secondary` (`Character/CharStats.cpp:71,99,111`, `Character/Job.cpp:235`), then the Lucky Seven percentage. With ordinary mastery 0.5, LUK 28, DEX 4 and total WATK 25, the source base bounds are 12–26; level-20 Lucky Seven multiplies those to 18–39 before monster defense and rolling.

`attack.hitcount = bulletCount` when prepared type is RANGED; otherwise `attackCount` (`Skill.cpp:205`). The enabled family/prone guards make these four use bulletCount. This applies equally to empty casts. Arrow Blow has 1 line; the other three have 2.

Bullet art chooses level `ball` first, then root `ball`, then the selected ammunition animation (`Skill.cpp:146`, `SkillBullet.cpp:25`). `SingleBullet::get` and `BySkillLevelBullet::get` ignore the ammo ID. Arrow Blow therefore uses its three-frame root ball; gun Double Shot uses its one-frame ball. Archer Double Shot and Lucky Seven use their actual arrows/stars. A live selected ammo ID does not override authored skill ball art. Conversely, every skill still requires and pays ammunition. Both ordinary two-line shots may share exactly the same position and launch time, so their sprites can overlap completely.

## Action selection and per-line timing

The three non-gun skills have no authored action. After registry enablement they use `RegularAction` (`Skill.cpp:112`, `SkillAction.cpp:24`) with the equipped weapon's ordinary ranged stance and its ordinary afterimage. Regular stances return the same delay for every line (`CharLook::get_attackdelay`, `Character/Look/CharLook.cpp:624`). Bow/crossbow Double Shot and Lucky Seven therefore launch their two projectiles simultaneously, not at an invented stagger.

Gun Double Shot uses named body action `doublefire`:

| Alias frame | Stance/frame | NX delay | Effective duration | Hit marker |
| --- | --- | ---: | ---: | ---: |
| 0 | shoot2 / 0 | 90 | 90 | 0 |
| 1 | stabO1 / 0, move=(2,0) | 360 | 360 | 90 |
| 2 | shoot2 / 0 | 0 | 100 | 450 |

`BodyAction` (`Character/Look/BodyDrawInfo.h:41`) converts zero delay to positive100; negatives contribute duration without a hit marker; positives contribute a marker at elapsed time before that frame. `BodyDrawInfo::init` (`BodyDrawInfo.cpp:39`) accumulates these markers; `get_attackdelay` (`:186`) returns the requested marker or zero when the index is absent. Do not clamp negative delays to1 or reuse the previous marker for absent indices. Only first two markers are used for the two damage lines, giving raw0/90. Divide each by the source float speed `1.7f - speed/10f`, then truncate to uint16 (`Character/Char.cpp:149,156`). At default gun speed5, the compiled C++ methods return delays **0/75ms** for the two lines; the third marker is375ms. Float division rounds before the integer cast, so reasoning from a double approximation can produce the wrong boundary result. Advancing the body action still uses the source integer fixed-tick duration rules. Preserve the +2px alias move in both facings.

`TimedQueue::emplace` (`Template/TimedQueue.h:44`) only enqueues even for a zero delay. Its update adds the8ms timestep before dispatch. `Combat::update` dispatches the bullet queue and then moves live bullets (`Gameplay/Combat/Combat.cpp:42`). Thus source zero-delay ranged effects become active on the next combat tick, not synchronously inside `use_move`; preserve any separately documented immediate unarmed practice behavior without extending it silently to these source skills. The generated delay fixture covers marker arithmetic, not the queue boundary.

`Combat::extract_effects` schedules each damage line using its own `user.get_attackdelay(i)` (`Gameplay/Combat/Combat.cpp:336,350,369`). Unity's previous one-boolean `Swing.Applied` scheduling must become per-line scheduling so immediate first gun shot and later second shot are distinct. Once a projectile has launched, ordinary animation completion must not discard it. Travel/death/context cancellation should preserve the established port behavior.

## Range

Source base projectile rectangle is x[-400,-5], y[-50,50] (`Character/CharStats.cpp:258`). `SkillData` reads `range` as a percentage /100 (`Data/SkillData.cpp:65`), and `Combat::apply_move` scales only the left boundary by that factor before mirroring, with integer truncation (`Gameplay/Combat/Combat.cpp:113`). Consequently the gun's NX level1 range245 gives980px and level20 range380 gives1520px. This is literal source arithmetic even if the data appears intended to be a pixel distance. Preserve and document it instead of silently interpreting the gun metadata differently. Empty casts still aim400px away regardless of this target-selection range (`Combat.cpp:344`).

The stats reference fixture now also evaluates the exact extracted `int16_t hrange = static_cast<int16_t>(range.left() * attack.hrange);` statement into `scaled_left`. The compiled C++ product yields-980/-1220/-1520 for gun levels1/10/20. In particular stored float3.7999999523 multiplied by-400 is rounded to float-1520 before truncation; retaining a wider intermediate until the cast would incorrectly shorten the level20 range to1519px. Unity must preserve the source float materialization at this boundary.

## Character-level and two-handed effects

Effect selection is independent for use and hit (`Gameplay/Combat/Skill.cpp:48,87`):

- Use: `CharLevel/10/effect.size>0` enables ByLevelUseEffect; otherwise other root effect forms are considered.
- Hit: `CharLevel/10/hit.size>0` enables the character-level hit path independently. Root hit/0 **and** hit/1 presence controls whether that character-level path is two-handed (`Skill.cpp:94`). Otherwise source can choose level-based hit or root hit.
- `ByLevelUseEffect::apply` (`SkillUseEffect.cpp:68`) and both character-level hit selectors (`SkillHitEffect.cpp:47,74`) select the greatest threshold strictly below the character level, except the first entry also applies below/equal its threshold. With keys10,15,20,25: levels1/10/15 use10;16/20 use15;21/25 use20;26+ use25.
- Root `TwoHandedUseEffect` chooses effect/0 or effect/1 according to equipped handedness (`SkillUseEffect.cpp:31`). Character-level use is a direct animation, not automatically a two-handed selector. Root or character-level two-handed hit chooses hit/0 or hit/1 using `AttackUser.secondweapon`, captured from `user.is_twohanded()` (`Combat.cpp:300`, `SkillHitEffect.cpp:31,61`). In the existing port, crossbows are two-handed, bows/claws/guns are not.

The Unity reader previously used a single `byLevel` decision from use art for both paths. The new skills need separate selectors to prevent silently choosing nonexistent character-level hit art or dropping root hit/1. The completed NX probe confirms the real paths below. Numeric child containers must be tested before treating `RealNxNode.Value` as a bitmap; this adapter can expose a descendant image on the parent.

| Skill | Source-selected use art | Source-selected hit art |
| --- | --- | --- |
| Arrow Blow | None | CharLevel/{threshold}/hit/0,4frames |
| Archer Double Shot | None | CharLevel/{threshold}/hit/0 bow6frames, hit/1 crossbow7frames |
| Lucky Seven | CharLevel/{threshold}/effect,7frames | CharLevel/{threshold}/hit/0,4frames |
| Gun Double Shot | CharLevel/{threshold}/effect,6frames | CharLevel/{threshold}/hit/0,4frames |

Although gun Double Shot's CharLevel tree also contains hit/1, its root hit tree has only0. The C++ selector therefore uses ByLevelHitEffect and ignores that second tree. Archer Double Shot has both root hit0 and hit1, which enables its character-level two-handed hit selector.

## Compiled reference artifacts

`Tools/Generate-HeavenRangedSkillReference.py` compiles unmodified generic `Skill::apply_stats/can_use`, the source BodyAction class, `BodyDrawInfo::get_attackdelay`, `CharLook::get_attackdelay`, and `Char::get_real_attackspeed/get_attackdelay` with controlled storage and metadata. It writes `Tools/HeavenRangedSkillReference-provenance.json`, source/fixture SHA256 hashes, and four CSV fixtures plus Unity metadata:

- HeavenRangedSkillStats:72 rows across four IDs, levels1/10/20, three attack types, and incoming ammo presence. Tests percentage application, bulletCount-vs-attackCount, projectile ID assignment, generic range override and percentage retention.
- HeavenRangedSkillCosts:168 rows across14 scenarios per metadata combination, covering inclusive ammo/MP costs, exclusive HP cost, levels, jobs, source weapon requirements, explicit bulletConsume0/3 overrides and guard precedence.
- HeavenRangedSkillDelays:128 rows across doublefire/ordinary actions,16 speeds, and four line indices (including an absent named marker). Ordinary lines all use the same two-frame afterimage delay; doublefire uses0/90/450/0 before source speed scaling.
- HeavenRangedSkillActions:6 frame rows verifying positive, negative and zero delay normalization plus the+2px alias move for doublefire and handgun.

The harness supplies source-parsed required weapons (only Lucky Seven has one), not Unity's additional family guard. It calls generic helpers directly and does not simulate source registry activation, public prone/cooldown guards, the render loop, projectile flight, RNG, network consumption or offline debiting. Lucky Seven fixtures deliberately verify generic source percentages and make no claim about retail formula correctness. The full adapter limits are recorded in provenance.

## Suggested focused verification

Validate every supported family/action and the rejected wrong-family/prone/no-MP/short-selected-stack cases; 1-vs-2 cost including final rounds; skill-ball-vs-ammo precedence; two-line snapshots and individual launch timing; both empty shots; closest-target and source-scaled gun range boundaries; per-line hit callbacks and damage; travel/death cancellation; independent effect lookup with crossbow hit/1 and exact character-level thresholds. Use generic C++ function extraction for damage/timing reference vectors and state plainly that only the registry/family enablement itself is new policy.
