using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameData.Adapters;
using PortalType = MapleClient.GameLogic.PortalType;
using GameData;

namespace MapleClient.GameData
{
    public class NxMapLoader : IMapLoader
    {
        private readonly INxFile mapNx;
        private readonly INxFile stringNx;
        private IFootholdService footholdService;
        private NXDataManagerSingleton nxManager;

        public NxMapLoader(string dataPath = "", IFootholdService footholdService = null)
        {
            this.footholdService = footholdService;
            
            // Try to use NXDataManagerSingleton for real NX data
            try
            {
                nxManager = NXDataManagerSingleton.Instance;
                if (nxManager != null && nxManager.DataManager != null)
                {
                    // Use real NX data from NXDataManagerSingleton
                    UnityEngine.Debug.Log("[NxMapLoader] Using real NX data from NXDataManagerSingleton");
                    mapNx = null; // We'll use nxManager instead
                    stringNx = null;
                    return;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[NxMapLoader] Could not get NXDataManagerSingleton: {e.Message}");
            }
            
            // Fallback to direct file loading or mock data
            try
            {
                if (string.IsNullOrEmpty(dataPath))
                {
                    // Use mock data for testing
                    UnityEngine.Debug.Log("[NxMapLoader] Using mock data");
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
                UnityEngine.Debug.Log("[NxMapLoader] NX files not found, using mock data");
                mapNx = new MockNxFile();
                stringNx = new MockNxFile();
            }
        }

        public MapData GetMap(int mapId)
        {
            var mapIdStr = mapId.ToString("D9");
            var mapCategory = mapIdStr.Substring(0, 1);
            
            INxNode mapNode = null;
            
            // Try to use NXDataManagerSingleton first
            if (nxManager != null)
            {
                UnityEngine.Debug.Log($"[NxMapLoader] Getting map node from NXDataManagerSingleton for map {mapId}");
                mapNode = nxManager.GetMapNode(mapId);
            }
            else if (mapNx != null)
            {
                // Fallback to direct node access
                var mapPath = $"Map/Map{mapCategory}/{mapIdStr}.img";
                UnityEngine.Debug.Log($"Looking for map at path: {mapPath}");
                mapNode = mapNx.GetNode(mapPath);
                
                if (mapNode == null)
                {
                    // Try without category for mock data
                    mapPath = $"Map/Map0/{mapIdStr}.img";
                    UnityEngine.Debug.Log($"First path failed, trying: {mapPath}");
                    mapNode = mapNx.GetNode(mapPath);
                    if (mapNode == null)
                    {
                        // Try without "Map/" prefix
                        mapPath = $"Map{mapCategory}/{mapIdStr}.img";
                        UnityEngine.Debug.Log($"Second path failed, trying: {mapPath}");
                        mapNode = mapNx.GetNode(mapPath);
                    }
                }
            }
            
            if (mapNode == null)
            {
                UnityEngine.Debug.LogError($"Failed to find map node for {mapId}");
                return null;
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

            // Update FootholdService if available
            if (footholdService != null && mapData.Platforms.Count > 0)
            {
                var footholds = FootholdDataAdapter.ConvertPlatformsToFootholds(mapData.Platforms);
                FootholdDataAdapter.BuildFootholdConnectivity(footholds);
                footholdService.LoadFootholds(footholds);
                UnityEngine.Debug.Log($"Updated FootholdService with {footholds.Count} footholds");
                UnityEngine.Debug.Log($"[FOOTHOLD_COLLISION] NxMapLoader updated FootholdService with {footholds.Count} footholds from {mapData.Platforms.Count} platforms");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[FOOTHOLD_COLLISION] NxMapLoader skipped FootholdService update - service:{footholdService != null}, platforms:{mapData.Platforms.Count}");
            }

            return mapData;
        }

        private string GetMapName(int mapId)
        {
            // For testing with mock data
            if (mapId == 100000000)
                return "Henesys";
            
            // Try to use NXDataManagerSingleton for string data
            if (nxManager != null && nxManager.DataManager != null)
            {
                try
                {
                    var stringNode = nxManager.DataManager.GetNode("String", $"Map.img/streetName/{mapId}");
                    if (stringNode != null && stringNode is INxNode nxNode)
                    {
                        return nxNode["streetName"]?.GetValue<string>() ?? $"Map {mapId}";
                    }
                    
                    var mapNameNode = nxManager.DataManager.GetNode("String", $"Map.img/mapName/{mapId}");
                    if (mapNameNode != null && mapNameNode is INxNode nxMapNode)
                    {
                        return nxMapNode["mapName"]?.GetValue<string>() ?? $"Map {mapId}";
                    }
                }
                catch { }
            }
            
            // Fallback to direct string NX access
            if (stringNx != null)
            {
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
            }

            return $"Map {mapId}";
        }

        private void LoadPlatforms(INxNode mapNode, MapData mapData)
        {
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null)
            {
                UnityEngine.Debug.LogWarning("No foothold node found in map");
                return;
            }

            UnityEngine.Debug.Log($"Loading platforms from foothold node with {footholdNode.Children.Count()} layers");
            
            // Debug: Print full node structure
            DebugPrintNodeStructure(footholdNode, "foothold", 0);
            
            int platformId = 1;
            foreach (var layerNode in footholdNode.Children)
            {
                UnityEngine.Debug.Log($"  Layer {layerNode.Name} has {layerNode.Children.Count()} groups");
                foreach (var groupNode in layerNode.Children)
                {
                    UnityEngine.Debug.Log($"    Group {groupNode.Name} has {groupNode.Children.Count()} children");
                    
                    // Process all children as potential footholds
                    foreach (var child in groupNode.Children)
                    {
                        // Check if this is a foothold node (has x1, y1, x2, y2)
                        if (child["x1"] != null && child["y1"] != null && 
                            child["x2"] != null && child["y2"] != null)
                        {
                            ProcessFoothold(child, ref platformId, mapData);
                        }
                        else
                        {
                            // Check one level deeper
                            foreach (var subChild in child.Children)
                            {
                                if (subChild["x1"] != null && subChild["y1"] != null && 
                                    subChild["x2"] != null && subChild["y2"] != null)
                                {
                                    ProcessFoothold(subChild, ref platformId, mapData);
                                }
                            }
                        }
                    }
                }
            }
            
            UnityEngine.Debug.Log($"Loaded {mapData.Platforms.Count} platforms from foothold data");
        }
        
        private void DebugPrintNodeStructure(INxNode node, string name, int depth)
        {
            if (depth > 5) return; // Prevent infinite recursion
            
            string indent = new string(' ', depth * 2);
            UnityEngine.Debug.Log($"{indent}{name} [{node.Children.Count()} children]");
            
            // Check for foothold properties
            if (node["x1"] != null || node["y1"] != null || node["x2"] != null || node["y2"] != null)
            {
                var x1 = node["x1"]?.GetValue<int>() ?? 0;
                var y1 = node["y1"]?.GetValue<int>() ?? 0;
                var x2 = node["x2"]?.GetValue<int>() ?? 0;
                var y2 = node["y2"]?.GetValue<int>() ?? 0;
                UnityEngine.Debug.Log($"{indent}  -> Foothold data: ({x1},{y1}) to ({x2},{y2})");
            }
            
            // Print first few children to avoid spam
            int count = 0;
            foreach (var child in node.Children)
            {
                if (count++ > 10) 
                {
                    UnityEngine.Debug.Log($"{indent}  ... and {node.Children.Count() - count} more");
                    break;
                }
                DebugPrintNodeStructure(child, child.Name, depth + 1);
            }
        }
        
        private void ProcessFoothold(INxNode fhNode, ref int platformId, MapData mapData)
        {
            var x1 = fhNode["x1"]?.GetValue<int>() ?? 0;
            var y1 = fhNode["y1"]?.GetValue<int>() ?? 0;
            var x2 = fhNode["x2"]?.GetValue<int>() ?? 0;
            var y2 = fhNode["y2"]?.GetValue<int>() ?? 0;

            // Skip invalid footholds
            if (x1 == 0 && y1 == 0 && x2 == 0 && y2 == 0)
            {
                UnityEngine.Debug.LogWarning($"Skipping invalid foothold {fhNode.Name}");
                return;
            }

            mapData.Platforms.Add(new Platform
            {
                Id = platformId++,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Type = PlatformType.Normal
            });
            
            UnityEngine.Debug.Log($"Platform {platformId-1}: ({x1},{y1}) to ({x2},{y2})");
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