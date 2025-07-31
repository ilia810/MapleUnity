using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class MinimalUnityTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\minimal-unity-test.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[TEST] Starting minimal test at {DateTime.Now}\n");
            Debug.Log("[TEST] Minimal Unity test starting...");
            
            // Simple test to verify Unity can execute
            File.AppendAllText(logPath, "[TEST] Unity is running in batch mode\n");
            File.AppendAllText(logPath, $"[TEST] Unity version: {Application.unityVersion}\n");
            File.AppendAllText(logPath, $"[TEST] Project path: {Application.dataPath}\n");
            
            // Check if we can access basic Unity functionality
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            File.AppendAllText(logPath, $"[TEST] Active scene: {scene.name}\n");
            
            File.AppendAllText(logPath, "[TEST] Test completed successfully!\n");
            Debug.Log("[TEST] Test completed successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"[TEST] ERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"[TEST] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}