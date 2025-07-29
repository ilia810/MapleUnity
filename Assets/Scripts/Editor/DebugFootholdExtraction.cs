using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using GameData;
using MapleClient.GameData;
using System.Linq;

public static class DebugFootholdExtraction
{
    private static string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
    
    public static void RunTest()
    {
        File.WriteAllText(logPath, $"[DEBUG_FOOTHOLD_EXTRACTION] Starting at {DateTime.Now}\n");
        
        try
        {
            // Get the NX manager
            var nxManager = NXDataManagerSingleton.Instance;
            if (nxManager == null)
            {
                LogToFile("[DEBUG_FOOTHOLD_EXTRACTION] ERROR: NXDataManagerSingleton is null!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get map node for Henesys
            int mapId = 100000000;
            var mapNode = nxManager.GetMapNode(mapId);
            
            if (mapNode == null)
            {
                LogToFile("[DEBUG_FOOTHOLD_EXTRACTION] ERROR: Could not get map node!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[DEBUG_FOOTHOLD_EXTRACTION] Got map node successfully");
            
            // Check foothold node
            var footholdNode = mapNode["foothold"];
            if (footholdNode == null)
            {
                LogToFile("[DEBUG_FOOTHOLD_EXTRACTION] ERROR: No foothold node in map!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[DEBUG_FOOTHOLD_EXTRACTION] Foothold node exists with {footholdNode.Children.Count()} layers");
            
            // Debug print the entire foothold structure
            LogToFile("\n[DEBUG_FOOTHOLD_EXTRACTION] Full foothold structure:");
            int totalFootholds = 0;
            
            foreach (var layer in footholdNode.Children)
            {
                LogToFile($"  Layer {layer.Name}: {layer.Children.Count()} groups");
                
                foreach (var group in layer.Children)
                {
                    LogToFile($"    Group {group.Name}: {group.Children.Count()} footholds");
                    
                    foreach (var fh in group.Children)
                    {
                        var x1 = fh["x1"]?.GetValue<int>();
                        var y1 = fh["y1"]?.GetValue<int>();
                        var x2 = fh["x2"]?.GetValue<int>();
                        var y2 = fh["y2"]?.GetValue<int>();
                        var next = fh["next"]?.GetValue<int>();
                        var prev = fh["prev"]?.GetValue<int>();
                        
                        if (x1.HasValue && y1.HasValue && x2.HasValue && y2.HasValue)
                        {
                            LogToFile($"      Foothold {fh.Name}: ({x1},{y1}) to ({x2},{y2}) next={next ?? 0} prev={prev ?? 0}");
                            totalFootholds++;
                        }
                        else
                        {
                            LogToFile($"      Foothold {fh.Name}: MISSING COORDINATES!");
                            LogToFile($"        x1={x1}, y1={y1}, x2={x2}, y2={y2}");
                        }
                    }
                }
            }
            
            LogToFile($"\n[DEBUG_FOOTHOLD_EXTRACTION] Total valid footholds found: {totalFootholds}");
            
            // Now test the MapDataExtractor
            LogToFile("\n[DEBUG_FOOTHOLD_EXTRACTION] Testing MapDataExtractor...");
            
            var extractor = new MapDataExtractor();
            var mapData = extractor.ExtractMapData(mapId);
            
            if (mapData == null)
            {
                LogToFile("[DEBUG_FOOTHOLD_EXTRACTION] ERROR: MapDataExtractor returned null!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[DEBUG_FOOTHOLD_EXTRACTION] MapDataExtractor found {mapData.Footholds?.Count ?? 0} footholds");
            
            // List the extracted footholds
            if (mapData.Footholds != null)
            {
                LogToFile("\n[DEBUG_FOOTHOLD_EXTRACTION] Extracted footholds:");
                foreach (var fh in mapData.Footholds.Take(10))
                {
                    LogToFile($"  ID={fh.Id}: ({fh.X1},{fh.Y1}) to ({fh.X2},{fh.Y2})");
                }
                
                if (mapData.Footholds.Count > 10)
                {
                    LogToFile($"  ... and {mapData.Footholds.Count - 10} more");
                }
            }
            
            // Check for specific known foothold IDs
            LogToFile("\n[DEBUG_FOOTHOLD_EXTRACTION] Checking specific foothold ranges:");
            
            var groundFootholds = mapData.Footholds?.Where(f => f.Y1 >= 180 && f.Y1 <= 220).ToList();
            LogToFile($"  Footholds at ground level (Y 180-220): {groundFootholds?.Count ?? 0}");
            
            if (groundFootholds != null && groundFootholds.Count > 0)
            {
                LogToFile("  Ground footholds:");
                foreach (var fh in groundFootholds)
                {
                    LogToFile($"    ID={fh.Id}: X range [{fh.X1},{fh.X2}] at Y={fh.Y1}");
                }
            }
            
            LogToFile("\n[DEBUG_FOOTHOLD_EXTRACTION] Test complete");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[DEBUG_FOOTHOLD_EXTRACTION] EXCEPTION: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
}