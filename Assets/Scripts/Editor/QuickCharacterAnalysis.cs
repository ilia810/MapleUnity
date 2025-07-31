using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class QuickCharacterAnalysis
{
    public static void RunTest()
    {
        string logPath = Path.Combine(Application.dataPath, "..", "quick-character-analysis.log");
        
        try
        {
            File.WriteAllText(logPath, "=== QUICK CHARACTER ANALYSIS ===\n");
            File.AppendAllText(logPath, $"Time: {System.DateTime.Now}\n");
            File.AppendAllText(logPath, $"Unity Version: {Application.unityVersion}\n\n");
            
            // Load henesys scene
            File.AppendAllText(logPath, "Loading henesys.unity scene...\n");
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(logPath, $"Scene loaded: {scene.name}\n\n");
            
            // Find all GameObjects with SpriteRenderers
            var allSprites = GameObject.FindObjectsOfType<SpriteRenderer>();
            File.AppendAllText(logPath, $"Total SpriteRenderers found: {allSprites.Length}\n\n");
            
            // Group by root GameObject
            var rootObjects = new System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<SpriteRenderer>>();
            
            foreach (var sr in allSprites)
            {
                var root = sr.transform.root.gameObject;
                if (!rootObjects.ContainsKey(root))
                {
                    rootObjects[root] = new System.Collections.Generic.List<SpriteRenderer>();
                }
                rootObjects[root].Add(sr);
            }
            
            File.AppendAllText(logPath, $"Found {rootObjects.Count} root GameObjects with sprites\n\n");
            
            // Analyze each root object
            foreach (var kvp in rootObjects)
            {
                var root = kvp.Key;
                var sprites = kvp.Value;
                
                File.AppendAllText(logPath, $"=== GameObject: {root.name} ===\n");
                File.AppendAllText(logPath, $"Position: {root.transform.position}\n");
                File.AppendAllText(logPath, $"Child sprites: {sprites.Count}\n");
                
                // Check for MapleCharacterRenderer
                var charRenderer = root.GetComponentInChildren<MapleClient.GameView.MapleCharacterRenderer>();
                if (charRenderer != null)
                {
                    File.AppendAllText(logPath, "HAS MapleCharacterRenderer!\n");
                    AnalyzeCharacterRenderer(charRenderer, logPath);
                }
                
                // Show first few sprites
                int count = 0;
                foreach (var sr in sprites)
                {
                    if (count++ >= 10) break;
                    
                    File.AppendAllText(logPath, $"\n  Sprite: {GetPath(sr.transform)}\n");
                    File.AppendAllText(logPath, $"    Local Pos: {sr.transform.localPosition}\n");
                    File.AppendAllText(logPath, $"    World Pos: {sr.transform.position}\n");
                    File.AppendAllText(logPath, $"    Scale: {sr.transform.localScale}\n");
                    File.AppendAllText(logPath, $"    FlipX: {sr.flipX}\n");
                    if (sr.sprite != null)
                    {
                        File.AppendAllText(logPath, $"    Sprite: {sr.sprite.name}\n");
                        File.AppendAllText(logPath, $"    Size: {sr.sprite.rect.width}x{sr.sprite.rect.height}\n");
                        File.AppendAllText(logPath, $"    Pivot: {sr.sprite.pivot}\n");
                    }
                }
                
                if (sprites.Count > 10)
                {
                    File.AppendAllText(logPath, $"\n  ... and {sprites.Count - 10} more sprites\n");
                }
                
                File.AppendAllText(logPath, "\n");
            }
            
            File.AppendAllText(logPath, "\n=== TEST COMPLETED ===\n");
            Debug.Log($"Test completed. Results written to: {logPath}");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nERROR: {e.Message}\n");
            File.AppendAllText(logPath, $"Stack trace: {e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void AnalyzeCharacterRenderer(MapleClient.GameView.MapleCharacterRenderer renderer, string logPath)
    {
        File.AppendAllText(logPath, "\n--- Character Renderer Analysis ---\n");
        
        // Get all child sprites
        var childSprites = renderer.GetComponentsInChildren<SpriteRenderer>();
        File.AppendAllText(logPath, $"Total child sprites: {childSprites.Length}\n");
        
        // Look for key body parts
        foreach (var sr in childSprites)
        {
            string name = sr.gameObject.name.ToLower();
            if (name.Contains("body") || name.Contains("arm") || name.Contains("head") || 
                name.Contains("face") || name.Contains("hair"))
            {
                File.AppendAllText(logPath, $"\n  {sr.gameObject.name}:\n");
                File.AppendAllText(logPath, $"    Local Pos: {sr.transform.localPosition}\n");
                File.AppendAllText(logPath, $"    FlipX: {sr.flipX}, Scale.x: {sr.transform.localScale.x}\n");
            }
        }
        
        // Test flipping using reflection
        File.AppendAllText(logPath, "\n--- Testing Direction Changes ---\n");
        var type = renderer.GetType();
        var setFlipXMethod = type.GetMethod("SetFlipX", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (setFlipXMethod != null)
        {
            // Test facing right
            setFlipXMethod.Invoke(renderer, new object[] { false });
            File.AppendAllText(logPath, "\nSet facing RIGHT (flipX = false):\n");
            LogKeySprites(renderer, logPath);
            
            // Test facing left
            setFlipXMethod.Invoke(renderer, new object[] { true });
            File.AppendAllText(logPath, "\nSet facing LEFT (flipX = true):\n");
            LogKeySprites(renderer, logPath);
        }
        else
        {
            File.AppendAllText(logPath, "ERROR: SetFlipX method not found!\n");
        }
    }
    
    private static void LogKeySprites(MapleClient.GameView.MapleCharacterRenderer renderer, string logPath)
    {
        var sprites = renderer.GetComponentsInChildren<SpriteRenderer>();
        string[] keyParts = { "body", "arm", "head", "face" };
        
        foreach (string partName in keyParts)
        {
            foreach (var sr in sprites)
            {
                if (sr.gameObject.name.ToLower().Contains(partName))
                {
                    File.AppendAllText(logPath, $"  {sr.gameObject.name}: flipX={sr.flipX}, scale.x={sr.transform.localScale.x:F2}, pos={sr.transform.localPosition}\n");
                    break;
                }
            }
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