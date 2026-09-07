using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>Authoring only. Compiles to the same definition and runtime as an NX skill.</summary>
    [CreateAssetMenu(menuName = "MapleUnity/Skill", fileName = "NewSkill")]
    public sealed class CustomSkillAsset : ScriptableObject
    {
        public const int FirstCustomId = 100000000;
        [Min(FirstCustomId)] public int Id = FirstCustomId;
        public string DisplayName = "New skill";
        [TextArea] public string Description;
        public int JobId = 200;
        public bool InheritJob = true;
        public SkillExecution Execution = SkillExecution.Self;
        public SkillAttackFamily AttackFamily;
        public SkillDamagePolicy DamagePolicy = SkillDamagePolicy.WeaponPhysical;
        public SkillTargeting Targeting = SkillTargeting.ForwardArea;
        public bool RequiresWeapon, UsesAmmunition, BlocksCrouching, ArmorEcho;
        public int[] AllowedWeapons = Array.Empty<int>();
        [Tooltip("Original character action name. Empty permits an instant self cast or a weapon attack.")]
        public string Action = "alert2";
        public string[] CastEffects = { SkillCastEffects.StatBuff };
        public Requirement[] Prerequisites = Array.Empty<Requirement>();
        public Rank[] Ranks = { new Rank() };
        [Header("Presentation (optional)")]
        [Tooltip("Borrow icon and effects from this original skill. Unity sprites below override its art.")]
        public int SourcePresentationSkillId;
        public Sprite Icon;
        public Animation Use = new Animation(), Hit = new Animation(), Projectile = new Animation();
        public Animation TargetStatus = new Animation();

        [Serializable] public sealed class Requirement { public int SkillId, Level = 1; }
        [Serializable] public sealed class Buff { public BuffType Type; public int Value; }
        [Serializable] public sealed class Parameter { public string Key; public float Value; }
        [Serializable] public sealed class Rank
        {
            public int HpCost, MpCost, CooldownMilliseconds, DurationMilliseconds;
            [Tooltip("Percentage for weapon damage; base damage for fixed policies.")]
            public int Damage = 100;
            public int AttackCount = 1, MobCount = 1, RangePercent = 100, BulletCount = 1, BulletConsume = 1;
            [Tooltip("Feet-relative pixels, authored facing left: left, top, right, bottom. Y points down.")]
            public Vector4 Area = new Vector4(-220, -90, 0, 20);
            public Buff[] Buffs = Array.Empty<Buff>();
            public int HealHp, HealHpPercent, RestoreMp, RestoreMpPercent;
            public Parameter[] EffectParameters = Array.Empty<Parameter>();
            public int PassiveWeaponAttack, PassiveMagicAttack, PassiveAccuracy, PassiveAvoidability;
            [Range(0,15600)] public int PassiveProjectileRangeBonus;
            [Tooltip("-1 keeps the default. Chance is a ratio; critical damage is a total multiplier (2 = double). Strongest source wins.")]
            public float PassiveCriticalChance = -1, PassiveCriticalDamageMultiplier = -1;
            [Tooltip("-1 leaves the existing value unchanged; otherwise a ratio (0.6 = 60%).")]
            public float PassiveMastery = -1, PassiveDamageMultiplier = -1, PassiveDamageReduction = -1;
            [Tooltip("-1 applies at any HP; otherwise apply only at or below this HP percentage.")]
            public int PassiveBelowHpPercent = -1;
            [Tooltip("Optional timed physical weakening, applied once per monster on a landed hit. Different sources replace it.")]
            public bool ApplyTargetDebuff, RejectSameDebuffSource;
            public int TargetPhysicalAttackChange, TargetPhysicalDefenseChange, TargetDebuffMilliseconds;
            [Range(0,100)] public int TargetDebuffChance = 100;
        }
        [Serializable] public sealed class Frame
        {
            public Sprite Sprite;
            [Min(1)] public int Milliseconds = 100;
            [Range(0, 1)] public float StartAlpha = 1, EndAlpha = 1;
            [Min(0)] public float StartScale = 1, EndScale = 1;
        }
        [Serializable] public sealed class Animation
        {
            public Frame[] Frames = Array.Empty<Frame>();
            [Tooltip("Hit effect anchor from the original client. Use/projectile art uses its sprite pivot.")]
            public int Position, Z;
        }

        internal bool Compile(NxSkillDataProvider source, SkillCastEffects effects, out SkillInfo info,
            out Dictionary<string, Sprite> sprites, out string error)
        {
            info = null; sprites = new Dictionary<string, Sprite>();
            error = "Custom skills need an ID of at least 100000000, ranks, unique prerequisites and valid presentation.";
            if (Id < FirstCustomId || Ranks == null || Ranks.Length == 0 || Ranks.Any(r => r == null) ||
                Prerequisites == null || Prerequisites.Any(r => r == null || r.SkillId <= 0 || r.SkillId == Id || r.Level <= 0) ||
                Prerequisites.Select(r => r.SkillId).Distinct().Count() != Prerequisites.Length) return false;
            var borrowed = SourcePresentationSkillId == 0 ? null : source.GetSkill(SourcePresentationSkillId);
            if (SourcePresentationSkillId != 0 && borrowed == null) return false;
            var result = new SkillInfo {
                SkillId = Id, Name = DisplayName, Description = Description, JobId = JobId, InheritJob = InheritJob,
                IsPassive = Execution == SkillExecution.Passive,
                Type = Execution == SkillExecution.Passive ? SkillType.Passive : Execution == SkillExecution.Attack ? SkillType.Attack : SkillType.Buff,
                Action = Execution == SkillExecution.Passive ? null : Action,
                Behavior = new SkillBehavior { Execution = Execution, AttackFamily = AttackFamily, DamagePolicy = DamagePolicy,
                    Targeting = Targeting, RequiresWeapon = RequiresWeapon, UsesAmmunition = UsesAmmunition,
                    AllowedWeapons = AllowedWeapons?.ToArray(), BlocksCrouching = BlocksCrouching, ArmorEcho = ArmorEcho,
                    RequiresAction = !string.IsNullOrEmpty(Action) && Execution != SkillExecution.Passive, CastEffects = CastEffects?.ToArray() },
                RequiredSkills = Prerequisites.ToDictionary(r => r.SkillId, r => r.Level),
                MaxLevel = Ranks.Length, Levels = new Dictionary<int, SkillInfo.LevelData>(),
                IconFile = borrowed?.IconFile, IconPath = borrowed?.IconPath,
                Effects = borrowed == null ? new SortedDictionary<int, SkillEffectSet>() : new SortedDictionary<int, SkillEffectSet>(borrowed.Effects)
            };
            if (Icon != null) { result.IconFile = "unity"; result.IconPath = Id + "/icon"; sprites.Add(result.IconPath, Icon); }
            if (!CompileAnimation(Use, "use", sprites, out var use) || !CompileAnimation(Hit, "hit", sprites, out var hit) ||
                !CompileAnimation(Projectile, "projectile", sprites, out var projectile) ||
                !CompileAnimation(TargetStatus, "target-status", sprites, out var targetStatus)) return false;
            if (use != null || hit != null)
            {
                if (result.Effects.Count == 0) result.Effects[0] = new SkillEffectSet();
                foreach (var key in result.Effects.Keys.ToArray())
                {
                    var original = result.Effects[key];
                    result.Effects[key] = new SkillEffectSet { Use = use ?? original.Use, Hit = hit ?? original.Hit,
                        TwoHandedHit = hit ?? original.TwoHandedHit, HasTwoHandedHit = hit == null && original.HasTwoHandedHit };
                }
            }
            for (int index = 0; index < Ranks.Length; index++)
            {
                var r = Ranks[index];
                if (r.Buffs == null || r.Buffs.Any(b => b == null) || r.Buffs.Select(b => b.Type).Distinct().Count() != r.Buffs.Length ||
                    r.EffectParameters == null || r.EffectParameters.Any(p => p == null || string.IsNullOrWhiteSpace(p.Key) || !Finite(p.Value)) ||
                    r.EffectParameters.Select(p => p.Key).Distinct().Count() != r.EffectParameters.Length ||
                    !ValidRatio(r.PassiveMastery, 1) || !ValidRatio(r.PassiveDamageMultiplier, 100) || !ValidRatio(r.PassiveDamageReduction, 1) ||
                    !ValidRatio(r.PassiveCriticalChance, 1) || !ValidRatio(r.PassiveCriticalDamageMultiplier, 100) ||
                    r.PassiveCriticalDamageMultiplier != -1 && r.PassiveCriticalDamageMultiplier < 1 ||
                    r.PassiveProjectileRangeBonus < 0 || r.PassiveProjectileRangeBonus > 15600 ||
                    r.PassiveBelowHpPercent < -1 || r.PassiveBelowHpPercent > 100 ||
                    !Finite(r.Area.x) || !Finite(r.Area.y) || !Finite(r.Area.z) || !Finite(r.Area.w) ||
                    new[] { r.Area.x, r.Area.y, r.Area.z, r.Area.w }.Any(v => v < -16000 || v > 16000)) return false;
                var borrowedRank = borrowed?.Levels?.Values.FirstOrDefault();
                if (borrowed?.Levels?.TryGetValue(index + 1, out var matching) == true) borrowedRank = matching;
                result.Levels[index + 1] = new SkillInfo.LevelData {
                    HpCost = r.HpCost, MpCost = r.MpCost, Cooldown = r.CooldownMilliseconds, Duration = r.DurationMilliseconds,
                    EffectParameters = r.EffectParameters.ToDictionary(p => p.Key, p => p.Value),
                    Damage = r.Damage, AttackCount = r.AttackCount, MobCount = r.MobCount, Range = r.RangePercent,
                    BulletCount = r.BulletCount, BulletConsume = r.BulletConsume,
                    AttackBounds = Targeting == SkillTargeting.ForwardArea ? new AttackBounds((int)r.Area.x, (int)r.Area.y, (int)r.Area.z, (int)r.Area.w) : (AttackBounds?)null,
                    Buffs = r.Buffs.ToDictionary(b => b.Type, b => b.Value), Hp = r.HealHp, HpR = r.HealHpPercent, Mp = r.RestoreMp, MpR = r.RestoreMpPercent,
                    Projectile = projectile ?? borrowedRank?.Projectile,
                    TargetDebuff = r.ApplyTargetDebuff ? new MonsterDebuffDefinition {
                        PhysicalAttackChange = r.TargetPhysicalAttackChange, PhysicalDefenseChange = r.TargetPhysicalDefenseChange,
                        DurationMilliseconds = r.TargetDebuffMilliseconds, ChancePercent = r.TargetDebuffChance,
                        RejectSameSource = r.RejectSameDebuffSource, Visual = targetStatus ?? borrowedRank?.TargetDebuff?.Visual } : null,
                    Passive = new PassiveContribution { WeaponAttack = r.PassiveWeaponAttack, MagicAttack = r.PassiveMagicAttack,
                        Accuracy = r.PassiveAccuracy, Avoidability = r.PassiveAvoidability, ProjectileRangeBonus = r.PassiveProjectileRangeBonus,
                        CriticalChance = r.PassiveCriticalChance == -1 ? null : (float?)r.PassiveCriticalChance,
                        CriticalDamageMultiplier = r.PassiveCriticalDamageMultiplier == -1 ? null : (float?)r.PassiveCriticalDamageMultiplier,
                        Mastery = r.PassiveMastery == -1 ? null : (float?)r.PassiveMastery,
                        DamagePercent = r.PassiveDamageMultiplier == -1 ? null : (float?)r.PassiveDamageMultiplier,
                        DamageReduction = r.PassiveDamageReduction == -1 ? null : (float?)r.PassiveDamageReduction,
                        BelowHpPercent = r.PassiveBelowHpPercent == -1 ? null : (int?)r.PassiveBelowHpPercent }
                };
            }
            source.ReadAction(result);
            result.EffectFrames = result.Effects.Values.FirstOrDefault()?.Use?.Frames;
            if (!SkillValidation.Definition(result, effects, out error)) return false;
            info = result; return true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidRatio(float value, float max) => Finite(value) && (value == -1 || value >= 0 && value <= max);
        private bool CompileAnimation(Animation animation, string name, Dictionary<string, Sprite> sprites, out SkillEffectDefinition result)
        {
            result = null;
            if (animation?.Frames == null) return false;
            if (animation.Frames.Length == 0) return true;
            if (animation.Frames.Any(f => f == null || f.Sprite == null || f.Milliseconds <= 0 ||
                !ValidRatio(f.StartAlpha, 1) || f.StartAlpha < 0 || !ValidRatio(f.EndAlpha, 1) || f.EndAlpha < 0 ||
                !Finite(f.StartScale) || f.StartScale < 0 || !Finite(f.EndScale) || f.EndScale < 0)) return false;
            var frames = new List<AfterimageFrame>();
            for (int i = 0; i < animation.Frames.Length; i++)
            {
                var f = animation.Frames[i]; string key = Id + "/" + name + "/" + i; sprites.Add(key, f.Sprite);
                frames.Add(new AfterimageFrame { Path = key, DelayMilliseconds = f.Milliseconds,
                    StartAlpha = f.StartAlpha, EndAlpha = f.EndAlpha, StartScale = f.StartScale, EndScale = f.EndScale });
            }
            result = new SkillEffectDefinition { AssetFile = "unity", Frames = frames.ToArray(), Position = animation.Position, Z = animation.Z };
            return true;
        }
    }
}
