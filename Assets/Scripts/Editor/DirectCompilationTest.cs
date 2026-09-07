using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

public static class DirectCompilationTest
{
    public static void TestCompilation()
    {
        Debug.Log("=== DIRECT COMPILATION TEST ===");
        
        // Check if the main assemblies are loaded
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        bool foundMainAssembly = false;
        
        foreach (var assembly in assemblies)
        {
            if (assembly.GetName().Name == "Assembly-CSharp")
            {
                foundMainAssembly = true;
                Debug.Log($"Found Assembly-CSharp with {assembly.GetTypes().Length} types");
                
                // Check for key types
                var gameWorldType = assembly.GetType("GameWorld");
                var rendererType = assembly.GetType("MapleCharacterRenderer");
                var loaderType = assembly.GetType("NXAssetLoader");
                
                Debug.Log($"GameWorld type found: {gameWorldType != null}");
                Debug.Log($"MapleCharacterRenderer type found: {rendererType != null}");
                Debug.Log($"NXAssetLoader type found: {loaderType != null}");
                
                if (gameWorldType != null && rendererType != null && loaderType != null)
                {
                    Debug.Log("✓ All key classes found - compilation successful!");
                    File.WriteAllText("direct-compilation-test.txt", "SUCCESS: All key classes compiled successfully");
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("✗ Some key classes are missing");
                    File.WriteAllText("direct-compilation-test.txt", "FAILED: Some key classes are missing");
                    EditorApplication.Exit(1);
                }
            }
        }
        
        if (!foundMainAssembly)
        {
            Debug.LogError("Assembly-CSharp not found - compilation likely failed!");
            File.WriteAllText("direct-compilation-test.txt", "FAILED: Assembly-CSharp not found");
            EditorApplication.Exit(1);
        }
    }
}