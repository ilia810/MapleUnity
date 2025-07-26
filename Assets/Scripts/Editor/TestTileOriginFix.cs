using UnityEngine;
using UnityEditor;
using GameData;
using System.Linq;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class TestTileOriginFix : EditorWindow
    {
        [MenuItem("MapleUnity/Debug/Test Tile Origin Fix")]
        public static void ShowWindow()
        {
            GetWindow<TestTileOriginFix>("Test Origin Fix");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tile Origin Fix Testing", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test woodMarble Edge Tiles"))
            {
                TestEdgeTiles();
            }
            
            if (GUILayout.Button("Compare Tile Positions"))
            {
                CompareTilePositions();
            }
        }
        
        private void TestEdgeTiles()
        {
            Debug.Log("=== Testing woodMarble Edge Tile Origins ===");
            
            var nxManager = NXDataManagerSingleton.Instance;
            string[] edgeVariants = { "edD", "edU", "enH0", "enH1", "enV0", "enV1" };
            
            foreach (var variant in edgeVariants)
            {
                Debug.Log($"\n--- Variant: {variant} ---");
                
                for (int i = 0; i < 3; i++)
                {
                    var (sprite, origin) = nxManager.GetTileSpriteWithOrigin("woodMarble", variant, i);
                    if (sprite != null)
                    {
                        Debug.Log($"Tile {i}: origin=({origin.x},{origin.y}), size=({sprite.rect.width}x{sprite.rect.height})");
                        
                        // Calculate what the offset should be
                        float centerX = sprite.rect.width / 2f;
                        float centerY = sprite.rect.height / 2f;
                        float offsetX = (centerX - origin.x) / 100f;
                        float offsetY = (origin.y - centerY) / 100f;
                        
                        Debug.Log($"  Expected offset: ({offsetX:F2}, {offsetY:F2}) Unity units");
                    }
                }
            }
        }
        
        private void CompareTilePositions()
        {
            Debug.Log("=== Comparing Tile Positions Before/After Origin Fix ===");
            
            // Find some edge tiles in the scene
            var tiles = GameObject.FindObjectsOfType<MapTile>();
            var edgeTiles = tiles.Where(t => t.variant.StartsWith("ed") || t.variant.StartsWith("en")).Take(10);
            
            foreach (var tile in edgeTiles)
            {
                var renderer = tile.GetComponentInChildren<SpriteRenderer>();
                if (renderer)
                {
                    var worldPos = tile.transform.position;
                    var localOffset = renderer.transform.localPosition;
                    var finalPos = worldPos + localOffset;
                    
                    Debug.Log($"{tile.name}:");
                    Debug.Log($"  Base position: {worldPos}");
                    Debug.Log($"  Origin offset: {localOffset}");
                    Debug.Log($"  Final sprite position: {finalPos}");
                }
            }
        }
    }
}