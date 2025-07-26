using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;

namespace MapleClient.Editor
{
    public class DebugNXNodeStructure : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug NX Node Structure")]
        public static void ShowWindow()
        {
            GetWindow<DebugNXNodeStructure>("NX Node Debug");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Inspect Tile Node Structure"))
            {
                InspectTileStructure();
            }
        }
        
        private void InspectTileStructure()
        {
            Debug.Log("=== Inspecting Tile Node Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check a specific enH0 tile that should have origin
            var tilePath = "Tile/woodMarble.img/enH0/0";
            var tileNode = dataManager.GetNode("map", tilePath);
            
            if (tileNode != null)
            {
                Debug.Log($"Found tile node at: {tilePath}");
                InspectNode(tileNode, "", 3);
                
                // Check if this is an image node
                var value = tileNode.Value;
                if (value != null)
                {
                    Debug.Log($"Node has direct value of type: {value.GetType()}");
                    if (value is byte[])
                    {
                        Debug.Log("  This is an image node (byte array)");
                    }
                }
                
                // Try to get properties in different ways
                Debug.Log("\n--- Checking for properties ---");
                
                // Check direct children
                foreach (var child in tileNode.Children.Take(10))
                {
                    Debug.Log($"Child: {child.Name} = {child.Value?.ToString() ?? "complex"}");
                }
                
                // Check parent structure
                if (tileNode.Parent != null)
                {
                    Debug.Log($"\nParent node: {tileNode.Parent.Name}");
                    foreach (var sibling in tileNode.Parent.Children.Take(5))
                    {
                        Debug.Log($"  Sibling: {sibling.Name}");
                    }
                }
                
                // Try the variant node (enH0)
                var variantNode = dataManager.GetNode("map", "Tile/woodMarble.img/enH0");
                if (variantNode != null)
                {
                    Debug.Log($"\n--- Variant node (enH0) structure ---");
                    foreach (var child in variantNode.Children.Take(5))
                    {
                        Debug.Log($"Tile {child.Name}:");
                        if (child["origin"] != null)
                        {
                            Debug.Log("  HAS ORIGIN!");
                        }
                        foreach (var prop in child.Children.Take(5))
                        {
                            Debug.Log($"    {prop.Name}: {prop.Value?.ToString() ?? "complex"}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"Could not find tile at: {tilePath}");
            }
        }
        
        private void InspectNode(INxNode node, string indent, int maxDepth)
        {
            if (maxDepth <= 0) return;
            
            Debug.Log($"{indent}Node: {node.Name}");
            Debug.Log($"{indent}  Type: {node.GetType().Name}");
            Debug.Log($"{indent}  Has Value: {node.Value != null}");
            Debug.Log($"{indent}  Children Count: {node.Children.Count()}");
            
            if (node.Value != null)
            {
                var valueType = node.Value.GetType();
                Debug.Log($"{indent}  Value Type: {valueType}");
                
                if (valueType.IsPrimitive || valueType == typeof(string))
                {
                    Debug.Log($"{indent}  Value: {node.Value}");
                }
                else if (node.Value is byte[] bytes)
                {
                    Debug.Log($"{indent}  Value: byte[{bytes.Length}]");
                }
            }
            
            // Check specific property access methods
            try
            {
                var originTest = node["origin"];
                if (originTest != null)
                {
                    Debug.Log($"{indent}  HAS ORIGIN NODE!");
                }
            }
            catch { }
            
            foreach (var child in node.Children.Take(5))
            {
                InspectNode(child, indent + "  ", maxDepth - 1);
            }
        }
    }
}