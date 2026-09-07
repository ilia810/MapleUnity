using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameData;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

public static class SimpleRuntimeAlignmentTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== SIMPLE RUNTIME ALIGNMENT TEST ===");
            
            // Create a new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Initialize NX with mock data
            var mockFile = new MockNxFile();
            NXAssetLoader.Instance.RegisterNxFile("character", mockFile);
            
            // Create character
            var characterObj = new GameObject("TestCharacter");
            var renderer = characterObj.AddComponent<MapleCharacterRenderer>();
            
            // Just call CreateSpriteRenderers to initialize the renderer components
            var createMethod = renderer.GetType().GetMethod("CreateSpriteRenderers", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (createMethod != null)
            {
                createMethod.Invoke(renderer, null);
                Debug.Log("Successfully called CreateSpriteRenderers");
            }
            else
            {
                Debug.LogError("Could not find CreateSpriteRenderers method!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Now set appearance after components are initialized
            renderer.SetCharacterAppearance(0, 20000, 30000);
            
            // Analyze the character structure
            Debug.Log("\n=== CHARACTER STRUCTURE ===");
            AnalyzeStructure(characterObj.transform, "");
            
            // Find key parts
            var body = FindChild(characterObj.transform, "Body");
            var head = FindChild(characterObj.transform, "Head");
            var face = FindChild(characterObj.transform, "Face");
            
            Debug.Log("\n=== POSITIONS ===");
            if (body != null) Debug.Log($"Body: world={body.position}, local={body.localPosition}");
            if (head != null) Debug.Log($"Head: world={head.position}, local={head.localPosition}");
            if (face != null) Debug.Log($"Face: world={face.position}, local={face.localPosition}");
            
            // Calculate offsets
            if (face != null && head != null)
            {
                Debug.Log("\n=== FACE OFFSET ANALYSIS ===");
                
                var faceOffset = face.position - head.position;
                Debug.Log($"Face offset from head: {faceOffset} units");
                Debug.Log($"Face offset in pixels: ({faceOffset.x * 100:F1}, {faceOffset.y * 100:F1})");
                
                // Check face sprite info
                var faceSr = face.GetComponent<SpriteRenderer>();
                if (faceSr != null && faceSr.sprite != null)
                {
                    Debug.Log($"Face sprite: {faceSr.sprite.name}");
                    Debug.Log($"  Pivot: {faceSr.sprite.pivot}");
                    Debug.Log($"  Bounds: center={faceSr.sprite.bounds.center}, size={faceSr.sprite.bounds.size}");
                    Debug.Log($"  Texture rect: {faceSr.sprite.textureRect}");
                }
            }
            
            // Check attachment points through reflection
            Debug.Log("\n=== ATTACHMENT POINTS ===");
            var attachField = renderer.GetType().GetField("currentAttachmentPoints", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (attachField != null)
            {
                var attachPoints = attachField.GetValue(renderer) as Dictionary<string, Vector2>;
                if (attachPoints != null)
                {
                    foreach (var kvp in attachPoints)
                    {
                        Debug.Log($"{kvp.Key}: {kvp.Value}");
                    }
                }
            }
            
            // Write results to file
            string results = "Runtime Alignment Test Results\n";
            results += "==============================\n\n";
            
            if (face != null && head != null)
            {
                var faceOffset = face.position - head.position;
                results += $"Face offset from head: {faceOffset}\n";
                results += $"Face offset in pixels: ({faceOffset.x * 100:F1}, {faceOffset.y * 100:F1})\n";
                results += $"Expected offset: (-8, -52) pixels\n";
                
                var actualPixels = new Vector2(faceOffset.x * 100, faceOffset.y * 100);
                var expectedPixels = new Vector2(-8, -52);
                var diff = actualPixels - expectedPixels;
                results += $"Difference: ({diff.x:F1}, {diff.y:F1}) pixels\n";
            }
            
            File.WriteAllText("runtime-alignment-results.txt", results);
            Debug.Log($"Results written to: {Path.GetFullPath("runtime-alignment-results.txt")}");
            
            Debug.Log("\n=== TEST COMPLETED SUCCESSFULLY ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
    
    static void AnalyzeStructure(Transform t, string indent)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        string info = $"{indent}{t.name}: pos={t.localPosition}";
        if (sr != null && sr.sprite != null)
        {
            info += $", sprite={sr.sprite.name}";
        }
        Debug.Log(info);
        
        foreach (Transform child in t)
        {
            AnalyzeStructure(child, indent + "  ");
        }
    }
    
    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}