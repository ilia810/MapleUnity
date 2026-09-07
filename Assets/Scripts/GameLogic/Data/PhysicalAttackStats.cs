// CharStats, Job, Player::prepare_attack and Mob damage rules from HeavenClient.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic.Data
{
    public readonly struct AttackHit
    {
        public int Damage { get; }
        public bool Critical { get; }
        public bool Miss => Damage == 0;
        public int LineIndex { get; }
        public AttackHit(int damage, bool critical, int lineIndex = 0) { Damage = damage; Critical = critical && damage > 0; LineIndex = lineIndex; }
    }

    public readonly struct PhysicalAttackStats
    {
        public double Minimum { get; }
        public double Maximum { get; }
        public int Accuracy { get; }
        public int Level { get; }
        public float CriticalChance { get; }
        public bool UsesMagicDefense { get; }
        public float CriticalDamageMultiplier { get; }
        public PhysicalAttackStats(double minimum, double maximum, int accuracy, int level, float criticalChance, bool magic = false, float criticalDamageMultiplier = 1.5f)
        { Minimum = minimum; Maximum = maximum; Accuracy = accuracy; Level = level; CriticalChance = criticalChance; UsesMagicDefense = magic; CriticalDamageMultiplier = criticalDamageMultiplier; }

        public static PhysicalAttackStats Calculate(int weaponId, int jobId, int str, int dex, int intelligence,
            int luk, int weaponAttack, int accuracyBonus, int level, float mastery = 0, float damagePercent = 0,
            float criticalChance = .05f, bool prone = false, bool skillAttack = false, float criticalDamageMultiplier = 1.5f)
        {
            // CharStats::add_value / set_total cap equipment totals before closing stats.
            str = Math.Min(999, str); dex = Math.Min(999, dex); intelligence = Math.Min(999, intelligence);
            luk = Math.Min(999, luk); weaponAttack = Math.Min(999, weaponAttack);
            int type = weaponId / 10000;
            int primaryStat, secondary;
            switch (jobId / 100)
            {
                case 2: primaryStat = intelligence; secondary = luk; break;
                case 3: primaryStat = dex; secondary = str; break;
                case 4: primaryStat = luk; secondary = dex; break;
                case 5: primaryStat = type == 149 ? dex : str; secondary = type == 149 ? str : dex; break;
                default: primaryStat = str; secondary = dex; break;
            }
            int primary = (int)(Multiplier(type) * primaryStat);
            float multiplier = damagePercent + weaponAttack / 100f;
            int maximum = (int)((primary + secondary) * multiplier);
            int minimum = (int)((primary * .9f * mastery + secondary) * multiplier);
            // Prone and ordinary wand/staff attacks use Player's degenerate close attack.
            bool degenerate = prone || (!skillAttack && (type == 137 || type == 138));
            // Source adds derived accuracy AFTER its capped equipment accuracy total.
            int accuracy = Math.Min(999, accuracyBonus) + (int)(dex * .8f + luk * .5f);
            return new PhysicalAttackStats(degenerate ? minimum / 10.0 : minimum, degenerate ? maximum / 10.0 : maximum, accuracy, level, criticalChance, criticalDamageMultiplier: criticalDamageMultiplier);
        }

        public static float Multiplier(int type)
        {
            switch (type)
            {
                case 130: return 4;
                case 131: case 132: case 137: case 138: return 4.4f;
                case 133: case 146: case 147: case 149: return 3.6f;
                case 140: return 4.6f;
                case 141: case 142: case 148: return 4.8f;
                case 143: case 144: return 5;
                case 145: return 3.4f;
                default: return 0;
            }
        }

        public float HitChance(MonsterTemplate target)
        {
            return AccuracyRules.HitChance(Accuracy, Level, target.Avoidability, target.Level);
        }
        public double MinimumAgainst(MonsterTemplate target, int? physicalDefense = null) => Math.Max(1, UsesMagicDefense ?
            Minimum - (1 + .01 * Math.Max(0, target.Level - Level)) * target.MagicDefense * .6 :
            Minimum * (1 - .01 * Math.Max(0, target.Level - Level)) - (physicalDefense ?? target.PhysicalDefense) * .6);
        public double MaximumAgainst(MonsterTemplate target, int? physicalDefense = null) => Math.Max(1, UsesMagicDefense ?
            Maximum - (1 + .01 * Math.Max(0, target.Level - Level)) * target.MagicDefense * .5 :
            Maximum * (1 - .01 * Math.Max(0, target.Level - Level)) - (physicalDefense ?? target.PhysicalDefense) * .5);

        // Keep the float draw below one, as Randomizer's [0,1) distribution does.
        private static float NextUnitFloat(Random random) => Math.Min(.99999994f, (float)random.NextDouble());

        public AttackHit Roll(MonsterTemplate target, Random random, int? physicalDefense = null)
        {
            // Randomizer::below draws a float; misses consume neither damage nor critical draws.
            if (NextUnitFloat(random) >= HitChance(target)) return new AttackHit(0, false);
            double minimum = MinimumAgainst(target, physicalDefense), maximum = MaximumAgainst(target, physicalDefense);
            double damage = minimum >= maximum ? minimum : minimum + random.NextDouble() * (maximum - minimum);
            bool critical = NextUnitFloat(random) < CriticalChance;
            if (critical) damage *= CriticalDamageMultiplier;
            return new AttackHit((int)Math.Max(1, Math.Min(999999, damage)), critical);
        }
    }
}
