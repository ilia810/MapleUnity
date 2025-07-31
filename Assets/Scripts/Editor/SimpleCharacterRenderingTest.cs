using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class SimpleCharacterRenderingTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\character-rendering-test-simple.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[TEST] Starting at {DateTime.Now}\n");
            Debug.Log("[TEST] Starting character rendering test...");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[TEST] Created new scene");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            LogToFile("[TEST] Initialized generators");
            
            // Generate map
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            if (mapRoot == null)
            {
                LogToFile("[TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            LogToFile("[TEST] Generated map successfully");
            
            // Clean up generator
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            var gmComponent = gameManagerObj.AddComponent<MapleClient.GameView.GameManager>();
            LogToFile("[TEST] Created GameManager");
            
            // Try to initialize GameManager to spawn player
            try
            {
                // Call Start method using reflection
                var startMethod = gmComponent.GetType().GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(gmComponent, null);
                    LogToFile("[TEST] Called GameManager.Start()");
                }
            }
            catch (Exception e)
            {
                LogToFile($"[TEST] Error calling Start: {e.Message}");
            }
            
            // Force update
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                // Try to create player manually
                LogToFile("[TEST] Player not found, creating manually...");
                
                playerGO = new GameObject("Player");
                playerGO.transform.position = new Vector3(0, -1.5f, 0);
                
                // Add PlayerView component
                var playerViewType = System.Type.GetType("MapleClient.GameView.PlayerView, Assembly-CSharp");
                if (playerViewType != null)
                {
                    var playerView = playerGO.AddComponent(playerViewType) as MonoBehaviour;
                    LogToFile("[TEST] Added PlayerView component");
                }
                
                // Add MapleCharacterRenderer
                var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
                if (rendererType != null)
                {
                    var renderer = playerGO.AddComponent(rendererType) as MonoBehaviour;
                    LogToFile("[TEST] Added MapleCharacterRenderer component");
                    
                    // Wait a frame for components to initialize
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                }
            }
            
            LogToFile($"[TEST] Player found at: {playerGO.transform.position}");
            
            // Get character renderer
            var characterRenderer = playerGO.GetComponentInChildren<MapleClient.GameView.MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                LogToFile("[TEST] WARNING: MapleCharacterRenderer not found, test will continue with limited analysis");
            }
            
            if (characterRenderer != null)
            {
                LogToFile("[TEST] Found MapleCharacterRenderer");
                
                // Analyze rendering
                AnalyzeCharacterRendering(characterRenderer);
                
                // Apply fixes
                ApplyRenderingFixes(characterRenderer);
                
                // Re-analyze
                AnalyzeCharacterRendering(characterRenderer, "AFTER FIXES");
            }
            else
            {
                // Analyze sprite renderers directly
                var spriteRenderers = playerGO.GetComponentsInChildren<SpriteRenderer>();
                LogToFile($"\n[TEST] Direct sprite renderer analysis (found {spriteRenderers.Length} renderers):");
                foreach (var sr in spriteRenderers)
                {
                    LogToFile($"\nRenderer: {sr.name}");
                    LogToFile($"  - Active: {sr.gameObject.activeSelf}, Enabled: {sr.enabled}");
                    LogToFile($"  - Position: {sr.transform.position}");
                    LogToFile($"  - Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
                }
            }
            
            LogToFile("\n[TEST] Test completed successfully!");
            Debug.Log("[TEST] Test completed successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[TEST] ERROR: {e.Message}\n{e.StackTrace}");
            Debug.LogError($"[TEST] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacterRendering(MapleClient.GameView.MapleCharacterRenderer renderer, string phase = "INITIAL")
    {
        LogToFile($"\n[TEST] === {phase} ANALYSIS ===");
        
        var spriteRenderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        LogToFile($"Total sprite renderers: {spriteRenderers.Length}");
        
        foreach (var sr in spriteRenderers)
        {
            if (sr.name == "Player") continue;
            
            LogToFile($"\nRenderer: {sr.name}");
            LogToFile($"  - Active: {sr.gameObject.activeSelf}, Enabled: {sr.enabled}");
            LogToFile($"  - Local pos: {sr.transform.localPosition}");
            LogToFile($"  - Sorting order: {sr.sortingOrder}");
            LogToFile($"  - Flip X: {sr.flipX}");
            
            if (sr.sprite != null)
            {
                LogToFile($"  - Sprite: {sr.sprite.name}");
            }
            else
            {
                LogToFile($"  - Sprite: NULL");
            }
        }
    }
    
    private static void ApplyRenderingFixes(MapleClient.GameView.MapleCharacterRenderer renderer)
    {
        LogToFile("\n[TEST] === APPLYING FIXES ===");
        
        // Fix head position
        var headRenderer = FindChildRenderer(renderer, "Head");
        var bodyRenderer = FindChildRenderer(renderer, "Body");
        
        if (headRenderer != null && bodyRenderer != null)
        {
            float headOffset = 0.45f;
            Vector3 newHeadPos = new Vector3(0, headOffset, 0);
            headRenderer.transform.localPosition = newHeadPos;
            LogToFile($"Fixed head position to: {newHeadPos}");
            
            // Also fix face position
            var faceRenderer = FindChildRenderer(renderer, "Face");
            if (faceRenderer != null)
            {
                faceRenderer.transform.localPosition = newHeadPos;
                LogToFile($"Fixed face position to: {newHeadPos}");
            }
        }
        
        // Force sprite update
        var updateMethod = renderer.GetType().GetMethod("UpdateSprites", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (updateMethod != null)
        {
            updateMethod.Invoke(renderer, null);
            LogToFile("Forced sprite update");
        }
    }
    
    private static SpriteRenderer FindChildRenderer(MapleClient.GameView.MapleCharacterRenderer renderer, string name)
    {
        var renderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            if (r.name == name) return r;
        }
        return null;
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}