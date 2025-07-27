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
        
        /// <summary>
        /// Load a sprite from an NX node
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
                    Debug.Log($"Successfully loaded texture for {name}: {texture.width}x{texture.height}");
                    
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
            
            // Debug: Log node hierarchy to understand structure
            Debug.Log($"GetOrigin for node: {node.Name}");
            
            var originNode = node["origin"];
            if (originNode != null)
            {
                // The RealNxNode automatically converts Point types to Vector2
                var value = originNode.Value;
                if (value is Vector2 vec2)
                {
                    Debug.Log($"Found origin as Vector2: {vec2}");
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
                        Debug.Log($"Found origin from x/y nodes: ({x}, {y})");
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
                        Debug.Log($"Found origin on parent as Vector2: {vec2}");
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
                        Debug.Log($"Found origin on grandparent as Vector2: {vec2}");
                        return vec2;
                    }
                }
            }
            
            Debug.Log($"No origin found for node: {node.Name}, returning (0,0)");
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