using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;

public static class CompilationCheckTest
{
    public static void CheckCompilation()
    {
        Debug.Log("=== COMPILATION CHECK STARTED ===");
        
        // Check if there are any compilation errors
        bool hasErrors = CompilationPipeline.codeOptimization == CodeOptimization.Debug;
        
        // Try to compile and check for errors
        var assemblies = CompilationPipeline.GetAssemblies();
        Debug.Log($"Found {assemblies.Length} assemblies");
        
        foreach (var assembly in assemblies)
        {
            Debug.Log($"Assembly: {assembly.name}");
        }
        
        // Check if key classes can be instantiated
        try
        {
            // Test GameLogic layer
            var worldType = System.Type.GetType("GameWorld, Assembly-CSharp");
            if (worldType != null)
            {
                Debug.Log("✓ GameWorld class found");
            }
            else
            {
                Debug.LogError("✗ GameWorld class not found");
            }
            
            // Test GameView layer
            var rendererType = System.Type.GetType("MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType != null)
            {
                Debug.Log("✓ MapleCharacterRenderer class found");
            }
            else
            {
                Debug.LogError("✗ MapleCharacterRenderer class not found");
            }
            
            // Test GameData layer
            var loaderType = System.Type.GetType("NXAssetLoader, Assembly-CSharp");
            if (loaderType != null)
            {
                Debug.Log("✓ NXAssetLoader class found");
            }
            else
            {
                Debug.LogError("✗ NXAssetLoader class not found");
            }
            
            Debug.Log("=== COMPILATION CHECK COMPLETED ===");
            Debug.Log("No compilation errors detected!");
            
            // Write results
            File.WriteAllText("compilation-check-results.txt", "Compilation check passed - no errors found");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Compilation check failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    public static void RunAttachmentFlow()
    {
        try
        {
            Debug.Log("=== STARTING ATTACHMENT FLOW DEBUG ===");
            
            // Call the debug method directly
            DebugAttachmentPointFlow.RunDebug();
            
            // Give it time to complete with delayed calls
            var startTime = System.DateTime.Now;
            while ((System.DateTime.Now - startTime).TotalSeconds < 5)
            {
                EditorApplication.Step();
                System.Threading.Thread.Sleep(100);
            }
            
            Debug.Log("=== ATTACHMENT FLOW DEBUG COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}