using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Reflection;

public static class CharacterRenderingTest
{
    public static void RunCharacterTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\character-rendering-test.log";
        
        try
        {
            File.WriteAllText(logPath, "[CHAR_TEST] Character rendering test started\n");
            Debug.Log("[CHAR_TEST] Starting character rendering analysis...");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity", OpenSceneMode.Single);
            File.AppendAllText(logPath, $"[CHAR_TEST] Scene opened: {scene.name}\n");
            
            // Try to initialize GameManager
            var gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                File.AppendAllText(logPath, "[CHAR_TEST] Found GameManager\n");
                
                // Get all components
                var components = gameManager.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        File.AppendAllText(logPath, $"[CHAR_TEST] GameManager component: {comp.GetType().FullName}\n");
                        
                        // Try to call Start method using reflection
                        var startMethod = comp.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (startMethod != null)
                        {
                            try
                            {
                                startMethod.Invoke(comp, null);
                                File.AppendAllText(logPath, $"[CHAR_TEST] Called Start on {comp.GetType().Name}\n");
                            }
                            catch (System.Exception e)
                            {
                                File.AppendAllText(logPath, $"[CHAR_TEST] Error calling Start: {e.Message}\n");
                            }
                        }
                    }
                }
            }
            
            // Look for all sprite renderers in the scene
            var allSpriteRenderers = GameObject.FindObjectsOfType<SpriteRenderer>();
            File.AppendAllText(logPath, $"\n[CHAR_TEST] Total sprite renderers in scene: {allSpriteRenderers.Length}\n");
            
            // Filter for character-related renderers
            var characterRenderers = allSpriteRenderers.Where(sr => 
                sr.name.Contains("Player") || 
                sr.name.Contains("Character") ||
                sr.name.Contains("Head") ||
                sr.name.Contains("Body") ||
                sr.name.Contains("Face") ||
                sr.name.Contains("Hair") ||
                sr.name.Contains("Arm") ||
                sr.name.Contains("Hand") ||
                sr.transform.parent != null && (
                    sr.transform.parent.name.Contains("Player") ||
                    sr.transform.parent.name.Contains("Character")
                ))
                .ToArray();
            
            File.AppendAllText(logPath, $"[CHAR_TEST] Character-related sprite renderers: {characterRenderers.Length}\n");
            
            if (characterRenderers.Length > 0)
            {
                File.AppendAllText(logPath, "\n[CHAR_TEST] === CHARACTER SPRITE ANALYSIS ===\n");
                
                foreach (var sr in characterRenderers)
                {
                    File.AppendAllText(logPath, $"\nSprite Renderer: {sr.name}\n");
                    File.AppendAllText(logPath, $"  Path: {GetGameObjectPath(sr.gameObject)}\n");
                    File.AppendAllText(logPath, $"  Active: {sr.gameObject.activeInHierarchy}\n");
                    File.AppendAllText(logPath, $"  Enabled: {sr.enabled}\n");
                    File.AppendAllText(logPath, $"  Position: {sr.transform.position}\n");
                    File.AppendAllText(logPath, $"  Local Position: {sr.transform.localPosition}\n");
                    File.AppendAllText(logPath, $"  Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}\n");
                    File.AppendAllText(logPath, $"  Color: {sr.color}\n");
                    File.AppendAllText(logPath, $"  Sorting Layer: {sr.sortingLayerName}\n");
                    File.AppendAllText(logPath, $"  Sorting Order: {sr.sortingOrder}\n");
                    File.AppendAllText(logPath, $"  Flip X: {sr.flipX}\n");
                    
                    if (sr.sprite != null)
                    {
                        File.AppendAllText(logPath, $"  Sprite Bounds: {sr.sprite.bounds}\n");
                        File.AppendAllText(logPath, $"  Sprite Pivot: {sr.sprite.pivot}\n");
                        File.AppendAllText(logPath, $"  Pixels Per Unit: {sr.sprite.pixelsPerUnit}\n");
                    }
                    
                    // Check for common issues
                    if (sr.color.a == 0)
                    {
                        File.AppendAllText(logPath, "  WARNING: Alpha is 0 (invisible)\n");
                    }
                    
                    if (sr.sprite == null)
                    {
                        File.AppendAllText(logPath, "  WARNING: No sprite assigned\n");
                    }
                }
                
                // Look for overlapping sprites
                File.AppendAllText(logPath, "\n[CHAR_TEST] === CHECKING FOR OVERLAPPING SPRITES ===\n");
                
                var headRenderers = characterRenderers.Where(sr => sr.name.Contains("Head")).ToArray();
                var bodyRenderers = characterRenderers.Where(sr => sr.name.Contains("Body")).ToArray();
                
                if (headRenderers.Length > 0 && bodyRenderers.Length > 0)
                {
                    foreach (var head in headRenderers)
                    {
                        foreach (var body in bodyRenderers)
                        {
                            var distance = Vector3.Distance(head.transform.position, body.transform.position);
                            File.AppendAllText(logPath, $"Distance between {head.name} and {body.name}: {distance}\n");
                            
                            if (distance < 0.1f)
                            {
                                File.AppendAllText(logPath, "  WARNING: Head and body might be overlapping!\n");
                                File.AppendAllText(logPath, "  Suggested fix: Set head Y offset to ~0.45 units\n");
                            }
                        }
                    }
                }
            }
            else
            {
                File.AppendAllText(logPath, "\n[CHAR_TEST] No character sprite renderers found.\n");
                
                // Try to create a player using reflection
                File.AppendAllText(logPath, "[CHAR_TEST] Attempting to spawn player...\n");
                
                var generatorType = System.Type.GetType("MapleClient.SceneGeneration.MapSceneGenerator, Assembly-CSharp");
                if (generatorType != null)
                {
                    var generatorObj = new GameObject("TempGenerator");
                    var generator = generatorObj.AddComponent(generatorType);
                    
                    // Initialize
                    var initMethod = generatorType.GetMethod("InitializeGenerators", BindingFlags.Public | BindingFlags.Instance);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(generator, null);
                        File.AppendAllText(logPath, "[CHAR_TEST] Initialized generator\n");
                    }
                    
                    // Try to generate map with player
                    var generateMethod = generatorType.GetMethod("GenerateMapScene", BindingFlags.Public | BindingFlags.Instance);
                    if (generateMethod != null)
                    {
                        var mapRoot = generateMethod.Invoke(generator, new object[] { 100000000 });
                        File.AppendAllText(logPath, "[CHAR_TEST] Generated map scene\n");
                    }
                    
                    GameObject.DestroyImmediate(generatorObj);
                    
                    // Check again for character renderers
                    allSpriteRenderers = GameObject.FindObjectsOfType<SpriteRenderer>();
                    File.AppendAllText(logPath, $"[CHAR_TEST] Sprite renderers after generation: {allSpriteRenderers.Length}\n");
                }
            }
            
            File.AppendAllText(logPath, "\n[CHAR_TEST] Test completed successfully\n");
            Debug.Log("[CHAR_TEST] Test completed successfully");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"[CHAR_TEST] ERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"[CHAR_TEST] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}