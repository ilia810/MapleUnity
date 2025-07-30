using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;

public static class ValidateCharacterRenderingFixes
{
    public static void RunValidation()
    {
        Debug.Log("=== Character Rendering Validation Test ===");
        Debug.Log($"Starting at: {System.DateTime.Now}");
        
        try
        {
            // Load the Henesys scene
            var scenePath = "Assets/Scenes/henesys.unity";
            Debug.Log($"Loading scene: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath);
            
            // Find all player characters
            var playerViews = GameObject.FindObjectsOfType<PlayerView>();
            Debug.Log($"Found {playerViews.Length} PlayerView objects");
            
            if (playerViews.Length == 0)
            {
                Debug.LogError("No PlayerView objects found! Creating test character...");
                CreateTestCharacter();
                playerViews = GameObject.FindObjectsOfType<PlayerView>();
            }
            
            // Test each player
            foreach (var playerView in playerViews)
            {
                Debug.Log($"\n=== Testing PlayerView: {playerView.name} ===");
                ValidatePlayerRendering(playerView);
            }
            
            Debug.Log("\n=== Test Complete ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed with exception: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CreateTestCharacter()
    {
        // Create a test character
        var playerGO = new GameObject("TestPlayer");
        var playerView = playerGO.AddComponent<PlayerView>();
        
        // Create character renderer
        var charGO = new GameObject("CharacterRenderer");
        charGO.transform.SetParent(playerGO.transform);
        var charRenderer = charGO.AddComponent<MapleCharacterRenderer>();
        
        // Use reflection to set skin, face, and hair IDs
        var rendererType = charRenderer.GetType();
        var skinField = rendererType.GetField("skinColor", BindingFlags.NonPublic | BindingFlags.Instance);
        var faceField = rendererType.GetField("faceId", BindingFlags.NonPublic | BindingFlags.Instance);
        var hairField = rendererType.GetField("hairId", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (skinField != null) skinField.SetValue(charRenderer, 0);
        if (faceField != null) faceField.SetValue(charRenderer, 20000);
        if (hairField != null) hairField.SetValue(charRenderer, 30000);
        
        // Initialize using the Initialize method with mock player and data provider
        var initMethod = rendererType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
        if (initMethod != null)
        {
            // Create a mock player - we can pass null for testing
            initMethod.Invoke(charRenderer, new object[] { null, null });
        }
        
        Debug.Log("Created test character with basic appearance");
    }
    
    private static void ValidatePlayerRendering(PlayerView playerView)
    {
        // Find the character renderer
        var charRenderer = playerView.GetComponentInChildren<MapleCharacterRenderer>();
        if (charRenderer == null)
        {
            Debug.LogError("No MapleCharacterRenderer found!");
            return;
        }
        
        // Get renderer internals via reflection
        var rendererType = charRenderer.GetType();
        var bodyPartField = rendererType.GetField("bodyParts", BindingFlags.NonPublic | BindingFlags.Instance);
        var bodyParts = bodyPartField?.GetValue(charRenderer) as Dictionary<string, GameObject>;
        
        if (bodyParts == null)
        {
            Debug.LogError("Could not access bodyParts dictionary!");
            return;
        }
        
        // 1. Test sprite pivot Y-flip formula
        Debug.Log("\n--- Testing Sprite Pivot Y-Flip Formula ---");
        TestSpritePivotFlip(charRenderer, bodyParts);
        
        // 2. Test attachment point calculations
        Debug.Log("\n--- Testing Attachment Point Calculations ---");
        TestAttachmentPoints(charRenderer, bodyParts);
        
        // 3. Test facing direction changes
        Debug.Log("\n--- Testing Facing Direction Changes ---");
        TestFacingDirection(charRenderer, playerView);
        
        // 4. Verify body part positions
        Debug.Log("\n--- Testing Body Part Positions ---");
        TestBodyPartPositions(bodyParts);
        
        // 5. Check face/eyes alignment
        Debug.Log("\n--- Testing Face/Eyes Alignment ---");
        TestFaceAlignment(bodyParts);
    }
    
    private static void TestSpritePivotFlip(MapleCharacterRenderer charRenderer, Dictionary<string, GameObject> bodyParts)
    {
        // Test each body part's flip state
        foreach (var kvp in bodyParts)
        {
            var partName = kvp.Key;
            var partGO = kvp.Value;
            var spriteRenderer = partGO.GetComponent<SpriteRenderer>();
            
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var sprite = spriteRenderer.sprite;
                var pivot = sprite.pivot;
                var textureHeight = sprite.texture.height;
                
                // Calculate expected flipped pivot Y
                var expectedFlippedY = textureHeight - pivot.y;
                
                Debug.Log($"Part: {partName}");
                Debug.Log($"  Original Pivot: {pivot}");
                Debug.Log($"  Texture Height: {textureHeight}");
                Debug.Log($"  Expected Flipped Y: {expectedFlippedY}");
                Debug.Log($"  Flip X: {spriteRenderer.flipX}");
                
                // Check if the formula is being applied correctly
                if (spriteRenderer.flipX)
                {
                    Debug.Log($"  [FLIPPED] Part is horizontally flipped");
                }
            }
        }
    }
    
    private static void TestAttachmentPoints(MapleCharacterRenderer charRenderer, Dictionary<string, GameObject> bodyParts)
    {
        // Get attachment data via reflection
        var rendererType = charRenderer.GetType();
        var attachmentDataField = rendererType.GetField("attachmentData", BindingFlags.NonPublic | BindingFlags.Instance);
        var attachmentData = attachmentDataField?.GetValue(charRenderer) as Dictionary<string, Vector2>;
        
        if (attachmentData == null)
        {
            Debug.LogWarning("Could not access attachmentData!");
            return;
        }
        
        // Log all attachment points
        foreach (var kvp in attachmentData)
        {
            Debug.Log($"Attachment Point: {kvp.Key} = {kvp.Value}");
        }
        
        // Test specific attachment calculations
        if (bodyParts.ContainsKey("head") && bodyParts.ContainsKey("body"))
        {
            var headGO = bodyParts["head"];
            var bodyGO = bodyParts["body"];
            
            // Get neck positions
            var bodyNeck = attachmentData.ContainsKey("body.neck") ? attachmentData["body.neck"] : Vector2.zero;
            var headNeck = attachmentData.ContainsKey("head.neck") ? attachmentData["head.neck"] : Vector2.zero;
            
            // Calculate expected head position
            var expectedHeadOffset = bodyNeck - headNeck;
            var actualHeadPos = headGO.transform.localPosition;
            
            Debug.Log($"\nHead Attachment Test:");
            Debug.Log($"  Body.neck: {bodyNeck}");
            Debug.Log($"  Head.neck: {headNeck}");
            Debug.Log($"  Expected Offset: {expectedHeadOffset}");
            Debug.Log($"  Actual Position: {actualHeadPos}");
            Debug.Log($"  Match: {Vector2.Distance(new Vector2(actualHeadPos.x, actualHeadPos.y), expectedHeadOffset) < 0.01f}");
        }
        
        // Test arm attachment
        if (attachmentData.ContainsKey("body.navel") && attachmentData.ContainsKey("arm.navel"))
        {
            var bodyNavel = attachmentData["body.navel"];
            var armNavel = attachmentData["arm.navel"];
            var expectedArmOffset = bodyNavel - armNavel;
            
            Debug.Log($"\nArm Attachment Test:");
            Debug.Log($"  Body.navel: {bodyNavel}");
            Debug.Log($"  Arm.navel: {armNavel}");
            Debug.Log($"  Expected Offset: {expectedArmOffset}");
        }
        
        // Test face attachment
        if (bodyParts.ContainsKey("face") && bodyParts.ContainsKey("head"))
        {
            var faceGO = bodyParts["face"];
            var headGO = bodyParts["head"];
            var headBrow = attachmentData.ContainsKey("head.brow") ? attachmentData["head.brow"] : Vector2.zero;
            
            var actualFacePos = faceGO.transform.localPosition;
            var headPos = headGO.transform.localPosition;
            
            Debug.Log($"\nFace Attachment Test:");
            Debug.Log($"  Head Position: {headPos}");
            Debug.Log($"  Head.brow: {headBrow}");
            Debug.Log($"  Face Position: {actualFacePos}");
            Debug.Log($"  Expected Face Pos: {new Vector2(headPos.x, headPos.y) + headBrow}");
        }
    }
    
    private static void TestFacingDirection(MapleCharacterRenderer charRenderer, PlayerView playerView)
    {
        // Test facing left
        Debug.Log("\nTesting Face Left:");
        var faceLeftMethod = charRenderer.GetType().GetMethod("UpdateFacingDirection", BindingFlags.NonPublic | BindingFlags.Instance);
        if (faceLeftMethod != null)
        {
            // Simulate velocity change to left
            faceLeftMethod.Invoke(charRenderer, new object[] { -1f });
            LogBodyPartFlipStates(charRenderer);
        }
        
        // Test facing right
        Debug.Log("\nTesting Face Right:");
        if (faceLeftMethod != null)
        {
            // Simulate velocity change to right
            faceLeftMethod.Invoke(charRenderer, new object[] { 1f });
            LogBodyPartFlipStates(charRenderer);
        }
    }
    
    private static void LogBodyPartFlipStates(MapleCharacterRenderer charRenderer)
    {
        var rendererType = charRenderer.GetType();
        var bodyPartField = rendererType.GetField("bodyParts", BindingFlags.NonPublic | BindingFlags.Instance);
        var bodyParts = bodyPartField?.GetValue(charRenderer) as Dictionary<string, GameObject>;
        
        if (bodyParts != null)
        {
            foreach (var kvp in bodyParts)
            {
                var sr = kvp.Value.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Debug.Log($"  {kvp.Key}: flipX = {sr.flipX}");
                }
            }
        }
    }
    
    private static void TestBodyPartPositions(Dictionary<string, GameObject> bodyParts)
    {
        Debug.Log("\nBody Part Positions:");
        foreach (var kvp in bodyParts)
        {
            var pos = kvp.Value.transform.localPosition;
            var rot = kvp.Value.transform.localRotation.eulerAngles;
            var scale = kvp.Value.transform.localScale;
            
            Debug.Log($"{kvp.Key}:");
            Debug.Log($"  Position: {pos}");
            Debug.Log($"  Rotation: {rot}");
            Debug.Log($"  Scale: {scale}");
            
            var sr = kvp.Value.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Debug.Log($"  Sprite: {sr.sprite.name}");
                Debug.Log($"  Pivot: {sr.sprite.pivot}");
                Debug.Log($"  Bounds: {sr.sprite.bounds}");
            }
        }
    }
    
    private static void TestFaceAlignment(Dictionary<string, GameObject> bodyParts)
    {
        if (!bodyParts.ContainsKey("face") || !bodyParts.ContainsKey("head"))
        {
            Debug.LogWarning("Face or head not found!");
            return;
        }
        
        var faceGO = bodyParts["face"];
        var headGO = bodyParts["head"];
        
        var facePos = faceGO.transform.localPosition;
        var headPos = headGO.transform.localPosition;
        
        Debug.Log($"Face Position: {facePos}");
        Debug.Log($"Head Position: {headPos}");
        Debug.Log($"Face Relative to Head: {facePos - headPos}");
        
        // Check if eyes exist
        if (bodyParts.ContainsKey("eye"))
        {
            var eyeGO = bodyParts["eye"];
            var eyePos = eyeGO.transform.localPosition;
            Debug.Log($"Eye Position: {eyePos}");
            Debug.Log($"Eye Relative to Face: {eyePos - facePos}");
        }
        
        // Check visual alignment
        var faceSR = faceGO.GetComponent<SpriteRenderer>();
        var headSR = headGO.GetComponent<SpriteRenderer>();
        
        if (faceSR != null && headSR != null)
        {
            var faceBounds = faceSR.bounds;
            var headBounds = headSR.bounds;
            
            Debug.Log($"Face Bounds Center: {faceBounds.center}");
            Debug.Log($"Head Bounds Center: {headBounds.center}");
            Debug.Log($"Distance: {Vector3.Distance(faceBounds.center, headBounds.center)}");
        }
    }
}