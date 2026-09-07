// Bounds and timing ported from HeavenClient Graphics/Animation and Template/Rectangle.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic
{
    public struct MobContactFrame
    {
        // Relative to feet, in source pixels (Y down). Collision bounds are not mirrored in Mob::is_in_range.
        public int Left, Top, Right, Bottom, DelayMilliseconds;
        public int HeadX, HeadY;
    }

    public sealed class MobContactAnimation
    {
        public MobContactFrame[] Frames { get; }
        public bool Zigzag { get; }
        public MobContactAnimation(MobContactFrame[] frames, bool zigzag)
        {
            Frames = frames; Zigzag = zigzag;
        }

        public MobContactFrame Sample(long milliseconds)
        {
            if (Frames.Length == 0) return default;
            int count = Frames.Length + (Zigzag ? Math.Max(0, Frames.Length - 2) : 0);
            long duration = 0;
            for (int i = 0; i < count; i++) duration += Math.Max(1, Frames[Index(i)].DelayMilliseconds);
            milliseconds = Math.Max(0, milliseconds) % duration;
            for (int i = 0; i < count; i++)
            {
                var frame = Frames[Index(i)];
                if (milliseconds < Math.Max(1, frame.DelayMilliseconds)) return frame;
                milliseconds -= Math.Max(1, frame.DelayMilliseconds);
            }
            return Frames[0];
        }

        private int Index(int i) => i < Frames.Length ? i : 2 * Frames.Length - 2 - i;
    }
}
