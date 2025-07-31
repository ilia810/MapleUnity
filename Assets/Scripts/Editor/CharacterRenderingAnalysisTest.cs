using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Reflection;
using System.Linq;

public static class CharacterRenderingAnalysisTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\character-rendering-analysis.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[ANALYSIS] Starting character rendering analysis at {DateTime.Now}\n");
            Debug.Log("[ANALYSIS] Starting character rendering analysis...");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity", OpenSceneMode.Single);
            LogToFile($"[ANALYSIS] Opened scene: {scene.name}");
            
            // List all root objects
            var rootObjects = scene.GetRootGameObjects();
            LogToFile($"[ANALYSIS] Root objects count: {rootObjects.Length}");
            foreach (var obj in rootObjects)
            {
                LogToFile($"  - {obj.name}");
            }
            
            // Try to find GameManager and use it to spawn a player
            var gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                LogToFile("\n[ANALYSIS] Found GameManager, analyzing components...");
                var components = gameManager.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        LogToFile($"  - Component: {comp.GetType().FullName}");
                    }
                }
                
                // Try to find and call initialization methods using reflection
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    
                    var type = comp.GetType();
                    
                    // Look for Start or Initialize methods
                    var startMethod = type.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var initMethod = type.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var awakeMethod = type.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    if (awakeMethod != null)
                    {
                        LogToFile($"  - Calling {type.Name}.Awake()");
                        try
                        {
                            awakeMethod.Invoke(comp, null);
                        }
                        catch (Exception e)
                        {
                            LogToFile($"    Error: {e.Message}");
                        }
                    }
                    
                    if (startMethod != null)
                    {
                        LogToFile($"  - Calling {type.Name}.Start()");
                        try
                        {
                            startMethod.Invoke(comp, null);
                        }
                        catch (Exception e)
                        {
                            LogToFile($"    Error: {e.Message}");
                        }
                    }
                    
                    if (initMethod != null)
                    {
                        LogToFile($"  - Calling {type.Name}.Initialize()");
                        try
                        {
                            initMethod.Invoke(comp, null);
                        }
                        catch (Exception e)
                        {
                            LogToFile($"    Error: {e.Message}");
                        }
                    }
                }
            }
            
            // Look for Player after initialization
            var player = GameObject.Find("Player");
            if (player == null)
            {
                // Try to find player in children
                player = GameObject.FindObjectsOfType<GameObject>()
                    .FirstOrDefault(go => go.name.Contains("Player") || go.name.Contains("player"));
            }
            
            if (player != null)
            {
                LogToFile($"\n[ANALYSIS] Found player: {player.name} at {player.transform.position}");
                AnalyzeGameObject(player, 0);
            }
            else
            {
                LogToFile("\n[ANALYSIS] No player found. Trying to create one...");
                
                // Try to create a player using MapSceneGenerator if available
                var generatorType = Type.GetType("MapleClient.SceneGeneration.MapSceneGenerator, Assembly-CSharp");
                if (generatorType != null)
                {
                    LogToFile("[ANALYSIS] Found MapSceneGenerator type");
                    
                    // Create generator
                    var generatorObj = new GameObject("TempGenerator");
                    var generator = generatorObj.AddComponent(generatorType) as MonoBehaviour;
                    
                    // Try to spawn player
                    var spawnMethod = generatorType.GetMethod("SpawnPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (spawnMethod != null)
                    {
                        LogToFile("[ANALYSIS] Calling SpawnPlayer method...");
                        try
                        {
                            spawnMethod.Invoke(generator, null);
                            
                            // Look for player again
                            player = GameObject.Find("Player");
                            if (player != null)
                            {
                                LogToFile($"[ANALYSIS] Player spawned at: {player.transform.position}");
                                AnalyzeGameObject(player, 0);
                            }
                        }
                        catch (Exception e)
                        {
                            LogToFile($"[ANALYSIS] Error spawning player: {e.Message}");
                        }
                    }
                    
                    GameObject.DestroyImmediate(generatorObj);
                }
            }
            
            // Analyze all sprite renderers in the scene
            LogToFile("\n[ANALYSIS] === ALL SPRITE RENDERERS IN SCENE ===");
            var allRenderers = GameObject.FindObjectsOfType<SpriteRenderer>();
            LogToFile($"Total sprite renderers: {allRenderers.Length}");
            
            var characterRenderers = allRenderers.Where(sr => 
                sr.name.Contains("Player") || 
                sr.name.Contains("Character") ||
                sr.name.Contains("Head") ||
                sr.name.Contains("Body") ||
                sr.name.Contains("Face") ||
                sr.name.Contains("Hair") ||
                sr.name.Contains("Arm") ||
                sr.name.Contains("Hand"))
                .ToArray();
                
            if (characterRenderers.Length > 0)
            {
                LogToFile($"\nFound {characterRenderers.Length} character-related sprite renderers:");
                foreach (var sr in characterRenderers)
                {
                    LogToFile($"\n  Renderer: {sr.name}");
                    LogToFile($"    - GameObject path: {GetGameObjectPath(sr.gameObject)}");
                    LogToFile($"    - Active: {sr.gameObject.activeInHierarchy}");
                    LogToFile($"    - Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
                    LogToFile($"    - Position: {sr.transform.position}");
                    LogToFile($"    - Sorting order: {sr.sortingOrder}");
                    LogToFile($"    - Color: {sr.color}");
                }
            }
            
            LogToFile("\n[ANALYSIS] Test completed successfully!");
            Debug.Log("[ANALYSIS] Test completed successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[ANALYSIS] ERROR: {e.Message}\n{e.StackTrace}");
            Debug.LogError($"[ANALYSIS] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeGameObject(GameObject obj, int depth)
    {
        string indent = new string(' ', depth * 2);
        LogToFile($"{indent}GameObject: {obj.name}");
        LogToFile($"{indent}  - Active: {obj.activeInHierarchy}");
        LogToFile($"{indent}  - Position: {obj.transform.position}");
        LogToFile($"{indent}  - LocalPosition: {obj.transform.localPosition}");
        
        // Check for sprite renderer
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            LogToFile($"{indent}  - SpriteRenderer:");
            LogToFile($"{indent}    - Enabled: {sr.enabled}");
            LogToFile($"{indent}    - Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
            LogToFile($"{indent}    - Color: {sr.color}");
            LogToFile($"{indent}    - Sorting Layer: {sr.sortingLayerName}");
            LogToFile($"{indent}    - Sorting Order: {sr.sortingOrder}");
            LogToFile($"{indent}    - FlipX: {sr.flipX}");
        }
        
        // Check for other relevant components
        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != null)
            {
                var typeName = comp.GetType().Name;
                if (typeName.Contains("Character") || typeName.Contains("Player") || typeName.Contains("Renderer"))
                {
                    LogToFile($"{indent}  - Component: {comp.GetType().FullName}");
                }
            }
        }
        
        // Analyze children
        foreach (Transform child in obj.transform)
        {
            AnalyzeGameObject(child.gameObject, depth + 1);
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
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}