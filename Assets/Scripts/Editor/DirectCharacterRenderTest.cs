using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Collections.Generic;

public static class DirectCharacterRenderTest
{
    public static void RunTest()
    {
        Debug.Log("=== DIRECT CHARACTER RENDERING TEST ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Batch Mode: {Application.isBatchMode}");
        
        try
        {
            // Initialize mock NX data
            Debug.Log("Initializing mock NX data...");
            var mockCharFile = new MockNxFile("character.nx");
            NXAssetLoader.Instance.RegisterNxFile("character", mockCharFile);
            Debug.Log("Registered mock character NX file");
            
            // Create new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create test character
            GameObject charGO = new GameObject("TestCharacter");
            charGO.transform.position = Vector3.zero;
            
            // Add MapleCharacterRenderer
            var renderer = charGO.AddComponent<MapleCharacterRenderer>();
            Debug.Log("Created character with MapleCharacterRenderer");
            
            // Force immediate initialization
            var startMethod = renderer.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (startMethod != null)
            {
                startMethod.Invoke(renderer, null);
                Debug.Log("Called Start() method");
            }
            
            // Wait one frame by forcing an update
            UnityEditor.EditorApplication.Step();
            
            // Now analyze the results
            AnalyzeCharacterRendering(charGO, renderer);
            
            Debug.Log("\n=== TEST COMPLETE ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError($"Stack trace:\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacterRendering(GameObject charGO, MapleCharacterRenderer renderer)
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
        var body = charGO.transform.Find("body");
        var arm = charGO.transform.Find("arm");
        var head = charGO.transform.Find("head");
        var face = charGO.transform.Find("face");
        
        if (body != null) Debug.Log($"BODY: Y={body.localPosition.y:F3}");
        else Debug.LogError("BODY NOT FOUND!");
        
        if (arm != null) Debug.Log($"ARM: Y={arm.localPosition.y:F3}");
        else Debug.LogError("ARM NOT FOUND!");
        
        if (head != null) 
        {
            Debug.Log($"HEAD: Y={head.localPosition.y:F3}");
            // Check for face as child of head
            var faceChild = head.Find("face");
            if (faceChild != null)
            {
                Debug.Log($"FACE (child of head): Local Y={faceChild.localPosition.y:F3}, World Y={faceChild.position.y:F3}");
            }
        }
        else Debug.LogError("HEAD NOT FOUND!");
        
        if (face != null && face.parent == charGO.transform)
        {
            Debug.LogWarning($"FACE at root level: Y={face.localPosition.y:F3}");
        }
        
        // Check attachment points
        Debug.Log("\n=== ATTACHMENT POINTS ===");
        var attachmentField = renderer.GetType().GetField("currentAttachmentPoints", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
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
        
        // Relative position analysis
        Debug.Log("\n=== RELATIVE POSITION ANALYSIS ===");
        if (body != null && arm != null && head != null)
        {
            float armDiff = arm.localPosition.y - body.localPosition.y;
            float headDiff = head.localPosition.y - body.localPosition.y;
            
            Debug.Log($"Arm vs Body Y difference: {armDiff:F3}");
            Debug.Log($"Head vs Body Y difference: {headDiff:F3}");
            
            if (armDiff < -0.1f)
            {
                Debug.LogError("ERROR: ARM APPEARS BELOW BODY!");
            }
            else if (System.Math.Abs(armDiff - 0.20f) < 0.01f)
            {
                Debug.Log("SUCCESS: Arm correctly positioned at Y=0.20");
            }
            else
            {
                Debug.LogWarning($"WARNING: Arm position differs from expected. Expected diff=0.20, actual={armDiff:F3}");
            }
            
            if (headDiff < 0.20f)
            {
                Debug.LogError("ERROR: HEAD TOO LOW!");
            }
            else if (headDiff > 0.25f && headDiff < 0.35f)
            {
                Debug.Log("SUCCESS: Head correctly positioned above body");
            }
            
            // Final layout summary
            Debug.Log("\n=== LAYOUT SUMMARY ===");
            Debug.Log($"Expected: Body=0.00, Arm=0.20, Head=0.28+");
            Debug.Log($"Actual: Body={body.localPosition.y:F3}, Arm={arm.localPosition.y:F3}, Head={head.localPosition.y:F3}");
        }
        
        // Check for specific rendering issues
        Debug.Log("\n=== ISSUE DETECTION ===");
        bool hasIssues = false;
        
        if (sprites.Length == 0)
        {
            Debug.LogError("CRITICAL: No sprite renderers found!");
            hasIssues = true;
        }
        
        if (body == null || arm == null || head == null)
        {
            Debug.LogError("CRITICAL: Missing essential body parts!");
            hasIssues = true;
        }
        
        if (!hasIssues)
        {
            Debug.Log("All checks passed - character rendering appears correct!");
        }
    }
}