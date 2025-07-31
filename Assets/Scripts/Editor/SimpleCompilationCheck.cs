using UnityEngine;
using UnityEditor;

public static class SimpleCompilationCheck
{
    [MenuItem("Test/Check Compilation")]
    public static void CheckCompilation()
    {
        Debug.Log("=== Simple Compilation Check ===");
        
        // This method will only be found if there are no compilation errors
        Debug.Log("If you see this message, the project compiled successfully!");
        
        // Exit Unity
        EditorApplication.Exit(0);
    }
}