using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameView;

public static class TestRealCharacterData
{
    [MenuItem("MapleUnity/Test Real Character Data")]
    public static void TestRealData()
    {
        Debug.Log("=== TESTING REAL CHARACTER DATA ===");
        
        // Create test scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create character object
        GameObject charGO = new GameObject("TestCharacter");
        charGO.transform.position = Vector3.zero;
        
        // Add renderer
        var renderer = charGO.AddComponent<MapleCharacterRenderer>();
        
        // Force immediate loading
        EditorApplication.delayCall += () => TestCharacterData(renderer);
    }
    
    static void TestCharacterData(MapleCharacterRenderer renderer)
    {
        Debug.Log("\n=== LOADING REAL NX DATA ===");
        
        var loader = NXAssetLoader.Instance;
        if (loader == null)
        {
            Debug.LogError("NXAssetLoader not available!");
            return;
        }
        
        // Test body parts loading
        Debug.Log("\n=== TESTING BODY PARTS ===");
        Dictionary<string, Vector2> attachmentPoints;
        var bodyParts = loader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
        
        if (bodyParts != null)
        {
            Debug.Log($"Loaded {bodyParts.Count} body parts");
            foreach (var part in bodyParts)
            {
                Debug.Log($"\n[{part.Key}]");
                Debug.Log($"  Sprite: {part.Value.name}");
                Debug.Log($"  Size: {part.Value.rect.width}x{part.Value.rect.height}");
                Debug.Log($"  Pivot: {part.Value.pivot}");
                Debug.Log($"  Pivot Normalized: ({part.Value.pivot.x/part.Value.rect.width:F3}, {part.Value.pivot.y/part.Value.rect.height:F3})");
            }
            
            Debug.Log($"\nAttachment points found: {attachmentPoints.Count}");
            foreach (var attach in attachmentPoints)
            {
                Debug.Log($"  {attach.Key}: {attach.Value}");
            }
        }
        
        // Test face loading
        Debug.Log("\n=== TESTING FACE DATA ===");
        var faceSprite = loader.LoadFace(20000, "default");
        if (faceSprite != null)
        {
            Debug.Log($"Face sprite loaded: {faceSprite.name}");
            Debug.Log($"Face pivot: {faceSprite.pivot}");
            Debug.Log($"Face size: {faceSprite.rect.width}x{faceSprite.rect.height}");
        }
        
        // Test facing direction
        Debug.Log("\n=== TESTING FACING DIRECTION ===");
        
        // Check current facing
        var bodyTransform = renderer.transform.Find("body");
        if (bodyTransform != null)
        {
            var bodySR = bodyTransform.GetComponent<SpriteRenderer>();
            if (bodySR != null)
            {
                Debug.Log($"Body flipX: {bodySR.flipX}");
                
                // Test flipping
                Debug.Log("Testing SetFlipX(true)...");
                var setFlipMethod = renderer.GetType().GetMethod("SetFlipX", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setFlipMethod != null)
                {
                    setFlipMethod.Invoke(renderer, new object[] { true });
                    Debug.Log($"After flip - Body flipX: {bodySR.flipX}");
                    
                    // Flip back
                    setFlipMethod.Invoke(renderer, new object[] { false });
                    Debug.Log($"After flip back - Body flipX: {bodySR.flipX}");
                }
            }
        }
        
        // Analyze positions
        Debug.Log("\n=== ANALYZING POSITIONS ===");
        AnalyzeCharacterPositions(renderer);
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
    
    static void AnalyzeCharacterPositions(MapleCharacterRenderer renderer)
    {
        var bodyT = renderer.transform.Find("body");
        var armT = renderer.transform.Find("arm");
        var headT = renderer.transform.Find("head");
        var faceT = renderer.transform.Find("face");
        
        if (bodyT && headT)
        {
            Debug.Log($"\nBody position: {bodyT.localPosition}");
            Debug.Log($"Head position: {headT.localPosition}");
            Debug.Log($"Head offset from body: {headT.localPosition - bodyT.localPosition}");
            
            if (armT)
            {
                Debug.Log($"Arm position: {armT.localPosition}");
                Debug.Log($"Arm offset from body: {armT.localPosition - bodyT.localPosition}");
                
                float armRelativeY = (armT.localPosition.y - bodyT.localPosition.y);
                Debug.Log($"Arm Y relative to body: {armRelativeY:F3}");
                if (armRelativeY < 0.1f)
                {
                    Debug.LogError("ERROR: Arm appears too low (near legs)!");
                }
                else if (armRelativeY > 0.15f && armRelativeY < 0.25f)
                {
                    Debug.Log("Arm correctly positioned at mid-body");
                }
            }
            
            if (faceT && headT)
            {
                Debug.Log($"Face position: {faceT.localPosition}");
                Debug.Log($"Face offset from head: {faceT.localPosition - headT.localPosition}");
                
                if (Mathf.Abs(faceT.localPosition.y - headT.localPosition.y) > 0.1f)
                {
                    Debug.LogError("ERROR: Face/eyes not aligned with head!");
                }
            }
        }
        
        // Check all sprite renderers
        Debug.Log("\n=== ALL SPRITE RENDERERS ===");
        var allSprites = renderer.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSprites)
        {
            Debug.Log($"{sr.gameObject.name}: pos={sr.transform.localPosition}, flipX={sr.flipX}, order={sr.sortingOrder}");
        }
    }
    
    public static void RunBatchTest()
    {
        TestRealData();
    }
}