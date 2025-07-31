using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System.Linq;

public class ExploreEquipmentStructure : MonoBehaviour
{
    [MenuItem("MapleUnity/Tests/Explore Equipment Structure")]
    public static void RunExploration()
    {
        Debug.Log("[EXPLORE_EQUIPMENT] Starting exploration...");
        
        var loader = NXAssetLoader.Instance;
        var charFile = loader.GetNxFile("character");
        
        if (charFile == null || charFile.Root == null)
        {
            Debug.LogError("Character NX file not loaded!");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log("\n=== Character NX Root Structure ===");
        Debug.Log("Looking for equipment categories:");
        
        // Check for common equipment categories
        string[] categories = { "Cap", "Coat", "Pants", "Shoes", "Glove", "Shield", "Cape", "Weapon" };
        
        foreach (string category in categories)
        {
            var categoryNode = charFile.GetNode(category);
            if (categoryNode != null)
            {
                Debug.Log($"\n✓ Found category: {category}");
                Debug.Log($"  Children count: {categoryNode.Children.Count()}");
                
                // Show first few items
                int count = 0;
                foreach (var child in categoryNode.Children)
                {
                    Debug.Log($"    - {child.Name}");
                    if (++count >= 5)
                    {
                        Debug.Log("    ... (more)");
                        break;
                    }
                }
            }
            else
            {
                Debug.Log($"✗ Category not found: {category}");
            }
        }
        
        // Try exploring a specific item
        Debug.Log("\n=== Exploring Specific Item ===");
        ExploreSpecificItem(charFile, "Cap", "01002140.img"); // Blue Bandana
        ExploreSpecificItem(charFile, "Weapon", "01302000.img"); // Sword
        
        Debug.Log("\n[EXPLORE_EQUIPMENT] Exploration complete!");
        
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
    
    private static void ExploreSpecificItem(INxFile charFile, string category, string itemFile)
    {
        Debug.Log($"\nExploring {category}/{itemFile}:");
        
        var itemNode = charFile.GetNode($"{category}/{itemFile}");
        if (itemNode != null)
        {
            Debug.Log($"  ✓ Found item node");
            Debug.Log($"  Children:");
            
            foreach (var child in itemNode.Children)
            {
                Debug.Log($"    - {child.Name}");
                
                // If it's an animation, show frames
                if (child.Name == "stand1" || child.Name == "walk1")
                {
                    Debug.Log($"      Frames:");
                    int frameCount = 0;
                    foreach (var frame in child.Children)
                    {
                        Debug.Log($"        - {frame.Name}");
                        
                        // Check frame structure
                        if (frame.Name == "0")
                        {
                            Debug.Log($"          Frame 0 children:");
                            foreach (var part in frame.Children)
                            {
                                Debug.Log($"            - {part.Name} (Type: {part.Value?.GetType().Name ?? "Container"})");
                            }
                        }
                        
                        if (++frameCount >= 3) break;
                    }
                }
            }
        }
        else
        {
            Debug.Log($"  ✗ Item not found at path: {category}/{itemFile}");
        }
    }
}