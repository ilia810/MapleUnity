using System;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Core
{
    public sealed class ActiveMonsterDebuff
    {
        public int SourceId { get; }
        public string Name { get; }
        public int PhysicalAttackChange { get; }
        public int PhysicalDefenseChange { get; }
        public int RemainingMilliseconds { get; internal set; }
        public SkillEffectDefinition Visual { get; }
        public int ElapsedMilliseconds { get; internal set; }
        internal ActiveMonsterDebuff(int source, string name, MonsterDebuffDefinition data)
        {
            SourceId = source; Name = name; PhysicalAttackChange = data.PhysicalAttackChange;
            PhysicalDefenseChange = data.PhysicalDefenseChange; RemainingMilliseconds = data.DurationMilliseconds;
            Visual = data.Visual;
        }
    }

    public partial class Monster
    {
        public ActiveMonsterDebuff StatDebuff { get; private set; }
        public int PhysicalAttack => Math.Max(0, template.PhysicalDamage + (StatDebuff?.PhysicalAttackChange ?? 0));
        public int PhysicalDefense => Math.Max(0, template.PhysicalDefense + (StatDebuff?.PhysicalDefenseChange ?? 0));
        public bool CanApplyDebuff(int source, MonsterDebuffDefinition data) => !IsDead && data?.IsValid == true &&
            (!data.RejectSameSource || StatDebuff?.SourceId != source);
        public bool TryApplyDebuff(int source, string name, MonsterDebuffDefinition data)
        {
            if (!CanApplyDebuff(source, data)) return false;
            // One physical weakening slot: a different source replaces the whole effect.
            // Never mutate the shared NX template or stack recasts into permanent stats.
            StatDebuff = new ActiveMonsterDebuff(source, name, data); return true;
        }
        private void TickDebuff()
        {
            if (StatDebuff == null) return;
            StatDebuff.RemainingMilliseconds = Math.Max(0, StatDebuff.RemainingMilliseconds - 8);
            StatDebuff.ElapsedMilliseconds += 8;
            if (StatDebuff.RemainingMilliseconds == 0) StatDebuff = null;
        }
    }
}
