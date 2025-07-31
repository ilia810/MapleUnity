using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Collections.Generic;

public static class CharacterRenderingBatchTest
{
    public static void RunTest()
    {
        Debug.Log("=== CHARACTER RENDERING BATCH TEST ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Batch Mode: {Application.isBatchMode}");
        
        try
        {
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create test character
            GameObject charGO = new GameObject("TestCharacter");
            charGO.transform.position = Vector3.zero;
            
            // Try to add MapleCharacterRenderer using reflection to avoid compile-time dependency
            System.Type rendererType = null;
            
            // Search through all assemblies for the MapleCharacterRenderer type
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType("MapleClient.GameView.MapleCharacterRenderer");
                    if (type != null)
                    {
                        rendererType = type;
                        Debug.Log($"Found MapleCharacterRenderer in assembly: {assembly.FullName}");
                        break;
                    }
                }
                catch (System.Exception) { }
            }
            
            if (rendererType == null)
            {
                // Try without namespace
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var type = assembly.GetType("MapleCharacterRenderer");
                        if (type != null)
                        {
                            rendererType = type;
                            Debug.Log($"Found MapleCharacterRenderer (no namespace) in assembly: {assembly.FullName}");
                            break;
                        }
                    }
                    catch (System.Exception) { }
                }
            }
            
            if (rendererType != null)
            {
                var renderer = charGO.AddComponent(rendererType);
                Debug.Log("Successfully added MapleCharacterRenderer component");
                
                // Call Start method via reflection
                var startMethod = rendererType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                if (startMethod != null)
                {
                    startMethod.Invoke(renderer, null);
                    Debug.Log("Called Start() method");
                }
                
                // Analyze sprite renderers after a delay
                EditorApplication.delayCall += () => AnalyzeCharacter(charGO, renderer);
            }
            else
            {
                Debug.LogError("Could not find MapleCharacterRenderer type!");
                
                // List all available types for debugging
                Debug.Log("\n=== Available Types in Assemblies ===");
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Contains("MapleUnity") || assembly.FullName.Contains("Game"))
                    {
                        Debug.Log($"\nAssembly: {assembly.FullName}");
                        try
                        {
                            var types = assembly.GetTypes();
                            foreach (var type in types)
                            {
                                if (type.Name.Contains("Character") || type.Name.Contains("Renderer"))
                                {
                                    Debug.Log($"  - {type.FullName}");
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"  Error getting types: {e.Message}");
                        }
                    }
                }
                
                EditorApplication.Exit(1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test setup failed: {e.Message}");
            Debug.LogError($"Stack trace:\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacter(GameObject charGO, object renderer)
    {
        try
        {
            Debug.Log("\n=== ANALYZING CHARACTER RENDERING ===");
            
            // Find all sprite renderers
            var sprites = charGO.GetComponentsInChildren<SpriteRenderer>();
            Debug.Log($"Found {sprites.Length} sprite renderers:");
            
            foreach (var sr in sprites)
            {
                Debug.Log($"\n[{sr.gameObject.name}]");
                Debug.Log($"  Local Position: {sr.transform.localPosition}");
                Debug.Log($"  World Position: {sr.transform.position}");
                Debug.Log($"  Sorting Order: {sr.sortingOrder}");
                Debug.Log($"  Has Sprite: {sr.sprite != null}");
                
                if (sr.sprite != null)
                {
                    Debug.Log($"  Sprite Name: {sr.sprite.name}");
                    Debug.Log($"  Sprite Size: {sr.sprite.rect.width}x{sr.sprite.rect.height}");
                    Debug.Log($"  Sprite Pivot: {sr.sprite.pivot}");
                    Debug.Log($"  Pivot Normalized: ({sr.sprite.pivot.x / sr.sprite.rect.width:F3}, {sr.sprite.pivot.y / sr.sprite.rect.height:F3})");
                }
            }
            
            // Check specific body parts
            Debug.Log("\n=== BODY PART POSITIONS ===");
            CheckBodyPart(charGO, "body", 0.00f);
            CheckBodyPart(charGO, "arm", 0.20f);
            CheckBodyPart(charGO, "head", 0.28f);
            CheckBodyPart(charGO, "face", 0.28f);
            
            // Check attachment points via reflection
            if (renderer != null)
            {
                Debug.Log("\n=== ATTACHMENT POINTS ===");
                var attachmentField = renderer.GetType().GetField("currentAttachmentPoints", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (attachmentField != null)
                {
                    var attachments = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
                    if (attachments != null && attachments.Count > 0)
                    {
                        Debug.Log($"Found {attachments.Count} attachment points:");
                        foreach (var kvp in attachments)
                        {
                            Debug.Log($"  {kvp.Key}: {kvp.Value}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No attachment points found");
                    }
                }
            }
            
            // Final summary
            Debug.Log("\n=== RENDERING VALIDATION SUMMARY ===");
            
            bool hasIssues = false;
            
            var body = charGO.transform.Find("body");
            var arm = charGO.transform.Find("arm");
            var head = charGO.transform.Find("head");
            
            if (body == null || arm == null || head == null)
            {
                Debug.LogError("CRITICAL: Missing body parts!");
                hasIssues = true;
            }
            else
            {
                float armDiff = arm.localPosition.y - body.localPosition.y;
                float headDiff = head.localPosition.y - body.localPosition.y;
                
                if (System.Math.Abs(armDiff - 0.20f) > 0.01f)
                {
                    Debug.LogError($"ISSUE: Arm positioning incorrect. Expected Y=0.20, actual diff={armDiff:F3}");
                    hasIssues = true;
                }
                
                if (headDiff < 0.20f)
                {
                    Debug.LogError($"ISSUE: Head too low. Expected Y>0.20, actual diff={headDiff:F3}");
                    hasIssues = true;
                }
                
                if (!hasIssues)
                {
                    Debug.Log("SUCCESS: All body parts correctly positioned!");
                    Debug.Log($"  Body: Y={body.localPosition.y:F3}");
                    Debug.Log($"  Arm: Y={arm.localPosition.y:F3} (diff={armDiff:F3})");
                    Debug.Log($"  Head: Y={head.localPosition.y:F3} (diff={headDiff:F3})");
                }
            }
            
            Debug.Log("\n=== TEST COMPLETE ===");
            EditorApplication.Exit(hasIssues ? 1 : 0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Analysis failed: {e.Message}");
            Debug.LogError($"Stack trace:\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckBodyPart(GameObject parent, string partName, float expectedY)
    {
        var part = parent.transform.Find(partName);
        if (part != null)
        {
            float actualY = part.localPosition.y;
            float diff = System.Math.Abs(actualY - expectedY);
            
            if (diff < 0.01f)
            {
                Debug.Log($"{partName.ToUpper()}: Y={actualY:F3} ✓ (matches expected {expectedY:F3})");
            }
            else
            {
                Debug.LogWarning($"{partName.ToUpper()}: Y={actualY:F3} (expected {expectedY:F3}, diff={diff:F3})");
            }
        }
        else
        {
            Debug.LogError($"{partName.ToUpper()}: NOT FOUND!");
        }
    }
}