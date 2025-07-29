using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class ProperBatchCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    private static int exitCode = 0;
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[BATCH_TEST] Starting at {DateTime.Now}\n");
            LogToFile("[BATCH_TEST] This test WILL exit Unity when complete");
            
            // Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile($"[BATCH_TEST] Created new scene");
            
            // Generate map
            if (!GenerateMap())
            {
                LogToFile("[BATCH_TEST] ERROR: Failed to generate map");
                EditorApplication.Exit(1);
                return;
            }
            
            // Initialize game systems
            if (!InitializeGameSystems())
            {
                LogToFile("[BATCH_TEST] ERROR: Failed to initialize game systems");
                EditorApplication.Exit(1);
                return;
            }
            
            // Run collision tests
            if (!RunCollisionTests())
            {
                LogToFile("[BATCH_TEST] ERROR: Collision tests failed");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[BATCH_TEST] All tests completed successfully!");
            LogToFile("[BATCH_TEST] Exiting Unity with code 0 (success)");
            
            // SUCCESS - Exit Unity
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
            LogToFile("[BATCH_TEST] Exiting Unity with code 1 (error)");
            EditorApplication.Exit(1);
        }
    }
    
    private static bool GenerateMap()
    {
        try
        {
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot == null)
            {
                return false;
            }
            
            LogToFile($"[BATCH_TEST] Generated map: {mapRoot.name}");
            
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            if (footholdManager != null)
            {
                LogToFile($"[BATCH_TEST] FootholdManager component found");
            }
            
            GameObject.DestroyImmediate(generatorObj);
            return true;
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] GenerateMap exception: {e.Message}");
            return false;
        }
    }
    
    private static bool InitializeGameSystems()
    {
        try
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Force initialization synchronously
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            LogToFile("[BATCH_TEST] GameManager initialized");
            return true;
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] InitializeGameSystems exception: {e.Message}");
            return false;
        }
    }
    
    private static bool RunCollisionTests()
    {
        try
        {
            // Find player
            var player = GameObject.Find("Player");
            if (player == null)
            {
                LogToFile("[BATCH_TEST] Player not found!");
                return false;
            }
            
            LogToFile($"[BATCH_TEST] Player spawn position: {player.transform.position}");
            
            // Simulate physics
            for (int i = 0; i < 60; i++)
            {
                Physics.Simulate(1f/60f);
            }
            
            float settledY = player.transform.position.y;
            LogToFile($"[BATCH_TEST] Player Y after physics: {settledY:F2}");
            
            // Test boundaries
            bool allTestsPassed = true;
            
            // Test X=45 (should stay on platform)
            player.transform.position = new Vector3(45f, settledY, 0);
            Physics.Simulate(0.5f);
            if (player.transform.position.y < -5f)
            {
                LogToFile($"[BATCH_TEST] FAIL: Player fell off at X=45");
                allTestsPassed = false;
            }
            else
            {
                LogToFile($"[BATCH_TEST] PASS: Player stayed on platform at X=45");
            }
            
            // Test X=49 (should stay on platform with our extended boundaries)
            player.transform.position = new Vector3(49f, settledY, 0);
            Physics.Simulate(0.5f);
            if (player.transform.position.y < -5f)
            {
                LogToFile($"[BATCH_TEST] FAIL: Player fell off at X=49");
                allTestsPassed = false;
            }
            else
            {
                LogToFile($"[BATCH_TEST] PASS: Player stayed on platform at X=49");
            }
            
            return allTestsPassed;
        }
        catch (Exception e)
        {
            LogToFile($"[BATCH_TEST] RunCollisionTests exception: {e.Message}");
            return false;
        }
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