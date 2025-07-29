namespace MapleClient.GameLogic
{
    /// <summary>
    /// Static utility class for converting between Unity and MapleStory coordinate systems.
    /// 
    /// Coordinate System Differences:
    /// - Unity: Y axis points upward (Y=0 at ground, positive Y is up)
    /// - MapleStory: Y axis points downward (Y=0 at top, positive Y is down)
    /// - Scale: 1 Unity unit = 100 MapleStory pixels
    /// 
    /// Conversion Formulas:
    /// - MapleX = UnityX * 100
    /// - MapleY = -UnityY * 100 (negative because Y axes are inverted)
    /// - UnityX = MapleX / 100
    /// - UnityY = -MapleY / 100 (negative because Y axes are inverted)
    /// </summary>
    public static class MaplePhysicsConverter
    {
        /// <summary>
        /// Conversion factor between Unity units and MapleStory pixels
        /// </summary>
        private const float PIXELS_PER_UNIT = 100f;
        
        /// <summary>
        /// Converts a position from Unity coordinates to MapleStory coordinates.
        /// </summary>
        /// <param name="unityPosition">Position in Unity world space</param>
        /// <returns>Position in MapleStory pixel coordinates</returns>
        public static Vector2 UnityToMaple(Vector2 unityPosition)
        {
            return new Vector2(
                unityPosition.X * PIXELS_PER_UNIT,
                -unityPosition.Y * PIXELS_PER_UNIT
            );
        }
        
        /// <summary>
        /// Converts a position from MapleStory coordinates to Unity coordinates.
        /// </summary>
        /// <param name="maplePosition">Position in MapleStory pixel coordinates</param>
        /// <returns>Position in Unity world space</returns>
        public static Vector2 MapleToUnity(Vector2 maplePosition)
        {
            return new Vector2(
                maplePosition.X / PIXELS_PER_UNIT,
                -maplePosition.Y / PIXELS_PER_UNIT
            );
        }
        
        /// <summary>
        /// Converts X coordinate from Unity to MapleStory.
        /// </summary>
        /// <param name="unityX">X coordinate in Unity units</param>
        /// <returns>X coordinate in MapleStory pixels</returns>
        public static float UnityToMapleX(float unityX)
        {
            return unityX * PIXELS_PER_UNIT;
        }
        
        /// <summary>
        /// Converts Y coordinate from Unity to MapleStory.
        /// Note: Y axis is inverted between the two systems.
        /// </summary>
        /// <param name="unityY">Y coordinate in Unity units</param>
        /// <returns>Y coordinate in MapleStory pixels</returns>
        public static float UnityToMapleY(float unityY)
        {
            return -unityY * PIXELS_PER_UNIT;
        }
        
        /// <summary>
        /// Converts X coordinate from MapleStory to Unity.
        /// </summary>
        /// <param name="mapleX">X coordinate in MapleStory pixels</param>
        /// <returns>X coordinate in Unity units</returns>
        public static float MapleToUnityX(float mapleX)
        {
            return mapleX / PIXELS_PER_UNIT;
        }
        
        /// <summary>
        /// Converts Y coordinate from MapleStory to Unity.
        /// Note: Y axis is inverted between the two systems.
        /// </summary>
        /// <param name="mapleY">Y coordinate in MapleStory pixels</param>
        /// <returns>Y coordinate in Unity units</returns>
        public static float MapleToUnityY(float mapleY)
        {
            return -mapleY / PIXELS_PER_UNIT;
        }
        
        /// <summary>
        /// Converts a velocity vector from Unity to MapleStory.
        /// </summary>
        /// <param name="unityVelocity">Velocity in Unity units per second</param>
        /// <returns>Velocity in MapleStory pixels per second</returns>
        public static Vector2 UnityVelocityToMaple(Vector2 unityVelocity)
        {
            // Same conversion as position since velocity is distance/time
            return UnityToMaple(unityVelocity);
        }
        
        /// <summary>
        /// Converts a velocity vector from MapleStory to Unity.
        /// </summary>
        /// <param name="mapleVelocity">Velocity in MapleStory pixels per second</param>
        /// <returns>Velocity in Unity units per second</returns>
        public static Vector2 MapleVelocityToUnity(Vector2 mapleVelocity)
        {
            // Same conversion as position since velocity is distance/time
            return MapleToUnity(mapleVelocity);
        }
        
        /// <summary>
        /// Converts a distance/size from Unity units to MapleStory pixels.
        /// </summary>
        /// <param name="unityDistance">Distance in Unity units</param>
        /// <returns>Distance in MapleStory pixels</returns>
        public static float UnityToMapleDistance(float unityDistance)
        {
            return unityDistance * PIXELS_PER_UNIT;
        }
        
        /// <summary>
        /// Converts a distance/size from MapleStory pixels to Unity units.
        /// </summary>
        /// <param name="mapleDistance">Distance in MapleStory pixels</param>
        /// <returns>Distance in Unity units</returns>
        public static float MapleToUnityDistance(float mapleDistance)
        {
            return mapleDistance / PIXELS_PER_UNIT;
        }
    }
}