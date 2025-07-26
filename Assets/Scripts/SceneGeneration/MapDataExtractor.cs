using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using MapleClient.GameData;
using GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Extracts map data from NX files for scene generation
    /// </summary>
    public class MapDataExtractor
    {
        private NXDataManagerSingleton nxManager;
        private NXDataManager dataManager;
        
        public MapDataExtractor()
        {
            nxManager = NXDataManagerSingleton.Instance;
            dataManager = nxManager.DataManager;
        }
        
        /// <summary>
        /// Extract all data for a specific map
        /// </summary>
        public MapData ExtractMapData(int mapId)
        {
            string mapIdStr = mapId.ToString("D9");
            var mapData = new MapData { MapId = mapId };
            
            // Try different map file locations
            string[] mapPaths = {
                $"Map/Map{mapIdStr[0]}/{mapIdStr}.img",
                $"Map/Map/{mapIdStr[0]}/{mapIdStr}.img",
                $"Map{mapIdStr[0]}/{mapIdStr}.img"
            };
            
            INxNode mapNode = nxManager.GetMapNode(mapId);
            
            if (mapNode == null)
            {
                Debug.LogError($"Map node not found for map ID: {mapId}");
                return null;
            }
            
            // Extract map info
            ExtractMapInfo(mapNode, mapData);
            
            // Extract footholds (platforms)
            ExtractFootholds(mapNode, mapData);
            
            // Extract portals
            ExtractPortals(mapNode, mapData);
            
            // Extract life (NPCs and monsters)
            ExtractLife(mapNode, mapData);
            
            // Extract backgrounds
            ExtractBackgrounds(mapNode, mapData);
            
            // Extract objects
            ExtractObjects(mapNode, mapData);
            
            // Extract tiles
            ExtractTiles(mapNode, mapData);
            
            return mapData;
        }
        
        private void ExtractMapInfo(INxNode mapNode, MapData mapData)
        {
            var infoNode = mapNode["info"];
            if (infoNode != null)
            {
                mapData.VRBounds = new Bounds();
                
                // Extract VR bounds
                var vrLeft = infoNode["VRLeft"]?.GetValue<int>() ?? -1000;
                var vrRight = infoNode["VRRight"]?.GetValue<int>() ?? 1000;
                var vrTop = infoNode["VRTop"]?.GetValue<int>() ?? -1000;
                var vrBottom = infoNode["VRBottom"]?.GetValue<int>() ?? 1000;
                
                mapData.VRBounds = CoordinateConverter.ToUnityBounds(vrLeft, vrRight, vrTop, vrBottom);
                
                // Extract other info
                mapData.BGM = infoNode["bgm"]?.GetValue<string>() ?? "";
                mapData.Cloud = infoNode["cloud"]?.GetValue<int>() ?? 0;
                mapData.FieldLimit = infoNode["fieldLimit"]?.GetValue<int>() ?? 0;
                mapData.ReturnMap = infoNode["returnMap"]?.GetValue<int>() ?? 0;
                mapData.ForcedReturn = infoNode["forcedReturn"]?.GetValue<int>() ?? 999999999;
            }
        }
        
        private void ExtractFootholds(INxNode mapNode, MapData mapData)
        {
            mapData.Footholds = new List<Foothold>();
            
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null) return;
            
            // Navigate through layers
            foreach (var layer in footholdNode.Children)
            {
                foreach (var group in layer.Children)
                {
                    foreach (var fh in group.Children)
                    {
                        var foothold = new Foothold
                        {
                            Id = int.Parse(fh.Name),
                            X1 = fh["x1"]?.GetValue<int>() ?? 0,
                            Y1 = fh["y1"]?.GetValue<int>() ?? 0,
                            X2 = fh["x2"]?.GetValue<int>() ?? 0,
                            Y2 = fh["y2"]?.GetValue<int>() ?? 0,
                            Next = fh["next"]?.GetValue<int>() ?? 0,
                            Prev = fh["prev"]?.GetValue<int>() ?? 0
                        };
                        
                        mapData.Footholds.Add(foothold);
                    }
                }
            }
        }
        
        private void ExtractPortals(INxNode mapNode, MapData mapData)
        {
            mapData.Portals = new List<Portal>();
            
            var portalNode = mapNode["portal"];
            if (portalNode == null) return;
            
            foreach (var portal in portalNode.Children)
            {
                var portalData = new Portal
                {
                    Id = int.Parse(portal.Name),
                    Name = portal["pn"]?.GetValue<string>() ?? "",
                    Type = portal["pt"]?.GetValue<int>() ?? 0,
                    X = portal["x"]?.GetValue<int>() ?? 0,
                    Y = portal["y"]?.GetValue<int>() ?? 0,
                    TargetMap = portal["tm"]?.GetValue<int>() ?? 999999999,
                    TargetName = portal["tn"]?.GetValue<string>() ?? ""
                };
                
                mapData.Portals.Add(portalData);
            }
        }
        
        private void ExtractLife(INxNode mapNode, MapData mapData)
        {
            mapData.NPCs = new List<LifeData>();
            mapData.Monsters = new List<LifeData>();
            
            var lifeNode = mapNode["life"];
            if (lifeNode == null) return;
            
            foreach (var life in lifeNode.Children)
            {
                var type = life["type"]?.GetValue<string>() ?? "";
                var lifeData = new LifeData
                {
                    Id = life["id"]?.GetValue<string>() ?? "",
                    X = life["x"]?.GetValue<int>() ?? 0,
                    Y = life["y"]?.GetValue<int>() ?? 0,
                    FH = life["fh"]?.GetValue<int>() ?? 0,
                    RX0 = life["rx0"]?.GetValue<int>() ?? 0,
                    RX1 = life["rx1"]?.GetValue<int>() ?? 0,
                    MobTime = life["mobTime"]?.GetValue<int>() ?? 0,
                    F = life["f"]?.GetValue<int>() ?? 0
                };
                
                if (type == "n")
                    mapData.NPCs.Add(lifeData);
                else if (type == "m")
                    mapData.Monsters.Add(lifeData);
            }
        }
        
        private void ExtractBackgrounds(INxNode mapNode, MapData mapData)
        {
            mapData.Backgrounds = new List<BackgroundData>();
            
            var backNode = mapNode["back"];
            if (backNode == null) return;
            
            foreach (var bg in backNode.Children)
            {
                var bgName = bg["bS"]?.GetValue<string>() ?? "";
                var layerNo = bg["no"]?.GetValue<int>() ?? int.Parse(bg.Name);
                
                var bgData = new BackgroundData
                {
                    No = layerNo,
                    BgName = bgName,
                    X = bg["x"]?.GetValue<int>() ?? 0,
                    Y = bg["y"]?.GetValue<int>() ?? 0,
                    RX = bg["rx"]?.GetValue<int>() ?? 0,
                    RY = bg["ry"]?.GetValue<int>() ?? 0,
                    Type = bg["type"]?.GetValue<int>() ?? 0,
                    A = bg["a"]?.GetValue<int>() ?? 255,
                    Front = bg["front"]?.GetValue<int>() ?? 0,
                    Ani = bg["ani"]?.GetValue<int>() ?? 0,
                    F = bg["f"]?.GetValue<int>() ?? 0
                };
                
                if (!string.IsNullOrEmpty(bgName))
                {
                    Debug.Log($"Background {bg.Name}: bS='{bgData.BgName}', type={bgData.Type}, no={bgData.No}");
                }
                
                mapData.Backgrounds.Add(bgData);
            }
        }
        
        private void ExtractObjects(INxNode mapNode, MapData mapData)
        {
            mapData.Objects = new List<ObjectData>();
            
            // Extract objects from each layer
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode == null) continue;
                
                var objNode = layerNode["obj"];
                if (objNode == null) continue;
                
                foreach (var obj in objNode.Children)
                {
                    var objData = new ObjectData
                    {
                        Layer = layer,
                        ObjName = obj["oS"]?.GetValue<string>() ?? "",
                        L0 = obj["l0"]?.GetValue<string>() ?? "",
                        L1 = obj["l1"]?.GetValue<string>() ?? "",
                        L2 = obj["l2"]?.GetValue<string>() ?? "",
                        X = obj["x"]?.GetValue<int>() ?? 0,
                        Y = obj["y"]?.GetValue<int>() ?? 0,
                        Z = obj["z"]?.GetValue<int>() ?? 0,
                        F = obj["f"]?.GetValue<int>() ?? 0,
                        ZM = obj["zM"]?.GetValue<int>() ?? 0
                    };
                    
                    mapData.Objects.Add(objData);
                }
            }
        }
        
        private void ExtractTiles(INxNode mapNode, MapData mapData)
        {
            mapData.Tiles = new List<TileData>();
            
            // IMPORTANT: In MapleStory, tiles are stored in numbered layers (0-7)
            // Each layer has its own info/tS value!
            Debug.Log("Extracting tiles from layers...");
            
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode != null)
                {
                    // Get the tileset for THIS LAYER
                    string layerTileSet = null;
                    var layerInfo = layerNode["info"];
                    if (layerInfo != null)
                    {
                        var tS = layerInfo["tS"]?.GetValue<string>();
                        if (tS != null)
                        {
                            layerTileSet = tS;
                            Debug.Log($"Layer {layer} has tS: '{layerTileSet}'");
                        }
                    }
                    
                    var layerTileNode = layerNode["tile"];
                    if (layerTileNode != null && layerTileNode.Children.Any())
                    {
                        Debug.Log($"Found tile node in layer {layer} with {layerTileNode.Children.Count()} tiles, tileSet='{layerTileSet}'");
                        ExtractTilesFromNode(layerTileNode, mapData.Tiles, layerTileSet, layer);
                    }
                }
            }
            
            Debug.Log($"Extracted {mapData.Tiles.Count} tiles total from all layers");
        }
        
        private void ExtractTilesFromNode(INxNode tileNode, List<TileData> tiles, string defaultTileSet = null, int layer = 0)
        {
            // Debug first few tiles to understand structure
            int debugCount = 0;
            int skippedCount = 0;
            
            foreach (var tile in tileNode.Children)
            {
                if (debugCount < 3)
                {
                    Debug.Log($"  Examining tile node '{tile.Name}':");
                    foreach (var child in tile.Children.Take(10))
                    {
                        Debug.Log($"    - {child.Name}: {child.Value?.ToString() ?? "complex node"}");
                    }
                    debugCount++;
                }
                
                // Try to get tileSet from node, or use default
                var tileSet = tile["tS"]?.GetValue<string>();
                
                // Debug individual tile tS values
                if (debugCount < 5 && tile["tS"] != null)
                {
                    Debug.Log($"  Tile {tile.Name} has its own tS: '{tileSet}'");
                }
                
                if (string.IsNullOrEmpty(tileSet) && !string.IsNullOrEmpty(defaultTileSet))
                {
                    tileSet = defaultTileSet;
                }
                
                var tileData = new TileData
                {
                    TileSet = tileSet ?? "",
                    Variant = tile["u"]?.GetValue<string>() ?? "",
                    No = tile["no"]?.GetValue<int>() ?? 0,
                    X = tile["x"]?.GetValue<int>() ?? 0,
                    Y = tile["y"]?.GetValue<int>() ?? 0,
                    Z = tile["z"]?.GetValue<int>() ?? 0,
                    ZM = tile["zM"]?.GetValue<int>() ?? 0,
                    Layer = layer
                };
                
                // In MapleStory, empty tileset is valid - it becomes ".img" in C++ client
                // So we should add all tiles, even with empty tileset
                tiles.Add(tileData);
                if (tiles.Count <= 5) // Log first few successful tiles
                {
                    string displayTileSet = string.IsNullOrEmpty(tileData.TileSet) ? ".img" : tileData.TileSet;
                    Debug.Log($"  Added tile: {displayTileSet}/{tileData.Variant}/{tileData.No} at ({tileData.X},{tileData.Y})");
                }
            }
            
            if (tiles.Count == 0 && tileNode.Children.Any())
            {
                Debug.LogWarning($"  No valid tiles found despite {tileNode.Children.Count()} tile nodes! Skipped {skippedCount} tiles.");
                if (string.IsNullOrEmpty(defaultTileSet))
                {
                    Debug.LogWarning("  Consider checking if tiles use an implicit tileSet name based on the map.");
                }
            }
            else if (skippedCount > 0)
            {
                Debug.LogWarning($"  Extracted {tiles.Count} tiles, skipped {skippedCount} tiles without tileSet.");
            }
        }
        
        private string GetDefaultTileSet_DEPRECATED(INxNode mapNode)
        {
            // Many MapleStory maps use a default tile set based on the map theme
            // We can try to infer this from various sources:
            
            // 1. Check if there's a tS property in map info
            var infoNode = mapNode["info"];
            if (infoNode != null)
            {
                var tS = infoNode["tS"]?.GetValue<string>();
                if (tS != null) // Found tS node (could be empty string)
                {
                    Debug.Log($"Found tS value in map info: '{tS}'");
                    
                    // The C++ client always appends .img to tS value
                    // So empty tS becomes ".img"
                    if (tS == "")
                    {
                        Debug.Log("Empty tS value - will use '.img' tileset (C++ client behavior)");
                    }
                    
                    return tS; // Return as-is, NXDataManagerSingleton will handle .img appending
                }
            }
            
            // 2. Check the first tile that has a tS property to use as default
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode != null)
                {
                    var layerTileNode = layerNode["tile"];
                    if (layerTileNode != null)
                    {
                        foreach (var tile in layerTileNode.Children)
                        {
                            var tS = tile["tS"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(tS))
                            {
                                Debug.Log($"Using tileSet from first tile with tS property: {tS}");
                                return tS;
                            }
                        }
                    }
                }
            }
            
            // 3. Common tile sets based on map ranges
            int mapId = mapNode.Name.Length >= 9 ? int.Parse(mapNode.Name.Substring(0, 9)) : 0;
            if (mapId >= 100000000 && mapId < 200000000) // Victoria Island
            {
                // Henesys area - try appropriate tilesets
                if (mapId >= 100000000 && mapId < 101000000)
                {
                    // Henesys is a beginner town, likely uses wood or village tileset
                    string[] candidateTilesets = { "wood", "village", "grassySoil", "woodMarble" };
                    string[] henesysVariants = { "bsc", "edD", "edU", "enH0", "enH1", "enV0", "enV1" };
                    
                    foreach (var candidate in candidateTilesets)
                    {
                        var tilesetNode = dataManager.GetNode("map", $"Tile/{candidate}.img");
                        if (tilesetNode != null)
                        {
                            // Check if it has the variants Henesys uses
                            var variants = new HashSet<string>(tilesetNode.Children.Select(c => c.Name));
                            bool hasAllVariants = henesysVariants.All(v => variants.Contains(v));
                            
                            if (hasAllVariants)
                            {
                                Debug.Log($"Using '{candidate}' tileSet for Henesys (has all needed variants)");
                                return candidate;
                            }
                            else
                            {
                                var missing = henesysVariants.Where(v => !variants.Contains(v)).ToList();
                                Debug.Log($"'{candidate}' tileset missing {missing.Count} variants: {string.Join(", ", missing)}");
                            }
                        }
                    }
                    
                    // If no perfect match, use wood as it's most likely for a village
                    Debug.Log("No perfect tileset match found, using 'wood' for Henesys");
                    return "wood";
                }
            }
            
            Debug.LogWarning("Could not determine default tileSet for map");
            return null;
        }
    }
    
    // Data structures
    public class MapData
    {
        public int MapId { get; set; }
        public Bounds VRBounds { get; set; }
        public string BGM { get; set; }
        public int Cloud { get; set; }
        public int FieldLimit { get; set; }
        public int ReturnMap { get; set; }
        public int ForcedReturn { get; set; }
        public List<Foothold> Footholds { get; set; }
        public List<Portal> Portals { get; set; }
        public List<LifeData> NPCs { get; set; }
        public List<LifeData> Monsters { get; set; }
        public List<BackgroundData> Backgrounds { get; set; }
        public List<ObjectData> Objects { get; set; }
        public List<TileData> Tiles { get; set; }
    }
    
    public class Foothold
    {
        public int Id { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int Next { get; set; }
        public int Prev { get; set; }
    }
    
    public class Portal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int TargetMap { get; set; }
        public string TargetName { get; set; }
    }
    
    public class LifeData
    {
        public string Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int FH { get; set; }
        public int RX0 { get; set; }
        public int RX1 { get; set; }
        public int MobTime { get; set; }
        public int F { get; set; }
    }
    
    public class BackgroundData
    {
        public int No { get; set; }
        public string BgName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int RX { get; set; }
        public int RY { get; set; }
        public int Type { get; set; }
        public int A { get; set; }
        public int Front { get; set; }
        public int Ani { get; set; }
        public int F { get; set; }
    }
    
    public class ObjectData
    {
        public int Layer { get; set; }
        public string ObjName { get; set; }
        public string L0 { get; set; }
        public string L1 { get; set; }
        public string L2 { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int F { get; set; }
        public int ZM { get; set; }
    }
    
    public class TileData
    {
        public string TileSet { get; set; }  // tS - tile set name (e.g., "grassySoil")
        public string Variant { get; set; }   // u - tile variant (e.g., "bsc", "edD", "edU")
        public int No { get; set; }           // no - tile number/frame
        public int X { get; set; }            // x position
        public int Y { get; set; }            // y position
        public int Z { get; set; }            // z order
        public int ZM { get; set; }           // z modifier
        public int Layer { get; set; }        // layer number (0-7)
    }
}