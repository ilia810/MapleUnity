using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Core
{
    /// <summary>
    /// Handles player spawning logic for MapleStory maps
    /// </summary>
    public class PlayerSpawnManager
    {
        private const float SPAWN_HEIGHT_OFFSET = 0.1f; // 10 pixels above spawn point
        private const float PLAYER_HEIGHT = 0.6f; // 60 pixels
        
        /// <summary>
        /// Find the best spawn point for a player in the given map
        /// </summary>
        public Vector2 FindSpawnPoint(MapData mapData, int portalId = -1)
        {
            // CUSTOM SPAWN POSITION - Always spawn at (-4.4, -0.8)
            return new Vector2(-4.4f, 0.8f); // Fixed: positive Y for above ground
            
            // Original code commented out:
            /*
            // First, try to find a specific portal if ID is provided
            if (portalId >= 0 && mapData.Portals != null)
            {
                var portal = mapData.Portals.FirstOrDefault(p => p.Id == portalId);
                if (portal != null)
                {
                    return GetSpawnPositionFromPortal(portal);
                }
            }
            
            // Next, try to find a spawn portal (type 0)
            if (mapData.Portals != null)
            {
                var spawnPortal = mapData.Portals.FirstOrDefault(p => p.Type == 0);
                if (spawnPortal != null)
                {
                    return GetSpawnPositionFromPortal(spawnPortal);
                }
            }
            
            // If no spawn portal, find a suitable platform near the center of the map
            if (mapData.Platforms != null && mapData.Platforms.Count > 0)
            {
                return FindPlatformSpawnPoint(mapData);
            }
            
            // Last resort: spawn at map center at a reasonable height
            float centerX = mapData.Width / 2f;
            float centerY = mapData.Height / 2f;
            return new Vector2(centerX / 100f, centerY / 100f);
            */
        }
        
        /// <summary>
        /// Get spawn position from a portal, with height offset
        /// </summary>
        private Vector2 GetSpawnPositionFromPortal(Portal portal)
        {
            // Convert from pixels to units and add height offset
            float x = portal.X / 100f;
            float y = -portal.Y / 100f + SPAWN_HEIGHT_OFFSET; // Invert Y axis
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Find a suitable platform to spawn on near the center of the map
        /// </summary>
        private Vector2 FindPlatformSpawnPoint(MapData mapData)
        {
            // Get map center
            float centerX = mapData.Width / 2f;
            float centerY = mapData.Height / 2f;
            
            // Find platforms that could be spawn points
            var candidatePlatforms = mapData.Platforms
                .Where(p => p.Type == PlatformType.Normal || p.Type == PlatformType.OneWay)
                .Select(p => new
                {
                    Platform = p,
                    CenterX = (p.X1 + p.X2) / 2,
                    CenterY = (p.Y1 + p.Y2) / 2,
                    Length = System.Math.Abs(p.X2 - p.X1)
                })
                .Where(p => p.Length > 200) // At least 200 pixels wide
                .OrderBy(p => System.Math.Abs(p.CenterX - centerX) + System.Math.Abs(p.CenterY - centerY))
                .ToList();
            
            if (candidatePlatforms.Count > 0)
            {
                var chosen = candidatePlatforms.First();
                float spawnX = chosen.CenterX / 100f;
                float spawnY = chosen.Platform.GetYAtX(chosen.CenterX) / 100f + PLAYER_HEIGHT / 2 + SPAWN_HEIGHT_OFFSET;
                return new Vector2(spawnX, spawnY);
            }
            
            // No suitable platforms found, use map center
            return new Vector2(centerX / 100f, centerY / 100f);
        }
        
        /// <summary>
        /// Spawn a player at the given position
        /// </summary>
        public void SpawnPlayer(Player player, Vector2 position)
        {
            player.Position = position;
            player.Velocity = Vector2.Zero;
            player.IsGrounded = false; // Let gravity pull them down to the platform
            player.IsJumping = false;
            // State will be set to Standing automatically by the Player class
        }
        
        /// <summary>
        /// Validate if a spawn point is safe and accessible
        /// </summary>
        public bool IsValidSpawnPoint(Vector2 position, MapData mapData)
        {
            // Check if position is within map bounds
            float x = position.X * 100f;
            float y = position.Y * 100f;
            
            if (x < 0 || x > mapData.Width ||
                y < 0 || y > mapData.Height)
            {
                return false;
            }
            
            // Check if there's a platform below this position
            var playerBottom = position.Y - PLAYER_HEIGHT / 2;
            var platformBelow = mapData.Platforms
                ?.Where(p => p.Type == PlatformType.Normal || p.Type == PlatformType.OneWay)
                .Where(p => x >= p.X1 && x <= p.X2)
                .Where(p => {
                    var platY = p.GetYAtX(x);
                    return !float.IsNaN(platY) && platY / 100f <= playerBottom + 2f; // Within 200 pixels below
                })
                .Any() ?? false;
            
            return platformBelow;
        }
    }
}