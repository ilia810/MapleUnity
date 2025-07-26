using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class InspectRemainingIssues : EditorWindow
    {
        private string searchVariant = "edU";
        private int maxResults = 20;
        
        [MenuItem("MapleUnity/Debug/Inspect Remaining Issues")]
        public static void ShowWindow()
        {
            GetWindow<InspectRemainingIssues>("Remaining Issues");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Inspect Remaining Tile Issues", EditorStyles.boldLabel);
            
            searchVariant = EditorGUILayout.TextField("Search Variant", searchVariant);
            maxResults = EditorGUILayout.IntField("Max Results", maxResults);
            
            if (GUILayout.Button("Find Overlapping Tiles"))
            {
                FindOverlappingTiles();
            }
            
            if (GUILayout.Button("Check Z-Value Distribution"))
            {
                CheckZValueDistribution();
            }
            
            if (GUILayout.Button("Find Tiles with Same Sort Order"))
            {
                FindTilesWithSameSortOrder();
            }
            
            if (GUILayout.Button("Analyze Specific Variant"))
            {
                AnalyzeSpecificVariant(searchVariant);
            }
            
            if (GUILayout.Button("Check Tile Boundaries"))
            {
                CheckTileBoundaries();
            }
        }
        
        private void FindOverlappingTiles()
        {
            Debug.Log("=== Finding Overlapping Tiles ===");
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Group tiles by position (rounded)
            var positionGroups = tiles.GroupBy(t => {
                var pos = t.transform.position;
                return new Vector2(Mathf.Round(pos.x * 100) / 100, Mathf.Round(pos.y * 100) / 100);
            });
            
            int overlapCount = 0;
            foreach (var group in positionGroups.Where(g => g.Count() > 1))
            {
                overlapCount++;
                if (overlapCount <= 10)
                {
                    var pos = group.Key;
                    Debug.LogWarning($"Multiple tiles at position ({pos.x:F2}, {pos.y:F2}):");
                    foreach (var tile in group)
                    {
                        Debug.Log($"  - {tile.variant}/{tile.tileNumber}, Layer={tile.layer}, z={tile.z}, zM={tile.zM}, sortOrder={tile.sortingOrder}");
                    }
                }
            }
            
            Debug.Log($"Total positions with overlapping tiles: {overlapCount}");
        }
        
        private void CheckZValueDistribution()
        {
            Debug.Log("=== Z-Value Distribution ===");
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Group by layer
            var layerGroups = tiles.GroupBy(t => t.layer).OrderByDescending(g => g.Key);
            
            foreach (var layerGroup in layerGroups)
            {
                Debug.Log($"\nLayer {layerGroup.Key}:");
                
                // Count z and zM values
                var zValues = layerGroup.GroupBy(t => t.z).OrderBy(g => g.Key);
                var zmValues = layerGroup.GroupBy(t => t.zM).OrderBy(g => g.Key);
                
                Debug.Log("  Z distribution:");
                foreach (var zGroup in zValues)
                {
                    var variants = zGroup.GroupBy(t => t.variant).Select(g => $"{g.Key}({g.Count()})");
                    Debug.Log($"    z={zGroup.Key}: {zGroup.Count()} tiles - {string.Join(", ", variants)}");
                }
                
                Debug.Log("  ZM distribution:");
                foreach (var zmGroup in zmValues)
                {
                    var variants = zmGroup.GroupBy(t => t.variant).Select(g => $"{g.Key}({g.Count()})");
                    Debug.Log($"    zM={zmGroup.Key}: {zmGroup.Count()} tiles - {string.Join(", ", variants)}");
                }
                
                // Check actual z used (z or zM if z is 0)
                var actualZValues = layerGroup.Select(t => t.z == 0 ? t.zM : t.z).Distinct().OrderBy(z => z);
                Debug.Log($"  Actual Z values used: [{string.Join(", ", actualZValues)}]");
            }
        }
        
        private void FindTilesWithSameSortOrder()
        {
            Debug.Log("=== Tiles with Same Sort Order ===");
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            var sortOrderGroups = tiles.GroupBy(t => t.sortingOrder).Where(g => g.Count() > 1).OrderBy(g => g.Key);
            
            int conflictCount = 0;
            foreach (var group in sortOrderGroups)
            {
                conflictCount++;
                if (conflictCount <= 10)
                {
                    Debug.LogWarning($"Sort Order {group.Key}: {group.Count()} tiles");
                    
                    // Show a few examples
                    foreach (var tile in group.Take(5))
                    {
                        Debug.Log($"  - {tile.name} at ({tile.transform.position.x:F1}, {tile.transform.position.y:F1})");
                    }
                }
            }
            
            Debug.Log($"Total sort order conflicts: {conflictCount}");
        }
        
        private void AnalyzeSpecificVariant(string variant)
        {
            Debug.Log($"=== Analyzing Variant: {variant} ===");
            var tiles = GameObject.FindObjectsOfType<MapTile>().Where(t => t.variant == variant).ToList();
            
            if (!tiles.Any())
            {
                Debug.LogWarning($"No tiles found with variant '{variant}'");
                return;
            }
            
            Debug.Log($"Found {tiles.Count} {variant} tiles");
            
            // Group by layer
            var layerGroups = tiles.GroupBy(t => t.layer).OrderByDescending(g => g.Key);
            
            foreach (var layerGroup in layerGroups)
            {
                Debug.Log($"\nLayer {layerGroup.Key}: {layerGroup.Count()} tiles");
                
                // Check z/zM values
                var zRange = layerGroup.Select(t => t.z).Distinct().OrderBy(z => z);
                var zmRange = layerGroup.Select(t => t.zM).Distinct().OrderBy(z => z);
                Debug.Log($"  z values: [{string.Join(", ", zRange)}]");
                Debug.Log($"  zM values: [{string.Join(", ", zmRange)}]");
                
                // Sample positions
                foreach (var tile in layerGroup.Take(5))
                {
                    var pos = tile.transform.position;
                    var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    Debug.Log($"  Tile at ({pos.x:F1}, {pos.y:F1}): z={tile.z}, zM={tile.zM}, " +
                             $"sortOrder={tile.sortingOrder}, sprite={renderer?.sprite?.name ?? "null"}");
                }
            }
            
            // Find neighboring tiles
            Debug.Log("\n--- Checking neighbors of first few tiles ---");
            foreach (var tile in tiles.Take(3))
            {
                CheckTileNeighbors(tile);
            }
        }
        
        private void CheckTileNeighbors(MapTile tile)
        {
            var pos = tile.transform.position;
            var allTiles = GameObject.FindObjectsOfType<MapTile>();
            
            Debug.Log($"\nNeighbors of {tile.variant}/{tile.tileNumber} at ({pos.x:F1}, {pos.y:F1}):");
            
            // Find tiles within 1.5 units
            var neighbors = allTiles.Where(t => t != tile && Vector3.Distance(t.transform.position, pos) < 1.5f)
                                   .OrderBy(t => Vector3.Distance(t.transform.position, pos));
            
            foreach (var neighbor in neighbors.Take(5))
            {
                var nPos = neighbor.transform.position;
                var offset = nPos - pos;
                Debug.Log($"  {neighbor.variant}/{neighbor.tileNumber} at offset ({offset.x:F2}, {offset.y:F2}), " +
                         $"layer={neighbor.layer}, sortOrder={neighbor.sortingOrder}");
                
                // Check if this neighbor should be in front or behind
                if (neighbor.sortingOrder < tile.sortingOrder)
                {
                    Debug.Log("    -> Behind (correct if base tile)");
                }
                else
                {
                    Debug.Log("    -> In front (correct if edge/overlay)");
                }
            }
        }
        
        private void CheckTileBoundaries()
        {
            Debug.Log("=== Checking Tile Boundaries ===");
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Group tiles by approximate grid position
            var gridSize = 0.3f; // ~30 pixels in Unity units
            var gridGroups = tiles.GroupBy(t => {
                var pos = t.transform.position;
                return new Vector2(
                    Mathf.Round(pos.x / gridSize) * gridSize,
                    Mathf.Round(pos.y / gridSize) * gridSize
                );
            });
            
            // Find grid positions with edge tiles
            var edgePositions = gridGroups.Where(g => g.Any(t => 
                t.variant.StartsWith("ed") || t.variant.StartsWith("en")))
                .Take(10);
            
            foreach (var group in edgePositions)
            {
                var pos = group.Key;
                Debug.Log($"\nGrid position ({pos.x:F1}, {pos.y:F1}):");
                
                var baseTiles = group.Where(t => t.variant == "bsc").ToList();
                var edgeTiles = group.Where(t => t.variant.StartsWith("ed") || t.variant.StartsWith("en")).ToList();
                
                Debug.Log($"  Base tiles: {baseTiles.Count}");
                Debug.Log($"  Edge tiles: {edgeTiles.Count}");
                
                // Check sorting
                if (baseTiles.Any() && edgeTiles.Any())
                {
                    var maxBaseSortOrder = baseTiles.Max(t => t.sortingOrder);
                    var minEdgeSortOrder = edgeTiles.Min(t => t.sortingOrder);
                    
                    if (minEdgeSortOrder <= maxBaseSortOrder)
                    {
                        Debug.LogError($"  ERROR: Edge tiles not properly on top! Base max={maxBaseSortOrder}, Edge min={minEdgeSortOrder}");
                        
                        // Show details
                        foreach (var b in baseTiles)
                        {
                            Debug.Log($"    Base: {b.variant}/{b.tileNumber}, z={b.z}, zM={b.zM}, sortOrder={b.sortingOrder}");
                        }
                        foreach (var e in edgeTiles)
                        {
                            Debug.Log($"    Edge: {e.variant}/{e.tileNumber}, z={e.z}, zM={e.zM}, sortOrder={e.sortingOrder}");
                        }
                    }
                    else
                    {
                        Debug.Log($"  OK: Edges properly on top (base max={maxBaseSortOrder}, edge min={minEdgeSortOrder})");
                    }
                }
            }
        }
    }
}