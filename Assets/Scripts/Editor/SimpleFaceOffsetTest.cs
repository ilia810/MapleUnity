using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using MapleClient.GameView;
using MapleClient.GameData;

public static class SimpleFaceOffsetTest
{
    [MenuItem("MapleUnity/Test/Simple Face Offset Test")]
    public static void RunTest()
    {
        Debug.Log("=== Simple Face Offset Test ===");
        
        // Based on the mock data attachment points:
        // Body neck: (-7, -31)
        // Head neck: (14, 19)
        // Head brow: (13, -2)
        
        Vector2 bodyNeck = new Vector2(-7, -31);
        Vector2 headNeck = new Vector2(14, 19);
        Vector2 headBrow = new Vector2(13, -2);
        
        // C++ formula: facePos = body.neck - head.neck + head.brow
        Vector2 faceOffset = bodyNeck - headNeck + headBrow;
        
        Debug.Log($"Body neck: {bodyNeck}");
        Debug.Log($"Head neck: {headNeck}");
        Debug.Log($"Head brow: {headBrow}");
        Debug.Log($"Calculated face offset (pixels): ({faceOffset.x}, {faceOffset.y})");
        Debug.Log($"Expected C++ offset: (-8, -52)");
        
        bool xMatches = Mathf.Approximately(faceOffset.x, -8f);
        bool yMatches = Mathf.Approximately(faceOffset.y, -52f);
        
        if (xMatches && yMatches)
        {
            Debug.Log("✓ Formula produces correct offset!");
        }
        else
        {
            Debug.LogError("✗ Formula produces incorrect offset!");
        }
        
        // Now check what the Unity implementation would produce
        // Unity formula in UpdateHeadPosition:
        // headPosition = (bodyNeck - headNeck) / 100f with Y flipped
        // facePosition = headPosition + headBrow / 100f with Y flipped
        
        Vector3 headPosition = new Vector3(
            (bodyNeck.x - headNeck.x) / 100f,
            -(bodyNeck.y - headNeck.y) / 100f,
            0
        );
        
        Vector3 facePosition = headPosition + new Vector3(
            headBrow.x / 100f,
            -headBrow.y / 100f,
            0
        );
        
        Vector2 unityPixelOffset = new Vector2(
            facePosition.x * 100f,
            -facePosition.y * 100f  // Flip back to get pixel offset
        );
        
        Debug.Log($"\nUnity implementation:");
        Debug.Log($"Head position (Unity units): {headPosition}");
        Debug.Log($"Face position (Unity units): {facePosition}");
        Debug.Log($"Face offset in pixels: ({unityPixelOffset.x}, {unityPixelOffset.y})");
        
        bool unityXMatches = Mathf.Approximately(unityPixelOffset.x, -8f);
        bool unityYMatches = Mathf.Approximately(unityPixelOffset.y, -52f);
        
        if (unityXMatches && unityYMatches)
        {
            Debug.Log("✓ Unity implementation produces correct offset!");
        }
        else
        {
            Debug.LogError("✗ Unity implementation produces incorrect offset!");
            Debug.LogError($"Difference: ({unityPixelOffset.x - (-8)}, {unityPixelOffset.y - (-52)})");
        }
        
        // Exit Unity
        EditorApplication.Exit(0);
    }
    
    public static void RunAttachmentFlowDebug()
    {
        try
        {
            Debug.Log("=== ATTACHMENT FLOW DEBUG ===");
            
            // Create test scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Initialize with mock data
            var mockFile = new MockNxFile();
            NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
            
            // Create character
            var characterObj = new GameObject("TestCharacter");
            var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
            
            // Create the sprite renderers manually since we're not in play mode
            var createMethod = typeof(MapleCharacterRenderer).GetMethod("CreateSpriteRenderers", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (createMethod != null)
            {
                createMethod.Invoke(renderer, null);
                Debug.Log("Called CreateSpriteRenderers");
            }
            
            // Hook into the renderer to see attachment points
            FieldInfo attachmentField = typeof(MapleCharacterRenderer).GetField("currentAttachmentPoints", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Now set the appearance
            renderer.SetCharacterAppearance(0, 20000, 30000);
            
            // Wait a bit
            System.Threading.Thread.Sleep(1000);
            
            Debug.Log("\n=== ATTACHMENT POINTS AFTER INITIALIZATION ===");
            
            if (attachmentField != null)
            {
                var attachmentPoints = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
                if (attachmentPoints != null)
                {
                    Debug.Log($"Total attachment points: {attachmentPoints.Count}");
                    foreach (var kvp in attachmentPoints)
                    {
                        Debug.Log($"  {kvp.Key}: {kvp.Value}");
                    }
                    
                    // Check specific points we need
                    CheckRequiredPoints(attachmentPoints);
                }
                else
                {
                    Debug.LogError("Attachment points dictionary is null!");
                }
            }
            
            // Check actual positions
            CheckActualPositions(characterObj);
            
            // Force an update to trigger ApplyAttachmentOffsets
            Debug.Log("\n=== FORCING UPDATE ===");
            renderer.SendMessage("UpdateSprites", SendMessageOptions.DontRequireReceiver);
            
            System.Threading.Thread.Sleep(500);
            
            Debug.Log("\n=== AFTER FORCED UPDATE ===");
            CheckActualPositions(characterObj);
            
            Debug.Log("\n=== TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckRequiredPoints(Dictionary<string, Vector2> points)
    {
        Debug.Log("\n--- Required Points Check ---");
        
        string[] required = {
            "body.map.neck", "body.neck",
            "body.map.navel", "body.navel",
            "head.map.neck", "head.neck",
            "head.map.brow", "head.brow"
        };
        
        foreach (var key in required)
        {
            if (points.ContainsKey(key))
            {
                Debug.Log($"✓ Found {key}: {points[key]}");
            }
            else
            {
                Debug.LogWarning($"✗ Missing {key}");
            }
        }
        
        // Calculate expected face offset
        Vector2 bodyNeck = Vector2.zero;
        Vector2 headNeck = Vector2.zero;
        Vector2 headBrow = Vector2.zero;
        
        if (points.TryGetValue("body.map.neck", out bodyNeck) || points.TryGetValue("body.neck", out bodyNeck))
        {
            Debug.Log($"Using body neck: {bodyNeck}");
        }
        
        if (points.TryGetValue("head.map.neck", out headNeck) || points.TryGetValue("head.neck", out headNeck))
        {
            Debug.Log($"Using head neck: {headNeck}");
        }
        
        if (points.TryGetValue("head.map.brow", out headBrow) || points.TryGetValue("head.brow", out headBrow))
        {
            Debug.Log($"Using head brow: {headBrow}");
        }
        
        Vector2 expectedFaceOffset = bodyNeck - headNeck + headBrow;
        Debug.Log($"\nExpected face offset calculation:");
        Debug.Log($"body.neck({bodyNeck}) - head.neck({headNeck}) + head.brow({headBrow}) = {expectedFaceOffset}");
    }
    
    private static void CheckActualPositions(GameObject character)
    {
        Debug.Log("\n--- Actual Positions ---");
        
        var body = FindChild(character.transform, "Body");
        var head = FindChild(character.transform, "Head");
        var face = FindChild(character.transform, "Face");
        
        if (body != null) Debug.Log($"Body: {body.localPosition}");
        if (head != null) Debug.Log($"Head: {head.localPosition}");
        if (face != null) 
        {
            Debug.Log($"Face: {face.localPosition}");
            
            if (head != null)
            {
                float distance = Vector3.Distance(head.position, face.position);
                Debug.Log($"Head-Face distance: {distance:F4}");
                
                if (distance < 0.001f)
                {
                    Debug.LogError("ERROR: Face and Head are at the same position!");
                }
                else
                {
                    Debug.Log("SUCCESS: Face and Head are at different positions!");
                }
            }
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