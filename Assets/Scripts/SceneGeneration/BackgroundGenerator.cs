using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameData;
using GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates background layers with viewport-based dynamic tiling
    /// Replicates the C++ client's behavior where backgrounds tile relative to camera view
    /// </summary>
    public class BackgroundGenerator
    {
        private NXDataManagerSingleton nxManager;
        
        // Standard MapleStory viewport dimensions (in pixels)
        public const float VIEWPORT_WIDTH = 1024f;
        public const float VIEWPORT_HEIGHT = 768f;
        
        // Convert to Unity units (assuming 100 pixels per unit)
        public const float VIEW_WIDTH_UNITS = VIEWPORT_WIDTH / 100f;  // ~10.24 units
        public const float VIEW_HEIGHT_UNITS = VIEWPORT_HEIGHT / 100f; // ~7.68 units
        
        // Buffer tiles as per C++ client
        public const int TILE_BUFFER = 3;
        
        public BackgroundGenerator()
        {
            nxManager = NXDataManagerSingleton.Instance;
        }
        
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
            
            // Skip backgrounds without sprite names unless they're type 3 (color layers)
            if (string.IsNullOrEmpty(bgData.BgName) && bgData.Type != 3)
            {
                Debug.Log($"  Skipping background layer {bgData.No} - no sprite name and not a color layer");
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
            
            // Load sprite
            var sprite = LoadBackgroundSprite(bgData.BgName, bgData.Ani != 0);
            if (sprite != null)
            {
                bgLayer.tileSprite = sprite;
                bgLayer.Initialize();
            }
        }
        
        private Sprite LoadBackgroundSprite(string bgName, bool isAnimated)
        {
            Debug.Log($"Loading background sprite: {bgName} (animated: {isAnimated})");
            
            // Check if bgName is empty or a generated name
            if (string.IsNullOrEmpty(bgName) || bgName.StartsWith("layer"))
            {
                // This layer doesn't have a sprite - it might be a solid color or gradient
                Debug.Log($"Background layer '{bgName}' has no sprite - using transparent");
                return null;
            }
            
            // Load from NX data
            var sprite = nxManager.GetBackgroundSprite(bgName);
            if (sprite != null)
            {
                Debug.Log($"Successfully loaded background sprite: {bgName}, size: {sprite.rect.width}x{sprite.rect.height}");
                
                // Check if the sprite is a solid color (like grassySoil)
                var texture = sprite.texture;
                if (texture.width == 256 && texture.height == 256)
                {
                    // This is likely a sky/base color layer
                    Debug.Log($"Background {bgName} appears to be a base color layer (256x256)");
                }
            }
            else
            {
                // Placeholder
                Debug.LogWarning($"Background sprite not found: {bgName}");
            }
            
            if (isAnimated)
            {
                // TODO: Add animation component
                Debug.Log($"Background {bgName} is animated - animation not implemented yet");
            }
            
            return sprite;
        }
    }
    
    /// <summary>
    /// Manages all background layers and updates them based on camera position
    /// </summary>
    public class DynamicBackgroundManager : MonoBehaviour
    {
        public Bounds vrBounds { get; set; }
        private Transform cameraTransform;
        private Vector3 lastCameraPosition;
        
        private void Start()
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                lastCameraPosition = cameraTransform.position;
            }
        }
        
        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            
            // Track camera movement
            Vector3 currentCameraPos = cameraTransform.position;
            if (currentCameraPos != lastCameraPosition)
            {
                lastCameraPosition = currentCameraPos;
                // Background layers will update themselves
            }
        }
    }
    
    /// <summary>
    /// Handles viewport-based tiling for a single background layer
    /// Replicates C++ client behavior of tiling relative to camera view
    /// </summary>
    public class ViewportBackgroundLayer : MonoBehaviour
    {
        public BackgroundData backgroundData { get; set; }
        public Sprite tileSprite { get; set; }
        public int sortingOrder { get; set; }
        public bool isForeground { get; set; }
        public DynamicBackgroundManager manager { get; set; }
        
        private Transform cameraTransform;
        private float tileWidth;
        private float tileHeight;
        private int horizontalTiles;
        private int verticalTiles;
        
        private List<GameObject> activeTiles = new List<GameObject>();
        private Queue<GameObject> tilePool = new Queue<GameObject>();
        
        // Parallax rates
        private float parallaxRateX;
        private float parallaxRateY;
        
        public void Initialize()
        {
            if (tileSprite == null) return;
            
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null) return;
            
            // Get tile dimensions in Unity units
            tileWidth = tileSprite.bounds.size.x;
            tileHeight = tileSprite.bounds.size.y;
            
            // Calculate parallax rates
            parallaxRateX = backgroundData.RX / 100f;
            parallaxRateY = backgroundData.RY / 100f;
            
            // Calculate tiles needed based on background type
            CalculateTileCount();
            
            // Create initial tiles
            UpdateTiling();
        }
        
        private void CalculateTileCount()
        {
            // Default to no tiling
            horizontalTiles = 1;
            verticalTiles = 1;
            
            // Use the viewport size constants from BackgroundGenerator
            float viewWidth = BackgroundGenerator.VIEW_WIDTH_UNITS;
            float viewHeight = BackgroundGenerator.VIEW_HEIGHT_UNITS;
            
            switch (backgroundData.Type)
            {
                case 0: // Normal - no tiling
                    horizontalTiles = 1;
                    verticalTiles = 1;
                    break;
                    
                case 1: // Horizontal tiling
                    horizontalTiles = Mathf.CeilToInt(viewWidth / tileWidth) + BackgroundGenerator.TILE_BUFFER;
                    verticalTiles = 1;
                    break;
                    
                case 2: // Vertical tiling (rare)
                    horizontalTiles = 1;
                    verticalTiles = Mathf.CeilToInt(viewHeight / tileHeight) + BackgroundGenerator.TILE_BUFFER;
                    break;
                    
                case 3: // Full screen tiling
                    horizontalTiles = Mathf.CeilToInt(viewWidth / tileWidth) + BackgroundGenerator.TILE_BUFFER;
                    verticalTiles = Mathf.CeilToInt(viewHeight / tileHeight) + BackgroundGenerator.TILE_BUFFER;
                    break;
                    
                case 4: // Scrolling (treated like horizontal for now)
                    horizontalTiles = Mathf.CeilToInt(viewWidth / tileWidth) + BackgroundGenerator.TILE_BUFFER;
                    verticalTiles = 1;
                    break;
            }
            
            Debug.Log($"Background {backgroundData.BgName} type {backgroundData.Type}: {horizontalTiles}x{verticalTiles} tiles needed");
            Debug.Log($"  Tile size: {tileWidth}x{tileHeight} units");
            Debug.Log($"  Parallax rates: RX={parallaxRateX} ({backgroundData.RX}%), RY={parallaxRateY} ({backgroundData.RY}%)");
            Debug.Log($"  Background position: {transform.position}");
        }
        
        private void LateUpdate()
        {
            if (cameraTransform == null || tileSprite == null) return;
            
            // Update tiling based on camera position
            UpdateTiling();
        }
        
        private void UpdateTiling()
        {
            Vector3 cameraPos = cameraTransform.position;
            
            // Calculate the starting position for the tile grid
            float baseX, baseY;
            
            // Type 3 backgrounds ALWAYS follow the camera to create infinite tiling
            if (backgroundData.Type == 3)
            {
                // The tiles are always centered on the camera
                baseX = cameraPos.x;
                baseY = cameraPos.y;
                
                // Apply a small offset based on the background's origin position and parallax
                // This creates the scrolling effect, but the tiles still follow the camera
                float offsetX = transform.position.x * (1f - parallaxRateX);
                float offsetY = transform.position.y * (1f - parallaxRateY);
                
                // Wrap the offset to create seamless tiling
                if (horizontalTiles > 1)
                {
                    offsetX = offsetX % tileWidth;
                    baseX += offsetX;
                    
                    // Center the tile grid on the camera
                    baseX -= (horizontalTiles * tileWidth) / 2f;
                }
                
                if (verticalTiles > 1)
                {
                    offsetY = offsetY % tileHeight;
                    baseY += offsetY;
                    
                    // Center the tile grid on the camera
                    baseY -= (verticalTiles * tileHeight) / 2f;
                }
            }
            else if (horizontalTiles > 1 || verticalTiles > 1)
            {
                // Other tiled types (1, 2, 4) - use standard parallax with tiling
                baseX = transform.position.x + (cameraPos.x * (1f - parallaxRateX));
                baseY = transform.position.y + (cameraPos.y * (1f - parallaxRateY));
                
                // For horizontal tiling (Type 1, 4)
                if (horizontalTiles > 1)
                {
                    // Wrap to camera position for seamless tiling
                    float offsetX = cameraPos.x - baseX;
                    offsetX = offsetX % tileWidth;
                    baseX = cameraPos.x - offsetX - (horizontalTiles * tileWidth) / 2f;
                }
                
                // For vertical tiling (Type 2)
                if (verticalTiles > 1)
                {
                    float offsetY = cameraPos.y - baseY;
                    offsetY = offsetY % tileHeight;
                    baseY = cameraPos.y - offsetY - (verticalTiles * tileHeight) / 2f;
                }
            }
            else
            {
                // Non-tiled backgrounds (Type 0) - simple parallax
                baseX = transform.position.x + (cameraPos.x - transform.position.x) * parallaxRateX;
                baseY = transform.position.y + (cameraPos.y - transform.position.y) * parallaxRateY;
            }
            
            // Clear existing tiles
            foreach (var tile in activeTiles)
            {
                tile.SetActive(false);
                tilePool.Enqueue(tile);
            }
            activeTiles.Clear();
            
            // Create/reuse tiles for current view
            for (int x = 0; x < horizontalTiles; x++)
            {
                for (int y = 0; y < verticalTiles; y++)
                {
                    GameObject tile = GetOrCreateTile();
                    
                    // Position tile
                    float tileX = baseX + (x * tileWidth);
                    float tileY = baseY + (y * tileHeight);
                    tile.transform.position = new Vector3(tileX, tileY, transform.position.z);
                    
                    tile.SetActive(true);
                    activeTiles.Add(tile);
                }
            }
        }
        
        private GameObject GetOrCreateTile()
        {
            GameObject tile;
            
            if (tilePool.Count > 0)
            {
                tile = tilePool.Dequeue();
            }
            else
            {
                tile = new GameObject($"Tile");
                tile.transform.parent = transform;
                
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = tileSprite;
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = sortingOrder;
                
                // Apply alpha if needed
                if (backgroundData.A < 255)
                {
                    Color color = renderer.color;
                    color.a = backgroundData.A / 255f;
                    renderer.color = color;
                }
                
                // Apply flip if needed
                if (backgroundData.F != 0)
                {
                    tile.transform.localScale = new Vector3(-1, 1, 1);
                }
            }
            
            return tile;
        }
        
        // Debug method to check coverage
        public void LogCoverage()
        {
            if (activeTiles.Count == 0) return;
            
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            
            foreach (var tile in activeTiles)
            {
                if (tile.activeSelf)
                {
                    Vector3 pos = tile.transform.position;
                    var renderer = tile.GetComponent<SpriteRenderer>();
                    if (renderer != null && renderer.sprite != null)
                    {
                        minX = Mathf.Min(minX, pos.x - renderer.bounds.extents.x);
                        maxX = Mathf.Max(maxX, pos.x + renderer.bounds.extents.x);
                        minY = Mathf.Min(minY, pos.y - renderer.bounds.extents.y);
                        maxY = Mathf.Max(maxY, pos.y + renderer.bounds.extents.y);
                    }
                }
            }
            
            Debug.Log($"Background {backgroundData.BgName} coverage: X({minX:F2} to {maxX:F2}), Y({minY:F2} to {maxY:F2})");
            Debug.Log($"  Total coverage: {maxX - minX:F2} x {maxY - minY:F2} units");
            Debug.Log($"  Camera at: {cameraTransform.position}");
        }
    }
}