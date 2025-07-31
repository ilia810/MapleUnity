using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class SimpleSceneTest
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "scene-test-results.txt");
        
        try
        {
            File.WriteAllText(logPath, "=== Scene Test Started ===\n");
            
            // Load TestScene
            var scenePath = "Assets/Scenes/TestScene.unity";
            EditorSceneManager.OpenScene(scenePath);
            
            File.AppendAllText(logPath, "Scene loaded successfully\n\n");
            
            // Find all GameObjects in scene
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            File.AppendAllText(logPath, $"Total GameObjects in scene: {allObjects.Length}\n\n");
            
            // Look for character-related objects
            File.AppendAllText(logPath, "=== Character-Related Objects ===\n");
            foreach (var obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("character") || name.Contains("player") || 
                    name.Contains("body") || name.Contains("head") || 
                    name.Contains("arm") || name.Contains("face"))
                {
                    File.AppendAllText(logPath, $"\nFound: {obj.name}\n");
                    File.AppendAllText(logPath, $"  Position: {obj.transform.position}\n");
                    File.AppendAllText(logPath, $"  Local Position: {obj.transform.localPosition}\n");
                    File.AppendAllText(logPath, $"  Parent: {(obj.transform.parent ? obj.transform.parent.name : "None")}\n");
                    
                    // Check for sprite renderer
                    var spriteRenderer = obj.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        File.AppendAllText(logPath, $"  Has SpriteRenderer: Yes\n");
                        File.AppendAllText(logPath, $"  Sprite: {(spriteRenderer.sprite ? spriteRenderer.sprite.name : "None")}\n");
                        File.AppendAllText(logPath, $"  Sorting Order: {spriteRenderer.sortingOrder}\n");
                    }
                    
                    // Check children
                    if (obj.transform.childCount > 0)
                    {
                        File.AppendAllText(logPath, $"  Children: {obj.transform.childCount}\n");
                        for (int i = 0; i < obj.transform.childCount; i++)
                        {
                            var child = obj.transform.GetChild(i);
                            File.AppendAllText(logPath, $"    - {child.name} at {child.localPosition}\n");
                        }
                    }
                }
            }
            
            // Check for specific hierarchy
            File.AppendAllText(logPath, "\n=== Checking Character Hierarchy ===\n");
            
            var characterObj = GameObject.Find("Character") ?? GameObject.Find("MapleCharacter") ?? GameObject.Find("Player");
            if (characterObj != null)
            {
                File.AppendAllText(logPath, $"Main character object: {characterObj.name}\n");
                AnalyzeHierarchy(characterObj.transform, logPath, 0);
            }
            else
            {
                File.AppendAllText(logPath, "No main character object found\n");
            }
            
            File.AppendAllText(logPath, "\n=== Test Completed Successfully ===\n");
            Debug.Log("Scene test completed. Results written to scene-test-results.txt");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeHierarchy(Transform transform, string logPath, int depth)
    {
        string indent = new string(' ', depth * 2);
        File.AppendAllText(logPath, $"{indent}{transform.name} - Pos: {transform.localPosition}, World: {transform.position}\n");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            AnalyzeHierarchy(transform.GetChild(i), logPath, depth + 1);
        }
    }
}