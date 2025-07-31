using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

public static class SimpleCharacterPositionAnalysis
{
    public static void RunTest()
    {
        var logPath = Path.Combine(Application.dataPath, "..", "character-position-analysis.log");
        var log = new System.Text.StringBuilder();
        
        try
        {
            log.AppendLine("=== CHARACTER POSITION ANALYSIS ===");
            log.AppendLine($"Time: {System.DateTime.Now}");
            log.AppendLine();

            // Load henesys scene
            var scenePath = "Assets/henesys.unity";
            log.AppendLine($"Loading scene: {scenePath}");
            var scene = EditorSceneManager.OpenScene(scenePath);
            
            // Create a test character object with sprites
            log.AppendLine("\nCreating test character hierarchy...");
            var characterRoot = new GameObject("TestCharacter");
            characterRoot.transform.position = Vector3.zero;
            
            // Create body parts hierarchy as MapleStory would
            var body = CreateSpritePart(characterRoot, "Body", new Vector3(0, 0, 0));
            var head = CreateSpritePart(characterRoot, "Head", new Vector3(0, 15, -0.1f));
            var leftArm = CreateSpritePart(characterRoot, "LeftArm", new Vector3(-5, 10, 0.1f));
            var rightArm = CreateSpritePart(characterRoot, "RightArm", new Vector3(5, 10, 0.1f));
            var face = CreateSpritePart(head.gameObject, "Face", new Vector3(0, 0, -0.1f));
            var hair = CreateSpritePart(head.gameObject, "Hair", new Vector3(0, 2, -0.2f));
            
            log.AppendLine("\n--- Initial Positions (Facing Right) ---");
            LogTransformHierarchy(characterRoot.transform, log, "");
            
            // Test flipping for left direction
            log.AppendLine("\n--- Testing Flip for Left Direction ---");
            
            // Method 1: Using flipX on SpriteRenderers
            log.AppendLine("\nMethod 1: Using SpriteRenderer.flipX");
            SetFlipXRecursive(characterRoot.transform, true);
            LogTransformHierarchy(characterRoot.transform, log, "");
            
            // Reset
            SetFlipXRecursive(characterRoot.transform, false);
            
            // Method 2: Using negative scale.x
            log.AppendLine("\nMethod 2: Using negative localScale.x");
            characterRoot.transform.localScale = new Vector3(-1, 1, 1);
            LogTransformHierarchy(characterRoot.transform, log, "");
            
            // Method 3: Flipping individual parts
            log.AppendLine("\nMethod 3: Flipping individual sprite scales");
            characterRoot.transform.localScale = Vector3.one;
            var sprites = characterRoot.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in sprites)
            {
                var scale = sr.transform.localScale;
                scale.x = -1;
                sr.transform.localScale = scale;
            }
            LogTransformHierarchy(characterRoot.transform, log, "");
            
            // Test attachment point calculations
            log.AppendLine("\n--- Attachment Point Analysis ---");
            
            // Simulate attachment points
            log.AppendLine("\nBody attachment points:");
            log.AppendLine($"  Head attach: {body.transform.position + new Vector3(0, 15, 0)}");
            log.AppendLine($"  Left arm attach: {body.transform.position + new Vector3(-5, 10, 0)}");
            log.AppendLine($"  Right arm attach: {body.transform.position + new Vector3(5, 10, 0)}");
            
            log.AppendLine("\nHead attachment points:");
            log.AppendLine($"  Face attach: {head.transform.position + new Vector3(0, 0, 0)}");
            log.AppendLine($"  Hair attach: {head.transform.position + new Vector3(0, 2, 0)}");
            
            // Check existing sprites in scene
            log.AppendLine("\n\n--- Existing Scene Analysis ---");
            var existingSprites = GameObject.FindObjectsOfType<SpriteRenderer>()
                .Where(sr => sr.gameObject != characterRoot && !sr.transform.IsChildOf(characterRoot.transform))
                .ToArray();
                
            log.AppendLine($"Found {existingSprites.Length} existing sprite renderers");
            
            // Group by root object
            var rootGroups = existingSprites.GroupBy(sr => sr.transform.root);
            foreach (var group in rootGroups)
            {
                log.AppendLine($"\nRoot Object: {group.Key.name}");
                foreach (var sr in group.Take(10))
                {
                    if (sr.sprite != null)
                    {
                        log.AppendLine($"  {GetPath(sr.transform)}: {sr.sprite.name}");
                        log.AppendLine($"    Pos: {sr.transform.position}, FlipX: {sr.flipX}");
                    }
                }
                if (group.Count() > 10)
                {
                    log.AppendLine($"  ... and {group.Count() - 10} more");
                }
            }
            
            // Clean up
            GameObject.DestroyImmediate(characterRoot);
            
            // Write results
            File.WriteAllText(logPath, log.ToString());
            Debug.Log($"Test completed. Results written to: {logPath}");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            log.AppendLine($"ERROR: {e.Message}");
            log.AppendLine($"Stack trace: {e.StackTrace}");
            File.WriteAllText(logPath, log.ToString());
            EditorApplication.Exit(1);
        }
    }
    
    private static GameObject CreateSpritePart(GameObject parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPosition;
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        
        return go;
    }
    
    private static void SetFlipXRecursive(Transform root, bool flip)
    {
        var sprites = root.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            sr.flipX = flip;
        }
    }
    
    private static void LogTransformHierarchy(Transform root, System.Text.StringBuilder log, string indent)
    {
        var sr = root.GetComponent<SpriteRenderer>();
        log.AppendLine($"{indent}{root.name}:");
        log.AppendLine($"{indent}  World Pos: {root.position}");
        log.AppendLine($"{indent}  Local Pos: {root.localPosition}");
        log.AppendLine($"{indent}  Local Scale: {root.localScale}");
        
        if (sr != null)
        {
            log.AppendLine($"{indent}  FlipX: {sr.flipX}");
            log.AppendLine($"{indent}  Sprite: {sr.sprite?.name ?? "null"}");
        }
        
        foreach (Transform child in root)
        {
            LogTransformHierarchy(child, log, indent + "  ");
        }
    }
    
    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}