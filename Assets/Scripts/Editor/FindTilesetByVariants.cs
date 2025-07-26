using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using System.Collections.Generic;

namespace MapleClient.Editor
{
    public class FindTilesetByVariants : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Find Tileset By Variants")]
        public static void ShowWindow()
        {
            GetWindow<FindTilesetByVariants>("Find Tileset By Variants");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Find Tileset By Variant Names", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Find Tileset with Henesys Variants"))
            {
                FindMatchingTileset();
            }
            
            if (GUILayout.Button("Check Default Tileset (.img)"))
            {
                CheckDefaultTileset();
            }
        }
        
        public static void ExecuteFindMatchingTileset()
        {
            FindMatchingTilesetStatic();
        }
        
        private void FindMatchingTileset()
        {
            FindMatchingTilesetStatic();
        }
        
        private static void FindMatchingTilesetStatic()
        {
            Debug.Log("=== Finding Tileset with Henesys Variants ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // These are the variants used in Henesys
            string[] henesysVariants = { "bsc", "edD", "edU", "enH0", "enH1", "enV0", "enV1" };
            
            var tileNode = dataManager.GetNode("map", "Tile");
            if (tileNode == null)
            {
                Debug.LogError("Tile folder not found!");
                return;
            }
            
            Debug.Log($"Checking {tileNode.Children.Count()} tilesets for Henesys variants...");
            
            var perfectMatches = new List<string>();
            var goodMatches = new List<(string name, int matchCount)>();
            
            foreach (var tileset in tileNode.Children)
            {
                // Get variant names from this tileset
                var variantNames = tileset.Children.Select(c => c.Name).ToHashSet();
                
                // Count how many Henesys variants this tileset has
                int matchCount = henesysVariants.Count(v => variantNames.Contains(v));
                
                if (matchCount == henesysVariants.Length)
                {
                    perfectMatches.Add(tileset.Name);
                }
                else if (matchCount >= henesysVariants.Length - 1) // Allow one missing
                {
                    goodMatches.Add((tileset.Name, matchCount));
                }
            }
            
            Debug.Log($"\n=== Results ===");
            if (perfectMatches.Any())
            {
                Debug.Log($"Perfect matches (has all {henesysVariants.Length} variants):");
                foreach (var match in perfectMatches.Take(10))
                {
                    Debug.Log($"  ✓ {match}");
                }
                if (perfectMatches.Count > 10)
                {
                    Debug.Log($"  ... and {perfectMatches.Count - 10} more");
                }
            }
            
            if (goodMatches.Any())
            {
                Debug.Log($"\nGood matches (missing 1 variant):");
                foreach (var (name, count) in goodMatches.Take(5))
                {
                    Debug.Log($"  - {name} ({count}/{henesysVariants.Length} variants)");
                }
            }
            
            // Check if there's a pattern in the names
            if (perfectMatches.Any())
            {
                Debug.Log("\n=== Analyzing Perfect Match Names ===");
                
                // Look for town/village related tilesets
                var townRelated = perfectMatches.Where(n => 
                    n.ToLower().Contains("town") || 
                    n.ToLower().Contains("village") ||
                    n.ToLower().Contains("henesys")).ToList();
                    
                if (townRelated.Any())
                {
                    Debug.Log("Town/Village related tilesets:");
                    foreach (var t in townRelated)
                    {
                        Debug.Log($"  ★ {t}");
                    }
                }
                
                // Check for "village" tileset specifically
                if (perfectMatches.Contains("village.img"))
                {
                    Debug.Log("\n★★★ Found 'village.img' - This is likely the default town tileset!");
                }
            }
        }
        
        private void CheckDefaultTileset()
        {
            Debug.Log("=== Checking for Default/Empty Tileset ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            var dataManager = nxManager.DataManager;
            
            // Check if there's a tileset named ".img" (empty tS value + .img)
            var emptyTileset = dataManager.GetNode("map", "Tile/.img");
            if (emptyTileset != null)
            {
                Debug.Log("Found '.img' tileset!");
                Debug.Log($"Variants: {string.Join(", ", emptyTileset.Children.Select(c => c.Name))}");
            }
            else
            {
                Debug.Log("No '.img' tileset found");
            }
            
            // Check for other potential defaults
            string[] potentialDefaults = { "", "default", "Default", "base", "Base" };
            foreach (var name in potentialDefaults)
            {
                var tileset = dataManager.GetNode("map", $"Tile/{name}.img");
                if (tileset == null && !string.IsNullOrEmpty(name))
                {
                    tileset = dataManager.GetNode("map", $"Tile/{name}");
                }
                
                if (tileset != null)
                {
                    Debug.Log($"Found '{name}' tileset with {tileset.Children.Count()} variants");
                }
            }
        }
    }
}