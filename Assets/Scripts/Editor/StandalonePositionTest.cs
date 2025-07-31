using UnityEngine;
using UnityEditor;
using System.IO;
using MapleClient.GameView;

public static class StandalonePositionTest
{
    [MenuItem("Test/Run Position Analysis")]
    public static void RunTest()
    {
        string logFile = @"C:\Users\me\MapleUnity\character-position-test.log";
        
        try
        {
            File.WriteAllText(logFile, "=== Character Position Test Started ===\n");
            Debug.Log("Starting character position test...");
            
            
            // Create scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Create character
            GameObject characterGO = new GameObject("TestCharacter");
            characterGO.transform.position = Vector3.zero;
            
            // Add renderer
            var renderer = characterGO.AddComponent<MapleCharacterRenderer>();
            File.AppendAllText(logFile, "Added MapleCharacterRenderer component\n");
            
            // Force initialization
            var startMethod = renderer.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (startMethod != null)
            {
                startMethod.Invoke(renderer, null);
                File.AppendAllText(logFile, "Called Start() method\n");
            }
            else
            {
                File.AppendAllText(logFile, "WARNING: Start() method not found\n");
            }
            
            // Analyze immediately (no delay)
            File.AppendAllText(logFile, "\n=== Analyzing Sprite Positions ===\n");
            
            var sprites = characterGO.GetComponentsInChildren<SpriteRenderer>();
            File.AppendAllText(logFile, $"Found {sprites.Length} sprite renderers\n");
            
            foreach (var sr in sprites)
            {
                File.AppendAllText(logFile, $"\n[{sr.gameObject.name}]\n");
                File.AppendAllText(logFile, $"  Position: {sr.transform.localPosition}\n");
                File.AppendAllText(logFile, $"  Has Sprite: {sr.sprite != null}\n");
                File.AppendAllText(logFile, $"  Sorting Order: {sr.sortingOrder}\n");
            }
            
            // Check specific parts
            File.AppendAllText(logFile, "\n=== Checking Specific Parts ===\n");
            
            var body = characterGO.transform.Find("body");
            File.AppendAllText(logFile, $"Body: {(body != null ? $"Found at Y={body.localPosition.y}" : "NOT FOUND")}\n");
            
            var arm = characterGO.transform.Find("arm");
            File.AppendAllText(logFile, $"Arm: {(arm != null ? $"Found at Y={arm.localPosition.y}" : "NOT FOUND")}\n");
            
            var head = characterGO.transform.Find("head");
            File.AppendAllText(logFile, $"Head: {(head != null ? $"Found at Y={head.localPosition.y}" : "NOT FOUND")}\n");
            
            // Check positions
            if (body != null && arm != null)
            {
                float diff = arm.localPosition.y - body.localPosition.y;
                File.AppendAllText(logFile, $"\nArm vs Body Y difference: {diff}\n");
                if (diff < -0.1f)
                {
                    File.AppendAllText(logFile, "ERROR: ARM IS BELOW BODY!\n");
                }
            }
            
            File.AppendAllText(logFile, "\n=== Test Completed ===\n");
            Debug.Log("Test completed, check character-position-test.log");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logFile, $"\nERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}