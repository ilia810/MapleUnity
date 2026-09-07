using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using System.IO;

public static class BatchTestCharacterFaceOffset
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "face-offset-test-results.txt");
        System.Text.StringBuilder results = new System.Text.StringBuilder();
        
        try
        {
            Debug.Log("=== BATCH CHARACTER FACE OFFSET TEST ===");
            results.AppendLine("=== BATCH CHARACTER FACE OFFSET TEST ===");
            
            // Create a new scene for testing
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create MapSceneGenerator
            var generatorObj = new GameObject("MapSceneGenerator");
            var generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            // Generate a simple map (Henesys)
            int mapId = 100000000;
            Debug.Log($"Generating map {mapId} (Henesys)...");
            results.AppendLine($"Generating map {mapId} (Henesys)...");
            
            var mapObj = generator.GenerateMapScene(mapId);
            
            if (mapObj == null)
            {
                string error = "Failed to generate map!";
                Debug.LogError(error);
                results.AppendLine($"ERROR: {error}");
                File.WriteAllText(logPath, results.ToString());
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("Map generated successfully!");
            results.AppendLine("Map generated successfully!");
            
            // Create a MapleCharacter for testing
            Debug.Log("Creating MapleCharacter...");
            results.AppendLine("\nCreating MapleCharacter...");
            
            var characterObj = new GameObject("MapleCharacter");
            var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
            
            // Set character appearance
            renderer.SetCharacterAppearance(0, 20000, 30000); // Default skin, face, hair
            
            // Position character at a reasonable location
            characterObj.transform.position = new Vector3(0, 5, 0);
            
            // Analyze face offset immediately
            Debug.Log("\n=== FACE OFFSET ANALYSIS ===");
            results.AppendLine("\n=== FACE OFFSET ANALYSIS ===");
            
            Transform face = null;
            Transform head = characterObj.transform.Find("Head");
            
            if (head != null)
            {
                face = head.Find("Face");
                if (face != null)
                {
                    Debug.Log($"Head position: {head.localPosition}");
                    Debug.Log($"Face relative to head: {face.localPosition}");
                    results.AppendLine($"Head position: {head.localPosition}");
                    results.AppendLine($"Face relative to head: {face.localPosition}");
                    
                    // Total offset from character origin
                    Vector3 totalOffset = head.localPosition + face.localPosition;
                    Debug.Log($"Face total offset from character: {totalOffset}");
                    results.AppendLine($"Face total offset from character: {totalOffset}");
                    
                    // Convert to pixels
                    Vector2 pixelOffset = new Vector2(totalOffset.x * 100, totalOffset.y * 100);
                    Debug.Log($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                    results.AppendLine($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                    
                    // Compare with expected C++ value
                    Vector2 expectedPixels = new Vector2(-8, -52);
                    float distance = Vector2.Distance(pixelOffset, expectedPixels);
                    
                    results.AppendLine($"\nExpected C++ runtime offset: {expectedPixels}");
                    results.AppendLine($"Distance from expected: {distance:F2} pixels");
                    
                    if (distance < 1f)
                    {
                        string success = "✓ Face offset MATCHES C++ runtime!";
                        Debug.Log(success);
                        results.AppendLine($"\n{success}");
                        results.AppendLine("TEST PASSED");
                    }
                    else
                    {
                        string error = $"✗ Face offset mismatch: Expected {expectedPixels}, Got {pixelOffset}";
                        Debug.LogWarning(error);
                        results.AppendLine($"\n{error}");
                        results.AppendLine($"Difference: ({pixelOffset.x - expectedPixels.x:F1}, {pixelOffset.y - expectedPixels.y:F1}) pixels");
                        results.AppendLine("TEST FAILED");
                    }
                }
            }
            else
            {
                // Check if face is directly under character
                face = characterObj.transform.Find("Face");
                if (face != null)
                {
                    Debug.Log($"Face direct offset from character: {face.localPosition}");
                    results.AppendLine($"Face direct offset from character: {face.localPosition}");
                    
                    Vector2 pixelOffset = new Vector2(face.localPosition.x * 100, face.localPosition.y * 100);
                    Debug.Log($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                    results.AppendLine($"Face pixel offset: ({pixelOffset.x:F0}, {pixelOffset.y:F0})");
                    
                    Vector2 expectedPixels = new Vector2(-8, -52);
                    float distance = Vector2.Distance(pixelOffset, expectedPixels);
                    
                    results.AppendLine($"\nExpected C++ runtime offset: {expectedPixels}");
                    results.AppendLine($"Distance from expected: {distance:F2} pixels");
                    
                    if (distance < 1f)
                    {
                        string success = "✓ Face offset MATCHES C++ runtime!";
                        Debug.Log(success);
                        results.AppendLine($"\n{success}");
                        results.AppendLine("TEST PASSED");
                    }
                    else
                    {
                        string error = $"✗ Face offset mismatch: Expected {expectedPixels}, Got {pixelOffset}";
                        Debug.LogWarning(error);
                        results.AppendLine($"\n{error}");
                        results.AppendLine("TEST FAILED");
                    }
                }
            }
            
            if (face == null)
            {
                string error = "Face not found in character hierarchy!";
                Debug.LogWarning(error);
                results.AppendLine($"\nERROR: {error}");
                results.AppendLine("TEST FAILED - No face found");
            }
            
            // Log full hierarchy
            results.AppendLine("\n=== FULL CHARACTER HIERARCHY ===");
            LogHierarchy(characterObj.transform, 0, results);
            
            // Clean up
            GameObject.DestroyImmediate(generatorObj);
            
            // Write results and exit
            File.WriteAllText(logPath, results.ToString());
            Debug.Log($"Test results written to: {logPath}");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            string error = $"Test failed with exception: {e.Message}\n{e.StackTrace}";
            Debug.LogError(error);
            results.AppendLine($"\nFATAL ERROR: {error}");
            File.WriteAllText(logPath, results.ToString());
            EditorApplication.Exit(1);
        }
    }
    
    private static void LogHierarchy(Transform t, int depth, System.Text.StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        var sr = t.GetComponent<SpriteRenderer>();
        string spriteInfo = sr != null && sr.sprite != null ? $" [Sprite: {sr.sprite.name}]" : "";
        sb.AppendLine($"{indent}{t.name}: {t.localPosition}{spriteInfo}");
        
        foreach (Transform child in t)
        {
            LogHierarchy(child, depth + 1, sb);
        }
    }
}