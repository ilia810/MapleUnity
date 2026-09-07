using System;
using System.Linq;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Skills
{
    public static class SkillValidation
    {
        public static bool Definition(SkillInfo info, SkillCastEffects effects, out string error)
        {
            error = "A skill needs a positive ID, a job, a name and contiguous levels starting at 1.";
            if (info == null || info.SkillId <= 0 || info.JobId < 0 || info.JobId > 9999 || string.IsNullOrWhiteSpace(info.Name) ||
                info.Levels == null || info.MaxLevel <= 0 || info.MaxLevel > 1000 || info.Levels.Count != info.MaxLevel ||
                Enumerable.Range(1, info.MaxLevel).Any(level => !info.Levels.ContainsKey(level))) return false;
            foreach (var pair in info.Levels)
                if (!Level(info, pair.Value, effects, out error)) { error = "Level " + pair.Key + ": " + error; return false; }
            error = ""; return true;
        }

        public static bool Level(SkillInfo info, SkillInfo.LevelData data, SkillCastEffects effects, out string error)
        {
            error = "This skill action is not available yet";
            var behavior = info?.Behavior;
            if (behavior?.Available != true || data == null) return false;
            error = "Invalid skill costs or behavior configuration.";
            if (!Enum.IsDefined(typeof(SkillExecution), behavior.Execution) || !Enum.IsDefined(typeof(SkillAttackFamily), behavior.AttackFamily) ||
                !Enum.IsDefined(typeof(SkillDamagePolicy), behavior.DamagePolicy) || !Enum.IsDefined(typeof(SkillTargeting), behavior.Targeting) ||
                data.HpCost < 0 || data.MpCost < 0 || data.Cooldown < 0 || data.Duration < 0 ||
                behavior.AllowedWeapons == null || behavior.AllowedWeapons.Any(w => SourceSkillRules.WeaponType(w) == 0)) return false;
            if (data.TargetDebuff != null && (!behavior.IsAttack || !data.TargetDebuff.IsValid))
            { error = "Target weakening needs a supported attack, nonpositive stat changes and a valid duration/chance."; return false; }
            if (behavior.DamagePolicy == SkillDamagePolicy.None && (!behavior.IsAttack || data.TargetDebuff == null ||
                behavior.UsesAmmunition || data.Projectile?.Frames.Length > 0 || data.AttackCount != 1))
            { error = "A non-damaging attack needs one direct target-effect attempt per monster."; return false; }
            if (behavior.Execution == SkillExecution.Passive)
            {
                error = "A passive skill needs passive values and cannot contain cast effects.";
                return info.IsPassive && data.Passive != null && data.Passive.ValidRangedValues && behavior.CastEffects?.Length == 0;
            }
            if (info.IsPassive || behavior.IsAttack && info.Type != SkillType.Attack ||
                behavior.Execution == SkillExecution.Self && info.Type != SkillType.Buff && info.Type != SkillType.Recovery) return false;
            error = "Skill action data is unavailable";
            bool hasAction = info.ActionDelays?.Length > 0;
            if (behavior.RequiresAction && !hasAction || !string.IsNullOrEmpty(info.Action) && !hasAction) return false;
            if (hasAction && (info.ActionStances?.Length != info.ActionDelays.Length || info.ActionFrames?.Length != info.ActionDelays.Length ||
                info.ActionDelays.Any(t => t <= 0) || info.ActionFrames.Any(f => f < 0))) return false;
            if (behavior.IsAttack)
            {
                error = "Attack targeting, damage or ammunition data is invalid.";
                if (behavior.AttackFamily == SkillAttackFamily.None || data.Damage < 0 || data.Range <= 0 || data.AttackCount < 1 || data.AttackCount > 32 ||
                    data.MobCount < 1 || data.MobCount > 64 || behavior.UsesAmmunition && (data.BulletConsume < 1 || data.BulletCount < 1 || data.BulletCount > 32)) return false;
                if (behavior.Targeting == SkillTargeting.ForwardArea && (!data.AttackBounds.HasValue ||
                    data.AttackBounds.Value.Left >= data.AttackBounds.Value.Right || data.AttackBounds.Value.Top >= data.AttackBounds.Value.Bottom)) return false;
            }
            return effects.Validate(info, data, out error);
        }
    }
}
