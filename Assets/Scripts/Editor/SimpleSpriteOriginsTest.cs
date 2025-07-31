using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using MapleClient.GameView;
using System.Reflection;

public static class SimpleSpriteOriginsTest
{
    public static void RunTest()
    {
        Debug.Log("=== SPRITE ORIGINS TEST STARTING ===");
        
        try
        {
            // Create a test character
            GameObject charObj = new GameObject("TestCharacter");
            var renderer = charObj.AddComponent<MapleCharacterRenderer>();
            
            Debug.Log("Created MapleCharacterRenderer");
            
            // Set appearance
            renderer.SetCharacterAppearance(0, 20000, 30000);
            renderer.UpdateAppearance();
            
            Debug.Log("Set character appearance");
            
            // Get sprite renderers via reflection
            var type = typeof(MapleCharacterRenderer);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            
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
                        Debug.Log($"\n=== {partName.ToUpper()} ===");
                        Debug.Log($"Local Position: {sr.transform.localPosition}");
                        Debug.Log($"Sprite Name: {sprite.name}");
                        Debug.Log($"Texture Size: {sprite.texture.width}x{sprite.texture.height}");
                        Debug.Log($"Sprite Rect: {sprite.rect}");
                        Debug.Log($"Pivot (pixels): {sprite.pivot}");
                        Debug.Log($"Pivot (normalized): ({sprite.pivot.x/sprite.rect.width:F3}, {sprite.pivot.y/sprite.rect.height:F3})");
                        Debug.Log($"Pixels Per Unit: {sprite.pixelsPerUnit}");
                        
                        var bounds = sr.bounds;
                        Debug.Log($"World Bounds: min=({bounds.min.x:F3}, {bounds.min.y:F3}), max=({bounds.max.x:F3}, {bounds.max.y:F3})");
                        Debug.Log($"Bounds Size: ({bounds.size.x:F3}, {bounds.size.y:F3})");
                    }
                    else
                    {
                        Debug.Log($"{partName}: No sprite loaded");
                    }
                }
            }
            
            // Check NX data directly
            Debug.Log("\n=== CHECKING NX DATA ===");
            var charFile = NXAssetLoader.Instance.GetNxFile("character");
            if (charFile != null)
            {
                // Check body origin
                var bodyNode = charFile.GetNode("00002000.img/stand1/0/body");
                if (bodyNode != null && bodyNode["origin"] != null)
                {
                    Debug.Log($"Body origin from NX: {bodyNode["origin"].Value}");
                }
                
                // Check face path
                var faceNode = charFile.GetNode("Face/00020000.img/default/face");
                if (faceNode != null && faceNode["origin"] != null)
                {
                    Debug.Log($"Face origin from NX: {faceNode["origin"].Value}");
                }
            }
            
            // Clean up
            Object.DestroyImmediate(charObj);
            
            Debug.Log("\n=== TEST COMPLETED SUCCESSFULLY ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}