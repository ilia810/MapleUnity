using UnityEngine;
using UnityEditor;
using GameData;
using System.Reflection;
using System.Linq;
using MapleClient.GameData;

namespace MapleClient.Editor
{
    public class DebugOriginDataStructure : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug Origin Data Structure")]
        public static void ShowWindow()
        {
            GetWindow<DebugOriginDataStructure>("Origin Data Debug");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Inspect Origin Data"))
            {
                InspectOriginData();
            }
        }
        
        private void InspectOriginData()
        {
            Debug.Log("=== Inspecting Origin Data Structure ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Test with a known tile that should have origin
            var tilePath = "Tile/woodMarble.img/enH0/0";
            var tileNode = dataManager.GetNode("map", tilePath);
            
            if (tileNode != null)
            {
                Debug.Log($"Found tile at: {tilePath}");
                
                // Check if this is an image node
                var nodeValue = tileNode.Value;
                if (nodeValue != null && nodeValue is byte[])
                {
                    Debug.Log("This is an image node (PNG data)");
                    
                    // For image nodes in C++ client, origin is stored as a property
                    // Let's check all possible ways to access it
                    
                    // Method 1: Direct child access
                    var originChild = tileNode["origin"];
                    if (originChild != null)
                    {
                        Debug.Log("Found origin as child node!");
                        InspectOriginNode(originChild);
                    }
                    else
                    {
                        Debug.Log("No origin child node");
                    }
                    
                    // Method 2: Check if origin is stored at parent level
                    if (tileNode.Parent != null)
                    {
                        Debug.Log($"\nChecking parent node: {tileNode.Parent.Name}");
                        var parentOrigin = tileNode.Parent["origin"];
                        if (parentOrigin != null)
                        {
                            Debug.Log("Found origin on parent!");
                            InspectOriginNode(parentOrigin);
                        }
                    }
                    
                    // Method 3: Use reflection to see internal structure
                    Debug.Log("\n=== Using Reflection ===");
                    InspectNodeViaReflection(tileNode);
                    
                    // Method 4: Check if origin is embedded in the image data somehow
                    // This is how C++ client might handle it
                    Debug.Log("\n=== Checking Image Metadata ===");
                    var imageBytes = nodeValue as byte[];
                    if (imageBytes != null && imageBytes.Length > 100)
                    {
                        Debug.Log($"Image size: {imageBytes.Length} bytes");
                        // PNG files can have metadata chunks
                        CheckPNGMetadata(imageBytes);
                    }
                }
                
                // Let's also check a different structure
                Debug.Log("\n=== Checking Different Tile Types ===");
                CheckMultipleTiles(dataManager);
            }
        }
        
        private void InspectOriginNode(INxNode originNode)
        {
            Debug.Log($"Origin node type: {originNode.GetType().Name}");
            Debug.Log($"Origin node value: {originNode.Value?.ToString() ?? "null"}");
            Debug.Log($"Origin node children count: {originNode.Children.Count()}");
            
            // Check if it has x/y children
            var x = originNode["x"];
            var y = originNode["y"];
            
            if (x != null && y != null)
            {
                Debug.Log($"  x = {x.GetValue<int>()}, y = {y.GetValue<int>()}");
            }
            else
            {
                // Maybe origin is stored as a single value?
                var value = originNode.Value;
                if (value != null)
                {
                    Debug.Log($"  Origin value type: {value.GetType()}");
                    Debug.Log($"  Origin value: {value}");
                }
                
                // Try different ways to extract
                try
                {
                    var intArray = originNode.GetValue<int[]>();
                    if (intArray != null)
                    {
                        Debug.Log($"  As int array: [{string.Join(", ", intArray)}]");
                    }
                }
                catch { }
                
                try
                {
                    var pointStruct = originNode.GetValue<object>();
                    if (pointStruct != null)
                    {
                        Debug.Log($"  As object: {pointStruct} (type: {pointStruct.GetType()})");
                    }
                }
                catch { }
            }
        }
        
        private void InspectNodeViaReflection(INxNode node)
        {
            var type = node.GetType();
            Debug.Log($"Node type: {type.FullName}");
            
            // Get all properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(node);
                    if (value != null && prop.Name.ToLower().Contains("origin"))
                    {
                        Debug.Log($"  Property {prop.Name} = {value} (type: {value.GetType()})");
                    }
                }
                catch { }
            }
            
            // Get all fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(node);
                    if (value != null && field.Name.ToLower().Contains("origin"))
                    {
                        Debug.Log($"  Field {field.Name} = {value} (type: {value.GetType()})");
                    }
                }
                catch { }
            }
        }
        
        private void CheckPNGMetadata(byte[] pngData)
        {
            // PNG signature
            if (pngData[0] == 0x89 && pngData[1] == 0x50 && pngData[2] == 0x4E && pngData[3] == 0x47)
            {
                Debug.Log("Valid PNG signature found");
                
                // Look for chunks after IHDR
                int pos = 8; // Skip PNG signature
                while (pos < pngData.Length - 12)
                {
                    // Read chunk length (big endian)
                    int length = (pngData[pos] << 24) | (pngData[pos + 1] << 16) | 
                                (pngData[pos + 2] << 8) | pngData[pos + 3];
                    
                    // Read chunk type
                    string chunkType = System.Text.Encoding.ASCII.GetString(pngData, pos + 4, 4);
                    
                    if (chunkType != "IHDR" && chunkType != "PLTE" && chunkType != "IDAT" && chunkType != "IEND")
                    {
                        Debug.Log($"  Found chunk: {chunkType} (length: {length})");
                    }
                    
                    // Move to next chunk
                    pos += 12 + length; // 12 = length(4) + type(4) + crc(4)
                    
                    if (pos > pngData.Length - 12) break;
                }
            }
        }
        
        private void CheckMultipleTiles(NXDataManager dataManager)
        {
            string[] testPaths = {
                "Tile/woodMarble.img/bsc/0",
                "Tile/woodMarble.img/edD/0",
                "Tile/woodMarble.img/edU/0",
                "Tile/grassySoil.img/enH0/0"
            };
            
            foreach (var path in testPaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"\nChecking {path}:");
                    
                    // Check direct origin
                    var origin = node["origin"];
                    if (origin != null)
                    {
                        Debug.Log("  Has direct origin node");
                    }
                    
                    // Check in parent's info node
                    if (node.Parent?.Parent != null)
                    {
                        var info = node.Parent.Parent["info"];
                        if (info != null && info["origin"] != null)
                        {
                            Debug.Log("  Has origin in parent's info node");
                        }
                    }
                    
                    // List all children
                    var children = node.Children.Take(10).Select(c => c.Name).ToArray();
                    if (children.Any())
                    {
                        Debug.Log($"  Children: {string.Join(", ", children)}");
                    }
                }
            }
        }
    }
}