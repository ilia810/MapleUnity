// Afterimage bounds/frames follow HeavenClient Afterimage.cpp and Graphics/Animation.cpp.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton. AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic.Data
{
    public struct AttackBounds
    {
        // Feet-relative source pixels, X right and Y down. Authored attacks face left.
        public int Left, Top, Right, Bottom;
        public AttackBounds(int left, int top, int right, int bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
        private sealed class ScaledCoordinate
        {
            // Store the source float product before truncating. Mono's extended
            // intermediates otherwise shorten -400 * 3.8f to -1519 pixels.
            public readonly float Value;
            public ScaledCoordinate(int coordinate, float scale) { Value = coordinate * scale; }
        }
        public AttackBounds ScaleForward(float scale) => new AttackBounds((short)new ScaledCoordinate(Left, scale).Value, Top, Right, Bottom);
        public AttackBounds At(int x, int y, bool facingRight) => facingRight
            ? new AttackBounds(x - Right, y + Top, x - Left, y + Bottom)
            : new AttackBounds(x + Left, y + Top, x + Right, y + Bottom);
        public bool Overlaps(AttackBounds other) => Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;
    }

    public sealed class AfterimageFrame
    {
        public string Path { get; set; }
        public int DelayMilliseconds { get; set; } = 100;
        public float StartAlpha { get; set; } = 1;
        public float EndAlpha { get; set; } = 1;
        public float StartScale { get; set; } = 1;
        public float EndScale { get; set; } = 1;
    }

    public sealed class WeaponAfterimage
    {
        public string Path { get; set; }
        public int FirstFrame { get; set; }
        public bool HasBounds { get; set; }
        public AttackBounds Bounds { get; set; }
        public AfterimageFrame[] Frames { get; set; } = Array.Empty<AfterimageFrame>();
        public bool Zigzag { get; set; }

        // One complete Animation cycle, including the returning frame 0 on a zigzag.
        // A completed afterimage stays hidden until another swing starts.
        public int Sample(int milliseconds, out float fraction)
        {
            int count = Frames.Length + (Zigzag && Frames.Length > 1 ? Frames.Length - 1 : 0);
            for (int i = 0; i < count; i++)
            {
                int index = i < Frames.Length ? i : 2 * Frames.Length - 2 - i;
                int delay = Math.Max(1, Frames[index].DelayMilliseconds);
                if (milliseconds < delay) { fraction = Math.Max(0, milliseconds) / (float)delay; return index; }
                milliseconds -= delay;
            }
            fraction = 1;
            return -1;
        }
    }
}
