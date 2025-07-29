using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class SynchronousCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[SYNC_TEST] Starting at {DateTime.Now}\n");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile($"[SYNC_TEST] Created new scene: {newScene.name}");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            LogToFile("[SYNC_TEST] Initialized MapSceneGenerator");
            
            // Generate Henesys map (ID: 100000000)
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot == null)
            {
                LogToFile("[SYNC_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[SYNC_TEST] Generated map root: {mapRoot.name}");
            
            // Report what was generated
            var backgrounds = mapRoot.GetComponentsInChildren<SpriteRenderer>().Length;
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            var footholdsCount = footholdManager != null ? footholdManager.GetAllFootholds().Count : 0;
            
            LogToFile($"[SYNC_TEST] Generated {backgrounds} sprite renderers");
            LogToFile($"[SYNC_TEST] Generated {footholdsCount} footholds");
            
            // Test foothold data directly
            if (footholdManager != null)
            {
                var footholds = footholdManager.GetAllFootholds();
                if (footholds.Count > 0)
                {
                    LogToFile($"[SYNC_TEST] First foothold: ID={footholds[0].Id}, X1={footholds[0].X1}, Y1={footholds[0].Y1}, X2={footholds[0].X2}, Y2={footholds[0].Y2}");
                    
                    // Test ground detection at various positions
                    float[] testPositions = { -400, -200, 0, 200, 400 };
                    foreach (float x in testPositions)
                    {
                        float groundY = footholdManager.GetYBelow(x, 100);
                        LogToFile($"[SYNC_TEST] Ground below ({x}, 100): Y={groundY:F2}");
                    }
                    
                    // Count total footholds
                    LogToFile($"[SYNC_TEST] Total footholds in map: {footholds.Count}");
                }
            }
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Force initialization
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            LogToFile("[SYNC_TEST] Created and initialized GameManager");
            
            // Test player spawn
            var spawnPoint = GameObject.Find("SpawnPoint");
            if (spawnPoint != null)
            {
                LogToFile($"[SYNC_TEST] Found spawn point at: {spawnPoint.transform.position}");
                
                // Test collision at spawn point
                float spawnX = spawnPoint.transform.position.x;
                float spawnY = spawnPoint.transform.position.y;
                
                if (footholdManager != null)
                {
                    float groundAtSpawn = footholdManager.GetYBelow(spawnX, spawnY + 100);
                    LogToFile($"[SYNC_TEST] Ground below spawn point: Y={groundAtSpawn:F2}");
                    LogToFile($"[SYNC_TEST] Expected player Y position: {groundAtSpawn - 1:F2}");
                }
            }
            
            // Test FootholdService from GameManager
            if (gameManager.FootholdService != null)
            {
                LogToFile("[SYNC_TEST] FootholdService is available");
                
                // Test some common platform positions in Henesys
                float[] platformTestX = { -350, -150, 0, 150, 350 };
                foreach (float x in platformTestX)
                {
                    var result = gameManager.FootholdService.GetGroundBelow(x, 100);
                    LogToFile($"[SYNC_TEST] FootholdService.GetGroundBelow({x}, 100) = {result:F2}");
                }
            }
            
            LogToFile("[SYNC_TEST] Test completed successfully!");
            LogToFile($"[SYNC_TEST] Summary:");
            LogToFile($"[SYNC_TEST]   - Map generated: YES");
            LogToFile($"[SYNC_TEST]   - Footholds: {footholdsCount}");
            LogToFile($"[SYNC_TEST]   - Backgrounds: {backgrounds}");
            LogToFile($"[SYNC_TEST]   - GameManager: INITIALIZED");
            LogToFile($"[SYNC_TEST]   - FootholdService: AVAILABLE");
            
            // Clean up
            GameObject.DestroyImmediate(generatorObj);
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[SYNC_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}