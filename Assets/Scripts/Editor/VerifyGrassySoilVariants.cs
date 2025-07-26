using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class VerifyGrassySoilVariants : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Verify grassySoil Variants")]
        public static void ShowWindow()
        {
            GetWindow<VerifyGrassySoilVariants>("Verify grassySoil");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Verify grassySoil Variants", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check grassySoil"))
            {
                CheckGrassySoil();
            }
            
            if (GUILayout.Button("Check DeepgrassySoil"))  
            {
                CheckDeepGrassySoil();
            }
        }
        
        private void CheckGrassySoil()
        {
            CheckTileSet("grassySoil");
        }
        
        private void CheckDeepGrassySoil()
        {
            CheckTileSet("DeepgrassySoil");
        }
        
        private void CheckTileSet(string tileSetName)
        {
            Debug.Log($"=== Checking {tileSetName} Variants ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileSetNode = dataManager.GetNode("map", $"Tile/{tileSetName}.img");
            if (tileSetNode != null)
            {
                Debug.Log($"Found {tileSetName}.img with {tileSetNode.Children.Count()} variants:");
                
                // List all variants
                foreach (var variant in tileSetNode.Children)
                {
                    Debug.Log($"  - {variant.Name} ({variant.Children.Count()} tiles)");
                }
                
                // Check specific variants we need for Henesys
                string[] neededVariants = { "bsc", "edD", "edU", "enH0", "enH1", "enV0" };
                Debug.Log("\nChecking needed variants:");
                
                foreach (var variantName in neededVariants)
                {
                    var variant = tileSetNode[variantName];
                    if (variant != null)
                    {
                        Debug.Log($"  ✓ {variantName} - {variant.Children.Count()} tiles");
                        
                        // List available tile numbers
                        var tileNumbers = variant.Children.Select(c => c.Name).Take(10).ToList();
                        Debug.Log($"    Available tiles: {string.Join(", ", tileNumbers)}");
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ {variantName} - NOT FOUND");
                    }
                }
                
                // Test loading a sprite
                Debug.Log($"\nTesting sprite loading from {tileSetName}:");
                var testSprite = nxManager.GetTileSprite(tileSetName, "bsc", 0);
                if (testSprite != null)
                {
                    Debug.Log($"✓ Successfully loaded bsc/0: {testSprite.texture.width}x{testSprite.texture.height}");
                }
                else
                {
                    Debug.LogError("✗ Failed to load bsc/0");
                }
            }
            else
            {
                Debug.LogError($"{tileSetName}.img not found!");
            }
        }
    }
}