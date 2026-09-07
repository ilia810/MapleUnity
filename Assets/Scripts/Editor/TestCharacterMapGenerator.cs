using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.SceneGeneration;

public static class TestCharacterMapGenerator
{
    [MenuItem("MapleUnity/Test/Test Character with Map Generator")]
    public static void TestCharacterWithMap()
    {
        Debug.Log("=== Testing Character with Map Generator ===");
        
        // Generate a test map scene
        var generator = new MapSceneGenerator();
        generator.GenerateMapScene(100000000); // Henesys
        
        // Create a character in the scene
        var characterObj = new GameObject("MapleCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        
        // Set character appearance (skin=0, face=20000, hair=30000)
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Wait a frame for initialization
        EditorApplication.delayCall += () =>
        {
            try
            {
                // Find the face transform
                var faceTransform = characterObj.transform.Find("Face");
                if (faceTransform != null)
                {
                    var localPos = faceTransform.localPosition;
                    var pixelOffset = new Vector2(localPos.x * 100, -localPos.y * 100); // Convert to pixels and flip Y
                    
                    Debug.Log($"=== Face Offset Test Results ===");
                    Debug.Log($"Face Transform Local Position: {localPos}");
                    Debug.Log($"Face Pixel Offset: ({pixelOffset.x}, {pixelOffset.y})");
                    Debug.Log($"Expected C++ Runtime Offset: (-8, -52)");
                    
                    bool xMatches = Mathf.Approximately(pixelOffset.x, -8f);
                    bool yMatches = Mathf.Approximately(pixelOffset.y, -52f);
                    
                    if (xMatches && yMatches)
                    {
                        Debug.Log("✓ SUCCESS: Face offset matches expected C++ runtime value!");
                    }
                    else
                    {
                        Debug.LogError($"✗ FAILURE: Face offset does not match!");
                        Debug.LogError($"  Expected: (-8, -52)");
                        Debug.LogError($"  Actual: ({pixelOffset.x}, {pixelOffset.y})");
                        Debug.LogError($"  Difference: ({pixelOffset.x - (-8)}, {pixelOffset.y - (-52)})");
                        
                        // Debug attachment points
                        var bodyNeck = new Vector2(-7, -31);  // From mock data
                        var headNeck = new Vector2(14, 19);   // From mock data
                        var headBrow = new Vector2(13, -2);   // From mock data
                        
                        Debug.Log($"\nAttachment Points (from mock data):");
                        Debug.Log($"  Body neck: {bodyNeck}");
                        Debug.Log($"  Head neck: {headNeck}");
                        Debug.Log($"  Head brow: {headBrow}");
                        
                        // Calculate expected position using C++ formula
                        var expectedOffset = bodyNeck - headNeck + headBrow;
                        Debug.Log($"  Formula result: {expectedOffset} (should be -8, -52)");
                    }
                }
                else
                {
                    Debug.LogError("Face transform not found!");
                }
                
                Debug.Log("\nTest completed. Character remains in scene for inspection.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during face offset test: {e.Message}\n{e.StackTrace}");
            }
        };
    }
    
    public static void RunBatchTest()
    {
        Debug.Log("=== Running Batch Test for Face Offset ===");
        TestCharacterWithMap();
        
        // Exit after a delay to allow the test to complete
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.Exit(0);
            };
        };
    }
}