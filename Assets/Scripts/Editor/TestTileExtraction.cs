using UnityEngine;
using UnityEditor;
using MapleClient.SceneGeneration;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class TestTileExtraction : EditorWindow
    {
        [MenuItem("MapleUnity/Test/Tile Extraction Fix")]
        public static void ShowWindow()
        {
            GetWindow<TestTileExtraction>("Test Tile Extraction");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Test Tile Extraction Fix", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test Henesys Tile Extraction"))
            {
                TestHenesysTileExtraction();
            }
        }
        
        private void TestHenesysTileExtraction()
        {
            Debug.Log("=== Testing Fixed Tile Extraction ===");
            
            // Initialize NX Manager
            var nxManager = NXDataManagerSingleton.Instance;
            
            // Create extractor
            MapDataExtractor extractor = new MapDataExtractor();
            
            // Extract Henesys data
            var mapData = extractor.ExtractMapData(100000000);
            
            if (mapData != null)
            {
                Debug.Log($"Extraction Results:");
                Debug.Log($"  - Tiles extracted: {mapData.Tiles?.Count ?? 0}");
                
                if (mapData.Tiles != null && mapData.Tiles.Count > 0)
                {
                    Debug.Log($"\nFirst 10 tiles:");
                    int count = 0;
                    var uniqueTileSets = mapData.Tiles.Select(t => t.TileSet).Distinct().ToList();
                    Debug.Log($"Unique tile sets found: {string.Join(", ", uniqueTileSets)}");
                    
                    foreach (var tile in mapData.Tiles.Take(10))
                    {
                        Debug.Log($"  [{count}] TileSet='{tile.TileSet}', Variant='{tile.Variant}', No={tile.No}, Pos=({tile.X},{tile.Y})");
                        
                        // Test loading the sprite
                        var sprite = nxManager.GetTileSprite(tile.TileSet, tile.Variant, tile.No);
                        if (sprite != null)
                        {
                            Debug.Log($"    ✓ Sprite loaded successfully: {sprite.texture.width}x{sprite.texture.height}");
                        }
                        else
                        {
                            Debug.LogWarning($"    ✗ Failed to load sprite");
                        }
                        count++;
                    }
                    
                    // Group by tileSet to see distribution
                    var tileSetGroups = mapData.Tiles.GroupBy(t => t.TileSet)
                                                   .OrderByDescending(g => g.Count())
                                                   .ToList();
                    
                    Debug.Log($"\nTile distribution by tileSet:");
                    foreach (var group in tileSetGroups)
                    {
                        Debug.Log($"  {group.Key}: {group.Count()} tiles");
                    }
                }
                else
                {
                    Debug.LogError("No tiles were extracted!");
                    
                    // Try to debug the raw tile data
                    var mapNode = nxManager.GetMapNode(100000000);
                    if (mapNode != null)
                    {
                        // Check layer 0 and 1 tiles
                        for (int layer = 0; layer <= 1; layer++)
                        {
                            var layerNode = mapNode[layer.ToString()];
                            if (layerNode != null)
                            {
                                var tileNode = layerNode["tile"];
                                if (tileNode != null)
                                {
                                    Debug.Log($"\nLayer {layer} raw tile data (first 3):");
                                    int debugCount = 0;
                                    foreach (var tile in tileNode.Children)
                                    {
                                        Debug.Log($"  Tile '{tile.Name}':");
                                        Debug.Log($"    Children: {string.Join(", ", tile.Children.Select(c => $"{c.Name}={c.Value}"))}");
                                        
                                        // Check all possible property names
                                        string[] props = {"tS", "u", "no", "x", "y", "z", "zM"};
                                        foreach (var prop in props)
                                        {
                                            var val = tile[prop]?.Value;
                                            if (val != null)
                                            {
                                                Debug.Log($"    {prop} = {val}");
                                            }
                                        }
                                        
                                        if (++debugCount >= 3) break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to extract map data!");
            }
        }
    }
}