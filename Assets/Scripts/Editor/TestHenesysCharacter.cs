using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;

public static class TestHenesysCharacter
{
    private static string resultPath = @"C:\Users\me\MapleUnity\character-test-results.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(resultPath, "[TEST] Starting character rendering verification in Henesys scene\n");
            File.AppendAllText(resultPath, $"[TEST] Time: {DateTime.Now}\n\n");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(resultPath, $"[TEST] Opened scene: {scene.name}\n");
            File.AppendAllText(resultPath, $"[TEST] Scene path: {scene.path}\n");
            File.AppendAllText(resultPath, $"[TEST] Root objects count: {scene.rootCount}\n\n");
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[INFO] Player not found in scene, looking for GameManager to spawn player\n");
                
                // Try to find GameManager and trigger player spawn
                var gameManager = GameObject.Find("GameManager");
                if (gameManager != null)
                {
                    File.AppendAllText(resultPath, "[TEST] Found GameManager\n");
                    
                    // Try to force Start/Awake
                    var gmComponent = gameManager.GetComponent<MonoBehaviour>();
                    if (gmComponent != null)
                    {
                        var startMethod = gmComponent.GetType().GetMethod("Start", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (startMethod != null)
                        {
                            startMethod.Invoke(gmComponent, null);
                            File.AppendAllText(resultPath, "[TEST] Called GameManager.Start()\n");
                        }
                    }
                    
                    // Try to find player again
                    playerGO = GameObject.Find("Player");
                }
            }
            
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Player still not found after GameManager initialization\n");
                
                // List all root objects
                File.AppendAllText(resultPath, "\n[DEBUG] All root objects in scene:\n");
                var rootObjects = scene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    File.AppendAllText(resultPath, $"  - {obj.name}\n");
                    
                    // Check children for Player
                    var player = obj.GetComponentInChildren<Transform>().Find("Player");
                    if (player != null)
                    {
                        File.AppendAllText(resultPath, $"    Found Player as child of {obj.name}\n");
                        playerGO = player.gameObject;
                        break;
                    }
                }
                
                if (playerGO == null)
                {
                    EditorApplication.Exit(1);
                    return;
                }
            }
            
            File.AppendAllText(resultPath, $"\n[TEST] Player found at: {playerGO.transform.position}\n");
            
            // Force initialization of PlayerView
            MonoBehaviour playerView = null;
            var components = playerGO.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name == "PlayerView")
                {
                    playerView = comp;
                    break;
                }
            }
            
            if (playerView != null)
            {
                File.AppendAllText(resultPath, "[TEST] Found PlayerView component\n");
                
                // Try to call Awake
                var awakeMethod = playerView.GetType().GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(playerView, null);
                    File.AppendAllText(resultPath, "[TEST] Called PlayerView.Awake()\n");
                }
                
                // Try to call Start
                var startMethod = playerView.GetType().GetMethod("Start", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(playerView, null);
                    File.AppendAllText(resultPath, "[TEST] Called PlayerView.Start()\n");
                }
            }
            
            // Get character renderer
            var renderers = playerGO.GetComponentsInChildren<SpriteRenderer>();
            File.AppendAllText(resultPath, $"[TEST] Found {renderers.Length} SpriteRenderers in Player\n");
            
            foreach (var renderer in renderers)
            {
                File.AppendAllText(resultPath, $"  - {renderer.name} (enabled: {renderer.enabled}, sprite: {(renderer.sprite != null ? renderer.sprite.name : "null")})\n");
            }
            
            // Look for MapleCharacterRenderer component
            var characterRenderers = playerGO.GetComponentsInChildren<MonoBehaviour>();
            MonoBehaviour mapleCharRenderer = null;
            
            foreach (var comp in characterRenderers)
            {
                if (comp.GetType().Name == "MapleCharacterRenderer")
                {
                    mapleCharRenderer = comp;
                    File.AppendAllText(resultPath, $"\n[TEST] Found MapleCharacterRenderer component\n");
                    break;
                }
            }
            
            if (mapleCharRenderer == null)
            {
                File.AppendAllText(resultPath, "[ERROR] MapleCharacterRenderer component not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            // Run checks
            CheckHeadPosition(renderers);
            CheckFacingDirection(renderers);
            CheckFaceFeatures(renderers);
            
            File.AppendAllText(resultPath, "\n[TEST] === SUMMARY ===\n");
            File.AppendAllText(resultPath, "[TEST] All checks completed. See results above.\n");
            File.AppendAllText(resultPath, $"[TEST] Test finished at: {DateTime.Now}\n");
            
            Debug.Log($"Test results written to: {resultPath}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"\n[ERROR] Exception: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
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
            
            // Additional face details
            if (faceRenderer.sprite != null)
            {
                File.AppendAllText(resultPath, $"Face sprite details: {faceRenderer.sprite.name}, size: {faceRenderer.sprite.rect.size}\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Face renderer not found\n");
        }
    }
}