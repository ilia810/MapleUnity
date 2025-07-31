using UnityEngine;
using UnityEditor;
using System.IO;

public static class MinimalCompilationStatus
{
    public static void CheckStatus()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "compilation-status.txt");
        
        try
        {
            File.WriteAllText(logPath, "=== Compilation Status Report ===\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n");
            File.AppendAllText(logPath, $"Project Path: {Application.dataPath}\n");
            File.AppendAllText(logPath, $"Platform: {Application.platform}\n\n");
            
            // This will be false if there are compilation errors
            bool compilationSucceeded = !EditorUtility.scriptCompilationFailed;
            
            if (compilationSucceeded)
            {
                File.AppendAllText(logPath, "RESULT: COMPILATION SUCCESSFUL\n");
                File.AppendAllText(logPath, "No compilation errors detected.\n");
                Debug.Log("Compilation successful!");
                EditorApplication.Exit(0);
            }
            else
            {
                File.AppendAllText(logPath, "RESULT: COMPILATION FAILED\n");
                File.AppendAllText(logPath, "There are compilation errors in the project.\n");
                File.AppendAllText(logPath, "Please check the Unity Console for specific error messages.\n");
                Debug.LogError("Compilation failed!");
                EditorApplication.Exit(1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during status check: {e.Message}");
            EditorApplication.Exit(2);
        }
    }
}