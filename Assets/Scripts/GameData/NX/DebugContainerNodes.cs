using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class DebugContainerNodes : MonoBehaviour
    {
        void Start()
        {
            DebugBodyNode();
        }
        
        void DebugBodyNode()
        {
            try
            {
                string nxPath = @"C:\HeavenClient\MapleStory-Client\nx\Character.nx";
                using (var nxFile = new NXFile(nxPath))
                {
                    Debug.Log("=== Debugging Container Nodes ===");
                    
                    // Navigate to body node that has 4 children
                    var bodyNode = nxFile.ResolvePath("00002000.img/stand1/0/body");
                    if (bodyNode == null)
                    {
                        Debug.LogError("Failed to find body node");
                        return;
                    }
                    
                    Debug.Log($"Found body node, type: {bodyNode.GetType().FullName}");
                    Debug.Log($"Children count: {bodyNode.Count()}");
                    
                    // Examine each child in detail
                    int childIndex = 0;
                    foreach (var child in bodyNode)
                    {
                        Debug.Log($"\nChild {childIndex}: '{child.Name}'");
                        Debug.Log($"  Type: {child.GetType().FullName}");
                        
                        // Check the actual type
                        var childType = child.GetType();
                        Debug.Log($"  Base type: {childType.BaseType?.FullName ?? "null"}");
                        
                        // Try to get value using reflection
                        var valueProperty = childType.GetProperty("Value");
                        if (valueProperty != null)
                        {
                            try
                            {
                                var value = valueProperty.GetValue(child);
                                if (value != null)
                                {
                                    Debug.Log($"  Value type: {value.GetType().FullName}");
                                    
                                    // Check if it's a bitmap
                                    if (value.GetType().Name == "Bitmap")
                                    {
                                        Debug.Log("  ==> This child contains a Bitmap!");
                                        
                                        // Try to extract PNG data
                                        var pngData = RealNxFile.ExtractPngFromBitmap(value, child.Name);
                                        if (pngData != null)
                                        {
                                            Debug.Log($"  ==> PNG extracted! Size: {pngData.Length} bytes");
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log("  Value is null");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Log($"  Error getting value: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.Log("  No Value property found");
                        }
                        
                        // Check if this child has more children
                        if (child.Count() > 0)
                        {
                            Debug.Log($"  This child has {child.Count()} sub-children");
                        }
                        
                        childIndex++;
                    }
                    
                    // Now test our RealNxNode wrapper
                    Debug.Log("\n=== Testing RealNxNode wrapper ===");
                    var realNode = new RealNxNode(bodyNode);
                    Debug.Log($"RealNxNode type check...");
                    var nodeValue = realNode.Value;
                    if (nodeValue != null)
                    {
                        Debug.Log($"RealNxNode.Value returned: {nodeValue.GetType().FullName}");
                        if (nodeValue is byte[] bytes)
                        {
                            Debug.Log($"SUCCESS! Got PNG data: {bytes.Length} bytes");
                        }
                    }
                    else
                    {
                        Debug.LogError("RealNxNode.Value returned null");
                        
                        // Debug why
                        Debug.Log("Checking GetChildSafe...");
                        // Check if we can manually extract
                        var child0 = bodyNode.FirstOrDefault(c => c.Name == "0");
                        if (child0 != null)
                        {
                            Debug.Log("Found child '0' manually");
                            var child0Real = new RealNxNode(child0);
                            var child0Value = child0Real.Value;
                            if (child0Value != null)
                            {
                                Debug.Log($"Child '0' value type: {child0Value.GetType().FullName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Debug failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}