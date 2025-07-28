using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>
    /// Converts between MapleStory coordinate system and Unity coordinate system
    /// MapleStory: Origin at top-left, Y increases downward (positive Y = down)
    /// Unity: Origin at center, Y increases upward (positive Y = up)
    /// </summary>
    public static class CoordinateConverter
    {
        // Conversion factor from MapleStory pixels to Unity units
        private const float PIXELS_TO_UNITS = 100f;
        
        /// <summary>
        /// Convert MapleStory Y coordinate to Unity Y coordinate
        /// In MapleStory: positive Y goes down, 0 is at top
        /// In Unity: positive Y goes up
        /// </summary>
        public static float MSToUnityY(float mapleY)
        {
            // Invert Y axis
            return -mapleY / PIXELS_TO_UNITS;
        }
        
        /// <summary>
        /// Convert Unity Y coordinate to MapleStory Y coordinate
        /// </summary>
        public static float UnityToMSY(float unityY)
        {
            // Invert Y axis and convert to pixels
            return -unityY * PIXELS_TO_UNITS;
        }
        
        /// <summary>
        /// Convert MapleStory X coordinate to Unity X coordinate
        /// X axis is the same in both systems, just needs unit conversion
        /// </summary>
        public static float MSToUnityX(float mapleX)
        {
            return mapleX / PIXELS_TO_UNITS;
        }
        
        /// <summary>
        /// Convert Unity X coordinate to MapleStory X coordinate
        /// </summary>
        public static float UnityToMSX(float unityX)
        {
            return unityX * PIXELS_TO_UNITS;
        }
        
        /// <summary>
        /// Convert MapleStory position (in pixels) to Unity position (in units)
        /// </summary>
        public static Vector3 MSToUnityPosition(float msX, float msY)
        {
            return new Vector3(MSToUnityX(msX), MSToUnityY(msY), 0);
        }
        
        /// <summary>
        /// Convert Unity position to MapleStory position
        /// </summary>
        public static Vector2 UnityToMSPosition(Vector3 unityPos)
        {
            return new Vector2(UnityToMSX(unityPos.x), UnityToMSY(unityPos.y));
        }
    }
}