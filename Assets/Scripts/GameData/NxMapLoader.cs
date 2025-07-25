using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameData
{
    public class NxMapLoader : IMapLoader
    {
        private readonly INxFile mapNx;
        private readonly INxFile stringNx;

        public NxMapLoader(string dataPath = "")
        {
            try
            {
                if (string.IsNullOrEmpty(dataPath))
                {
                    // Use mock data for testing
                    mapNx = new MockNxFile();
                    stringNx = new MockNxFile();
                }
                else
                {
                    mapNx = new NxFile(System.IO.Path.Combine(dataPath, "Map.nx"));
                    stringNx = new NxFile(System.IO.Path.Combine(dataPath, "String.nx"));
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // If NX files not found, create mock loader
                mapNx = new MockNxFile();
                stringNx = new MockNxFile();
            }
        }

        public MapData GetMap(int mapId)
        {
            var mapIdStr = mapId.ToString("D9");
            var mapCategory = mapIdStr.Substring(0, 1);
            
            // Try to find map node
            var mapPath = $"Map/Map{mapCategory}/{mapIdStr}.img";
            var mapNode = mapNx.GetNode(mapPath);
            
            if (mapNode == null)
            {
                // Try without category for mock data
                mapNode = mapNx.GetNode($"Map/Map0/{mapIdStr}.img");
                if (mapNode == null)
                {
                    return null;
                }
            }

            var mapData = new MapData
            {
                MapId = mapId,
                Name = GetMapName(mapId),
                Width = 2000, // Default values for now
                Height = 1000
            };

            // Load platforms/footholds
            LoadPlatforms(mapNode, mapData);
            
            // Load portals
            LoadPortals(mapNode, mapData);
            
            // Load life (NPCs and monster spawns)
            LoadLife(mapNode, mapData);

            // Load BGM
            var infoNode = mapNode["info"];
            if (infoNode != null)
            {
                mapData.BgmId = infoNode["bgm"]?.GetValue<string>() ?? "";
            }

            return mapData;
        }

        private string GetMapName(int mapId)
        {
            // For testing with mock data
            if (mapId == 100000000)
                return "Henesys";

            var streetNode = stringNx.GetNode($"Map.img/streetName/{mapId}");
            if (streetNode != null)
            {
                return streetNode["streetName"]?.GetValue<string>() ?? $"Map {mapId}";
            }

            var mapNode = stringNx.GetNode($"Map.img/mapName/{mapId}");
            if (mapNode != null)
            {
                return mapNode["mapName"]?.GetValue<string>() ?? $"Map {mapId}";
            }

            return $"Map {mapId}";
        }

        private void LoadPlatforms(INxNode mapNode, MapData mapData)
        {
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null)
                return;

            int platformId = 1;
            foreach (var layerNode in footholdNode.Children)
            {
                foreach (var groupNode in layerNode.Children)
                {
                    foreach (var fhNode in groupNode.Children)
                    {
                        var x1 = fhNode["x1"]?.GetValue<int>() ?? 0;
                        var y1 = fhNode["y1"]?.GetValue<int>() ?? 0;
                        var x2 = fhNode["x2"]?.GetValue<int>() ?? 0;
                        var y2 = fhNode["y2"]?.GetValue<int>() ?? 0;

                        mapData.Platforms.Add(new Platform
                        {
                            Id = platformId++,
                            X1 = x1,
                            Y1 = y1,
                            X2 = x2,
                            Y2 = y2,
                            Type = PlatformType.Normal
                        });
                    }
                }
            }
        }

        private void LoadPortals(INxNode mapNode, MapData mapData)
        {
            var portalNode = mapNode["portal"];
            if (portalNode == null)
                return;

            foreach (var pNode in portalNode.Children)
            {
                var portal = new Portal
                {
                    Id = pNode["id"]?.GetValue<int>() ?? 0,
                    Name = pNode["pn"]?.GetValue<string>() ?? "",
                    X = pNode["x"]?.GetValue<int>() ?? 0,
                    Y = pNode["y"]?.GetValue<int>() ?? 0,
                    TargetMapId = pNode["tm"]?.GetValue<int>() ?? 0,
                    TargetPortalName = pNode["tn"]?.GetValue<string>() ?? "",
                    Type = GetPortalType(pNode["pt"]?.GetValue<int>() ?? 0)
                };

                mapData.Portals.Add(portal);
            }
        }

        private void LoadLife(INxNode mapNode, MapData mapData)
        {
            var lifeNode = mapNode["life"];
            if (lifeNode == null)
                return;

            foreach (var life in lifeNode.Children)
            {
                var type = life["type"]?.GetValue<string>();
                var id = life["id"]?.GetValue<string>() ?? "";
                var x = life["x"]?.GetValue<int>() ?? 0;
                var y = life["y"]?.GetValue<int>() ?? 0;

                if (type == "m" && int.TryParse(id, out var mobId))
                {
                    // Monster spawn
                    mapData.MonsterSpawns.Add(new MonsterSpawn
                    {
                        MonsterId = mobId,
                        X = x,
                        Y = y,
                        SpawnInterval = life["mobTime"]?.GetValue<int>() ?? 30,
                        MaxCount = 1
                    });
                }
                else if (type == "n" && int.TryParse(id, out var npcId))
                {
                    // NPC spawn
                    mapData.NpcSpawns.Add(new NpcSpawn
                    {
                        NpcId = npcId,
                        X = x,
                        Y = y,
                        FlipX = life["f"]?.GetValue<int>() == 1
                    });
                }
            }
        }

        private PortalType GetPortalType(int type)
        {
            return type switch
            {
                0 => PortalType.Spawn,
                1 => PortalType.Normal,
                2 => PortalType.Hidden,
                3 => PortalType.Script,
                _ => PortalType.Normal
            };
        }
    }
}