using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class DebugTileOriginData : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug Tile Origin Data")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileOriginData>("Debug Origin Data");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check Origin Data in NX"))
            {
                CheckOriginData();
            }
        }
        
        private void CheckOriginData()
        {
            Debug.Log("=== Checking Origin Data in woodMarble Tileset ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check a specific tile that should have origin data
            var tileNode = dataManager.GetNode("map", "Tile/woodMarble.img/enH0/0");
            if (tileNode != null)
            {
                Debug.Log("Found enH0/0 tile node");
                
                // Check for origin node
                var originNode = tileNode["origin"];
                if (originNode != null)
                {
                    Debug.Log("  Found origin node!");
                    var x = originNode["x"];
                    var y = originNode["y"];
                    if (x != null) Debug.Log($"    x node exists, value: {x.GetValue<int>()}");
                    if (y != null) Debug.Log($"    y node exists, value: {y.GetValue<int>()}");
                }
                else
                {
                    Debug.Log("  No origin node found directly on tile");
                }
                
                // Check all children
                Debug.Log("  All children of tile node:");
                foreach (var child in tileNode.Children)
                {
                    Debug.Log($"    - {child.Name}: {child.Value?.ToString() ?? "complex node"}");
                }
                
                // Check if tile is a direct image
                var value = tileNode.Value;
                if (value != null)
                {
                    Debug.Log($"  Tile node has direct value of type: {value.GetType()}");
                }
                
                // Try parent node
                if (tileNode.Parent != null)
                {
                    Debug.Log("  Checking parent node for origin...");
                    var parentOrigin = tileNode.Parent["origin"];
                    if (parentOrigin != null)
                    {
                        Debug.Log("    Found origin on parent!");
                    }
                }
            }
            
            // Let's also check the raw structure
            Debug.Log("\n=== Raw Structure Check ===");
            var woodMarble = dataManager.GetNode("map", "Tile/woodMarble.img");
            if (woodMarble != null)
            {
                // Check info node
                var info = woodMarble["info"];
                if (info != null)
                {
                    Debug.Log("woodMarble has info node:");
                    foreach (var child in info.Children)
                    {
                        Debug.Log($"  - {child.Name}: {child.Value}");
                    }
                }
                
                // Check enH0 variant structure
                var enH0 = woodMarble["enH0"];
                if (enH0 != null)
                {
                    Debug.Log("\nenH0 variant structure:");
                    foreach (var child in enH0.Children.Take(3))
                    {
                        Debug.Log($"  Tile {child.Name}:");
                        if (child.Children.Any())
                        {
                            foreach (var subchild in child.Children.Take(5))
                            {
                                Debug.Log($"    - {subchild.Name}: {subchild.Value?.ToString() ?? "complex"}");
                            }
                        }
                        else
                        {
                            Debug.Log($"    Direct image node (no children)");
                        }
                    }
                }
            }
        }
    }
}