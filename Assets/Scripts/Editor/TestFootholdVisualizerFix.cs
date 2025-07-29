using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameView.Debugging;
using System.Linq;

public static class TestFootholdVisualizerFix
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        File.WriteAllText(logPath, $"[FOOTHOLD_VIS_TEST] Starting at {DateTime.Now}\n");
        
        try
        {
            // Create scene and generate map
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            GameObject.DestroyImmediate(generatorObj);
            LogToFile("[FOOTHOLD_VIS_TEST] Map generated");
            
            // Initialize GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            // Get actual scene footholds
            var footholdDataComponents = GameObject.FindObjectsOfType<FootholdData>();
            LogToFile($"[FOOTHOLD_VIS_TEST] Found {footholdDataComponents.Length} FootholdData components in scene");
            
            // Sample a few footholds from the scene
            LogToFile("\n[FOOTHOLD_VIS_TEST] Scene foothold positions (Unity coords):");
            int count = 0;
            foreach (var fhData in footholdDataComponents.Take(5))
            {
                var collider = fhData.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    var pos = fhData.transform.position;
                    LogToFile($"  Foothold {fhData.footholdId}: Center at ({pos.x:F2}, {pos.y:F2}), Size: {collider.size}");
                }
            }
            
            // Get footholds from FootholdService (MapleStory coords)
            var footholdService = gameManager.FootholdService;
            if (footholdService != null)
            {
                var serviceFootholds = footholdService.GetFootholdsInArea(-10000, -1000, 10000, 1000).Take(5).ToList();
                LogToFile("\n[FOOTHOLD_VIS_TEST] FootholdService data (MapleStory coords):");
                foreach (var fh in serviceFootholds)
                {
                    LogToFile($"  Foothold {fh.Id}: ({fh.X1},{fh.Y1}) to ({fh.X2},{fh.Y2})");
                    
                    // Convert to Unity coords to verify
                    var unityStart = MapleClient.SceneGeneration.CoordinateConverter.ToUnityPosition(fh.X1, fh.Y1);
                    var unityEnd = MapleClient.SceneGeneration.CoordinateConverter.ToUnityPosition(fh.X2, fh.Y2);
                    LogToFile($"    -> Unity: ({unityStart.x:F2},{unityStart.y:F2}) to ({unityEnd.x:F2},{unityEnd.y:F2})");
                }
            }
            
            // Create visualizer
            GameObject debugObj = new GameObject("FootholdDebugVisualizer");
            var visualizer = debugObj.AddComponent<FootholdDebugVisualizer>();
            visualizer.SetFootholdService(footholdService);
            LogToFile("\n[FOOTHOLD_VIS_TEST] Created FootholdDebugVisualizer");
            
            LogToFile("\n[FOOTHOLD_VIS_TEST] Test complete - visualizer should now match scene footholds");
            LogToFile("Check if the green debug lines align with the actual foothold colliders in the scene view");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[FOOTHOLD_VIS_TEST] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}