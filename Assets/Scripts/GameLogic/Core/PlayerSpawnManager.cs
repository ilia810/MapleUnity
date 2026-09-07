using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameLogic.Core
{
    /// <summary>Resolve portal feet onto real footholds before notifying map observers.</summary>
    public class PlayerSpawnManager
    {
        private readonly IFootholdService footholdService;
        public PlayerSpawnManager(IFootholdService footholdService) { this.footholdService = footholdService; }

        public Vector2 FindSpawnPoint(MapData mapData, int portalId = -1)
        {
            if (TryFindSafeSpawn(mapData, out var position, portalId)) return position;
            var portal = SelectPortal(mapData, portalId);
            // No terrain is available. Preserve the requested point without claiming contact.
            return portal != null ? new Vector2(portal.X / 100f, -portal.Y / 100f + Player.Height / 2) : Vector2.Zero;
        }

        public bool TryFindSafeSpawn(MapData mapData, out Vector2 position, int portalId = -1)
        {
            position = Vector2.Zero;
            if (mapData == null) return false;
            var terrain = GetTerrain(mapData);
            if (!terrain.HasGround) return false;
            var portal = SelectPortal(mapData, portalId);
            double x = portal != null ? portal.X : (terrain.LeftWall + terrain.RightWall) / 2;
            double y = portal != null ? portal.Y : terrain.Top;
            return terrain.TryFindSpawn(x, y, out position);
        }

        private static Portal SelectPortal(MapData mapData, int portalId)
        {
            var portals = mapData?.Portals;
            return portals?.FirstOrDefault(p => p.Id == portalId && portalId >= 0) ??
                portals?.FirstOrDefault(p => p.Type == PortalType.Spawn);
        }

        private NormalTerrain GetTerrain(MapData mapData)
        {
            var footholds = footholdService?.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToList();
            if (footholds == null || footholds.Count == 0)
                footholds = mapData?.Platforms?.Where(p => p.Type != PlatformType.Ladder && p.Type != PlatformType.Rope)
                    .Select(p => new Foothold(p.Id,p.X1,p.Y1,p.X2,p.Y2) { Layer=p.Layer }).ToList() ?? new List<Foothold>();
            return new NormalTerrain(footholds);
        }

        public void SpawnPlayer(Player player, Vector2 position)
        {
            player.Position = position;
            player.Velocity = Vector2.Zero;
            player.IsGrounded = false;
            player.ResetMovementForMap();
        }

        public bool IsValidSpawnPoint(Vector2 position, MapData mapData)
        {
            if (mapData == null || float.IsNaN(position.X) || float.IsNaN(position.Y) ||
                float.IsInfinity(position.X) || float.IsInfinity(position.Y)) return false;
            var terrain = GetTerrain(mapData);
            double x = position.X * 100.0, y = -(position.Y - Player.Height / 2) * 100.0;
            var floor = terrain.Get(terrain.Below(x, y));
            return floor != null && x >= terrain.LeftWall && x <= terrain.RightWall &&
                NormalTerrain.Ground(floor, x) - y <= 200;
        }
    }
}
