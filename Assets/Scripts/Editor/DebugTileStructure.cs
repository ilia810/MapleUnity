using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class DebugTileStructure : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Tile Structure")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileStructure>("Debug Tile Structure");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Debug Tile Structure", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Debug Wood Tile Structure"))
            {
                DebugWoodTileStructure();
            }
            
            if (GUILayout.Button("List All Tile Sets"))
            {
                ListAllTileSets();
            }
            
            if (GUILayout.Button("Debug grassySoil Tile Structure"))
            {
                DebugTileSetStructure("grassySoil");
            }
            
            if (GUILayout.Button("Find Henesys Tile Pattern"))
            {
                FindHenesysTilePattern();
            }
            
            if (GUILayout.Button("List Grassy Tile Sets"))
            {
                ListGrassyTileSets();
            }
        }
        
        private void DebugWoodTileStructure()
        {
            Debug.Log("=== Debugging Wood Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check if wood.img exists
            var woodNode = dataManager.GetNode("map", "Tile/wood.img");
            if (woodNode != null)
            {
                Debug.Log("Found wood.img node!");
                Debug.Log($"wood.img has {woodNode.Children.Count()} children");
                
                // List all variants
                int count = 0;
                foreach (var variant in woodNode.Children)
                {
                    Debug.Log($"  Variant: {variant.Name} (has {variant.Children.Count()} children)");
                    
                    // Show first few tiles in each variant
                    int tileCount = 0;
                    foreach (var tile in variant.Children.Take(3))
                    {
                        Debug.Log($"    Tile: {tile.Name}");
                        
                        // Check if it's an image node
                        if (tile.Value != null)
                        {
                            Debug.Log($"      Has direct value: {tile.Value.GetType().Name}");
                        }
                        
                        // Check children
                        if (tile.Children.Any())
                        {
                            Debug.Log($"      Children: {string.Join(", ", tile.Children.Select(c => c.Name))}");
                        }
                        
                        tileCount++;
                    }
                    
                    if (++count >= 5) break; // Only show first 5 variants
                }
            }
            else
            {
                Debug.LogError("wood.img not found in Tile folder!");
                
                // Try without .img
                woodNode = dataManager.GetNode("map", "Tile/wood");
                if (woodNode != null)
                {
                    Debug.Log("Found wood node (without .img)!");
                    Debug.Log($"wood has {woodNode.Children.Count()} children");
                }
                else
                {
                    Debug.LogError("No wood tile set found!");
                }
            }
        }
        
        private void ListAllTileSets()
        {
            Debug.Log("=== Listing All Tile Sets ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode != null)
            {
                Debug.Log($"Tile folder has {tileNode.Children.Count()} entries");
                
                int count = 0;
                foreach (var child in tileNode.Children)
                {
                    Debug.Log($"  - {child.Name}");
                    if (++count >= 30)
                    {
                        Debug.Log("  ... (truncated)");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("Tile folder not found!");
                
                // Check if tiles are stored differently
                Debug.Log("Checking alternative tile locations...");
                
                // Check if tiles are embedded in map data
                var mapNode = dataManager.GetNode("map", "Map/Map1/100000000.img");
                if (mapNode != null)
                {
                    for (int layer = 0; layer <= 7; layer++)
                    {
                        var layerNode = mapNode[layer.ToString()];
                        if (layerNode != null)
                        {
                            var layerTileNode = layerNode["tile"];
                            if (layerTileNode != null && layerTileNode.Children.Any())
                            {
                                Debug.Log($"Found tiles in map layer {layer}!");
                                var firstTile = layerTileNode.Children.First();
                                Debug.Log($"First tile structure:");
                                Debug.Log($"  Name: {firstTile.Name}");
                                foreach (var prop in firstTile.Children.Take(10))
                                {
                                    Debug.Log($"  - {prop.Name}: {prop.Value}");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void DebugTileSetStructure(string tileSetName)
        {
            Debug.Log($"=== Debugging {tileSetName} Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileSetNode = dataManager.GetNode("map", $"Tile/{tileSetName}.img");
            if (tileSetNode != null)
            {
                Debug.Log($"Found {tileSetName}.img node!");
                Debug.Log($"{tileSetName}.img has {tileSetNode.Children.Count()} children");
                
                // List all variants
                int count = 0;
                foreach (var variant in tileSetNode.Children)
                {
                    Debug.Log($"  Variant: {variant.Name} (has {variant.Children.Count()} tiles)");
                    
                    // Show first few tiles in each variant
                    int tileCount = 0;
                    foreach (var tile in variant.Children.Take(3))
                    {
                        Debug.Log($"    Tile: {tile.Name}");
                        
                        // Check if it's an image node
                        if (tile.Value != null)
                        {
                            Debug.Log($"      Has direct value of type: {tile.Value.GetType().Name}");
                        }
                        
                        // Check children
                        if (tile.Children.Any())
                        {
                            Debug.Log($"      Children: {string.Join(", ", tile.Children.Select(c => c.Name))}");
                        }
                        
                        tileCount++;
                    }
                    
                    if (++count >= 10) break; // Show first 10 variants
                }
            }
            else
            {
                Debug.LogError($"{tileSetName}.img not found in Tile folder!");
            }
        }
        
        private void FindHenesysTilePattern()
        {
            Debug.Log("=== Finding Henesys Tile Pattern ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Load Henesys map and check what tiles it references
            var mapNode = dataManager.GetNode("map", "Map/Map1/100000000.img");
            if (mapNode != null)
            {
                // Collect all unique tile patterns
                var tilePatterns = new System.Collections.Generic.HashSet<string>();
                
                for (int layer = 0; layer <= 7; layer++)
                {
                    var layerNode = mapNode[layer.ToString()];
                    if (layerNode != null)
                    {
                        var layerTileNode = layerNode["tile"];
                        if (layerTileNode != null)
                        {
                            foreach (var tile in layerTileNode.Children.Take(20))
                            {
                                var u = tile["u"]?.GetValue<string>() ?? "";
                                var no = tile["no"]?.GetValue<int>() ?? 0;
                                if (!string.IsNullOrEmpty(u))
                                {
                                    tilePatterns.Add($"variant={u}, no={no}");
                                }
                            }
                        }
                    }
                }
                
                Debug.Log($"Found {tilePatterns.Count} unique tile patterns in Henesys:");
                foreach (var pattern in tilePatterns)
                {
                    Debug.Log($"  - {pattern}");
                }
                
                // Now check which tile sets contain these variants
                Debug.Log("\nChecking which tile sets contain these variants...");
                
                var tileNode = dataManager.GetNode("map", "Tile");
                if (tileNode != null)
                {
                    foreach (var tileSet in tileNode.Children.Take(20))
                    {
                        bool hasMatch = false;
                        foreach (var variant in tileSet.Children)
                        {
                            if (variant.Name == "bsc" || variant.Name == "edD" || variant.Name == "enH0" || variant.Name == "enV0")
                            {
                                hasMatch = true;
                                break;
                            }
                        }
                        
                        if (hasMatch)
                        {
                            Debug.Log($"  ✓ {tileSet.Name} contains matching variants!");
                        }
                    }
                }
            }
        }
        
        private void ListGrassyTileSets()
        {
            Debug.Log("=== Listing Grassy Tile Sets ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode != null)
            {
                var grassyNodes = tileNode.Children.Where(c => c.Name.ToLower().Contains("grass")).ToList();
                Debug.Log($"Found {grassyNodes.Count} tile sets containing 'grass':");
                foreach (var node in grassyNodes)
                {
                    Debug.Log($"  - {node.Name}");
                    
                    // Check if it has the variants we need
                    bool hasBsc = node.Children.Any(c => c.Name == "bsc");
                    bool hasEdD = node.Children.Any(c => c.Name == "edD");
                    bool hasEnH0 = node.Children.Any(c => c.Name == "enH0");
                    
                    if (hasBsc && hasEdD && hasEnH0)
                    {
                        Debug.Log($"    ✓ Has all needed variants!");
                    }
                }
                
                // Also check for other common Henesys tile sets
                Debug.Log("\nChecking other potential Henesys tile sets:");
                string[] possibleNames = { "village", "town", "henesys", "wood", "soil" };
                foreach (var keyword in possibleNames)
                {
                    var matchingNodes = tileNode.Children.Where(c => c.Name.ToLower().Contains(keyword)).ToList();
                    if (matchingNodes.Any())
                    {
                        Debug.Log($"\nTile sets containing '{keyword}':");
                        foreach (var node in matchingNodes)
                        {
                            Debug.Log($"  - {node.Name}");
                        }
                    }
                }
            }
        }
    }
}