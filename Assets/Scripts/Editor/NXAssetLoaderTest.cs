using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System.Collections.Generic;

public static class NXAssetLoaderTest
{
    public static void RunTest()
    {
        Debug.Log("=== NX ASSET LOADER TEST ===");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Batch Mode: {Application.isBatchMode}");
        
        try
        {
            // Initialize mock data first
            Debug.Log("Initializing mock NX data...");
            var mockCharFile = new MockNxFile("character.nx");
            
            // Check NXAssetLoader instance
            var loader = NXAssetLoader.Instance;
            
            // Register the mock file
            loader.RegisterNxFile("character", mockCharFile);
            Debug.Log("Registered mock character NX file");
            if (loader == null)
            {
                Debug.LogError("NXAssetLoader.Instance is NULL!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("NXAssetLoader instance obtained successfully");
            
            // Check if it's using mock data
            var loaderType = loader.GetType();
            Debug.Log($"Loader type: {loaderType.FullName}");
            
            // Check for MockNxFile type
            var mockType = System.Type.GetType("MapleClient.GameData.MockNxFile");
            if (mockType != null)
            {
                Debug.LogWarning("MockNxFile type exists - might be using mock data");
            }
            
            // Try to load character body parts
            Debug.Log("\n=== TESTING BODY PARTS LOADING ===");
            Dictionary<string, Vector2> attachmentPoints;
            var bodyParts = loader.LoadCharacterBodyParts(0, "stand1", 0, out attachmentPoints);
            
            if (bodyParts == null)
            {
                Debug.LogError("LoadCharacterBodyParts returned NULL!");
            }
            else if (bodyParts.Count == 0)
            {
                Debug.LogError("LoadCharacterBodyParts returned empty dictionary!");
            }
            else
            {
                Debug.Log($"Successfully loaded {bodyParts.Count} body parts:");
                foreach (var part in bodyParts)
                {
                    var sprite = part.Value;
                    Debug.Log($"  - {part.Key}: {sprite.rect.width}x{sprite.rect.height}, pivot=({sprite.pivot.x:F1}, {sprite.pivot.y:F1})");
                }
            }
            
            if (attachmentPoints != null && attachmentPoints.Count > 0)
            {
                Debug.Log($"\nFound {attachmentPoints.Count} attachment points:");
                foreach (var ap in attachmentPoints)
                {
                    Debug.Log($"  - {ap.Key}: {ap.Value}");
                }
            }
            else
            {
                Debug.LogWarning("No attachment points found");
            }
            
            // Try to load a face
            Debug.Log("\n=== TESTING FACE LOADING ===");
            var faceSprite = loader.LoadFace(20000, "default");
            if (faceSprite == null)
            {
                Debug.LogError("LoadFace returned NULL!");
            }
            else
            {
                Debug.Log($"Successfully loaded face: {faceSprite.rect.width}x{faceSprite.rect.height}");
            }
            
            // Check NX file access
            Debug.Log("\n=== CHECKING NX FILE ACCESS ===");
            var charFile = loader.GetNxFile("character");
            if (charFile == null)
            {
                Debug.LogError("Character NX file is NULL!");
                
                // Check NX file path
                var nxPath = System.IO.Path.Combine(Application.dataPath, "NX");
                Debug.Log($"Looking for NX files at: {nxPath}");
                if (System.IO.Directory.Exists(nxPath))
                {
                    var nxFiles = System.IO.Directory.GetFiles(nxPath, "*.nx");
                    Debug.Log($"Found {nxFiles.Length} .nx files:");
                    foreach (var file in nxFiles)
                    {
                        Debug.Log($"  - {System.IO.Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Debug.LogError($"NX directory not found at: {nxPath}");
                }
            }
            else
            {
                Debug.Log("Character NX file loaded successfully");
                
                // Check specific nodes
                var bodyNode = charFile.GetNode("00002000.img");
                Debug.Log($"Body node (00002000.img) exists: {bodyNode != null}");
                
                if (bodyNode != null)
                {
                    var stand1 = bodyNode["stand1"];
                    Debug.Log($"Stand1 node exists: {stand1 != null}");
                    
                    if (stand1 != null)
                    {
                        var frame0 = stand1["0"];
                        Debug.Log($"Frame 0 exists: {frame0 != null}");
                    }
                }
            }
            
            Debug.Log("\n=== TEST COMPLETE ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed with exception: {e.Message}");
            Debug.LogError($"Stack trace:\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}