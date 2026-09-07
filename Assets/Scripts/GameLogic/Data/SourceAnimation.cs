using System;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Data
{
    public sealed class SourceAnimationFrame
    {
        public int DelayMilliseconds = 100;
        public float StartOpacity = 255, EndOpacity = 255;
        public float StartScale = 100, EndScale = 100;
    }

    /// <summary>HeavenClient Animation::update and its Nominal/Linear draw interpolation.</summary>
    public sealed class SourceAnimation
    {
        private readonly IReadOnlyList<SourceAnimationFrame> frames;
        private readonly bool zigzag;
        private int frame, previousFrame, direction, remaining;
        private float threshold, opacity, previousOpacity, scale, previousScale;
        public long Ticks { get; private set; }

        public SourceAnimation(IReadOnlyList<SourceAnimationFrame> frames, bool zigzag)
        {
            if (frames == null || frames.Count == 0) throw new ArgumentException("Animation needs a frame.", nameof(frames));
            this.frames = frames;
            this.zigzag = zigzag;
            Reset();
        }

        public void Reset()
        {
            Ticks = 0;
            frame = previousFrame = 0;
            direction = 1;
            threshold = 0;
            remaining = Delay(frames[0]);
            opacity = previousOpacity = frames[0].StartOpacity;
            scale = previousScale = frames[0].StartScale;
        }

        public void AdvanceTo(long ticks)
        {
            if (ticks < Ticks) Reset();
            while (Ticks < ticks) Advance();
        }

        public void Advance()
        {
            const int step = 8;
            var current = frames[frame];
            previousOpacity = opacity;
            opacity += step * (current.EndOpacity - current.StartOpacity) / Delay(current);
            previousScale = scale;
            scale += step * (current.EndScale - current.StartScale) / Delay(current);
            // Source checks the previous Linear value, including its scale/opacity typo.
            if (previousOpacity < 0) opacity = previousOpacity = 0;
            else if (previousOpacity > 255) opacity = previousOpacity = 255;
            if (previousScale < 0) opacity = previousOpacity = 0;
            previousFrame = frame;
            if (step >= remaining)
            {
                int next = frame;
                if (frames.Count > 1)
                {
                    if (zigzag)
                    {
                        if (direction > 0 && frame == frames.Count - 1) direction = -1;
                        else if (direction < 0 && frame == 0) direction = 1;
                        next += direction;
                    }
                    else next = (frame + 1) % frames.Count;
                }
                int delta = step - remaining;
                threshold = (float)delta / step;
                frame = next;
                remaining = Delay(frames[frame]);
                if (remaining >= delta) remaining -= delta;
                opacity = previousOpacity = frames[frame].StartOpacity;
                scale = previousScale = frames[frame].StartScale;
            }
            else remaining -= step;
            Ticks++;
        }

        public int Sample(float interpolation, out float alpha, out float size)
        {
            float t = Math.Max(0, Math.Min(1, interpolation));
            alpha = ((1 - t) * previousOpacity + t * opacity) / 255f;
            size = ((1 - t) * previousScale + t * scale) / 100f;
            return t >= threshold ? frame : previousFrame;
        }

        private static int Delay(SourceAnimationFrame frame) => frame.DelayMilliseconds > 0 ? frame.DelayMilliseconds : 100;
    }
}
