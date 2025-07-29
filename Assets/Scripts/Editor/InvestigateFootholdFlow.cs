using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.GameView;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.SceneGeneration;
using System.Linq;

public static class InvestigateFootholdFlow
{
    private static string logPath = @"C:\Users\me\MapleUnity\foothold_flow_investigation.log";
    
    [MenuItem("MapleTools/Investigate Foothold Flow")]
    public static void RunInvestigation()
    {
        File.WriteAllText(logPath, $"[FOOTHOLD_FLOW] Starting investigation at {DateTime.Now}\n");
        
        try
        {
            // Create a new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[FOOTHOLD_FLOW] Created new scene");
            
            // Step 1: Check NX data directly
            LogToFile("\n=== STEP 1: Checking NX Data Directly ===");
            var extractor = new MapDataExtractor();
            var mapData = extractor.ExtractMapData(100000000);
            
            if (mapData != null)
            {
                LogToFile($"MapDataExtractor found:");
                LogToFile($"  - Footholds: {mapData.Footholds?.Count ?? 0}");
                if (mapData.Footholds != null && mapData.Footholds.Count > 0)
                {
                    var firstFh = mapData.Footholds[0];
                    LogToFile($"  - First foothold: ({firstFh.X1},{firstFh.Y1}) to ({firstFh.X2},{firstFh.Y2})");
                }
            }
            
            // Step 2: Check what NxMapLoader does
            LogToFile("\n=== STEP 2: Checking NxMapLoader ===");
            
            // Create GameManager and dependencies
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Create FootholdService
            var footholdService = new FootholdService();
            LogToFile($"Created FootholdService");
            
            // Create NxMapLoader
            var mapLoader = new NxMapLoader("", footholdService);
            LogToFile($"Created NxMapLoader");
            
            // Get map through NxMapLoader
            var nxMapData = mapLoader.GetMap(100000000);
            
            if (nxMapData != null)
            {
                LogToFile($"NxMapLoader.GetMap returned:");
                LogToFile($"  - Platforms: {nxMapData.Platforms.Count}");
                LogToFile($"  - Portals: {nxMapData.Portals.Count}");
                LogToFile($"  - NpcSpawns: {nxMapData.NpcSpawns.Count}");
                LogToFile($"  - MonsterSpawns: {nxMapData.MonsterSpawns.Count}");
                
                // Check if Platforms is being used instead of Footholds
                if (nxMapData.Platforms.Count > 0)
                {
                    var firstPlatform = nxMapData.Platforms[0];
                    LogToFile($"  - First platform: ({firstPlatform.X1},{firstPlatform.Y1}) to ({firstPlatform.X2},{firstPlatform.Y2})");
                }
            }
            
            // Step 3: Check FootholdService state
            LogToFile("\n=== STEP 3: Checking FootholdService State ===");
            var allFootholds = footholdService.GetFootholdsInArea(-100000, -10000, 100000, 10000);
            LogToFile($"FootholdService.GetFootholdsInArea returned: {allFootholds.Count()} footholds");
            
            int count = 0;
            foreach (var fh in allFootholds)
            {
                if (count++ < 3)
                {
                    LogToFile($"  - Foothold: ({fh.X1},{fh.Y1}) to ({fh.X2},{fh.Y2})");
                }
            }
            
            // Step 4: Check SceneGeneration flow
            LogToFile("\n=== STEP 4: Checking SceneGeneration Flow ===");
            
            // Create scene with FootholdManager
            GameObject footholdManagerObj = new GameObject("FootholdManager");
            var footholdManager = footholdManagerObj.AddComponent<FootholdManager>();
            
            // Generate map scene
            var generator = new MapSceneGenerator();
            generator.GenerateMapScene(100000000);
            
            // Check FootholdManager state
            var footholdDataObjects = GameObject.FindObjectsOfType<FootholdData>();
            LogToFile($"FootholdManager created {footholdDataObjects.Length} FootholdData components");
            
            // Key insight
            LogToFile("\n=== KEY INSIGHT ===");
            LogToFile($"The discrepancy is:");
            LogToFile($"1. MapDataExtractor finds {mapData?.Footholds?.Count ?? 0} footholds in NX data");
            LogToFile($"2. NxMapLoader returns MapData with {nxMapData?.Platforms.Count ?? 0} Platforms (not Footholds!)");
            LogToFile($"3. FootholdService receives {allFootholds.Count()} footholds");
            LogToFile($"4. The issue is that NxMapLoader is using Platforms instead of Footholds!");
            
            LogToFile("\n[FOOTHOLD_FLOW] Investigation complete");
            
            // Clean up
            GameObject.DestroyImmediate(gameManagerObj);
            GameObject.DestroyImmediate(footholdManagerObj);
            
            Debug.Log($"Foothold flow investigation complete. Results written to: {logPath}");
        }
        catch (Exception e)
        {
            LogToFile($"[FOOTHOLD_FLOW] EXCEPTION: {e.Message}\n{e.StackTrace}");
            Debug.LogError($"Investigation failed: {e.Message}");
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}