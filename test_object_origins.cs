using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;

class TestObjectOrigins
{
    static void Main()
    {
        var dataManager = new NXDataManager(@"C:\HeavenClient\MapleStory-Client\nx");
        
        // Test the guide sign object that we know has origin (91,55)
        var testPaths = new[] {
            "Obj/guide.img/common/post/0",
            "Obj/guide.img/common/sign/0",
            "Obj/houseGS.img/house9/basic/1"
        };
        
        foreach (var path in testPaths)
        {
            Console.WriteLine($"\n=== Testing path: {path} ===");
            var node = dataManager.GetNode("map", path);
            
            if (node != null)
            {
                ExploreNode(node, path, 0);
            }
            else
            {
                Console.WriteLine($"Node not found!");
            }
        }
    }
    
    static void ExploreNode(INxNode node, string path, int depth)
    {
        var indent = new string(' ', depth * 2);
        
        Console.WriteLine($"{indent}Node: {node.Name}");
        Console.WriteLine($"{indent}  Type: {node.GetType().Name}");
        Console.WriteLine($"{indent}  Has Value: {node.Value != null}");
        if (node.Value != null)
        {
            Console.WriteLine($"{indent}  Value Type: {node.Value.GetType().Name}");
            if (node.Value is byte[] bytes)
            {
                Console.WriteLine($"{indent}  Value: byte[{bytes.Length}]");
            }
            else
            {
                Console.WriteLine($"{indent}  Value: {node.Value}");
            }
        }
        
        // Check for origin child
        var originNode = node["origin"];
        if (originNode != null)
        {
            Console.WriteLine($"{indent}  HAS ORIGIN CHILD!");
            Console.WriteLine($"{indent}    Origin Value: {originNode.Value}");
            Console.WriteLine($"{indent}    Origin Type: {originNode.Value?.GetType().Name}");
        }
        
        // List children
        var children = node.Children.ToList();
        if (children.Count > 0)
        {
            Console.WriteLine($"{indent}  Children ({children.Count}):");
            foreach (var child in children.Take(5))
            {
                Console.WriteLine($"{indent}    - {child.Name}");
                
                // For first level children, explore deeper
                if (depth < 2)
                {
                    ExploreNode(child, path + "/" + child.Name, depth + 1);
                }
            }
        }
    }
}