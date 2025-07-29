using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.IO;
using System;

public class BatchModeCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunCollisionTest()
    {
        try
        {
            Debug.Log("[BATCH_TEST] Starting batch mode collision test...");
            
            // Clear previous log
            File.WriteAllText(logPath, $"[BATCH_TEST] Collision Test Started at {DateTime.Now}\n");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            LogToFile($"[BATCH_TEST] Opened scene: {scene.name}");
            
            // Enter play mode
            EditorApplication.EnterPlaymode();
            
            // Since we can't use coroutines in batch mode, we'll use a delayed call
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    // Wait a bit more for physics to settle
                    EditorApplication.delayCall += () =>
                    {
                        RunTestsAndExit();
                    };
                };
            };
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void RunTestsAndExit()
    {
        try
        {
            // Find player
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                LogToFile($"[BATCH_TEST] Player found at: {player.transform.position}");
                LogToFile($"[BATCH_TEST] Player Y position: {player.transform.position.y:F2}");
                
                // Check if RuntimeCollisionTest is running
                var collisionTest = GameObject.FindObjectOfType<MapleClient.GameView.RuntimeCollisionTest>();
                if (collisionTest != null)
                {
                    LogToFile("[BATCH_TEST] RuntimeCollisionTest component is active");
                }
                else
                {
                    LogToFile("[BATCH_TEST] WARNING: RuntimeCollisionTest not found!");
                }
            }
            else
            {
                LogToFile("[BATCH_TEST] ERROR: Player not found!");
            }
            
            // Log some foothold service info if available
            var gameManager = GameObject.FindObjectOfType<MapleClient.GameView.GameManager>();
            if (gameManager != null)
            {
                LogToFile("[BATCH_TEST] GameManager found");
            }
            
            // Wait a bit more then exit
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    LogToFile("[BATCH_TEST] Test completed, exiting Unity...");
                    EditorApplication.Exit(0);
                };
            };
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] ERROR in RunTestsAndExit: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        try
        {
            File.AppendAllText(logPath, message + "\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
    }
}