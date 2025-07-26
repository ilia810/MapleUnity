using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic
{
    public class SkillData
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxLevel { get; set; }
        public SkillType Type { get; set; }
        public float Cooldown { get; set; }
        public int MPCost { get; set; }
        public float Range { get; set; }
        public int TargetCount { get; set; }
        public float Duration { get; set; }
        public int DamagePercent { get; set; }
    }
}