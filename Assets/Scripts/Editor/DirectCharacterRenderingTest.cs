using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class DirectCharacterRenderingTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\direct-character-test.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[DIRECT_TEST] Starting at {DateTime.Now}\n");
            Debug.Log("[DIRECT_TEST] Starting direct character rendering test...");
            
            // Create player GameObject
            GameObject player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0, 0, 0);
            LogToFile($"[DIRECT_TEST] Created player GameObject at {player.transform.position}");
            
            // Add MapleCharacterRenderer directly
            var rendererType = System.Type.GetType("MapleClient.GameView.MapleCharacterRenderer, Assembly-CSharp");
            if (rendererType != null)
            {
                var renderer = player.AddComponent(rendererType) as MonoBehaviour;
                LogToFile("[DIRECT_TEST] Added MapleCharacterRenderer component");
                
                // Force Awake
                var awakeMethod = rendererType.GetMethod("Awake", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null)
                {
                    awakeMethod.Invoke(renderer, null);
                    LogToFile("[DIRECT_TEST] Called Awake on MapleCharacterRenderer");
                }
                
                // Check sprite renderers after Awake
                var spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
                LogToFile($"[DIRECT_TEST] Sprite renderers after Awake: {spriteRenderers.Length}");
                
                foreach (var sr in spriteRenderers)
                {
                    LogToFile($"  - {sr.name}: Active={sr.gameObject.activeSelf}, Sprite={(sr.sprite != null ? sr.sprite.name : "NULL")}");
                }
                
                // Try to manually create body parts
                LogToFile("\n[DIRECT_TEST] Manually creating body parts...");
                
                string[] bodyParts = { "Body", "Head", "Face", "Hair", "Arm", "Hand", "Foot" };
                foreach (string part in bodyParts)
                {
                    GameObject partObj = new GameObject(part);
                    partObj.transform.SetParent(player.transform);
                    partObj.transform.localPosition = Vector3.zero;
                    
                    SpriteRenderer sr = partObj.AddComponent<SpriteRenderer>();
                    sr.sortingLayerName = "Character";
                    sr.sortingOrder = GetSortingOrder(part);
                    
                    LogToFile($"[DIRECT_TEST] Created {part} sprite renderer");
                }
                
                // Check again
                spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
                LogToFile($"\n[DIRECT_TEST] Sprite renderers after manual creation: {spriteRenderers.Length}");
                
                // Try to load character sprites using NXAssetLoader
                var assetLoaderType = System.Type.GetType("MapleClient.GameData.NXAssetLoader, Assembly-CSharp");
                if (assetLoaderType != null)
                {
                    var instanceProp = assetLoaderType.GetProperty("Instance");
                    if (instanceProp != null)
                    {
                        var loader = instanceProp.GetValue(null);
                        if (loader != null)
                        {
                            LogToFile("\n[DIRECT_TEST] Found NXAssetLoader instance");
                            
                            // Try to load a body sprite
                            var loadMethod = assetLoaderType.GetMethod("LoadCharacterSprite");
                            if (loadMethod != null)
                            {
                                try
                                {
                                    // LoadCharacterSprite(int characterId, string bodyPart, string state, int frame)
                                    var sprite = loadMethod.Invoke(loader, new object[] { 2000, "body", "stand1", 0 });
                                    LogToFile($"[DIRECT_TEST] LoadCharacterSprite returned: {(sprite != null ? "SUCCESS" : "NULL")}");
                                }
                                catch (Exception e)
                                {
                                    LogToFile($"[DIRECT_TEST] Error loading sprite: {e.Message}");
                                }
                            }
                        }
                    }
                }
                
                // Final analysis
                LogToFile("\n[DIRECT_TEST] === FINAL ANALYSIS ===");
                spriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
                LogToFile($"Total sprite renderers: {spriteRenderers.Length}");
                
                int visibleCount = 0;
                foreach (var sr in spriteRenderers)
                {
                    if (sr.sprite != null && sr.enabled && sr.gameObject.activeInHierarchy)
                    {
                        visibleCount++;
                    }
                    
                    LogToFile($"\n{sr.name}:");
                    LogToFile($"  - GameObject Active: {sr.gameObject.activeInHierarchy}");
                    LogToFile($"  - Renderer Enabled: {sr.enabled}");
                    LogToFile($"  - Has Sprite: {sr.sprite != null}");
                    LogToFile($"  - Position: {sr.transform.position}");
                    LogToFile($"  - Sorting Order: {sr.sortingOrder}");
                }
                
                LogToFile($"\nVisible sprite renderers: {visibleCount}");
                
                if (visibleCount == 0)
                {
                    LogToFile("\n[DIRECT_TEST] ISSUE: No visible sprites!");
                    LogToFile("Possible causes:");
                    LogToFile("1. Sprites not loading from NX files");
                    LogToFile("2. Character data not initialized");
                    LogToFile("3. Rendering components not properly set up");
                }
            }
            else
            {
                LogToFile("[DIRECT_TEST] ERROR: Could not find MapleCharacterRenderer type!");
            }
            
            // Clean up
            GameObject.DestroyImmediate(player);
            
            LogToFile("\n[DIRECT_TEST] Test completed successfully!");
            Debug.Log("[DIRECT_TEST] Test completed successfully!");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            LogToFile($"[DIRECT_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            Debug.LogError($"[DIRECT_TEST] ERROR: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static int GetSortingOrder(string bodyPart)
    {
        switch (bodyPart.ToLower())
        {
            case "body": return 10;
            case "head": return 20;
            case "face": return 21;
            case "hair": return 22;
            case "arm": return 5;
            case "hand": return 6;
            case "foot": return 1;
            default: return 0;
        }
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}