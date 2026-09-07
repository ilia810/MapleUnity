using System;
namespace MapleClient.GameLogic.Data
{
    public sealed class SkillEffectDefinition
    {
        public AfterimageFrame[] Frames { get; set; } = Array.Empty<AfterimageFrame>();
        public int Position { get; set; }
        public int Z { get; set; }
        public string AssetFile { get; set; } = "skill";
    }
    public sealed class SkillEffectSet
    {
        public SkillEffectDefinition Use { get; set; }
        public SkillEffectDefinition Hit { get; set; }
        public SkillEffectDefinition TwoHandedHit { get; set; }
        public bool HasTwoHandedHit { get; set; }
    }
}
