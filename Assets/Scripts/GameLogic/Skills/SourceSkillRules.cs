// Job, SkillData flags, PassiveBuffs and CharStats follow HeavenClient.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Skills
{
    public struct PassiveStats
    {
        public int WeaponAttack, MagicAttack, Accuracy, Avoidability;
        public float? Mastery, DamagePercent, CriticalChance, CriticalDamageMultiplier;
        public int ProjectileRangeBonus;
        public float DamageReduction;
    }
    public static class SourceSkillRules
    {
        public static IEnumerable<int> JobBranches(int job)
        {
            yield return 0;
            int level = job == 0 ? 0 : job % 100 == 0 ? 1 : job % 10 == 0 ? 2 : job % 10 == 1 ? 3 : 4;
            if (level >= 1) yield return job / 100 * 100;
            if (level >= 2) yield return job / 10 * 10;
            if (level >= 3) yield return level == 4 ? job - 1 : job;
            if (level >= 4) yield return job;
        }
        public static bool CanUse(int job, int skillId)
        { foreach (int branch in JobBranches(job)) if (branch == skillId / 10000) return true; return false; }
        public static int WeaponType(int type) => (type >= 130 && type <= 133) || type == 137 || type == 138 || (type >= 140 && type <= 149) ? type : 0;
        // Compatibility queries for source-oracle fixtures and explicit practice kits.
        // Runtime casting, combat, progression and UI use SkillInfo.Behavior instead.
        public static bool IsBooster(int id) => ClassicSkillCatalog.Get(id)?.Booster == true;
        public static bool IsSupportBuff(int id) => ClassicSkillCatalog.Get(id)?.Behavior.Execution == SkillExecution.Self && !IsBooster(id);
        public static bool IsSupportedBuff(int id) => ClassicSkillCatalog.Get(id)?.Behavior.Execution == SkillExecution.Self;
        public static bool IsArmorEcho(int id) => ClassicSkillCatalog.Get(id)?.Behavior.ArmorEcho == true;
        public static bool IsMeleeAttack(int id) => ClassicSkillCatalog.Get(id)?.Behavior.AttackFamily == SkillAttackFamily.Melee;
        public static bool IsMagicAttack(int id) => ClassicSkillCatalog.Get(id)?.Behavior.AttackFamily == SkillAttackFamily.Magic;
        public static bool IsRangedAttack(int id) => ClassicSkillCatalog.Get(id)?.Behavior.AttackFamily == SkillAttackFamily.Ranged;
        public static bool RangedWeaponMatches(int id, int weapon) => IsRangedAttack(id) && ClassicSkillCatalog.Get(id).Behavior.WeaponMatches(weapon);
        public static int[] RangedPracticeSkills(int weapon) => WeaponPracticeSkills(weapon);
        public static int[] WeaponPracticeSkills(int weapon) => weapon == 133 ? new[] { 4001002, 4001334, 4001003 } :
            weapon == 145 || weapon == 146 ? new[] { 3001004, 3001005 } :
            weapon == 147 ? new[] { 4001344 } : weapon == 149 ? new[] { 5001003 } : Array.Empty<int>();
        public static bool IsSupportedAttack(int id) => ClassicSkillCatalog.Get(id)?.Behavior.IsAttack == true;
        public static PhysicalAttackStats ApplyAttackStats(PhysicalAttackStats stats, SkillInfo.LevelData data) => new PhysicalAttackStats(
            stats.Minimum * (data.MagicAttack != 0 ? 1 : data.DamageMultiplier),
            stats.Maximum * (data.MagicAttack != 0 ? 1 : data.DamageMultiplier),
            stats.Accuracy, stats.Level, stats.CriticalChance, data.MagicAttack != 0, stats.CriticalDamageMultiplier);
        public static MapleClient.GameLogic.Data.SkillEffectSet EffectsFor(SkillInfo skill, int characterLevel)
        {
            MapleClient.GameLogic.Data.SkillEffectSet selected = null;
            // The C++ selector uses the preceding threshold on an exact match, except at the first entry.
            foreach (var pair in skill.Effects)
            { if (selected == null) selected = pair.Value; if (pair.Key >= characterLevel) break; selected = pair.Value; }
            return selected;
        }
        public static bool IsAttack(int id) => ClassicSkillCatalog.IsAttackMetadata(id);
        public static bool SupportsPassive(int id) => ClassicSkillCatalog.Get(id)?.Behavior.Execution == SkillExecution.Passive;
        public static void ApplyPassive(ref PassiveStats stats, int id, SkillInfo.LevelData data, int weapon, int maxHp, int hp)
        {
            if (ClassicSkillCatalog.Get(id)?.Behavior.WeaponMatches(weapon) == true)
                ClassicSkillCatalog.DecodePassive(id,data)?.Apply(ref stats,maxHp,hp);
        }
    }
}
