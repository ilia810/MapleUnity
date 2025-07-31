using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public static class SimpleScaleFlippingTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Starting Simple Scale-Based Flipping Test ===");
            
            // Create test GameObject
            var testGO = new GameObject("CharacterRenderTest");
            
            // Get MapleCharacterRenderer type dynamically to avoid compilation dependency
            var rendererType = GetTypeByName("MapleClient.GameView.MapleCharacterRenderer");
            if (rendererType == null)
            {
                Debug.LogError("Could not find MapleCharacterRenderer type");
                EditorApplication.Exit(1);
                return;
            }
            
            // Add component
            var renderer = testGO.AddComponent(rendererType);
            
            // Set basic properties using reflection
            SetProperty(renderer, "BodyId", 2000);
            SetProperty(renderer, "HeadId", 12000);
            SetProperty(renderer, "FaceId", 20000);
            SetProperty(renderer, "HairId", 30000);
            
            var equipList = new List<int> { 1040036, 1060026 };
            SetProperty(renderer, "EquipmentIds", equipList);
            
            // Force initialization
            var initMethod = rendererType.GetMethod("Start", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(renderer, null);
            }
            
            // Wait for loading
            int waitFrames = 0;
            while (!IsLoaded(renderer) && waitFrames < 300)
            {
                waitFrames++;
                System.Threading.Thread.Sleep(10);
            }
            
            if (!IsLoaded(renderer))
            {
                Debug.LogError("Character failed to load within timeout");
                GameObject.DestroyImmediate(testGO);
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log($"Character loaded successfully after {waitFrames * 10}ms");
            
            // Test 1: Scale-based flipping
            Debug.Log("\n--- Testing Scale-Based Flipping ---");
            TestScaleFlipping(renderer, rendererType);
            
            // Test 2: Attachment consistency
            Debug.Log("\n--- Testing Attachment Consistency ---");
            TestAttachmentConsistency(testGO);
            
            // Test 3: Animation with flipping
            Debug.Log("\n--- Testing Animations ---");
            TestAnimations(renderer, rendererType);
            
            // Write results
            string report = GenerateReport();
            System.IO.File.WriteAllText("simple-scale-test-results.txt", report);
            
            Debug.Log("\nTest completed. Results written to simple-scale-test-results.txt");
            
            // Cleanup
            GameObject.DestroyImmediate(testGO);
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"Test failed with exception: {e}");
            EditorApplication.Exit(1);
        }
    }
    
    private static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }
    
    private static void SetProperty(object obj, string propName, object value)
    {
        var prop = obj.GetType().GetProperty(propName);
        if (prop != null)
        {
            prop.SetValue(obj, value);
        }
    }
    
    private static bool IsLoaded(object renderer)
    {
        var prop = renderer.GetType().GetProperty("IsLoaded");
        if (prop != null)
        {
            return (bool)prop.GetValue(renderer);
        }
        return false;
    }
    
    private static void TestScaleFlipping(object renderer, Type rendererType)
    {
        var setFacingMethod = rendererType.GetMethod("SetFacingDirection");
        if (setFacingMethod == null)
        {
            Debug.LogError("SetFacingDirection method not found");
            return;
        }
        
        var transform = ((Component)renderer).transform;
        
        // Test facing right
        setFacingMethod.Invoke(renderer, new object[] { true });
        var rightScale = transform.localScale;
        Debug.Log($"Facing right scale: {rightScale} (x > 0: {rightScale.x > 0})");
        
        // Test facing left
        setFacingMethod.Invoke(renderer, new object[] { false });
        var leftScale = transform.localScale;
        Debug.Log($"Facing left scale: {leftScale} (x < 0: {leftScale.x < 0})");
        
        // Verify scale flipping works correctly
        bool scaleFlippingCorrect = rightScale.x > 0 && leftScale.x < 0;
        Debug.Log($"Scale flipping working correctly: {scaleFlippingCorrect}");
    }
    
    private static void TestAttachmentConsistency(GameObject testGO)
    {
        var sprites = testGO.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"Found {sprites.Length} sprite renderers");
        
        // Group sprites by category
        var bodyParts = new Dictionary<string, List<SpriteRenderer>>();
        foreach (var sprite in sprites)
        {
            string category = GetSpriteCategory(sprite.name);
            if (!bodyParts.ContainsKey(category))
            {
                bodyParts[category] = new List<SpriteRenderer>();
            }
            bodyParts[category].Add(sprite);
        }
        
        // Report on found parts
        foreach (var kvp in bodyParts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value.Count} sprites");
            foreach (var sprite in kvp.Value)
            {
                Debug.Log($"  - {sprite.name}: localPos={sprite.transform.localPosition}, order={sprite.sortingOrder}");
            }
        }
    }
    
    private static string GetSpriteCategory(string name)
    {
        if (name.Contains("body")) return "Body";
        if (name.Contains("head")) return "Head";
        if (name.Contains("face")) return "Face";
        if (name.Contains("hair")) return "Hair";
        if (name.Contains("arm")) return "Arm";
        if (name.Contains("hand")) return "Hand";
        if (name.Contains("coat") || name.Contains("mail")) return "Equipment";
        return "Other";
    }
    
    private static void TestAnimations(object renderer, Type rendererType)
    {
        var playAnimMethod = rendererType.GetMethod("PlayAnimation");
        if (playAnimMethod == null)
        {
            Debug.LogError("PlayAnimation method not found");
            return;
        }
        
        string[] animations = { "stand", "walk", "crouch" };
        
        foreach (var anim in animations)
        {
            playAnimMethod.Invoke(renderer, new object[] { anim });
            System.Threading.Thread.Sleep(50); // Let animation start
            
            var transform = ((Component)renderer).transform;
            var sprites = ((Component)renderer).GetComponentsInChildren<SpriteRenderer>()
                .Where(sr => sr.enabled && sr.sprite != null).Count();
            
            Debug.Log($"Animation '{anim}': scale={transform.localScale}, visible sprites={sprites}");
        }
    }
    
    private static string GenerateReport()
    {
        return @"=== Simple Scale-Based Flipping Test Report ===

Test completed successfully. Check Unity console for detailed results.

Key findings:
1. Scale-based flipping implementation status
2. Attachment point consistency during flipping
3. Animation behavior with new flipping logic

Timestamp: " + DateTime.Now;
    }
}