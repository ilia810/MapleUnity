using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;

public static class VerifyCompilationSuccess
{
    public static void Verify()
    {
        Debug.Log("=== VERIFYING COMPILATION SUCCESS ===");
        var logPath = Path.Combine(Application.dataPath, "..", "compilation-verification.txt");
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== COMPILATION VERIFICATION ===");
        report.AppendLine($"Verified at: {System.DateTime.Now}");
        report.AppendLine();
        
        bool success = true;
        
        // Get all assemblies
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        
        // Check GameLogic assembly
        var gameLogicAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "GameLogic");
        if (gameLogicAssembly != null)
        {
            var types = gameLogicAssembly.GetTypes();
            report.AppendLine($"✓ GameLogic assembly loaded with {types.Length} types");
            
            // Check for GameWorld
            var gameWorldType = types.FirstOrDefault(t => t.Name == "GameWorld");
            if (gameWorldType != null)
            {
                report.AppendLine($"  ✓ GameWorld class found in namespace: {gameWorldType.Namespace}");
            }
            else
            {
                report.AppendLine("  ✗ GameWorld class NOT found");
                success = false;
            }
        }
        else
        {
            report.AppendLine("✗ GameLogic assembly NOT found!");
            success = false;
        }
        
        // Check GameView assembly
        var gameViewAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "GameView");
        if (gameViewAssembly != null)
        {
            var types = gameViewAssembly.GetTypes();
            report.AppendLine($"✓ GameView assembly loaded with {types.Length} types");
            
            // Check for MapleCharacterRenderer
            var rendererType = types.FirstOrDefault(t => t.Name == "MapleCharacterRenderer");
            if (rendererType != null)
            {
                report.AppendLine($"  ✓ MapleCharacterRenderer class found in namespace: {rendererType.Namespace}");
            }
            else
            {
                report.AppendLine("  ✗ MapleCharacterRenderer class NOT found");
                success = false;
            }
        }
        else
        {
            report.AppendLine("✗ GameView assembly NOT found!");
            success = false;
        }
        
        // Check GameData assembly
        var gameDataAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "GameData");
        if (gameDataAssembly != null)
        {
            var types = gameDataAssembly.GetTypes();
            report.AppendLine($"✓ GameData assembly loaded with {types.Length} types");
            
            // Check for NXAssetLoader
            var loaderType = types.FirstOrDefault(t => t.Name == "NXAssetLoader");
            if (loaderType != null)
            {
                report.AppendLine($"  ✓ NXAssetLoader class found in namespace: {loaderType.Namespace}");
            }
            else
            {
                report.AppendLine("  ✗ NXAssetLoader class NOT found");
                success = false;
            }
        }
        else
        {
            report.AppendLine("✗ GameData assembly NOT found!");
            success = false;
        }
        
        report.AppendLine();
        if (success)
        {
            report.AppendLine("=== ✓ COMPILATION SUCCESSFUL ===");
            report.AppendLine("All main assemblies and core classes compiled successfully!");
        }
        else
        {
            report.AppendLine("=== ✗ COMPILATION FAILED ===");
            report.AppendLine("Some core classes are missing!");
        }
        
        // Write report
        File.WriteAllText(logPath, report.ToString());
        Debug.Log(report.ToString());
        
        EditorApplication.Exit(success ? 0 : 1);
    }
}