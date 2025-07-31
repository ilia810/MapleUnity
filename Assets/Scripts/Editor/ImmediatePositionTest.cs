using UnityEngine;
using UnityEditor;
using System.IO;
using MapleClient.GameView;

[InitializeOnLoad]
public static class ImmediatePositionTest
{
    static ImmediatePositionTest()
    {
        // This runs when Unity loads, not when executeMethod is called
        if (Application.isBatchMode)
        {
            EditorApplication.delayCall += RunTest;
        }
    }
    
    static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\immediate-position-test.txt";
        
        try
        {
            File.WriteAllText(logPath, "=== Immediate Character Position Test ===\n");
            File.AppendAllText(logPath, $"Time: {System.DateTime.Now}\n");
            File.AppendAllText(logPath, "Running in batch mode\n\n");
            
            // Create test scene
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            
            // Create character
            GameObject characterGO = new GameObject("TestCharacter");
            var renderer = characterGO.AddComponent<MapleCharacterRenderer>();
            
            File.AppendAllText(logPath, "Created character with MapleCharacterRenderer\n");
            
            // Force Start
            var startMethod = renderer.GetType().GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (startMethod != null)
            {
                startMethod.Invoke(renderer, null);
                File.AppendAllText(logPath, "Called Start() on renderer\n");
            }
            
            // Analyze positions
            File.AppendAllText(logPath, "\n=== Sprite Analysis ===\n");
            var sprites = characterGO.GetComponentsInChildren<SpriteRenderer>();
            File.AppendAllText(logPath, $"Total sprites: {sprites.Length}\n");
            
            foreach (var sr in sprites)
            {
                File.AppendAllText(logPath, $"\n{sr.gameObject.name}:\n");
                File.AppendAllText(logPath, $"  Position: {sr.transform.localPosition}\n");
                File.AppendAllText(logPath, $"  Sorting Order: {sr.sortingOrder}\n");
            }
            
            // Check specific parts
            CheckPart(characterGO, "body", logPath);
            CheckPart(characterGO, "arm", logPath);
            CheckPart(characterGO, "head", logPath);
            CheckPart(characterGO, "face", logPath);
            
            // Position analysis
            var body = characterGO.transform.Find("body");
            var arm = characterGO.transform.Find("arm");
            var head = characterGO.transform.Find("head");
            
            File.AppendAllText(logPath, "\n=== Position Analysis ===\n");
            if (body != null && arm != null)
            {
                float diff = arm.localPosition.y - body.localPosition.y;
                File.AppendAllText(logPath, $"Arm vs Body Y: {diff:F3}\n");
                if (diff < -0.1f)
                {
                    File.AppendAllText(logPath, "ERROR: ARM BELOW BODY!\n");
                }
            }
            
            if (body != null && head != null)
            {
                float diff = head.localPosition.y - body.localPosition.y;
                File.AppendAllText(logPath, $"Head vs Body Y: {diff:F3}\n");
                if (diff < 0)
                {
                    File.AppendAllText(logPath, "ERROR: HEAD BELOW BODY!\n");
                }
            }
            
            File.AppendAllText(logPath, "\nTest completed\n");
            Debug.Log("Position test completed - check immediate-position-test.txt");
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n");
            Debug.LogError($"Test failed: {e.Message}");
        }
    }
    
    static void CheckPart(GameObject parent, string name, string logPath)
    {
        var part = parent.transform.Find(name);
        if (part != null)
        {
            File.AppendAllText(logPath, $"\n{name}: Found at Y={part.localPosition.y:F3}\n");
        }
        else
        {
            File.AppendAllText(logPath, $"\n{name}: NOT FOUND\n");
        }
    }
}