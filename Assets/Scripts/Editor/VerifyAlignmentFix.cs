using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;

public static class VerifyAlignmentFix
{
    [MenuItem("MapleUnity/Test/Verify Alignment Fix")]
    public static void RunTest()
    {
        Debug.Log("=== VERIFYING ALIGNMENT FIX ===");
        
        // Create test scene
        EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
        
        // Initialize with mock data
        var mockFile = new MockNxFile();
        NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
        
        // Create character
        var characterObj = new GameObject("TestCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Force initialization
        renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        
        // Wait a frame for setup
        EditorApplication.delayCall += () => {
            Debug.Log("\n=== CHARACTER STRUCTURE ===");
            
            // Find all parts
            var body = FindChild(characterObj.transform, "Body");
            var head = FindChild(characterObj.transform, "Head");
            var face = FindChild(characterObj.transform, "Face");
            
            if (body != null)
            {
                Debug.Log($"Body local position: {body.localPosition}");
                Debug.Log($"Body world position: {body.position}");
            }
            
            if (head != null)
            {
                Debug.Log($"Head local position: {head.localPosition}");
                Debug.Log($"Head world position: {head.position}");
            }
            
            if (face != null)
            {
                Debug.Log($"Face local position: {face.localPosition}");
                Debug.Log($"Face world position: {face.position}");
                
                // Calculate face offset from character origin
                Vector3 faceOffset = face.position - characterObj.transform.position;
                Vector2 pixelOffset = new Vector2(faceOffset.x * 100, faceOffset.y * 100);
                
                Debug.Log($"\n=== FACE OFFSET VERIFICATION ===");
                Debug.Log($"Face offset from character: {faceOffset}");
                Debug.Log($"In pixels: ({pixelOffset.x:F1}, {pixelOffset.y:F1})");
                Debug.Log($"Expected: (-8, -52)");
                
                float distance = Vector2.Distance(pixelOffset, new Vector2(-8, -52));
                if (distance < 1f)
                {
                    Debug.Log("✓ SUCCESS: Face is correctly positioned!");
                }
                else
                {
                    Debug.LogError($"✗ FAILED: Face offset is incorrect. Distance: {distance:F1} pixels");
                }
            }
            
            // Visual check
            Debug.Log("\n=== VISUAL ALIGNMENT CHECK ===");
            if (body != null && head != null)
            {
                float headAboveBody = head.position.y - body.position.y;
                Debug.Log($"Head is {headAboveBody:F3} units above body");
                if (headAboveBody > 0)
                {
                    Debug.Log("✓ Head is correctly above body");
                }
                else
                {
                    Debug.LogError("✗ Head is NOT above body!");
                }
            }
            
            Debug.Log("\n=== TEST COMPLETE ===");
            Debug.Log("Check the Scene view to visually verify character alignment.");
        };
    }
    
    public static void RunBatchTest()
    {
        Debug.Log("=== BATCH MODE: VERIFYING ALIGNMENT FIX ===");
        
        try
        {
            // Call the test method
            RunTest();
            
            // Since the test uses delayCall, we need to manually trigger it
            if (EditorApplication.delayCall != null)
            {
                var delayedAction = EditorApplication.delayCall;
                EditorApplication.delayCall = null;
                delayedAction.Invoke();
            }
            
            // Exit with success
            Debug.Log("=== BATCH TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Batch test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
    
    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}