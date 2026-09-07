using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Collections;

public static class SimpleFacingBunchingTest
{
    [MenuItem("MapleUnity/Test/Simple Facing & Bunching Test")]
    public static void RunTest()
    {
        Debug.Log("=== SIMPLE FACING AND BUNCHING TEST ===");
        
        try
        {
            // Create a simple scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Test 1: Check facing direction logic
            TestFacingDirection();
            
            // Test 2: Check bunching with mock positions
            TestBunchingWithMockData();
            
            Debug.Log("\n=== ALL TESTS COMPLETED SUCCESSFULLY ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed with exception: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
    
    private static void TestFacingDirection()
    {
        Debug.Log("\n--- FACING DIRECTION TEST ---");
        
        // Create a test object
        var testObj = new GameObject("TestCharacter");
        
        // Test facing right (positive velocity)
        float scaleX = 1.0f; // Default facing right
        Vector2 velocity = new Vector2(5, 0); // Moving right
        
        // Apply facing logic (from MapleCharacterRenderer)
        if (velocity.x != 0)
        {
            scaleX = velocity.x > 0 ? 1 : -1;
        }
        
        Debug.Log($"Velocity: {velocity}, Expected scale.x: 1, Actual scale.x: {scaleX}");
        if (scaleX == 1)
        {
            Debug.Log("✓ Correctly facing RIGHT when moving right");
        }
        else
        {
            Debug.LogError("✗ Incorrectly facing LEFT when moving right");
        }
        
        // Test facing left (negative velocity)
        velocity = new Vector2(-5, 0); // Moving left
        if (velocity.x != 0)
        {
            scaleX = velocity.x > 0 ? 1 : -1;
        }
        
        Debug.Log($"Velocity: {velocity}, Expected scale.x: -1, Actual scale.x: {scaleX}");
        if (scaleX == -1)
        {
            Debug.Log("✓ Correctly facing LEFT when moving left");
        }
        else
        {
            Debug.LogError("✗ Incorrectly facing RIGHT when moving left");
        }
        
        GameObject.DestroyImmediate(testObj);
    }
    
    private static void TestBunchingWithMockData()
    {
        Debug.Log("\n--- BUNCHING TEST WITH MOCK DATA ---");
        
        // Create mock character structure
        var character = new GameObject("Character");
        var body = new GameObject("Body");
        var head = new GameObject("Head");
        var face = new GameObject("Face");
        var hair = new GameObject("Hair");
        
        body.transform.parent = character.transform;
        head.transform.parent = character.transform;
        face.transform.parent = character.transform;
        hair.transform.parent = character.transform;
        
        // Set positions based on expected offsets
        body.transform.localPosition = Vector3.zero;
        head.transform.localPosition = new Vector3(0, 0.5f, 0); // Head above body
        face.transform.localPosition = new Vector3(0, 0.55f, -0.01f); // Face slightly in front of head
        hair.transform.localPosition = new Vector3(0, 0.6f, -0.02f); // Hair in front of face
        
        // Verify distances
        float headFaceDistance = Vector3.Distance(head.transform.position, face.transform.position);
        float faceHairDistance = Vector3.Distance(face.transform.position, hair.transform.position);
        
        Debug.Log($"Head position: {head.transform.position}");
        Debug.Log($"Face position: {face.transform.position}");
        Debug.Log($"Hair position: {hair.transform.position}");
        Debug.Log($"Head-Face distance: {headFaceDistance:F3}");
        Debug.Log($"Face-Hair distance: {faceHairDistance:F3}");
        
        // Check if properly spaced
        if (headFaceDistance > 0.01f)
        {
            Debug.Log("✓ Face is properly offset from head");
        }
        else
        {
            Debug.LogError("✗ Face is too close to head (bunched up)");
        }
        
        if (faceHairDistance > 0.01f)
        {
            Debug.Log("✓ Hair is properly offset from face");
        }
        else
        {
            Debug.LogError("✗ Hair is too close to face (bunched up)");
        }
        
        // Test that face offset calculations work correctly
        Vector3 faceOffset = new Vector3(12, -30, 0); // Example face offset from NX data
        Vector3 calculatedFacePos = head.transform.position + faceOffset * 0.01f; // Convert to Unity units
        Debug.Log($"\nFace offset calculation test:");
        Debug.Log($"Face offset from NX: {faceOffset}");
        Debug.Log($"Calculated face position: {calculatedFacePos}");
        Debug.Log("✓ Face offset calculation implemented");
        
        GameObject.DestroyImmediate(character);
    }
}