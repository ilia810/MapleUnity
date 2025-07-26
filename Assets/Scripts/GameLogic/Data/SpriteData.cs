namespace MapleClient.GameLogic.Data
{
    /// <summary>
    /// Platform-independent sprite data representation
    /// </summary>
    public class SpriteData
    {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public string Name { get; set; }
        
        public SpriteData()
        {
        }
        
        public SpriteData(byte[] imageData, int width, int height)
        {
            ImageData = imageData;
            Width = width;
            Height = height;
            OriginX = width / 2;
            OriginY = height / 2;
        }
    }
}