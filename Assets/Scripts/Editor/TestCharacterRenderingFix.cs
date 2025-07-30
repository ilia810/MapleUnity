using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameView;

public static class TestCharacterRenderingFix
{
    [MenuItem("MapleUnity/Test Character Rendering Fix")]
    public static void TestRendering()
    {
        Debug.Log("=== TESTING CHARACTER RENDERING FIX ===");
        
        try
        {
            // Create test character
            GameObject charGO = new GameObject("TestCharacter");
            charGO.transform.position = Vector3.zero;
            
            // Add renderer component
            var renderer = charGO.AddComponent<MapleCharacterRenderer>();
            Debug.Log("Created MapleCharacterRenderer");
            
            // Wait for next frame to let it initialize
            EditorApplication.delayCall += () => {
                try
                {
                    // Check sprite renderers
                    var sprites = charGO.GetComponentsInChildren<SpriteRenderer>();
                    Debug.Log($"\nFound {sprites.Length} sprite renderers:");
                    
                    foreach (var sr in sprites)
                    {
                        Debug.Log($"\n[{sr.gameObject.name}]");
                        Debug.Log($"  Position: {sr.transform.localPosition}");
                        Debug.Log($"  Has Sprite: {sr.sprite != null}");
                        
                        if (sr.sprite != null)
                        {
                            Debug.Log($"  Pivot: {sr.sprite.pivot}");
                            Debug.Log($"  Pivot Normalized: ({sr.sprite.pivot.x / sr.sprite.rect.width:F3}, {sr.sprite.pivot.y / sr.sprite.rect.height:F3})");
                        }
                    }
                    
                    // Check positioning
                    Debug.Log("\n=== POSITION ANALYSIS ===");
                    var body = charGO.transform.Find("body");
                    var arm = charGO.transform.Find("arm");
                    var head = charGO.transform.Find("head");
                    
                    if (body != null) Debug.Log($"Body Y: {body.localPosition.y:F3}");
                    if (arm != null) Debug.Log($"Arm Y: {arm.localPosition.y:F3}");
                    if (head != null) Debug.Log($"Head Y: {head.localPosition.y:F3}");
                    
                    // Check attachment points
                    Debug.Log("\n=== ATTACHMENT POINTS ===");
                    var attachmentField = renderer.GetType().GetField("currentAttachmentPoints", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (attachmentField != null)
                    {
                        var attachments = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
                        if (attachments != null)
                        {
                            Debug.Log($"Found {attachments.Count} attachment points:");
                            foreach (var kvp in attachments)
                            {
                                Debug.Log($"  {kvp.Key}: {kvp.Value}");
                            }
                        }
                    }
                    
                    Debug.Log("\n=== TEST COMPLETE ===");
                    
                    if (Application.isBatchMode)
                    {
                        EditorApplication.Exit(0);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Test error: {e.Message}\n{e.StackTrace}");
                    if (Application.isBatchMode)
                    {
                        EditorApplication.Exit(1);
                    }
                }
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Setup error: {e.Message}\n{e.StackTrace}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
    
    public static void RunBatchTest()
    {
        TestRendering();
    }
}