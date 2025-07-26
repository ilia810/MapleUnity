using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    public class DebugSpecificNode : MonoBehaviour
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
                    Debug.Log("=== Debugging Specific Body Node ===");
                    
                    // Navigate manually to avoid exceptions
                    var rootNode = nxFile.BaseNode;
                    
                    // Get 00002000.img
                    NXNode bodyImg = null;
                    foreach (var child in rootNode)
                    {
                        if (child.Name == "00002000.img")
                        {
                            bodyImg = child;
                            break;
                        }
                    }
                    
                    if (bodyImg == null)
                    {
                        Debug.LogError("00002000.img not found");
                        return;
                    }
                    
                    // Get stand1
                    NXNode stand1 = null;
                    foreach (var child in bodyImg)
                    {
                        if (child.Name == "stand1")
                        {
                            stand1 = child;
                            break;
                        }
                    }
                    
                    if (stand1 == null)
                    {
                        Debug.LogError("stand1 not found");
                        return;
                    }
                    
                    // Get frame 0
                    NXNode frame0 = null;
                    foreach (var child in stand1)
                    {
                        if (child.Name == "0")
                        {
                            frame0 = child;
                            break;
                        }
                    }
                    
                    if (frame0 == null)
                    {
                        Debug.LogError("frame 0 not found");
                        return;
                    }
                    
                    // Get body part
                    NXNode bodyPart = null;
                    foreach (var child in frame0)
                    {
                        if (child.Name == "body")
                        {
                            bodyPart = child;
                            break;
                        }
                    }
                    
                    if (bodyPart == null)
                    {
                        Debug.LogError("body part not found");
                        return;
                    }
                    
                    Debug.Log($"Found body part node, type: {bodyPart.GetType().FullName}");
                    Debug.Log($"Children count: {bodyPart.Count()}");
                    
                    // List all children with their types
                    foreach (var child in bodyPart)
                    {
                        Debug.Log($"Child: '{child.Name}' type: {child.GetType().FullName}");
                        
                        // Check if it's a valued node
                        var childType = child.GetType();
                        while (childType != null)
                        {
                            if (childType.IsGenericType && childType.GetGenericTypeDefinition().Name.StartsWith("NXValuedNode"))
                            {
                                Debug.Log($"  - This is an NXValuedNode!");
                                var genArgs = childType.GetGenericArguments();
                                if (genArgs.Length > 0)
                                {
                                    Debug.Log($"  - Generic argument: {genArgs[0].FullName}");
                                }
                                break;
                            }
                            childType = childType.BaseType;
                        }
                    }
                    
                    // Now test getting the value through our INxNode interface
                    var nxAssetLoader = NXAssetLoader.Instance;
                    var charFile = nxAssetLoader.GetNxFile("character");
                    var bodyNode = charFile.GetNode("00002000.img/stand1/0/body");
                    
                    if (bodyNode != null)
                    {
                        Debug.Log("\n=== Testing through INxNode interface ===");
                        Debug.Log($"INxNode type: {bodyNode.GetType().FullName}");
                        
                        try
                        {
                            var value = bodyNode.Value;
                            if (value != null)
                            {
                                Debug.Log($"Value type: {value.GetType().FullName}");
                                if (value is byte[] bytes)
                                {
                                    Debug.Log($"SUCCESS! Got PNG data: {bytes.Length} bytes");
                                }
                            }
                            else
                            {
                                Debug.Log("Value is null");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error getting value: {ex.Message}\n{ex.StackTrace}");
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