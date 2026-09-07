using UnityEngine;
using UnityEditor;
using MapleClient.GameView;

public static class VerifyFaceOffset
{
    [MenuItem("MapleUnity/Test/Verify Face Offset")]
    public static void RunTest()
    {
        Debug.Log("=== Verifying Face Offset Test ===");
        
        try
        {
            // Create a temporary GameObject with MapleCharacterRenderer
            var testObj = new GameObject("TestCharacter");
            var renderer = testObj.AddComponent<MapleCharacterRenderer>();
            
            // Set character appearance
            renderer.SetCharacterAppearance(0, 20000, 30000);
            
            // Wait a frame for initialization
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Find the face transform
                    var faceTransform = testObj.transform.Find("Face");
                    if (faceTransform != null)
                    {
                        var localPos = faceTransform.localPosition;
                        var pixelOffset = new Vector2(localPos.x * 100, -localPos.y * 100); // Convert to pixels and flip Y
                        
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
                        }
                    }
                    else
                    {
                        Debug.LogError("Face transform not found!");
                    }
                    
                    // Clean up
                    Object.DestroyImmediate(testObj);
                    Debug.Log("Test completed and cleaned up.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error during face offset verification: {e.Message}");
                    if (testObj != null) Object.DestroyImmediate(testObj);
                }
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set up face offset test: {e.Message}");
        }
    }
}