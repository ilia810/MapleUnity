using UnityEngine;
using UnityEditor;
using System.Linq;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class DebugTileOrigins : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Debug Tile Origins")]
        public static void ShowWindow()
        {
            GetWindow<DebugTileOrigins>("Tile Origins Debug");
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Show Tiles with Origins"))
            {
                ShowTilesWithOrigins();
            }
            
            if (GUILayout.Button("Highlight Edge Tiles"))
            {
                HighlightEdgeTiles();
            }
        }
        
        private void ShowTilesWithOrigins()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            Debug.Log($"=== Tiles with Origin Offsets ===");
            
            int count = 0;
            foreach (var tile in tiles)
            {
                var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                if (renderer && renderer.transform.localPosition != Vector3.zero)
                {
                    count++;
                    if (count <= 20) // Show first 20
                    {
                        Debug.Log($"Tile {tile.name}: local offset = {renderer.transform.localPosition}, variant = {tile.variant}");
                    }
                }
            }
            
            Debug.Log($"Total tiles with origin offsets: {count} out of {tiles.Length}");
        }
        
        private void HighlightEdgeTiles()
        {
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            
            foreach (var tile in tiles)
            {
                // Edge tiles typically have variants like edD, edU, enH0, enH1, enV0, enV1
                if (tile.variant.StartsWith("ed") || tile.variant.StartsWith("en"))
                {
                    var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                    if (renderer)
                    {
                        // Add a colored outline to edge tiles
                        renderer.color = new Color(1f, 0.8f, 0.8f, 1f); // Light red tint
                        
                        // Log the first few
                        if (Random.Range(0, 10) == 0)
                        {
                            Debug.Log($"Edge tile: {tile.name} at {tile.transform.position}, offset = {renderer.transform.localPosition}");
                        }
                    }
                }
            }
            
            Debug.Log("Edge tiles highlighted in light red");
        }
    }
}