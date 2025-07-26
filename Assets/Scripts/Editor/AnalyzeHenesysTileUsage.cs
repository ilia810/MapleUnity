using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using System.Collections.Generic;

namespace MapleClient.Editor
{
    public class AnalyzeHenesysTileUsage : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Analyze Henesys Tile Usage")]
        public static void ShowWindow()
        {
            GetWindow<AnalyzeHenesysTileUsage>("Henesys Tile Analysis");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Analyze Tile Usage"))
            {
                AnalyzeTiles();
            }
        }
        
        private void AnalyzeTiles()
        {
            Debug.Log("=== Henesys Tile Usage Analysis ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(100000000);
            
            if (mapNode == null)
            {
                Debug.LogError("Failed to load Henesys map!");
                return;
            }
            
            // Analyze each layer
            for (int layer = 0; layer <= 7; layer++)
            {
                var layerNode = mapNode[layer.ToString()];
                if (layerNode == null) continue;
                
                // Get layer tileset
                string layerTileSet = "";
                var layerInfo = layerNode["info"];
                if (layerInfo != null)
                {
                    var tS = layerInfo["tS"]?.GetValue<string>();
                    if (tS != null) layerTileSet = tS;
                }
                
                var tileNode = layerNode["tile"];
                if (tileNode == null || !tileNode.Children.Any()) continue;
                
                Debug.Log($"\n--- Layer {layer} (tileSet: '{layerTileSet}') ---");
                
                // Track variant usage
                var variantUsage = new Dictionary<string, List<int>>();
                int minY = int.MaxValue, maxY = int.MinValue;
                
                foreach (var tile in tileNode.Children)
                {
                    string variant = tile["u"]?.GetValue<string>() ?? "";
                    int no = tile["no"]?.GetValue<int>() ?? 0;
                    int y = tile["y"]?.GetValue<int>() ?? 0;
                    
                    if (!variantUsage.ContainsKey(variant))
                        variantUsage[variant] = new List<int>();
                    
                    if (!variantUsage[variant].Contains(no))
                        variantUsage[variant].Add(no);
                        
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
                
                Debug.Log($"Y range: {minY} to {maxY}");
                Debug.Log("Variant usage:");
                foreach (var kvp in variantUsage.OrderBy(x => x.Key))
                {
                    kvp.Value.Sort();
                    Debug.Log($"  {kvp.Key}: tiles {string.Join(", ", kvp.Value)} (count: {kvp.Value.Count})");
                }
                
                // Sample some tiles to see what they look like
                Debug.Log("\nSample tiles:");
                int samples = 0;
                foreach (var tile in tileNode.Children)
                {
                    if (samples++ >= 5) break;
                    
                    string variant = tile["u"]?.GetValue<string>() ?? "";
                    int no = tile["no"]?.GetValue<int>() ?? 0;
                    int x = tile["x"]?.GetValue<int>() ?? 0;
                    int y = tile["y"]?.GetValue<int>() ?? 0;
                    
                    Debug.Log($"  Tile at ({x},{y}): {variant}/{no}");
                }
            }
            
            // Check the actual woodMarble tileset
            Debug.Log("\n=== woodMarble tileset contents ===");
            var woodMarble = nxManager.DataManager.GetNode("map", "Tile/woodMarble.img");
            if (woodMarble != null)
            {
                foreach (var variant in woodMarble.Children.Take(10))
                {
                    int tileCount = variant.Children.Count();
                    Debug.Log($"Variant {variant.Name}: {tileCount} tiles");
                }
            }
        }
    }
}