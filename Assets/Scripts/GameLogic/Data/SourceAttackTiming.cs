// Char::get_real_attackspeed/get_attackdelay from HeavenClient. AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic.Data
{
    internal sealed class SourceAttackTiming
    {
        // Materialize each source float operation. Mono can otherwise retain extra
        // precision until the integer cast (149 instead of 150 ms at speed 1).
        private readonly float speedFraction;
        public readonly float Multiplier;
        private readonly float impact;
        public int HitDelayMilliseconds => (int)impact;
        public int AdvancePerTick => Math.Max(1, (int)(8 * Multiplier));
        public SourceAttackTiming(int speed, int unscaledDelay)
        {
            speedFraction = speed / 10f;
            Multiplier = 1.7f - speedFraction;
            impact = unscaledDelay / Multiplier;
        }
    }
}
