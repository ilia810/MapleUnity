using System.Collections.Generic;

namespace MapleClient.GameLogic
{
    /// <summary>
    /// Service for querying foothold (ground) information in MapleStory maps.
    /// All coordinates are in MapleStory coordinate system where:
    /// - X increases from left to right
    /// - Y increases from top to bottom (Y=0 is at the top, positive Y is downward)
    /// - All values are in MapleStory pixels (not Unity units)
    /// </summary>
    public interface IFootholdService
    {
        /// <summary>
        /// Gets the ground Y position below a given point.
        /// Searches downward from the given position to find the nearest foothold.
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels to search from</param>
        /// <returns>Y coordinate of the ground below in MapleStory pixels, or float.MaxValue if no ground found</returns>
        float GetGroundBelow(float x, float y);
        
        /// <summary>
        /// Checks if a position is on solid ground within the specified tolerance.
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels</param>
        /// <param name="tolerance">Vertical tolerance in MapleStory pixels (default: 1 pixel)</param>
        /// <returns>True if the position is on a foothold within tolerance</returns>
        bool IsOnGround(float x, float y, float tolerance = 1f);
        
        /// <summary>
        /// Gets the foothold at the specified position.
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels</param>
        /// <returns>The foothold at the position, or null if none found</returns>
        Foothold GetFootholdAt(float x, float y);
        
        /// <summary>
        /// Gets the foothold below a given point (for landing calculations).
        /// Unlike GetFootholdAt, this searches downward to find the nearest foothold.
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels to search from</param>
        /// <returns>The nearest foothold below the position, or null if none found</returns>
        Foothold GetFootholdBelow(float x, float y);
        
        /// <summary>
        /// Gets all footholds in a specified area.
        /// Useful for physics queries and collision detection.
        /// </summary>
        /// <param name="minX">Minimum X coordinate in MapleStory pixels</param>
        /// <param name="minY">Minimum Y coordinate in MapleStory pixels</param>
        /// <param name="maxX">Maximum X coordinate in MapleStory pixels</param>
        /// <param name="maxY">Maximum Y coordinate in MapleStory pixels</param>
        /// <returns>List of footholds in the specified area</returns>
        IEnumerable<Foothold> GetFootholdsInArea(float minX, float minY, float maxX, float maxY);
        
        /// <summary>
        /// Gets the connected foothold in the specified direction.
        /// Used for character movement along connected platforms.
        /// </summary>
        /// <param name="currentFoothold">The foothold the character is currently on</param>
        /// <param name="movingRight">True if moving right, false if moving left</param>
        /// <returns>The connected foothold, or null if none exists</returns>
        Foothold GetConnectedFoothold(Foothold currentFoothold, bool movingRight);
        
        /// <summary>
        /// Finds the nearest foothold to a given position.
        /// Useful for spawning or teleporting characters.
        /// </summary>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <param name="y">Y coordinate in MapleStory pixels</param>
        /// <param name="maxDistance">Maximum search distance in MapleStory pixels</param>
        /// <returns>The nearest foothold, or null if none found within maxDistance</returns>
        Foothold FindNearestFoothold(float x, float y, float maxDistance = 1000f);
        
        /// <summary>
        /// Checks if a foothold is a wall (vertical platform).
        /// </summary>
        /// <param name="foothold">The foothold to check</param>
        /// <returns>True if the foothold is a wall</returns>
        bool IsWall(Foothold foothold);
        
        /// <summary>
        /// Gets the slope at a specific X position on a foothold.
        /// Used for adjusting character animations and physics.
        /// </summary>
        /// <param name="foothold">The foothold to check</param>
        /// <param name="x">X coordinate in MapleStory pixels</param>
        /// <returns>Slope angle in radians, or 0 if X is outside foothold bounds</returns>
        float GetSlopeAt(Foothold foothold, float x);
        
        /// <summary>
        /// Loads foothold data into the service.
        /// This replaces any existing foothold data.
        /// </summary>
        /// <param name="footholds">List of footholds to load</param>
        void LoadFootholds(List<Foothold> footholds);
        
        /// <summary>
        /// Updates or adds a single foothold.
        /// </summary>
        /// <param name="foothold">The foothold to update or add</param>
        void UpdateFoothold(Foothold foothold);
    }
}