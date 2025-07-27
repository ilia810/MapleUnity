using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameData;

namespace MapleClient.GameData
{
    /// <summary>
    /// Handles loading and converting sprites from NX data to Unity sprites
    /// </summary>
    public static class SpriteLoader
    {
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static Dictionary<string, SpriteWithOrigin> spriteWithOriginCache = new Dictionary<string, SpriteWithOrigin>();
        
        /// <summary>
        /// Load sprite and origin together from the same node
        /// </summary>
        public static SpriteWithOrigin LoadSpriteWithOrigin(INxNode node, string path = null, NXDataManager dataManager = null)
        {
            if (node == null) return null;
            
            string cacheKey = path ?? node.Name;
            if (spriteWithOriginCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            
            // Find the actual image node and load both sprite and origin from it
            var result = LoadSpriteAndOriginFromNode(node, cacheKey, dataManager);
            if (result != null)
            {
                spriteWithOriginCache[cacheKey] = result;
                return result;
            }
            
            // Try common child nodes
            string[] imageChildNames = { "canvas", "0", "source" };
            foreach (var childName in imageChildNames)
            {
                var childNode = node[childName];
                if (childNode != null)
                {
                    result = LoadSpriteAndOriginFromNode(childNode, cacheKey, dataManager);
                    if (result != null)
                    {
                        spriteWithOriginCache[cacheKey] = result;
                        return result;
                    }
                }
            }
            
            // Try numbered frames
            if (node.Children.Any())
            {
                var firstChild = node.Children.FirstOrDefault();
                if (firstChild != null && int.TryParse(firstChild.Name, out _))
                {
                    return LoadSpriteWithOrigin(firstChild, path + "/" + firstChild.Name, dataManager);
                }
            }
            
            Debug.LogWarning($"Could not find sprite data at: {path}");
            return null;
        }
        
        /// <summary>
        /// Load a sprite from an NX node (for backward compatibility)
        /// </summary>
        public static Sprite LoadSprite(INxNode node, string path = null, NXDataManager dataManager = null)
        {
            if (node == null) return null;
            
            // Resolve any outlinks to get the actual sprite data
            if (dataManager != null)
            {
                node = node.ResolveOutlink(dataManager);
            }
            
            string nodePath = path ?? node.Name;
            
            // Check cache first
            string cacheKey = nodePath;
            if (spriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            
            // Based on C++ client, nodes can be directly bitmap nodes
            // Check if this node has bitmap data (Value property)
            var nodeValue = node.Value;
            if (nodeValue != null)
            {
                // Check if it's byte array (PNG data)
                if (nodeValue is byte[] bytes)
                {
                    var sprite = ConvertNodeToSprite(node, cacheKey);
                    if (sprite != null)
                    {
                        spriteCache[cacheKey] = sprite;
                        return sprite;
                    }
                }
            }
            
            // Try GetValue<T> method which might handle data extraction differently
            try
            {
                var imageData = node.GetValue<byte[]>();
                if (imageData != null && imageData.Length > 0)
                {
                    var sprite = ConvertNodeToSprite(node, cacheKey);
                    if (sprite != null)
                    {
                        spriteCache[cacheKey] = sprite;
                        return sprite;
                    }
                }
            }
            catch { }
            
            // Some nodes might have the image in a child node
            // Try common child names
            string[] imageChildNames = { "canvas", "0", "source" };
            foreach (var childName in imageChildNames)
            {
                var childNode = node[childName];
                if (childNode != null)
                {
                    var childValue = childNode.Value;
                    if (childValue != null)
                    {
                        var sprite = ConvertNodeToSprite(childNode, cacheKey);
                        if (sprite != null)
                        {
                            spriteCache[cacheKey] = sprite;
                            return sprite;
                        }
                    }
                }
            }
            
            // For numbered frames (animations)
            if (node.Children.Any())
            {
                var firstChild = node.Children.FirstOrDefault();
                if (firstChild != null && int.TryParse(firstChild.Name, out _))
                {
                    return LoadSprite(firstChild, nodePath + "/" + firstChild.Name);
                }
            }
            
            // Last resort - check if this node itself might be an image even without obvious markers
            var finalSprite = ConvertNodeToSprite(node, cacheKey);
            if (finalSprite != null)
            {
                spriteCache[cacheKey] = finalSprite;
                return finalSprite;
            }
            
            Debug.LogWarning($"Could not find sprite data at: {nodePath} (has {node.Children.Count()} children)");
            return null;
        }
        
        /// <summary>
        /// Load animation frames from an NX node
        /// </summary>
        public static Sprite[] LoadAnimationFrames(INxNode node)
        {
            if (node == null) return null;
            
            var frames = new List<Sprite>();
            int frameIndex = 0;
            
            // Keep loading numbered frames until we can't find any more
            while (true)
            {
                var frameNode = node[frameIndex.ToString()];
                if (frameNode == null)
                    break;
                    
                var sprite = LoadSprite(frameNode, $"{node.Name}/{frameIndex}");
                if (sprite != null)
                    frames.Add(sprite);
                    
                frameIndex++;
            }
            
            return frames.Count > 0 ? frames.ToArray() : null;
        }
        
        /// <summary>
        /// Load sprite and origin from the same image node
        /// </summary>
        private static SpriteWithOrigin LoadSpriteAndOriginFromNode(INxNode node, string name, NXDataManager dataManager)
        {
            if (node == null) return null;
            
            // Resolve any outlinks
            if (dataManager != null)
            {
                node = node.ResolveOutlink(dataManager);
            }
            
            
            // For objects, the structure is often:
            // Obj/guide.img/common/post/0 (container node with origin)
            //   └── 0 (child node with actual image data)
            
            // Check if THIS node is the container with origin
            Vector2 origin = Vector2.zero;
            INxNode imageNode = node;
            INxNode containerNode = node; // Keep reference to original container
            
            // First, check if this node has an origin child
            var originNode = node["origin"];
            if (originNode != null && originNode.Value is Vector2 vec)
            {
                origin = vec;
            }
            
            // Check if this node has image data directly
            var value = node.Value;
            bool hasDirectImageData = (value != null && value is byte[]);
            
            if (!hasDirectImageData)
            {
                // Try GetValue<byte[]>
                try
                {
                    var imageData = node.GetValue<byte[]>();
                    hasDirectImageData = (imageData != null && imageData.Length > 0);
                }
                catch
                {
                    hasDirectImageData = false;
                }
            }
            
            // If no direct image data, look for child nodes with image data
            if (!hasDirectImageData)
            {
                // For objects, check if there's a "0" child with the actual image
                var imageChild = node["0"] ?? node["1"] ?? node["canvas"];
                if (imageChild != null)
                {
                    // The origin stays at the container level, but image comes from child
                    imageNode = imageChild;
                    
                    // If we didn't find origin at container level, check the image node
                    // BUT ALSO check the parent container if the child has no origin
                    if (origin == Vector2.zero)
                    {
                        var imageOrigin = imageChild["origin"];
                        if (imageOrigin != null && imageOrigin.Value is Vector2 imgVec)
                        {
                            origin = imgVec;
                        }
                        else
                        {
                            // Check parent node for origin (container pattern)
                            var parentOrigin = containerNode["origin"];
                            if (parentOrigin != null && parentOrigin.Value is Vector2 parentVec)
                            {
                                origin = parentVec;
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // This node has image data directly (like tiles)
                // Origin should be at this same level
            }
            
            // If we still haven't found an origin and this is a different node than the original
            // check the image node for origin (for cases where origin is with the image data)
            if (origin == Vector2.zero && imageNode != node)
            {
                var imgOriginNode = imageNode["origin"];
                if (imgOriginNode != null)
                {
                    var originValue = imgOriginNode.Value;
                    if (originValue is Vector2 vec2)
                    {
                        origin = vec2;
                    }
                }
            }
            
            // Method 2: Check parent node for origin (common pattern for objects)
            if (origin == Vector2.zero && imageNode.Parent != null)
            {
                var parentOrigin = imageNode.Parent["origin"];
                if (parentOrigin != null && parentOrigin.Value is Vector2 parentVec)
                {
                    origin = parentVec;
                }
            }
            
            // Method 3: For NX nodes, properties might be stored differently
            // Try using reflection to access the underlying NX node's properties
            if (origin == Vector2.zero && imageNode is RealNxNode realNode)
            {;
                
                // Get the underlying NX node
                var nxNodeField = realNode.GetType().GetField("nxNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (nxNodeField != null)
                {
                    var nxNode = nxNodeField.GetValue(realNode);
                    if (nxNode != null)
                    {
                        
                        // For NX bitmap/canvas nodes, origin might be a direct property
                        var nxType = nxNode.GetType();
                        
                        // Check all properties on the NX node
                        var properties = nxType.GetProperties();
                        foreach (var prop in properties)
                        {
                            if (prop.Name.ToLower().Contains("origin"))
                            {
                                var propValue = prop.GetValue(nxNode);
                                if (propValue != null)
                                {
                                    // Found origin property
                                }
                            }
                        }
                        
                        // The NX library might store canvas properties including origin
                        // Check if this node has Canvas-related data
                        var canvasProperty = nxType.GetProperty("Canvas");
                        if (canvasProperty != null)
                        {
                            var canvas = canvasProperty.GetValue(nxNode);
                            if (canvas != null)
                            {
                                var canvasType = canvas.GetType();
                                var originProp = canvasType.GetProperty("Origin") ?? canvasType.GetProperty("origin");
                                if (originProp != null)
                                {
                                    var originVal = originProp.GetValue(canvas);
                                    if (originVal != null)
                                    {
                                        // Convert to Vector2
                                        var originValType = originVal.GetType();
                                        if (originValType.Name == "Point")
                                        {
                                            var x = (int)(originValType.GetProperty("X")?.GetValue(originVal) ?? 0);
                                            var y = (int)(originValType.GetProperty("Y")?.GetValue(originVal) ?? 0);
                                            origin = new Vector2(x, y);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // If still no origin found, try C++ wrapper as last resort
            if (origin == Vector2.zero && dataManager != null)
            {
                // Build path for C++ wrapper
                string nxFile = "Map.nx"; // Default to Map.nx for objects
                string nodePath = "";
                
                // Method 1: Extract from the name parameter if it contains full path
                if (name.Contains("/"))
                {
                    nodePath = name;
                    // Remove any .nx prefix if present
                    if (nodePath.StartsWith("Map.nx/"))
                    {
                        nodePath = nodePath.Substring(7);
                    }
                    else if (name.Contains(".nx/"))
                    {
                        var parts = name.Split(new[] { ".nx/" }, StringSplitOptions.None);
                        if (parts.Length >= 2)
                        {
                            nxFile = parts[0] + ".nx";
                            nodePath = parts[1];
                        }
                    }
                }
                else
                {
                    // Method 2: Build from node hierarchy
                    var currentNode = containerNode;
                    var pathParts = new List<string>();
                    
                    // Walk up the tree to build the full path
                    while (currentNode != null)
                    {
                        if (!string.IsNullOrEmpty(currentNode.Name))
                        {
                            pathParts.Insert(0, currentNode.Name);
                        }
                        currentNode = currentNode.Parent;
                    }
                    
                    // Remove the NX file name if it's at the beginning
                    if (pathParts.Count > 0 && pathParts[0].EndsWith(".nx"))
                    {
                        nxFile = pathParts[0];
                        pathParts.RemoveAt(0);
                    }
                    
                    nodePath = string.Join("/", pathParts);
                }
                
                if (!string.IsNullOrEmpty(nodePath))
                {
                    var cppOrigin = CppNxSpriteLoader.GetSpriteOrigin(nxFile, nodePath);
                    if (cppOrigin.HasValue)
                    {
                        origin = cppOrigin.Value;
                    }
                }
            }
            
            // Now create the sprite
            var sprite = ConvertNodeToSprite(imageNode, name);
            if (sprite == null) return null;
            
            return new SpriteWithOrigin(sprite, origin);
        }
        
        /// <summary>
        /// Convert NX node data to Unity Sprite
        /// </summary>
        private static Sprite ConvertNodeToSprite(INxNode node, string name)
        {
            try
            {
                
                // Get image data as byte array
                byte[] imageData = null;
                
                // The Value property should contain the PNG data if this is an image node
                var value = node.Value;
                if (value != null)
                {
                    if (value is byte[] bytes)
                    {
                        imageData = bytes;
                    }
                }
                
                // If Value didn't work, try GetValue<byte[]>
                if (imageData == null)
                {
                    try
                    {
                        imageData = node.GetValue<byte[]>();
                    }
                    catch { }
                }
                
                // Some nodes might have the data in a specific property
                if (imageData == null)
                {
                    try
                    {
                        var dataNode = node["data"] ?? node["_data"];
                        if (dataNode != null)
                        {
                            imageData = dataNode.GetValue<byte[]>() ?? dataNode.Value as byte[];
                        }
                    }
                    catch { }
                }
                
                if (imageData == null || imageData.Length == 0)
                {
                    Debug.LogWarning($"No image data found for sprite: {name} (value type: {value?.GetType().Name ?? "null"})");
                    Debug.LogWarning($"Node type: {node.GetType().Name}, Has children: {node.Children.Any()}");
                    
                    // If this is a RealNxNode, check the underlying NX node type
                    if (node is RealNxNode realNode)
                    {
                        var nxNodeField = realNode.GetType().GetField("nxNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (nxNodeField != null)
                        {
                            var nxNode = nxNodeField.GetValue(realNode);
                            if (nxNode != null)
                            {
                                Debug.LogWarning($"Underlying NX node type: {nxNode.GetType().FullName}");
                            }
                        }
                    }
                    
                    return null;
                }
                
                // Verify PNG header
                if (imageData.Length > 8)
                {
                    bool isPng = imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47;
                }
                
                // Create texture from PNG data
                var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture.name = name;
                
                if (texture.LoadImage(imageData))
                {
                    
                    // Sample first few pixels to check if it's solid color
                    if (texture.width > 0 && texture.height > 0)
                    {
                        Color topLeft = texture.GetPixel(0, 0);
                        Color center = texture.GetPixel(texture.width / 2, texture.height / 2);
                        Color bottomRight = texture.GetPixel(texture.width - 1, texture.height - 1);
                        
                        // Check if texture is completely transparent or black
                        bool isTransparent = topLeft.a == 0 && center.a == 0 && bottomRight.a == 0;
                        bool isBlack = topLeft == Color.black && center == Color.black && bottomRight == Color.black;
                        
                        if (isTransparent)
                        {
                            // Reduce log level for transparent textures - many are intentionally transparent
                            Debug.LogWarning($"Texture {name} is completely transparent! PNG data might be corrupted.");
                        }
                        else if (isBlack)
                        {
                            Debug.LogWarning($"Texture {name} is solid black! This might indicate a loading issue.");
                        }
                        else if (topLeft == center && center == bottomRight)
                        {
                            // Info level for solid colors - sky backgrounds are often solid blue
                            Debug.Log($"Texture {name} appears to be solid color: {topLeft}");
                        }
                        
                        // Log more pixel samples for debugging
                        if (isTransparent || isBlack)
                        {
                            Debug.Log($"  Image dimensions: {texture.width}x{texture.height}");
                            Debug.Log($"  PNG data size: {imageData.Length} bytes");
                            Debug.Log($"  First 16 bytes: {string.Join(" ", System.Linq.Enumerable.Take(imageData, 16).Select(b => b.ToString("X2")))}");
                        }
                    }
                    
                    // Set texture settings for pixel art
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    
                    // In C++ client, origin is NOT used as sprite pivot
                    // Instead, origin is subtracted from position during drawing
                    // MapleStory uses top-left origin, but Unity uses bottom-left for (0,0) pivot
                    // So we use top-left pivot (0,1) to match MapleStory's coordinate system
                    Vector2 pivot = new Vector2(0f, 1f); // Top-left pivot
                    
                    // Create sprite with proper pixels per unit for MapleStory
                    // MapleStory uses 100 pixels = 1 game unit
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        pivot,
                        100f // pixels per unit
                    );
                    
                    return sprite;
                }
                else
                {
                    Debug.LogError($"Failed to load image data for sprite: {name} (data length: {imageData.Length})");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to convert node to sprite: {name} - {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Get origin/pivot point from NX data
        /// </summary>
        public static Vector2 GetOrigin(INxNode node, NXDataManager dataManager = null)
        {
            if (node == null) return Vector2.zero;
            
            // Resolve any outlinks to get the actual data node
            if (dataManager != null)
            {
                node = node.ResolveOutlink(dataManager);
            }
            
            
            var originNode = node["origin"];
            if (originNode != null)
            {
                // The RealNxNode automatically converts Point types to Vector2
                var value = originNode.Value;
                if (value is Vector2 vec2)
                {
                    return vec2;
                }
                
                // Fallback: Try as child nodes with x/y
                var xNode = originNode["x"];
                var yNode = originNode["y"];
                if (xNode != null && yNode != null)
                {
                    try
                    {
                        var x = xNode.GetValue<int>();
                        var y = yNode.GetValue<int>();
                        return new Vector2(x, y);
                    }
                    catch { }
                }
            }
            
            // Check parent for origin (some nodes store origin at parent level)
            if (node.Parent != null)
            {
                var parentOrigin = node.Parent["origin"];
                if (parentOrigin != null)
                {
                    var value = parentOrigin.Value;
                    if (value is Vector2 vec2)
                    {
                        return vec2;
                    }
                }
            }
            
            // Check grandparent for origin (objects might store origin at container level)
            if (node.Parent != null && node.Parent.Parent != null)
            {
                var grandparentOrigin = node.Parent.Parent["origin"];
                if (grandparentOrigin != null)
                {
                    var value = grandparentOrigin.Value;
                    if (value is Vector2 vec2)
                    {
                        return vec2;
                    }
                }
            }
            
            return Vector2.zero;
        }
        
        /// <summary>
        /// Clear the sprite cache
        /// </summary>
        public static void ClearCache()
        {
            foreach (var sprite in spriteCache.Values)
            {
                if (sprite != null && sprite.texture != null)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                }
            }
            spriteCache.Clear();
        }
        
        /// <summary>
        /// Get a sprite from cache
        /// </summary>
        public static bool TryGetCachedSprite(string key, out Sprite sprite)
        {
            return spriteCache.TryGetValue(key, out sprite);
        }
    }
}