using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;

public class AnalyzeRenderingIssue : MonoBehaviour
{
    [MenuItem("MapleUnity/Analyze Rendering Issue")]
    public static void Analyze()
    {
        Debug.Log("=== ANALYZING CHARACTER RENDERING ISSUE ===");
        
        // Create test scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Create character at origin
        GameObject charObj = new GameObject("TestCharacter");
        charObj.transform.position = Vector3.zero;
        
        // Add MapleCharacterRenderer
        var renderer = charObj.AddComponent<MapleCharacterRenderer>();
        renderer.SetCharacterAppearance(0, 20000, 30000);
        renderer.UpdateAppearance();
        
        // Let Unity update then analyze
        EditorApplication.delayCall += () => {
            AnalyzeCharacter(renderer);
        };
    }
    
    private static void AnalyzeCharacter(MapleCharacterRenderer renderer)
    {
        var type = typeof(MapleCharacterRenderer);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        Debug.Log("\n=== ACTUAL SPRITE POSITIONS ===");
        
        // Check key renderers
        string[] parts = { "bodyRenderer", "armRenderer", "headRenderer", "faceRenderer" };
        
        foreach (var partName in parts)
        {
            var field = type.GetField(partName, flags);
            if (field != null)
            {
                var sr = field.GetValue(renderer) as SpriteRenderer;
                if (sr != null && sr.sprite != null)
                {
                    Debug.Log($"\n{partName}:");
                    Debug.Log($"  Local Position: {sr.transform.localPosition}");
                    Debug.Log($"  World Position: {sr.transform.position}");
                    
                    var bounds = sr.bounds;
                    Debug.Log($"  Bounds Min: {bounds.min}");
                    Debug.Log($"  Bounds Max: {bounds.max}");
                    Debug.Log($"  Y Range: {bounds.min.y:F3} to {bounds.max.y:F3}");
                    
                    // Check overlap
                    if (partName == "armRenderer")
                    {
                        var bodyField = type.GetField("bodyRenderer", flags);
                        var bodySR = bodyField.GetValue(renderer) as SpriteRenderer;
                        if (bodySR != null && bodySR.sprite != null)
                        {
                            var bodyBounds = bodySR.bounds;
                            Debug.Log($"  [ARM vs BODY] Body Y: {bodyBounds.min.y:F3} to {bodyBounds.max.y:F3}");
                            Debug.Log($"  [ARM vs BODY] Arm overlaps body from Y={Mathf.Max(bounds.min.y, bodyBounds.min.y):F3} to Y={Mathf.Min(bounds.max.y, bodyBounds.max.y):F3}");
                        }
                    }
                }
            }
        }
        
        Debug.Log("\n=== VISUAL ANALYSIS ===");
        Debug.Log("If head is below body: Head Y range should be higher than body Y range");
        Debug.Log("If arm is near legs: Arm min Y should be close to body min Y");
        Debug.Log("If eyes too high: Face Y should match head Y");
        
        // Create debug visualization
        CreateDebugMarkers(renderer.transform);
    }
    
    private static void CreateDebugMarkers(Transform parent)
    {
        // Ground line
        CreateLine(parent, new Vector3(-1, 0, -0.1f), new Vector3(1, 0, -0.1f), Color.red, "Ground Y=0");
        
        // Expected ranges
        CreateLine(parent, new Vector3(-0.8f, 0.31f, -0.1f), new Vector3(0.8f, 0.31f, -0.1f), Color.green, "Body Top");
        CreateLine(parent, new Vector3(-0.8f, 0.28f, -0.1f), new Vector3(0.8f, 0.28f, -0.1f), Color.blue, "Head Bottom");
        CreateLine(parent, new Vector3(-0.8f, 0.58f, -0.1f), new Vector3(0.8f, 0.58f, -0.1f), Color.cyan, "Head Top");
    }
    
    private static void CreateLine(Transform parent, Vector3 start, Vector3 end, Color color, string name)
    {
        var line = new GameObject(name);
        line.transform.SetParent(parent);
        var lr = line.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = 0.005f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}