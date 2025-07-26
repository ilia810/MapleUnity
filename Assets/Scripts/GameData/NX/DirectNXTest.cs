using System;
using System.Linq;
using UnityEngine;
using reNX;
using reNX.NXProperties;

namespace MapleClient.GameData
{
    /// <summary>
    /// Direct test of NX bitmap loading without our wrapper classes
    /// </summary>
    public class DirectNXTest : MonoBehaviour
    {
        void Start()
        {
            TestDirectBitmapAccess();
            TestImprovedBitmapExtraction();
        }
        
        void TestDirectBitmapAccess()
        {
            try
            {
                Debug.Log("=== Direct NX Bitmap Access Test ===");
                
                string mapNxPath = @"C:\HeavenClient\MapleStory-Client\nx\Map.nx";
                using (var nxFile = new NXFile(mapNxPath))
                {
                    // Test 1: Back/grassySoil.img/back/0
                    Debug.Log("\n--- Testing grassySoil background ---");
                    TestBitmapPath(nxFile, "Back/grassySoil.img/back/0");
                    
                    // Test 2: Try without the /0 
                    Debug.Log("\n--- Testing grassySoil/back ---");
                    TestBitmapPath(nxFile, "Back/grassySoil.img/back");
                    
                    // Test 3: Try the img node directly
                    Debug.Log("\n--- Testing grassySoil.img ---");
                    TestBitmapPath(nxFile, "Back/grassySoil.img");
                    
                    // Test 4: Object sprite
                    Debug.Log("\n--- Testing object sprite ---");
                    TestBitmapPath(nxFile, "Obj/houseGS.img/house9/basic/1");
                }
                
                // Test NPC file
                string npcNxPath = @"C:\HeavenClient\MapleStory-Client\nx\Npc.nx";
                using (var nxFile = new NXFile(npcNxPath))
                {
                    Debug.Log("\n--- Testing NPC sprite ---");
                    TestBitmapPath(nxFile, "9000036.img/stand/0");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct NX test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        void TestBitmapPath(NXFile nxFile, string path)
        {
            try
            {
                Debug.Log($"Testing path: {path}");
                
                // Navigate to the node
                var parts = path.Split('/');
                NXNode current = nxFile.BaseNode;
                
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    
                    bool found = false;
                    foreach (var child in current)
                    {
                        if (child.Name == part)
                        {
                            current = child;
                            found = true;
                            Debug.Log($"  Found: {part} (type: {child.GetType().Name})");
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogError($"  Not found: {part}");
                        return;
                    }
                }
                
                // Analyze the final node
                Debug.Log($"Final node: {current.Name}, Type: {current.GetType().FullName}");
                
                // Check if it's a bitmap node by checking the generic type
                var nodeType = current.GetType();
                if (nodeType.IsGenericType)
                {
                    var genArgs = nodeType.GetGenericArguments();
                    if (genArgs.Length > 0 && genArgs[0].Name.Contains("Bitmap"))
                    {
                        Debug.Log("  This IS a bitmap node!");
                        
                        // Get the Value property through reflection
                        var valueProperty = nodeType.GetProperty("Value");
                        if (valueProperty != null)
                        {
                            var bitmap = valueProperty.GetValue(current);
                            if (bitmap != null)
                            {
                                Debug.Log($"  Bitmap value found: {bitmap.GetType().FullName}");
                                
                                // Try to get Width and Height through reflection
                                var bitmapType = bitmap.GetType();
                                var widthProp = bitmapType.GetProperty("Width");
                                var heightProp = bitmapType.GetProperty("Height");
                                
                                if (widthProp != null && heightProp != null)
                                {
                                    int width = (int)widthProp.GetValue(bitmap);
                                    int height = (int)heightProp.GetValue(bitmap);
                                    Debug.Log($"  Bitmap size: {width}x{height}");
                                }
                                
                                // The PNG extraction is handled by RealNxFile.ExtractPngFromBitmap
                                var pngData = RealNxFile.ExtractPngFromBitmap(bitmap, current.Name);
                                if (pngData != null && pngData.Length > 0)
                                {
                                    Debug.Log($"  PNG data extracted: {pngData.Length} bytes");
                                    
                                    // Verify PNG header
                                    if (pngData.Length > 8 && pngData[0] == 0x89 && pngData[1] == 0x50)
                                    {
                                        Debug.Log("  Valid PNG header confirmed!");
                                        
                                        // Try to load in Unity
                                        var texture = new Texture2D(2, 2);
                                        if (texture.LoadImage(pngData))
                                        {
                                            Debug.Log($"  Unity loaded texture: {texture.width}x{texture.height}");
                                            
                                            // Check pixel colors
                                            var topLeft = texture.GetPixel(0, 0);
                                            var center = texture.GetPixel(texture.width/2, texture.height/2);
                                            Debug.Log($"  Sample pixels - TopLeft: {topLeft}, Center: {center}");
                                        }
                                        UnityEngine.Object.Destroy(texture);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("  Bitmap value is null!");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"  Not a bitmap node. Actual type: {current.GetType().FullName}");
                    
                    // Check base type
                    var baseType = current.GetType().BaseType;
                    if (baseType != null)
                    {
                        Debug.Log($"  Base type: {baseType.FullName}");
                        if (baseType.IsGenericType)
                        {
                            var genArgs = baseType.GetGenericArguments();
                            Debug.Log($"  Generic args: {string.Join(", ", genArgs.Select(t => t.FullName))}");
                        }
                    }
                    
                    // List children if any
                    if (current.Count() > 0)
                    {
                        Debug.Log($"  Has {current.Count()} children:");
                        foreach (var child in current.Take(5))
                        {
                            Debug.Log($"    - {child.Name} ({child.GetType().Name})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error testing path {path}: {ex.Message}");
            }
        }
        
        void TestImprovedBitmapExtraction()
        {
            try
            {
                Debug.Log("\n=== Testing Improved Bitmap Extraction ===");
                
                // Test using our improved RealNxFile
                using (var realNxFile = new RealNxFile(@"C:\HeavenClient\MapleStory-Client\nx\Map.nx"))
                {
                    // Test 1: grassySoil background
                    Debug.Log("\n--- Test 1: grassySoil via RealNxFile ---");
                    var grassySoilNode = realNxFile.GetNode("Back/grassySoil.img/back/0");
                    if (grassySoilNode != null)
                    {
                        Debug.Log($"Found node: {grassySoilNode.Name}");
                        var value = grassySoilNode.Value;
                        if (value != null)
                        {
                            Debug.Log($"Value type: {value.GetType().Name}");
                            if (value is byte[] bytes)
                            {
                                Debug.Log($"✓ Got PNG data: {bytes.Length} bytes");
                                
                                // Verify PNG header
                                if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50)
                                {
                                    Debug.Log("✓ Valid PNG header");
                                    
                                    // Try to load as texture
                                    var texture = new Texture2D(2, 2);
                                    if (texture.LoadImage(bytes))
                                    {
                                        Debug.Log($"✓ Loaded as texture: {texture.width}x{texture.height}");
                                        var color = texture.GetPixel(128, 128);
                                        Debug.Log($"  Center color: {color}");
                                        UnityEngine.Object.Destroy(texture);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("✗ Node value is null");
                        }
                    }
                    
                    // Test 2: Object sprite
                    Debug.Log("\n--- Test 2: Object sprite via RealNxFile ---");
                    var objNode = realNxFile.GetNode("Obj/houseGS.img/house9/basic/1");
                    if (objNode != null)
                    {
                        var value = objNode.Value;
                        if (value is byte[] bytes)
                        {
                            Debug.Log($"✓ Got object PNG data: {bytes.Length} bytes");
                        }
                        else
                        {
                            Debug.LogError($"✗ Object value type: {value?.GetType().Name ?? "null"}");
                        }
                    }
                    
                    // Test 3: Test full sprite loading
                    Debug.Log("\n--- Test 3: Full sprite loading ---");
                    var sprite = SpriteLoader.LoadSprite(grassySoilNode, "test");
                    if (sprite != null)
                    {
                        Debug.Log($"✓ SpriteLoader successfully created sprite: {sprite.texture.width}x{sprite.texture.height}");
                    }
                    else
                    {
                        Debug.LogError("✗ SpriteLoader failed");
                    }
                }
                
                Debug.Log("\n=== Improved Bitmap Extraction Test Complete ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Improved bitmap extraction test failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}