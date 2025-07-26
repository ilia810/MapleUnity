using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    /// <summary>
    /// Tool to explore NX file structure
    /// </summary>
    public class NXExplorer : MonoBehaviour
    {
        void Start()
        {
            ExploreCharacterNX();
        }
        
        void ExploreCharacterNX()
        {
            try
            {
                string nxPath = @"C:\HeavenClient\MapleStory-Client\nx\Character.nx";
                using (var nxFile = new NXFile(nxPath))
                {
                    Debug.Log("=== NX Explorer: Character.nx ===");
                    
                    var rootNode = nxFile.BaseNode;
                    
                    // Look for body sprites
                    Debug.Log("\nSearching for body sprite structures...");
                    
                    // Check 00002000.img
                    var bodyNode = GetChildByName(rootNode, "00002000.img");
                    if (bodyNode != null)
                    {
                        Debug.Log("Found 00002000.img!");
                        ExploreNode(bodyNode, "00002000.img", 2);
                        
                        // Look for specific animations
                        string[] animations = { "stand", "stand1", "walk", "walk1", "jump", "alert" };
                        foreach (var anim in animations)
                        {
                            var animNode = GetChildByName(bodyNode, anim);
                            if (animNode != null)
                            {
                                Debug.Log($"\nFound animation '{anim}' in 00002000.img");
                                ExploreNode(animNode, $"00002000.img/{anim}", 2);
                            }
                        }
                        
                        // V92 structure check - explore first child to see if it contains animations
                        var firstCategory = bodyNode.First();
                        if (firstCategory != null)
                        {
                            Debug.Log($"\nExploring first category '{firstCategory.Name}' for v92 structure:");
                            
                            // Check if animations are under this category
                            foreach (var anim in animations)
                            {
                                var animUnderCategory = GetChildByName(firstCategory, anim);
                                if (animUnderCategory != null)
                                {
                                    Debug.Log($"  Found '{anim}' under category '{firstCategory.Name}'!");
                                    ExploreNode(animUnderCategory, $"00002000.img/{firstCategory.Name}/{anim}", 3);
                                    break; // Just check one to confirm structure
                                }
                            }
                        }
                    }
                    
                    // Check if body sprites might be elsewhere
                    Debug.Log("\nChecking root level for body-related nodes:");
                    int count = 0;
                    foreach (var child in rootNode)
                    {
                        if (child.Name.Contains("body") || child.Name.Contains("Body") || 
                            child.Name.StartsWith("00002") || child.Name.Contains("skin"))
                        {
                            Debug.Log($"Found: {child.Name}");
                            ExploreNode(child, child.Name, 1);
                        }
                        if (++count > 100) break; // Limit search
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"NX Explorer failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        void ExploreNode(NXNode node, string path, int maxDepth)
        {
            if (maxDepth <= 0) return;
            
            Debug.Log($"Exploring {path}:");
            Debug.Log($"  Type: {node.GetType().Name}");
            Debug.Log($"  Children: {node.Count()}");
            
            // Check node value
            try
            {
                var nodeType = node.GetType();
                if (nodeType.BaseType != null && nodeType.BaseType.IsGenericType)
                {
                    var valueProperty = nodeType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var value = valueProperty.GetValue(node);
                        if (value != null)
                        {
                            Debug.Log($"  Has value of type: {value.GetType().Name}");
                        }
                    }
                }
            }
            catch { }
            
            // List first few children
            if (node.Count() > 0)
            {
                Debug.Log("  First children:");
                int count = 0;
                foreach (var child in node)
                {
                    Debug.Log($"    - {child.Name}");
                    if (++count >= 5) break;
                }
                
                // Recursively explore first child if it looks interesting
                var firstChild = node.First();
                if (firstChild != null && maxDepth > 1)
                {
                    ExploreNode(firstChild, $"{path}/{firstChild.Name}", maxDepth - 1);
                }
            }
        }
        
        NXNode GetChildByName(NXNode parent, string name)
        {
            foreach (var child in parent)
            {
                if (child.Name == name)
                    return child;
            }
            return null;
        }
    }
}