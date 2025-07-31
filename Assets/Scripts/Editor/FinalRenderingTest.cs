using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Reflection;
using System.Linq;

public class FinalRenderingTest : MonoBehaviour
{
    [MenuItem("MapleUnity/Final Rendering Test")]
    public static void RunTest()
    {
        Debug.Log("=== FINAL CHARACTER RENDERING TEST ===");
        
        // Create clean scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create character
        GameObject charObj = new GameObject("Character");
        charObj.transform.position = Vector3.zero;
        
        var renderer = charObj.AddComponent<MapleCharacterRenderer>();
        renderer.SetCharacterAppearance(0, 20000, 30000);
        renderer.UpdateAppearance();
        
        // Add visual guides
        CreateVisualGuides(charObj.transform);
        
        // Analyze after frame
        EditorApplication.delayCall += () => {
            AnalyzeRendering(renderer);
            
            // Also test NX data directly
            TestNXData();
        };
    }
    
    private static void AnalyzeRendering(MapleCharacterRenderer renderer)
    {
        Debug.Log("\n=== CURRENT RENDERING STATE ===");
        
        var type = typeof(MapleCharacterRenderer);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        
        // Get all renderers
        var bodyR = type.GetField("bodyRenderer", flags).GetValue(renderer) as SpriteRenderer;
        var armR = type.GetField("armRenderer", flags).GetValue(renderer) as SpriteRenderer;
        var headR = type.GetField("headRenderer", flags).GetValue(renderer) as SpriteRenderer;
        var faceR = type.GetField("faceRenderer", flags).GetValue(renderer) as SpriteRenderer;
        
        if (bodyR?.sprite != null)
        {
            Debug.Log("\nBODY:");
            Debug.Log($"  Position: {bodyR.transform.localPosition}");
            Debug.Log($"  Bounds Y: {bodyR.bounds.min.y:F3} to {bodyR.bounds.max.y:F3}");
            Debug.Log($"  Pivot: {bodyR.sprite.pivot} (normalized: {bodyR.sprite.pivot.y/bodyR.sprite.rect.height:F3})");
        }
        
        if (armR?.sprite != null)
        {
            Debug.Log("\nARM:");
            Debug.Log($"  Position: {armR.transform.localPosition}");
            Debug.Log($"  Bounds Y: {armR.bounds.min.y:F3} to {armR.bounds.max.y:F3}");
            Debug.Log($"  [PROBLEM?] Arm at leg level if min.y near 0");
        }
        
        if (headR?.sprite != null)
        {
            Debug.Log("\nHEAD:");
            Debug.Log($"  Position: {headR.transform.localPosition}");
            Debug.Log($"  Bounds Y: {headR.bounds.min.y:F3} to {headR.bounds.max.y:F3}");
            Debug.Log($"  [PROBLEM?] Head at body level if overlaps body Y range");
        }
        
        if (faceR?.sprite != null)
        {
            Debug.Log("\nFACE:");
            Debug.Log($"  Position: {faceR.transform.localPosition}");
            Debug.Log($"  Bounds Y: {faceR.bounds.min.y:F3} to {faceR.bounds.max.y:F3}");
            Debug.Log($"  [PROBLEM?] Face outside head if not within head bounds");
        }
        
        Debug.Log("\n=== VISUAL CHECK ===");
        Debug.Log("Body should be at ground (Y=0)");
        Debug.Log("Arm should be mid-body (Y~0.15-0.25)");
        Debug.Log("Head should be above body (Y~0.25-0.35)");
        Debug.Log("Face should be within head bounds");
    }
    
    private static void TestNXData()
    {
        Debug.Log("\n=== TESTING NX DATA STRUCTURE ===");
        
        var charFile = NXAssetLoader.Instance.GetNxFile("character");
        if (charFile == null) return;
        
        // Check body/arm origins
        var bodyNode = charFile.GetNode("00002000.img/stand1/0/body");
        var armNode = charFile.GetNode("00002000.img/stand1/0/arm");
        
        if (bodyNode != null)
        {
            var origin = bodyNode["origin"];
            Debug.Log($"Body origin: {origin?.Value ?? "null"}");
            
            // Check for attachment points
            var map = bodyNode["map"];
            if (map != null)
            {
                Debug.Log("Body map nodes:");
                foreach (var child in map.Children)
                {
                    Debug.Log($"  {child.Name}: {child.Value}");
                }
            }
        }
        
        if (armNode != null)
        {
            var origin = armNode["origin"];
            Debug.Log($"Arm origin: {origin?.Value ?? "null"}");
        }
        
        // Check if head node exists at frame level
        var frameNode = charFile.GetNode("00002000.img/stand1/0");
        if (frameNode != null)
        {
            var headNode = frameNode["head"];
            Debug.Log($"Frame-level head node: {headNode?.Value ?? "null"}");
        }
    }
    
    private static void CreateVisualGuides(Transform parent)
    {
        // Create height reference lines
        for (float y = 0; y <= 0.6f; y += 0.1f)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"Y={y:F1}";
            line.transform.SetParent(parent);
            line.transform.localPosition = new Vector3(0, y, -0.5f);
            line.transform.localScale = new Vector3(2, 0.01f, 0.01f);
            line.GetComponent<Renderer>().material.color = y == 0 ? Color.red : Color.gray;
            DestroyImmediate(line.GetComponent<Collider>());
        }
    }
}