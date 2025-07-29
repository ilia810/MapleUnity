using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using System.Linq;
using System.Reflection;

public static class FootholdCoverageTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        File.WriteAllText(logPath, $"[FOOTHOLD_COVERAGE] Starting at {DateTime.Now}\n");
        
        try
        {
            // Create scene and generate map
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            GameObject.DestroyImmediate(generatorObj);
            
            // Initialize GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            var awakeMethod = gameManager.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            // Get FootholdService
            var footholdService = gameManager.FootholdService;
            if (footholdService == null)
            {
                LogToFile("[FOOTHOLD_COVERAGE] ERROR: No FootholdService!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get all footholds in the area
            var allFootholds = footholdService.GetFootholdsInArea(-10000, -1000, 10000, 1000);
            LogToFile($"[FOOTHOLD_COVERAGE] Total footholds loaded: {allFootholds.Count()}");
            
            // Group footholds by Y coordinate to find platforms
            var platformGroups = allFootholds.GroupBy(f => f.Y1).OrderBy(g => g.Key);
            LogToFile($"[FOOTHOLD_COVERAGE] Found {platformGroups.Count()} distinct Y levels");
            
            foreach (var group in platformGroups.Take(5))
            {
                var footholds = group.OrderBy(f => f.X1).ToList();
                float minX = footholds.Min(f => f.X1);
                float maxX = footholds.Max(f => f.X2);
                LogToFile($"  Y={group.Key}: {footholds.Count} footholds, X range [{minX},{maxX}]");
            }
            
            // Test ground detection at specific X positions
            LogToFile("\n[FOOTHOLD_COVERAGE] Testing ground detection at key positions:");
            
            float[] testX = { 0, 100, 200, 300, 400, 500, 1000, 2000, 3000, 4000, 4500, 5000, 5500 };
            foreach (float x in testX)
            {
                float ground = footholdService.GetGroundBelow(x, 0);
                bool hasGround = ground != float.MaxValue;
                LogToFile($"  X={x}: Ground at Y={ground} {(hasGround ? "" : "(NO GROUND!)")}");
            }
            
            // Find gaps in coverage
            LogToFile("\n[FOOTHOLD_COVERAGE] Checking for coverage gaps:");
            
            // Get main platform footholds (around Y=200)
            var mainPlatform = allFootholds.Where(f => f.Y1 >= 180 && f.Y1 <= 220).OrderBy(f => f.X1).ToList();
            LogToFile($"Main platform has {mainPlatform.Count} footholds");
            
            if (mainPlatform.Count > 0)
            {
                // Check for gaps
                for (int i = 0; i < mainPlatform.Count - 1; i++)
                {
                    var current = mainPlatform[i];
                    var next = mainPlatform[i + 1];
                    
                    float gap = next.X1 - current.X2;
                    if (gap > 1)
                    {
                        LogToFile($"  GAP found: Between X={current.X2} and X={next.X1} (gap size: {gap})");
                    }
                }
                
                // Check total coverage
                float totalMinX = mainPlatform.Min(f => f.X1);
                float totalMaxX = mainPlatform.Max(f => f.X2);
                LogToFile($"Main platform total coverage: X[{totalMinX},{totalMaxX}]");
            }
            
            // Check what FootholdManager sees
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            if (footholdManager != null)
            {
                // LogToFile($"\n[FOOTHOLD_COVERAGE] FootholdManager reports {footholdManager.GetFootholdCount()} footholds");
                LogToFile($"\n[FOOTHOLD_COVERAGE] FootholdManager component found");
            }
            
            LogToFile("\n[FOOTHOLD_COVERAGE] Test complete");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[FOOTHOLD_COVERAGE] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}