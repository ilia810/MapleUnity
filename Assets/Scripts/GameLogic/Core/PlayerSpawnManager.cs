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
        private readonly IFootholdService footholdService;
        private const float SPAWN_HEIGHT_OFFSET = 0.1f; // 10 pixels above spawn point
        private const float PLAYER_HEIGHT = 0.6f; // 60 pixels
        // Force recompile: spawn fix v2
        
        public PlayerSpawnManager(IFootholdService footholdService)
        {
            this.footholdService = footholdService;
        }
        
        /// <summary>
        /// Find the best spawn point for a player in the given map
        /// </summary>
        public Vector2 FindSpawnPoint(MapData mapData, int portalId = -1)
        {
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] FindSpawnPoint called with portalId={portalId}");
            // First, try to find a specific portal if ID is provided
            if (portalId >= 0 && mapData.Portals != null)
            {
                var portal = mapData.Portals.FirstOrDefault(p => p.Id == portalId);
                if (portal != null)
                {
                    return GetSpawnPositionFromPortal(portal);
                }
            }
            
            // Next, try to find a spawn portal
            if (mapData.Portals != null)
            {
                System.Console.WriteLine($"[FOOTHOLD_COLLISION] Checking {mapData.Portals.Count} portals for spawn portal");
                var spawnPortal = mapData.Portals.FirstOrDefault(p => p.Type == PortalType.Spawn);
                if (spawnPortal != null)
                {
                    System.Console.WriteLine($"[FOOTHOLD_COLLISION] Found spawn portal at ({spawnPortal.X}, {spawnPortal.Y})");
                    return GetSpawnPositionFromPortal(spawnPortal);
                }
                else
                {
                    System.Console.WriteLine($"[FOOTHOLD_COLLISION] No spawn portal found among {mapData.Portals.Count} portals");
                }
            }
            
            // If no spawn portal, use map center
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] No spawn portal found, calling FindPlatformSpawnPoint");
            return FindPlatformSpawnPoint(mapData);
        }
        
        /// <summary>
        /// Get spawn position from a portal, placed on ground
        /// </summary>
        private Vector2 GetSpawnPositionFromPortal(Portal portal)
        {
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] GetSpawnPositionFromPortal: portal at ({portal.X}, {portal.Y})");
            
            // Find ground below portal position
            float groundY = footholdService.GetGroundBelow(portal.X, portal.Y);
            
            // If no ground found, use portal Y
            if (groundY == float.MaxValue)
            {
                groundY = portal.Y;
            }
            
            // GetGroundBelow returns ground-1, so actual ground is groundY+1
            float actualGroundY = groundY + 1;
            
            // Spawn player just above the ground
            float spawnY = actualGroundY - 50; // 50 pixels above ground (about player height)
            
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] Portal spawn: ground at Y={actualGroundY}, spawning at Y={spawnY}");
            
            // Convert to Unity coordinates
            return MapleCoordinateConverter.MapleToUnity(portal.X, spawnY);
        }
        
        /// <summary>
        /// Find a suitable spawn point near the center of the map
        /// </summary>
        private Vector2 FindPlatformSpawnPoint(MapData mapData)
        {
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] ===== FindPlatformSpawnPoint START =====");
            // Get map center in MapleStory coordinates
            float centerX = mapData.Width / 2f;
            float centerY = mapData.Height / 2f;
            
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] Map center: ({centerX}, {centerY}), checking for ground...");
            
            // First try to find ground from center Y
            float groundY = footholdService.GetGroundBelow(centerX, centerY);
            
            // If no ground found from center, try from top of map
            if (groundY == float.MaxValue)
            {
                groundY = footholdService.GetGroundBelow(centerX, 0);
            }
            
            // If still no ground found, use map center as fallback
            if (groundY == float.MaxValue)
            {
                groundY = centerY;
            }
            
            // GetGroundBelow returns ground-1 (e.g., 199 when ground is at 200)
            float actualGroundY = groundY + 1; // Add 1 to get actual ground position
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] FindPlatformSpawnPoint: ground from GetGroundBelow = {groundY}");
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] FindPlatformSpawnPoint: actual ground = {actualGroundY}");
            
            // Spawn player just above the ground
            // In MapleStory coords, smaller Y = higher position
            float spawnY = actualGroundY - 50; // Spawn 50 pixels above the ground (about player height)
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] FindPlatformSpawnPoint: spawn Y = {spawnY} (50 above ground)");
            
            // Convert to Unity coordinates
            var spawnPos = MapleCoordinateConverter.MapleToUnity(centerX, spawnY);
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] Spawn position: Maple({centerX}, {spawnY}) -> Unity({spawnPos.X:F2}, {spawnPos.Y:F2})");
            
            return spawnPos;
        }
        
        /// <summary>
        /// Spawn a player at the given position
        /// </summary>
        public void SpawnPlayer(Player player, Vector2 position)
        {
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] SpawnPlayer called with position: Unity({position.X:F2}, {position.Y:F2})");
            player.Position = position;
            player.Velocity = Vector2.Zero;
            player.IsGrounded = false; // Let gravity pull them down to the platform
            player.IsJumping = false;
            // State will be set to Standing automatically by the Player class
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] SpawnPlayer set player.Position to: ({player.Position.X:F2}, {player.Position.Y:F2})");
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