using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System.Linq;
using System.IO;

public static class BatchEquipmentExplorer
{
    public static void Run()
    {
        Debug.Log("[BATCH_EQUIPMENT] Starting equipment exploration in batch mode...");
        
        try
        {
            // Initialize NX Asset Loader with mock data
            Debug.Log("[BATCH_EQUIPMENT] Creating mock character NX file...");
            
            // Create a mock character file with equipment data
            var mockCharFile = new MockNxFile("character.nx");
            var charRoot = mockCharFile.Root as NxNode;
            
            // Add equipment categories
            AddMockEquipmentCategory(charRoot, "Cap", new[] { "01002140.img", "01002141.img", "01002142.img" });
            AddMockEquipmentCategory(charRoot, "Coat", new[] { "01040002.img", "01040003.img", "01040004.img" });
            AddMockEquipmentCategory(charRoot, "Pants", new[] { "01060002.img", "01060003.img" });
            AddMockEquipmentCategory(charRoot, "Shoes", new[] { "01072001.img", "01072002.img" });
            AddMockEquipmentCategory(charRoot, "Glove", new[] { "01082001.img", "01082002.img" });
            AddMockEquipmentCategory(charRoot, "Weapon", new[] { "01302000.img", "01302001.img", "01312000.img" });
            
            // Register the mock file
            NXAssetLoader.Instance.RegisterNxFile("character", mockCharFile);
            
            // Get character file
            var charFile = NXAssetLoader.Instance.GetNxFile("character");
            
            if (charFile == null || charFile.Root == null)
            {
                Debug.LogError("[BATCH_EQUIPMENT] Character NX file not loaded!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("[BATCH_EQUIPMENT] Character NX file loaded successfully!");
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
            Debug.Log("\n=== Exploring Specific Items ===");
            ExploreSpecificItem(charFile, "Cap", "01002140.img"); // Blue Bandana
            ExploreSpecificItem(charFile, "Weapon", "01302000.img"); // Sword
            ExploreSpecificItem(charFile, "Coat", "01040002.img"); // White Undershirt
            
            Debug.Log("\n[BATCH_EQUIPMENT] Exploration complete!");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BATCH_EQUIPMENT] Error: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void ExploreSpecificItem(INxFile charFile, string category, string itemFile)
    {
        Debug.Log($"\n[ITEM] Exploring {category}/{itemFile}:");
        
        var itemNode = charFile.GetNode($"{category}/{itemFile}");
        if (itemNode != null)
        {
            Debug.Log($"  ✓ Found item node");
            Debug.Log($"  Children: {itemNode.Children.Count()}");
            
            foreach (var child in itemNode.Children)
            {
                Debug.Log($"    - {child.Name}");
                
                // If it's an animation state, show frames
                if (child.Name == "stand1" || child.Name == "walk1" || child.Name == "alert" || 
                    child.Name == "swingO1" || child.Name == "swingO2" || child.Name == "swingO3")
                {
                    Debug.Log($"      Animation frames: {child.Children.Count()}");
                    int frameCount = 0;
                    foreach (var frame in child.Children)
                    {
                        Debug.Log($"        - Frame {frame.Name}");
                        
                        // Check frame structure for first frame
                        if (frame.Name == "0")
                        {
                            Debug.Log($"          Frame 0 structure:");
                            foreach (var part in frame.Children)
                            {
                                var type = part.Value?.GetType().Name ?? "Container";
                                Debug.Log($"            - {part.Name} (Type: {type})");
                                
                                // If it's a sprite, check properties
                                if (part.Value != null)
                                {
                                    var sprite = part.GetNode("_inlink");
                                    if (sprite != null && sprite.Value is string linkPath)
                                    {
                                        Debug.Log($"              Sprite link: {linkPath}");
                                    }
                                    
                                    var origin = part.GetNode("origin");
                                    if (origin != null && origin.Value is System.Drawing.Point point)
                                    {
                                        Debug.Log($"              Origin: ({point.X}, {point.Y})");
                                    }
                                    
                                    var z = part.GetNode("z");
                                    if (z != null)
                                    {
                                        Debug.Log($"              Z-order: {z.Value}");
                                    }
                                }
                            }
                        }
                        
                        if (++frameCount >= 2) break;
                    }
                }
                
                // Check info node
                if (child.Name == "info")
                {
                    Debug.Log($"      Info node children:");
                    foreach (var info in child.Children)
                    {
                        Debug.Log($"        - {info.Name}: {info.Value}");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"  ✗ Item not found at path: {category}/{itemFile}");
        }
    }
    
    private static void AddMockEquipmentCategory(NxNode charRoot, string categoryName, string[] items)
    {
        var categoryNode = new NxNode(categoryName);
        charRoot.AddChild(categoryNode);
        
        foreach (var itemName in items)
        {
            var itemNode = new NxNode(itemName);
            categoryNode.AddChild(itemNode);
            
            // Add info node
            var infoNode = new NxNode("info");
            infoNode.AddChild(new NxNode("icon", "_icon"));
            infoNode.AddChild(new NxNode("iconRaw", "_iconRaw"));
            infoNode.AddChild(new NxNode("islot", categoryName));
            infoNode.AddChild(new NxNode("vslot", categoryName));
            itemNode.AddChild(infoNode);
            
            // Add animation states
            AddMockAnimationState(itemNode, "stand1", 3);
            AddMockAnimationState(itemNode, "walk1", 4);
            
            if (categoryName == "Weapon")
            {
                AddMockAnimationState(itemNode, "swingO1", 3);
                AddMockAnimationState(itemNode, "swingO2", 3);
                AddMockAnimationState(itemNode, "swingO3", 2);
            }
        }
    }
    
    private static void AddMockAnimationState(NxNode itemNode, string stateName, int frameCount)
    {
        var stateNode = new NxNode(stateName);
        itemNode.AddChild(stateNode);
        
        for (int i = 0; i < frameCount; i++)
        {
            var frameNode = new NxNode(i.ToString());
            stateNode.AddChild(frameNode);
            
            // Add mock parts for frame 0
            if (i == 0)
            {
                var partNode = new NxNode(itemNode.Parent.Name);
                partNode.AddChild(new NxNode("_inlink", $"Character/{itemNode.Parent.Name}/{itemNode.Name}/{stateName}/{i}"));
                partNode.AddChild(new NxNode("origin", new System.Drawing.Point(0, 0)));
                partNode.AddChild(new NxNode("z", itemNode.Parent.Name));
                frameNode.AddChild(partNode);
                
                if (itemNode.Parent.Name == "Weapon")
                {
                    var handNode = new NxNode("hand");
                    handNode.AddChild(new NxNode("_inlink", $"Character/Weapon/{itemNode.Name}/{stateName}/{i}/hand"));
                    handNode.AddChild(new NxNode("origin", new System.Drawing.Point(-10, -5)));
                    handNode.AddChild(new NxNode("z", "weaponOverHand"));
                    frameNode.AddChild(handNode);
                }
            }
        }
    }
}