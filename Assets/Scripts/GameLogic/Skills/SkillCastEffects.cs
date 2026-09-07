using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Skills
{
    public interface ISkillEffectSource { SkillCastEffects CastEffects { get; } }
    /// <summary>Validate without mutation. Apply runs once, after every check and cost succeeds.</summary>
    public interface ISkillCastEffect
    {
        string Key { get; }
        bool Validate(SkillInfo.LevelData level, out string error);
        void Apply(Player caster, SkillInfo skill, SkillInfo.LevelData level);
    }

    public sealed class SkillCastEffects
    {
        public const string StatBuff = "stat-buff", Recovery = "recovery";
        private readonly Dictionary<string, ISkillCastEffect> handlers = new Dictionary<string, ISkillCastEffect>();
        public SkillCastEffects() { Register(new StatBuffEffect()); Register(new RecoveryEffect()); }
        public void Register(ISkillCastEffect handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.Key) || handlers.ContainsKey(handler.Key))
                throw new ArgumentException("A skill effect needs a unique, nonempty key.");
            handlers.Add(handler.Key, handler);
        }
        public bool Validate(SkillInfo skill, SkillInfo.LevelData level, out string error)
        {
            error = "This skill's effect is not implemented yet; your resources are preserved.";
            if (skill?.Behavior?.Available != true || level == null) return false;
            var effects = skill.Behavior.CastEffects;
            if (effects == null || effects.Distinct().Count() != effects.Length) return false;
            if (skill.Behavior.Execution == SkillExecution.Self && effects.Length == 0) return false;
            foreach (var key in effects)
            {
                if (key == null || !handlers.TryGetValue(key, out var handler)) { error = "Unknown skill effect: " + (key ?? "<empty>"); return false; }
                if (!handler.Validate(level, out error)) return false;
            }
            error = ""; return true;
        }
        public void Apply(Player caster, SkillInfo info, SkillInfo.LevelData level)
        { foreach (var key in info.Behavior.CastEffects) handlers[key].Apply(caster, info, level); }

        private sealed class StatBuffEffect : ISkillCastEffect
        {
            public string Key => StatBuff;
            public bool Validate(SkillInfo.LevelData level, out string error)
            {
                error = "This buff effect is not available yet";
                return Player.ValidStatBuffs(level.Buffs, level.Duration);
            }
            public void Apply(Player caster, SkillInfo skill, SkillInfo.LevelData level) => caster.ApplyStatBuffs(skill.SkillId, skill.Name, level.Buffs, level.Duration);
        }
        private sealed class RecoveryEffect : ISkillCastEffect
        {
            public string Key => Recovery;
            public bool Validate(SkillInfo.LevelData level, out string error)
            {
                error = "Recovery amounts must be nonnegative, with at least one effect.";
                return level.Hp >= 0 && level.Mp >= 0 && level.HpR >= 0 && level.HpR <= 100 && level.MpR >= 0 && level.MpR <= 100 &&
                    (level.Hp > 0 || level.Mp > 0 || level.HpR > 0 || level.MpR > 0);
            }
            public void Apply(Player caster, SkillInfo skill, SkillInfo.LevelData level)
            {
                caster.Heal((int)Math.Min(int.MaxValue, (long)level.Hp + (long)caster.MaxHP * level.HpR / 100));
                caster.RestoreMana((int)Math.Min(int.MaxValue, (long)level.Mp + (long)caster.MaxMP * level.MpR / 100));
                caster.OnStatsChanged();
            }
        }
    }
}
