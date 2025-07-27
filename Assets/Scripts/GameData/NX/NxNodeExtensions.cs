using System;
using System.Linq;
using MapleClient.GameData;

namespace GameData
{
    /// <summary>
    /// Extension methods for INxNode to handle outlinks and other NX-specific functionality
    /// </summary>
    public static class NxNodeExtensions
    {
        /// <summary>
        /// Resolves outlinks to get the actual data node
        /// Based on C++ client's Texture.cpp outlink resolution
        /// </summary>
        public static INxNode ResolveOutlink(this INxNode node, NXDataManager dataManager)
        {
            if (node == null) return null;
            
            // Check if this node has an outlink
            var outlinkNode = node["_outlink"];
            if (outlinkNode == null) return node;
            
            string outlinkPath = outlinkNode.Value as string;
            if (string.IsNullOrEmpty(outlinkPath)) return node;
            
            // Parse the outlink path (format: "Map/path/to/actual/data")
            int firstSlash = outlinkPath.IndexOf('/');
            if (firstSlash == -1) return node;
            
            string filePrefix = outlinkPath.Substring(0, firstSlash);
            string path = outlinkPath.Substring(firstSlash + 1);
            
            // Currently we only handle Map outlinks (most common for objects)
            if (filePrefix == "Map")
            {
                var resolvedNode = dataManager.GetNode("map", path);
                if (resolvedNode != null)
                {
                    UnityEngine.Debug.Log($"Resolved outlink: {outlinkPath} -> found node");
                    return resolvedNode;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Failed to resolve outlink: {outlinkPath}");
                }
            }
            
            return node;
        }
        
        /// <summary>
        /// Gets the actual sprite node, resolving any outlinks
        /// </summary>
        public static INxNode GetSpriteNode(this INxNode node, NXDataManager dataManager)
        {
            if (node == null) return null;
            
            // First resolve any outlinks
            var resolved = node.ResolveOutlink(dataManager);
            
            // The resolved node might itself have child nodes (like canvas)
            // Check for common sprite node patterns
            if (resolved["canvas"] != null)
            {
                return resolved["canvas"];
            }
            
            return resolved;
        }
    }
}