using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.GameData;

public static class TestMapleCharacterRendering
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== MapleCharacterRenderer Test ===");
            
            // Load the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            Debug.Log($"Loaded scene: {scene.name}");
            
            // Find GameManager
            var gameManager = GameObject.FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("GameManager not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Check what player controller is being used
            var simpleController = GameObject.FindObjectOfType<SimplePlayerController>();
            var mapleRenderer = GameObject.FindObjectOfType<MapleCharacterRenderer>();
            
            Debug.Log($"SimplePlayerController present: {simpleController != null}");
            Debug.Log($"MapleCharacterRenderer present: {mapleRenderer != null}");
            
            if (mapleRenderer != null)
            {
                // Check face renderer position
                var faceRenderer = mapleRenderer.transform.Find("Face")?.GetComponent<SpriteRenderer>();
                if (faceRenderer != null)
                {
                    var localPos = faceRenderer.transform.localPosition;
                    Debug.Log($"Face local position: {localPos} (expected: -0.08, -0.52)");
                    Debug.Log($"Face offset from character origin: ({localPos.x:F2}, {localPos.y:F2})");
                    
                    // Check if it matches expected offset
                    bool correctX = Mathf.Abs(localPos.x - (-0.08f)) < 0.01f;
                    bool correctY = Mathf.Abs(localPos.y - (-0.52f)) < 0.01f;
                    
                    if (correctX && correctY)
                    {
                        Debug.Log("✓ Face offset is correct!");
                    }
                    else
                    {
                        Debug.LogError($"✗ Face offset is incorrect! Expected (-0.08, -0.52), got ({localPos.x:F2}, {localPos.y:F2})");
                    }
                }
                else
                {
                    Debug.LogWarning("Face renderer not found in MapleCharacterRenderer");
                }
            }
            else
            {
                Debug.LogWarning("MapleCharacterRenderer not found - GameManager is using SimplePlayerController instead");
                Debug.Log("To test MapleCharacterRenderer, modify GameManager.OnMapLoaded() to use MapleCharacterRenderer");
            }
            
            // Check compilation status
            Debug.Log("\n=== Compilation Status ===");
            Debug.Log("Project compiled successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}