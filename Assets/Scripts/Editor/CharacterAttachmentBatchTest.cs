using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using MapleClient.GameData;
using MapleClient.GameView;

/// <summary>
/// Batch mode test for character attachment points that runs synchronously
/// </summary>
public static class CharacterAttachmentBatchTest
{
    public static void RunTest()
    {
        string logPath = @"C:\Users\me\MapleUnity\character-attachment-test.log";
        
        try
        {
            File.WriteAllText(logPath, "=== CHARACTER ATTACHMENT BATCH TEST ===\n");
            File.AppendAllText(logPath, $"Started at: {System.DateTime.Now}\n\n");
            
            // Setup mock NX files
            File.AppendAllText(logPath, "Setting up mock NX files...\n");
            SetupMockNxFiles();
            
            // Initialize NX loader
            var nxLoader = NXAssetLoader.Instance;
            if (nxLoader == null)
            {
                File.AppendAllText(logPath, "ERROR: Failed to get NXAssetLoader instance!\n");
                EditorApplication.Exit(1);
                return;
            }
            
            // Load character body parts with attachment points
            Dictionary<string, Vector2> attachmentPoints;
            var bodyParts = nxLoader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
            
            if (bodyParts == null || bodyParts.Count == 0)
            {
                File.AppendAllText(logPath, "ERROR: Failed to load body parts!\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, $"\n--- Loaded {bodyParts.Count} body parts ---\n");
            foreach (var part in bodyParts.Keys)
            {
                File.AppendAllText(logPath, $"  - {part}\n");
            }
            
            File.AppendAllText(logPath, $"\n--- Found {attachmentPoints.Count} attachment points ---\n");
            foreach (var attachment in attachmentPoints)
            {
                File.AppendAllText(logPath, $"  {attachment.Key}: {attachment.Value} (Unity: {attachment.Value / 100f})\n");
            }
            
            // Create test character to verify positioning
            File.AppendAllText(logPath, "\n--- Creating Test Character ---\n");
            var testChar = new GameObject("TestCharacter");
            testChar.transform.position = Vector3.zero;
            
            // Add MapleCharacterRenderer component
            var renderer = testChar.AddComponent<MapleCharacterRenderer>();
            File.AppendAllText(logPath, "Added MapleCharacterRenderer component\n");
            
            // Force initialization by calling Start via reflection
            var startMethod = renderer.GetType().GetMethod("Start", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (startMethod != null)
            {
                startMethod.Invoke(renderer, null);
                File.AppendAllText(logPath, "Called Start() on renderer\n");
            }
            
            // Analyze sprite positions immediately
            File.AppendAllText(logPath, "\n--- Analyzing sprite positions ---\n");
            
            // Find key body parts
            var body = testChar.transform.Find("body");
            var arm = testChar.transform.Find("arm");
            var head = testChar.transform.Find("head");
            var face = testChar.transform.Find("face") ?? testChar.transform.Find("head/face");
            
            // Log positions
            if (body != null)
                File.AppendAllText(logPath, $"Body position: {body.localPosition}\n");
            else
                File.AppendAllText(logPath, "Body not found!\n");
                
            if (arm != null)
            {
                File.AppendAllText(logPath, $"Arm position: {arm.localPosition}\n");
                if (arm.localPosition.y < 0.1f)
                {
                    File.AppendAllText(logPath, "ERROR: Arm is too low! Expected Y ~= 0.20\n");
                }
            }
            else
                File.AppendAllText(logPath, "Arm not found!\n");
                
            if (head != null)
            {
                File.AppendAllText(logPath, $"Head position: {head.localPosition}\n");
                if (head.localPosition.y < 0.2f)
                {
                    File.AppendAllText(logPath, "ERROR: Head is too low! Expected Y >= 0.28\n");
                }
            }
            else
                File.AppendAllText(logPath, "Head not found!\n");
                
            if (face != null)
                File.AppendAllText(logPath, $"Face position: {face.position} (world)\n");
            else
                File.AppendAllText(logPath, "Face not found!\n");
            
            // Visual hierarchy summary
            File.AppendAllText(logPath, "\n--- Expected vs Actual Positions ---\n");
            File.AppendAllText(logPath, "Expected: Body at Y=0.00, Arm at Y=0.20, Head at Y=0.28+\n");
            File.AppendAllText(logPath, $"Actual: Body={body?.localPosition.y ?? -1:F2}, Arm={arm?.localPosition.y ?? -1:F2}, Head={head?.localPosition.y ?? -1:F2}\n");
            
            // Check if positions are correct
            bool hasErrors = false;
            if (arm != null && arm.localPosition.y < 0.1f)
            {
                File.AppendAllText(logPath, "\nISSUE DETECTED: Arm is positioned too low (at leg level)\n");
                hasErrors = true;
            }
            
            if (head != null && body != null && head.localPosition.y <= body.localPosition.y)
            {
                File.AppendAllText(logPath, "\nISSUE DETECTED: Head is not above body\n");
                hasErrors = true;
            }
            
            // Cleanup
            GameObject.DestroyImmediate(testChar);
            
            File.AppendAllText(logPath, $"\n=== TEST COMPLETE - {(hasErrors ? "ISSUES FOUND" : "SUCCESS")} ===\n");
            File.AppendAllText(logPath, $"Ended at: {System.DateTime.Now}\n");
            
            Debug.Log($"Character attachment test complete - {(hasErrors ? "ISSUES FOUND" : "SUCCESS")}");
            EditorApplication.Exit(hasErrors ? 1 : 0);
        }
        catch (System.Exception e)
        {
            File.AppendAllText(logPath, $"\nEXCEPTION: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void SetupMockNxFiles()
    {
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
        bodyOriginNode.Value = new Vector2(16, 48); // Center-bottom
        bodyNode.AddChild(bodyOriginNode);
        
        // Add body attachment point for head (neck)
        var bodyMapNode = new NxNode("map");
        bodyNode.AddChild(bodyMapNode);
        var neckNode = new NxNode("neck");
        neckNode.Value = new Vector2(0, 28); // Head attachment at Y=28
        bodyMapNode.AddChild(neckNode);
        
        var armNode = new NxNode("arm");
        armNode.Value = CreateMockSprite("arm", 24, 32);
        frame0Node.AddChild(armNode);
        
        // Add origin for arm (should be positioned at Y=20)
        var armOriginNode = new NxNode("origin");
        armOriginNode.Value = new Vector2(12, 16); // Center of arm
        armNode.AddChild(armOriginNode);
        
        var headNode = new NxNode("head");
        headNode.Value = CreateMockSprite("head", 32, 32);
        frame0Node.AddChild(headNode);
        
        // Add origin for head
        var headOriginNode = new NxNode("origin");
        headOriginNode.Value = new Vector2(16, 16); // Center of head
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
        Object.DestroyImmediate(texture);
        
        return pngData;
    }
}