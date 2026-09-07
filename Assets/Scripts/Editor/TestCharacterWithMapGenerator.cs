using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.SceneGeneration;
using MapleClient.GameView;

public static class TestCharacterWithMapGenerator
{
    [MenuItem("MapleUnity/Test/Test Character with Map Generator")]
    public static void RunTest()
    {
        Debug.Log("=== CHARACTER RENDERING TEST WITH MAP GENERATOR ===");
        
        // Create a new scene for testing
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create MapSceneGenerator
        var generatorObj = new GameObject("MapSceneGenerator");
        var generator = generatorObj.AddComponent<MapSceneGenerator>();
        generator.InitializeGenerators();
        
        // Generate a simple map (Henesys)
        int mapId = 100000000;
        Debug.Log($"Generating map {mapId} (Henesys)...");
        var mapObj = generator.GenerateMapScene(mapId);
        
        if (mapObj == null)
        {
            Debug.LogError("Failed to generate map!");
            GameObject.DestroyImmediate(generatorObj);
            return;
        }
        
        Debug.Log("Map generated successfully!");
        
        // Create a MapleCharacter for testing
        Debug.Log("\nCreating MapleCharacter...");
        var characterObj = new GameObject("MapleCharacter");
        var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
        
        // Set character appearance
        renderer.SetCharacterAppearance(0, 20000, 30000); // Default skin, face, hair
        
        // Position character at a reasonable location
        characterObj.transform.position = new Vector3(0, 5, 0);
        
        // Wait a frame for initialization
        EditorApplication.delayCall += () => {
            Debug.Log("\n=== CHARACTER STRUCTURE ===");
            LogCharacterStructure(characterObj);
            
            // Clean up
            GameObject.DestroyImmediate(generatorObj);
            
            Debug.Log("\n=== TEST COMPLETE ===");
            Debug.Log("The character has been created in the scene. Check the hierarchy and scene view.");
        };
    }
    
    private static void LogCharacterStructure(GameObject character)
    {
        Debug.Log($"Character at: {character.transform.position}");
        Debug.Log($"Scale: {character.transform.localScale}");
        
        // Log hierarchy
        Debug.Log("\nHierarchy:");
        LogHierarchy(character.transform, 0);
        
        // Check face offset
        Transform face = null;
        Transform head = character.transform.Find("Head");
        
        if (head != null)
        {
            face = head.Find("Face");
            if (face != null)
            {
                Debug.Log($"\n--- FACE OFFSET ANALYSIS ---");
                Debug.Log($"Head position: {head.localPosition}");
                Debug.Log($"Face relative to head: {face.localPosition}");
                
                // Total offset from character origin
                Vector3 totalOffset = head.localPosition + face.localPosition;
                Debug.Log($"Face total offset from character: {totalOffset}");
                
                // Convert to pixels
                Vector2 pixelOffset = new Vector2(totalOffset.x * 100, totalOffset.y * 100);
                Debug.Log($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                
                // Compare with expected C++ value
                Vector2 expectedPixels = new Vector2(-8, -52);
                float distance = Vector2.Distance(pixelOffset, expectedPixels);
                
                if (distance < 1f)
                {
                    Debug.Log($"✓ Face offset MATCHES C++ runtime!");
                }
                else
                {
                    Debug.LogWarning($"✗ Face offset mismatch: Expected {expectedPixels}, Got {pixelOffset}");
                    Debug.Log($"  Difference: ({pixelOffset.x - expectedPixels.x:F1}, {pixelOffset.y - expectedPixels.y:F1}) pixels");
                }
            }
        }
        else
        {
            // Check if face is directly under character
            face = character.transform.Find("Face");
            if (face != null)
            {
                Debug.Log($"\n--- FACE OFFSET ANALYSIS ---");
                Debug.Log($"Face direct offset from character: {face.localPosition}");
                
                Vector2 pixelOffset = new Vector2(face.localPosition.x * 100, face.localPosition.y * 100);
                Debug.Log($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                
                Vector2 expectedPixels = new Vector2(-8, -52);
                float distance = Vector2.Distance(pixelOffset, expectedPixels);
                
                if (distance < 1f)
                {
                    Debug.Log($"✓ Face offset MATCHES C++ runtime!");
                }
                else
                {
                    Debug.LogWarning($"✗ Face offset mismatch: Expected {expectedPixels}, Got {pixelOffset}");
                }
            }
        }
        
        if (face == null)
        {
            Debug.LogWarning("Face not found in character hierarchy!");
        }
    }
    
    private static void LogHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        var sr = t.GetComponent<SpriteRenderer>();
        string spriteInfo = sr != null && sr.sprite != null ? $" [Sprite: {sr.sprite.name}]" : "";
        Debug.Log($"{indent}{t.name}: {t.localPosition}{spriteInfo}");
        
        foreach (Transform child in t)
        {
            LogHierarchy(child, depth + 1);
        }
    }
}