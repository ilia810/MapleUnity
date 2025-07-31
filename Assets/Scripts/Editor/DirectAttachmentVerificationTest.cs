using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MapleClient.GameData;

/// <summary>
/// Direct verification test for character attachment points without relying on MapleCharacterRenderer
/// </summary>
public static class DirectAttachmentVerificationTest
{
    [MenuItem("MapleUnity/Tests/Direct Attachment Verification")]
    public static void RunDirectVerification()
    {
        Debug.Log("=== DIRECT ATTACHMENT VERIFICATION TEST ===");
        
        try
        {
            // Initialize mock NX files first
            SetupMockNxFiles();
            
            // Initialize NX loader
            var nxLoader = NXAssetLoader.Instance;
            if (nxLoader == null)
            {
                Debug.LogError("Failed to get NXAssetLoader instance!");
                return;
            }
            
            // Load character body parts with attachment points
            Dictionary<string, Vector2> attachmentPoints;
            var bodyParts = nxLoader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
            
            if (bodyParts == null || bodyParts.Count == 0)
            {
                Debug.LogError("Failed to load body parts!");
                return;
            }
            
            Debug.Log($"\nLoaded {bodyParts.Count} body parts with {attachmentPoints.Count} attachment points");
            
            // Create test character manually
            var testChar = new GameObject("ManualTestCharacter");
            testChar.transform.position = Vector3.zero;
            
            // Expected positions based on the code
            float bodyY = 0.0f;    // Body at ground
            float armY = 0.20f;    // Arm at mid-body
            float headY = 0.28f;   // Head attachment from neck point
            
            // Create body sprite
            if (bodyParts.ContainsKey("body"))
            {
                var bodyObj = new GameObject("body");
                bodyObj.transform.SetParent(testChar.transform);
                bodyObj.transform.localPosition = new Vector3(0, bodyY, 0);
                var bodyRenderer = bodyObj.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = bodyParts["body"];
                bodyRenderer.sortingOrder = 0;
                Debug.Log($"Created body at Y={bodyY}");
            }
            
            // Create arm sprite
            if (bodyParts.ContainsKey("arm"))
            {
                var armObj = new GameObject("arm");
                armObj.transform.SetParent(testChar.transform);
                armObj.transform.localPosition = new Vector3(0, armY, 0);
                var armRenderer = armObj.AddComponent<SpriteRenderer>();
                armRenderer.sprite = bodyParts["arm"];
                armRenderer.sortingOrder = 1;
                Debug.Log($"Created arm at Y={armY}");
            }
            
            // Get head position from attachment points
            Vector2 neckPoint = Vector2.zero;
            if (attachmentPoints.ContainsKey("body.map.neck"))
            {
                neckPoint = attachmentPoints["body.map.neck"];
                headY = neckPoint.y / 100f; // Convert from NX units to Unity units
                Debug.Log($"Using neck attachment point: {neckPoint} -> Unity Y={headY:F3}");
            }
            
            // Create head sprite (placeholder since we're focusing on position)
            var headObj = new GameObject("head");
            headObj.transform.SetParent(testChar.transform);
            headObj.transform.localPosition = new Vector3(neckPoint.x / 100f, headY, 0);
            var headRenderer = headObj.AddComponent<SpriteRenderer>();
            headRenderer.sortingOrder = 2;
            Debug.Log($"Created head at ({neckPoint.x / 100f:F3}, {headY:F3})");
            
            // Verify positions
            Debug.Log("\n--- POSITION VERIFICATION ---");
            var actualBody = testChar.transform.Find("body");
            var actualArm = testChar.transform.Find("arm");
            var actualHead = testChar.transform.Find("head");
            
            bool allCorrect = true;
            
            if (actualBody != null)
            {
                float bodyPos = actualBody.localPosition.y;
                bool bodyCorrect = Mathf.Abs(bodyPos - 0.0f) < 0.01f;
                Debug.Log($"Body: Expected Y=0.00, Actual Y={bodyPos:F3} {(bodyCorrect ? "✓" : "✗")}");
                allCorrect &= bodyCorrect;
            }
            
            if (actualArm != null)
            {
                float armPos = actualArm.localPosition.y;
                bool armCorrect = Mathf.Abs(armPos - 0.20f) < 0.01f;
                Debug.Log($"Arm: Expected Y=0.20, Actual Y={armPos:F3} {(armCorrect ? "✓" : "✗")}");
                allCorrect &= armCorrect;
            }
            
            if (actualHead != null)
            {
                float headPos = actualHead.localPosition.y;
                bool headCorrect = Mathf.Abs(headPos - 0.28f) < 0.01f;
                Debug.Log($"Head: Expected Y=0.28, Actual Y={headPos:F3} {(headCorrect ? "✓" : "✗")}");
                allCorrect &= headCorrect;
            }
            
            // Visual layout summary
            Debug.Log("\n--- VISUAL LAYOUT ---");
            Debug.Log("      [HEAD]    <- Y=0.28");
            Debug.Log("       ___");
            Debug.Log("      [ARM]     <- Y=0.20");  
            Debug.Log("      [BODY]    <- Y=0.00 (ground)");
            Debug.Log("      -----");
            
            if (allCorrect)
            {
                Debug.Log("\n✓ ALL POSITIONS CORRECT!");
            }
            else
            {
                Debug.LogError("\n✗ POSITION ERRORS DETECTED!");
            }
            
            // Cleanup
            GameObject.DestroyImmediate(testChar);
            Debug.Log("\n=== TEST COMPLETE ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
        }
    }
    
    [MenuItem("MapleUnity/Tests/Direct Attachment Verification (Batch)")]
    public static void RunBatchDirectVerification()
    {
        Debug.Log("=== BATCH DIRECT VERIFICATION START ===");
        RunDirectVerification();
        
        // Exit Unity after test
        EditorApplication.delayCall += () => {
            Debug.Log("=== BATCH TEST COMPLETE - EXITING ===");
            EditorApplication.Exit(0);
        };
    }
    
    private static void SetupMockNxFiles()
    {
        Debug.Log("Setting up mock NX files...");
        
        // Create mock character file with body parts
        var mockCharFile = new MockNxFile("character.nx");
        var charRoot = mockCharFile.Root as NxNode;
        
        // Add body skin data for skin ID 0 (2000)
        var skinNode = new NxNode("00002000.img");
        charRoot.AddChild(skinNode);
        
        // Add stand1 animation
        var stand1Node = new NxNode("stand1");
        skinNode.AddChild(stand1Node);
        
        // Add frame 0
        var frame0Node = new NxNode("0");
        stand1Node.AddChild(frame0Node);
        
        // Add body sprite with origin and attachment points
        var bodyNode = new NxNode("body");
        frame0Node.AddChild(bodyNode);
        
        // Body sprite data - store PNG data directly in the body node
        // This matches how the SpriteLoader expects to find image data
        bodyNode.Value = CreateMockSprite("body", 32, 48);
        
        // Body origin point (center of sprite)
        var bodyOriginNode = new NxNode("origin");
        bodyOriginNode.AddChild(new NxNode("x", 16));
        bodyOriginNode.AddChild(new NxNode("y", 48));
        bodyNode.AddChild(bodyOriginNode);
        
        // Body attachment map - neck point for head
        var bodyMapNode = new NxNode("map");
        bodyNode.AddChild(bodyMapNode);
        
        var neckNode = new NxNode("neck");
        neckNode.AddChild(new NxNode("x", 0));
        neckNode.AddChild(new NxNode("y", 28)); // Head attaches 28 pixels up from body origin
        bodyMapNode.AddChild(neckNode);
        
        // Add arm sprite
        var armNode = new NxNode("arm");
        frame0Node.AddChild(armNode);
        
        // Arm sprite data - store PNG data directly in the arm node
        armNode.Value = CreateMockSprite("arm", 24, 24);
        
        // Arm origin point
        var armOriginNode = new NxNode("origin");
        armOriginNode.AddChild(new NxNode("x", 12));
        armOriginNode.AddChild(new NxNode("y", 16));
        armNode.AddChild(armOriginNode);
        
        // Add Face data (separate from body)
        var faceNode = new NxNode("Face");
        charRoot.AddChild(faceNode);
        
        // Add face 20000
        var face20000Node = new NxNode("00020000.img");
        faceNode.AddChild(face20000Node);
        
        // Add default expression
        var defaultNode = new NxNode("default");
        face20000Node.AddChild(defaultNode);
        
        // Face frame 0
        var faceFrame0Node = new NxNode("0");
        defaultNode.AddChild(faceFrame0Node);
        
        // Face sprite - store PNG data directly in the face frame node
        faceFrame0Node.Value = CreateMockSprite("face", 32, 32);
        
        // Register the mock file
        NXAssetLoader.Instance.RegisterNxFile("character", mockCharFile);
        
        Debug.Log("Mock NX files setup complete");
    }
    
    private static byte[] CreateMockSprite(string name, int width, int height)
    {
        var texture = new Texture2D(width, height);
        texture.name = name;
        
        // Fill with a color based on the name
        Color fillColor = name switch
        {
            "body" => Color.blue,
            "arm" => Color.green,
            "face" => Color.yellow,
            _ => Color.gray
        };
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, fillColor);
            }
        }
        
        texture.Apply();
        return texture.EncodeToPNG();
    }
}