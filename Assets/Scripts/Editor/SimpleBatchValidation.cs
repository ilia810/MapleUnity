using UnityEngine;
using UnityEditor;

public static class SimpleBatchValidation
{
    public static void RunValidation()
    {
        Debug.Log("=== SIMPLE BATCH VALIDATION ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Is Batch Mode: {Application.isBatchMode}");
        Debug.Log("Test executed successfully!");
        
        // Force exit
        EditorApplication.Exit(0);
    }
}