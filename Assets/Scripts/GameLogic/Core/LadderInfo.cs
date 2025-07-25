namespace MapleClient.GameLogic.Core
{
    public class LadderInfo
    {
        public float X { get; set; }
        public float Y1 { get; set; } // Bottom
        public float Y2 { get; set; } // Top
        
        public bool ContainsPosition(Vector2 position, float tolerance = 10f)
        {
            return System.Math.Abs(position.X - X) <= tolerance &&
                   position.Y >= Y1 && position.Y <= Y2;
        }
    }
}