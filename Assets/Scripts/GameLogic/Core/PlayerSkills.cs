using System;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameLogic.Core
{
    public partial class Player
    {
        internal Func<PassiveStats> PassiveStatsSource;
        private PassiveStats Passives => PassiveStatsSource?.Invoke() ?? default;
        public int EquippedWeaponType => equippedItems.TryGetValue(EquipSlot.Weapon, out int id) ? id / 10000 : 0;
        public float PassiveDamageReduction => Passives.DamageReduction;
        public SkillUseVisual SkillEffect { get; internal set; }
        internal int CombatContextVersion { get; private set; }
        // The inspected C++ magic path uses these WATK-derived bounds, with INT/LUK
        // for magicians. It loads skill mad but never consumes it in damage calculation.
        internal PhysicalAttackStats SourceSkillAttackStats => PhysicalAttackStats.Calculate(
            equippedItems.TryGetValue(EquipSlot.Weapon, out int id) ? id : 0,
            JobId, STR, DEX, INT, LUK, WeaponAttack, AccuracyBonus,
            Level, CombatMastery, CombatDamagePercent, CriticalChance, State == PlayerState.Crouching, true, CriticalDamageMultiplier);
        public PhysicalAttackStats MagicSkillAttackStats
        {
            get { var stats = SourceSkillAttackStats; return new PhysicalAttackStats(stats.Minimum, stats.Maximum, stats.Accuracy, stats.Level, stats.CriticalChance, true, stats.CriticalDamageMultiplier); }
        }
        internal void NotifySkillStatsChanged() => OnStatsChanged();
        internal void ShowSkillUse(SkillInfo skill)
        {
            if (skill.Behavior.ArmorEcho) ArmorEchoMilliseconds = 500;
            var effect = SourceSkillRules.EffectsFor(skill, Level)?.Use;
            if (effect?.Frames.Length > 0)
                SkillEffect = new SkillUseVisual(effect.Frames, 1.7f - EffectiveAttackSpeed / 10f, FacingRight ?? true, effect.AssetFile) { Z = effect.Z };
        }
        internal bool PlaySkillAnimation(SkillInfo skill, bool attack = false)
        {
            if (IsDead || attack && IsHidden || IsBasicAttacking || State == PlayerState.Climbing || skill.ActionDelays == null || skill.ActionDelays.Length == 0) return false;
            var hitDelays = attack ? (skill.ActionHitDelays ?? Array.Empty<int>()).Select(t => new SourceAttackTiming(EffectiveAttackSpeed, t).HitDelayMilliseconds).ToArray() : null;
            WeaponAfterimage trail = null;
            if (attack && CurrentWeapon != null) CurrentWeapon.Afterimages.TryGetValue(skill.ActionStances[0], out trail);
            BasicAttack = new BasicAttackMotion(skill.ActionStances[0], skill.ActionDelays, 8, trail,
                hitDelays?.FirstOrDefault() ?? 0, skill.ActionStances, skill.ActionFrames, skill.ActionMoves, hitDelays);
            BasicAttack.FacingRight = FacingRight ?? true;
            BasicAttack.Completed += FinishBasicAttack;
            BasicAttack.TickAdvance = () => Math.Max(1, (int)(8 * (1.7f - EffectiveAttackSpeed / 10f)));
            if (!attack) ShowSkillUse(skill);
            TriggerAnimationEvent(PlayerAnimationEvent.Attack);
            return true;
        }
        // Skill HP costs spend HP without creating a contact hit, knockback or damage number.
        internal void SpendSkillHp(int amount) { hp -= amount; OnStatsChanged(); }
    }
}
