using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Collections.Generic;

public static class DebugRuntimeAlignment
{
    [MenuItem("MapleUnity/Debug/Runtime Alignment Check")]
    public static void RunDebug()
    {
        Debug.Log("=== RUNTIME ALIGNMENT DEBUG ===");
        
        // Create a new scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Initialize NX with mock data
        var mockFile = new MockNxFile();
        NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
        
        // Create character
        var characterObj = new GameObject("DebugCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        
        // Force Start to initialize components
        renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        
        // Now set appearance after components are initialized
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Wait one frame then analyze
        EditorApplication.delayCall += () => {
            Debug.Log("\n=== CHARACTER TRANSFORM ANALYSIS ===");
            AnalyzeTransforms(characterObj.transform, "");
            
            // Find specific parts
            var body = FindChildRecursive(characterObj.transform, "Body");
            var head = FindChildRecursive(characterObj.transform, "Head");
            var face = FindChildRecursive(characterObj.transform, "Face");
            
            Debug.Log("\n=== KEY POSITIONS ===");
            if (body != null) Debug.Log($"Body position: {body.position} (local: {body.localPosition})");
            if (head != null) Debug.Log($"Head position: {head.position} (local: {head.localPosition})");
            if (face != null) Debug.Log($"Face position: {face.position} (local: {face.localPosition})");
            
            // Calculate actual offsets
            if (face != null)
            {
                Debug.Log("\n=== FACE OFFSET ANALYSIS ===");
                
                // Face offset from character origin
                Vector3 faceOffsetFromChar = face.position - characterObj.transform.position;
                Debug.Log($"Face offset from character: {faceOffsetFromChar}");
                Debug.Log($"In pixels: ({faceOffsetFromChar.x * 100:F1}, {faceOffsetFromChar.y * 100:F1})");
                
                // Face offset from head
                if (head != null)
                {
                    Vector3 faceOffsetFromHead = face.position - head.position;
                    Debug.Log($"Face offset from head: {faceOffsetFromHead}");
                    Debug.Log($"In pixels: ({faceOffsetFromHead.x * 100:F1}, {faceOffsetFromHead.y * 100:F1})");
                }
                
                // Expected vs actual
                Debug.Log($"\nExpected offset from C++: (-8, -52) pixels");
                Vector2 actualPixels = new Vector2(faceOffsetFromChar.x * 100, faceOffsetFromChar.y * 100);
                Vector2 expectedPixels = new Vector2(-8, -52);
                Debug.Log($"Difference: ({actualPixels.x - expectedPixels.x:F1}, {actualPixels.y - expectedPixels.y:F1}) pixels");
            }
            
            // Check attachment points being used
            Debug.Log("\n=== ATTACHMENT POINTS IN USE ===");
            CheckAttachmentPoints(renderer);
            
            // Visual alignment check
            Debug.Log("\n=== VISUAL ALIGNMENT CHECK ===");
            CheckVisualAlignment(characterObj);
        };
    }
    
    private static void AnalyzeTransforms(Transform t, string indent)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        string info = $"{indent}{t.name}: pos={t.localPosition}, scale={t.localScale}";
        if (sr != null && sr.sprite != null)
        {
            info += $", sprite={sr.sprite.name}, pivot={sr.sprite.pivot}, flipX={sr.flipX}";
        }
        Debug.Log(info);
        
        foreach (Transform child in t)
        {
            AnalyzeTransforms(child, indent + "  ");
        }
    }
    
    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        
        foreach (Transform child in parent)
        {
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    private static void CheckAttachmentPoints(MapleCharacterRenderer renderer)
    {
        // Use reflection to get current attachment points
        var field = renderer.GetType().GetField("currentAttachmentPoints", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var attachmentPoints = field.GetValue(renderer) as Dictionary<string, Vector2>;
            if (attachmentPoints != null)
            {
                Debug.Log($"Found {attachmentPoints.Count} attachment points:");
                foreach (var kvp in attachmentPoints)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
            }
        }
    }
    
    private static void CheckVisualAlignment(GameObject character)
    {
        var renderers = character.GetComponentsInChildren<SpriteRenderer>();
        
        // Check Y positions
        float? bodyY = null, headY = null, faceY = null;
        
        foreach (var r in renderers)
        {
            if (r.name == "Body") bodyY = r.transform.position.y;
            if (r.name == "Head") headY = r.transform.position.y;
            if (r.name == "Face") faceY = r.transform.position.y;
        }
        
        if (bodyY.HasValue && headY.HasValue)
        {
            float headAboveBody = headY.Value - bodyY.Value;
            Debug.Log($"Head is {headAboveBody:F3} units above body");
            Debug.Log(headAboveBody > 0 ? "✓ Head is above body" : "✗ Head is NOT above body!");
        }
        
        if (headY.HasValue && faceY.HasValue)
        {
            float faceRelativeToHead = faceY.Value - headY.Value;
            Debug.Log($"Face is {faceRelativeToHead:F3} units relative to head");
            Debug.Log(Mathf.Abs(faceRelativeToHead) < 0.1f ? "✓ Face aligned with head" : "✗ Face NOT aligned with head!");
        }
        
        // Check if any parts are at ground level
        foreach (var r in renderers)
        {
            if (r.transform.position.y < 0.1f && r.transform.position.y > -0.1f)
            {
                Debug.LogWarning($"WARNING: {r.name} is at ground level (Y={r.transform.position.y})");
            }
        }
    }
}