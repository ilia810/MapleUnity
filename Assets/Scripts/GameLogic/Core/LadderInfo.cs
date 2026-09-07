// Ladder range rules ported from HeavenClient MapInfo.cpp.
// Copyright (C) 2015-2019 Daniel Allendorf, Ryan Payton; AGPL-3.0-or-later.
using System;

namespace MapleClient.GameLogic.Core
{
    public class LadderInfo
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y1 { get; set; } // Bottom, Unity world units
        public float Y2 { get; set; } // Top, Unity world units
        public bool IsLadder { get; set; } = true;

        // NX coordinates are integral pixels. Recover them without float drift.
        public double SourceX => RoundPixel(X * 100.0);
        public double SourceTop => RoundPixel(-Y2 * 100.0);
        public double SourceBottom => RoundPixel(-Y1 * 100.0);
        private static double RoundPixel(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

        public bool InRange(double feetX, double feetY, bool upwards)
        {
            double x = RoundPixel(feetX), y = RoundPixel(feetY) + (upwards ? -5 : 5);
            return Math.Abs(x - SourceX) <= 10 && y >= SourceTop && y <= SourceBottom;
        }

        public bool CanEnter(Vector2 playerCenter, bool upwards) =>
            InRange(playerCenter.X * 100.0, -(playerCenter.Y - Player.Height / 2) * 100.0, upwards);

        public bool FellOff(double feetY, bool downwards)
        {
            double y = RoundPixel(feetY);
            return y + (downwards ? 5 : -5) > SourceBottom || y + 5 < SourceTop;
        }

        public bool ContainsPosition(Vector2 position, float tolerance = 0.1f) =>
            Math.Abs(position.X - X) <= tolerance && position.Y >= Y1 && position.Y <= Y2;
    }
}
