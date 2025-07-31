using UnityEngine;
using UnityEditor;

public static class SimpleCompilationTest
{
    public static void TestCompilation()
    {
        Debug.Log("=== Unity Compilation Status Check ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Project Path: {Application.dataPath}");
        
        // Check if there are compilation errors
        if (EditorUtility.scriptCompilationFailed)
        {
            Debug.LogError("COMPILATION FAILED: There are compilation errors in the project!");
            Debug.LogError("Please check the Console for specific error messages.");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("COMPILATION SUCCESSFUL: No compilation errors detected!");
            Debug.Log("The project compiles without errors.");
            EditorApplication.Exit(0);
        }
    }
}