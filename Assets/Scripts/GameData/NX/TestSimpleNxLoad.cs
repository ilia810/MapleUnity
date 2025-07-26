using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class TestSimpleNxLoad : MonoBehaviour
    {
        void Start()
        {
            TestDirectNxAccess();
        }
        
        void TestDirectNxAccess()
        {
            try
            {
                string nxPath = @"C:\HeavenClient\MapleStory-Client\nx\Character.nx";
                using (var nxFile = new NXFile(nxPath))
                {
                    Debug.Log("=== Testing Direct NX Access ===");
                    
                    // Navigate to a known sprite location
                    var rootNode = nxFile.BaseNode;
                    
                    // Try to access 00002000.img/stand1/0/body/0 directly
                    NXNode current = rootNode;
                    string[] path = { "00002000.img", "stand1", "0", "body", "0" };
                    
                    foreach (var segment in path)
                    {
                        NXNode next = null;
                        foreach (var child in current)
                        {
                            if (child.Name == segment)
                            {
                                next = child;
                                break;
                            }
                        }
                        
                        if (next == null)
                        {
                            Debug.LogError($"Could not find segment: {segment}");
                            return;
                        }
                        
                        current = next;
                        Debug.Log($"Found segment: {segment}, type: {current.GetType().FullName}");
                    }
                    
                    // Now we should be at the sprite node
                    Debug.Log($"\nFinal node: {current.Name}, type: {current.GetType().FullName}");
                    
                    // Check if it's a valued node
                    var nodeType = current.GetType();
                    Debug.Log($"Node type name: {nodeType.Name}");
                    Debug.Log($"Is generic: {nodeType.IsGenericType}");
                    
                    if (nodeType.BaseType != null)
                    {
                        Debug.Log($"Base type: {nodeType.BaseType.FullName}");
                        Debug.Log($"Base type is generic: {nodeType.BaseType.IsGenericType}");
                        
                        if (nodeType.BaseType.IsGenericType)
                        {
                            var genArgs = nodeType.BaseType.GetGenericArguments();
                            foreach (var arg in genArgs)
                            {
                                Debug.Log($"Generic argument: {arg.FullName}");
                            }
                        }
                    }
                    
                    // Try to get the value
                    var valueProperty = nodeType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        Debug.Log($"Found Value property, type: {valueProperty.PropertyType.FullName}");
                        
                        try
                        {
                            var value = valueProperty.GetValue(current);
                            if (value != null)
                            {
                                Debug.Log($"Value is not null, type: {value.GetType().FullName}");
                                
                                // If it's a bitmap, try to save it
                                if (value.GetType().Name == "Bitmap")
                                {
                                    Debug.Log("This is a Bitmap!");
                                    
                                    // Try to convert to PNG
                                    var pngData = RealNxFile.ExtractPngFromBitmap(value, current.Name);
                                    if (pngData != null)
                                    {
                                        Debug.Log($"SUCCESS! Extracted PNG data: {pngData.Length} bytes");
                                        
                                        // Verify it's a valid PNG
                                        if (pngData.Length > 8)
                                        {
                                            bool isPng = pngData[0] == 0x89 && pngData[1] == 0x50 && 
                                                        pngData[2] == 0x4E && pngData[3] == 0x47;
                                            Debug.Log($"PNG header check: {isPng}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("Value is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error getting value: {ex.Message}");
                            if (ex.InnerException != null)
                            {
                                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No Value property found");
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