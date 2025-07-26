using UnityEngine;
using UnityEditor;
using System.Linq;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class DebugTileSorting : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug Tile Sorting")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileSorting>("Tile Sorting Debug");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Analyze Tile Sorting"))
            {
                AnalyzeTileSorting();
            }
            
            if (GUILayout.Button("Show Tiles by Layer"))
            {
                ShowTilesByLayer();
            }
            
            if (GUILayout.Button("Show Edge Tiles"))
            {
                ShowEdgeTiles();
            }
        }
        
        private void AnalyzeTileSorting()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log($"=== Tile Sorting Analysis ({tiles.Length} tiles) ===");
            
            // Group by layer
            var layerGroups = tiles.GroupBy(t => t.layer).OrderByDescending(g => g.Key);
            
            foreach (var group in layerGroups)
            {
                Debug.Log($"\n--- Layer {group.Key} ({group.Count()} tiles) ---");
                
                // Show first 10 tiles with their sorting values
                var samples = group.OrderBy(t => t.sortingOrder).Take(10);
                foreach (var tile in samples)
                {
                    var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    Debug.Log($"  {tile.variant}/{tile.tileNumber}: z={tile.z}, zM={tile.zM}, sortOrder={tile.sortingOrder}, " +
                             $"pos=({tile.transform.position.x:F1}, {tile.transform.position.y:F1})");
                }
                
                // Check for overlapping sorting orders
                var sortingGroups = group.GroupBy(t => t.sortingOrder);
                var overlaps = sortingGroups.Where(g => g.Count() > 1).ToList();
                if (overlaps.Any())
                {
                    Debug.LogWarning($"  Found {overlaps.Count} sorting order conflicts!");
                    foreach (var overlap in overlaps.Take(3))
                    {
                        Debug.LogWarning($"    SortOrder {overlap.Key}: {overlap.Count()} tiles");
                    }
                }
            }
        }
        
        private void ShowTilesByLayer()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Group by layer
            var layerGroups = tiles.GroupBy(t => t.layer).OrderByDescending(g => g.Key);
            
            foreach (var group in layerGroups)
            {
                Debug.Log($"\n=== Layer {group.Key} ===");
                
                // Group by variant type
                var variantGroups = group.GroupBy(t => t.variant).OrderBy(g => g.Key);
                foreach (var vGroup in variantGroups)
                {
                    var avgZ = vGroup.Average(t => t.z);
                    var avgZM = vGroup.Average(t => t.zM);
                    Debug.Log($"  {vGroup.Key}: {vGroup.Count()} tiles, avg z={avgZ:F1}, avg zM={avgZM:F1}");
                    
                    // Show z/zM distribution
                    var zValues = vGroup.Select(t => t.z).Distinct().OrderBy(z => z);
                    var zmValues = vGroup.Select(t => t.zM).Distinct().OrderBy(zm => zm);
                    Debug.Log($"    z values: [{string.Join(", ", zValues)}]");
                    Debug.Log($"    zM values: [{string.Join(", ", zmValues)}]");
                }
            }
        }
        
        private void ShowEdgeTiles()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Filter edge tiles
            var edgeTiles = tiles.Where(t => 
                t.variant.StartsWith("ed") || 
                t.variant.StartsWith("en") ||
                t.variant.StartsWith("sl")).ToList();
            
            Debug.Log($"=== Edge Tiles ({edgeTiles.Count} total) ===");
            
            // Group by variant
            var variantGroups = edgeTiles.GroupBy(t => t.variant).OrderBy(g => g.Key);
            
            foreach (var group in variantGroups)
            {
                Debug.Log($"\n{group.Key}: {group.Count()} tiles");
                
                // Check z/zM values
                var zRange = group.Select(t => t.z).Distinct().OrderBy(z => z).ToList();
                var zmRange = group.Select(t => t.zM).Distinct().OrderBy(zm => zm).ToList();
                
                Debug.Log($"  z values: [{string.Join(", ", zRange)}]");
                Debug.Log($"  zM values: [{string.Join(", ", zmRange)}]");
                
                // Compare with base tiles
                var baseTiles = tiles.Where(t => t.variant == "bsc" && t.layer == group.First().layer).ToList();
                if (baseTiles.Any())
                {
                    var baseAvgZ = baseTiles.Average(t => t.z);
                    var baseAvgZM = baseTiles.Average(t => t.zM);
                    var edgeAvgZ = group.Average(t => t.z);
                    var edgeAvgZM = group.Average(t => t.zM);
                    
                    Debug.Log($"  Base tiles (bsc): avg z={baseAvgZ:F1}, avg zM={baseAvgZM:F1}");
                    Debug.Log($"  This edge type: avg z={edgeAvgZ:F1}, avg zM={edgeAvgZM:F1}");
                    
                    if (edgeAvgZ <= baseAvgZ && edgeAvgZM <= baseAvgZM)
                    {
                        Debug.LogWarning($"  WARNING: Edge tiles have same or lower depth than base tiles!");
                    }
                }
            }
        }
    }
}