using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using MapleClient.GameLogic;

public static class DebugFacingAndBunching
{
    [MenuItem("MapleUnity/Debug/Facing and Bunching Issue")]
    public static void RunDebug()
    {
        Debug.Log("=== DEBUG FACING AND BUNCHING ISSUE ===");
        
        // Create test scene
        EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
        
        // Initialize with mock data
        var mockFile = new MockNxFile();
        NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
        
        // Create character
        var characterObj = new GameObject("TestCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Force initialization
        renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        
        // Test both facing directions
        EditorApplication.delayCall += () => {
            Debug.Log("\n=== INITIAL STATE (FACING RIGHT) ===");
            AnalyzeCharacter(characterObj, "Initial");
            
            // Simulate moving right (positive velocity)
            Debug.Log("\n=== MOVING RIGHT (Positive X Velocity) ===");
            renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(1, 0));
            EditorApplication.delayCall += () => {
                AnalyzeCharacter(characterObj, "Moving Right");
                
                // Simulate moving left (negative velocity)
                Debug.Log("\n=== MOVING LEFT (Negative X Velocity) ===");
                renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(-1, 0));
                EditorApplication.delayCall += () => {
                    AnalyzeCharacter(characterObj, "Moving Left");
                    
                    // Check the actual flip state
                    Debug.Log("\n=== FLIP STATE ANALYSIS ===");
                    CheckFlipState(renderer);
                };
            };
        };
    }
    
    private static void AnalyzeCharacter(GameObject character, string state)
    {
        Debug.Log($"\n--- {state} ---");
        Debug.Log($"Character scale: {character.transform.localScale}");
        
        // Find all sprite renderers
        var renderers = character.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"Found {renderers.Length} sprite renderers");
        
        // Check positions and flip states
        foreach (var sr in renderers)
        {
            Debug.Log($"{sr.name}:");
            Debug.Log($"  Local Position: {sr.transform.localPosition}");
            Debug.Log($"  Local Scale: {sr.transform.localScale}");
            Debug.Log($"  Sprite FlipX: {sr.flipX}");
            Debug.Log($"  World Position: {sr.transform.position}");
            
            // Check if positions are too close (bunched up)
            foreach (var other in renderers)
            {
                if (sr != other)
                {
                    float distance = Vector3.Distance(sr.transform.position, other.transform.position);
                    if (distance < 0.1f)
                    {
                        Debug.LogWarning($"  WARNING: {sr.name} and {other.name} are very close ({distance:F3} units)");
                    }
                }
            }
        }
        
        // Check attachment point application
        Debug.Log("\nAttachment Point Application:");
        var body = FindChild(character.transform, "Body");
        var head = FindChild(character.transform, "Head");
        var face = FindChild(character.transform, "Face");
        
        if (body != null && head != null)
        {
            Vector3 expectedHeadOffset = new Vector3(-0.21f, 0.5f, 0);  // From C++ formula
            Vector3 actualHeadOffset = head.localPosition - body.localPosition;
            Debug.Log($"Expected head offset: {expectedHeadOffset}");
            Debug.Log($"Actual head offset: {actualHeadOffset}");
            Debug.Log($"Difference: {(actualHeadOffset - expectedHeadOffset).magnitude:F3}");
        }
    }
    
    private static void CheckFlipState(MapleCharacterRenderer renderer)
    {
        // Use reflection to check internal flip state
        var flipField = renderer.GetType().GetField("flip", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (flipField != null)
        {
            bool flip = (bool)flipField.GetValue(renderer);
            Debug.Log($"Internal flip state: {flip}");
            Debug.Log($"Character transform scale: {renderer.transform.localScale}");
            
            // According to research7.txt:
            // - Sprites face LEFT by default
            // - flip=true means facing LEFT (confusing but that's how C++ does it)
            // - Moving right should set flip=false, moving left should set flip=true
            
            Debug.Log("\nExpected behavior (from research7.txt):");
            Debug.Log("- Sprites face LEFT by default");
            Debug.Log("- flip=true = facing LEFT");
            Debug.Log("- flip=false = facing RIGHT");
            Debug.Log("- Scale.x = -1 when flip=true (to flip the left-facing sprite)");
        }
    }
    
    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}