using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;

public static class FinalRenderingAnalysis
{
    public static void RunAnalysis()
    {
        string resultPath = @"C:\Users\me\MapleUnity\final-rendering-analysis.txt";
        
        try
        {
            File.WriteAllText(resultPath, "=== FINAL CHARACTER RENDERING ANALYSIS ===\n");
            File.AppendAllText(resultPath, $"Time: {DateTime.Now}\n\n");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(resultPath, $"Opened scene: {scene.name}\n\n");
            
            // Find Player GameObject
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Player GameObject not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "=== PLAYER TRANSFORM ANALYSIS ===\n");
            File.AppendAllText(resultPath, $"Player Position: {playerGO.transform.position}\n");
            File.AppendAllText(resultPath, $"Player Rotation: {playerGO.transform.rotation.eulerAngles}\n");
            File.AppendAllText(resultPath, $"Player Scale: {playerGO.transform.localScale}\n\n");
            
            // Analyze all sprite renderers
            var renderers = playerGO.GetComponentsInChildren<SpriteRenderer>(true);
            File.AppendAllText(resultPath, $"=== SPRITE RENDERER ANALYSIS ({renderers.Length} found) ===\n\n");
            
            foreach (var renderer in renderers)
            {
                File.AppendAllText(resultPath, $"[{renderer.name}]\n");
                File.AppendAllText(resultPath, $"  Active: {renderer.gameObject.activeSelf}\n");
                File.AppendAllText(resultPath, $"  Enabled: {renderer.enabled}\n");
                File.AppendAllText(resultPath, $"  Has Sprite: {renderer.sprite != null}\n");
                
                if (renderer.sprite != null)
                {
                    File.AppendAllText(resultPath, $"  Sprite Name: {renderer.sprite.name}\n");
                    File.AppendAllText(resultPath, $"  Sprite Bounds: {renderer.sprite.bounds}\n");
                    File.AppendAllText(resultPath, $"  Sprite Pivot: {renderer.sprite.pivot}\n");
                    File.AppendAllText(resultPath, $"  Pixels Per Unit: {renderer.sprite.pixelsPerUnit}\n");
                }
                
                File.AppendAllText(resultPath, $"  Local Position: {renderer.transform.localPosition}\n");
                File.AppendAllText(resultPath, $"  World Position: {renderer.transform.position}\n");
                File.AppendAllText(resultPath, $"  Sorting Order: {renderer.sortingOrder}\n");
                File.AppendAllText(resultPath, $"  Sorting Layer: {renderer.sortingLayerName}\n");
                File.AppendAllText(resultPath, $"  FlipX: {renderer.flipX}\n");
                File.AppendAllText(resultPath, $"  Color: {renderer.color}\n\n");
            }
            
            // Analyze body parts positioning
            File.AppendAllText(resultPath, "=== BODY PARTS POSITIONING ANALYSIS ===\n\n");
            
            var bodyRenderer = GetRenderer(renderers, "Body");
            var armRenderer = GetRenderer(renderers, "Arm");
            var headRenderer = GetRenderer(renderers, "Head");
            var faceRenderer = GetRenderer(renderers, "Face");
            
            if (bodyRenderer != null && headRenderer != null)
            {
                float headYOffset = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
                File.AppendAllText(resultPath, $"Head Y offset from Body: {headYOffset}\n");
                File.AppendAllText(resultPath, $"Expected: ~0.45 units above body\n");
                File.AppendAllText(resultPath, $"Status: {(headYOffset > 0.4f && headYOffset < 0.5f ? "CORRECT" : "INCORRECT")}\n\n");
            }
            
            if (bodyRenderer != null && armRenderer != null)
            {
                float armYOffset = armRenderer.transform.position.y - bodyRenderer.transform.position.y;
                File.AppendAllText(resultPath, $"Arm Y offset from Body: {armYOffset}\n");
                File.AppendAllText(resultPath, $"Expected: 0 (same level as body)\n");
                File.AppendAllText(resultPath, $"Status: {(Math.Abs(armYOffset) < 0.01f ? "CORRECT" : "INCORRECT - ARM IS MISALIGNED")}\n\n");
            }
            
            if (headRenderer != null && faceRenderer != null)
            {
                float faceXOffset = faceRenderer.transform.position.x - headRenderer.transform.position.x;
                float faceYOffset = faceRenderer.transform.position.y - headRenderer.transform.position.y;
                File.AppendAllText(resultPath, $"Face offset from Head: ({faceXOffset}, {faceYOffset})\n");
                File.AppendAllText(resultPath, $"Expected: (0, 0) - should be at same position as head\n");
                File.AppendAllText(resultPath, $"Status: {(Math.Abs(faceXOffset) < 0.01f && Math.Abs(faceYOffset) < 0.01f ? "CORRECT" : "INCORRECT - FACE IS MISALIGNED")}\n\n");
            }
            
            // Check for sprite origin issues
            File.AppendAllText(resultPath, "=== SPRITE ORIGIN ANALYSIS ===\n\n");
            
            if (bodyRenderer != null && bodyRenderer.sprite != null)
            {
                AnalyzeSpriteOrigin(resultPath, "Body", bodyRenderer);
            }
            
            if (armRenderer != null && armRenderer.sprite != null)
            {
                AnalyzeSpriteOrigin(resultPath, "Arm", armRenderer);
            }
            
            if (headRenderer != null && headRenderer.sprite != null)
            {
                AnalyzeSpriteOrigin(resultPath, "Head", headRenderer);
            }
            
            // Summary
            File.AppendAllText(resultPath, "\n=== SUMMARY OF ISSUES ===\n");
            
            if (armRenderer != null && bodyRenderer != null)
            {
                float armOffset = armRenderer.transform.position.y - bodyRenderer.transform.position.y;
                if (Math.Abs(armOffset) > 0.01f)
                {
                    File.AppendAllText(resultPath, "1. ARM MISALIGNMENT: Arm is not at the same Y level as body\n");
                    File.AppendAllText(resultPath, $"   - Current offset: {armOffset}\n");
                    File.AppendAllText(resultPath, "   - This causes body to appear at head level\n\n");
                }
            }
            
            if (faceRenderer != null && headRenderer != null)
            {
                float faceXOffset = faceRenderer.transform.position.x - headRenderer.transform.position.x;
                float faceYOffset = faceRenderer.transform.position.y - headRenderer.transform.position.y;
                if (Math.Abs(faceXOffset) > 0.01f || Math.Abs(faceYOffset) > 0.01f)
                {
                    File.AppendAllText(resultPath, "2. FACE MISALIGNMENT: Face is not properly aligned with head\n");
                    File.AppendAllText(resultPath, $"   - Current offset: ({faceXOffset}, {faceYOffset})\n\n");
                }
            }
            
            File.AppendAllText(resultPath, $"\nAnalysis completed at: {DateTime.Now}\n");
            Debug.Log($"Rendering analysis completed. Results saved to: {resultPath}");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"\n[ERROR] Exception: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static SpriteRenderer GetRenderer(SpriteRenderer[] renderers, string name)
    {
        foreach (var r in renderers)
        {
            if (r.name == name) return r;
        }
        return null;
    }
    
    private static void AnalyzeSpriteOrigin(string logPath, string partName, SpriteRenderer renderer)
    {
        var sprite = renderer.sprite;
        File.AppendAllText(logPath, $"[{partName}]\n");
        File.AppendAllText(logPath, $"  Sprite Bounds Center: {sprite.bounds.center}\n");
        File.AppendAllText(logPath, $"  Sprite Bounds Size: {sprite.bounds.size}\n");
        File.AppendAllText(logPath, $"  Pivot (normalized): {sprite.pivot / new Vector2(sprite.rect.width, sprite.rect.height)}\n");
        File.AppendAllText(logPath, $"  Pivot (pixels): {sprite.pivot}\n");
        
        // Check if pivot is at expected position
        var normalizedPivot = sprite.pivot / new Vector2(sprite.rect.width, sprite.rect.height);
        bool pivotCentered = Math.Abs(normalizedPivot.x - 0.5f) < 0.1f && Math.Abs(normalizedPivot.y - 0.5f) < 0.1f;
        
        File.AppendAllText(logPath, $"  Pivot Status: {(pivotCentered ? "Centered" : "Off-center - MAY CAUSE POSITIONING ISSUES")}\n\n");
    }
}