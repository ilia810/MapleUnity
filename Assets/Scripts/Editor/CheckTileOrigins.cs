using UnityEngine;
using UnityEditor;
using GameData;

namespace MapleClient.Editor
{
    public class CheckTileOrigins : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Check Tile Origins")]
        public static void ShowWindow()
        {
            GetWindow<CheckTileOrigins>("Tile Origins");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check woodMarble Tile Origins"))
            {
                CheckOrigins();
            }
        }
        
        private void CheckOrigins()
        {
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var woodMarble = dataManager.GetNode("map", "Tile/woodMarble.img");
            if (woodMarble == null)
            {
                Debug.LogError("woodMarble tileset not found!");
                return;
            }
            
            Debug.Log("=== woodMarble Tile Origins ===");
            
            string[] variants = { "bsc", "edD", "edU", "enH0", "enV0" };
            
            foreach (var variant in variants)
            {
                var variantNode = woodMarble[variant];
                if (variantNode == null) continue;
                
                Debug.Log($"\n--- Variant: {variant} ---");
                
                for (int i = 0; i < 5; i++)
                {
                    var tileNode = variantNode[i.ToString()];
                    if (tileNode == null) break;
                    
                    // Check for origin
                    var originNode = tileNode["origin"];
                    if (originNode != null)
                    {
                        var x = originNode["x"]?.GetValue<int>() ?? 0;
                        var y = originNode["y"]?.GetValue<int>() ?? 0;
                        Debug.Log($"  Tile {i}: origin=({x},{y})");
                    }
                    else
                    {
                        Debug.Log($"  Tile {i}: NO ORIGIN");
                    }
                    
                    // Check dimensions
                    var width = tileNode["width"]?.GetValue<int>() ?? 0;
                    var height = tileNode["height"]?.GetValue<int>() ?? 0;
                    if (width > 0 && height > 0)
                    {
                        Debug.Log($"    Size: {width}x{height}");
                    }
                }
            }
        }
    }
}