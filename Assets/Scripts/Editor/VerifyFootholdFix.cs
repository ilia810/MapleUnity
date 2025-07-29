using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using System.Reflection;
using System.Linq;

public static class VerifyFootholdFix
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[VERIFY_FOOTHOLD_FIX] Starting at {startTime}\n");
        
        try
        {
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[VERIFY_FOOTHOLD_FIX] Created new scene");
            
            // Generate map using MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("[VERIFY_FOOTHOLD_FIX] Map generated");
            
            // Create and initialize GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Force initialization
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            LogToFile("[VERIFY_FOOTHOLD_FIX] GameManager initialized");
            
            // Check FootholdService
            var footholdService = gameManager.FootholdService;
            if (footholdService == null)
            {
                LogToFile("[VERIFY_FOOTHOLD_FIX] ERROR: No FootholdService!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Count footholds
            var allFootholds = footholdService.GetFootholdsInArea(-100000, -10000, 100000, 10000);
            int footholdCount = allFootholds.Count();
            LogToFile($"[VERIFY_FOOTHOLD_FIX] FootholdService has {footholdCount} footholds");
            
            // Check coverage at various X positions
            LogToFile("\n[VERIFY_FOOTHOLD_FIX] Testing ground detection:");
            float[] testX = { 0, 500, 1000, 2000, 3000, 4000, 4500, 5000, 5500, 6000, 7000, 8000 };
            int groundedCount = 0;
            
            foreach (float x in testX)
            {
                float ground = footholdService.GetGroundBelow(x, 0);
                bool hasGround = ground != float.MaxValue;
                if (hasGround) groundedCount++;
                LogToFile($"  X={x}: Ground at Y={ground} {(hasGround ? "✓" : "✗ NO GROUND!")}");
            }
            
            // Get GameWorld and test physics
            var gameWorldField = gameManager.GetType().GetField("gameWorld", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gameManager) as GameWorld;
            
            if (gameWorld?.Player != null)
            {
                LogToFile("\n[VERIFY_FOOTHOLD_FIX] Testing player physics:");
                
                // Test player at different positions
                var player = gameWorld.Player;
                float fixedDeltaTime = 1f / 60f;
                
                foreach (float x in new[] { 0f, 2000f, 4000f, 6000f })
                {
                    player.Position = new MapleClient.GameLogic.Vector2(x, -1.5f);
                    
                    // Simulate physics
                    for (int i = 0; i < 60; i++)
                    {
                        gameWorld.UpdatePhysics(fixedDeltaTime);
                    }
                    
                    LogToFile($"  X={x}: Player at Y={player.Position.Y}, Grounded={player.IsGrounded}");
                }
            }
            
            // Final verdict
            LogToFile($"\n[VERIFY_FOOTHOLD_FIX] Summary:");
            LogToFile($"  - Total footholds loaded: {footholdCount}");
            LogToFile($"  - Ground coverage: {groundedCount}/{testX.Length} positions have ground");
            LogToFile($"  - Expected footholds: 338");
            
            bool success = footholdCount > 100; // We expect many more than 4
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogToFile($"\n[VERIFY_FOOTHOLD_FIX] Test duration: {duration:F2}s");
            
            if (success)
            {
                LogToFile("[VERIFY_FOOTHOLD_FIX] SUCCESS: Foothold loading is fixed!");
                EditorApplication.Exit(0);
            }
            else
            {
                LogToFile($"[VERIFY_FOOTHOLD_FIX] FAILURE: Only {footholdCount} footholds loaded (expected > 100)");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogToFile($"[VERIFY_FOOTHOLD_FIX] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}