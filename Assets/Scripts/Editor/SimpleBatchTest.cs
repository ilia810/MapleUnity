using UnityEngine;
using UnityEditor;
using System.IO;

public static class SimpleBatchTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
        
        try
        {
            File.WriteAllText(logPath, "[UNITY_BATCH] Test started successfully\n");
            Debug.Log("[UNITY_BATCH] Unity is running in batch mode");
            
            // Log Unity version
            File.AppendAllText(logPath, $"[UNITY_BATCH] Unity Version: {Application.unityVersion}\n");
            
            // Log project path
            File.AppendAllText(logPath, $"[UNITY_BATCH] Project Path: {Application.dataPath}\n");
            
            // Check if we can find the GameManager
            var gameManagerType = System.Type.GetType("MapleClient.GameView.GameManager, Assembly-CSharp");
            if (gameManagerType != null)
            {
                File.AppendAllText(logPath, "[UNITY_BATCH] GameManager type found\n");
            }
            else
            {
                File.AppendAllText(logPath, "[UNITY_BATCH] GameManager type not found\n");
            }
            
            File.AppendAllText(logPath, "[UNITY_BATCH] Test completed\n");
            Debug.Log("[UNITY_BATCH] Test completed successfully");
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"[UNITY_BATCH] ERROR: {e.Message}\n");
            Debug.LogError($"[UNITY_BATCH] ERROR: {e.Message}");
        }
    }
}