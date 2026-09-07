// Bullet.cpp movement from HeavenClient. AGPL-3.0-or-later.
using System;
using System.Linq;

namespace MapleClient.GameLogic.Data
{
    /// <summary>A source bullet travels at fixed velocity; the live target only controls arrival.</summary>
    public sealed class SkillProjectile
    {
        private double x, y, previousX, previousY, speedX, speedY;
        private readonly WeaponAfterimage animation;
        private readonly int cycle;
        public bool FacingRight { get; private set; }
        public bool HasArrived { get; private set; }
        public int ElapsedMilliseconds { get; private set; }
        public string AssetFile { get; }
        public double SourceX => x;
        public double SourceY => y;
        public Vector2 Position => new Vector2((float)x / 100, -(float)y / 100);
        public Vector2 InterpolatedPosition(float alpha) => new Vector2(
            (float)(previousX + (x - previousX) * alpha) / 100,
            -(float)(previousY + (y - previousY) * alpha) / 100);

        public SkillProjectile(int originX, int originY, bool facingRight, int targetX, int targetY, SkillEffectDefinition art)
        {
            AssetFile = art.AssetFile;
            x = previousX = originX + (facingRight ? 30 : -30);
            y = previousY = originY - 26;
            animation = new WeaponAfterimage { Frames = art.Frames };
            cycle = Math.Max(1, art.Frames.Sum(f => Math.Max(1, f.DelayMilliseconds)));
            double dx = targetX - x, dy = targetY - y;
            if (Math.Abs(dx) < 10) { HasArrived = true; return; }
            FacingRight = dx > 0;
            speedX = dx > 0 ? Math.Max(3, Math.Min(6, dx / 32)) : Math.Min(-3, Math.Max(-6, dx / 32));
            speedY = speedX * dy / dx;
        }
        public void Tick(int targetX)
        {
            if (HasArrived) return;
            ElapsedMilliseconds += 8;
            previousX = x; previousY = y; x += speedX; y += speedY;
            int delta = (short)(targetX - (int)Math.Round(x, MidpointRounding.AwayFromZero));
            // Preserve Bullet::update's asymmetric leftward threshold in this checkout.
            HasArrived = speedX > 0 ? delta < 10 : delta > 10;
        }
        public AfterimageFrame Sample(out float fraction)
        {
            int frame = animation.Sample(ElapsedMilliseconds % cycle, out fraction);
            return frame < 0 ? null : animation.Frames[frame];
        }
    }
}
