using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using MapleClient.GameView;

public static class SimpleCharacterOffsetTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Simple Character Offset Test ===");
            
            // Load the henesys scene
            string scenePath = "Assets/henesys.unity";
            Debug.Log($"Loading scene: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath);
            
            // Check for GameManager
            var gameManager = GameObject.FindAnyObjectByType<GameManager>();
            Debug.Log($"GameManager found: {gameManager != null}");
            
            // Check current implementation being used
            var simpleControllers = GameObject.FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);
            var mapleRenderers = GameObject.FindObjectsByType<MapleCharacterRenderer>(FindObjectsSortMode.None);
            
            Debug.Log($"\n=== CHARACTER RENDERING SUMMARY ===");
            Debug.Log($"SimplePlayerController count: {simpleControllers.Length}");
            Debug.Log($"MapleCharacterRenderer count: {mapleRenderers.Length}");
            
            if (simpleControllers.Length > 0)
            {
                Debug.LogWarning("✗ Project is using SimplePlayerController instead of MapleCharacterRenderer!");
                Debug.LogWarning("  This means the proper MapleStory character rendering with face offsets is NOT active.");
            }
            
            if (mapleRenderers.Length > 0)
            {
                Debug.Log("✓ MapleCharacterRenderer found! Checking face offset...");
                
                foreach (var renderer in mapleRenderers)
                {
                    // Use reflection to check face offset
                    var rendererType = renderer.GetType();
                    var faceSpriteField = rendererType.GetField("faceSprite", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (faceSpriteField != null)
                    {
                        var faceSprite = faceSpriteField.GetValue(renderer) as SpriteRenderer;
                        if (faceSprite != null)
                        {
                            var offset = faceSprite.transform.localPosition;
                            bool isCorrect = Mathf.Approximately(offset.x, -0.08f) && 
                                           Mathf.Approximately(offset.y, -0.52f);
                            
                            if (isCorrect)
                            {
                                Debug.Log($"✓ Face offset CORRECT: {offset} (expected: -0.08, -0.52)");
                            }
                            else
                            {
                                Debug.LogError($"✗ Face offset INCORRECT: {offset} (expected: -0.08, -0.52)");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No MapleCharacterRenderer components found.");
                Debug.LogWarning("The project needs to use MapleCharacterRenderer for proper face rendering.");
            }
            
            // Check if there's any evidence of face sprites being loaded
            var allSprites = GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int faceCount = 0;
            foreach (var sr in allSprites)
            {
                if (sr.name.Contains("face") || sr.name.Contains("Face") || sr.name.Contains("20000"))
                {
                    faceCount++;
                    Debug.Log($"Found face-related sprite: {sr.name} at {sr.transform.position}");
                }
            }
            
            Debug.Log($"\nTotal face-related sprites found: {faceCount}");
            
            Debug.Log("\n=== Test Completed ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
}