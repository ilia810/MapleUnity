using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;

public static class VerifyBunchingFix
{
    [MenuItem("MapleUnity/Test/Verify Bunching Fix")]
    public static void RunTest()
    {
        Debug.Log("=== VERIFYING BUNCHING AND FACING FIXES ===");
        
        // Create test scene
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Initialize with mock data
        var mockFile = new MockNxFile();
        NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
        
        // Create character
        var characterObj = new GameObject("TestCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        renderer.SetCharacterAppearance(0, 20000, 30000);
        
        // Force initialization
        renderer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        
        // Test facing directions
        EditorApplication.delayCall += () => {
            Debug.Log("\n=== FACING DIRECTION TEST ===");
            
            // Test moving right
            Debug.Log("\nMoving RIGHT (positive velocity):");
            renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(5, 0));
            EditorApplication.delayCall += () => {
                Debug.Log($"Character scale: {characterObj.transform.localScale}");
                bool facingRight = characterObj.transform.localScale.x > 0;
                Debug.Log(facingRight ? "✓ Correctly facing RIGHT" : "✗ Incorrectly facing LEFT");
                
                // Test moving left
                Debug.Log("\nMoving LEFT (negative velocity):");
                renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(-5, 0));
                EditorApplication.delayCall += () => {
                    Debug.Log($"Character scale: {characterObj.transform.localScale}");
                    bool facingLeft = characterObj.transform.localScale.x < 0;
                    Debug.Log(facingLeft ? "✓ Correctly facing LEFT" : "✗ Incorrectly facing RIGHT");
                    
                    // Check bunching
                    CheckBunching(characterObj);
                };
            };
        };
    }
    
    private static void CheckBunching(GameObject character)
    {
        Debug.Log("\n=== BUNCHING TEST ===");
        
        var body = FindChild(character.transform, "Body");
        var head = FindChild(character.transform, "Head");
        var face = FindChild(character.transform, "Face");
        var hair = FindChild(character.transform, "Hair");
        
        if (body != null) Debug.Log($"Body position: {body.position}");
        if (head != null) Debug.Log($"Head position: {head.position}");
        if (face != null) Debug.Log($"Face position: {face.position}");
        if (hair != null) Debug.Log($"Hair position: {hair.position}");
        
        // Check distances
        if (head != null && face != null)
        {
            float distance = Vector3.Distance(head.position, face.position);
            Debug.Log($"\nHead-Face distance: {distance:F3}");
            if (distance > 0.05f)
            {
                Debug.Log("✓ Face is properly offset from head");
            }
            else
            {
                Debug.LogError("✗ Face is too close to head (bunched up)");
            }
        }
        
        if (face != null && hair != null)
        {
            float distance = Vector3.Distance(face.position, hair.position);
            Debug.Log($"Face-Hair distance: {distance:F3}");
            if (distance > 0.01f)
            {
                Debug.Log("✓ Hair is properly offset from face");
            }
            else
            {
                Debug.LogError("✗ Hair and face are at the same position (bunched up)");
            }
        }
        
        Debug.Log("\n=== TEST COMPLETE ===");
        Debug.Log("Character should now:");
        Debug.Log("1. Face the correct direction when moving");
        Debug.Log("2. Have properly spaced body parts (not bunched)");
        
        // Exit Unity in batch mode
        EditorApplication.Exit(0);
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