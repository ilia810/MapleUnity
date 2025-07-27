using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace MapleClient.GameData
{
    /// <summary>
    /// Sprite loader that uses the C++ NX library to get origin data
    /// </summary>
    public class CppNxSpriteLoader
    {
        private static bool initialized = false;
        
        public static bool Initialize()
        {
            if (initialized) return true;
            
            string nxPath = @"C:\HeavenClient\MapleStory-Client\nx";
            initialized = CppNxWrapper.Initialize(nxPath);
            
            if (!initialized)
            {
                Debug.LogError("Failed to initialize C++ NX wrapper for sprite loading");
            }
            
            return initialized;
        }
        
        public static void Cleanup()
        {
            if (initialized)
            {
                CppNxWrapper.Cleanup();
                initialized = false;
            }
        }
        
        /// <summary>
        /// Get origin for a sprite node using C++ NX library
        /// </summary>
        public static Vector2? GetSpriteOrigin(string nxFile, string nodePath)
        {
            if (!initialized && !Initialize())
            {
                return null;
            }
            
            // Construct full path like "Map.nx/Obj/guide.img/common/post/0"
            string fullPath = $"{nxFile}/{nodePath}";
            
            IntPtr node = CppNxWrapper.GetNode(fullPath);
            if (node == IntPtr.Zero)
            {
                return null;
            }
            
            // First check if this node has origin directly
            if (CppNxWrapper.GetOrigin(node, out Vector2 origin))
            {
                return origin;
            }
            
            // For container nodes, check the first child (usually "0")
            if (CppNxWrapper.GetNodeType(node) == CppNxWrapper.NodeType.None)
            {
                IntPtr firstChild = CppNxWrapper.GetChild(node, "0");
                if (firstChild != IntPtr.Zero && CppNxWrapper.GetOrigin(firstChild, out Vector2 childOrigin))
                {
                    return childOrigin;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get origin for a specific frame in an animated sprite
        /// </summary>
        public static Vector2? GetFrameOrigin(string nxFile, string nodePath, string frameName)
        {
            if (!initialized && !Initialize())
            {
                return null;
            }
            
            string fullPath = $"{nxFile}/{nodePath}/{frameName}";
            
            IntPtr node = CppNxWrapper.GetNode(fullPath);
            if (node == IntPtr.Zero)
            {
                return null;
            }
            
            // Check if this node has origin directly
            if (CppNxWrapper.GetOrigin(node, out Vector2 origin))
            {
                return origin;
            }
            
            return null;
        }
    }
}