using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class TestMapleCharacterSprites
{
    private static string logPath = @"C:\Users\me\MapleUnity\character-sprite-test.log";
    
    [MenuItem("MapleUnity/Tests/Test MapleStory Character Sprites")]
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[CHARACTER_SPRITE_TEST] Starting at {DateTime.Now}\n");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile($"[CHARACTER_SPRITE_TEST] Created new scene");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            LogToFile("[CHARACTER_SPRITE_TEST] Initialized MapSceneGenerator");
            
            // Generate simple test map
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot == null)
            {
                LogToFile("[CHARACTER_SPRITE_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[CHARACTER_SPRITE_TEST] Generated map: {mapRoot.name}");
            
            // Clean up generator
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Force GameManager to initialize by calling its methods manually
            // In batch mode, Start() might not be called automatically
            var initMethod = typeof(GameManager).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(gameManager, null);
                LogToFile("[CHARACTER_SPRITE_TEST] Manually invoked GameManager.Start()");
            }
            
            // Small delay to ensure everything is initialized
            System.Threading.Thread.Sleep(100);
            
            // Find the player and ensure its components are initialized
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var simpleController = player.GetComponent<SimplePlayerController>();
                if (simpleController != null)
                {
                    // Force Awake to be called on SimplePlayerController
                    var awakeMethod = typeof(SimplePlayerController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (awakeMethod != null)
                    {
                        awakeMethod.Invoke(simpleController, null);
                        LogToFile("[CHARACTER_SPRITE_TEST] Manually invoked SimplePlayerController.Awake()");
                    }
                }
            }
            
            // Now check for character sprites
            CheckCharacterSprites();
        }
        catch (Exception e)
        {
            LogToFile($"[CHARACTER_SPRITE_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckCharacterSprites()
    {
        try
        {
            // Find player
            var player = GameObject.Find("Player");
            if (player == null)
            {
                LogToFile("[CHARACTER_SPRITE_TEST] ERROR: Player not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[CHARACTER_SPRITE_TEST] Found player at: {player.transform.position}");
            
            // Check for MapleCharacterRenderer
            var characterRenderer = player.GetComponentInChildren<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                LogToFile("[CHARACTER_SPRITE_TEST] ERROR: MapleCharacterRenderer not found!");
                
                // Check what components are on the player
                var components = player.GetComponents<Component>();
                LogToFile($"[CHARACTER_SPRITE_TEST] Player has {components.Length} components:");
                foreach (var comp in components)
                {
                    LogToFile($"  - {comp.GetType().Name}");
                }
                
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[CHARACTER_SPRITE_TEST] ✓ MapleCharacterRenderer found!");
            
            // Check if MapleCharacterRenderer is initialized
            var playerField = typeof(MapleCharacterRenderer).GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var characterDataField = typeof(MapleCharacterRenderer).GetField("characterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (playerField != null && characterDataField != null)
            {
                var playerValue = playerField.GetValue(characterRenderer);
                var characterDataValue = characterDataField.GetValue(characterRenderer);
                
                LogToFile($"[CHARACTER_SPRITE_TEST] MapleCharacterRenderer.player: {(playerValue != null ? "Set" : "NULL")}");
                LogToFile($"[CHARACTER_SPRITE_TEST] MapleCharacterRenderer.characterData: {(characterDataValue != null ? "Set" : "NULL")}");
                
                // If not initialized, try to initialize it manually
                if (playerValue == null || characterDataValue == null)
                {
                    var simpleController = player.GetComponent<SimplePlayerController>();
                    if (simpleController != null)
                    {
                        var gameManager = GameObject.Find("GameManager")?.GetComponent<GameManager>();
                        if (gameManager != null && gameManager.Player != null)
                        {
                            var characterDataProvider = new MapleClient.GameData.CharacterDataProvider();
                            characterRenderer.Initialize(gameManager.Player, characterDataProvider);
                            LogToFile("[CHARACTER_SPRITE_TEST] Manually initialized MapleCharacterRenderer");
                        }
                    }
                }
            }
            
            // Force the MapleCharacterRenderer to update sprites
            var updateMethod = typeof(MapleCharacterRenderer).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateMethod != null)
            {
                updateMethod.Invoke(characterRenderer, null);
                LogToFile("[CHARACTER_SPRITE_TEST] Manually invoked MapleCharacterRenderer.Update()");
            }
            
            // Check sprite renderers
            var spriteRenderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            LogToFile($"[CHARACTER_SPRITE_TEST] Found {spriteRenderers.Length} sprite renderer layers");
            
            int loadedSprites = 0;
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    LogToFile($"  ✓ Layer '{sr.name}' has sprite: {sr.sprite.name} ({sr.sprite.rect.width}x{sr.sprite.rect.height})");
                    LogToFile($"    - Enabled: {sr.enabled}, Active: {sr.gameObject.activeSelf}");
                    LogToFile($"    - Sorting: {sr.sortingLayerName} order {sr.sortingOrder}");
                    loadedSprites++;
                }
                else if (sr.enabled && sr.gameObject.activeSelf)
                {
                    LogToFile($"  ✗ Layer '{sr.name}' is enabled but has no sprite");
                }
            }
            
            LogToFile($"\n[CHARACTER_SPRITE_TEST] Summary:");
            LogToFile($"  - Total sprite layers: {spriteRenderers.Length}");
            LogToFile($"  - Loaded sprites: {loadedSprites}");
            
            if (loadedSprites > 0)
            {
                LogToFile($"[CHARACTER_SPRITE_TEST] ✓ SUCCESS! Character sprites are loading correctly!");
                
                // Check for blue rectangle
                var simpleController = player.GetComponent<SimplePlayerController>();
                if (simpleController != null)
                {
                    var mainSpriteRenderer = player.GetComponent<SpriteRenderer>();
                    if (mainSpriteRenderer != null && mainSpriteRenderer.enabled)
                    {
                        LogToFile("[CHARACTER_SPRITE_TEST] WARNING: Blue rectangle sprite renderer is still enabled!");
                    }
                    else
                    {
                        LogToFile("[CHARACTER_SPRITE_TEST] ✓ Blue rectangle sprite renderer is disabled");
                    }
                }
                
                EditorApplication.Exit(0);
            }
            else
            {
                LogToFile($"[CHARACTER_SPRITE_TEST] ✗ FAILED! No character sprites loaded!");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            LogToFile($"[CHARACTER_SPRITE_TEST] ERROR in CheckCharacterSprites: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}