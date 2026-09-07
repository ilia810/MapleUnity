using System;
namespace MapleClient.GameLogic.Data
{
    /// <summary>One source use-effect cycle, continuing independently of the casting pose.</summary>
    public sealed class SkillUseVisual
    {
        private readonly WeaponAfterimage animation;
        private readonly float speed;
        private double accumulator;
        private int elapsed;
        public bool FacingRight { get; }
        public string AssetFile { get; }
        public int Z { get; set; }
        public MapleClient.GameLogic.Vector2 Offset { get; set; }
        public SkillUseVisual(AfterimageFrame[] frames, float speed, bool facingRight, string assetFile = "skill")
        { AssetFile = assetFile; animation = new WeaponAfterimage { Frames = frames }; this.speed = speed; FacingRight = facingRight; }
        public AfterimageFrame Sample(out float fraction)
        { int frame = animation.Sample(elapsed, out fraction); return frame < 0 ? null : animation.Frames[frame]; }
        public void Advance(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            accumulator += seconds;
            while (accumulator + 1e-8 >= .008)
            { accumulator = Math.Max(0, accumulator - .008); elapsed += Math.Max(1, (int)(8 * speed)); }
        }
    }
}
