using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using GameData;

public static class SimpleFootholdCheck
{
    [MenuItem("MapleUnity/Debug/Check Foothold Count")]
    public static void CheckFootholds()
    {
        var logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
        File.WriteAllText(logPath, $"[SIMPLE_FOOTHOLD_CHECK] Starting at {DateTime.Now}\n");
        
        try
        {
            // Use the NX manager to check footholds
            var nxManager = NXDataManagerSingleton.Instance;
            var mapNode = nxManager.GetMapNode(100000000);
            
            if (mapNode == null)
            {
                Debug.LogError("Could not get map node!");
                File.AppendAllText(logPath, "[SIMPLE_FOOTHOLD_CHECK] ERROR: Could not get map node!\n");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, "[SIMPLE_FOOTHOLD_CHECK] Got map node\n");
            
            // Navigate to footholds
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null)
            {
                Debug.LogError("No foothold node!");
                File.AppendAllText(logPath, "[SIMPLE_FOOTHOLD_CHECK] ERROR: No foothold node!\n");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            
            int totalCount = 0;
            
            // Count all footholds
            foreach (var layer in footholdNode.Children)
            {
                foreach (var group in layer.Children)
                {
                    foreach (var fh in group.Children)
                    {
                        if (fh["x1"] != null && fh["y1"] != null && 
                            fh["x2"] != null && fh["y2"] != null)
                        {
                            totalCount++;
                        }
                    }
                }
            }
            
            var message = $"[SIMPLE_FOOTHOLD_CHECK] Total footholds in NX data: {totalCount}";
            Debug.Log(message);
            File.AppendAllText(logPath, message + "\n");
            
            // Also check what MapDataExtractor finds
            var extractor = new MapleClient.SceneGeneration.MapDataExtractor();
            var mapData = extractor.ExtractMapData(100000000);
            
            if (mapData != null && mapData.Footholds != null)
            {
                message = $"[SIMPLE_FOOTHOLD_CHECK] MapDataExtractor found: {mapData.Footholds.Count} footholds";
                Debug.Log(message);
                File.AppendAllText(logPath, message + "\n");
            }
            
            File.AppendAllText(logPath, "[SIMPLE_FOOTHOLD_CHECK] Complete\n");
            
            if (Application.isBatchMode) 
            {
                EditorApplication.Exit(totalCount > 4 ? 0 : 1);
            }
        }
        catch (Exception e)
        {
            var error = $"[SIMPLE_FOOTHOLD_CHECK] Exception: {e.Message}\n{e.StackTrace}";
            Debug.LogError(error);
            File.AppendAllText(logPath, error + "\n");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
    
    public static void RunTest()
    {
        CheckFootholds();
    }
}