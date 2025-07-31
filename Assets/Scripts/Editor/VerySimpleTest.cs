using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public static class VerySimpleTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\cpp-formula-test-results.txt";
        var log = new System.Text.StringBuilder();
        
        try
        {
            log.AppendLine("=== Testing C++ Character Rendering Formulas ===");
            log.AppendLine($"Test started at: {System.DateTime.Now}");
            
            // Create a test GameObject
            GameObject testObject = new GameObject("TestCharacter");
            testObject.transform.position = Vector3.zero;
            log.AppendLine($"\nCreated test object at position: {testObject.transform.position}");
            
            // Try to add MapleCharacterRenderer if it compiles
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType != null)
            {
                log.AppendLine("\nFound MapleCharacterRenderer type!");
                var renderer = testObject.AddComponent(rendererType);
                log.AppendLine("Added MapleCharacterRenderer component");
                
                // Create a minimal player for testing
                var playerType = System.Type.GetType("MapleClient.GameLogic.Core.Player, Assembly-CSharp");
                if (playerType != null)
                {
                    var vector2Type = System.Type.GetType("MapleClient.GameLogic.Vector2, Assembly-CSharp");
                    var vector2Constructor = vector2Type.GetConstructor(new[] { typeof(float), typeof(float) });
                    var position = vector2Constructor.Invoke(new object[] { 0f, 0f });
                    
                    var playerConstructor = playerType.GetConstructor(new[] { vector2Type });
                    var player = playerConstructor.Invoke(new[] { position });
                    
                    // Initialize the renderer
                    var initMethod = rendererType.GetMethod("Initialize");
                    initMethod.Invoke(renderer, new[] { player, null });
                    log.AppendLine("Initialized renderer with player");
                    
                    // Force an update
                    var updateMethod = rendererType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (updateMethod != null)
                    {
                        updateMethod.Invoke(renderer, null);
                        log.AppendLine("Called Update on renderer");
                    }
                    
                    // Log character positions
                    log.AppendLine("\n=== Character Part Positions ===");
                    LogCharacterPositions(renderer, rendererType, log);
                    
                    // Log attachment points
                    log.AppendLine("\n=== Attachment Points ===");
                    LogAttachmentPoints(renderer, rendererType, log);
                }
                else
                {
                    log.AppendLine("ERROR: Could not find Player type");
                }
            }
            else
            {
                log.AppendLine("ERROR: MapleCharacterRenderer type not found - likely compilation errors");
            }
            
            // Clean up
            GameObject.DestroyImmediate(testObject);
            
            log.AppendLine("\n=== Test Complete ===");
            log.AppendLine("Check the positions to verify C++ formulas are applied correctly:");
            log.AppendLine("1. Body's navel should be at (0,0)");
            log.AppendLine("2. Arm should align its navel with body's navel");
            log.AppendLine("3. Head should align its neck with body's neck");
            log.AppendLine("4. Face should be at head position + head's brow offset");
            
            File.WriteAllText(logPath, log.ToString());
            Debug.Log($"Test results written to: {logPath}");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            log.AppendLine($"\nERROR: Test failed - {e.Message}");
            log.AppendLine($"Stack trace:\n{e.StackTrace}");
            File.WriteAllText(logPath, log.ToString());
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogCharacterPositions(object renderer, System.Type rendererType, System.Text.StringBuilder log)
    {
        var transform = (renderer as Component).transform;
        var parts = new string[] { "Body", "Arm", "Head", "Face", "Hair" };
        
        foreach (var partName in parts)
        {
            var partTransform = transform.Find(partName);
            if (partTransform != null)
            {
                var sr = partTransform.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    log.AppendLine($"\n{partName}:");
                    log.AppendLine($"  Local Position: {partTransform.localPosition}");
                    log.AppendLine($"  World Position: {partTransform.position}");
                    log.AppendLine($"  Sprite: {sr.sprite.name}");
                    log.AppendLine($"  Pivot: {sr.sprite.pivot}");
                    log.AppendLine($"  Bounds: {sr.bounds}");
                }
                else
                {
                    log.AppendLine($"\n{partName}: No sprite renderer or sprite");
                }
            }
            else
            {
                log.AppendLine($"\n{partName}: Not found");
            }
        }
    }
    
    private static void LogAttachmentPoints(object renderer, System.Type rendererType, System.Text.StringBuilder log)
    {
        var attachmentField = rendererType.GetField("currentAttachmentPoints", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (attachmentField != null)
        {
            var attachments = attachmentField.GetValue(renderer) as Dictionary<string, Vector2>;
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var kvp in attachments)
                {
                    log.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                log.AppendLine("  No attachment points found");
            }
        }
        else
        {
            log.AppendLine("  Could not access attachment points field");
        }
    }
}