using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Data;

public static class TestCharacterWithEquipment
{
    private static string logPath = @"C:\Users\me\MapleUnity\character-equipment-test.log";
    
    [MenuItem("MapleUnity/Tests/Test Character With Equipment")]
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, $"[EQUIPMENT_TEST] Starting at {DateTime.Now}\n");
            
            // Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            LogToFile($"[EQUIPMENT_TEST] Created new scene");
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            LogToFile("[EQUIPMENT_TEST] Initialized MapSceneGenerator");
            
            // Generate simple map
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            
            if (mapRoot == null)
            {
                LogToFile("[EQUIPMENT_TEST] ERROR: Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile($"[EQUIPMENT_TEST] Generated map: {mapRoot.name}");
            
            // Clean up generator
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Wait for initialization
            EditorApplication.delayCall += () =>
            {
                EquipTestItems();
            };
        }
        catch (Exception e)
        {
            LogToFile($"[EQUIPMENT_TEST] ERROR: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void EquipTestItems()
    {
        try
        {
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                LogToFile("[EQUIPMENT_TEST] ERROR: Player GameObject not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            var playerView = playerGO.GetComponent<PlayerView>();
            if (playerView == null)
            {
                LogToFile("[EQUIPMENT_TEST] ERROR: PlayerView not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get the game logic player through reflection (since Player property is internal)
            var playerField = playerView.GetType().GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var player = playerField?.GetValue(playerView) as MapleClient.GameLogic.Core.Player;
            
            if (player == null)
            {
                LogToFile("[EQUIPMENT_TEST] ERROR: Could not access Player from PlayerView!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("[EQUIPMENT_TEST] Found player, equipping test items...");
            
            // Equip some basic items
            player.EquipItem(1002140, EquipSlot.Hat);     // Blue Bandana
            player.EquipItem(1040002, EquipSlot.Top);     // White Undershirt
            player.EquipItem(1060002, EquipSlot.Bottom);  // Blue Jean Shorts
            player.EquipItem(1072001, EquipSlot.Shoes);   // Red Rubber Boots
            player.EquipItem(1102054, EquipSlot.Cape);    // Adventurer Cape
            player.EquipItem(1302000, EquipSlot.Weapon);  // Sword
            
            LogToFile("[EQUIPMENT_TEST] Equipped test items:");
            LogToFile("  - Hat: 1002140 (Blue Bandana)");
            LogToFile("  - Top: 1040002 (White Undershirt)");
            LogToFile("  - Bottom: 1060002 (Blue Jean Shorts)");
            LogToFile("  - Shoes: 1072001 (Red Rubber Boots)");
            LogToFile("  - Cape: 1102054 (Adventurer Cape)");
            LogToFile("  - Weapon: 1302000 (Sword)");
            
            // Force sprite update
            var characterRenderer = playerGO.GetComponentInChildren<MapleCharacterRenderer>();
            if (characterRenderer != null)
            {
                characterRenderer.UpdateAppearance();
                LogToFile("[EQUIPMENT_TEST] Forced character appearance update");
            }
            
            // Wait a moment then check results
            EditorApplication.delayCall += () =>
            {
                CheckEquipmentSprites();
            };
        }
        catch (Exception e)
        {
            LogToFile($"[EQUIPMENT_TEST] ERROR in EquipTestItems: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckEquipmentSprites()
    {
        try
        {
            var playerGO = GameObject.Find("Player");
            var characterRenderer = playerGO?.GetComponentInChildren<MapleCharacterRenderer>();
            
            if (characterRenderer == null)
            {
                LogToFile("[EQUIPMENT_TEST] ERROR: MapleCharacterRenderer not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            LogToFile("\n[EQUIPMENT_TEST] Checking sprite renderers:");
            var spriteRenderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            
            int equipmentWithSprites = 0;
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    LogToFile($"  ✓ {sr.name}: {sr.sprite.name} ({sr.sprite.rect.width}x{sr.sprite.rect.height})");
                    
                    // Count equipment sprites
                    if (sr.name == "Hat" || sr.name == "Top" || sr.name == "Bottom" || 
                        sr.name == "Shoes" || sr.name == "Cape" || sr.name == "Weapon")
                    {
                        equipmentWithSprites++;
                    }
                }
                else if (sr.enabled && sr.name != "Player")
                {
                    LogToFile($"  ✗ {sr.name}: No sprite");
                }
            }
            
            LogToFile($"\n[EQUIPMENT_TEST] Summary:");
            LogToFile($"  - Equipment slots with sprites: {equipmentWithSprites}/6");
            
            if (equipmentWithSprites > 0)
            {
                LogToFile($"\n[EQUIPMENT_TEST] ✓ SUCCESS! Equipment sprites are loading!");
                EditorApplication.Exit(0);
            }
            else
            {
                LogToFile($"\n[EQUIPMENT_TEST] ✗ FAILED! No equipment sprites loaded!");
                
                // Try to debug the issue
                LogToFile("\n[EQUIPMENT_TEST] Debugging equipment loading...");
                TestDirectEquipmentLoading();
            }
        }
        catch (Exception e)
        {
            LogToFile($"[EQUIPMENT_TEST] ERROR in CheckEquipmentSprites: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestDirectEquipmentLoading()
    {
        var loader = MapleClient.GameData.NXAssetLoader.Instance;
        
        // Test loading hat directly
        LogToFile("\nTesting direct equipment loading:");
        var hatSprite = loader.LoadEquipment(1002140, "Cap", "stand1", 0);
        LogToFile($"  Hat sprite: {(hatSprite != null ? "Loaded" : "Failed")}");
        
        var weaponSprite = loader.LoadEquipment(1302000, "Weapon", "stand1", 0);
        LogToFile($"  Weapon sprite: {(weaponSprite != null ? "Loaded" : "Failed")}");
        
        EditorApplication.Exit(1);
    }
    
    private static void LogToFile(string message)
    {
        File.AppendAllText(logPath, message + "\n");
        Debug.Log(message);
    }
}