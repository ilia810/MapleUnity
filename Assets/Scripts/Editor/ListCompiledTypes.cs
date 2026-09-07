using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Linq;

public static class ListCompiledTypes
{
    public static void ListAllTypes()
    {
        Debug.Log("=== LISTING ALL COMPILED TYPES ===");
        
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var report = new System.Text.StringBuilder();
        
        foreach (var assembly in assemblies)
        {
            if (assembly.GetName().Name == "Assembly-CSharp")
            {
                var types = assembly.GetTypes();
                report.AppendLine($"Assembly-CSharp contains {types.Length} types:");
                report.AppendLine("=====================================");
                
                foreach (var type in types.OrderBy(t => t.FullName))
                {
                    report.AppendLine($"  - {type.FullName}");
                }
                
                // Check for specific namespaces
                var gameLogicTypes = types.Where(t => t.Namespace != null && t.Namespace.Contains("GameLogic")).ToArray();
                var gameViewTypes = types.Where(t => t.Namespace != null && t.Namespace.Contains("GameView")).ToArray();
                var gameDataTypes = types.Where(t => t.Namespace != null && t.Namespace.Contains("GameData")).ToArray();
                
                report.AppendLine("\nNamespace Summary:");
                report.AppendLine($"  GameLogic types: {gameLogicTypes.Length}");
                report.AppendLine($"  GameView types: {gameViewTypes.Length}");
                report.AppendLine($"  GameData types: {gameDataTypes.Length}");
                
                Debug.Log(report.ToString());
                File.WriteAllText("compiled-types-list.txt", report.ToString());
                
                // Check if this looks like a successful compilation
                if (types.Length < 50)
                {
                    Debug.LogError($"Only {types.Length} types compiled - likely compilation errors!");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"Found {types.Length} compiled types - compilation successful!");
                    EditorApplication.Exit(0);
                }
                return;
            }
        }
        
        Debug.LogError("Assembly-CSharp not found!");
        EditorApplication.Exit(1);
    }
}