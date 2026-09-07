using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Reflection;
using MapleClient.GameView;  // Add namespace for MapleCharacterRenderer
using MapleClient.GameView.UI;  // For PlayerView

public static class VerifyCharacterRenderingTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Starting Character Rendering Verification Test ===");
            
            // Load the henesys scene which contains character rendering
            string scenePath = "Assets/henesys.unity";
            Debug.Log($"Loading scene: {scenePath}");
            
            var scene = EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"Scene loaded: {scene.name}, isLoaded: {scene.isLoaded}");
            
            // Find GameManager and trigger initialization
            var gameManager = GameObject.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Debug.Log("Found GameManager, triggering initialization...");
                
                // Use reflection to call private Start method
                var gmType = gameManager.GetType();
                var startMethod = gmType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(gameManager, null);
                    Debug.Log("GameManager.Start() called");
                }
                
                // Wait a frame for initialization
                System.Threading.Thread.Sleep(100);
            }
            
            // Find all MapleCharacterRenderer components
            var characterRenderers = GameObject.FindObjectsByType<MapleCharacterRenderer>(FindObjectsSortMode.None);
            Debug.Log($"Found {characterRenderers.Length} MapleCharacterRenderer components");
            
            // Also check for PlayerView which might contain MapleCharacterRenderer
            var playerViews = GameObject.FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
            Debug.Log($"Found {playerViews.Length} PlayerView components");
            
            // Check SimplePlayerController too
            var simpleControllers = GameObject.FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);
            Debug.Log($"Found {simpleControllers.Length} SimplePlayerController components");
            
            if (characterRenderers.Length == 0)
            {
                Debug.LogWarning("No MapleCharacterRenderer components found in scene!");
                
                // Let's check if there are any sprite renderers that might be character faces
                var allSpriteRenderers = GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                Debug.Log($"Found {allSpriteRenderers.Length} total SpriteRenderer components");
                
                foreach (var sr in allSpriteRenderers)
                {
                    if (sr.name.Contains("face") || sr.name.Contains("Face") || sr.name.Contains("20000"))
                    {
                        Debug.Log($"Found potential face sprite: {sr.name} at position {sr.transform.position}, local: {sr.transform.localPosition}");
                    }
                }
            }
            
            // Check each character renderer
            foreach (var renderer in characterRenderers)
            {
                Debug.Log($"\n--- Checking character: {renderer.gameObject.name} ---");
                Debug.Log($"Position: {renderer.transform.position}");
                
                // Use reflection to check private fields
                var rendererType = renderer.GetType();
                
                // Check for face sprite renderer
                var faceSpriteField = rendererType.GetField("faceSprite", BindingFlags.NonPublic | BindingFlags.Instance);
                if (faceSpriteField != null)
                {
                    var faceSprite = faceSpriteField.GetValue(renderer) as SpriteRenderer;
                    if (faceSprite != null)
                    {
                        Debug.Log($"Face sprite found: {faceSprite.name}");
                        Debug.Log($"Face local position: {faceSprite.transform.localPosition}");
                        Debug.Log($"Face world position: {faceSprite.transform.position}");
                        
                        // Check if face offset is correct (-0.08, -0.52)
                        var expectedOffset = new Vector3(-0.08f, -0.52f, 0f);
                        var actualOffset = faceSprite.transform.localPosition;
                        
                        bool offsetCorrect = Mathf.Approximately(actualOffset.x, expectedOffset.x) && 
                                           Mathf.Approximately(actualOffset.y, expectedOffset.y);
                        
                        if (offsetCorrect)
                        {
                            Debug.Log("✓ Face offset is CORRECT: -0.08, -0.52 Unity units (-8, -52 pixels)");
                        }
                        else
                        {
                            Debug.LogError($"✗ Face offset is INCORRECT! Expected: {expectedOffset}, Actual: {actualOffset}");
                            Debug.LogError($"  In pixels: Expected: (-8, -52), Actual: ({actualOffset.x * 100}, {actualOffset.y * 100})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Face sprite renderer is null");
                    }
                }
                
                // Check character parts dictionary
                var characterPartsField = rendererType.GetField("characterParts", BindingFlags.NonPublic | BindingFlags.Instance);
                if (characterPartsField != null)
                {
                    var parts = characterPartsField.GetValue(renderer) as System.Collections.Generic.Dictionary<string, SpriteRenderer>;
                    if (parts != null)
                    {
                        Debug.Log($"Character has {parts.Count} parts:");
                        foreach (var kvp in parts)
                        {
                            Debug.Log($"  - {kvp.Key}: {kvp.Value?.name ?? "null"}, position: {kvp.Value?.transform.localPosition}");
                        }
                    }
                }
                
                // Try to manually trigger initialization if needed
                var initMethod = rendererType.GetMethod("InitializeRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (initMethod != null)
                {
                    Debug.Log("Manually initializing renderer...");
                    initMethod.Invoke(renderer, null);
                    
                    // Re-check face after initialization
                    var faceSpriteAfterInit = faceSpriteField?.GetValue(renderer) as SpriteRenderer;
                    if (faceSpriteAfterInit != null)
                    {
                        Debug.Log($"After init - Face position: {faceSpriteAfterInit.transform.localPosition}");
                    }
                }
            }
            
            // Check for any compilation errors
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("Script compilation failed!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("\n=== Character Rendering Verification Test Completed ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed with exception: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}