using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using GameData;
using System.Linq;

public class ExploreCharacterNX : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Explore Character NX Structure")]
    public static void RunExploration()
    {
        Debug.Log("[EXPLORE_CHARACTER_NX] Starting exploration...");
        
        // Initialize NX data if not already loaded
        var singleton = NXDataManagerSingleton.Instance;
        if (singleton == null || singleton.DataManager == null)
        {
            Debug.LogError("NXDataManager not initialized!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        var loader = NXAssetLoader.Instance;
        var charFile = loader.GetNxFile("character");
        
        if (charFile == null || charFile.Root == null)
        {
            Debug.LogError("Character NX file not loaded!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log("\n=== Character NX Root Structure ===");
        Debug.Log("Top-level categories:");
        int count = 0;
        foreach (var child in charFile.Root.Children.OrderBy(c => c.Name))
        {
            Debug.Log($"  {child.Name}");
            if (++count >= 20)
            {
                Debug.Log("  ... (more)");
                break;
            }
        }
        
        // Explore body sprites
        Debug.Log("\n=== Body Sprites (00002000.img) ===");
        var bodyNode = charFile.GetNode("00002000.img");
        if (bodyNode != null)
        {
            Debug.Log("Animations available:");
            foreach (var anim in bodyNode.Children.Take(10))
            {
                Debug.Log($"  {anim.Name} - {anim.Children.Count()} frames");
            }
            
            // Check stand animation structure
            var standNode = bodyNode["stand"];
            if (standNode != null)
            {
                var frame0 = standNode["0"];
                if (frame0 != null)
                {
                    Debug.Log("\nFrame 0 body parts:");
                    foreach (var part in frame0.Children)
                    {
                        Debug.Log($"    {part.Name} - Type: {part.Value?.GetType().Name ?? "Container"}");
                    }
                }
            }
        }
        
        // Explore face sprites
        Debug.Log("\n=== Face Sprites ===");
        var faceNode = charFile.GetNode("Face");
        if (faceNode != null)
        {
            Debug.Log("Face IDs found (first 10):");
            count = 0;
            foreach (var face in faceNode.Children.OrderBy(c => c.Name))
            {
                Debug.Log($"  {face.Name}");
                if (++count >= 10) break;
            }
            
            // Try common face IDs
            string[] testFaceIds = { "00020000.img", "00021000.img", "00022000.img" };
            foreach (var faceId in testFaceIds)
            {
                var testFace = faceNode[faceId];
                if (testFace != null)
                {
                    Debug.Log($"\nFound face {faceId}, expressions:");
                    foreach (var expr in testFace.Children.Take(5))
                    {
                        Debug.Log($"    {expr.Name}");
                    }
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning("No Face node found!");
        }
        
        // Explore hair sprites
        Debug.Log("\n=== Hair Sprites ===");
        var hairNode = charFile.GetNode("Hair");
        if (hairNode != null)
        {
            Debug.Log("Hair IDs found (first 10):");
            count = 0;
            foreach (var hair in hairNode.Children.OrderBy(c => c.Name))
            {
                Debug.Log($"  {hair.Name}");
                if (++count >= 10) break;
            }
        }
        
        Debug.Log("\n[EXPLORE_CHARACTER_NX] Exploration complete!");
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
}