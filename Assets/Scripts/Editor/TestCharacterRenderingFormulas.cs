using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class TestCharacterRenderingFormulas
{
    [MenuItem("MapleUnity/Test Character Rendering Formulas")]
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Testing Character Rendering with C++ Formulas ===");
            
            // Load test scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            Debug.Log($"Loaded scene: {scene.name}");
            
            // Find the PlayerView object
            var playerView = GameObject.Find("PlayerView");
            if (playerView == null)
            {
                Debug.LogError("PlayerView not found in scene!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get the MapleCharacterRenderer
            var characterRenderer = playerView.GetComponentInChildren<MapleClient.GameView.MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                Debug.LogError("MapleCharacterRenderer not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("Found MapleCharacterRenderer, checking sprite positions...");
            
            // Get all sprite renderers to check their positions
            var renderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            Debug.Log($"\nSprite Renderer Positions (Total: {renderers.Length}):");
            
            foreach (var renderer in renderers)
            {
                if (renderer.sprite != null)
                {
                    var worldPos = renderer.transform.position;
                    var localPos = renderer.transform.localPosition;
                    var bounds = renderer.bounds;
                    
                    Debug.Log($"\n{renderer.name}:");
                    Debug.Log($"  - Sprite: {renderer.sprite.name}");
                    Debug.Log($"  - Local Position: {localPos}");
                    Debug.Log($"  - World Position: {worldPos}");
                    Debug.Log($"  - Bounds: center={bounds.center}, size={bounds.size}");
                    Debug.Log($"  - Pivot: {renderer.sprite.pivot}");
                    Debug.Log($"  - Sorting Order: {renderer.sortingOrder}");
                }
            }
            
            // Check specific body parts
            Debug.Log("\n=== Body Part Analysis ===");
            
            var body = characterRenderer.transform.Find("Body");
            var head = characterRenderer.transform.Find("Head");
            var arm = characterRenderer.transform.Find("Arm");
            var face = characterRenderer.transform.Find("Face");
            var hair = characterRenderer.transform.Find("Hair");
            
            if (body != null && body.GetComponent<SpriteRenderer>().sprite != null)
            {
                Debug.Log($"\nBody Analysis:");
                Debug.Log($"  - Position: {body.localPosition}");
                Debug.Log($"  - Should have navel at (0,0) after offset");
            }
            
            if (head != null && head.GetComponent<SpriteRenderer>().sprite != null)
            {
                Debug.Log($"\nHead Analysis:");
                Debug.Log($"  - Position: {head.localPosition}");
                Debug.Log($"  - Should align neck with body's neck");
            }
            
            if (arm != null && arm.GetComponent<SpriteRenderer>().sprite != null)
            {
                Debug.Log($"\nArm Analysis:");
                Debug.Log($"  - Position: {arm.localPosition}");
                Debug.Log($"  - Should align navel with body's navel");
            }
            
            if (face != null && face.GetComponent<SpriteRenderer>().sprite != null)
            {
                Debug.Log($"\nFace Analysis:");
                Debug.Log($"  - Position: {face.localPosition}");
                Debug.Log($"  - Should be at head position + brow offset");
            }
            
            // Take a screenshot
            string screenshotPath = "test-character-rendering.png";
            ScreenCapture.CaptureScreenshot(screenshotPath);
            Debug.Log($"\nScreenshot saved to: {screenshotPath}");
            
            Debug.Log("\n=== Test Complete ===");
            Debug.Log("Check the screenshot and logs to verify character rendering.");
            
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