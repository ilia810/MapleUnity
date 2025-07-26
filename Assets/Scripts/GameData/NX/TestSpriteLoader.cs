using System;
using System.Linq;
using UnityEngine;
using MapleClient.GameData;

namespace GameData
{
    /// <summary>
    /// Debug tool to test specific sprite loading paths
    /// </summary>
    public class TestSpriteLoader : MonoBehaviour
    {
        void Start()
        {
            TestSpecificSprites();
        }
        
        void TestSpecificSprites()
        {
            try
            {
                var nxManager = NXDataManagerSingleton.Instance;
                var dataManager = nxManager.DataManager;
                
                Debug.Log("=== Testing Specific Sprite Paths ===");
                
                // Test 1: Background sprite
                Debug.Log("\n--- Test 1: Background Sprite ---");
                TestPath(dataManager, "map", "Back/grassySoil.img/back/0", "Background grassySoil");
                
                // Test 2: NPC sprite  
                Debug.Log("\n--- Test 2: NPC Sprite ---");
                TestPath(dataManager, "npc", "9000036.img/stand/0", "NPC 9000036");
                
                // Test 3: Object sprite
                Debug.Log("\n--- Test 3: Object Sprite ---");
                TestPath(dataManager, "map", "Obj/houseGS.img/house9/basic/1", "Object house9");
                
                // Test Map.nx structure to understand how sprites are organized
                Debug.Log("\n--- Exploring Map.nx Structure ---");
                ExploreMapStructure(dataManager);
                
                // Test direct NX file access to understand bitmap storage
                Debug.Log("\n--- Testing Direct NX Access ---");
                TestDirectNXAccess();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        void TestPath(NXDataManager dataManager, string nxFile, string path, string description)
        {
            Debug.Log($"Testing {description} at path: {path}");
            
            var node = dataManager.GetNode(nxFile, path);
            if (node == null)
            {
                Debug.LogError($"Node not found at path: {path}");
                
                // Try to find parent nodes
                var pathParts = path.Split('/');
                string currentPath = "";
                foreach (var part in pathParts)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : currentPath + "/" + part;
                    var testNode = dataManager.GetNode(nxFile, currentPath);
                    if (testNode != null)
                    {
                        Debug.Log($"  Found node at: {currentPath}");
                        if (testNode.Children.Any())
                        {
                            Debug.Log($"  Children: {string.Join(", ", testNode.Children.Take(10).Select(c => c.Name))}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"  Failed at: {currentPath}");
                        break;
                    }
                }
                return;
            }
            
            Debug.Log($"Node found! Type: {node.GetType().Name}");
            Debug.Log($"Has children: {node.Children.Any()} (count: {node.Children.Count()})");
            
            // Check node value
            var nodeValue = node.Value;
            if (nodeValue != null)
            {
                Debug.Log($"Node.Value type: {nodeValue.GetType().FullName}");
                if (nodeValue is byte[] bytes)
                {
                    Debug.Log($"Node.Value is byte array: {bytes.Length} bytes");
                    
                    // Check PNG header
                    if (bytes.Length > 8)
                    {
                        bool isPng = bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
                        Debug.Log($"Is PNG: {isPng}");
                    }
                }
            }
            else
            {
                Debug.Log("Node.Value is null");
            }
            
            // Try GetValue<byte[]>
            try
            {
                var byteData = node.GetValue<byte[]>();
                if (byteData != null)
                {
                    Debug.Log($"GetValue<byte[]> returned: {byteData.Length} bytes");
                }
                else
                {
                    Debug.Log("GetValue<byte[]> returned null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GetValue<byte[]> failed: {ex.Message}");
            }
            
            // List children if any
            if (node.Children.Any())
            {
                Debug.Log("Children nodes:");
                foreach (var child in node.Children.Take(10))
                {
                    Debug.Log($"  - {child.Name}");
                }
            }
            
            // Try to load as sprite
            var sprite = SpriteLoader.LoadSprite(node, path);
            if (sprite != null)
            {
                Debug.Log($"SUCCESS! Sprite loaded: {sprite.texture.width}x{sprite.texture.height}");
            }
            else
            {
                Debug.LogError("Failed to load sprite");
            }
        }
        
        void ExploreMapStructure(NXDataManager dataManager)
        {
            // Check Back folder structure
            var backNode = dataManager.GetNode("map", "Back");
            if (backNode != null)
            {
                Debug.Log("Back folder children (first 10):");
                foreach (var child in backNode.Children.Take(10))
                {
                    Debug.Log($"  - {child.Name}");
                    
                    // Check grassySoil specifically
                    if (child.Name == "grassySoil.img")
                    {
                        Debug.Log("    grassySoil.img children:");
                        foreach (var subchild in child.Children.Take(10))
                        {
                            Debug.Log($"      - {subchild.Name}");
                            
                            if (subchild.Name == "back")
                            {
                                Debug.Log("        back children:");
                                foreach (var frame in subchild.Children.Take(5))
                                {
                                    Debug.Log($"          - {frame.Name} (Value type: {frame.Value?.GetType().Name ?? "null"})");
                                }
                            }
                        }
                    }
                }
            }
            
            // Check Obj folder structure
            var objNode = dataManager.GetNode("map", "Obj");
            if (objNode != null)
            {
                Debug.Log("\nObj folder children (first 10):");
                foreach (var child in objNode.Children.Take(10))
                {
                    Debug.Log($"  - {child.Name}");
                }
                
                // Check houseGS specifically
                var houseNode = dataManager.GetNode("map", "Obj/houseGS.img");
                if (houseNode != null)
                {
                    Debug.Log("\nhouseGS.img children:");
                    foreach (var child in houseNode.Children.Take(10))
                    {
                        Debug.Log($"  - {child.Name}");
                        
                        if (child.Name == "house9")
                        {
                            Debug.Log("    house9 children:");
                            foreach (var subchild in child.Children.Take(10))
                            {
                                Debug.Log($"      - {subchild.Name}");
                                
                                if (subchild.Name == "basic")
                                {
                                    Debug.Log("        basic children:");
                                    foreach (var frame in subchild.Children.Take(5))
                                    {
                                        Debug.Log($"          - {frame.Name} (Value type: {frame.Value?.GetType().Name ?? "null"})");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        void TestDirectNXAccess()
        {
            try
            {
                string mapNxPath = @"C:\HeavenClient\MapleStory-Client\nx\Map.nx";
                using (var realNxFile = new RealNxFile(mapNxPath))
                {
                    // Test grassySoil directly
                    var grassySoilNode = realNxFile.GetNode("Back/grassySoil.img/back/0");
                    if (grassySoilNode != null)
                    {
                        Debug.Log($"Direct access to grassySoil/back/0:");
                        Debug.Log($"  Node type: {grassySoilNode.GetType().Name}");
                        
                        var value = grassySoilNode.Value;
                        if (value != null)
                        {
                            Debug.Log($"  Value type: {value.GetType().FullName}");
                            if (value is byte[] bytes)
                            {
                                Debug.Log($"  Byte array length: {bytes.Length}");
                                
                                // Test sprite loading
                                var sprite = SpriteLoader.LoadSprite(grassySoilNode, "test");
                                if (sprite != null)
                                {
                                    Debug.Log($"  SPRITE LOADED! {sprite.texture.width}x{sprite.texture.height}");
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("  Value is null - checking for container pattern");
                            
                            // If this is a container node, it might have the bitmap as a child
                            if (grassySoilNode.Children.Any())
                            {
                                Debug.Log($"  Has {grassySoilNode.Children.Count()} children");
                                foreach (var child in grassySoilNode.Children)
                                {
                                    Debug.Log($"    Child: {child.Name}, Value type: {child.Value?.GetType().Name ?? "null"}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Could not find Back/grassySoil.img/back/0 via direct access");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct NX access failed: {ex.Message}");
            }
        }
    }
}