using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MapleClient.GameData;
using MapleClient.GameView;

/// <summary>
/// Focused test for verifying character body part attachment points
/// </summary>
public static class CharacterAttachmentTest
{
    [MenuItem("MapleUnity/Tests/Test Character Attachments")]
    public static void RunAttachmentTest()
    {
        Debug.Log("=== CHARACTER ATTACHMENT POINT TEST ===");
        
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
            
            Debug.Log($"\n--- Loaded {bodyParts.Count} body parts ---");
            foreach (var part in bodyParts.Keys)
            {
                Debug.Log($"  - {part}");
            }
            
            Debug.Log($"\n--- Found {attachmentPoints.Count} attachment points ---");
            foreach (var attachment in attachmentPoints)
            {
                Debug.Log($"  {attachment.Key}: {attachment.Value} (Unity: {attachment.Value / 100f})");
            }
            
            // Create test character to verify positioning
            var testChar = new GameObject("TestCharacter");
            testChar.transform.position = Vector3.zero;
            
            // Add MapleCharacterRenderer component
            var renderer = testChar.AddComponent<MapleCharacterRenderer>();
            
            // Let it initialize
            EditorApplication.delayCall += () => {
                Debug.Log("\n--- Analyzing sprite positions ---");
                
                // Find key body parts
                var body = testChar.transform.Find("body");
                var arm = testChar.transform.Find("arm");
                var head = testChar.transform.Find("head");
                var face = testChar.transform.Find("face") ?? testChar.transform.Find("head/face");
                
                // Log positions
                if (body != null)
                    Debug.Log($"Body position: {body.localPosition}");
                else
                    Debug.LogWarning("Body not found!");
                    
                if (arm != null)
                {
                    Debug.Log($"Arm position: {arm.localPosition}");
                    if (arm.localPosition.y < 0.1f)
                    {
                        Debug.LogError("ERROR: Arm is too low! Expected Y ~= 0.20");
                    }
                }
                else
                    Debug.LogWarning("Arm not found!");
                    
                if (head != null)
                {
                    Debug.Log($"Head position: {head.localPosition}");
                    if (head.localPosition.y < 0.2f)
                    {
                        Debug.LogError("ERROR: Head is too low! Expected Y >= 0.28");
                    }
                }
                else
                    Debug.LogWarning("Head not found!");
                    
                if (face != null)
                    Debug.Log($"Face position: {face.position} (world)");
                else
                    Debug.LogWarning("Face not found!");
                
                // Visual hierarchy summary
                Debug.Log("\n--- Expected vs Actual Positions ---");
                Debug.Log("Expected: Body at Y=0.00, Arm at Y=0.20, Head at Y=0.28+");
                Debug.Log($"Actual: Body={body?.localPosition.y ?? -1:F2}, Arm={arm?.localPosition.y ?? -1:F2}, Head={head?.localPosition.y ?? -1:F2}");
                
                // Cleanup
                GameObject.DestroyImmediate(testChar);
                Debug.Log("\n=== TEST COMPLETE ===");
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
        }
    }
    
    [MenuItem("MapleUnity/Tests/Test Attachment Points (Batch)")]
    public static void RunBatchAttachmentTest()
    {
        Debug.Log("=== BATCH ATTACHMENT TEST START ===");
        RunAttachmentTest();
        
        // Exit after a delay to allow the test to complete
        EditorApplication.delayCall += () => {
            EditorApplication.delayCall += () => {
                Debug.Log("=== BATCH TEST COMPLETE - EXITING ===");
                EditorApplication.Exit(0);
            };
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
        
        // Add body parts with mock sprites
        var bodyNode = new NxNode("body");
        bodyNode.Value = CreateMockSprite("body", 32, 48);
        frame0Node.AddChild(bodyNode);
        
        // Add origin for body (center-bottom)
        var bodyOriginNode = new NxNode("origin");
        bodyOriginNode.Value = new UnityEngine.Vector2(16, 48); // Center-bottom
        bodyNode.AddChild(bodyOriginNode);
        
        // Add body attachment point for head (neck)
        var bodyMapNode = new NxNode("map");
        bodyNode.AddChild(bodyMapNode);
        var neckNode = new NxNode("neck");
        neckNode.Value = new UnityEngine.Vector2(0, 28); // Head attachment at Y=28
        bodyMapNode.AddChild(neckNode);
        
        var armNode = new NxNode("arm");
        armNode.Value = CreateMockSprite("arm", 24, 32);
        frame0Node.AddChild(armNode);
        
        // Add origin for arm (should be positioned at Y=20)
        var armOriginNode = new NxNode("origin");
        armOriginNode.Value = new UnityEngine.Vector2(12, 16); // Center of arm
        armNode.AddChild(armOriginNode);
        
        var headNode = new NxNode("head");
        headNode.Value = CreateMockSprite("head", 32, 32);
        frame0Node.AddChild(headNode);
        
        // Add origin for head
        var headOriginNode = new NxNode("origin");
        headOriginNode.Value = new UnityEngine.Vector2(16, 16); // Center of head
        headNode.AddChild(headOriginNode);
        
        // Add face data
        var faceNode = new NxNode("Face");
        charRoot.AddChild(faceNode);
        
        var face20000Node = new NxNode("00020000.img");
        faceNode.AddChild(face20000Node);
        
        var defaultFaceNode = new NxNode("default");
        face20000Node.AddChild(defaultFaceNode);
        
        var faceFrame0Node = new NxNode("0");
        defaultFaceNode.AddChild(faceFrame0Node);
        
        var faceSprite = new NxNode("face");
        faceSprite.Value = CreateMockSprite("face", 28, 28);
        faceFrame0Node.AddChild(faceSprite);
        
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
            "head" => Color.red,
            "face" => Color.yellow,
            _ => Color.gray
        };
        
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = fillColor;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Convert to PNG byte array
        byte[] pngData = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        
        return pngData;
    }
}