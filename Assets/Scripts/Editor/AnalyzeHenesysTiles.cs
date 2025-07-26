using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using System.Collections.Generic;

namespace MapleClient.Editor
{
    public class AnalyzeHenesysTiles : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Analyze Henesys Tiles")]
        public static void ShowWindow()
        {
            GetWindow<AnalyzeHenesysTiles>("Analyze Henesys Tiles");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Analyze Henesys Tile Data", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Analyze Tile Structure"))
            {
                AnalyzeTiles();
            }
            
            if (GUILayout.Button("Find Maps with tS Value"))
            {
                FindMapsWithTilesets();
            }
        }
        
        private void AnalyzeTiles()
        {
            Debug.Log("=== Analyzing Henesys Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var mapNode = dataManager.GetNode("map", "Map/Map1/100000000.img");
            if (mapNode == null)
            {
                Debug.LogError("Could not load Henesys map!");
                return;
            }
            
            // Collect all unique property names from tiles
            var uniqueProperties = new HashSet<string>();
            var tileCount = 0;
            
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode != null)
                {
                    var tileNode = layerNode["tile"];
                    if (tileNode != null)
                    {
                        foreach (var tile in tileNode.Children)
                        {
                            tileCount++;
                            
                            // Collect all property names
                            foreach (var prop in tile.Children)
                            {
                                uniqueProperties.Add(prop.Name);
                                
                                // If it's the first few tiles, show detailed info
                                if (tileCount <= 3)
                                {
                                    Debug.Log($"Tile {tileCount} property '{prop.Name}' = {prop.Value}");
                                }
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"\n=== Tile Analysis Results ===");
            Debug.Log($"Total tiles: {tileCount}");
            Debug.Log($"Unique properties found in tiles: {string.Join(", ", uniqueProperties)}");
            
            // Check if any tiles have properties that might indicate tileset
            Debug.Log("\n=== Checking for tileset hints ===");
            string[] possibleTilesetProps = { "tS", "tileset", "ts", "set", "type" };
            foreach (var prop in possibleTilesetProps)
            {
                if (uniqueProperties.Contains(prop))
                {
                    Debug.Log($"✓ Found property '{prop}' in tiles!");
                }
            }
            
            // Check objects to see what tileset they use
            Debug.Log("\n=== Checking Objects for Tileset Clues ===");
            for (int layer = 0; layer <= 2; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode != null)
                {
                    var objNode = layerNode["obj"];
                    if (objNode != null && objNode.Children.Any())
                    {
                        foreach (var obj in objNode.Children.Take(5))
                        {
                            var oS = obj["oS"]?.GetValue<string>();
                            if (oS != null && oS.Contains("grassySoil"))
                            {
                                Debug.Log($"Object uses grassySoil theme: {oS}");
                            }
                        }
                    }
                }
            }
        }
        
        private void FindMapsWithTilesets()
        {
            Debug.Log("=== Finding Maps with tS Values ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check several Victoria Island maps
            int[] mapIds = {
                100000000, // Henesys
                101000000, // Ellinia
                102000000, // Perion
                103000000, // Kerning City
                104000000, // Lith Harbor
                105040300, // Sleepywood
                110000000, // Eastern Road
                120000000  // Nautilus Harbor
            };
            
            foreach (var mapId in mapIds)
            {
                var mapPath = $"Map/Map{mapId.ToString()[0]}/{mapId.ToString("D9")}.img";
                var mapNode = dataManager.GetNode("map", mapPath);
                
                if (mapNode != null)
                {
                    var tS = mapNode["info"]?["tS"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(tS))
                    {
                        Debug.Log($"Map {mapId}: tS = '{tS}'");
                        
                        // Check if this tileset exists
                        var tilesetNode = dataManager.GetNode("map", $"Tile/{tS}.img");
                        if (tilesetNode != null)
                        {
                            Debug.Log($"  ✓ Tileset exists with {tilesetNode.Children.Count()} variants");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Map {mapId}: No tS value");
                        
                        // Check if it has tiles
                        bool hasTiles = false;
                        for (int layer = 0; layer <= 7; layer++)
                        {
                            var tiles = mapNode[layer.ToString()]?["tile"];
                            if (tiles != null && tiles.Children.Any())
                            {
                                hasTiles = true;
                                break;
                            }
                        }
                        
                        if (hasTiles)
                        {
                            Debug.LogWarning($"  ⚠️ Has tiles but no tileset!");
                        }
                    }
                }
            }
        }
    }
}