using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System;

namespace MapleClient.Editor
{
    public class QuickTestCppNx
    {
        [MenuItem("MapleUnity/Debug/Quick Test C++ NX")]
        public static void QuickTest()
        {
            Debug.Log("=== QUICK C++ NX TEST ===");
            
            try
            {
                // Try to initialize
                string nxPath = @"C:\HeavenClient\MapleStory-Client\nx";
                Debug.Log($"Initializing with path: {nxPath}");
                
                bool initialized = CppNxWrapper.Initialize(nxPath);
                Debug.Log($"Initialization result: {initialized}");
                
                if (initialized)
                {
                    // Try to get a simple node - test object with origin
                    var node = CppNxWrapper.GetNode("Map.nx/Obj/guide.img/common/post/0");
                    if (node != IntPtr.Zero)
                    {
                        Debug.Log($"Successfully got guide sign node!");
                        var nodeType = CppNxWrapper.GetNodeType(node);
                        Debug.Log($"Node type: {nodeType}");
                        
                        // Check for origin
                        if (CppNxWrapper.GetOrigin(node, out Vector2 origin))
                        {
                            Debug.Log($"Guide sign HAS ORIGIN: {origin} !!!");
                        }
                        else
                        {
                            Debug.Log("Guide sign has no origin at this level");
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to get node");
                    }
                    
                    CppNxWrapper.Cleanup();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception: {e}");
            }
        }
    }
}