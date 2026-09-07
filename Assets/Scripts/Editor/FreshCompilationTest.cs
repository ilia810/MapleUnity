using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using System.Linq;

public static class FreshCompilationTest
{
    public static void RunTest()
    {
        Debug.Log("=== FRESH COMPILATION TEST ===");
        var logPath = Path.Combine(Application.dataPath, "..", "fresh-compilation-test.txt");
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== FRESH COMPILATION TEST ===");
        report.AppendLine($"Test started at: {System.DateTime.Now}");
        report.AppendLine();
        
        // Force a compilation refresh
        CompilationPipeline.RequestScriptCompilation();
        
        // Check assemblies
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var csharpAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
        
        if (csharpAssembly != null)
        {
            var types = csharpAssembly.GetTypes();
            report.AppendLine($"Assembly-CSharp found with {types.Length} types");
            
            // Check for main classes
            var gameWorldFound = types.Any(t => t.Name == "GameWorld");
            var rendererFound = types.Any(t => t.Name == "MapleCharacterRenderer");
            var loaderFound = types.Any(t => t.Name == "NXAssetLoader");
            
            report.AppendLine($"GameWorld found: {gameWorldFound}");
            report.AppendLine($"MapleCharacterRenderer found: {rendererFound}");
            report.AppendLine($"NXAssetLoader found: {loaderFound}");
            
            // List all types in GameLogic, GameView, GameData namespaces
            var gameLogicTypes = types.Where(t => t.Namespace?.Contains("GameLogic") ?? false).ToArray();
            var gameViewTypes = types.Where(t => t.Namespace?.Contains("GameView") ?? false).ToArray();
            var gameDataTypes = types.Where(t => t.Namespace?.Contains("GameData") ?? false).ToArray();
            
            report.AppendLine();
            report.AppendLine($"GameLogic types: {gameLogicTypes.Length}");
            foreach (var type in gameLogicTypes.Take(10))
                report.AppendLine($"  - {type.FullName}");
                
            report.AppendLine();
            report.AppendLine($"GameView types: {gameViewTypes.Length}");
            foreach (var type in gameViewTypes.Take(10))
                report.AppendLine($"  - {type.FullName}");
                
            report.AppendLine();
            report.AppendLine($"GameData types: {gameDataTypes.Length}");
            foreach (var type in gameDataTypes.Take(10))
                report.AppendLine($"  - {type.FullName}");
            
            if (types.Length < 50)
            {
                report.AppendLine();
                report.AppendLine("✗ COMPILATION FAILED: Too few types compiled");
                report.AppendLine("This suggests there are compilation errors preventing most code from compiling.");
            }
            else
            {
                report.AppendLine();
                report.AppendLine("✓ COMPILATION SUCCESSFUL: All main assemblies compiled");
            }
        }
        else
        {
            report.AppendLine("✗ Assembly-CSharp not found - critical compilation failure!");
        }
        
        // Write results
        File.WriteAllText(logPath, report.ToString());
        Debug.Log($"Results written to: {logPath}");
        
        EditorApplication.Exit(csharpAssembly != null && csharpAssembly.GetTypes().Length >= 50 ? 0 : 1);
    }
}