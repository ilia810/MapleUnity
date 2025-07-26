using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class TestBitmapExtraction : MonoBehaviour
    {
        void Start()
        {
            TestExtraction();
        }
        
        void TestExtraction()
        {
            try
            {
                string nxPath = @"C:\HeavenClient\MapleStory-Client\nx\Character.nx";
                using (var nxFile = new NXFile(nxPath))
                {
                    Debug.Log("=== Testing Bitmap Extraction ===");
                    
                    // Navigate to a known body part
                    var rootNode = nxFile.BaseNode;
                    
                    // Try to get to 00002000.img/stand1/0/body
                    NXNode current = rootNode;
                    string[] path = { "00002000.img", "stand1", "0", "body" };
                    
                    foreach (var part in path)
                    {
                        NXNode next = null;
                        foreach (var child in current)
                        {
                            if (child.Name == part)
                            {
                                next = child;
                                break;
                            }
                        }
                        
                        if (next == null)
                        {
                            Debug.LogError($"Failed to find '{part}' in path");
                            return;
                        }
                        current = next;
                    }
                    
                    Debug.Log($"Found body node! Type: {current.GetType().FullName}");
                    Debug.Log($"Children count: {current.Count()}");
                    
                    // List all children
                    Debug.Log("Body node children:");
                    foreach (var child in current)
                    {
                        Debug.Log($"  - '{child.Name}' type: {child.GetType().FullName}");
                        
                        // Check if this child has a bitmap value
                        var realChild = new RealNxNode(child);
                        var childValue = realChild.Value;
                        if (childValue != null)
                        {
                            Debug.Log($"    Child '{child.Name}' has value of type: {childValue.GetType().FullName}");
                            if (childValue is byte[] bytes)
                            {
                                Debug.Log($"    ==> BITMAP DATA FOUND! Size: {bytes.Length} bytes");
                            }
                        }
                    }
                    
                    // Test our RealNxNode wrapper
                    var realNode = new RealNxNode(current);
                    var value = realNode.Value;
                    if (value != null)
                    {
                        Debug.Log($"RealNxNode.Value returned: {value.GetType().FullName}");
                        if (value is byte[] pngData)
                        {
                            Debug.Log($"==> SUCCESS! Container handling worked. PNG size: {pngData.Length} bytes");
                        }
                    }
                    else
                    {
                        Debug.LogError("RealNxNode.Value returned null for container node");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}