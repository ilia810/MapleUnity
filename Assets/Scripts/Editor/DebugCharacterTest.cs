using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

public static class DebugCharacterTest
{
    private static string resultPath = @"C:\Users\me\MapleUnity\character-test-results.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(resultPath, "[TEST] Starting character rendering debug test\n");
            File.AppendAllText(resultPath, $"[TEST] Time: {DateTime.Now}\n\n");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(resultPath, $"[TEST] Opened scene: {scene.name}\n");
            
            // Find GameManager and trigger initialization
            var gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                File.AppendAllText(resultPath, "[TEST] Found GameManager\n");
                
                var gmComponent = gameManager.GetComponent<MonoBehaviour>();
                if (gmComponent != null)
                {
                    // Force Awake
                    InvokeMethod(gmComponent, "Awake");
                    File.AppendAllText(resultPath, "[TEST] Called GameManager.Awake()\n");
                    
                    // Force Start
                    InvokeMethod(gmComponent, "Start");
                    File.AppendAllText(resultPath, "[TEST] Called GameManager.Start()\n");
                }
            }
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Player not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, $"\n[TEST] Player found at: {playerGO.transform.position}\n");
            
            // List all components on Player
            var allComponents = playerGO.GetComponents<Component>();
            File.AppendAllText(resultPath, $"[TEST] Player has {allComponents.Length} components:\n");
            foreach (var comp in allComponents)
            {
                File.AppendAllText(resultPath, $"  - {comp.GetType().Name}\n");
            }
            
            // Find PlayerView
            MonoBehaviour playerView = null;
            MonoBehaviour mapleCharRenderer = null;
            
            foreach (var comp in playerGO.GetComponents<MonoBehaviour>())
            {
                if (comp.GetType().Name == "PlayerView")
                    playerView = comp;
                else if (comp.GetType().Name == "MapleCharacterRenderer")
                    mapleCharRenderer = comp;
            }
            
            if (playerView != null)
            {
                File.AppendAllText(resultPath, "\n[TEST] Found PlayerView\n");
                
                // Force initialization
                InvokeMethod(playerView, "Awake");
                File.AppendAllText(resultPath, "[TEST] Called PlayerView.Awake()\n");
                
                // Check if MapleCharacterRenderer was created
                mapleCharRenderer = playerGO.GetComponent<MonoBehaviour>();
                foreach (var comp in playerGO.GetComponents<MonoBehaviour>())
                {
                    if (comp.GetType().Name == "MapleCharacterRenderer")
                    {
                        mapleCharRenderer = comp;
                        break;
                    }
                }
                
                if (mapleCharRenderer != null)
                {
                    File.AppendAllText(resultPath, "[TEST] MapleCharacterRenderer component was created\n");
                }
                
                // Call Start
                InvokeMethod(playerView, "Start");
                File.AppendAllText(resultPath, "[TEST] Called PlayerView.Start()\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[ERROR] PlayerView not found\n");
            }
            
            // Check child objects
            File.AppendAllText(resultPath, $"\n[TEST] Player has {playerGO.transform.childCount} children:\n");
            for (int i = 0; i < playerGO.transform.childCount; i++)
            {
                var child = playerGO.transform.GetChild(i);
                File.AppendAllText(resultPath, $"  - {child.name} at {child.localPosition}\n");
                
                // Check for sprite renderers on children
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    File.AppendAllText(resultPath, $"    Has SpriteRenderer: enabled={sr.enabled}, sprite={(sr.sprite != null ? sr.sprite.name : "null")}\n");
                }
            }
            
            // Get all sprite renderers
            var renderers = playerGO.GetComponentsInChildren<SpriteRenderer>(true); // Include inactive
            File.AppendAllText(resultPath, $"\n[TEST] Found {renderers.Length} SpriteRenderers (including inactive):\n");
            
            foreach (var renderer in renderers)
            {
                File.AppendAllText(resultPath, $"  - {renderer.name}: active={renderer.gameObject.activeSelf}, enabled={renderer.enabled}, sprite={(renderer.sprite != null ? renderer.sprite.name : "null")}\n");
            }
            
            // Run character checks if we have renderers
            if (renderers.Length > 0)
            {
                CheckHeadPosition(renderers);
                CheckFacingDirection(renderers);
                CheckFaceFeatures(renderers);
            }
            else
            {
                File.AppendAllText(resultPath, "\n[ERROR] No sprite renderers found - character not properly initialized\n");
            }
            
            File.AppendAllText(resultPath, $"\n[TEST] Test finished at: {DateTime.Now}\n");
            Debug.Log($"Test results written to: {resultPath}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"\n[ERROR] Exception: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void InvokeMethod(object obj, string methodName)
    {
        try
        {
            var method = obj.GetType().GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(obj, null);
            }
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"[ERROR] Failed to invoke {methodName}: {e.Message}\n");
        }
    }
    
    private static void CheckHeadPosition(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== HEAD POSITION CHECK ===\n");
        
        var headRenderer = renderers.FirstOrDefault(r => r.name == "Head");
        var bodyRenderer = renderers.FirstOrDefault(r => r.name == "Body");
        
        if (headRenderer != null && bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body position: {bodyRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head position: {headRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head local position: {headRenderer.transform.localPosition}\n");
            
            float yDiff = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
            File.AppendAllText(resultPath, $"Head Y offset from body: {yDiff}\n");
            
            if (yDiff > 0)
            {
                File.AppendAllText(resultPath, "[PASS] Head is ABOVE body (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Head is BELOW body (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, $"[FAIL] Could not find renderers - Head: {headRenderer != null}, Body: {bodyRenderer != null}\n");
        }
    }
    
    private static void CheckFacingDirection(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== FACING DIRECTION CHECK ===\n");
        
        var bodyRenderer = renderers.FirstOrDefault(r => r.name == "Body");
        
        if (bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body flipX: {bodyRenderer.flipX}\n");
            File.AppendAllText(resultPath, "Expected: false (facing right by default)\n");
            
            if (!bodyRenderer.flipX)
            {
                File.AppendAllText(resultPath, "[PASS] Character facing right by default (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Character facing left by default (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Body renderer not found\n");
        }
    }
    
    private static void CheckFaceFeatures(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== FACE FEATURES CHECK ===\n");
        
        var faceRenderer = renderers.FirstOrDefault(r => r.name == "Face");
        
        if (faceRenderer != null)
        {
            File.AppendAllText(resultPath, "Face renderer found\n");
            File.AppendAllText(resultPath, $"Face sprite: {(faceRenderer.sprite != null ? faceRenderer.sprite.name : "NULL")}\n");
            File.AppendAllText(resultPath, $"Face enabled: {faceRenderer.enabled}\n");
            File.AppendAllText(resultPath, $"Face position: {faceRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Face local position: {faceRenderer.transform.localPosition}\n");
            File.AppendAllText(resultPath, $"Face sorting order: {faceRenderer.sortingOrder}\n");
            
            if (faceRenderer.sprite != null && faceRenderer.enabled)
            {
                File.AppendAllText(resultPath, "[PASS] Face has sprite and is enabled (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Face missing sprite or disabled (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Face renderer not found\n");
        }
    }
}