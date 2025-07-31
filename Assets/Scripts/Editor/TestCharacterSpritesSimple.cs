using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;

public class TestCharacterSpritesSimple
{
    [MenuItem("MapleUnity/Tests/Test Character Sprites Simple")]
    public static void RunTest()
    {
        Debug.Log("[TEST_CHARACTER_SPRITES] Starting character sprite test...");
        
        // Ensure NX files are loaded
        var nxManager = NXDataManagerSingleton.Instance;
        if (nxManager == null || nxManager.DataManager == null)
        {
            Debug.LogError("[TEST_CHARACTER_SPRITES] NXDataManagerSingleton not initialized!");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            return;
        }
        
        Debug.Log("[TEST_CHARACTER_SPRITES] NX Data Manager initialized successfully");
        
        // Test 1: Direct sprite loading through NXAssetLoader
        Debug.Log("\n=== TEST 1: Direct NXAssetLoader Test ===");
        TestDirectSpriteLoading();
        
        // Test 2: CharacterDataProvider test
        Debug.Log("\n=== TEST 2: CharacterDataProvider Test ===");
        TestCharacterDataProvider();
        
        Debug.Log("\n[TEST_CHARACTER_SPRITES] Test complete!");
        
        if (Application.isBatchMode)
        {
            Debug.Log("[TEST_CHARACTER_SPRITES] Exiting batch mode with success...");
            EditorApplication.Exit(0);
        }
    }
    
    private static void TestDirectSpriteLoading()
    {
        var loader = NXAssetLoader.Instance;
        
        // Test loading body sprite
        Debug.Log("Testing body sprite loading...");
        var bodySprite = loader.LoadCharacterBody(0, "stand1", 0);
        if (bodySprite != null)
        {
            Debug.Log($"✓ Body sprite loaded: {bodySprite.name} ({bodySprite.rect.width}x{bodySprite.rect.height})");
            Debug.Log($"  Pivot: {bodySprite.pivot}, PPU: {bodySprite.pixelsPerUnit}");
        }
        else
        {
            Debug.LogError("✗ Failed to load body sprite");
        }
        
        // Test loading head sprite
        Debug.Log("\nTesting head sprite loading...");
        var headSprite = loader.LoadCharacterHead(0, "stand1", 0);
        if (headSprite != null)
        {
            Debug.Log($"✓ Head sprite loaded: {headSprite.name}");
        }
        else
        {
            Debug.LogError("✗ Failed to load head sprite");
        }
        
        // Test loading face sprite
        Debug.Log("\nTesting face sprite loading...");
        var faceSprite = loader.LoadFace(20000, "default");
        if (faceSprite != null)
        {
            Debug.Log($"✓ Face sprite loaded: {faceSprite.name}");
        }
        else
        {
            Debug.LogError("✗ Failed to load face sprite");
        }
        
        // Test loading hair sprite
        Debug.Log("\nTesting hair sprite loading...");
        var hairSprite = loader.LoadHair(30000, "stand1", 0);
        if (hairSprite != null)
        {
            Debug.Log($"✓ Hair sprite loaded: {hairSprite.name}");
        }
        else
        {
            Debug.LogError("✗ Failed to load hair sprite");
        }
    }
    
    private static void TestCharacterDataProvider()
    {
        var provider = new CharacterDataProvider();
        
        // Test body sprite data
        Debug.Log("Testing CharacterDataProvider body sprite...");
        var bodySpriteData = provider.GetBodySprite(0, MapleClient.GameLogic.Interfaces.CharacterState.Stand, 0);
        if (bodySpriteData != null)
        {
            Debug.Log($"✓ Body sprite data returned: {bodySpriteData.Name}");
            Debug.Log($"  Size: {bodySpriteData.Width}x{bodySpriteData.Height}");
            Debug.Log($"  Origin: ({bodySpriteData.OriginX}, {bodySpriteData.OriginY})");
            
            if (bodySpriteData is UnitySpriteData unityData && unityData.UnitySprite != null)
            {
                Debug.Log($"  ✓ Unity sprite available: {unityData.UnitySprite.name}");
            }
            else
            {
                Debug.LogError("  ✗ No Unity sprite in data!");
            }
        }
        else
        {
            Debug.LogError("✗ Failed to get body sprite data");
        }
        
        // Test animation frame counts
        Debug.Log("\nTesting animation frame counts...");
        Debug.Log($"Stand frames: {provider.GetAnimationFrameCount(MapleClient.GameLogic.Interfaces.CharacterState.Stand)}");
        Debug.Log($"Walk frames: {provider.GetAnimationFrameCount(MapleClient.GameLogic.Interfaces.CharacterState.Walk)}");
        Debug.Log($"Jump frames: {provider.GetAnimationFrameCount(MapleClient.GameLogic.Interfaces.CharacterState.Jump)}");
    }
}