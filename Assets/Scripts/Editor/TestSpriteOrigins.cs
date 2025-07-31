using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using MapleClient.GameView;
using System.Reflection;
using System.Linq;

public class TestSpriteOrigins
{
    [MenuItem("MapleUnity/Test Sprite Origins")]
    public static void TestOrigins()
    {
        Debug.Log("=== TESTING SPRITE ORIGINS ===");
        
        try
        {
            // Create a test character
            GameObject charObj = new GameObject("TestCharacter");
            var renderer = charObj.AddComponent<MapleCharacterRenderer>();
            renderer.SetCharacterAppearance(0, 20000, 30000);
            renderer.UpdateAppearance();
            
            // Analyze immediately in batch mode
            AnalyzeCharacterSprites(renderer);
            
            // Clean up
            Object.DestroyImmediate(charObj);
            
            Debug.Log("=== TEST COMPLETED SUCCESSFULLY ===");
            
            // Exit Unity in batch mode
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
    
    private static void AnalyzeCharacterSprites(MapleCharacterRenderer renderer)
    {
        var type = typeof(MapleCharacterRenderer);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        
        Debug.Log("\n=== SPRITE ANALYSIS ===");
        
        string[] parts = { "bodyRenderer", "armRenderer", "headRenderer", "faceRenderer" };
        
        foreach (var partName in parts)
        {
            var field = type.GetField(partName, flags);
            if (field != null)
            {
                var sr = field.GetValue(renderer) as SpriteRenderer;
                if (sr != null && sr.sprite != null)
                {
                    var sprite = sr.sprite;
                    Debug.Log($"\n{partName}:");
                    Debug.Log($"  Local Position: {sr.transform.localPosition}");
                    Debug.Log($"  Sprite Name: {sprite.name}");
                    Debug.Log($"  Texture Size: {sprite.texture.width}x{sprite.texture.height}");
                    Debug.Log($"  Sprite Rect: {sprite.rect}");
                    Debug.Log($"  Pivot (pixels): {sprite.pivot}");
                    Debug.Log($"  Pivot (normalized): ({sprite.pivot.x/sprite.rect.width:F3}, {sprite.pivot.y/sprite.rect.height:F3})");
                    Debug.Log($"  Pixels Per Unit: {sprite.pixelsPerUnit}");
                    
                    // Calculate where the sprite appears
                    var bounds = sr.bounds;
                    Debug.Log($"  World Bounds: min=({bounds.min.x:F3}, {bounds.min.y:F3}), max=({bounds.max.x:F3}, {bounds.max.y:F3})");
                    
                    // Check if this looks correct
                    if (partName == "bodyRenderer")
                    {
                        Debug.Log($"  [BODY CHECK] Bottom at Y={bounds.min.y:F3}, should be near 0");
                    }
                    else if (partName == "headRenderer")
                    {
                        Debug.Log($"  [HEAD CHECK] Bottom at Y={bounds.min.y:F3}, should be above body");
                    }
                    else if (partName == "faceRenderer")
                    {
                        Debug.Log($"  [FACE CHECK] Should be inside head bounds");
                    }
                }
            }
        }
        
        // Test direct NX loading to verify origins
        Debug.Log("\n=== VERIFYING NX DATA ===");
        var charFile = NXAssetLoader.Instance.GetNxFile("character");
        if (charFile != null)
        {
            // Check body origin
            var bodyNode = charFile.GetNode("00002000.img/stand1/0/body");
            if (bodyNode != null)
            {
                var originNode = bodyNode["origin"];
                if (originNode != null && originNode.Value != null)
                {
                    Debug.Log($"Body origin from NX: {originNode.Value}");
                }
            }
            
            // Check face structure
            var faceNode = charFile.GetNode("Face/00020000.img/default");
            if (faceNode != null)
            {
                Debug.Log($"Face node path: Face/00020000.img/default");
                if (faceNode.Children != null)
                {
                    Debug.Log($"Face node has {faceNode.Children.Count()} children");
                    foreach (var child in faceNode.Children)
                    {
                        Debug.Log($"  Face child: {child.Name}");
                        if (child.Name == "face" && child["origin"] != null)
                        {
                            Debug.Log($"    Face origin: {child["origin"].Value}");
                        }
                    }
                }
                else
                {
                    Debug.Log("Face node has no children collection");
                }
            }
            else
            {
                Debug.Log("Face node not found at Face/00020000.img/default");
            }
        }
    }
}