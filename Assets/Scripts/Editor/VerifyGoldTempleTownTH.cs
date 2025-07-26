using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class VerifyGoldTempleTownTH : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Verify goldTempleTownTH Tileset")]
        public static void ShowWindow()
        {
            GetWindow<VerifyGoldTempleTownTH>("Verify goldTempleTownTH");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Verify goldTempleTownTH Tileset", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check goldTempleTownTH"))
            {
                CheckTileset();
            }
        }
        
        private void CheckTileset()
        {
            Debug.Log("=== Checking goldTempleTownTH Tileset ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileSetNode = dataManager.GetNode("map", "Tile/goldTempleTownTH.img");
            if (tileSetNode != null)
            {
                Debug.Log($"Found goldTempleTownTH.img with {tileSetNode.Children.Count()} variants:");
                
                // List all variants
                foreach (var variant in tileSetNode.Children)
                {
                    var tileNumbers = variant.Children.Select(c => c.Name).ToList();
                    Debug.Log($"  {variant.Name}: {variant.Children.Count()} tiles - [{string.Join(", ", tileNumbers)}]");
                }
                
                // Check specific tiles needed by Henesys
                Debug.Log("\nChecking tiles used by Henesys:");
                string[,] neededTiles = {
                    {"bsc", "0"}, {"bsc", "1"}, {"bsc", "2"}, {"bsc", "3"}, {"bsc", "4"},
                    {"edD", "0"}, {"edD", "1"},
                    {"edU", "0"}, {"edU", "1"},
                    {"enH0", "0"}, {"enH0", "1"}, {"enH0", "2"}, {"enH0", "3"},
                    {"enH1", "0"}, {"enH1", "1"}, {"enH1", "2"}, {"enH1", "3"},
                    {"enV0", "0"}, {"enV0", "1"}, {"enV0", "2"}
                };
                
                int found = 0;
                int missing = 0;
                
                for (int i = 0; i < neededTiles.GetLength(0); i++)
                {
                    var variant = neededTiles[i, 0];
                    var tileNo = neededTiles[i, 1];
                    
                    var variantNode = tileSetNode[variant];
                    if (variantNode != null && variantNode[tileNo] != null)
                    {
                        Debug.Log($"  ✓ {variant}/{tileNo} found");
                        found++;
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ {variant}/{tileNo} MISSING");
                        missing++;
                    }
                }
                
                Debug.Log($"\nSummary: {found} tiles found, {missing} tiles missing");
                
                if (missing == 0)
                {
                    Debug.Log("✅ goldTempleTownTH has ALL tiles needed for Henesys!");
                }
                else
                {
                    Debug.LogWarning("⚠️ goldTempleTownTH is missing some tiles");
                }
            }
            else
            {
                Debug.LogError("goldTempleTownTH.img not found!");
            }
        }
    }
}