using System;

namespace MapleClient.GameLogic.Data
{
    /// <summary>CharLook's looping body frame interpolation for delays at least one 8 ms tick.</summary>
    public static class SourceStanceTiming
    {
        public static int SampleLoop(long ticks, float interpolation, int[] delays)
        {
            if (delays == null || delays.Length == 0) return 0;
            int duration = 0;
            foreach (int delay in delays)
            {
                if (delay < 8) throw new ArgumentException("Body frame delay is shorter than a source tick.", nameof(delays));
                duration += delay;
            }
            // CharLook uses remaining-delay / timestep as its frame threshold,
            // unlike Graphics/Animation's opposite threshold. The authored fly
            // frames form a continuous timeline between completed source ticks.
            double time = ticks > 0 ? (ticks - 1 + (double)Math.Max(0, Math.Min(1, interpolation))) * 8 : 0;
            time %= duration;
            for (int i = 0; i < delays.Length; i++)
            {
                if (time < delays[i]) return i;
                time -= delays[i];
            }
            return 0;
        }
    }
}
