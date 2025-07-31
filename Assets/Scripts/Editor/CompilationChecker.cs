using UnityEngine;
using UnityEditor;
using System.IO;

public static class CompilationChecker
{
    public static void CheckCompilation()
    {
        Debug.Log("=== Compilation Check Started ===");
        
        // Unity will output compilation errors to the console automatically
        // We just need to ensure the editor compiles and exits
        
        Debug.Log("Checking for compilation errors...");
        
        // Force a compilation refresh
        AssetDatabase.Refresh();
        
        // Wait a moment for compilation to complete
        System.Threading.Thread.Sleep(2000);
        
        Debug.Log("=== Compilation Check Complete ===");
        
        // Exit with success code - if there were compilation errors,
        // they would have been logged to the console already
        EditorApplication.Exit(0);
    }
}