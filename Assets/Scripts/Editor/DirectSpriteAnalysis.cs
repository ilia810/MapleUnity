using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using MapleClient.GameView;
using System.Reflection;
using System.IO;

public static class DirectSpriteAnalysis
{
    public static void AnalyzeSprites()
    {
        string logPath = @"C:\Users\me\MapleUnity\sprite-analysis-results.txt";
        
        try
        {
            File.WriteAllText(logPath, "=== SPRITE ORIGIN AND POSITION ANALYSIS ===\n\n");
            Debug.Log("=== SPRITE ORIGIN AND POSITION ANALYSIS ===");
            
            // Create test character
            GameObject charObj = new GameObject("TestCharacter");
            charObj.transform.position = Vector3.zero;
            var renderer = charObj.AddComponent<MapleCharacterRenderer>();
            
            // Force initialization by calling Start
            var type = typeof(MapleCharacterRenderer);
            var startMethod = type.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
            if (startMethod != null)
            {
                startMethod.Invoke(renderer, null);
                File.AppendAllText(logPath, "Called Start() method\n");
            }
            
            // Set appearance
            renderer.SetCharacterAppearance(0, 20000, 30000);
            renderer.UpdateAppearance();
            
            File.AppendAllText(logPath, "Character created and appearance set\n\n");
            
            // Use reflection to get sprite renderers
            // type already declared above
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            string[] parts = { "bodyRenderer", "armRenderer", "headRenderer", "faceRenderer" };
            
            File.AppendAllText(logPath, "=== SPRITE RENDERER ANALYSIS ===\n");
            
            foreach (var partName in parts)
            {
                var field = type.GetField(partName, flags);
                if (field != null)
                {
                    var sr = field.GetValue(renderer) as SpriteRenderer;
                    if (sr != null && sr.sprite != null)
                    {
                        var sprite = sr.sprite;
                        File.AppendAllText(logPath, $"\n{partName.ToUpper()}:\n");
                        File.AppendAllText(logPath, $"  GameObject Local Position: {sr.transform.localPosition}\n");
                        File.AppendAllText(logPath, $"  Sprite Name: {sprite.name}\n");
                        File.AppendAllText(logPath, $"  Texture Size: {sprite.texture.width}x{sprite.texture.height}\n");
                        File.AppendAllText(logPath, $"  Sprite Rect: {sprite.rect}\n");
                        File.AppendAllText(logPath, $"  Pivot (pixels): {sprite.pivot}\n");
                        File.AppendAllText(logPath, $"  Pivot (normalized): ({sprite.pivot.x/sprite.rect.width:F3}, {sprite.pivot.y/sprite.rect.height:F3})\n");
                        File.AppendAllText(logPath, $"  Pixels Per Unit: {sprite.pixelsPerUnit}\n");
                        
                        // Calculate bounds
                        var bounds = sr.bounds;
                        File.AppendAllText(logPath, $"  World Bounds: min=({bounds.min.x:F3}, {bounds.min.y:F3}), max=({bounds.max.x:F3}, {bounds.max.y:F3})\n");
                        File.AppendAllText(logPath, $"  Bounds Size: ({bounds.size.x:F3}, {bounds.size.y:F3})\n");
                        
                        // Calculate where sprite appears relative to its position
                        float pivotOffsetY = sprite.pivot.y / sprite.pixelsPerUnit;
                        float bottomY = sr.transform.localPosition.y - pivotOffsetY;
                        float topY = bottomY + (sprite.rect.height / sprite.pixelsPerUnit);
                        
                        File.AppendAllText(logPath, $"  Calculated positions:\n");
                        File.AppendAllText(logPath, $"    Bottom edge: Y={bottomY:F3}\n");
                        File.AppendAllText(logPath, $"    Top edge: Y={topY:F3}\n");
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"\n{partName}: No sprite loaded\n");
                    }
                }
            }
            
            // Check NX data directly
            File.AppendAllText(logPath, "\n=== NX DATA VERIFICATION ===\n");
            var nxLoader = NXAssetLoader.Instance;
            if (nxLoader != null)
            {
                var charFile = nxLoader.GetNxFile("character");
                if (charFile != null)
                {
                    // Check body origin
                    var bodyNode = charFile.GetNode("00002000.img/stand1/0/body");
                    if (bodyNode != null && bodyNode["origin"] != null)
                    {
                        File.AppendAllText(logPath, $"Body origin from NX: {bodyNode["origin"].Value}\n");
                    }
                    
                    // Check arm origin
                    var armNode = charFile.GetNode("00002000.img/stand1/0/arm");
                    if (armNode != null && armNode["origin"] != null)
                    {
                        File.AppendAllText(logPath, $"Arm origin from NX: {armNode["origin"].Value}\n");
                    }
                    
                    // Check head origin
                    var headNode = charFile.GetNode("00002000.img/stand1/0/head");
                    if (headNode != null && headNode["origin"] != null)
                    {
                        File.AppendAllText(logPath, $"Head origin from NX: {headNode["origin"].Value}\n");
                    }
                    
                    // Check face origin
                    var faceNode = charFile.GetNode("Face/00020000.img/default/face");
                    if (faceNode != null && faceNode["origin"] != null)
                    {
                        File.AppendAllText(logPath, $"Face origin from NX: {faceNode["origin"].Value}\n");
                    }
                    
                    // Check head attachment point
                    var bodyMapNode = charFile.GetNode("00002000.img/stand1/0/body/map");
                    if (bodyMapNode != null)
                    {
                        var headAttach = bodyMapNode["head"];
                        if (headAttach != null)
                        {
                            File.AppendAllText(logPath, $"Head attachment point from body/map: {headAttach.Value}\n");
                        }
                    }
                }
            }
            
            // Summary
            File.AppendAllText(logPath, "\n=== POSITION ANALYSIS SUMMARY ===\n");
            
            // Get all renderers again for summary
            var bodyField = type.GetField("bodyRenderer", flags);
            var armField = type.GetField("armRenderer", flags);
            var headField = type.GetField("headRenderer", flags);
            var faceField = type.GetField("faceRenderer", flags);
            
            var bodySR = bodyField?.GetValue(renderer) as SpriteRenderer;
            var armSR = armField?.GetValue(renderer) as SpriteRenderer;
            var headSR = headField?.GetValue(renderer) as SpriteRenderer;
            var faceSR = faceField?.GetValue(renderer) as SpriteRenderer;
            
            if (bodySR != null && armSR != null && headSR != null)
            {
                File.AppendAllText(logPath, $"Body local Y: {bodySR.transform.localPosition.y:F3}\n");
                File.AppendAllText(logPath, $"Arm local Y: {armSR.transform.localPosition.y:F3}\n");
                File.AppendAllText(logPath, $"Head local Y: {headSR.transform.localPosition.y:F3}\n");
                if (faceSR != null)
                {
                    File.AppendAllText(logPath, $"Face local Y: {faceSR.transform.localPosition.y:F3}\n");
                }
                
                File.AppendAllText(logPath, $"\nArm Y offset from body: {armSR.transform.localPosition.y - bodySR.transform.localPosition.y:F3}\n");
                File.AppendAllText(logPath, $"Head Y offset from body: {headSR.transform.localPosition.y - bodySR.transform.localPosition.y:F3}\n");
                
                // Check if arm is at wrong position
                if (armSR.transform.localPosition.y < bodySR.transform.localPosition.y)
                {
                    File.AppendAllText(logPath, "\nERROR: Arm is positioned BELOW body!\n");
                }
                if (System.Math.Abs(armSR.transform.localPosition.y - 0.2f) > 0.01f)
                {
                    File.AppendAllText(logPath, $"WARNING: Arm Y position is {armSR.transform.localPosition.y:F3}, expected 0.20\n");
                }
            }
            
            File.AppendAllText(logPath, "\n=== TEST COMPLETED ===\n");
            Debug.Log("Test completed. Results written to: " + logPath);
            
            // Clean up
            Object.DestroyImmediate(charObj);
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}