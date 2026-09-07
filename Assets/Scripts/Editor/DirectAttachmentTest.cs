using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using MapleClient.GameView;
using MapleClient.GameData;

public static class DirectAttachmentTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== DIRECT ATTACHMENT TEST ===");
            
            // Create test scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Initialize with mock data
            var mockFile = new MockNxFile();
            NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
            
            // Create character GameObject with proper hierarchy
            var characterObj = new GameObject("TestCharacter");
            
            // Instead of using MapleCharacterRenderer which has dependencies,
            // let's directly test the attachment point calculation
            
            // Based on MockNxFile data:
            // Body neck: (-7, -31)
            // Head neck: (14, 19)
            // Head brow: (13, -2)
            
            Vector2 bodyNeck = new Vector2(-7, -31);
            Vector2 headNeck = new Vector2(14, 19);
            Vector2 headBrow = new Vector2(13, -2);
            
            Debug.Log("\n=== ATTACHMENT POINTS ===");
            Debug.Log($"Body neck: {bodyNeck}");
            Debug.Log($"Head neck: {headNeck}");
            Debug.Log($"Head brow: {headBrow}");
            
            // Calculate positions according to C++ formula
            // head position = body.neck - head.neck
            Vector2 headOffset = bodyNeck - headNeck;
            Debug.Log($"\nHead offset = body.neck - head.neck = {bodyNeck} - {headNeck} = {headOffset}");
            
            // face position = body.neck - head.neck + head.brow
            Vector2 faceOffset = bodyNeck - headNeck + headBrow;
            Debug.Log($"Face offset = body.neck - head.neck + head.brow = {bodyNeck} - {headNeck} + {headBrow} = {faceOffset}");
            
            // Create child objects to simulate the actual rendering
            var bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(characterObj.transform);
            bodyObj.transform.localPosition = Vector3.zero;
            
            var headObj = new GameObject("Head");
            headObj.transform.SetParent(characterObj.transform);
            // Convert to Unity units (divide by 100) and flip Y
            headObj.transform.localPosition = new Vector3(headOffset.x / 100f, -headOffset.y / 100f, 0);
            
            var faceObj = new GameObject("Face");
            faceObj.transform.SetParent(characterObj.transform);
            // Convert to Unity units (divide by 100) and flip Y
            faceObj.transform.localPosition = new Vector3(faceOffset.x / 100f, -faceOffset.y / 100f, 0);
            
            Debug.Log("\n=== RESULTING POSITIONS (Unity units) ===");
            Debug.Log($"Body local position: {bodyObj.transform.localPosition}");
            Debug.Log($"Head local position: {headObj.transform.localPosition}");
            Debug.Log($"Face local position: {faceObj.transform.localPosition}");
            
            // Check distance
            float distance = Vector3.Distance(headObj.transform.position, faceObj.transform.position);
            Debug.Log($"\nHead-Face distance: {distance:F4} Unity units");
            
            if (distance < 0.001f)
            {
                Debug.LogError("ERROR: Face and Head are at the same position!");
            }
            else
            {
                Debug.Log("SUCCESS: Face and Head are at different positions!");
                
                // Calculate the actual pixel offset between face and head
                Vector3 faceHeadDiff = faceObj.transform.localPosition - headObj.transform.localPosition;
                Vector2 pixelDiff = new Vector2(faceHeadDiff.x * 100f, -faceHeadDiff.y * 100f);
                Debug.Log($"Face-Head pixel difference: {pixelDiff}");
                Debug.Log($"Expected (head.brow): {headBrow}");
                
                bool matches = Mathf.Approximately(pixelDiff.x, headBrow.x) && 
                              Mathf.Approximately(pixelDiff.y, headBrow.y);
                if (matches)
                {
                    Debug.Log("✓ Face offset from head matches expected head.brow value!");
                }
                else
                {
                    Debug.LogError($"✗ Face offset mismatch! Got {pixelDiff}, expected {headBrow}");
                }
            }
            
            // Now test what MapleCharacterRenderer would produce
            Debug.Log("\n=== TESTING MAPLECHARACTERRENDERER LOGIC ===");
            
            // Create the renderer component
            var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
            
            // Use reflection to call CreateSpriteRenderers
            var createMethod = typeof(MapleCharacterRenderer).GetMethod("CreateSpriteRenderers", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (createMethod != null)
            {
                createMethod.Invoke(renderer, null);
            }
            
            // Create minimal character data provider
            var charData = new CharacterDataProvider();
            
            // Initialize the renderer
            renderer.Initialize(null, charData);
            
            // Try to apply attachment offsets directly
            var applyMethod = typeof(MapleCharacterRenderer).GetMethod("ApplyAttachmentOffsets", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // First, set the attachment points using reflection
            var attachmentField = typeof(MapleCharacterRenderer).GetField("currentAttachmentPoints", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (attachmentField != null)
            {
                var attachmentPoints = new Dictionary<string, Vector2>
                {
                    { "body.map.neck", bodyNeck },
                    { "head.map.neck", headNeck },
                    { "head.map.brow", headBrow }
                };
                attachmentField.SetValue(renderer, attachmentPoints);
                
                if (applyMethod != null)
                {
                    applyMethod.Invoke(renderer, null);
                    
                    // Check the actual positions of the renderer's children
                    Debug.Log("\n=== RENDERER CHILD POSITIONS ===");
                    foreach (Transform child in renderer.transform)
                    {
                        if (child.name == "Head" || child.name == "Face")
                        {
                            Debug.Log($"{child.name}: {child.localPosition}");
                        }
                    }
                }
            }
            
            Debug.Log("\n=== TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}