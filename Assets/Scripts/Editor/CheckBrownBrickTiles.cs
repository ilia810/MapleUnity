using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class CheckBrownBrickTiles : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check brownBrick Tiles")]
        public static void ShowWindow()
        {
            GetWindow<CheckBrownBrickTiles>("Check brownBrick");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Check brownBrick Tiles", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check brownBrick Structure"))
            {
                CheckBrownBrick();
            }
        }
        
        private void CheckBrownBrick()
        {
            Debug.Log("=== Checking brownBrick Tile Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var brownBrickNode = dataManager.GetNode("map", "Tile/brownBrick.img");
            if (brownBrickNode != null)
            {
                Debug.Log($"Found brownBrick.img with {brownBrickNode.Children.Count()} variants:");
                
                // List all variants and their tiles
                foreach (var variant in brownBrickNode.Children)
                {
                    var tileNumbers = variant.Children.Select(c => c.Name).ToList();
                    Debug.Log($"  {variant.Name}: {variant.Children.Count()} tiles - [{string.Join(", ", tileNumbers)}]");
                }
                
                // Check specific missing tiles
                Debug.Log("\nChecking specific missing tiles:");
                string[,] missingTiles = {
                    {"edD", "1"},
                    {"enH0", "3"},
                    {"enH1", "3"},
                    {"edU", "1"}
                };
                
                for (int i = 0; i < missingTiles.GetLength(0); i++)
                {
                    var variant = missingTiles[i, 0];
                    var tileNo = missingTiles[i, 1];
                    
                    var variantNode = brownBrickNode[variant];
                    if (variantNode != null)
                    {
                        var tileNode = variantNode[tileNo];
                        if (tileNode != null)
                        {
                            Debug.Log($"  ✓ {variant}/{tileNo} exists");
                        }
                        else
                        {
                            Debug.LogWarning($"  ✗ {variant}/{tileNo} NOT FOUND - variant exists but tile {tileNo} missing");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ {variant}/{tileNo} NOT FOUND - variant {variant} doesn't exist");
                    }
                }
                
                // Try to find a better matching tileset
                Debug.Log("\n=== Looking for alternative tilesets with these tiles ===");
                CheckAlternativeTilesets();
            }
            else
            {
                Debug.LogError("brownBrick.img not found!");
            }
        }
        
        private void CheckAlternativeTilesets()
        {
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode == null) return;
            
            // Check which tilesets have ALL the tiles we need
            string[,] neededTiles = {
                {"bsc", "0"}, {"bsc", "1"}, {"bsc", "2"}, {"bsc", "3"}, {"bsc", "4"},
                {"edD", "0"}, {"edD", "1"},
                {"edU", "0"}, {"edU", "1"},
                {"enH0", "0"}, {"enH0", "1"}, {"enH0", "2"}, {"enH0", "3"},
                {"enH1", "0"}, {"enH1", "1"}, {"enH1", "2"}, {"enH1", "3"},
                {"enV0", "0"}, {"enV0", "1"}, {"enV0", "2"}
            };
            
            foreach (var tileSet in tileNode.Children.Take(20))
            {
                int matchCount = 0;
                int totalNeeded = neededTiles.GetLength(0);
                
                for (int i = 0; i < totalNeeded; i++)
                {
                    var variant = neededTiles[i, 0];
                    var tileNo = neededTiles[i, 1];
                    
                    var variantNode = tileSet[variant];
                    if (variantNode != null && variantNode[tileNo] != null)
                    {
                        matchCount++;
                    }
                }
                
                if (matchCount == totalNeeded)
                {
                    Debug.Log($"✓ {tileSet.Name} has ALL needed tiles! ({matchCount}/{totalNeeded})");
                }
                else if (matchCount > totalNeeded * 0.8)
                {
                    Debug.Log($"  {tileSet.Name} has {matchCount}/{totalNeeded} tiles");
                }
            }
        }
    }
}