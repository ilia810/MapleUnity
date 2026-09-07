using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
public static class PlayerCompilationValidation
{
    public static void Run()
    {
        try
        {
            var settings = new ScriptCompilationSettings { target = BuildTarget.StandaloneWindows64, group = BuildTargetGroup.Standalone, options = ScriptCompilationOptions.None };
            var output = Path.GetFullPath("Temp/PlayerCompilationValidation");
            Directory.CreateDirectory(output);
            var result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
            if (result.assemblies == null || result.assemblies.Count() == 0) throw new Exception("Player script compilation produced no assemblies.");
            var names = result.assemblies.Select(Path.GetFileNameWithoutExtension).ToArray();
            foreach (var expected in new[] { "GameLogic", "GameData", "GameView", "SceneGeneration", "Assembly-CSharp" })
                if (!names.Contains(expected)) throw new Exception("Missing runtime assembly: " + expected);
            if (names.Any(name => name.EndsWith(".Tests") || name == "Editor")) throw new Exception("Test/editor assembly leaked into player compilation.");
            Debug.Log("PLAYER_COMPILATION_VALIDATION_PASSED: " + string.Join(", ", names));
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
}
