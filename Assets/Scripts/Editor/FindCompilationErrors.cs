using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using System.Linq;

public static class FindCompilationErrors  
{
    public static void FindErrors()
    {
        Debug.Log("=== SEARCHING FOR COMPILATION ERRORS ===");
        var logPath = Path.Combine(Application.dataPath, "..", "compilation-errors-found.txt");
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== COMPILATION ERROR SEARCH ===");
        report.AppendLine($"Started at: {System.DateTime.Now}");
        
        // Try to access compilation messages if available
        var messages = new System.Collections.Generic.List<string>();
        
        // Check console logs
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntries != null)
        {
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getEntryMethod = logEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            
            if (getCountMethod != null)
            {
                int count = (int)getCountMethod.Invoke(null, null);
                report.AppendLine($"\nFound {count} console entries");
            }
        }
        
        // Check assembly compilation status
        var assemblies = CompilationPipeline.GetAssemblies();
        report.AppendLine($"\nFound {assemblies.Length} assemblies:");
        
        foreach (var assembly in assemblies)
        {
            report.AppendLine($"\n{assembly.name}:");
            report.AppendLine($"  Output path: {assembly.outputPath}");
            report.AppendLine($"  Source files: {assembly.sourceFiles.Length}");
            
            // List first few source files
            foreach (var sourceFile in assembly.sourceFiles.Take(5))
            {
                report.AppendLine($"    - {Path.GetFileName(sourceFile)}");
            }
            
            // Check if the assembly output exists
            if (File.Exists(assembly.outputPath))
            {
                var fileInfo = new FileInfo(assembly.outputPath);
                report.AppendLine($"  Output exists: {fileInfo.Length} bytes");
            }
            else
            {
                report.AppendLine($"  Output MISSING!");
            }
        }
        
        // Check specifically for our assemblies
        var gameLogicAssembly = assemblies.FirstOrDefault(a => a.name == "GameLogic");
        var gameViewAssembly = assemblies.FirstOrDefault(a => a.name == "GameView");
        var gameDataAssembly = assemblies.FirstOrDefault(a => a.name == "GameData");
        
        report.AppendLine("\n=== MAIN ASSEMBLIES STATUS ===");
        report.AppendLine($"GameLogic: {(gameLogicAssembly != null ? "Found" : "MISSING")}");
        report.AppendLine($"GameView: {(gameViewAssembly != null ? "Found" : "MISSING")}");
        report.AppendLine($"GameData: {(gameDataAssembly != null ? "Found" : "MISSING")}");
        
        // Write report
        File.WriteAllText(logPath, report.ToString());
        Debug.Log($"Report written to: {logPath}");
        
        EditorApplication.Exit(0);
    }
}