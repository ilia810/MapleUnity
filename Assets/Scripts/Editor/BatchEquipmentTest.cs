using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public static class BatchEquipmentTest
{
    public static void RunEquipmentTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "equipment-batch-test.log");
        
        try
        {
            File.WriteAllText(logPath, $"[BATCH_TEST] Starting equipment test at {DateTime.Now}\n");
            
            // Since we're in batch mode, we'll test the asset loading directly
            var loader = MapleClient.GameData.NXAssetLoader.Instance;
            
            File.AppendAllText(logPath, "\n[BATCH_TEST] Testing equipment sprite loading:\n");
            
            // Test loading various equipment sprites
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
            
            File.AppendAllText(logPath, $"\n[BATCH_TEST] Summary: {successCount}/{testItems.Length} equipment sprites loaded successfully\n");
            
            // Test the complete character rendering system
            File.AppendAllText(logPath, "\n[BATCH_TEST] Testing character renderer initialization:\n");
            
            // Create a test GameObject with character renderer
            var testGO = new GameObject("TestCharacter");
            var characterRenderer = testGO.AddComponent<MapleClient.GameView.MapleCharacterRenderer>();
            
            // Create a mock player and character data provider
            var player = new MapleClient.GameLogic.Core.Player();
            
            // Equip test items
            player.EquipItem(1002140, MapleClient.GameLogic.Data.EquipSlot.Hat);
            player.EquipItem(1040002, MapleClient.GameLogic.Data.EquipSlot.Top);
            player.EquipItem(1060002, MapleClient.GameLogic.Data.EquipSlot.Bottom);
            player.EquipItem(1072001, MapleClient.GameLogic.Data.EquipSlot.Shoes);
            player.EquipItem(1102054, MapleClient.GameLogic.Data.EquipSlot.Cape);
            player.EquipItem(1302000, MapleClient.GameLogic.Data.EquipSlot.Weapon);
            
            var characterData = new MapleClient.GameData.CharacterDataProvider();
            characterRenderer.Initialize(player, characterData);
            characterRenderer.UpdateAppearance();
            
            // Check sprite renderers
            var spriteRenderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            int equipmentSpritesFound = 0;
            
            File.AppendAllText(logPath, "  Checking sprite renderers after initialization:\n");
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null && (sr.name == "Hat" || sr.name == "Coat" || sr.name == "Pants" || 
                    sr.name == "Shoes" || sr.name == "Cape" || sr.name == "Weapon"))
                {
                    equipmentSpritesFound++;
                    File.AppendAllText(logPath, $"    ✓ {sr.name}: {sr.sprite.name}\n");
                }
            }
            
            File.AppendAllText(logPath, $"\n[BATCH_TEST] Equipment sprites in renderer: {equipmentSpritesFound}\n");
            
            // Clean up
            GameObject.DestroyImmediate(testGO);
            
            File.AppendAllText(logPath, $"\n[BATCH_TEST] Test completed successfully at {DateTime.Now}\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\n[BATCH_TEST] ERROR: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
}