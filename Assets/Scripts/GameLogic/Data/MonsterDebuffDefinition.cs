namespace MapleClient.GameLogic.Data
{
    /// <summary>One timed physical weakening effect, shared by original and authored skills.</summary>
    public sealed class MonsterDebuffDefinition
    {
        public int PhysicalAttackChange { get; set; }
        public int PhysicalDefenseChange { get; set; }
        public int DurationMilliseconds { get; set; }
        public int ChancePercent { get; set; } = 100;
        public bool RejectSameSource { get; set; }
        public SkillEffectDefinition Visual { get; set; }
        public bool IsValid => DurationMilliseconds > 0 && ChancePercent >= 0 && ChancePercent <= 100 &&
            PhysicalAttackChange <= 0 && PhysicalAttackChange >= -999999 &&
            PhysicalDefenseChange <= 0 && PhysicalDefenseChange >= -999999 &&
            (PhysicalAttackChange < 0 || PhysicalDefenseChange < 0);
        public MonsterDebuffDefinition Copy() => (MonsterDebuffDefinition)MemberwiseClone();
    }
}
