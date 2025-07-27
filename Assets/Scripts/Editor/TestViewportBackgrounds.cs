using UnityEngine;
using UnityEditor;
using MapleClient.SceneGeneration;

namespace MapleClient.Editor
{
    public class TestViewportBackgrounds
    {
        [MenuItem("MapleUnity/Test/Test Viewport Backgrounds")]
        public static void TestBackgrounds()
        {
            Debug.Log("=== TESTING VIEWPORT-BASED BACKGROUND SYSTEM ===");
            
            // Find the background layers
            var bgManager = Object.FindObjectOfType<DynamicBackgroundManager>();
            if (bgManager == null)
            {
                Debug.LogError("No DynamicBackgroundManager found!");
                return;
            }
            
            Debug.Log("Found DynamicBackgroundManager");
            Debug.Log($"VR Bounds: {bgManager.vrBounds}");
            
            // Find all viewport background layers
            var bgLayers = Object.FindObjectsOfType<ViewportBackgroundLayer>();
            Debug.Log($"Found {bgLayers.Length} background layers");
            
            foreach (var layer in bgLayers)
            {
                if (layer.backgroundData != null)
                {
                    Debug.Log($"Layer {layer.backgroundData.No}:");
                    Debug.Log($"  Name: {layer.backgroundData.BgName}");
                    Debug.Log($"  Type: {layer.backgroundData.Type}");
                    Debug.Log($"  Sprite: {(layer.tileSprite != null ? layer.tileSprite.name : "null")}");
                    Debug.Log($"  Sorting Order: {layer.sortingOrder}");
                    
                    // Count active tiles
                    var tiles = layer.GetComponentsInChildren<SpriteRenderer>(false);
                    Debug.Log($"  Active tiles: {tiles.Length}");
                }
            }
            
            // Test camera movement
            var camera = Camera.main;
            if (camera != null)
            {
                Debug.Log($"Camera position: {camera.transform.position}");
                Debug.Log("Move the camera in Scene view to see backgrounds update dynamically!");
            }
        }
        
        [MenuItem("MapleUnity/Test/Log Background Coverage")]
        public static void LogBackgroundCoverage()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No main camera found!");
                return;
            }
            
            Vector3 camPos = camera.transform.position;
            Debug.Log($"=== BACKGROUND COVERAGE AT CAMERA POS {camPos} ===");
            
            // Calculate viewport coverage
            float viewWidth = BackgroundGenerator.VIEW_WIDTH_UNITS;
            float viewHeight = BackgroundGenerator.VIEW_HEIGHT_UNITS;
            
            float leftEdge = camPos.x - viewWidth / 2f;
            float rightEdge = camPos.x + viewWidth / 2f;
            float bottomEdge = camPos.y - viewHeight / 2f;
            float topEdge = camPos.y + viewHeight / 2f;
            
            Debug.Log($"Viewport coverage: X({leftEdge:F2} to {rightEdge:F2}), Y({bottomEdge:F2} to {topEdge:F2})");
            Debug.Log($"Viewport size: {viewWidth:F2} x {viewHeight:F2} units");
            
            // Check tile positions
            var bgLayers = Object.FindObjectsOfType<ViewportBackgroundLayer>();
            foreach (var layer in bgLayers)
            {
                if (layer.backgroundData?.Type == 3) // Full screen tiled
                {
                    layer.LogCoverage();
                }
            }
        }
    }
}