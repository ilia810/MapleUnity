using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class DebugTransparentTextures : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Transparent Textures")]
        public static void ShowWindow()
        {
            GetWindow<DebugTransparentTextures>("Debug Transparent Textures");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Debug Transparent Texture Issues", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check Problematic Texture Nodes"))
            {
                CheckProblematicNodes();
            }
        }
        
        private void CheckProblematicNodes()
        {
            Debug.Log("=== Checking Problematic Texture Nodes ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check some of the reported transparent textures
            string[] problematicPaths = {
                "Obj/houseGS.img/house9/deco/1",
                "Obj/door.img/grassySoil/door/19",
                "Obj/acc1.img/grassySoil/nature/25",
                "Npc/9000017.img/stand/0"
            };
            
            foreach (var path in problematicPaths)
            {
                Debug.Log($"\n--- Checking: {path} ---");
                
                var node = dataManager.GetNode("character", path);
                if (node == null)
                {
                    node = dataManager.GetNode("map", path);
                }
                
                if (node != null)
                {
                    Debug.Log($"Node found: {node.Name}");
                    Debug.Log($"  Has Value: {node.Value != null}");
                    Debug.Log($"  Value Type: {node.Value?.GetType().Name ?? "null"}");
                    Debug.Log($"  Children Count: {node.Children.Count()}");
                    
                    // Check if this is a container node
                    if (node.Value == null && node.Children.Any())
                    {
                        Debug.LogWarning("  This appears to be a CONTAINER node, not a bitmap!");
                        
                        // Look for the actual image data in children
                        foreach (var child in node.Children.Take(5))
                        {
                            Debug.Log($"    Child: {child.Name}");
                            Debug.Log($"      Has Value: {child.Value != null}");
                            Debug.Log($"      Value Type: {child.Value?.GetType().Name ?? "null"}");
                            
                            // Check for common image-related child names
                            if (child.Name == "0" || child.Name == "canvas" || child.Name == "source")
                            {
                                Debug.Log($"      ^ This might be the actual image!");
                            }
                        }
                    }
                    
                    // Check for alpha properties
                    var a0 = node["a0"];
                    var a1 = node["a1"];
                    if (a0 != null || a1 != null)
                    {
                        Debug.Log($"  Has alpha properties: a0={a0?.Value}, a1={a1?.Value}");
                    }
                    
                    // Check for origin
                    var origin = node["origin"];
                    if (origin != null)
                    {
                        Debug.Log($"  Has origin node");
                    }
                }
                else
                {
                    Debug.LogError($"Node not found at path: {path}");
                }
            }
        }
    }
}