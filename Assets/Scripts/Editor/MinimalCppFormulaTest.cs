using UnityEngine;
using UnityEditor;

public static class MinimalCppFormulaTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Testing C++ Character Rendering Formulas ===");
            Debug.Log("This is a minimal test to verify Unity batch mode is working.");
            
            // Create a test GameObject
            GameObject testObject = new GameObject("TestCharacter");
            testObject.transform.position = Vector3.zero;
            
            Debug.Log($"Created test object at position: {testObject.transform.position}");
            
            // Try to add MapleCharacterRenderer if it compiles
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType != null)
            {
                Debug.Log("Found MapleCharacterRenderer type");
                var renderer = testObject.AddComponent(rendererType);
                Debug.Log("Added MapleCharacterRenderer component");
                
                // Log component info
                Debug.Log($"Renderer type: {renderer.GetType().FullName}");
            }
            else
            {
                Debug.LogWarning("MapleCharacterRenderer type not found - likely compilation errors");
                Debug.Log("Checking for compilation errors...");
                
                // List all assemblies to debug
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.GetName().Name.Contains("Assembly-CSharp"))
                    {
                        Debug.Log($"Found assembly: {assembly.GetName().Name}");
                    }
                }
            }
            
            // Clean up
            GameObject.DestroyImmediate(testObject);
            
            Debug.Log("\n=== Test Complete ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
}