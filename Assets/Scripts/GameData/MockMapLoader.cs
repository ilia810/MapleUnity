using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameData
{
    public class MockMapLoader : IMapLoader
    {
        public MapData GetMap(int mapId)
        {
            // Create a simple test map
            var mapData = new MapData
            {
                MapId = mapId,
                Name = "Test Map - Henesys",
                Width = 2000,
                Height = 1000,
                BgmId = "bgm_henesys"
            };

            // Add some platforms
            mapData.Platforms.Add(new Platform 
            { 
                Id = 1, 
                X1 = -1000, Y1 = 0, 
                X2 = 1000, Y2 = 0, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 2, 
                X1 = -300, Y1 = 150, 
                X2 = -100, Y2 = 150, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 3, 
                X1 = 100, Y1 = 200, 
                X2 = 300, Y2 = 200, 
                Type = PlatformType.Normal 
            });
            
            mapData.Platforms.Add(new Platform 
            { 
                Id = 4, 
                X1 = -600, Y1 = 300, 
                X2 = -400, Y2 = 350, 
                Type = PlatformType.Normal 
            });

            // Add a portal
            mapData.Portals.Add(new Portal
            {
                Id = 1,
                Name = "spawn",
                X = 0,
                Y = 50,
                Type = PortalType.Spawn
            });

            // Add some test ladders
            mapData.Ladders.Add(new GameLogic.Core.LadderInfo 
            { 
                X = 200, 
                Y1 = 0, 
                Y2 = 200 
            });
            
            mapData.Ladders.Add(new GameLogic.Core.LadderInfo 
            { 
                X = -200, 
                Y1 = 0, 
                Y2 = 150 
            });

            // Add test portals based on map ID
            if (mapId == 100000000) // Henesys
            {
                mapData.Portals.Add(new Portal
                {
                    Id = 2,
                    Name = "toHuntingGround",
                    X = 400,
                    Y = 50,
                    Type = PortalType.Regular,
                    TargetMapId = 100000001 // Henesys Hunting Ground
                });
            }
            else if (mapId == 100000001) // Hunting Ground
            {
                mapData.Name = "Henesys Hunting Ground";
                mapData.Portals.Add(new Portal
                {
                    Id = 2,
                    Name = "toTown",
                    X = -400,
                    Y = 50,
                    Type = PortalType.Regular,
                    TargetMapId = 100000000 // Back to Henesys
                });
                
                // Different platform layout for variety
                mapData.Platforms.Clear();
                mapData.Platforms.Add(new Platform 
                { 
                    Id = 1, 
                    X1 = -800, Y1 = 0, 
                    X2 = 800, Y2 = 0, 
                    Type = PlatformType.Normal 
                });
                mapData.Platforms.Add(new Platform 
                { 
                    Id = 2, 
                    X1 = -500, Y1 = 100, 
                    X2 = -200, Y2 = 100, 
                    Type = PlatformType.Normal 
                });
                mapData.Platforms.Add(new Platform 
                { 
                    Id = 3, 
                    X1 = 200, Y1 = 100, 
                    X2 = 500, Y2 = 100, 
                    Type = PlatformType.Normal 
                });
            }

            return mapData;
        }
    }
}