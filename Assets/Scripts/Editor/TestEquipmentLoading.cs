using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System.Collections.Generic;

public class TestEquipmentLoading : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Test Equipment Loading")]
    public static void RunTest()
    {
        Debug.Log("[TEST_EQUIPMENT] Starting equipment loading test...");
        
        var loader = NXAssetLoader.Instance;
        
        // Test common equipment IDs
        TestHatLoading(loader);
        TestWeaponLoading(loader);
        TestCapeLoading(loader);
        
        Debug.Log("[TEST_EQUIPMENT] Test complete!");
    }
    
    private static void TestHatLoading(NXAssetLoader loader)
    {
        Debug.Log("\n=== Testing Hat Loading ===");
        
        // Common hat IDs
        int[] testHatIds = { 1002001, 1002005, 1002140 }; // Basic hats
        
        foreach (int hatId in testHatIds)
        {
            Debug.Log($"\nTesting hat {hatId}:");
            
            // Try loading for stand animation
            var hatSprite = loader.LoadEquipment(hatId, "Cap", "stand1", 0);
            if (hatSprite != null)
            {
                Debug.Log($"  ✓ Loaded: {hatSprite.name} ({hatSprite.rect.width}x{hatSprite.rect.height})");
            }
            else
            {
                Debug.Log($"  ✗ Failed to load hat {hatId}");
            }
        }
    }
    
    private static void TestWeaponLoading(NXAssetLoader loader)
    {
        Debug.Log("\n=== Testing Weapon Loading ===");
        
        // Common weapon IDs
        int[] testWeaponIds = { 1302000, 1402000, 1312000 }; // Basic weapons
        
        foreach (int weaponId in testWeaponIds)
        {
            Debug.Log($"\nTesting weapon {weaponId}:");
            
            // Try loading for stand animation
            var weaponSprite = loader.LoadEquipment(weaponId, "Weapon", "stand1", 0);
            if (weaponSprite != null)
            {
                Debug.Log($"  ✓ Loaded: {weaponSprite.name} ({weaponSprite.rect.width}x{weaponSprite.rect.height})");
            }
            else
            {
                Debug.Log($"  ✗ Failed to load weapon {weaponId}");
                
                // Try exploring the structure
                ExploreEquipmentStructure(weaponId);
            }
        }
    }
    
    private static void TestCapeLoading(NXAssetLoader loader)
    {
        Debug.Log("\n=== Testing Cape Loading ===");
        
        // Common cape IDs
        int[] testCapeIds = { 1102000, 1102001, 1102002 }; // Basic capes
        
        foreach (int capeId in testCapeIds)
        {
            Debug.Log($"\nTesting cape {capeId}:");
            
            // Try loading for stand animation
            var capeSprite = loader.LoadEquipment(capeId, "Cape", "stand1", 0);
            if (capeSprite != null)
            {
                Debug.Log($"  ✓ Loaded: {capeSprite.name} ({capeSprite.rect.width}x{capeSprite.rect.height})");
            }
            else
            {
                Debug.Log($"  ✗ Failed to load cape {capeId}");
            }
        }
    }
    
    private static void ExploreEquipmentStructure(int itemId)
    {
        Debug.Log($"\nExploring equipment structure for {itemId}:");
        
        var loader = NXAssetLoader.Instance;
        var charFile = loader.GetNxFile("character");
        
        if (charFile == null) return;
        
        // Build the equipment path
        string category = GetEquipmentCategory(itemId);
        string itemFile = $"{itemId:D8}.img";
        string basePath = $"{category}/{itemFile}";
        
        var itemNode = charFile.GetNode(basePath);
        if (itemNode != null)
        {
            Debug.Log($"  Found item node at: {basePath}");
            Debug.Log($"  Children:");
            int count = 0;
            foreach (var child in itemNode.Children)
            {
                Debug.Log($"    - {child.Name}");
                if (++count >= 10)
                {
                    Debug.Log("    ... (more)");
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"  Item node not found at: {basePath}");
        }
    }
    
    private static string GetEquipmentCategory(int itemId)
    {
        int subtype = (itemId / 1000) % 100;
        
        switch (subtype)
        {
            case 0: return "Cap";
            case 2: return "Accessory";
            case 4: return "Coat";
            case 6: return "Pants";
            case 7: return "Shoes";
            case 8: return "Glove";
            case 9: return "Shield";
            case 10: return "Cape";
            case 30:
            case 31:
            case 32:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 38:
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49: return "Weapon";
            default: return "Etc";
        }
    }
}