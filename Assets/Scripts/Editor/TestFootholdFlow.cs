using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using MapleClient.GameView;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using System.Reflection;

public static class TestFootholdFlow
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        File.WriteAllText(logPath, $"[TEST_FOOTHOLD_FLOW] Starting at {DateTime.Now}\n");
        
        try
        {
            // Create a new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[TEST_FOOTHOLD_FLOW] Created new scene");
            
            // Create GameManager manually to trace foothold loading
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Create FootholdService manually
            var footholdService = new FootholdService();
            LogToFile($"[TEST_FOOTHOLD_FLOW] Created FootholdService");
            
            // Create NxMapLoader with the service
            var mapLoader = new NxMapLoader("", footholdService);
            LogToFile($"[TEST_FOOTHOLD_FLOW] Created NxMapLoader");
            
            // Load map data
            LogToFile($"[TEST_FOOTHOLD_FLOW] Loading map 100000000...");
            var mapData = mapLoader.GetMap(100000000);
            
            if (mapData == null)
            {
                LogToFile("[TEST_FOOTHOLD_FLOW] ERROR: GetMap returned null!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[TEST_FOOTHOLD_FLOW] GetMap returned MapData with:");
            LogToFile($"  - Platforms: {mapData.Platforms.Count}");
            LogToFile($"  - Portals: {mapData.Portals.Count}");
            LogToFile($"  - NpcSpawns: {mapData.NpcSpawns.Count}");
            LogToFile($"  - MonsterSpawns: {mapData.MonsterSpawns.Count}");
            
            // Check what the FootholdService has
            var allFootholds = footholdService.GetFootholdsInArea(-100000, -10000, 100000, 10000);
            LogToFile($"[TEST_FOOTHOLD_FLOW] FootholdService now has {allFootholds.Count()} footholds");
            
            // List some footholds
            int count = 0;
            foreach (var fh in allFootholds)
            {
                if (count++ < 5)
                {
                    LogToFile($"  Foothold: ({fh.X1},{fh.Y1}) to ({fh.X2},{fh.Y2})");
                }
            }
            
            // Now test the SceneGeneration path
            LogToFile("\n[TEST_FOOTHOLD_FLOW] Testing SceneGeneration path...");
            
            var extractor = new MapleClient.SceneGeneration.MapDataExtractor();
            var sceneMapData = extractor.ExtractMapData(100000000);
            
            if (sceneMapData != null)
            {
                LogToFile($"[TEST_FOOTHOLD_FLOW] MapDataExtractor found {sceneMapData.Footholds?.Count ?? 0} footholds");
            }
            
            // Check the difference between Platforms and Footholds
            LogToFile($"\n[TEST_FOOTHOLD_FLOW] Key insight:");
            LogToFile($"  - NxMapLoader uses mapData.Platforms (count: {mapData.Platforms.Count})");
            LogToFile($"  - MapDataExtractor uses mapData.Footholds (count: {sceneMapData?.Footholds?.Count ?? 0})");
            LogToFile($"  - The issue is that NxMapLoader is loading Platforms, not Footholds!");
            
            LogToFile("\n[TEST_FOOTHOLD_FLOW] Test complete");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[TEST_FOOTHOLD_FLOW] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}