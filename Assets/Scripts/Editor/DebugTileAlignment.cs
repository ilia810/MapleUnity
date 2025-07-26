using UnityEngine;
using UnityEditor;
using System.Linq;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class DebugTileAlignment : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug Tile Alignment")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileAlignment>("Tile Alignment Debug");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Check Tile Alignment in Scene"))
            {
                CheckAlignment();
            }
            
            if (GUILayout.Button("Snap Tiles to Grid"))
            {
                SnapToGrid();
            }
        }
        
        private void CheckAlignment()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log($"Found {tiles.Length} tiles in scene");
            
            // Group by layer
            var layerGroups = tiles.GroupBy(t => {
                var parts = t.name.Split('_');
                if (parts.Length > 1 && parts[1].StartsWith("L"))
                {
                    return int.Parse(parts[1].Substring(1));
                }
                return 0;
            });
            
            foreach (var group in layerGroups.OrderBy(g => g.Key))
            {
                Debug.Log($"\n=== Layer {group.Key} ===");
                
                // Check alignment
                int misaligned = 0;
                foreach (var tile in group.Take(10)) // Check first 10 tiles
                {
                    var pos = tile.transform.position;
                    float expectedX = Mathf.Round(pos.x / 30f) * 30f; // 30 pixel grid
                    float expectedY = Mathf.Round(pos.y / 30f) * 30f;
                    
                    if (Mathf.Abs(pos.x - expectedX) > 0.1f || Mathf.Abs(pos.y - expectedY) > 0.1f)
                    {
                        misaligned++;
                        Debug.LogWarning($"Misaligned tile: {tile.name} at ({pos.x:F2}, {pos.y:F2}) - should be ({expectedX}, {expectedY})");
                    }
                }
                
                if (misaligned == 0)
                {
                    Debug.Log("All sampled tiles are properly aligned!");
                }
                else
                {
                    Debug.LogWarning($"{misaligned} tiles are misaligned!");
                }
                
                // Check sorting order
                var sortingOrders = group.Select(t => t.GetComponentInChildren<SpriteRenderer>()?.sortingOrder ?? 0).Distinct().OrderBy(x => x);
                Debug.Log($"Sorting orders in this layer: {string.Join(", ", sortingOrders.Take(10))}");
            }
        }
        
        private void SnapToGrid()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            int snapped = 0;
            
            foreach (var tile in tiles)
            {
                var pos = tile.transform.position;
                float gridSize = 1.0f; // Unity units, adjust as needed
                
                float snappedX = Mathf.Round(pos.x / gridSize) * gridSize;
                float snappedY = Mathf.Round(pos.y / gridSize) * gridSize;
                
                if (Mathf.Abs(pos.x - snappedX) > 0.01f || Mathf.Abs(pos.y - snappedY) > 0.01f)
                {
                    tile.transform.position = new Vector3(snappedX, snappedY, pos.z);
                    snapped++;
                }
            }
            
            Debug.Log($"Snapped {snapped} tiles to grid");
        }
    }
}