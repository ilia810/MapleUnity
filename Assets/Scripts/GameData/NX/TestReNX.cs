using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class TestReNX : MonoBehaviour
    {
        void Start()
        {
            TestNXStructure();
        }
        
        void TestNXStructure()
        {
            try
            {
                // Load a test NX file
                string testPath = @"C:\HeavenClient\MapleStory-Client\nx\Character.nx";
                using (var nxFile = new NXFile(testPath))
                {
                    Debug.Log($"=== Testing NX Structure ===");
                    Debug.Log($"Loaded: {testPath}");
                    
                    // Try different ways to access nodes
                    var rootNode = nxFile.BaseNode;
                    Debug.Log($"Root node type: {rootNode.GetType().FullName}");
                    
                    // Method 1: Look for 00002000.img
                    NXNode bodyImgNode = null;
                    foreach (var child in rootNode)
                    {
                        if (child.Name == "00002000.img")
                        {
                            bodyImgNode = child;
                            break;
                        }
                    }
                    
                    if (bodyImgNode != null)
                    {
                        Debug.Log($"Found 00002000.img, type: {bodyImgNode.GetType().FullName}");
                        
                        // Count total children
                        int totalChildren = 0;
                        foreach (var child in bodyImgNode)
                        {
                            totalChildren++;
                        }
                        Debug.Log($"Total children in 00002000.img: {totalChildren}");
                        
                        // List first few children
                        Debug.Log("First 20 children of 00002000.img:");
                        int count = 0;
                        foreach (var child in bodyImgNode)
                        {
                            Debug.Log($"- '{child.Name}' (type: {child.GetType().Name})");
                            if (++count >= 20) break;
                        }
                        
                        // Check if first child is a container
                        var firstChild = bodyImgNode.First();
                        if (firstChild != null)
                        {
                            Debug.Log($"\nExploring first child '{firstChild.Name}':");
                            count = 0;
                            foreach (var subchild in firstChild)
                            {
                                Debug.Log($"  - '{subchild.Name}'");
                                if (++count >= 10) break;
                            }
                        }
                        
                        // Try to get stand node
                        NXNode standNode = null;
                        foreach (var child in bodyImgNode)
                        {
                            if (child.Name == "stand")
                            {
                                standNode = child;
                                break;
                            }
                        }
                        
                        if (standNode != null)
                        {
                            Debug.Log($"Found stand node, type: {standNode.GetType().FullName}");
                            
                            // List frames
                            Debug.Log("Frames in stand:");
                            foreach (var frame in standNode)
                            {
                                Debug.Log($"- Frame {frame.Name}, type: {frame.GetType().FullName}");
                                
                                // List parts in frame 0
                                if (frame.Name == "0")
                                {
                                    Debug.Log("  Parts in frame 0:");
                                    foreach (var part in frame)
                                    {
                                        Debug.Log($"  - {part.Name}, type: {part.GetType().FullName}");
                                        
                                        // Check if it's a bitmap node
                                        var partType = part.GetType();
                                        if (partType.BaseType != null && partType.BaseType.IsGenericType)
                                        {
                                            var genArgs = partType.BaseType.GetGenericArguments();
                                            if (genArgs.Length > 0 && genArgs[0].Name == "Bitmap")
                                            {
                                                Debug.Log($"    This is a bitmap node!");
                                                
                                                // Try to get the bitmap
                                                var valueProperty = partType.GetProperty("Value");
                                                if (valueProperty != null)
                                                {
                                                    var bitmap = valueProperty.GetValue(part);
                                                    if (bitmap != null)
                                                    {
                                                        var widthProp = bitmap.GetType().GetProperty("Width");
                                                        var heightProp = bitmap.GetType().GetProperty("Height");
                                                        if (widthProp != null && heightProp != null)
                                                        {
                                                            var width = widthProp.GetValue(bitmap);
                                                            var height = heightProp.GetValue(bitmap);
                                                            Debug.Log($"    Bitmap size: {width}x{height}");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("stand node not found under 00002000.img (v83 uses stand1)"); // Force recompile
                            
                            // Try stand1 instead
                            NXNode stand1Node = null;
                            foreach (var child in bodyImgNode)
                            {
                                if (child.Name == "stand1")
                                {
                                    stand1Node = child;
                                    break;
                                }
                            }
                            
                            if (stand1Node != null)
                            {
                                Debug.Log("Found stand1 node!");
                                
                                // Get frame 0
                                NXNode frame0 = null;
                                foreach (var child in stand1Node)
                                {
                                    if (child.Name == "0")
                                    {
                                        frame0 = child;
                                        break;
                                    }
                                }
                                
                                if (frame0 != null)
                                {
                                    Debug.Log("Found frame 0 of stand1");
                                    
                                    // Look for body part
                                    foreach (var part in frame0)
                                    {
                                        if (part.Name == "body")
                                        {
                                            Debug.Log($"Found body part, type: {part.GetType().FullName}");
                                            Debug.Log($"Body has {part.Count()} children");
                                            
                                            // Test our updated RealNxNode that handles container nodes
                                            var realNxNode = new RealNxNode(part);
                                            var nodeValue = realNxNode.Value;
                                            if (nodeValue != null)
                                            {
                                                Debug.Log($"==> RealNxNode.Value returned type: {nodeValue.GetType().FullName}");
                                                if (nodeValue is byte[] bytes)
                                                {
                                                    Debug.Log($"==> SUCCESS! Got PNG data: {bytes.Length} bytes");
                                                    
                                                    // Try to load it as a sprite
                                                    var sprite = SpriteLoader.LoadSprite(realNxNode, "test/body");
                                                    if (sprite != null)
                                                    {
                                                        Debug.Log($"==> SPRITE LOADED! Size: {sprite.texture.width}x{sprite.texture.height}");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Debug.Log("==> RealNxNode.Value returned null");
                                            }
                                            
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("00002000.img not found");
                        
                        // List root children
                        Debug.Log("Root children:");
                        int count = 0;
                        foreach (var child in rootNode)
                        {
                            Debug.Log($"- {child.Name}");
                            if (++count >= 10) break;
                        }
                    }
                    
                    // Method 2: Try ResolvePath
                    Debug.Log("\nTrying ResolvePath method:");
                    try
                    {
                        // First check if stand exists, otherwise use stand1
                        var nodePath = "00002000.img/stand1/0";
                        // Check if path exists before resolving
                        if (rootNode["00002000.img"] != null)
                        {
                            var resolvedNode = nxFile.ResolvePath(nodePath);
                            if (resolvedNode != null)
                            {
                                Debug.Log($"ResolvePath worked for {nodePath}! Type: {resolvedNode.GetType().FullName}");
                            }
                        }
                        else
                        {
                            Debug.Log("00002000.img not found for ResolvePath test");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"ResolvePath failed: {ex.Message}");
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