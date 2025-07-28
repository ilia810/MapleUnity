using UnityEngine;

namespace MapleClient.GameView
{
    /// <summary>
    /// Centralized rendering configuration for MapleUnity
    /// Ensures consistent settings across all components
    /// </summary>
    public static class RenderingConfiguration
    {
        // Camera settings for MapleStory's 1024x768 viewport
        public const float CAMERA_ORTHOGRAPHIC_SIZE = 3.84f; // 768 pixels / 2 / 100
        public const float CAMERA_NEAR_CLIP = -100f;
        public const float CAMERA_FAR_CLIP = 100f;
        
        // Background color (light blue similar to MapleStory)
        public static readonly Color CAMERA_BACKGROUND_COLOR = new Color(0.53f, 0.81f, 0.92f); // Sky blue
        
        // Sorting layer names (must match TagManager)
        public const string SORTING_LAYER_BACKGROUND = "Background";
        public const string SORTING_LAYER_TILES = "Tiles";
        public const string SORTING_LAYER_OBJECTS = "Objects";
        public const string SORTING_LAYER_NPCS = "NPCs";
        public const string SORTING_LAYER_EFFECTS = "Effects";
        public const string SORTING_LAYER_FOREGROUND = "Foreground";
        public const string SORTING_LAYER_UI = "UI";
        
        // Sorting order bases
        public const int BACKGROUND_SORTING_BASE = -1000;
        public const int TILE_SORTING_BASE = 0;
        public const int OBJECT_SORTING_BASE = 0;
        public const int NPC_SORTING_BASE = 500;
        public const int FOREGROUND_SORTING_BASE = 1000;
        
        // Optimization settings
        public const float BACKGROUND_UPDATE_THRESHOLD = 0.5f; // Only update backgrounds if camera moves this far
        public const int BACKGROUND_TILE_BUFFER = 3; // Extra tiles to render off-screen
        
        /// <summary>
        /// Configure main camera for MapleStory rendering
        /// </summary>
        public static void ConfigureCamera(Camera camera)
        {
            if (camera == null) return;
            
            camera.orthographic = true;
            camera.orthographicSize = CAMERA_ORTHOGRAPHIC_SIZE;
            camera.nearClipPlane = CAMERA_NEAR_CLIP;
            camera.farClipPlane = CAMERA_FAR_CLIP;
            camera.backgroundColor = CAMERA_BACKGROUND_COLOR;
            camera.clearFlags = CameraClearFlags.SolidColor;
            
            // Remove Y offset to prevent misalignment
            var pos = camera.transform.position;
            camera.transform.position = new Vector3(pos.x, 0, pos.z);
        }
        
        /// <summary>
        /// Get sorting order for a specific layer and depth
        /// </summary>
        public static int GetSortingOrder(string layerName, int depth)
        {
            int baseOrder = 0;
            
            switch (layerName)
            {
                case SORTING_LAYER_BACKGROUND:
                    baseOrder = BACKGROUND_SORTING_BASE;
                    break;
                case SORTING_LAYER_TILES:
                    baseOrder = TILE_SORTING_BASE;
                    break;
                case SORTING_LAYER_OBJECTS:
                    baseOrder = OBJECT_SORTING_BASE;
                    break;
                case SORTING_LAYER_NPCS:
                    baseOrder = NPC_SORTING_BASE;
                    break;
                case SORTING_LAYER_FOREGROUND:
                    baseOrder = FOREGROUND_SORTING_BASE;
                    break;
            }
            
            return baseOrder + depth;
        }
        
        /// <summary>
        /// Log sorting layer configuration
        /// </summary>
        public static void LogSortingLayerInfo()
        {
            Debug.Log("MapleUnity Sorting Layer Configuration:");
            Debug.Log($"  Background: base order {BACKGROUND_SORTING_BASE}");
            Debug.Log($"  Tiles: base order {TILE_SORTING_BASE}");
            Debug.Log($"  Objects: base order {OBJECT_SORTING_BASE}");
            Debug.Log($"  NPCs: base order {NPC_SORTING_BASE}");
            Debug.Log($"  Foreground: base order {FOREGROUND_SORTING_BASE}");
            Debug.Log("Ensure these layers exist in TagManager.asset");
        }
    }
}