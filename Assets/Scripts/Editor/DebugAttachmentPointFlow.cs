using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Collections.Generic;
using System.Reflection;

public static class DebugAttachmentPointFlow
{
    [MenuItem("MapleUnity/Debug/Attachment Point Flow")]
    public static void RunDebug()
    {
        Debug.Log("=== DEBUG ATTACHMENT POINT FLOW ===");
        
        // Create test scene
        EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
        
        // Initialize with mock data
        var mockFile = new MockNxFile();
        NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
        
        // Create character
        var characterObj = new GameObject("TestCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        
        // Hook into the renderer to see attachment points
        FieldInfo attachmentField = typeof(MapleCharacterRenderer).GetField("currentAttachmentPoints", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Force initialization
        renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        
        // Check attachment points after each step
        EditorApplication.delayCall += () => {
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
            
            EditorApplication.delayCall += () => {
                Debug.Log("\n=== AFTER FORCED UPDATE ===");
                CheckActualPositions(characterObj);
            };
        };
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