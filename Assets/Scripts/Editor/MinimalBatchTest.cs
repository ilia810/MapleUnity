using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class MinimalBatchTest
{
    public static void RunMinimalTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
        
        try
        {
            File.WriteAllText(logPath, "[MINIMAL_BATCH] Test started successfully\n");
            Debug.Log("[MINIMAL_BATCH] Unity is running in batch mode");
            
            // Log Unity version
            File.AppendAllText(logPath, $"[MINIMAL_BATCH] Unity Version: {Application.unityVersion}\n");
            
            // Log project path
            File.AppendAllText(logPath, $"[MINIMAL_BATCH] Project Path: {Application.dataPath}\n");
            
            // Log if we're in batch mode
            File.AppendAllText(logPath, $"[MINIMAL_BATCH] Is Batch Mode: {Application.isBatchMode}\n");
            
            // Try to open and test the scene
            if (Application.isBatchMode)
            {
                File.AppendAllText(logPath, "[MINIMAL_BATCH] Running in batch mode, attempting scene operations...\n");
                
                // Try to open scene
                try 
                {
                    var scene = EditorSceneManager.OpenScene("Assets/henesys.unity", OpenSceneMode.Single);
                    File.AppendAllText(logPath, $"[MINIMAL_BATCH] Scene opened: {scene.name}\n");
                    File.AppendAllText(logPath, $"[MINIMAL_BATCH] Scene path: {scene.path}\n");
                    File.AppendAllText(logPath, $"[MINIMAL_BATCH] Scene is loaded: {scene.isLoaded}\n");
                    
                    // Log some scene info
                    var rootObjects = scene.GetRootGameObjects();
                    File.AppendAllText(logPath, $"[MINIMAL_BATCH] Root objects count: {rootObjects.Length}\n");
                    
                    foreach (var obj in rootObjects)
                    {
                        if (obj.name == "Player" || obj.name.Contains("Player"))
                        {
                            File.AppendAllText(logPath, $"[MINIMAL_BATCH] Found player object: {obj.name} at {obj.transform.position}\n");
                            
                            // Test different positions
                            Vector3[] testPositions = new Vector3[]
                            {
                                new Vector3(0, -1.5f, 0),
                                new Vector3(30f, -1.5f, 0),
                                new Vector3(45f, -1.5f, 0),
                                new Vector3(-30f, -1.5f, 0),
                                new Vector3(-45f, -1.5f, 0),
                                new Vector3(49f, -1.5f, 0)
                            };
                            
                            File.AppendAllText(logPath, "\n[MINIMAL_BATCH] Testing player positions:\n");
                            foreach (var pos in testPositions)
                            {
                                obj.transform.position = pos;
                                File.AppendAllText(logPath, $"[MINIMAL_BATCH] Moved player to: {pos}\n");
                                
                                // Simulate physics update
                                Physics.Simulate(0.02f);
                                
                                File.AppendAllText(logPath, $"[MINIMAL_BATCH] Player position after physics: {obj.transform.position}\n");
                            }
                        }
                        File.AppendAllText(logPath, $"[MINIMAL_BATCH] Object: {obj.name}\n");
                    }
                }
                catch (System.Exception e)
                {
                    File.AppendAllText(logPath, $"[MINIMAL_BATCH] Error opening scene: {e.Message}\n");
                }
            }
            
            File.AppendAllText(logPath, "[MINIMAL_BATCH] Test completed successfully\n");
            Debug.Log("[MINIMAL_BATCH] Test completed successfully");
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"[MINIMAL_BATCH] ERROR: {e.Message}\n");
            Debug.LogError($"[MINIMAL_BATCH] ERROR: {e.Message}");
        }
    }
}