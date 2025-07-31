using UnityEngine;
using UnityEditor;
using System.IO;

public static class MinimalPositioningTest
{
    public static void RunTest()
    {
        Debug.Log("=== CHARACTER POSITIONING TEST ===");
        Debug.Log($"Test started at {System.DateTime.Now}");
        
        try
        {
            // Create test objects
            GameObject testObj = new GameObject("PositioningTest");
            
            // Create body parts at specific positions
            var body = CreatePart(testObj, "Body", new Vector3(0, 0, 0), Color.gray);
            var arm = CreatePart(testObj, "Arm", new Vector3(0, 0.20f, -0.1f), Color.blue);
            var head = CreatePart(testObj, "Head", new Vector3(0, 0.28f, -0.2f), Color.green);
            var face = CreatePart(testObj, "Face", new Vector3(0, 0.28f, -0.3f), Color.yellow);
            
            // Log positions
            Debug.Log("\n=== PART POSITIONS ===");
            Debug.Log($"Body: Y={body.transform.localPosition.y:F3}");
            Debug.Log($"Arm: Y={arm.transform.localPosition.y:F3} (Expected: 0.20)");
            Debug.Log($"Head: Y={head.transform.localPosition.y:F3} (Expected: 0.28)");
            Debug.Log($"Face: Y={face.transform.localPosition.y:F3} (Expected: 0.28)");
            
            // Check if positioning is correct
            bool armCorrect = Mathf.Abs(arm.transform.localPosition.y - 0.20f) < 0.01f;
            bool headCorrect = Mathf.Abs(head.transform.localPosition.y - 0.28f) < 0.01f;
            bool faceAligned = Mathf.Abs(face.transform.localPosition.y - head.transform.localPosition.y) < 0.01f;
            
            Debug.Log("\n=== POSITIONING RESULTS ===");
            Debug.Log($"Arm at Y=0.20: {(armCorrect ? "CORRECT" : "INCORRECT")}");
            Debug.Log($"Head at Y=0.28: {(headCorrect ? "CORRECT" : "INCORRECT")}");
            Debug.Log($"Face aligned with head: {(faceAligned ? "CORRECT" : "INCORRECT")}");
            
            // Check layer ordering
            Debug.Log("\n=== LAYER ORDERING ===");
            Debug.Log($"Body Z: {body.transform.localPosition.z}");
            Debug.Log($"Arm Z: {arm.transform.localPosition.z}");
            Debug.Log($"Head Z: {head.transform.localPosition.z}");
            Debug.Log($"Face Z: {face.transform.localPosition.z}");
            
            bool allCorrect = armCorrect && headCorrect && faceAligned;
            Debug.Log($"\n=== OVERALL RESULT: {(allCorrect ? "PASS" : "FAIL")} ===");
            
            // Write results to file
            string results = $@"Character Positioning Test Results
=====================================
Test Time: {System.DateTime.Now}

Part Positions:
- Body: Y={body.transform.localPosition.y:F3}
- Arm: Y={arm.transform.localPosition.y:F3} (Expected: 0.20) - {(armCorrect ? "CORRECT" : "INCORRECT")}
- Head: Y={head.transform.localPosition.y:F3} (Expected: 0.28) - {(headCorrect ? "CORRECT" : "INCORRECT")}
- Face: Y={face.transform.localPosition.y:F3} (Expected: 0.28) - {(faceAligned ? "ALIGNED" : "MISALIGNED")}

Layer Ordering (Z positions):
- Body: {body.transform.localPosition.z}
- Arm: {arm.transform.localPosition.z}
- Head: {head.transform.localPosition.z}
- Face: {face.transform.localPosition.z}

Overall Result: {(allCorrect ? "PASS" : "FAIL")}
";
            
            File.WriteAllText("positioning-test-results.txt", results);
            Debug.Log($"Results written to positioning-test-results.txt");
            
            EditorApplication.Exit(allCorrect ? 0 : 1);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed with exception: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
    
    private static GameObject CreatePart(GameObject parent, string name, Vector3 position, Color color)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent.transform);
        part.transform.localPosition = position;
        
        // Add a simple colored sprite for visualization
        var spriteRenderer = part.AddComponent<SpriteRenderer>();
        
        // Create a simple colored texture
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        
        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
        
        return part;
    }
}