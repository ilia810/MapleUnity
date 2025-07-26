using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using MapleClient.GameData;

namespace MapleClient.Editor
{
    public class FindGreyStoneTilesets : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Find Grey Stone Tilesets")]
        public static void ShowWindow()
        {
            GetWindow<FindGreyStoneTilesets>("Find Grey Stone Tilesets");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Find Grey/Stone Tilesets", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Search for Grey/Stone Tilesets"))
            {
                SearchTilesets();
            }
        }
        
        private void SearchTilesets()
        {
            Debug.Log("=== Searching for Grey/Stone Tilesets ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode != null)
            {
                // Search for grey/stone related tilesets
                string[] keywords = { "grey", "gray", "stone", "concrete", "cement", "town", "village" };
                
                foreach (var keyword in keywords)
                {
                    var matches = tileNode.Children.Where(c => c.Name.ToLower().Contains(keyword)).ToList();
                    if (matches.Any())
                    {
                        Debug.Log($"\nTilesets containing '{keyword}':");
                        foreach (var match in matches)
                        {
                            Debug.Log($"  - {match.Name}");
                            
                            // Check if it has the variants we need
                            bool hasBsc = match.Children.Any(c => c.Name == "bsc");
                            bool hasEdD = match.Children.Any(c => c.Name == "edD");
                            bool hasEnH0 = match.Children.Any(c => c.Name == "enH0");
                            
                            if (hasBsc && hasEdD && hasEnH0)
                            {
                                Debug.Log($"    ✓ Has required variants!");
                                
                                // Test load a tile to see what it looks like
                                var testNode = match["bsc"]?["0"];
                                if (testNode != null)
                                {
                                    var sprite = SpriteLoader.LoadSprite(testNode, $"Tile/{match.Name}/bsc/0");
                                    if (sprite != null)
                                    {
                                        var texture = sprite.texture;
                                        // Sample the center pixel to get dominant color
                                        var centerColor = texture.GetPixel(texture.width / 2, texture.height / 2);
                                        Debug.Log($"    Center color: {centerColor} (Grey level: {centerColor.grayscale})");
                                        
                                        if (centerColor.grayscale > 0.4f && centerColor.grayscale < 0.8f)
                                        {
                                            Debug.Log($"    ★ This looks like a grey tileset!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Also check the "brownBrick" and similar that might actually be grey
                Debug.Log("\nChecking specific tilesets that might be grey:");
                string[] checkTilesets = { "brownBrick", "grayBrick1", "grayBrick2", "grayBrick3", "holeBrick" };
                
                foreach (var tileName in checkTilesets)
                {
                    var tileSet = tileNode[tileName + ".img"];
                    if (tileSet != null)
                    {
                        Debug.Log($"\n{tileName}:");
                        var testNode = tileSet["bsc"]?["0"];
                        if (testNode != null)
                        {
                            var sprite = SpriteLoader.LoadSprite(testNode, $"Tile/{tileName}/bsc/0");
                            if (sprite != null)
                            {
                                var texture = sprite.texture;
                                var centerColor = texture.GetPixel(texture.width / 2, texture.height / 2);
                                Debug.Log($"  Color: {centerColor} (Grey level: {centerColor.grayscale})");
                            }
                        }
                    }
                }
            }
        }
    }
}