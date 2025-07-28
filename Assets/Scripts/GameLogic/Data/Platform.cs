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
        
        // Environmental properties
        public bool IsSlippery { get; set; }
        public bool IsConveyor { get; set; }
        public float ConveyorSpeed { get; set; }

        public Platform()
        {
        }

        public Platform(float x1, float y1, float x2, float y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Type = PlatformType.Normal;
        }

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