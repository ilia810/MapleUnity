using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;

public static class DirectCharacterTest
{
    private static string resultPath = @"C:\Users\me\MapleUnity\character-test-results.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(resultPath, "[TEST] Starting character rendering verification\n");
            File.AppendAllText(resultPath, $"[TEST] Time: {DateTime.Now}\n\n");
            
            // Create test scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            File.AppendAllText(resultPath, "[TEST] Created new scene\n");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            var generatorType = System.Type.GetType("MapleClient.SceneGeneration.MapSceneGenerator, Assembly-CSharp");
            if (generatorType == null)
            {
                File.AppendAllText(resultPath, "[ERROR] MapSceneGenerator type not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            var generator = generatorObj.AddComponent(generatorType) as MonoBehaviour;
            File.AppendAllText(resultPath, "[TEST] Created MapSceneGenerator\n");
            
            // Initialize generators
            var initMethod = generatorType.GetMethod("InitializeGenerators");
            initMethod.Invoke(generator, null);
            File.AppendAllText(resultPath, "[TEST] Initialized generators\n");
            
            // Generate map
            var generateMethod = generatorType.GetMethod("GenerateMapScene");
            var mapRoot = generateMethod.Invoke(generator, new object[] { 100000000 }) as GameObject;
            
            if (mapRoot == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Failed to generate map\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "[TEST] Generated map successfully\n");
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            var gmType = System.Type.GetType("MapleClient.GameView.GameManager, Assembly-CSharp");
            if (gmType != null)
            {
                gameManagerObj.AddComponent(gmType);
                File.AppendAllText(resultPath, "[TEST] Created GameManager\n");
            }
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Player not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, $"[TEST] Player found at: {playerGO.transform.position}\n");
            
            // Get character renderer
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType == null)
            {
                File.AppendAllText(resultPath, "[ERROR] MapleCharacterRenderer type not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            var characterRenderer = playerGO.GetComponentInChildren(rendererType) as MonoBehaviour;
            if (characterRenderer == null)
            {
                File.AppendAllText(resultPath, "[ERROR] MapleCharacterRenderer component not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "[TEST] Found MapleCharacterRenderer\n\n");
            
            // Run checks
            CheckHeadPosition(characterRenderer);
            CheckFacingDirection(characterRenderer);
            CheckFaceFeatures(characterRenderer);
            
            File.AppendAllText(resultPath, "\n[TEST] === SUMMARY ===\n");
            File.AppendAllText(resultPath, "[TEST] All checks completed. See results above.\n");
            File.AppendAllText(resultPath, $"[TEST] Test finished at: {DateTime.Now}\n");
            
            Debug.Log($"Test results written to: {resultPath}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"\n[ERROR] Exception: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckHeadPosition(MonoBehaviour renderer)
    {
        File.AppendAllText(resultPath, "\n=== HEAD POSITION CHECK ===\n");
        
        var renderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer headRenderer = null;
        SpriteRenderer bodyRenderer = null;
        
        foreach (var sr in renderers)
        {
            if (sr.name == "Head") headRenderer = sr;
            if (sr.name == "Body") bodyRenderer = sr;
        }
        
        if (headRenderer != null && bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body position: {bodyRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head position: {headRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head local position: {headRenderer.transform.localPosition}\n");
            
            float yDiff = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
            File.AppendAllText(resultPath, $"Head Y offset from body: {yDiff}\n");
            
            if (yDiff > 0)
            {
                File.AppendAllText(resultPath, "[PASS] Head is ABOVE body (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Head is BELOW body (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, $"[FAIL] Could not find renderers - Head: {headRenderer != null}, Body: {bodyRenderer != null}\n");
        }
    }
    
    private static void CheckFacingDirection(MonoBehaviour renderer)
    {
        File.AppendAllText(resultPath, "\n=== FACING DIRECTION CHECK ===\n");
        
        var renderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer bodyRenderer = null;
        
        foreach (var sr in renderers)
        {
            if (sr.name == "Body") 
            {
                bodyRenderer = sr;
                break;
            }
        }
        
        if (bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body flipX: {bodyRenderer.flipX}\n");
            File.AppendAllText(resultPath, "Expected: false (facing right by default)\n");
            
            if (!bodyRenderer.flipX)
            {
                File.AppendAllText(resultPath, "[PASS] Character facing right by default (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Character facing left by default (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Body renderer not found\n");
        }
    }
    
    private static void CheckFaceFeatures(MonoBehaviour renderer)
    {
        File.AppendAllText(resultPath, "\n=== FACE FEATURES CHECK ===\n");
        
        var renderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer faceRenderer = null;
        
        foreach (var sr in renderers)
        {
            if (sr.name == "Face")
            {
                faceRenderer = sr;
                break;
            }
        }
        
        if (faceRenderer != null)
        {
            File.AppendAllText(resultPath, "Face renderer found\n");
            File.AppendAllText(resultPath, $"Face sprite: {(faceRenderer.sprite != null ? faceRenderer.sprite.name : "NULL")}\n");
            File.AppendAllText(resultPath, $"Face enabled: {faceRenderer.enabled}\n");
            File.AppendAllText(resultPath, $"Face position: {faceRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Face sorting order: {faceRenderer.sortingOrder}\n");
            
            if (faceRenderer.sprite != null && faceRenderer.enabled)
            {
                File.AppendAllText(resultPath, "[PASS] Face has sprite and is enabled (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Face missing sprite or disabled (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Face renderer not found\n");
        }
    }
}