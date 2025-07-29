using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class GenerateAndTestCollision
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[GENERATE_AND_TEST] Starting at {DateTime.Now}\n");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile($"[GENERATE_AND_TEST] Created new scene: {newScene.name}");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            LogToFile("[GENERATE_AND_TEST] Initialized MapSceneGenerator");
            
            // Generate Henesys map (ID: 100000000)
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot == null)
            {
                LogToFile("[GENERATE_AND_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[GENERATE_AND_TEST] Generated map root: {mapRoot.name}");
            
            // Report what was generated
            var backgrounds = mapRoot.GetComponentsInChildren<SpriteRenderer>().Length;
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            var footholdsCount = footholdManager != null ? footholdManager.GetAllFootholds().Count : 0;
            
            LogToFile($"[GENERATE_AND_TEST] Generated {backgrounds} sprite renderers");
            LogToFile($"[GENERATE_AND_TEST] Generated {footholdsCount} footholds");
            
            // Clean up generator
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager to handle game logic
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            LogToFile("[GENERATE_AND_TEST] Created GameManager");
            
            // Wait a frame for components to initialize
            EditorApplication.delayCall += () =>
            {
                ContinueTest(gameManager);
            };
        }
        catch (Exception e)
        {
            LogToFile($"[GENERATE_AND_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void ContinueTest(GameManager gameManager)
    {
        try
        {
            // Force GameManager initialization
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (startMethod != null)
            {
                LogToFile("[GENERATE_AND_TEST] Calling GameManager.Start()");
                startMethod.Invoke(gameManager, null);
            }
            
            // Give it another frame to fully initialize
            EditorApplication.delayCall += () =>
            {
                RunCollisionTests();
            };
        }
        catch (Exception e)
        {
            LogToFile($"[GENERATE_AND_TEST] ERROR in ContinueTest: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void RunCollisionTests()
    {
        try
        {
            // Find player
            var player = GameObject.Find("Player");
            if (player == null)
            {
                LogToFile("[GENERATE_AND_TEST] ERROR: Player not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[GENERATE_AND_TEST] Found player at: {player.transform.position}");
            
            // Test spawn position
            float spawnY = player.transform.position.y;
            LogToFile($"[GENERATE_AND_TEST] Player spawn Y: {spawnY:F2} (expected around -1.5)");
            
            // Simulate physics to let player settle
            LogToFile("[GENERATE_AND_TEST] Simulating physics...");
            for (int i = 0; i < 60; i++) // 1 second at 60 FPS
            {
                Physics.Simulate(1f/60f);
            }
            
            LogToFile($"[GENERATE_AND_TEST] Player Y after physics: {player.transform.position.y:F2}");
            
            // Test platform boundaries
            TestPlatformBoundaries(player);
            
            // Check FootholdService
            var gameManager = GameObject.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                var footholdService = gameManager.FootholdService;
                if (footholdService != null)
                {
                    LogToFile("[GENERATE_AND_TEST] FootholdService is available");
                    
                    // Test foothold detection at player position
                    var groundY = footholdService.GetGroundBelow(0, 100);
                    LogToFile($"[GENERATE_AND_TEST] Ground below (0,100): {groundY}");
                }
            }
            
            LogToFile("[GENERATE_AND_TEST] Test completed successfully!");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[GENERATE_AND_TEST] ERROR in RunCollisionTests: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestPlatformBoundaries(GameObject player)
    {
        LogToFile("\n[GENERATE_AND_TEST] Testing platform boundaries...");
        
        // Test various X positions
        float[] testPositions = { 30f, 45f, 49f, -30f, -45f, -49f };
        
        foreach (float xPos in testPositions)
        {
            player.transform.position = new Vector3(xPos, player.transform.position.y, 0);
            
            // Simulate physics
            for (int i = 0; i < 30; i++) // 0.5 seconds
            {
                Physics.Simulate(1f/60f);
            }
            
            float yPos = player.transform.position.y;
            bool onPlatform = yPos > -5f; // Should be around -2 if on platform
            
            LogToFile($"[GENERATE_AND_TEST] At X={xPos}: Y={yPos:F2} - {(onPlatform ? "ON PLATFORM" : "FELL OFF")}");
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