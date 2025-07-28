using MapleClient.GameLogic;

namespace MapleClient.GameLogic
{
    public static class MapleCoordinateConverter
    {
        /// <summary>
        /// Convert MapleStory coordinates to Unity coordinates
        /// MapleStory: Y=0 is ground, negative Y is above ground
        /// Unity: Y=0 is ground, positive Y is above ground
        /// </summary>
        public static Vector2 MapleToUnity(float mapleX, float mapleY)
        {
            // Convert pixels to units and invert Y
            return new Vector2(mapleX / 100f, -mapleY / 100f);
        }
        
        /// <summary>
        /// Convert Unity coordinates to MapleStory coordinates
        /// </summary>
        public static Vector2 UnityToMaple(float unityX, float unityY)
        {
            // Convert units to pixels and invert Y
            return new Vector2(unityX * 100f, -unityY * 100f);
        }
        
        /// <summary>
        /// Convert MapleStory pixel Y to Unity Y
        /// </summary>
        public static float MapleYToUnityY(float mapleY)
        {
            return -mapleY / 100f;
        }
    }
}