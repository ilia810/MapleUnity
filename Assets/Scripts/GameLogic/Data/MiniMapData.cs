using System;

namespace MapleClient.GameLogic
{
    /// <summary>Original map pixels use downward Y; simulation units use upward Y and 100 px/unit.</summary>
    public sealed class MiniMapData
    {
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Magnification { get; set; }
        public bool Hidden { get; set; }
        public bool HasCanvas { get; set; }
        public string MapMark { get; set; } = "";
        public float Scale => (float)Math.Pow(2, Math.Max(0, Math.Min(16, Magnification)));
        public Vector2 ProjectSource(float x, float y) => new Vector2((x + CenterX) / Scale, (y + CenterY) / Scale);
        public Vector2 ProjectFeet(Vector2 feet) => ProjectSource(feet.X * 100, -feet.Y * 100);
    }
}
