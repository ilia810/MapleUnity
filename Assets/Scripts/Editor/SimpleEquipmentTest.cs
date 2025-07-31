using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using MapleClient.GameLogic.Data;

public static class SimpleEquipmentTest
{
    public static void RunSimpleTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "simple-equipment-test.log");
        
        try
        {
            File.WriteAllText(logPath, $"[SIMPLE_TEST] Starting at {DateTime.Now}\n");
            
            // Test 1: Direct asset loading
            File.AppendAllText(logPath, "\n=== Test 1: Direct Asset Loading ===\n");
            TestDirectAssetLoading(logPath);
            
            // Test 2: Character renderer
            File.AppendAllText(logPath, "\n=== Test 2: Character Renderer ===\n");
            TestCharacterRenderer(logPath);
            
            File.AppendAllText(logPath, $"\n[SIMPLE_TEST] Completed successfully at {DateTime.Now}\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\n[SIMPLE_TEST] ERROR: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestDirectAssetLoading(string logPath)
    {
        var loader = MapleClient.GameData.NXAssetLoader.Instance;
        
        // Test equipment items
        var testItems = new[]
        {
            new { id = 1002140, slot = "Cap", name = "Blue Bandana" },
            new { id = 1040002, slot = "Coat", name = "White Undershirt" },
            new { id = 1060002, slot = "Pants", name = "Blue Jean Shorts" },
            new { id = 1072001, slot = "Shoes", name = "Red Rubber Boots" },
            new { id = 1102054, slot = "Cape", name = "Adventurer Cape" },
            new { id = 1302000, slot = "Weapon", name = "Sword" }
        };
        
        int successCount = 0;
        foreach (var item in testItems)
        {
            var sprite = loader.LoadEquipment(item.id, item.slot, "stand1", 0);
            bool loaded = sprite != null;
            if (loaded) successCount++;
            
            File.AppendAllText(logPath, $"  {(loaded ? "✓" : "✗")} {item.name} ({item.id}): {(loaded ? "Loaded" : "Failed")}\n");
            
            if (loaded)
            {
                File.AppendAllText(logPath, $"    - Sprite: {sprite.name} ({sprite.rect.width}x{sprite.rect.height})\n");
            }
        }
        
        File.AppendAllText(logPath, $"\nDirect loading: {successCount}/{testItems.Length} items loaded\n");
    }
    
    private static void TestCharacterRenderer(string logPath)
    {
        // Create a new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create test character
        var testGO = new GameObject("TestCharacter");
        var characterRenderer = testGO.AddComponent<MapleCharacterRenderer>();
        
        // Create mock player
        var player = new MapleClient.GameLogic.Core.Player();
        
        // Equip items
        player.EquipItem(1002140, EquipSlot.Hat);
        player.EquipItem(1040002, EquipSlot.Top);
        player.EquipItem(1060002, EquipSlot.Bottom);
        player.EquipItem(1072001, EquipSlot.Shoes);
        player.EquipItem(1102054, EquipSlot.Cape);
        player.EquipItem(1302000, EquipSlot.Weapon);
        
        File.AppendAllText(logPath, "Equipped items on player\n");
        
        // Initialize renderer
        var characterData = new MapleClient.GameData.CharacterDataProvider();
        characterRenderer.Initialize(player, characterData);
        characterRenderer.UpdateAppearance();
        
        File.AppendAllText(logPath, "Initialized character renderer\n");
        
        // Check sprite renderers
        var spriteRenderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
        int equipmentSpritesFound = 0;
        
        File.AppendAllText(logPath, "\nSprite renderers:\n");
        foreach (var sr in spriteRenderers)
        {
            if (sr.sprite != null)
            {
                File.AppendAllText(logPath, $"  ✓ {sr.name}: {sr.sprite.name}\n");
                
                // Count equipment sprites
                if (sr.name == "Hat" || sr.name == "Coat" || sr.name == "Pants" || 
                    sr.name == "Shoes" || sr.name == "Cape" || sr.name == "Weapon")
                {
                    equipmentSpritesFound++;
                }
            }
            else if (sr.enabled)
            {
                File.AppendAllText(logPath, $"  ✗ {sr.name}: No sprite\n");
            }
        }
        
        File.AppendAllText(logPath, $"\nCharacter renderer: {equipmentSpritesFound} equipment sprites found\n");
        
        // Clean up
        GameObject.DestroyImmediate(testGO);
        
        if (equipmentSpritesFound > 0)
        {
            File.AppendAllText(logPath, "\n✓ SUCCESS: Equipment sprites are rendering!\n");
        }
        else
        {
            File.AppendAllText(logPath, "\n✗ FAILED: No equipment sprites found in renderer\n");
        }
    }
}