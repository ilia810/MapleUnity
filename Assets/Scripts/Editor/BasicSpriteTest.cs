using UnityEngine;
using UnityEditor;

public static class BasicSpriteTest
{
    public static void TestSprites()
    {
        Debug.Log("=== BASIC SPRITE TEST STARTING ===");
        Debug.Log("Unity Version: " + Application.unityVersion);
        Debug.Log("Project Path: " + Application.dataPath);
        Debug.Log("Batch Mode: " + Application.isBatchMode);
        
        // Create a simple test object
        GameObject testObj = new GameObject("TestObject");
        testObj.transform.position = Vector3.zero;
        
        Debug.Log("Created test object at: " + testObj.transform.position);
        
        // Clean up
        Object.DestroyImmediate(testObj);
        
        Debug.Log("=== TEST COMPLETED ===");
        
        // Exit Unity
        EditorApplication.Exit(0);
    }
}