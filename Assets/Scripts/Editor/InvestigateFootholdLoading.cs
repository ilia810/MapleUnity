using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Linq;
using System.Reflection;
using GameData;

public static class InvestigateFootholdLoading
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        File.WriteAllText(logPath, $"[FOOTHOLD_INVESTIGATION] Starting at {DateTime.Now}\n");
        
        try
        {
            // First, let's check what NX data says about footholds
            LogToFile("[FOOTHOLD_INVESTIGATION] Checking NX data directly...");
            
            var mapId = 100000000; // Henesys
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(mapId);
            
            if (mapNode == null)
            {
                LogToFile("[FOOTHOLD_INVESTIGATION] ERROR: Could not load map node!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Check foothold data in NX
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null)
            {
                LogToFile("[FOOTHOLD_INVESTIGATION] ERROR: No foothold node in map data!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[FOOTHOLD_INVESTIGATION] Foothold node has {footholdNode.Children.Count()} layers");
            
            // Count total footholds in NX data
            int totalFootholdsInNX = 0;
            foreach (var layer in footholdNode.Children)
            {
                LogToFile($"  Layer {layer.Name} has {layer.Children.Count()} groups");
                foreach (var group in layer.Children)
                {
                    LogToFile($"    Group {group.Name} has {group.Children.Count()} footholds");
                    totalFootholdsInNX += group.Children.Count();
                }
            }
            
            LogToFile($"[FOOTHOLD_INVESTIGATION] Total footholds in NX data: {totalFootholdsInNX}");
            
            // Now create scene and check what gets loaded
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            // Check FootholdGenerator specifically
            var footholdGen = generator.GetComponent<FootholdGenerator>();
            if (footholdGen == null)
            {
                LogToFile("[FOOTHOLD_INVESTIGATION] ERROR: No FootholdGenerator found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[FOOTHOLD_INVESTIGATION] Generating map scene...");
            GameObject mapRoot = generator.GenerateMapScene(mapId);
            GameObject.DestroyImmediate(generatorObj);
            
            // Check FootholdManager
            var footholdManager = mapRoot.GetComponent<FootholdManager>();
            if (footholdManager != null)
            {
                var footholds = footholdManager.GetAllFootholds();
                LogToFile($"[FOOTHOLD_INVESTIGATION] FootholdManager reports {footholds?.Count ?? 0} footholds");
                
                // Try to get more details
                var footholdField = footholdManager.GetType().GetField("footholds", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (footholdField != null)
                {
                    var footholdsList = footholdField.GetValue(footholdManager);
                    LogToFile($"[FOOTHOLD_INVESTIGATION] Footholds field type: {footholdsList?.GetType()}");
                }
            }
            
            // Initialize GameManager to check FootholdService
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            var awakeMethod = gameManager.GetType().GetMethod("Awake", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(gameManager, null);
            
            var startMethod = gameManager.GetType().GetMethod("Start", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            startMethod?.Invoke(gameManager, null);
            
            var footholdService = gameManager.FootholdService;
            if (footholdService != null)
            {
                var allFootholds = footholdService.GetFootholdsInArea(-100000, -10000, 100000, 10000);
                LogToFile($"[FOOTHOLD_INVESTIGATION] FootholdService has {allFootholds.Count()} footholds in area");
                
                // List first 10 footholds
                int count = 0;
                foreach (var fh in allFootholds.Take(10))
                {
                    LogToFile($"  Foothold {count++}: X1={fh.X1}, Y1={fh.Y1}, X2={fh.X2}, Y2={fh.Y2}");
                }
            }
            
            // Check for any error logs
            LogToFile("\n[FOOTHOLD_INVESTIGATION] Checking for loading errors...");
            
            // Manually call foothold loading method to see if there are issues
            var loaderField = gameManager.GetType().GetField("footholdLoader", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (loaderField != null)
            {
                var loader = loaderField.GetValue(gameManager);
                LogToFile($"[FOOTHOLD_INVESTIGATION] FootholdLoader type: {loader?.GetType()}");
            }
            
            LogToFile("[FOOTHOLD_INVESTIGATION] Investigation complete");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[FOOTHOLD_INVESTIGATION] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}