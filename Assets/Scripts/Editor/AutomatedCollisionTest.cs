using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;

public static class AutomatedCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[AUTOMATED_TEST] Starting at {DateTime.Now}\n");
            
            // Load the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            LogToFile($"[AUTOMATED_TEST] Loaded scene: {scene.name}");
            
            // Find GameManager
            var gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                LogToFile("[AUTOMATED_TEST] ERROR: GameManager not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[AUTOMATED_TEST] Found GameManager");
            
            // Get GameManager component
            var gameManager = gameManagerObj.GetComponent<MapleClient.GameView.GameManager>();
            if (gameManager == null)
            {
                LogToFile("[AUTOMATED_TEST] ERROR: GameManager component not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Force initialization by calling Start manually
            var startMethod = gameManager.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (startMethod != null)
            {
                LogToFile("[AUTOMATED_TEST] Calling GameManager.Start()");
                startMethod.Invoke(gameManager, null);
            }
            
            // Now check if player was created
            var player = GameObject.Find("Player");
            if (player != null)
            {
                LogToFile($"[AUTOMATED_TEST] Player created at: {player.transform.position}");
                
                // Test spawn position
                float spawnY = player.transform.position.y;
                LogToFile($"[AUTOMATED_TEST] Player spawn Y: {spawnY:F2} (expected around -1.5)");
                
                // Simulate some physics updates
                for (int i = 0; i < 60; i++) // 1 second at 60 FPS
                {
                    Physics.Simulate(1f/60f);
                }
                
                LogToFile($"[AUTOMATED_TEST] Player Y after physics: {player.transform.position.y:F2}");
                
                // Test platform boundaries
                TestPlatformBoundaries(player);
            }
            else
            {
                LogToFile("[AUTOMATED_TEST] ERROR: Player was not created after GameManager.Start()");
            }
            
            LogToFile("[AUTOMATED_TEST] Test completed");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[AUTOMATED_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestPlatformBoundaries(GameObject player)
    {
        LogToFile("\n[AUTOMATED_TEST] Testing platform boundaries...");
        
        // Test right boundary
        player.transform.position = new Vector3(45f, player.transform.position.y, 0);
        Physics.Simulate(0.5f);
        LogToFile($"[AUTOMATED_TEST] At X=45: Y={player.transform.position.y:F2}");
        
        // Test left boundary  
        player.transform.position = new Vector3(-45f, player.transform.position.y, 0);
        Physics.Simulate(0.5f);
        LogToFile($"[AUTOMATED_TEST] At X=-45: Y={player.transform.position.y:F2}");
        
        // Test beyond old boundary (should still be on platform with our fix)
        player.transform.position = new Vector3(49f, player.transform.position.y, 0);
        Physics.Simulate(1f);
        LogToFile($"[AUTOMATED_TEST] At X=49: Y={player.transform.position.y:F2} (should stay on platform)");
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        try
        {
            File.AppendAllText(logPath, message + "\n");
        }
        catch { }
    }
}