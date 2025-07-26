using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Converts between MapleStory and Unity coordinate systems
    /// </summary>
    public static class CoordinateConverter
    {
        // MapleStory uses a different coordinate system:
        // - Y axis is inverted (positive Y goes down)
        // - Scale may need adjustment
        private const float COORDINATE_SCALE = 0.01f; // Adjust based on testing
        
        /// <summary>
        /// Converts MapleStory position to Unity world position
        /// </summary>
        public static Vector3 ToUnityPosition(float msX, float msY, float z = 0)
        {
            return new Vector3(
                msX * COORDINATE_SCALE,
                -msY * COORDINATE_SCALE, // Invert Y axis
                z
            );
        }
        
        /// <summary>
        /// Converts MapleStory position to Unity world position
        /// </summary>
        public static Vector3 ToUnityPosition(Vector2 msPosition, float z = 0)
        {
            return ToUnityPosition(msPosition.x, msPosition.y, z);
        }
        
        /// <summary>
        /// Converts Unity position back to MapleStory coordinates
        /// </summary>
        public static Vector2 ToMapleStoryPosition(Vector3 unityPosition)
        {
            return new Vector2(
                unityPosition.x / COORDINATE_SCALE,
                -unityPosition.y / COORDINATE_SCALE
            );
        }
        
        /// <summary>
        /// Converts MapleStory bounds to Unity bounds
        /// </summary>
        public static Bounds ToUnityBounds(float msLeft, float msRight, float msTop, float msBottom)
        {
            Vector3 min = ToUnityPosition(msLeft, msBottom);
            Vector3 max = ToUnityPosition(msRight, msTop);
            
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            
            return new Bounds(center, size);
        }
    }
}