using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;
using System;
using System.Reflection;
using System.Linq;

namespace MapleClient.Editor
{
    public class InspectNXNode : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Inspect NX Node Structure")]
        public static void ShowWindow()
        {
            GetWindow<InspectNXNode>("Inspect NX Node");
        }
        
        private string nodePath = "Obj/guide.img/common/post/0";
        
        private void OnGUI()
        {
            nodePath = EditorGUILayout.TextField("Node Path:", nodePath);
            
            if (GUILayout.Button("Inspect Node"))
            {
                InspectNode();
            }
        }
        
        private void InspectNode()
        {
            Debug.Log($"=== INSPECTING NX NODE: {nodePath} ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var node = dataManager.GetNode("map", nodePath);
            if (node == null)
            {
                Debug.LogError($"Node not found: {nodePath}");
                return;
            }
            
            Debug.Log($"Node type: {node.GetType().FullName}");
            
            // Check parent node
            if (node.Parent != null)
            {
                Debug.Log($"\n=== PARENT NODE: {node.Parent.Name} ===");
                var parentOrigin = node.Parent["origin"];
                if (parentOrigin != null)
                {
                    Debug.Log($"Parent has origin child! Value: {parentOrigin.Value}");
                }
                else
                {
                    Debug.Log($"Parent has NO origin child");
                }
            }
            
            // If it's a RealNxNode, get the underlying NX node
            if (node is RealNxNode realNode)
            {
                var nxNodeField = realNode.GetType().GetField("nxNode", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nxNodeField != null)
                {
                    var nxNode = nxNodeField.GetValue(realNode);
                    if (nxNode != null)
                    {
                        Debug.Log($"\nUnderlying NX node type: {nxNode.GetType().FullName}");
                        InspectObject(nxNode, "  ");
                    }
                }
            }
        }
        
        private void InspectObject(object obj, string indent)
        {
            if (obj == null) return;
            
            var type = obj.GetType();
            
            // Get all properties
            Debug.Log($"{indent}=== PROPERTIES ===");
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    var valueStr = value?.ToString() ?? "null";
                    if (value is byte[] bytes)
                    {
                        valueStr = $"byte[{bytes.Length}]";
                    }
                    Debug.Log($"{indent}{prop.Name} ({prop.PropertyType.Name}): {valueStr}");
                    
                    // Special handling for properties that might contain origin
                    if (prop.Name.ToLower().Contains("origin") || 
                        prop.Name.ToLower().Contains("point") ||
                        prop.Name.ToLower().Contains("offset"))
                    {
                        Debug.Log($"{indent}  *** POTENTIAL ORIGIN PROPERTY ***");
                        if (value != null && value.GetType().Name == "Point")
                        {
                            var x = value.GetType().GetProperty("X")?.GetValue(value);
                            var y = value.GetType().GetProperty("Y")?.GetValue(value);
                            Debug.Log($"{indent}  Point value: ({x}, {y})");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"{indent}{prop.Name}: ERROR - {e.Message}");
                }
            }
            
            // Get all fields
            Debug.Log($"{indent}=== FIELDS ===");
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    var valueStr = value?.ToString() ?? "null";
                    if (value is byte[] bytes)
                    {
                        valueStr = $"byte[{bytes.Length}]";
                    }
                    Debug.Log($"{indent}{field.Name} ({field.FieldType.Name}): {valueStr}");
                    
                    // Special handling for fields that might contain origin
                    if (field.Name.ToLower().Contains("origin") || 
                        field.Name.ToLower().Contains("point") ||
                        field.Name.ToLower().Contains("offset"))
                    {
                        Debug.Log($"{indent}  *** POTENTIAL ORIGIN FIELD ***");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"{indent}{field.Name}: ERROR - {e.Message}");
                }
            }
            
            // Check for indexer
            Debug.Log($"{indent}=== INDEXER CHECK ===");
            var indexer = type.GetProperty("Item");
            if (indexer != null)
            {
                Debug.Log($"{indent}Has indexer, trying to access 'origin'...");
                try
                {
                    var originChild = indexer.GetValue(obj, new object[] { "origin" });
                    if (originChild != null)
                    {
                        Debug.Log($"{indent}Found 'origin' child via indexer!");
                        Debug.Log($"{indent}Origin child type: {originChild.GetType().FullName}");
                    }
                    else
                    {
                        Debug.Log($"{indent}No 'origin' child found via indexer");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log($"{indent}Indexer access failed: {e.Message}");
                }
            }
            
            // Try to enumerate children
            Debug.Log($"{indent}=== ENUMERATION CHECK ===");
            if (obj is System.Collections.IEnumerable enumerable)
            {
                Debug.Log($"{indent}Node is enumerable, checking children:");
                int count = 0;
                foreach (var child in enumerable)
                {
                    if (child != null && count < 10)
                    {
                        var childType = child.GetType();
                        var nameProp = childType.GetProperty("Name");
                        if (nameProp != null)
                        {
                            var name = nameProp.GetValue(child) as string;
                            Debug.Log($"{indent}  Child: {name} (Type: {childType.Name})");
                        }
                    }
                    count++;
                }
                Debug.Log($"{indent}Total children: {count}");
            }
        }
    }
}