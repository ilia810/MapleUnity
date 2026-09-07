using System;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Skills
{
    public enum SkillExecution { Unavailable, Attack, Self, Passive }
    public enum SkillAttackFamily { None, Melee, Magic, Ranged }
    public enum SkillDamagePolicy { SourceClient, WeaponPhysical, FixedPhysical, FixedMagic, None }
    public enum SkillTargeting { SourceWeapon, ForwardArea }

    /// <summary>Executable capabilities, independent of an ID or asset format.</summary>
    public sealed class SkillBehavior
    {
        public SkillExecution Execution { get; set; }
        public SkillAttackFamily AttackFamily { get; set; }
        public SkillDamagePolicy DamagePolicy { get; set; }
        public SkillTargeting Targeting { get; set; }
        public bool RequiresWeapon { get; set; }
        public int[] AllowedWeapons { get; set; } = Array.Empty<int>();
        public bool UsesAmmunition { get; set; }
        public bool BlocksCrouching { get; set; }
        public bool RequiresAction { get; set; } = true;
        public bool ArmorEcho { get; set; }
        public string[] CastEffects { get; set; } = Array.Empty<string>();
        public SkillBehavior Copy()
        {
            var copy = (SkillBehavior)MemberwiseClone();
            copy.AllowedWeapons = (int[])AllowedWeapons.Clone(); copy.CastEffects = (string[])CastEffects.Clone(); return copy;
        }
        public bool Available => Execution != SkillExecution.Unavailable;
        public bool IsAttack => Execution == SkillExecution.Attack;
        public bool WeaponMatches(int type) => (!RequiresWeapon || type != 0) &&
            (AllowedWeapons.Length == 0 || AllowedWeapons.Contains(type));
    }

    /// <summary>Normalized passive values. NX x/y/z decoding stays in its catalog.</summary>
    public sealed class PassiveContribution
    {
        public int WeaponAttack, MagicAttack, Accuracy, Avoidability, ProjectileRangeBonus;
        public float? Mastery, DamagePercent, DamageReduction, CriticalChance, CriticalDamageMultiplier;
        public bool ValidRangedValues => ProjectileRangeBonus >= 0 && ProjectileRangeBonus <= 15600 &&
            ValidOptional(CriticalChance, 0, 1) && ValidOptional(CriticalDamageMultiplier, 1, 100);
        private static bool ValidOptional(float? value, float min, float max) => !value.HasValue ||
            !float.IsNaN(value.Value) && value.Value >= min && value.Value <= max;
        public int? BelowHpPercent;
        public void Apply(ref PassiveStats result, int maxHp, int hp)
        {
            if (BelowHpPercent.HasValue && hp > (int)(maxHp * (BelowHpPercent.Value / 100f))) return;
            result.WeaponAttack += WeaponAttack; result.MagicAttack += MagicAttack;
            result.Accuracy += Accuracy; result.Avoidability += Avoidability;
            result.ProjectileRangeBonus = Math.Min(15600, result.ProjectileRangeBonus + ProjectileRangeBonus);
            // Independent sources use the strongest critical contribution, not registration order.
            if (CriticalChance.HasValue) result.CriticalChance = Math.Max(result.CriticalChance ?? 0, CriticalChance.Value);
            if (CriticalDamageMultiplier.HasValue) result.CriticalDamageMultiplier = Math.Max(result.CriticalDamageMultiplier ?? 1, CriticalDamageMultiplier.Value);
            if (Mastery.HasValue) result.Mastery = Mastery;
            if (DamagePercent.HasValue) result.DamagePercent = DamagePercent;
            if (DamageReduction.HasValue) result.DamageReduction = DamageReduction.Value;
        }
    }

    public static class SkillRules
    {
        public static int JobTier(int job) => job == 0 ? 0 : job % 100 == 0 ? 1 : job % 10 == 0 ? 2 : job % 10 == 1 ? 3 : 4;
        public static bool JobMatches(int job, SkillInfo info) => info != null &&
            (info.InheritJob ? SourceSkillRules.JobBranches(job).Contains(info.JobId) : info.JobId == job);
        public static void ApplyPassive(ref PassiveStats result, SkillInfo info, SkillInfo.LevelData data, Player player)
        {
            if (info.Behavior.Execution == SkillExecution.Passive && info.Behavior.WeaponMatches(player.EquippedWeaponType))
                data.Passive?.Apply(ref result, player.MaxHP, player.CurrentHP);
        }
        public static PhysicalAttackStats AttackStats(Player player, SkillInfo info, SkillInfo.LevelData level)
        {
            switch (info.Behavior.DamagePolicy)
            {
                case SkillDamagePolicy.FixedPhysical: case SkillDamagePolicy.FixedMagic:
                    return new PhysicalAttackStats(level.Damage, level.Damage, player.Accuracy, player.Level,
                        player.CriticalChance, info.Behavior.DamagePolicy == SkillDamagePolicy.FixedMagic, player.CriticalDamageMultiplier);
                case SkillDamagePolicy.WeaponPhysical:
                    var physical = player.PhysicalAttackStats;
                    return new PhysicalAttackStats(physical.Minimum * level.DamageMultiplier, physical.Maximum * level.DamageMultiplier,
                        physical.Accuracy, physical.Level, physical.CriticalChance, false, physical.CriticalDamageMultiplier);
                default:
                    // Preserve the inspected C++ formula for imported skills, including its mad behavior.
                    return SourceSkillRules.ApplyAttackStats(info.Behavior.AttackFamily == SkillAttackFamily.Magic ?
                        player.SourceSkillAttackStats : player.PhysicalAttackStats, level);
            }
        }
    }
}
