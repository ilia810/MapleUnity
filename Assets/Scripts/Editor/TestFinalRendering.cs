using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;

public static class TestFinalRendering
{
    [MenuItem("MapleUnity/Test/Test Final Character Rendering")]
    public static void RunTest()
    {
        Debug.Log("=== FINAL CHARACTER RENDERING TEST ===");
        
        // Try to load henesys scene if it exists
        try {
            EditorSceneManager.OpenScene("Assets/henesys.unity");
            Debug.Log("Loaded henesys.unity scene");
        } catch {
            Debug.Log("Could not load scene, testing in current scene");
        }
        
        // Find existing MapleCharacter
        var mapleCharacter = GameObject.Find("MapleCharacter");
        if (mapleCharacter != null)
        {
            Debug.Log("Found MapleCharacter in scene");
            LogCharacterStructure(mapleCharacter);
        }
        else
        {
            Debug.Log("MapleCharacter not found in scene, creating one for testing...");
            
            // Create a test character
            mapleCharacter = new GameObject("MapleCharacter");
            var renderer = mapleCharacter.AddComponent<MapleCharacterRenderer>();
            
            // Let it initialize
            renderer.SetCharacterAppearance(0, 20000, 30000);
            
            // Log structure after initialization
            Debug.Log("\nCreated MapleCharacter:");
            LogCharacterStructure(mapleCharacter);
        }
        
        Debug.Log("\n=== TEST COMPLETE ===");
        
        // Exit Unity in batch mode
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
    
    private static void LogCharacterStructure(GameObject character)
    {
        Debug.Log($"\nCharacter at: {character.transform.position}");
        Debug.Log($"Scale: {character.transform.localScale}");
        
        // Find Face
        var face = character.transform.Find("Face");
        if (face != null)
        {
            Debug.Log($"\nFace found:");
            Debug.Log($"  Local Position: {face.localPosition}");
            Debug.Log($"  Pixel Offset: ({face.localPosition.x * 100:F0}, {face.localPosition.y * 100:F0})");
            
            // Check if it matches expected C++ offset
            Vector2 expectedPixels = new Vector2(-8, -52);
            Vector2 actualPixels = new Vector2(face.localPosition.x * 100, face.localPosition.y * 100);
            float distance = Vector2.Distance(expectedPixels, actualPixels);
            
            if (distance < 1f)
            {
                Debug.Log($"  ✓ Face offset MATCHES C++ runtime: {actualPixels} ≈ {expectedPixels}");
            }
            else
            {
                Debug.LogWarning($"  ✗ Face offset MISMATCH: Expected {expectedPixels}, Got {actualPixels}");
            }
        }
        else
        {
            // Check if face is under head
            var head = character.transform.Find("Head");
            if (head != null)
            {
                face = head.Find("Face");
                if (face != null)
                {
                    Debug.Log($"\nFace found under Head:");
                    Debug.Log($"  Face relative to Head: {face.localPosition}");
                    
                    // Calculate total offset
                    Vector3 totalOffset = head.localPosition + face.localPosition;
                    Debug.Log($"  Total offset from character: {totalOffset}");
                    Debug.Log($"  Total pixel offset: ({totalOffset.x * 100:F0}, {totalOffset.y * 100:F0})");
                }
            }
        }
        
        // Log full hierarchy
        Debug.Log("\nFull Hierarchy:");
        LogHierarchy(character.transform, 0);
    }
    
    private static void LogHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}{t.name}: {t.localPosition}");
        
        foreach (Transform child in t)
        {
            LogHierarchy(child, depth + 1);
        }
    }
}