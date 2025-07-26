using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class CheckGrassySoilTiles : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check grassySoil Tiles")]
        public static void ShowWindow()
        {
            GetWindow<CheckGrassySoilTiles>("Check grassySoil");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Check grassySoil Tile Structure", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check grassySoil Structure"))
            {
                CheckStructure();
            }
        }
        
        private void CheckStructure()
        {
            Debug.Log("=== Checking grassySoil Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check if grassySoil exists
            var grassySoilNode = dataManager.GetNode("map", "Tile/grassySoil.img");
            if (grassySoilNode != null)
            {
                Debug.Log("Found grassySoil.img!");
                Debug.Log($"grassySoil.img has {grassySoilNode.Children.Count()} variants");
                
                // Check for the variants we need
                string[] neededVariants = { "bsc", "edD", "edU", "enH0", "enH1", "enV0" };
                
                foreach (var variantName in neededVariants)
                {
                    var variant = grassySoilNode[variantName];
                    if (variant != null)
                    {
                        Debug.Log($"  ✓ Found variant '{variantName}' with {variant.Children.Count()} tiles");
                        
                        // Check specific tiles
                        for (int i = 0; i <= 4; i++)
                        {
                            var tile = variant[i.ToString()];
                            if (tile != null)
                            {
                                Debug.Log($"    - Tile {i} exists");
                                if (tile.Value != null)
                                {
                                    Debug.Log($"      Value type: {tile.Value.GetType().Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ Variant '{variantName}' NOT FOUND");
                    }
                }
                
                // Test loading a specific tile
                Debug.Log("\nTesting tile loading:");
                var testSprite = nxManager.GetTileSprite("grassySoil", "bsc", 0);
                if (testSprite != null)
                {
                    Debug.Log($"✓ Successfully loaded test sprite: {testSprite.texture.width}x{testSprite.texture.height}");
                }
                else
                {
                    Debug.LogError("✗ Failed to load test sprite");
                    
                    // Try direct path
                    var testNode = dataManager.GetNode("map", "Tile/grassySoil.img/bsc/0");
                    if (testNode != null)
                    {
                        Debug.Log("Node exists at Tile/grassySoil.img/bsc/0");
                        Debug.Log($"Node type: {testNode.GetType().Name}");
                        Debug.Log($"Has value: {testNode.Value != null}");
                        Debug.Log($"Has children: {testNode.Children.Any()}");
                    }
                }
            }
            else
            {
                Debug.LogError("grassySoil.img NOT FOUND!");
                
                // Check if it's in the list
                var tileNode = dataManager.GetNode("map", "Tile");
                if (tileNode != null)
                {
                    var grassyNodes = tileNode.Children.Where(c => c.Name.ToLower().Contains("grassy"));
                    Debug.Log($"Found {grassyNodes.Count()} nodes containing 'grassy':");
                    foreach (var node in grassyNodes)
                    {
                        Debug.Log($"  - {node.Name}");
                    }
                }
            }
        }
    }
}