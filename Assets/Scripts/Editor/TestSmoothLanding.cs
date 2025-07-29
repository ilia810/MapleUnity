using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using System.Reflection;

public static class TestSmoothLanding
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[SMOOTH_LANDING_TEST] Starting at {startTime}\n");
        
        try
        {
            // Create scene and generate map
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("[SMOOTH_LANDING_TEST] Map generated");
            
            // Initialize GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            // Get components
            var gameWorldField = gameManager.GetType().GetField("gameWorld", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gameManager) as GameWorld;
            var player = gameWorld?.Player;
            
            if (player == null)
            {
                LogToFile("[SMOOTH_LANDING_TEST] ERROR: No player!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Test 1: Initial spawn and fall
            LogToFile("\n[TEST 1] Initial spawn and gravity fall:");
            LogToFile($"  Initial position: {player.Position}");
            LogToFile($"  Initial velocity: {player.Velocity}");
            LogToFile($"  Initial grounded: {player.IsGrounded}");
            
            // Track position during fall
            float deltaTime = 1f / 60f;
            int landingFrame = -1;
            float maxFallDistance = 0;
            var startY = player.Position.Y;
            
            for (int frame = 0; frame < 120; frame++)
            {
                var prevPos = player.Position;
                var prevVel = player.Velocity;
                var prevGrounded = player.IsGrounded;
                
                gameWorld.UpdatePhysics(deltaTime);
                
                // Log every 10 frames or on state changes
                if (frame % 10 == 0 || player.IsGrounded != prevGrounded)
                {
                    LogToFile($"  Frame {frame}: Pos={player.Position}, Vel={player.Velocity}, Grounded={player.IsGrounded}");
                    
                    if (!prevGrounded && player.IsGrounded)
                    {
                        landingFrame = frame;
                        LogToFile($"  >>> LANDED at frame {frame}!");
                        LogToFile($"  >>> Position change: {prevPos} -> {player.Position}");
                        LogToFile($"  >>> Y difference: {player.Position.Y - prevPos.Y:F4}");
                    }
                }
                
                if (!player.IsGrounded)
                {
                    maxFallDistance = Math.Max(maxFallDistance, startY - player.Position.Y);
                }
            }
            
            LogToFile($"\n  Summary:");
            LogToFile($"    Landed at frame: {landingFrame}");
            LogToFile($"    Max fall distance: {maxFallDistance:F4}");
            LogToFile($"    Final position: {player.Position}");
            LogToFile($"    Landing was {(landingFrame > 0 && landingFrame < 20 ? "SMOOTH" : "TELEPORTED/DELAYED")}");
            
            // Test 2: Jump and land
            LogToFile("\n[TEST 2] Jump and landing:");
            
            if (player.IsGrounded)
            {
                LogToFile($"  Pre-jump: Pos={player.Position}, Grounded={player.IsGrounded}");
                
                // Make player jump
                player.Jump();
                
                // Track the jump arc
                float peakY = player.Position.Y;
                int peakFrame = 0;
                landingFrame = -1;
                
                for (int frame = 0; frame < 180; frame++)
                {
                    var prevGrounded = player.IsGrounded;
                    var jumpPrevY = player.Position.Y;
                    
                    gameWorld.UpdatePhysics(deltaTime);
                    
                    if (player.Position.Y > peakY)
                    {
                        peakY = player.Position.Y;
                        peakFrame = frame;
                    }
                    
                    // Log key moments
                    if (frame % 15 == 0 || (prevGrounded != player.IsGrounded))
                    {
                        LogToFile($"  Frame {frame}: Y={player.Position.Y:F4}, VelY={player.Velocity.Y:F4}, Grounded={player.IsGrounded}");
                        
                        if (!prevGrounded && player.IsGrounded)
                        {
                            landingFrame = frame;
                            float landingSpeed = Math.Abs(player.Position.Y - jumpPrevY) / deltaTime;
                            LogToFile($"  >>> LANDED at frame {frame}!");
                            LogToFile($"  >>> Landing speed: {landingSpeed:F2} units/s");
                            LogToFile($"  >>> Y change on landing: {player.Position.Y - jumpPrevY:F4}");
                        }
                    }
                }
                
                LogToFile($"\n  Jump Summary:");
                LogToFile($"    Peak height reached at frame {peakFrame}: Y={peakY:F4}");
                LogToFile($"    Jump height: {peakY - player.Position.Y:F4}");
                LogToFile($"    Landing frame: {landingFrame}");
                LogToFile($"    Total air time: {(landingFrame - 0) * deltaTime:F2}s");
            }
            
            // Test 3: Fall from height
            LogToFile("\n[TEST 3] Fall from greater height:");
            
            // Place player high above ground
            player.Position = new MapleClient.GameLogic.Vector2(0, 5f); // 5 units high
            player.Velocity = MapleClient.GameLogic.Vector2.Zero;
            player.IsGrounded = false;
            
            LogToFile($"  Starting at: {player.Position}");
            
            float prevY = player.Position.Y;
            for (int frame = 0; frame < 120; frame++)
            {
                gameWorld.UpdatePhysics(deltaTime);
                
                if (frame % 10 == 0 || player.IsGrounded)
                {
                    float fallSpeed = (prevY - player.Position.Y) / (10 * deltaTime);
                    LogToFile($"  Frame {frame}: Y={player.Position.Y:F4}, Fall speed={fallSpeed:F2} units/s, Grounded={player.IsGrounded}");
                    prevY = player.Position.Y;
                    
                    if (player.IsGrounded)
                    {
                        LogToFile($"  >>> Landed from 5 unit drop!");
                        break;
                    }
                }
            }
            
            LogToFile($"\n[SMOOTH_LANDING_TEST] Test complete");
            LogToFile($"Result: Landing behavior should be smooth, not teleporting");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[SMOOTH_LANDING_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}