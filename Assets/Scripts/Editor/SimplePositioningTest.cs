using UnityEngine;
using UnityEditor;

public static class SimplePositioningTest
{
    public static void TestBasic()
    {
        Debug.Log("=== Simple Positioning Test Started ===");
        
        try
        {
            // Just test if we can access basic Unity functionality
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Project Path: {Application.dataPath}");
            
            // Try to create a simple game object
            GameObject test = new GameObject("SimpleTest");
            Debug.Log($"Created GameObject: {test.name}");
            
            // Check if we can find MapleCharacterRenderer type
            var type = System.Type.GetType("MapleCharacterRenderer");
            if (type != null)
            {
                Debug.Log("MapleCharacterRenderer type found!");
                
                // Try to add the component
                var component = test.AddComponent(type);
                if (component != null)
                {
                    Debug.Log("Successfully added MapleCharacterRenderer component");
                }
            }
            else
            {
                Debug.LogError("MapleCharacterRenderer type not found - compilation issues?");
            }
            
            Debug.Log("=== Test Complete ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}