using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;

public static class SimpleRenderingTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\character-rendering-test.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[TEST] Starting character rendering analysis at {DateTime.Now}\n");
            Debug.Log("[TEST] Starting character rendering analysis...");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile("[TEST] Created new scene");
            
            // Try to find and load the MapSceneGenerator
            var generatorType = System.Type.GetType("MapleClient.SceneGeneration.MapSceneGenerator, Assembly-CSharp");
            if (generatorType == null)
            {
                LogToFile("[TEST] ERROR: MapSceneGenerator type not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Create generator GameObject
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            var generator = generatorObj.AddComponent(generatorType) as MonoBehaviour;
            LogToFile("[TEST] Created MapSceneGenerator");
            
            // Initialize generators using reflection
            var initMethod = generatorType.GetMethod("InitializeGenerators");
            if (initMethod != null)
            {
                initMethod.Invoke(generator, null);
                LogToFile("[TEST] Initialized generators");
            }
            
            // Generate map using reflection
            var generateMethod = generatorType.GetMethod("GenerateMapScene");
            if (generateMethod != null)
            {
                GameObject mapRoot = generateMethod.Invoke(generator, new object[] { 100000000 }) as GameObject;
                if (mapRoot == null)
                {
                    LogToFile("[TEST] ERROR: Failed to generate map!");
                    EditorApplication.Exit(1);
                    return;
                }
                LogToFile("[TEST] Generated map successfully");
            }
            
            // Clean up generator
            GameObject.DestroyImmediate(generatorObj);
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                LogToFile("[TEST] ERROR: Player not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[TEST] Player found at: {playerGO.transform.position}");
            
            // Analyze character rendering
            AnalyzeCharacterRendering(playerGO);
            
            LogToFile("\n[TEST] Test completed successfully!");
            Debug.Log("[TEST] Test completed successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[TEST] ERROR: {e.Message}\n{e.StackTrace}");
            Debug.LogError($"[TEST] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacterRendering(GameObject playerGO)
    {
        LogToFile("\n[TEST] === CHARACTER RENDERING ANALYSIS ===");
        
        // Find all sprite renderers
        var spriteRenderers = playerGO.GetComponentsInChildren<SpriteRenderer>();
        LogToFile($"Total sprite renderers: {spriteRenderers.Length}");
        
        foreach (var sr in spriteRenderers)
        {
            if (sr.name == "Player") continue;
            
            LogToFile($"\nRenderer: {sr.name}");
            LogToFile($"  - Active: {sr.gameObject.activeSelf}, Enabled: {sr.enabled}");
            LogToFile($"  - Local pos: {sr.transform.localPosition}");
            LogToFile($"  - World pos: {sr.transform.position}");
            LogToFile($"  - Sorting layer: {sr.sortingLayerName}");
            LogToFile($"  - Sorting order: {sr.sortingOrder}");
            LogToFile($"  - Flip X: {sr.flipX}");
            LogToFile($"  - Color: {sr.color}");
            
            if (sr.sprite != null)
            {
                LogToFile($"  - Sprite: {sr.sprite.name}");
                LogToFile($"  - Sprite bounds: {sr.sprite.bounds}");
                LogToFile($"  - Sprite pivot: {sr.sprite.pivot}");
                LogToFile($"  - Pixels per unit: {sr.sprite.pixelsPerUnit}");
            }
            else
            {
                LogToFile($"  - Sprite: NULL");
            }
            
            // Check for common issues
            if (sr.color.a == 0)
            {
                LogToFile($"  - WARNING: Alpha is 0 (invisible)");
            }
            
            if (!sr.gameObject.activeInHierarchy)
            {
                LogToFile($"  - WARNING: GameObject is not active in hierarchy");
            }
        }
        
        // Look for character renderer component using reflection
        var components = playerGO.GetComponentsInChildren<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            
            var typeName = comp.GetType().Name;
            if (typeName.Contains("Character") || typeName.Contains("Renderer"))
            {
                LogToFile($"\nFound component: {typeName}");
                
                // Try to find sprite-related fields
                var fields = comp.GetType().GetFields(System.Reflection.BindingFlags.Instance | 
                                                     System.Reflection.BindingFlags.NonPublic | 
                                                     System.Reflection.BindingFlags.Public);
                
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(SpriteRenderer) || 
                        field.FieldType == typeof(Sprite) ||
                        field.Name.ToLower().Contains("sprite") ||
                        field.Name.ToLower().Contains("renderer"))
                    {
                        var value = field.GetValue(comp);
                        LogToFile($"  - Field {field.Name}: {value}");
                    }
                }
            }
        }
        
        // Check for animation issues
        var animator = playerGO.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            LogToFile($"\nAnimator found:");
            LogToFile($"  - Enabled: {animator.enabled}");
            LogToFile($"  - Has controller: {animator.runtimeAnimatorController != null}");
            if (animator.runtimeAnimatorController != null)
            {
                LogToFile($"  - Controller: {animator.runtimeAnimatorController.name}");
            }
        }
        
        // Suggest fixes
        LogToFile("\n[TEST] === SUGGESTED FIXES ===");
        
        bool hasVisibleSprites = false;
        foreach (var sr in spriteRenderers)
        {
            if (sr.sprite != null && sr.enabled && sr.gameObject.activeInHierarchy && sr.color.a > 0)
            {
                hasVisibleSprites = true;
                break;
            }
        }
        
        if (!hasVisibleSprites)
        {
            LogToFile("- No visible sprites found! Check:");
            LogToFile("  1. Sprites are assigned to SpriteRenderers");
            LogToFile("  2. GameObjects are active");
            LogToFile("  3. SpriteRenderers are enabled");
            LogToFile("  4. Alpha/transparency is not 0");
            LogToFile("  5. Sprites are in the correct sorting layer");
        }
        
        // Check for overlapping sprites
        var headRenderer = FindRenderer(spriteRenderers, "Head");
        var bodyRenderer = FindRenderer(spriteRenderers, "Body");
        
        if (headRenderer != null && bodyRenderer != null)
        {
            var headY = headRenderer.transform.position.y;
            var bodyY = bodyRenderer.transform.position.y;
            var distance = headY - bodyY;
            
            LogToFile($"\nHead-Body vertical distance: {distance}");
            if (Math.Abs(distance) < 0.1f)
            {
                LogToFile("- WARNING: Head and body might be overlapping!");
                LogToFile("  Suggested fix: Offset head Y position by ~0.45 units");
            }
        }
    }
    
    private static SpriteRenderer FindRenderer(SpriteRenderer[] renderers, string name)
    {
        foreach (var r in renderers)
        {
            if (r.name.Contains(name)) return r;
        }
        return null;
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}