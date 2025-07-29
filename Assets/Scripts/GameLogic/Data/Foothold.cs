namespace MapleClient.GameLogic
{
    /// <summary>
    /// Represents a foothold (ground platform) in MapleStory.
    /// Coordinates are in MapleStory coordinate system where Y increases downward.
    /// </summary>
    public class Foothold
    {
        /// <summary>
        /// Unique identifier for this foothold
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Left X coordinate in MapleStory pixels
        /// </summary>
        public float X1 { get; set; }
        
        /// <summary>
        /// Left Y coordinate in MapleStory pixels (Y increases downward)
        /// </summary>
        public float Y1 { get; set; }
        
        /// <summary>
        /// Right X coordinate in MapleStory pixels
        /// </summary>
        public float X2 { get; set; }
        
        /// <summary>
        /// Right Y coordinate in MapleStory pixels (Y increases downward)
        /// </summary>
        public float Y2 { get; set; }
        
        /// <summary>
        /// ID of the foothold to the left of this one (0 if none)
        /// </summary>
        public int PreviousId { get; set; }
        
        /// <summary>
        /// ID of the foothold to the right of this one (0 if none)
        /// </summary>
        public int NextId { get; set; }
        
        /// <summary>
        /// The layer this foothold belongs to (for vertical grouping)
        /// </summary>
        public int Layer { get; set; }
        
        /// <summary>
        /// Whether this foothold can be fallen through (like platforms)
        /// </summary>
        public bool IsWall { get; set; }
        
        /// <summary>
        /// Environmental properties
        /// </summary>
        public bool IsSlippery { get; set; }
        public bool IsConveyor { get; set; }
        public float ConveyorSpeed { get; set; }
        
        public Foothold()
        {
        }
        
        public Foothold(int id, float x1, float y1, float x2, float y2)
        {
            Id = id;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }
        
        /// <summary>
        /// Gets the Y position at the given X coordinate on this foothold.
        /// Returns the interpolated Y value in MapleStory coordinates (Y increases downward).
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <returns>Y coordinate in MapleStory pixels, or float.NaN if X is outside foothold bounds</returns>
        public float GetYAtX(float x)
        {
            // Ensure X1 <= X2 for proper bounds checking
            float minX = System.Math.Min(X1, X2);
            float maxX = System.Math.Max(X1, X2);
            
            if (x < minX || x > maxX)
                return float.NaN;
            
            // Handle vertical footholds (walls)
            if (System.Math.Abs(X2 - X1) < 0.0001f)
                return System.Math.Min(Y1, Y2); // Return the top Y
            
            // Linear interpolation
            float t = (x - X1) / (X2 - X1);
            return Y1 + t * (Y2 - Y1);
        }
        
        /// <summary>
        /// Checks if a point is on or very close to this foothold
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels</param>
        /// <param name="tolerance">Vertical tolerance in MapleStory pixels</param>
        /// <returns>True if the point is on the foothold within tolerance</returns>
        public bool ContainsPoint(float x, float y, float tolerance = 1f)
        {
            float footholdY = GetYAtX(x);
            if (float.IsNaN(footholdY))
                return false;
                
            // In MapleStory coordinates, being "on" the foothold means Y >= footholdY
            // (since Y increases downward)
            return y >= footholdY - tolerance && y <= footholdY + tolerance;
        }
        
        /// <summary>
        /// Gets the slope angle of this foothold in radians
        /// </summary>
        /// <returns>Angle in radians, positive for upward slopes (left to right)</returns>
        public float GetSlope()
        {
            if (System.Math.Abs(X2 - X1) < 0.0001f)
                return 0f; // Vertical wall, treat as flat
                
            // Note: In MapleStory coordinates, Y increases downward
            // So a negative slope in screen coordinates is an upward slope in game
            return (float)System.Math.Atan2(Y2 - Y1, X2 - X1);
        }
    }
}