using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates background layers with viewport-based dynamic tiling
    /// Replicates the C++ client's behavior where backgrounds tile relative to camera view
    /// </summary>
    public class BackgroundGenerator
    {
        
        // Standard MapleStory viewport dimensions (in pixels)
        public const float VIEWPORT_WIDTH = 1024f;
        public const float VIEWPORT_HEIGHT = 768f;
        
        // Convert to Unity units (assuming 100 pixels per unit)
        public const float VIEW_WIDTH_UNITS = VIEWPORT_WIDTH / 100f;  // ~10.24 units
        public const float VIEW_HEIGHT_UNITS = VIEWPORT_HEIGHT / 100f; // ~7.68 units
        
        // Buffer tiles as per C++ client
        public const int TILE_BUFFER = 3;
        
        public GameObject GenerateBackgrounds(List<BackgroundData> backgrounds, Transform parent, Bounds vrBounds)
        {
            GameObject bgContainer = new GameObject("Backgrounds");
            bgContainer.transform.parent = parent;
            
            // Add the dynamic background manager component
            var bgManager = bgContainer.AddComponent<DynamicBackgroundManager>();
            bgManager.vrBounds = vrBounds;
            
            // Separate backgrounds and foregrounds
            var bgLayers = new List<BackgroundData>();
            var fgLayers = new List<BackgroundData>();
            
            foreach (var bg in backgrounds)
            {
                if (bg.Front == 0)
                    bgLayers.Add(bg);
                else
                    fgLayers.Add(bg);
            }
            
            // Sort by layer order (No field)
            bgLayers.Sort((a, b) => a.No.CompareTo(b.No));
            fgLayers.Sort((a, b) => a.No.CompareTo(b.No));
            
            // Create background layers
            GameObject bgLayerContainer = new GameObject("BackgroundLayers");
            bgLayerContainer.transform.parent = bgContainer.transform;
            CreateLayers(bgLayers, bgLayerContainer.transform, false, bgManager);
            
            // Create foreground layers
            GameObject fgLayerContainer = new GameObject("ForegroundLayers");
            fgLayerContainer.transform.parent = bgContainer.transform;
            CreateLayers(fgLayers, fgLayerContainer.transform, true, bgManager);
            
            return bgContainer;
        }
        
        private void CreateLayers(List<BackgroundData> layers, Transform parent, bool isForeground, DynamicBackgroundManager manager)
        {
            // Backgrounds should render before all map elements (tiles start at 0)
            // Use negative values for backgrounds, high positive for foregrounds
            int sortingOrder = isForeground ? 10000 : -1000;
            
            foreach (var layer in layers)
            {
                // Within backgrounds/foregrounds, use the layer's No field for ordering
                // Lower No values render first (further back)
                int layerSortingOrder = sortingOrder + (layer.No * 10);
                
                CreateBackgroundLayer(layer, parent, layerSortingOrder, isForeground, manager);
            }
        }
        
        private void CreateBackgroundLayer(BackgroundData bgData, Transform parent, int sortingOrder, bool isForeground, DynamicBackgroundManager manager)
        {
            Debug.Log($"Creating background layer {bgData.No}: {bgData.BgName} (Type: {bgData.Type}, Pos: {bgData.X},{bgData.Y})");
            
            // Skip entries without a background asset.
            if (string.IsNullOrEmpty(bgData.BgName))
            {
                Debug.Log($"  Skipping background layer {bgData.No} - no sprite name");
                return;
            }
            
            GameObject layerObj = new GameObject($"Layer_{bgData.No}_{bgData.BgName}");
            layerObj.transform.parent = parent;
            
            // Set position
            Vector3 position = CoordinateConverter.ToUnityPosition(bgData.X, bgData.Y, bgData.No);
            layerObj.transform.position = position;
            
            // Add viewport-based tiling component
            ViewportBackgroundLayer bgLayer = layerObj.AddComponent<ViewportBackgroundLayer>();
            bgLayer.backgroundData = bgData;
            bgLayer.sortingOrder = sortingOrder;
            bgLayer.isForeground = isForeground;
            bgLayer.manager = manager;
            
            bgLayer.Initialize();
        }
    }
}
