namespace MapleClient.GameLogic
{
    public class Platform
    {
        public int Id { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public PlatformType Type { get; set; }

        public float GetYAtX(float x)
        {
            if (x < X1 || x > X2)
                return float.NaN;
            
            float t = (x - X1) / (X2 - X1);
            return Y1 + t * (Y2 - Y1);
        }
    }

    public enum PlatformType
    {
        Normal,
        OneWay,
        Ladder,
        Rope
    }
}