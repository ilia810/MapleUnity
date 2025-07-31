using UnityEngine;
using UnityEditor;

public static class MinimalScaleTest
{
    public static void RunTest()
    {
        Debug.Log("=== Minimal Scale Test Starting ===");
        
        try
        {
            // Just test that we can run
            Debug.Log("Test is running in Unity");
            
            // Create a simple GameObject
            var go = new GameObject("TestObject");
            Debug.Log($"Created GameObject: {go.name}");
            
            // Test scale manipulation
            go.transform.localScale = new Vector3(1, 1, 1);
            Debug.Log($"Initial scale: {go.transform.localScale}");
            
            go.transform.localScale = new Vector3(-1, 1, 1);
            Debug.Log($"Flipped scale: {go.transform.localScale}");
            
            // Cleanup
            GameObject.DestroyImmediate(go);
            
            Debug.Log("Test completed successfully");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}