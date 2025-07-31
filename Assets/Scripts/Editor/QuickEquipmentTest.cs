using UnityEngine;
using UnityEditor;
using MapleClient.GameData;
using System.IO;
using System.Text;
using System.Linq;

public class QuickEquipmentTest
{
    [MenuItem("MapleUnity/Tests/Quick Equipment Test")]
    public static void RunQuickTest()
    {
        RunTest(false);
    }
    
    public static void RunBatchTest()
    {
        RunTest(true);
    }
    
    private static void RunTest(bool isBatchMode)
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine("=== Quick Equipment Loading Test ===");
        output.AppendLine($"Time: {System.DateTime.Now}");
        output.AppendLine();
        
        var loader = NXAssetLoader.Instance;
        
        // Test hats
        output.AppendLine("--- Testing Hats ---");
        TestEquipmentCategory(loader, new[] { 1002001, 1002005, 1002140 }, "Cap", output);
        
        // Test weapons  
        output.AppendLine("\n--- Testing Weapons ---");
        TestEquipmentCategory(loader, new[] { 1302000, 1402000, 1312000 }, "Weapon", output);
        
        // Test capes
        output.AppendLine("\n--- Testing Capes ---");
        TestEquipmentCategory(loader, new[] { 1102000, 1102001, 1102002 }, "Cape", output);
        
        // Write to file
        string resultsPath = Path.Combine(Application.dataPath, "../equipment_test_results.txt");
        File.WriteAllText(resultsPath, output.ToString());
        
        // Also log to console
        Debug.Log(output.ToString());
        
        Debug.Log($"\nTest results written to: {resultsPath}");
        
        // If in batch mode, exit
        if (isBatchMode || Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }
    
    private static void TestEquipmentCategory(NXAssetLoader loader, int[] itemIds, string category, StringBuilder output)
    {
        foreach (int itemId in itemIds)
        {
            output.AppendLine($"\nTesting {category} {itemId}:");
            
            // Try loading for stand animation
            var sprite = loader.LoadEquipment(itemId, category, "stand1", 0);
            if (sprite != null)
            {
                output.AppendLine($"  ✓ Loaded: {sprite.name} ({sprite.rect.width}x{sprite.rect.height})");
            }
            else
            {
                output.AppendLine($"  ✗ Failed to load {category} {itemId}");
                
                // Explore structure
                ExploreStructure(loader, itemId, category, output);
            }
        }
    }
    
    private static void ExploreStructure(NXAssetLoader loader, int itemId, string category, StringBuilder output)
    {
        var charFile = loader.GetNxFile("character");
        if (charFile == null)
        {
            output.AppendLine("    - Character NX file not found!");
            return;
        }
        
        string itemFile = $"{itemId:D8}.img";
        string basePath = $"{category}/{itemFile}";
        
        var itemNode = charFile.GetNode(basePath);
        if (itemNode != null)
        {
            output.AppendLine($"    - Found node at: {basePath}");
            output.AppendLine("    - Children:");
            int count = 0;
            foreach (var child in itemNode.Children)
            {
                output.AppendLine($"      * {child.Name}");
                if (++count >= 5)
                {
                    output.AppendLine("      ... (more)");
                    break;
                }
            }
        }
        else
        {
            output.AppendLine($"    - Node not found at: {basePath}");
            
            // Check if category folder exists
            var categoryNode = charFile.GetNode(category);
            if (categoryNode != null)
            {
                output.AppendLine($"    - Category '{category}' exists with {categoryNode.Children.Count()} items");
                
                // Show first few items
                int shown = 0;
                foreach (var child in categoryNode.Children)
                {
                    if (child.Name.EndsWith(".img"))
                    {
                        output.AppendLine($"      * {child.Name}");
                        if (++shown >= 3) break;
                    }
                }
            }
            else
            {
                output.AppendLine($"    - Category '{category}' not found!");
            }
        }
    }
}