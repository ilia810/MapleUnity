using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class StandaloneMinimalTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\standalone-minimal-test.log";
    
    public static void RunTest()
    {
        try
        {
            // Create log file
            File.WriteAllText(logPath, $"[TEST] Starting standalone minimal test at {DateTime.Now}\n");
            
            // Log to Unity console
            Debug.Log("[TEST] Standalone Minimal Unity test starting...");
            
            // Simple test to verify Unity can execute
            File.AppendAllText(logPath, "[TEST] Unity is running in batch mode\n");
            File.AppendAllText(logPath, $"[TEST] Unity version: {Application.unityVersion}\n");
            File.AppendAllText(logPath, $"[TEST] Project path: {Application.dataPath}\n");
            
            // Test basic Unity functionality
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            File.AppendAllText(logPath, $"[TEST] Active scene: {activeScene.name}\n");
            
            // Test if we can create a GameObject
            var testObject = new GameObject("TestObject");
            File.AppendAllText(logPath, "[TEST] Successfully created a GameObject\n");
            
            // Clean up
            if (testObject != null)
            {
                GameObject.DestroyImmediate(testObject);
                File.AppendAllText(logPath, "[TEST] Successfully destroyed GameObject\n");
            }
            
            File.AppendAllText(logPath, "[TEST] Test completed successfully!\n");
            Debug.Log("[TEST] Test completed successfully!");
            
            // Exit with success
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            var errorMsg = $"[TEST] ERROR: {e.Message}\n{e.StackTrace}\n";
            
            try
            {
                File.AppendAllText(logPath, errorMsg);
            }
            catch
            {
                // If we can't write to file, at least log to console
            }
            
            Debug.LogError($"[TEST] ERROR: {e.Message}");
            
            // Exit with failure
            EditorApplication.Exit(1);
        }
    }
}