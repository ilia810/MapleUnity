using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class DebugTileAlignmentDetailed : EditorWindow
    {
        private bool showGizmos = false;
        private bool showOnlyEdges = false;
        
        [MenuItem("MapleUnity/Debug/Debug Tile Alignment (Detailed)")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileAlignmentDetailed>("Tile Alignment Detail");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tile Alignment Analysis", EditorStyles.boldLabel);
            
            showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);
            showOnlyEdges = EditorGUILayout.Toggle("Show Only Edges", showOnlyEdges);
            
            if (GUILayout.Button("Analyze Tile Gaps"))
            {
                AnalyzeTileGaps();
            }
            
            if (GUILayout.Button("Check Tile Dimensions"))
            {
                CheckTileDimensions();
            }
            
            if (GUILayout.Button("Analyze Edge Connections"))
            {
                AnalyzeEdgeConnections();
            }
            
            if (GUILayout.Button("Show Tile Grid"))
            {
                ShowTileGrid();
            }
            
            if (showGizmos)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                SceneView.RepaintAll();
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI;
            }
        }
        
        private void AnalyzeTileGaps()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log($"=== Analyzing Tile Gaps ({tiles.Length} tiles) ===");
            
            // Group tiles by layer
            var layerGroups = tiles.GroupBy(t => t.layer).OrderByDescending(g => g.Key);
            
            foreach (var layerGroup in layerGroups)
            {
                Debug.Log($"\n--- Layer {layerGroup.Key} ---");
                
                // Create spatial index for quick neighbor lookup
                var spatialIndex = new Dictionary<string, MapTile>();
                foreach (var tile in layerGroup)
                {
                    var pos = tile.transform.position;
                    var key = $"{Mathf.RoundToInt(pos.x)},{Mathf.RoundToInt(pos.y)}";
                    spatialIndex[key] = tile;
                }
                
                // Check each tile for gaps with neighbors
                int gapCount = 0;
                foreach (var tile in layerGroup.Take(20)) // Sample first 20
                {
                    var pos = tile.transform.position;
                    var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    if (!renderer || !renderer.sprite) continue;
                    
                    var bounds = renderer.bounds;
                    
                    // Check right neighbor
                    var rightKey = $"{Mathf.RoundToInt(pos.x + 1)},{Mathf.RoundToInt(pos.y)}";
                    if (spatialIndex.ContainsKey(rightKey))
                    {
                        var rightTile = spatialIndex[rightKey];
                        var rightRenderer = rightTile.GetComponentInChildren<SpriteRenderer>();
                        if (rightRenderer && rightRenderer.sprite)
                        {
                            var rightBounds = rightRenderer.bounds;
                            var gap = rightBounds.min.x - bounds.max.x;
                            
                            if (Mathf.Abs(gap) > 0.01f)
                            {
                                gapCount++;
                                Debug.LogWarning($"  Gap detected: {tile.name} -> {rightTile.name}, gap={gap:F3} units");
                                Debug.Log($"    Left tile: pos={pos}, bounds=({bounds.min.x:F2},{bounds.min.y:F2})-({bounds.max.x:F2},{bounds.max.y:F2})");
                                Debug.Log($"    Right tile: pos={rightTile.transform.position}, bounds=({rightBounds.min.x:F2},{rightBounds.min.y:F2})-({rightBounds.max.x:F2},{rightBounds.max.y:F2})");
                            }
                        }
                    }
                    
                    // Check bottom neighbor
                    var bottomKey = $"{Mathf.RoundToInt(pos.x)},{Mathf.RoundToInt(pos.y - 1)}";
                    if (spatialIndex.ContainsKey(bottomKey))
                    {
                        var bottomTile = spatialIndex[bottomKey];
                        var bottomRenderer = bottomTile.GetComponentInChildren<SpriteRenderer>();
                        if (bottomRenderer && bottomRenderer.sprite)
                        {
                            var bottomBounds = bottomRenderer.bounds;
                            var gap = bounds.min.y - bottomBounds.max.y;
                            
                            if (Mathf.Abs(gap) > 0.01f)
                            {
                                gapCount++;
                                Debug.LogWarning($"  Vertical gap: {tile.name} -> {bottomTile.name}, gap={gap:F3} units");
                            }
                        }
                    }
                }
                
                if (gapCount == 0)
                {
                    Debug.Log("  No gaps detected in sampled tiles!");
                }
            }
        }
        
        private void CheckTileDimensions()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log("=== Checking Tile Dimensions ===");
            
            // Group by variant
            var variantGroups = tiles.GroupBy(t => t.variant).OrderBy(g => g.Key);
            
            foreach (var group in variantGroups)
            {
                var sampleTiles = group.Take(5);
                var dimensions = new List<Vector2>();
                
                foreach (var tile in sampleTiles)
                {
                    var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    if (renderer && renderer.sprite)
                    {
                        var sprite = renderer.sprite;
                        var width = sprite.rect.width;
                        var height = sprite.rect.height;
                        dimensions.Add(new Vector2(width, height));
                        
                        // Check pivot point
                        var pivot = sprite.pivot;
                        var normalizedPivot = new Vector2(pivot.x / width, pivot.y / height);
                        
                        if (dimensions.Count == 1) // First tile
                        {
                            Debug.Log($"\n{group.Key}:");
                            Debug.Log($"  Dimensions: {width}x{height} pixels");
                            Debug.Log($"  Pivot: ({pivot.x:F1}, {pivot.y:F1}) pixels");
                            Debug.Log($"  Normalized Pivot: ({normalizedPivot.x:F2}, {normalizedPivot.y:F2})");
                            Debug.Log($"  Origin stored: ({tile.GetComponent<MapTile>().tileSet})");
                            
                            // Check if origin offset is applied
                            var localPos = renderer.transform.localPosition;
                            if (localPos != Vector3.zero)
                            {
                                Debug.Log($"  Local offset applied: ({localPos.x:F3}, {localPos.y:F3})");
                            }
                        }
                    }
                }
                
                // Check for consistency
                if (dimensions.Count > 1)
                {
                    var distinct = dimensions.Distinct().Count();
                    if (distinct > 1)
                    {
                        Debug.LogWarning($"  WARNING: Inconsistent dimensions found! {distinct} different sizes");
                    }
                }
            }
        }
        
        private void AnalyzeEdgeConnections()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log("=== Analyzing Edge Connections ===");
            
            // Find base tiles and their adjacent edges
            var baseTiles = tiles.Where(t => t.variant == "bsc").ToList();
            var edgeTiles = tiles.Where(t => t.variant.StartsWith("ed") || t.variant.StartsWith("en")).ToList();
            
            Debug.Log($"Found {baseTiles.Count} base tiles and {edgeTiles.Count} edge tiles");
            
            // For each edge tile, find the nearest base tile
            foreach (var edge in edgeTiles.Take(10))
            {
                var edgePos = edge.transform.position;
                var nearestBase = baseTiles.OrderBy(b => Vector3.Distance(b.transform.position, edgePos)).FirstOrDefault();
                
                if (nearestBase != null)
                {
                    var basePos = nearestBase.transform.position;
                    var offset = edgePos - basePos;
                    
                    Debug.Log($"\nEdge {edge.variant}/{edge.tileNumber} at ({edgePos.x:F1}, {edgePos.y:F1})");
                    Debug.Log($"  Nearest base at ({basePos.x:F1}, {basePos.y:F1})");
                    Debug.Log($"  Offset: ({offset.x:F1}, {offset.y:F1})");
                    Debug.Log($"  Edge z={edge.z}, zM={edge.zM}, sortOrder={edge.sortingOrder}");
                    Debug.Log($"  Base z={nearestBase.z}, zM={nearestBase.zM}, sortOrder={nearestBase.sortingOrder}");
                    
                    // Check if edge is properly on top
                    if (edge.sortingOrder <= nearestBase.sortingOrder)
                    {
                        Debug.LogError($"  ERROR: Edge tile sorting order ({edge.sortingOrder}) is not greater than base ({nearestBase.sortingOrder})!");
                    }
                }
            }
        }
        
        private void ShowTileGrid()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            // Find bounds
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            
            foreach (var tile in tiles)
            {
                var pos = tile.transform.position;
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
            
            Debug.Log($"=== Tile Grid Analysis ===");
            Debug.Log($"Bounds: X[{minX:F1}, {maxX:F1}], Y[{minY:F1}, {maxY:F1}]");
            
            // Check grid alignment
            var xPositions = tiles.Select(t => t.transform.position.x).Distinct().OrderBy(x => x).ToList();
            var yPositions = tiles.Select(t => t.transform.position.y).Distinct().OrderBy(y => y).ToList();
            
            // Calculate grid spacing
            if (xPositions.Count > 1)
            {
                var xDiffs = new List<float>();
                for (int i = 1; i < xPositions.Count && i < 10; i++)
                {
                    xDiffs.Add(xPositions[i] - xPositions[i-1]);
                }
                Debug.Log($"X spacing samples: {string.Join(", ", xDiffs.Select(d => d.ToString("F3")))}");
            }
            
            if (yPositions.Count > 1)
            {
                var yDiffs = new List<float>();
                for (int i = 1; i < yPositions.Count && i < 10; i++)
                {
                    yDiffs.Add(yPositions[i] - yPositions[i-1]);
                }
                Debug.Log($"Y spacing samples: {string.Join(", ", yDiffs.Select(d => d.ToString("F3")))}");
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!showGizmos) return;
            
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            foreach (var tile in tiles)
            {
                if (showOnlyEdges && !tile.variant.StartsWith("ed") && !tile.variant.StartsWith("en"))
                    continue;
                
                var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                if (!renderer || !renderer.sprite) continue;
                
                var bounds = renderer.bounds;
                
                // Draw tile bounds
                Handles.color = GetColorForVariant(tile.variant);
                Handles.DrawWireCube(bounds.center, bounds.size);
                
                // Draw pivot point
                Handles.color = Color.red;
                Handles.DrawWireDisc(tile.transform.position, Vector3.forward, 0.05f);
                
                // Draw sprite center
                Handles.color = Color.blue;
                Handles.DrawWireDisc(bounds.center, Vector3.forward, 0.03f);
            }
        }
        
        private Color GetColorForVariant(string variant)
        {
            if (variant == "bsc") return Color.gray;
            if (variant.StartsWith("ed")) return Color.green;
            if (variant.StartsWith("en")) return Color.cyan;
            if (variant.StartsWith("sl")) return Color.yellow;
            return Color.white;
        }
        
        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}