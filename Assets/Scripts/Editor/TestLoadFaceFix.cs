using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;

public class TestLoadFaceFix : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Test LoadFace Fix")]
    public static void RunTest()
    {
        Debug.Log("[TEST_LOADFACE_FIX] Starting test...");
        
        // Initialize NX data if not already loaded
        var singleton = NXDataManagerSingleton.Instance;
        if (singleton == null || singleton.DataManager == null)
        {
            Debug.LogError("NXDataManager not initialized!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        var loader = NXAssetLoader.Instance;
        
        // Test loading face ID 20000 with different expressions
        int[] faceIds = { 20000, 20001, 20002 };
        string[] expressions = { "default", "smile", "angry", "blink" };
        
        Debug.Log("\n=== Testing LoadFace with multiple face IDs and expressions ===");
        
        int successCount = 0;
        int totalTests = 0;
        
        foreach (var faceId in faceIds)
        {
            Debug.Log($"\nTesting face ID {faceId}:");
            
            foreach (var expression in expressions)
            {
                totalTests++;
                try
                {
                    var sprite = loader.LoadFace(faceId, expression);
                    if (sprite != null)
                    {
                        successCount++;
                        Debug.Log($"  ✓ {expression}: SUCCESS - {sprite.name} ({sprite.rect.width}x{sprite.rect.height})");
                    }
                    else
                    {
                        Debug.Log($"  ✗ {expression}: Failed (returned null)");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"  ✗ {expression}: Exception - {e.Message}");
                }
            }
        }
        
        Debug.Log($"\n=== Test Summary ===");
        Debug.Log($"Total tests: {totalTests}");
        Debug.Log($"Successful: {successCount}");
        Debug.Log($"Failed: {totalTests - successCount}");
        Debug.Log($"Success rate: {(float)successCount / totalTests * 100:F1}%");
        
        // Test the specific case that was failing
        Debug.Log("\n=== Testing specific case: LoadFace(20000, \"default\") ===");
        var testSprite = loader.LoadFace(20000, "default");
        if (testSprite != null)
        {
            Debug.Log("SUCCESS! Face ID 20000 with default expression now loads correctly!");
            Debug.Log($"Sprite details: {testSprite.name}, size: {testSprite.rect.width}x{testSprite.rect.height}");
        }
        else
        {
            Debug.LogError("FAILED! Face ID 20000 with default expression still returns null.");
        }
        
        Debug.Log("\n[TEST_LOADFACE_FIX] Test complete!");
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(testSprite != null ? 0 : 1);
        }
    }
}