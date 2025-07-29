using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using System.Reflection;

public static class FixedCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[FIXED_COLLISION_TEST] Starting at {startTime}\n");
        
        try
        {
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[FIXED_COLLISION_TEST] Created new scene");
            
            // Generate map
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            if (mapRoot == null)
            {
                LogToFile("[FIXED_COLLISION_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("[FIXED_COLLISION_TEST] Map generated");
            
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
            
            LogToFile("[FIXED_COLLISION_TEST] GameManager initialized");
            
            // Get GameWorld from GameManager
            var gameWorldField = gameManager.GetType().GetField("gameWorld", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gameManager) as GameWorld;
            
            if (gameWorld == null)
            {
                LogToFile("[FIXED_COLLISION_TEST] ERROR: Could not get GameWorld!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get the GameLogic Player
            var logicPlayer = gameWorld.Player;
            if (logicPlayer == null)
            {
                LogToFile("[FIXED_COLLISION_TEST] ERROR: GameLogic Player is null!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get Unity player object
            var unityPlayer = GameObject.Find("Player");
            if (unityPlayer == null)
            {
                LogToFile("[FIXED_COLLISION_TEST] ERROR: Unity Player GameObject not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[FIXED_COLLISION_TEST] Initial states:");
            LogToFile($"  GameLogic Player position: {logicPlayer.Position}");
            LogToFile($"  Unity Player position: {unityPlayer.transform.position}");
            LogToFile($"  GameLogic IsGrounded: {logicPlayer.IsGrounded}");
            LogToFile($"  GameLogic Velocity: {logicPlayer.Velocity}");
            
            // Test 1: Vertical physics (falling and landing)
            LogToFile("\n[FIXED_COLLISION_TEST] Test 1: Vertical physics");
            
            float fixedDeltaTime = 1f / 60f;
            for (int frame = 0; frame < 120; frame++)
            {
                gameWorld.UpdatePhysics(fixedDeltaTime);
                Physics.Simulate(fixedDeltaTime);
                Physics2D.Simulate(fixedDeltaTime);
                
                // Force Unity player controller update
                var controller = unityPlayer.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    var updateMethod = controller.GetType().GetMethod("Update", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    updateMethod?.Invoke(controller, null);
                    
                    var fixedUpdateMethod = controller.GetType().GetMethod("FixedUpdate", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    fixedUpdateMethod?.Invoke(controller, null);
                }
                
                // Log every 30 frames
                if (frame % 30 == 0)
                {
                    LogToFile($"  Frame {frame}:");
                    LogToFile($"    GameLogic: Pos={logicPlayer.Position}, Vel={logicPlayer.Velocity}, Grounded={logicPlayer.IsGrounded}");
                    LogToFile($"    Unity: Pos={unityPlayer.transform.position}");
                    
                    // Check if positions are synced
                    var diff = Vector3.Distance(
                        new Vector3(logicPlayer.Position.X, logicPlayer.Position.Y, 0),
                        unityPlayer.transform.position
                    );
                    LogToFile($"    Position difference: {diff:F4} units");
                }
            }
            
            // Test 2: Horizontal movement at different X positions
            LogToFile("\n[FIXED_COLLISION_TEST] Test 2: Horizontal movement test");
            
            // Test positions across the map
            float[] testPositions = { 0, 500, 1000, 2000, 3000, 4000, 4500, 5000, 5500, 6000 };
            
            foreach (float xPos in testPositions)
            {
                // Move player to test position
                logicPlayer.Position = new MapleClient.GameLogic.Vector2(xPos, -1.5f);
                
                // Simulate physics for 1 second
                for (int i = 0; i < 60; i++)
                {
                    gameWorld.UpdatePhysics(fixedDeltaTime);
                    
                    // Force Unity sync
                    var controller = unityPlayer.GetComponent<SimplePlayerController>();
                    if (controller != null)
                    {
                        var fixedUpdateMethod = controller.GetType().GetMethod("FixedUpdate", 
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        fixedUpdateMethod?.Invoke(controller, null);
                    }
                }
                
                LogToFile($"  X={xPos}:");
                LogToFile($"    GameLogic: Pos={logicPlayer.Position}, Grounded={logicPlayer.IsGrounded}");
                LogToFile($"    Unity: Pos={unityPlayer.transform.position}");
                LogToFile($"    Synced: {(Math.Abs(logicPlayer.Position.X - unityPlayer.transform.position.x) < 0.01f && Math.Abs(logicPlayer.Position.Y - unityPlayer.transform.position.y) < 0.01f ? "YES" : "NO")}");
            }
            
            // Test 3: Check foothold coverage
            LogToFile("\n[FIXED_COLLISION_TEST] Test 3: Foothold coverage analysis");
            
            var footholdService = gameManager.FootholdService;
            if (footholdService != null)
            {
                var allFootholds = footholdService.GetFootholdsInArea(-10000, -1000, 10000, 1000);
                LogToFile($"  Total footholds: {allFootholds.Count()}");
                
                // Check ground detection at key positions
                foreach (float x in testPositions)
                {
                    float ground = footholdService.GetGroundBelow(x, 0);
                    bool hasGround = ground != float.MaxValue;
                    LogToFile($"  X={x}: Ground at Y={ground} {(hasGround ? "" : "(NO GROUND!)")}");
                }
            }
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogToFile($"\n[FIXED_COLLISION_TEST] Test duration: {duration:F2}s");
            
            // Final verdict
            bool allTestsPassed = true;
            
            // Check if player is grounded
            if (!logicPlayer.IsGrounded)
            {
                LogToFile("[FIXED_COLLISION_TEST] FAIL: Player not grounded!");
                allTestsPassed = false;
            }
            
            // Check if visual is synced
            var finalDiff = Vector3.Distance(
                new Vector3(logicPlayer.Position.X, logicPlayer.Position.Y, 0),
                unityPlayer.transform.position
            );
            if (finalDiff > 0.01f)
            {
                LogToFile($"[FIXED_COLLISION_TEST] FAIL: Visual not synced! Difference: {finalDiff}");
                allTestsPassed = false;
            }
            
            if (allTestsPassed)
            {
                LogToFile("[FIXED_COLLISION_TEST] SUCCESS: All tests passed!");
                EditorApplication.Exit(0);
            }
            else
            {
                LogToFile("[FIXED_COLLISION_TEST] FAILURE: Some tests failed!");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogToFile($"[FIXED_COLLISION_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
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