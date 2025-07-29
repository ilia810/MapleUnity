using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using System.Linq;

public static class RealMapCollisionTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        var startTime = DateTime.Now;
        File.WriteAllText(logPath, $"[REAL_MAP_TEST] Starting at {startTime}\n");
        
        try
        {
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[REAL_MAP_TEST] Created new scene");
            
            // Generate the actual Henesys map
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            if (mapRoot == null)
            {
                LogToFile("[REAL_MAP_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get foothold info from generated map
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            if (footholdManager != null)
            {
                var allFootholds = footholdManager.GetAllFootholds();
                int footholdCount = allFootholds?.Count ?? 0;
                LogToFile($"[REAL_MAP_TEST] Map generated with {footholdCount} footholds");
                
                // Log some foothold details
                var footholds = footholdManager.GetAllFootholds();
                if (footholds != null && footholds.Count > 0)
                {
                    LogToFile($"[REAL_MAP_TEST] First 5 footholds:");
                    int count = 0;
                    foreach (var fh in footholds.Take(5))
                    {
                        LogToFile($"  FH{fh.Id}: X[{fh.X1},{fh.X2}] Y[{fh.Y1},{fh.Y2}]");
                        count++;
                    }
                    
                    // Find main ground footholds
                    var groundFootholds = footholds.Where(f => f.Y1 == f.Y2 && f.Y1 > 100 && f.Y1 < 300).ToList();
                    LogToFile($"[REAL_MAP_TEST] Found {groundFootholds.Count} potential ground footholds (Y between 100-300)");
                    
                    foreach (var gf in groundFootholds.Take(3))
                    {
                        LogToFile($"  Ground FH{gf.Id}: X[{gf.X1},{gf.X2}] Y={gf.Y1}");
                    }
                }
            }
            
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Initialize
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            LogToFile("[REAL_MAP_TEST] GameManager initialized");
            
            // Check FootholdService
            var footholdService = gameManager.FootholdService;
            if (footholdService != null)
            {
                LogToFile("[REAL_MAP_TEST] FootholdService available, testing ground detection:");
                
                // Test at various X positions
                float[] testX = { -400f, -200f, 0f, 200f, 400f, 800f, 1000f };
                foreach (float x in testX)
                {
                    float ground = footholdService.GetGroundBelow(x, 0);
                    LogToFile($"  Ground at X={x}: Y={ground}");
                }
            }
            
            // Find player
            var player = GameObject.Find("Player");
            if (player == null)
            {
                LogToFile("[REAL_MAP_TEST] ERROR: Player not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            float spawnY = player.transform.position.y;
            LogToFile($"[REAL_MAP_TEST] Player spawned at: {player.transform.position}");
            
            // Try to get player physics info
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                LogToFile($"[REAL_MAP_TEST] Player has Rigidbody - Velocity: {rb.velocity}, Is Kinematic: {rb.isKinematic}");
            }
            
            var collider = player.GetComponent<Collider>();
            if (collider != null)
            {
                LogToFile($"[REAL_MAP_TEST] Player has Collider - Enabled: {collider.enabled}, Bounds: {collider.bounds}");
            }
            
            // Simulate physics
            LogToFile("[REAL_MAP_TEST] Simulating physics for 2 seconds...");
            for (int i = 0; i < 120; i++) // 2 seconds at 60 FPS
            {
                Physics.Simulate(1f/60f);
                
                // Log position every 30 frames (0.5 seconds)
                if (i % 30 == 0)
                {
                    LogToFile($"  Frame {i}: Player Y = {player.transform.position.y:F3}");
                }
            }
            
            float finalY = player.transform.position.y;
            LogToFile($"[REAL_MAP_TEST] Final player position: {player.transform.position}");
            
            // Test if player fell through map
            bool fellThrough = finalY < -10f;
            LogToFile($"[REAL_MAP_TEST] Player fell through map: {fellThrough}");
            
            // Test movement
            LogToFile("[REAL_MAP_TEST] Testing horizontal movement...");
            
            // Move right
            player.transform.position = new Vector3(5f, spawnY, 0);
            Physics.Simulate(1f);
            LogToFile($"  After moving to X=5: Y={player.transform.position.y:F3}");
            
            // Move more right
            player.transform.position = new Vector3(10f, spawnY, 0);
            Physics.Simulate(1f);
            LogToFile($"  After moving to X=10: Y={player.transform.position.y:F3}");
            
            // Move left
            player.transform.position = new Vector3(-5f, spawnY, 0);
            Physics.Simulate(1f);
            LogToFile($"  After moving to X=-5: Y={player.transform.position.y:F3}");
            
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogToFile($"[REAL_MAP_TEST] Test duration: {duration:F2}s");
            LogToFile($"[REAL_MAP_TEST] Test complete - Check results above");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[REAL_MAP_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
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