using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class TestAttachmentPoints
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "attachment-point-test.log");
        
        try
        {
            File.WriteAllText(logPath, $"=== Attachment Point Test - {DateTime.Now} ===\n\n");
            Debug.Log("Starting attachment point test...");
            
            // Test loading body parts with attachment points
            File.AppendAllText(logPath, "Testing NXAssetLoader.LoadCharacterBodyParts():\n\n");
            
            var loader = MapleClient.GameData.NXAssetLoader.Instance;
            if (loader == null)
            {
                File.AppendAllText(logPath, "[ERROR] NXAssetLoader.Instance is null\n");
                EditorApplication.Exit(1);
                return;
            }
            
            // Test loading body parts for standing animation
            System.Collections.Generic.Dictionary<string, UnityEngine.Vector2> attachmentPoints;
            var bodyParts = loader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
            
            if (bodyParts != null && bodyParts.Count > 0)
            {
                File.AppendAllText(logPath, $"Successfully loaded {bodyParts.Count} body parts:\n");
                foreach (var part in bodyParts)
                {
                    File.AppendAllText(logPath, $"  - {part.Key}: {part.Value.name} ({part.Value.rect.width}x{part.Value.rect.height})\n");
                }
                File.AppendAllText(logPath, "\n");
                
                // Check attachment points
                if (attachmentPoints != null && attachmentPoints.Count > 0)
                {
                    File.AppendAllText(logPath, $"Found {attachmentPoints.Count} attachment points:\n");
                    foreach (var point in attachmentPoints)
                    {
                        File.AppendAllText(logPath, $"  - {point.Key}: {point.Value}\n");
                        File.AppendAllText(logPath, $"    Unity position: ({point.Value.x / 100f:F3}, {-point.Value.y / 100f:F3})\n");
                    }
                }
                else
                {
                    File.AppendAllText(logPath, "No attachment points found (null or empty)\n");
                }
                
                // Test specific expected attachment points
                File.AppendAllText(logPath, "\n=== Expected Attachment Points ===\n");
                string[] expectedPoints = { "neck", "navel", "hand", "body.neck", "body.navel", "arm.navel", "arm.hand" };
                foreach (var key in expectedPoints)
                {
                    if (attachmentPoints != null && attachmentPoints.ContainsKey(key))
                    {
                        var point = attachmentPoints[key];
                        File.AppendAllText(logPath, $"{key}: FOUND at {point} (Unity: {point.x / 100f:F3}, {-point.y / 100f:F3})\n");
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"{key}: NOT FOUND\n");
                    }
                }
                
                // Calculate expected positions
                File.AppendAllText(logPath, "\n=== Position Calculations ===\n");
                
                // Body should be at origin
                File.AppendAllText(logPath, "Body position: (0, 0)\n");
                
                // Arm position calculation
                if (attachmentPoints != null)
                {
                    UnityEngine.Vector2 bodyNavel = UnityEngine.Vector2.zero;
                    UnityEngine.Vector2 armNavel = UnityEngine.Vector2.zero;
                    
                    if (attachmentPoints.TryGetValue("body.navel", out bodyNavel) || attachmentPoints.TryGetValue("navel", out bodyNavel))
                    {
                        File.AppendAllText(logPath, $"Body navel: {bodyNavel}\n");
                    }
                    
                    if (attachmentPoints.TryGetValue("arm.navel", out armNavel))
                    {
                        File.AppendAllText(logPath, $"Arm navel: {armNavel}\n");
                        
                        // Calculate arm offset
                        float armOffsetX = (bodyNavel.x - armNavel.x) / 100f;
                        float armOffsetY = -(bodyNavel.y - armNavel.y) / 100f;
                        File.AppendAllText(logPath, $"Calculated arm offset: ({armOffsetX:F3}, {armOffsetY:F3})\n");
                    }
                    
                    // Head position from neck
                    UnityEngine.Vector2 bodyNeck = UnityEngine.Vector2.zero;
                    if (attachmentPoints.TryGetValue("body.neck", out bodyNeck) || attachmentPoints.TryGetValue("neck", out bodyNeck))
                    {
                        float headX = bodyNeck.x / 100f;
                        float headY = -bodyNeck.y / 100f;
                        File.AppendAllText(logPath, $"Head position from neck: ({headX:F3}, {headY:F3})\n");
                    }
                }
            }
            else
            {
                File.AppendAllText(logPath, "[ERROR] Failed to load body parts\n");
            }
            
            File.AppendAllText(logPath, "\n=== TEST COMPLETED ===\n");
            Debug.Log("Test completed! Check attachment-point-test.log");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\n[ERROR] Test failed: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}