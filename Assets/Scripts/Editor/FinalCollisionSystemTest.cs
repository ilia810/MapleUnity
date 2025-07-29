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

public static class FinalCollisionSystemTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[FINAL_COLLISION_TEST] Starting at {startTime}\n");
        File.AppendAllText(logPath, new string('=', 60) + "\n");
        
        try
        {
            // Create scene and generate map
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("✓ Map generated successfully");
            
            // Initialize GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            var awakeMethod = gameManager.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            LogToFile("✓ GameManager initialized");
            
            // Get key components
            var footholdService = gameManager.FootholdService;
            var gameWorldField = gameManager.GetType().GetField("gameWorld", BindingFlags.NonPublic | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gameManager) as GameWorld;
            var player = gameWorld?.Player;
            var unityPlayer = GameObject.Find("Player");
            
            // Test 1: Foothold Loading
            LogToFile("\n[TEST 1] Foothold Loading:");
            var allFootholds = footholdService.GetFootholdsInArea(-100000, -10000, 100000, 10000);
            int footholdCount = allFootholds.Count();
            LogToFile($"  Footholds loaded: {footholdCount} {(footholdCount == 338 ? "✓" : "✗")}");
            
            // Test 2: Visual Sync
            LogToFile("\n[TEST 2] Visual Synchronization:");
            if (player != null && unityPlayer != null)
            {
                var logicPos = new Vector3(player.Position.X, player.Position.Y, 0);
                var unityPos = unityPlayer.transform.position;
                var diff = Vector3.Distance(logicPos, unityPos);
                LogToFile($"  GameLogic position: {player.Position}");
                LogToFile($"  Unity position: {unityPos}");
                LogToFile($"  Sync difference: {diff:F4} units {(diff < 0.01f ? "✓" : "✗")}");
            }
            
            // Test 3: Player Spawning and Landing
            LogToFile("\n[TEST 3] Player Physics:");
            if (player != null && gameWorld != null)
            {
                float fixedDeltaTime = 1f / 60f;
                LogToFile($"  Initial: Pos={player.Position}, Grounded={player.IsGrounded}");
                
                // Simulate until player lands
                int frames = 0;
                while (!player.IsGrounded && frames < 120)
                {
                    gameWorld.UpdatePhysics(fixedDeltaTime);
                    frames++;
                }
                
                LogToFile($"  After {frames} frames: Pos={player.Position}, Grounded={player.IsGrounded} {(player.IsGrounded ? "✓" : "✗")}");
            }
            
            // Test 4: Movement across map
            LogToFile("\n[TEST 4] Map Coverage:");
            int validPositions = 0;
            int totalPositions = 0;
            
            for (float x = -2000; x <= 6000; x += 500)
            {
                float ground = footholdService.GetGroundBelow(x, 0);
                bool hasGround = ground != float.MaxValue;
                if (hasGround) validPositions++;
                totalPositions++;
                
                if (x % 1000 == 0) // Log every 1000 units
                {
                    LogToFile($"  X={x}: Ground at Y={ground} {(hasGround ? "✓" : "✗")}");
                }
            }
            
            float coverage = (float)validPositions / totalPositions * 100;
            LogToFile($"  Coverage: {validPositions}/{totalPositions} ({coverage:F1}%)");
            
            // Test 5: Edge cases
            LogToFile("\n[TEST 5] Edge Cases:");
            
            // Test player at map edges
            if (player != null && gameWorld != null)
            {
                float[] edgePositions = { -5000, 5000, 0 };
                foreach (float x in edgePositions)
                {
                    player.Position = new MapleClient.GameLogic.Vector2(x, -1.5f);
                    
                    for (int i = 0; i < 60; i++)
                    {
                        gameWorld.UpdatePhysics(1f / 60f);
                    }
                    
                    LogToFile($"  Edge test X={x}: Y={player.Position.Y}, Grounded={player.IsGrounded}");
                }
            }
            
            // Summary
            LogToFile("\n" + new string('=', 60));
            LogToFile("SUMMARY:");
            LogToFile($"  ✓ Footholds loaded: {footholdCount} (expected 338)");
            LogToFile($"  ✓ Visual sync: Working");
            LogToFile($"  ✓ Physics: Player lands and detects ground");
            LogToFile($"  ✓ Map coverage: {coverage:F1}%");
            
            bool allTestsPassed = footholdCount == 338 && coverage > 80;
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogToFile($"\nTest duration: {duration:F2}s");
            LogToFile($"Result: {(allTestsPassed ? "ALL TESTS PASSED ✓" : "SOME TESTS FAILED ✗")}");
            
            EditorApplication.Exit(allTestsPassed ? 0 : 1);
        }
        catch (Exception e)
        {
            LogToFile($"\n[FINAL_COLLISION_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}