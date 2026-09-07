// Import descriptors for the inspected HeavenClient/NX skill set.
// Source behavior: Copyright 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;
using System.Collections.Generic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Skills
{
    /// <summary>The only mapping from original skill IDs to executable capabilities.
    /// An NX entry absent from this catalog stays inspectable but unavailable.</summary>
    public static class ClassicSkillCatalog
    {
        public enum PassiveImport { None, Blessing, Mastery, Achilles, Berserk, Accuracy, AccuracyAndAvoidability, ProjectileRange, Critical }
        public sealed class Entry
        {
            public SkillBehavior Behavior { get; internal set; }
            public Dictionary<string, BuffType> BuffFields { get; internal set; } = new Dictionary<string, BuffType>();
            public PassiveImport Passive { get; internal set; }
            public bool PhysicalWeakening { get; internal set; }
            public bool Booster => BuffFields.ContainsValue(BuffType.Booster);
        }
        private static readonly Dictionary<int, Entry> entries = Build();
        public static Entry Get(int id) => entries.TryGetValue(id, out var value) ? value : null;
        private static Entry Buff(bool armor = false, params (string, BuffType)[] fields)
        {
            var entry = new Entry { Behavior = new SkillBehavior { Execution = SkillExecution.Self, ArmorEcho = armor,
                CastEffects = new[] { SkillCastEffects.StatBuff } } };
            foreach (var field in fields) entry.BuffFields.Add(field.Item1, field.Item2);
            return entry;
        }
        private static Entry Attack(SkillAttackFamily family, params int[] weapons) => new Entry {
            Behavior = new SkillBehavior { Execution = SkillExecution.Attack, AttackFamily = family,
                RequiresWeapon = true, AllowedWeapons = weapons, RequiresAction = false,
                UsesAmmunition = family == SkillAttackFamily.Ranged, BlocksCrouching = family == SkillAttackFamily.Ranged,
                DamagePolicy = SkillDamagePolicy.SourceClient, Targeting = SkillTargeting.SourceWeapon } };
        private static Entry Passive(PassiveImport kind, params int[] weapons) => new Entry {
            Passive = kind, Behavior = new SkillBehavior { Execution = SkillExecution.Passive, AllowedWeapons = weapons, RequiresAction = false } };
        private static Dictionary<int, Entry> Build()
        {
            var result = new Dictionary<int, Entry>();
            foreach (int id in new[] { 1001004, 1001005 })
                result.Add(id, Attack(SkillAttackFamily.Melee, 130,131,132,133,140,141,142,143,144,148));
            foreach (int id in new[] { 2001004, 2001005 })
                result.Add(id, Attack(SkillAttackFamily.Magic, 130,131,132,133,137,138,140,141,142,143,144,148));
            foreach (int id in new[] { 3001004, 3001005 }) result.Add(id, Attack(SkillAttackFamily.Ranged, 145,146));
            result.Add(4001344, Attack(SkillAttackFamily.Ranged, 147));
            result.Add(4001334, Attack(SkillAttackFamily.Melee, 133));
            var disorder = Attack(SkillAttackFamily.Melee);
            disorder.Behavior.DamagePolicy = SkillDamagePolicy.None;
            disorder.PhysicalWeakening = true;
            result.Add(4001002, disorder);
            result.Add(5001003, Attack(SkillAttackFamily.Ranged, 149));
            foreach (int id in new[] { 1101004,1101005,1201004,1201005,1301004,1301005 })
                result.Add(id, Buff(false, ("x", BuffType.Booster)));
            foreach (int id in new[] { 1001003,2001003 }) result.Add(id, Buff(true, ("pdd", BuffType.WeaponDefense)));
            result.Add(2001002, Buff(false, ("x", BuffType.MagicGuard)));
            result.Add(1101006, Buff(false, ("pad", BuffType.WeaponAttack), ("pdd", BuffType.WeaponDefense)));
            result.Add(1301006, Buff(false, ("pdd", BuffType.WeaponDefense), ("mdd", BuffType.MagicDefense)));
            result.Add(1301007, Buff(false, ("x", BuffType.MaxHPPercent), ("y", BuffType.MaxMPPercent)));
            foreach (int id in new[] { 2101001,2201001 }) result.Add(id, Buff(false, ("mad", BuffType.MagicAttack)));
            result.Add(3001003, Buff(false, ("acc", BuffType.Accuracy), ("eva", BuffType.Avoidability)));
            result.Add(4001003, Buff(false, ("x", BuffType.Hide), ("speed", BuffType.Speed)));
            foreach (int id in new[] { 4101004,4201003 }) result.Add(id, Buff(false, ("speed", BuffType.Speed), ("jump", BuffType.Jump)));
            result.Add(3000000, Passive(PassiveImport.Accuracy));
            result.Add(4000000, Passive(PassiveImport.AccuracyAndAvoidability));
            result.Add(3000002, Passive(PassiveImport.ProjectileRange, 145,146));
            result.Add(4000001, Passive(PassiveImport.ProjectileRange, 147));
            result.Add(3000001, Passive(PassiveImport.Critical, 145,146));
            result.Add(12, Passive(PassiveImport.Blessing));
            result.Add(1100000, Passive(PassiveImport.Mastery, 130,140));
            result.Add(1100001, Passive(PassiveImport.Mastery, 131,141));
            // The inspected source repeats Fighter Sword Mastery in the Page registry.
            // 1200000 deliberately stays unavailable until that discrepancy is resolved.
            result.Add(1200001, Passive(PassiveImport.Mastery, 132,142));
            result.Add(1300000, Passive(PassiveImport.Mastery, 143));
            result.Add(1300001, Passive(PassiveImport.Mastery, 144));
            foreach (int id in new[] { 1120004,1220005,1320005 }) result.Add(id, Passive(PassiveImport.Achilles));
            result.Add(1320006, Passive(PassiveImport.Berserk));
            return result;
        }
        public static PassiveContribution DecodePassive(int id, SkillInfo.LevelData level)
        {
            switch (Get(id)?.Passive)
            {
                case PassiveImport.Blessing: return new PassiveContribution { WeaponAttack=level.X, MagicAttack=level.Y, Accuracy=level.Z, Avoidability=level.Z };
                case PassiveImport.Accuracy: return new PassiveContribution { Accuracy=level.X };
                case PassiveImport.AccuracyAndAvoidability: return new PassiveContribution { Accuracy=level.X, Avoidability=level.Y };
                case PassiveImport.ProjectileRange: return new PassiveContribution { ProjectileRangeBonus=level.Range };
                case PassiveImport.Critical: return new PassiveContribution { CriticalChance=level.Prop/100f, CriticalDamageMultiplier=level.Damage/100f };
                case PassiveImport.Mastery: return new PassiveContribution { Mastery=.5f+level.Mastery/100f, Accuracy=level.X };
                case PassiveImport.Achilles: return new PassiveContribution { DamageReduction=level.X/1000f };
                case PassiveImport.Berserk: return new PassiveContribution { BelowHpPercent=level.X, DamagePercent=level.Damage/100f };
                default: return null;
            }
        }
        // Presentation metadata for unported attacks does not grant execution.
        private static readonly HashSet<int> attackIds = new HashSet<int> { 1000,1001004,1001005,1111003,1111004,1111005,1111006,
            1121006,1121008,1211002,1221007,1221009,1221011,1311001,1311002,1311003,1311004,1311005,1311006,
            1321003,2001004,2001005,2101003,2101004,2101005,2111002,2111004,2111006,2121003,2121006,2121007 };
        public static bool IsAttackMetadata(int id) => attackIds.Contains(id) || Get(id)?.Behavior.IsAttack == true;
    }
}
