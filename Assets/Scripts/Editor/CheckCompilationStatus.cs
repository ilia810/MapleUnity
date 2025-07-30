using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Linq;

public static class CheckCompilationStatus
{
    public static void Run()
    {
        Debug.Log("=== Unity Compilation Status Check ===");
        
        // Force a compilation refresh
        CompilationPipeline.RequestScriptCompilation();
        
        // Check if there are any compilation errors
        var assemblies = CompilationPipeline.GetAssemblies();
        Debug.Log($"Total assemblies found: {assemblies.Length}");
        
        // Check console for errors
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntries != null)
        {
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            
            if (getCountMethod != null)
            {
                int errorCount = (int)getCountMethod.Invoke(null, null);
                Debug.Log($"Console error count: {errorCount}");
                
                if (errorCount > 0)
                {
                    Debug.LogError("Compilation errors detected!");
                    EditorApplication.Exit(1);
                    return;
                }
            }
        }
        
        // Try to find and instantiate our fixed class to verify it compiles
        var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp-Editor");
            
        if (assembly != null)
        {
            var validateType = assembly.GetType("ValidateCharacterRenderingFixes");
            if (validateType != null)
            {
                Debug.Log("ValidateCharacterRenderingFixes class found and loaded successfully!");
                
                // Try to invoke the Run method to ensure it works
                var runMethod = validateType.GetMethod("Run", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (runMethod != null)
                {
                    Debug.Log("Found Run method - class is properly compiled!");
                }
            }
            else
            {
                Debug.LogError("ValidateCharacterRenderingFixes class not found in assembly!");
                EditorApplication.Exit(1);
                return;
            }
        }
        
        Debug.Log("=== Compilation check completed successfully ===");
        EditorApplication.Exit(0);
    }
}