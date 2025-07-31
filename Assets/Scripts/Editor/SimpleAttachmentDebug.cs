using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class SimpleAttachmentDebug
{
    public static void DebugNXStructure()
    {
        Debug.Log("=== SIMPLE NX STRUCTURE DEBUG ===");
        
        try
        {
            // First, let's check if NX files exist
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string nxPath = Path.Combine(projectPath, "nx");
            
            Debug.Log($"Looking for NX files at: {nxPath}");
            
            if (!Directory.Exists(nxPath))
            {
                Debug.LogError($"NX directory not found at: {nxPath}");
                EditorApplication.Exit(1);
                return;
            }
            
            var nxFiles = Directory.GetFiles(nxPath, "*.nx");
            Debug.Log($"Found {nxFiles.Length} NX files:");
            foreach (var file in nxFiles)
            {
                Debug.Log($"  - {Path.GetFileName(file)}");
            }
            
            // Look for character.nx specifically
            string characterNxPath = Path.Combine(nxPath, "Character.nx");
            if (!File.Exists(characterNxPath))
            {
                // Try lowercase
                characterNxPath = Path.Combine(nxPath, "character.nx");
                if (!File.Exists(characterNxPath))
                {
                    Debug.LogError("Character.nx file not found!");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            
            Debug.Log($"Found character NX file at: {characterNxPath}");
            Debug.Log($"File size: {new FileInfo(characterNxPath).Length} bytes");
            
            // Now let's try to understand why NXAssetLoader might be failing
            Debug.Log("\n=== Checking MockNxFile Implementation ===");
            
            // Check if we're using mock data
            var mockNxFilePath = Path.Combine(Application.dataPath, "Scripts/GameData/NX/MockNxFile.cs");
            if (File.Exists(mockNxFilePath))
            {
                Debug.Log("MockNxFile.cs exists - we might be using mock data instead of real NX files!");
                
                // Let's check the NXAssetLoader to see what it's doing
                var nxAssetLoaderPath = Path.Combine(Application.dataPath, "Scripts/GameData/NX/NXAssetLoader.cs");
                if (File.Exists(nxAssetLoaderPath))
                {
                    Debug.Log("NXAssetLoader.cs exists");
                    
                    // Read first few lines to check if it's using mock data
                    var lines = File.ReadAllLines(nxAssetLoaderPath);
                    bool usingMock = false;
                    for (int i = 0; i < Mathf.Min(50, lines.Length); i++)
                    {
                        if (lines[i].Contains("MockNxFile") || lines[i].Contains("USE_MOCK"))
                        {
                            usingMock = true;
                            Debug.LogWarning($"Line {i + 1}: Found mock reference: {lines[i].Trim()}");
                        }
                    }
                    
                    if (usingMock)
                    {
                        Debug.LogError("NXAssetLoader appears to be using MockNxFile instead of real NX files!");
                    }
                }
            }
            
            Debug.Log("\n=== DEBUG COMPLETE ===");
            Debug.Log("Next step: Check if NXAssetLoader is configured to use mock data or real NX files");
            
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception: {e}");
            EditorApplication.Exit(1);
        }
    }
}