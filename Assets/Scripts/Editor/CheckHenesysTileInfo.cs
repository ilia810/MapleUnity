using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class CheckHenesysTileInfo : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check Henesys Tile Info")]
        public static void ShowWindow()
        {
            GetWindow<CheckHenesysTileInfo>("Henesys Tile Info");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Check Henesys Tile Info", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check info/tS Value"))
            {
                CheckTileSetInfo();
            }
            
            if (GUILayout.Button("List Brick Tile Sets"))
            {
                ListBrickTileSets();
            }
        }
        
        private void CheckTileSetInfo()
        {
            Debug.Log("=== Checking Henesys info/tS Value ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check map info node
            var mapNode = dataManager.GetNode("map", "Map/Map1/100000000.img");
            if (mapNode != null)
            {
                var infoNode = mapNode["info"];
                if (infoNode != null)
                {
                    Debug.Log("Found info node. Checking all properties:");
                    
                    foreach (var child in infoNode.Children)
                    {
                        Debug.Log($"  {child.Name}: {child.Value}");
                    }
                    
                    // Specifically check for tS
                    var tS = infoNode["tS"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(tS))
                    {
                        Debug.Log($"\n✓ Found tS value: '{tS}'");
                    }
                    else
                    {
                        Debug.LogWarning("\n✗ No tS value found in info node");
                    }
                }
                else
                {
                    Debug.LogError("No info node found!");
                }
                
                // Check actual tiles to see what they look like
                Debug.Log("\n=== Sampling Actual Tiles ===");
                for (int layer = 0; layer <= 1; layer++)
                {
                    var layerNode = mapNode[layer.ToString()];
                    if (layerNode != null)
                    {
                        var tileNode = layerNode["tile"];
                        if (tileNode != null && tileNode.Children.Any())
                        {
                            Debug.Log($"\nLayer {layer} - First 5 tiles:");
                            int count = 0;
                            foreach (var tile in tileNode.Children.Take(5))
                            {
                                var u = tile["u"]?.GetValue<string>();
                                var no = tile["no"]?.GetValue<int>() ?? 0;
                                var x = tile["x"]?.GetValue<int>() ?? 0;
                                var y = tile["y"]?.GetValue<int>() ?? 0;
                                
                                Debug.Log($"  Tile {count}: variant={u}, no={no}, pos=({x},{y})");
                                count++;
                            }
                        }
                    }
                }
            }
        }
        
        private void ListBrickTileSets()
        {
            Debug.Log("=== Listing Potential Brick Tile Sets ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode != null)
            {
                // Search for brick-related tile sets
                string[] brickKeywords = { "brick", "stone", "block", "floor", "wood", "town" };
                
                foreach (var keyword in brickKeywords)
                {
                    var matches = tileNode.Children.Where(c => c.Name.ToLower().Contains(keyword)).ToList();
                    if (matches.Any())
                    {
                        Debug.Log($"\nTile sets containing '{keyword}':");
                        foreach (var match in matches)
                        {
                            Debug.Log($"  - {match.Name}");
                            
                            // Check if it has the variants we need
                            bool hasBsc = match.Children.Any(c => c.Name == "bsc");
                            bool hasEdD = match.Children.Any(c => c.Name == "edD");
                            bool hasEnH0 = match.Children.Any(c => c.Name == "enH0");
                            
                            if (hasBsc || hasEdD || hasEnH0)
                            {
                                Debug.Log($"    ✓ Has matching variants: bsc={hasBsc}, edD={hasEdD}, enH0={hasEnH0}");
                            }
                        }
                    }
                }
            }
        }
    }
}