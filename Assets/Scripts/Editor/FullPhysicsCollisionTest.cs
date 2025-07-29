using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using System.Reflection;

public static class FullPhysicsCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[FULL_PHYSICS_TEST] Starting at {startTime}\n");
        
        try
        {
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[FULL_PHYSICS_TEST] Created new scene");
            
            // Generate map
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            if (mapRoot == null)
            {
                LogToFile("[FULL_PHYSICS_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("[FULL_PHYSICS_TEST] Map generated");
            
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
            
            LogToFile("[FULL_PHYSICS_TEST] GameManager initialized");
            
            // Get GameWorld from GameManager
            var gameWorldField = gameManager.GetType().GetField("gameWorld", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gameManager) as GameWorld;
            
            if (gameWorld == null)
            {
                LogToFile("[FULL_PHYSICS_TEST] ERROR: Could not get GameWorld!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get the GameLogic Player
            var logicPlayer = gameWorld.Player;
            if (logicPlayer == null)
            {
                LogToFile("[FULL_PHYSICS_TEST] ERROR: GameLogic Player is null!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[FULL_PHYSICS_TEST] GameLogic Player initial position: {logicPlayer.Position}");
            LogToFile($"[FULL_PHYSICS_TEST] GameLogic Player IsGrounded: {logicPlayer.IsGrounded}");
            LogToFile($"[FULL_PHYSICS_TEST] GameLogic Player Velocity: {logicPlayer.Velocity}");
            
            // Simulate the game loop manually
            LogToFile("[FULL_PHYSICS_TEST] Starting physics simulation...");
            
            float fixedDeltaTime = 1f / 60f; // 60 FPS
            for (int frame = 0; frame < 120; frame++) // 2 seconds
            {
                // Call the GameWorld physics update directly
                gameWorld.UpdatePhysics(fixedDeltaTime);
                
                // Also update Unity physics for any Unity components
                Physics.Simulate(fixedDeltaTime);
                Physics2D.Simulate(fixedDeltaTime);
                
                // Log every 30 frames (0.5 seconds)
                if (frame % 30 == 0)
                {
                    LogToFile($"  Frame {frame}: Player Pos={logicPlayer.Position}, Vel={logicPlayer.Velocity}, Grounded={logicPlayer.IsGrounded}");
                    
                    // Also check Unity player position
                    var unityPlayer = GameObject.Find("Player");
                    if (unityPlayer != null)
                    {
                        LogToFile($"    Unity Player: {unityPlayer.transform.position}");
                    }
                }
            }
            
            // Final state
            LogToFile($"[FULL_PHYSICS_TEST] Final GameLogic Player position: {logicPlayer.Position}");
            LogToFile($"[FULL_PHYSICS_TEST] Final GameLogic Player grounded: {logicPlayer.IsGrounded}");
            
            // Test horizontal movement
            LogToFile("[FULL_PHYSICS_TEST] Testing horizontal movement...");
            
            // Save initial position
            var initialPos = logicPlayer.Position;
            
            // Move player and simulate
            logicPlayer.Position = new MapleClient.GameLogic.Vector2(500, initialPos.Y);
            for (int i = 0; i < 60; i++)
            {
                gameWorld.UpdatePhysics(fixedDeltaTime);
            }
            LogToFile($"  After moving to X=500: Pos={logicPlayer.Position}, Grounded={logicPlayer.IsGrounded}");
            
            // Move to edge
            logicPlayer.Position = new MapleClient.GameLogic.Vector2(4500, initialPos.Y);
            for (int i = 0; i < 60; i++)
            {
                gameWorld.UpdatePhysics(fixedDeltaTime);
            }
            LogToFile($"  After moving to X=4500: Pos={logicPlayer.Position}, Grounded={logicPlayer.IsGrounded}");
            
            // Move beyond edge (should fall)
            logicPlayer.Position = new MapleClient.GameLogic.Vector2(5500, initialPos.Y);
            for (int i = 0; i < 60; i++)
            {
                gameWorld.UpdatePhysics(fixedDeltaTime);
            }
            LogToFile($"  After moving to X=5500: Pos={logicPlayer.Position}, Grounded={logicPlayer.IsGrounded}");
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogToFile($"[FULL_PHYSICS_TEST] Test duration: {duration:F2}s");
            LogToFile("[FULL_PHYSICS_TEST] Test complete");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[FULL_PHYSICS_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
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
        catch { }
    }
}