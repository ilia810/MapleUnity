using UnityEngine;
using UnityEditor;
using System.IO;

public static class UnityBasicTest
{
    public static void Run()
    {
        string logPath = @"C:\Users\me\MapleUnity\unity-basic-test.log";
        
        try
        {
            File.WriteAllText(logPath, "=== Unity Basic Test Started ===\n");
            File.AppendAllText(logPath, $"Time: {System.DateTime.Now}\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n");
            File.AppendAllText(logPath, $"Batch Mode: {Application.isBatchMode}\n");
            File.AppendAllText(logPath, "Test completed successfully\n");
            
            Debug.Log("Unity Basic Test - Success");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"ERROR: {e.Message}\n");
            Debug.LogError($"Unity Basic Test - Failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}