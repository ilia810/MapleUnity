using UnityEngine;
using UnityEditor;
using System.IO;
using MapleClient.GameView;

public static class SimplePositionTest
{
    [MenuItem("Test/Analyze Character Positions")]
    public static void RunTest()
    {
        Debug.Log("Starting character position analysis...");
        
        try
        {
            // Create test scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, 
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Create character
            var characterGO = new GameObject("TestCharacter");
            var renderer = characterGO.AddComponent<MapleCharacterRenderer>();
            
            // Log initial state
            Debug.Log("Created character with MapleCharacterRenderer");
            
            // Schedule analysis for next frame
            EditorApplication.delayCall += () => AnalyzePositions(characterGO);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create test: " + e.Message);
            EditorApplication.Exit(1);
        }
    }
    
    static void AnalyzePositions(GameObject characterGO)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine("=== Character Position Analysis ===");
        
        // Find all children
        foreach (Transform child in characterGO.transform)
        {
            output.AppendLine($"\nChild: {child.name}");
            output.AppendLine($"  Position: {child.localPosition}");
            
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                output.AppendLine($"  Has Sprite: {sr.sprite != null}");
                output.AppendLine($"  Sorting Order: {sr.sortingOrder}");
            }
        }
        
        // Check specific parts
        output.AppendLine("\n=== Specific Parts ===");
        
        var body = characterGO.transform.Find("body");
        output.AppendLine($"Body: {(body != null ? $"Y={body.localPosition.y}" : "NOT FOUND")}");
        
        var arm = characterGO.transform.Find("arm");
        output.AppendLine($"Arm: {(arm != null ? $"Y={arm.localPosition.y}" : "NOT FOUND")}");
        
        var head = characterGO.transform.Find("head");
        output.AppendLine($"Head: {(head != null ? $"Y={head.localPosition.y}" : "NOT FOUND")}");
        
        var face = characterGO.transform.Find("face");
        if (face == null && head != null)
        {
            face = head.Find("face");
        }
        output.AppendLine($"Face: {(face != null ? $"Y={face.position.y} (world)" : "NOT FOUND")}");
        
        // Visual analysis
        if (body != null && arm != null)
        {
            float armBodyDiff = arm.localPosition.y - body.localPosition.y;
            output.AppendLine($"\nArm vs Body Y difference: {armBodyDiff}");
            if (armBodyDiff < -5) output.AppendLine("WARNING: Arm appears BELOW body!");
        }
        
        if (body != null && head != null)
        {
            float headBodyDiff = head.localPosition.y - body.localPosition.y;
            output.AppendLine($"Head vs Body Y difference: {headBodyDiff}");
            if (headBodyDiff < 0) output.AppendLine("WARNING: Head appears BELOW body!");
        }
        
        // Save and exit
        File.WriteAllText("position-test-results.txt", output.ToString());
        Debug.Log(output.ToString());
        Debug.Log("Analysis saved to position-test-results.txt");
        
        EditorApplication.Exit(0);
    }
}