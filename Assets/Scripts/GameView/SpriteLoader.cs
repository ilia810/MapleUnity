using System;
using UnityEngine;
using MapleClient.GameData;

namespace MapleClient.GameView
{
    /// <summary>
    /// Utility class for converting NX image data to Unity Sprites
    /// Handles coordinate system conversion and pivot calculations
    /// </summary>
    public static class SpriteLoader
    {
        // MapleStory uses 100 pixels per unit in the physics system
        private const float PIXELS_PER_UNIT = 100f;
        
        /// <summary>
        /// Convert an NX node containing image data to a Unity Sprite
        /// </summary>
        public static Sprite LoadSprite(INxNode node, string spriteName = null)
        {
            if (node == null) return null;
            
            // Check if this node contains image data
            var imageData = GetImageData(node);
            if (imageData == null) return null;
            
            // Get origin point if available
            Vector2 origin = GetOrigin(node);
            
            return ConvertToSprite(imageData, origin, spriteName ?? node.Name);
        }
        
        /// <summary>
        /// Convert a character sprite node to Unity Sprite with proper origin handling
        /// </summary>
        public static Sprite ConvertCharacterNodeToSprite(INxNode node, string spriteName, Vector2 origin)
        {
            if (node == null) return null;
            
            var imageData = GetImageData(node);
            if (imageData == null) return null;
            
            return ConvertToSprite(imageData, origin, spriteName);
        }
        
        /// <summary>
        /// Extract image data from an NX node
        /// </summary>
        private static byte[] GetImageData(INxNode node)
        {
            // Direct image data
            if (node.Value is byte[] directData)
            {
                return directData;
            }
            
            // Check for _inlink or _outlink references
            var linkNode = node["_inlink"] ?? node["_outlink"];
            if (linkNode != null && linkNode.Value is string linkPath)
            {
                Debug.Log($"[SpriteLoader] Following link: {linkPath}");
                // TODO: Implement link resolution through NXDataManager
                // For now, return null and handle in NXAssetLoader
                return null;
            }
            
            // Check for image child node
            var imageNode = node["image"];
            if (imageNode != null && imageNode.Value is byte[] imageData)
            {
                return imageData;
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract origin point from node
        /// </summary>
        private static Vector2 GetOrigin(INxNode node)
        {
            var originNode = node["origin"];
            if (originNode != null && originNode.Value is Vector2 vec)
            {
                return vec;
            }
            
            // Some sprites have origin as separate x,y values
            var originX = node["originX"];
            var originY = node["originY"];
            if (originX != null && originY != null)
            {
                float x = Convert.ToSingle(originX.Value);
                float y = Convert.ToSingle(originY.Value);
                return new Vector2(x, y);
            }
            
            return Vector2.zero;
        }
        
        /// <summary>
        /// Convert byte array PNG data to Unity Sprite
        /// </summary>
        private static Sprite ConvertToSprite(byte[] pngData, Vector2 origin, string name)
        {
            if (pngData == null || pngData.Length == 0) return null;
            
            // Create texture from PNG data
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(pngData))
            {
                Debug.LogError($"[SpriteLoader] Failed to load PNG data for sprite: {name}");
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            
            // Apply texture settings for pixel art
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            // Convert MapleStory origin (top-left based) to Unity pivot (bottom-left based)
            // In MapleStory: origin is offset from top-left corner
            // In Unity: pivot is normalized position from bottom-left (0,0) to top-right (1,1)
            float pivotX = origin.x / texture.width;
            float pivotY = 1.0f - (origin.y / texture.height); // Invert Y axis
            
            Vector2 pivot = new Vector2(pivotX, pivotY);
            
            // Create sprite with proper pixels per unit
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, PIXELS_PER_UNIT);
            sprite.name = name;
            
            Debug.Log($"[SpriteLoader] Created sprite '{name}': {texture.width}x{texture.height}, origin=({origin.x},{origin.y}), pivot=({pivotX:F2},{pivotY:F2})");
            
            return sprite;
        }
        
        /// <summary>
        /// Create a composite sprite from multiple body parts
        /// </summary>
        public static Sprite CreateCompositeSprite(INxNode frameNode, string spriteName)
        {
            // TODO: Implement composite sprite creation for multi-part characters
            // For now, just load the first valid part
            foreach (var partNode in frameNode.Children)
            {
                if (partNode.Name == "delay" || partNode.Name == "face") continue;
                
                var sprite = LoadSprite(partNode, $"{spriteName}/{partNode.Name}");
                if (sprite != null) return sprite;
            }
            
            return null;
        }
    }
}