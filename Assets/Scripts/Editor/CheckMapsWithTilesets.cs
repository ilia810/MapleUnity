using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using System.Collections.Generic;

namespace MapleClient.Editor
{
    public class CheckMapsWithTilesets : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check Maps With Tilesets")]
        public static void ShowWindow()
        {
            GetWindow<CheckMapsWithTilesets>("Check Maps With Tilesets");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Check Maps That Have tS Values", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Find Maps with Tilesets"))
            {
                FindMapsWithTilesets();
            }
            
            if (GUILayout.Button("Check Specific Map Range"))
            {
                CheckMapRange();
            }
        }
        
        private void FindMapsWithTilesets()
        {
            Debug.Log("=== Finding Maps with tS Values ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var mapNode = dataManager.GetNode("map", "Map");
            if (mapNode == null)
            {
                Debug.LogError("Map node not found!");
                return;
            }
            
            var mapsWithTilesets = new List<(string mapId, string tileset)>();
            var mapsWithoutTilesets = new List<string>();
            
            // Check all Map folders
            foreach (var mapFolder in mapNode.Children)
            {
                if (!mapFolder.Name.StartsWith("Map")) continue;
                
                foreach (var mapFile in mapFolder.Children)
                {
                    if (!mapFile.Name.EndsWith(".img")) continue;
                    
                    var infoNode = mapFile["info"];
                    if (infoNode != null)
                    {
                        var tS = infoNode["tS"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(tS))
                        {
                            mapsWithTilesets.Add((mapFile.Name.Replace(".img", ""), tS));
                        }
                        else
                        {
                            // Check if this map has tiles
                            bool hasTiles = false;
                            for (int i = 0; i <= 7; i++)
                            {
                                var layer = mapFile[i.ToString()];
                                if (layer?["tile"] != null && layer["tile"].Children.Any())
                                {
                                    hasTiles = true;
                                    break;
                                }
                            }
                            
                            if (hasTiles)
                            {
                                mapsWithoutTilesets.Add(mapFile.Name.Replace(".img", ""));
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"\n=== Maps WITH tS values ({mapsWithTilesets.Count}) ===");
            // Group by tileset
            var tilesetGroups = mapsWithTilesets.GroupBy(m => m.tileset);
            foreach (var group in tilesetGroups.OrderBy(g => g.Key))
            {
                Debug.Log($"\nTileset '{group.Key}': {group.Count()} maps");
                foreach (var map in group.Take(5))
                {
                    Debug.Log($"  - {map.mapId}");
                }
                if (group.Count() > 5)
                {
                    Debug.Log($"  ... and {group.Count() - 5} more");
                }
            }
            
            Debug.Log($"\n=== Maps WITHOUT tS but WITH tiles ({mapsWithoutTilesets.Count}) ===");
            foreach (var map in mapsWithoutTilesets.Take(20))
            {
                Debug.Log($"  - {map}");
            }
            if (mapsWithoutTilesets.Count > 20)
            {
                Debug.Log($"  ... and {mapsWithoutTilesets.Count - 20} more");
            }
        }
        
        private void CheckMapRange()
        {
            Debug.Log("=== Checking Specific Map Ranges ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check some known map ranges
            var mapRanges = new Dictionary<string, (int start, int end)>
            {
                { "Maple Island", (0, 10000000) },
                { "Victoria Island - Henesys", (100000000, 101000000) },
                { "Victoria Island - Ellinia", (101000000, 102000000) },
                { "Victoria Island - Perion", (102000000, 103000000) },
                { "Victoria Island - Kerning", (103000000, 104000000) },
                { "Orbis", (200000000, 201000000) },
                { "El Nath", (211000000, 212000000) },
                { "Ludibrium", (220000000, 230000000) },
                { "Aquarium", (230000000, 240000000) },
                { "Leafre", (240000000, 250000000) }
            };
            
            foreach (var range in mapRanges)
            {
                Debug.Log($"\n=== {range.Key} ===");
                
                var foundMaps = new List<(string mapId, string tileset, bool hasTiles)>();
                
                for (int mapId = range.Value.start; mapId < range.Value.end; mapId += 100)
                {
                    var mapPath = $"Map/Map{mapId.ToString()[0]}/{mapId.ToString("D9")}.img";
                    var mapNode = dataManager.GetNode("map", mapPath);
                    
                    if (mapNode != null)
                    {
                        var tS = mapNode["info"]?["tS"]?.GetValue<string>() ?? "";
                        
                        // Check if has tiles
                        bool hasTiles = false;
                        for (int i = 0; i <= 7; i++)
                        {
                            var layer = mapNode[i.ToString()];
                            if (layer?["tile"] != null && layer["tile"].Children.Any())
                            {
                                hasTiles = true;
                                break;
                            }
                        }
                        
                        if (hasTiles || !string.IsNullOrEmpty(tS))
                        {
                            foundMaps.Add((mapId.ToString(), tS, hasTiles));
                        }
                    }
                }
                
                if (foundMaps.Any())
                {
                    foreach (var map in foundMaps.Take(10))
                    {
                        Debug.Log($"  Map {map.mapId}: tS='{map.tileset}' hasTiles={map.hasTiles}");
                    }
                }
                else
                {
                    Debug.Log("  No maps with tiles found in this range");
                }
            }
        }
    }
}