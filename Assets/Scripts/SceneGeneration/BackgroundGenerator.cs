using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameData;
using GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates background and foreground layers with parallax scrolling
    /// </summary>
    public class BackgroundGenerator
    {
        private NXDataManagerSingleton nxManager;
        
        public BackgroundGenerator()
        {
            nxManager = NXDataManagerSingleton.Instance;
        }
        
        private Bounds mapBounds; // Store map bounds for tiling calculations
        
        public GameObject GenerateBackgrounds(List<BackgroundData> backgrounds, Transform parent, Bounds vrBounds)
        {
            this.mapBounds = vrBounds; // Store for use in tiling methods
            
            GameObject bgContainer = new GameObject("Backgrounds");
            bgContainer.transform.parent = parent;
            
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
            CreateLayers(bgLayers, bgLayerContainer.transform, false);
            
            // Create foreground layers
            GameObject fgLayerContainer = new GameObject("ForegroundLayers");
            fgLayerContainer.transform.parent = bgContainer.transform;
            CreateLayers(fgLayers, fgLayerContainer.transform, true);
            
            return bgContainer;
        }
        
        private void CreateLayers(List<BackgroundData> layers, Transform parent, bool isForeground)
        {
            int sortingOrder = isForeground ? 100 : -100;
            
            foreach (var layer in layers)
            {
                // Sky backgrounds (type 3) should render furthest back
                int layerSortingOrder = sortingOrder;
                if (!isForeground && layer.Type == 3)
                {
                    layerSortingOrder = -500; // Far behind everything else
                }
                
                CreateBackgroundLayer(layer, parent, layerSortingOrder, isForeground);
                sortingOrder += isForeground ? 1 : -1;
            }
        }
        
        private void CreateBackgroundLayer(BackgroundData bgData, Transform parent, int sortingOrder, bool isForeground)
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
            
            // Add parallax component
            ParallaxLayer parallax = layerObj.AddComponent<ParallaxLayer>();
            parallax.parallaxRateX = bgData.RX / 100f;
            parallax.parallaxRateY = bgData.RY / 100f;
            parallax.layerType = bgData.Type;
            parallax.isForeground = isForeground;
            
            // Create sprite object
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.parent = layerObj.transform;
            spriteObj.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = isForeground ? "Foreground" : "Background";
            renderer.sortingOrder = sortingOrder;
            
            // Set alpha
            if (bgData.A < 255)
            {
                Color color = renderer.color;
                color.a = bgData.A / 255f;
                renderer.color = color;
            }
            
            // Flip if needed
            if (bgData.F != 0)
            {
                spriteObj.transform.localScale = new Vector3(-1, 1, 1);
            }
            
            // Handle different background types
            // Type 0: Static single image
            // Type 1: Tiled horizontally
            // Type 2: Tiled vertically (?)
            // Type 3: Tiled both directions (sky/color fill)
            // Type 4: Scrolling effect
            
            // Load sprite
            LoadBackgroundSprite(bgData.BgName, renderer, bgData.Ani != 0);
            
            // Handle tiling based on type
            if (bgData.Type == 1)
            {
                // Horizontal tiling
                CreateTiledBackground(bgData, layerObj, renderer.sprite, sortingOrder);
            }
            else if (bgData.Type == 3)
            {
                // Full screen tiling (sky/color backgrounds)
                CreateFullScreenTiledBackground(bgData, layerObj, renderer.sprite, sortingOrder);
            }
        }
        
        private void LoadBackgroundSprite(string bgName, SpriteRenderer renderer, bool isAnimated)
        {
            Debug.Log($"Loading background sprite: {bgName} (animated: {isAnimated})");
            
            // Check if bgName is empty or a generated name
            if (string.IsNullOrEmpty(bgName) || bgName.StartsWith("layer"))
            {
                // This layer doesn't have a sprite - it might be a solid color or gradient
                Debug.Log($"Background layer '{bgName}' has no sprite - using transparent");
                renderer.sprite = null;
                renderer.color = new Color(0, 0, 0, 0); // Transparent
                return;
            }
            
            // Load from NX data
            var sprite = nxManager.GetBackgroundSprite(bgName);
            if (sprite != null)
            {
                renderer.sprite = sprite;
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
                renderer.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
            
            if (isAnimated)
            {
                // TODO: Add animation component
                Debug.Log($"Background {bgName} is animated - animation not implemented yet");
            }
        }
        
        private void CreateTiledBackground(BackgroundData bgData, GameObject parent, Sprite sprite, int sortingOrder)
        {
            if (sprite == null) return;
            
            // Calculate tile count based on screen size
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;
            
            // Get camera bounds (approximate)
            float screenWidth = 20f; // Adjust based on your camera setup
            float screenHeight = 12f;
            
            int tilesX = Mathf.CeilToInt(screenWidth / spriteWidth) + 2;
            int tilesY = bgData.Type == 1 ? Mathf.CeilToInt(screenHeight / spriteHeight) + 2 : 1;
            
            // Create tile container
            GameObject tileContainer = new GameObject("Tiles");
            tileContainer.transform.parent = parent.transform;
            tileContainer.transform.localPosition = Vector3.zero;
            
            // Create tiles
            for (int x = -tilesX / 2; x <= tilesX / 2; x++)
            {
                for (int y = -tilesY / 2; y <= tilesY / 2; y++)
                {
                    if (x == 0 && y == 0) continue; // Skip center (already created)
                    
                    GameObject tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.parent = tileContainer.transform;
                    tile.transform.localPosition = new Vector3(x * spriteWidth, y * spriteHeight, 0);
                    
                    SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
                    tileRenderer.sprite = sprite;
                    tileRenderer.sortingLayerName = parent.GetComponentInChildren<SpriteRenderer>().sortingLayerName;
                    tileRenderer.sortingOrder = sortingOrder;
                    
                    // Copy color/alpha
                    tileRenderer.color = parent.GetComponentInChildren<SpriteRenderer>().color;
                }
            }
            
            // Add tile manager component
            parent.AddComponent<TiledBackgroundManager>();
        }
        
        private void CreateFullScreenTiledBackground(BackgroundData bgData, GameObject parent, Sprite sprite, int sortingOrder)
        {
            if (sprite == null) return;
            
            Debug.Log($"Creating full screen tiled background for {bgData.BgName} (type 3, sorting order: {sortingOrder})");
            
            // For type 3 backgrounds (like grassySoil), we need to tile across the entire visible area
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;
            
            // Use actual map bounds for tiling area
            float areaWidth = mapBounds.size.x > 0 ? mapBounds.size.x : 40f;
            float areaHeight = mapBounds.size.y > 0 ? mapBounds.size.y : 30f;
            
            // Add padding to ensure full coverage
            areaWidth += spriteWidth * 2;
            areaHeight += spriteHeight * 2;
            
            int tilesX = Mathf.CeilToInt(areaWidth / spriteWidth) + 2;
            int tilesY = Mathf.CeilToInt(areaHeight / spriteHeight) + 2;
            
            // Create tile container
            GameObject tileContainer = new GameObject("FullScreenTiles");
            tileContainer.transform.parent = parent.transform;
            tileContainer.transform.localPosition = Vector3.zero;
            
            // Set Z position to push it back
            tileContainer.transform.localPosition = new Vector3(0, 0, 10f); // Push back in Z
            
            // Create tiles to fill the screen, centered on map bounds
            Vector3 mapCenter = mapBounds.center;
            float startX = mapCenter.x - (tilesX * spriteWidth) / 2f;
            float startY = mapCenter.y - (tilesY * spriteHeight) / 2f;
            
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    GameObject tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.parent = tileContainer.transform;
                    tile.transform.localPosition = new Vector3(
                        startX + (x * spriteWidth), 
                        startY + (y * spriteHeight), 
                        0);
                    
                    SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
                    tileRenderer.sprite = sprite;
                    tileRenderer.sortingLayerName = "Background"; // Fixed layer name
                    tileRenderer.sortingOrder = sortingOrder;
                    
                    // Apply alpha if needed
                    if (bgData.A < 255)
                    {
                        Color color = tileRenderer.color;
                        color.a = bgData.A / 255f;
                        tileRenderer.color = color;
                    }
                }
            }
            
            Debug.Log($"Created {tilesX * tilesY} tiles for full screen background");
        }
    }
    
    /// <summary>
    /// Handles parallax scrolling for background layers
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public float parallaxRateX = 1f;
        public float parallaxRateY = 1f;
        public int layerType = 0; // 0 = normal, 1 = tiled, 2 = special
        public bool isForeground = false;
        
        private Transform cameraTransform;
        private Vector3 lastCameraPosition;
        private Vector3 startPosition;
        
        private void Start()
        {
            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;
            startPosition = transform.position;
        }
        
        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            
            Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
            
            // Apply parallax
            Vector3 parallaxMovement = new Vector3(
                deltaMovement.x * (1f - parallaxRateX),
                deltaMovement.y * (1f - parallaxRateY),
                0
            );
            
            transform.position += parallaxMovement;
            
            lastCameraPosition = cameraTransform.position;
        }
    }
    
    /// <summary>
    /// Manages tiled backgrounds to ensure seamless tiling
    /// </summary>
    public class TiledBackgroundManager : MonoBehaviour
    {
        private Transform cameraTransform;
        private SpriteRenderer mainRenderer;
        private float tileWidth;
        private float tileHeight;
        
        private void Start()
        {
            cameraTransform = Camera.main.transform;
            mainRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (mainRenderer != null && mainRenderer.sprite != null)
            {
                tileWidth = mainRenderer.sprite.bounds.size.x;
                tileHeight = mainRenderer.sprite.bounds.size.y;
            }
        }
        
        private void Update()
        {
            if (cameraTransform == null || mainRenderer == null) return;
            
            // Reposition tiles if camera moves too far
            // This creates infinite scrolling effect for tiled backgrounds
            Vector3 cameraPos = cameraTransform.position;
            Vector3 myPos = transform.position;
            
            // Calculate offset and wrap around
            float offsetX = cameraPos.x - myPos.x;
            float offsetY = cameraPos.y - myPos.y;
            
            if (Mathf.Abs(offsetX) > tileWidth)
            {
                float wrapX = Mathf.Floor(offsetX / tileWidth) * tileWidth;
                transform.position = new Vector3(myPos.x + wrapX, myPos.y, myPos.z);
            }
            
            if (Mathf.Abs(offsetY) > tileHeight)
            {
                float wrapY = Mathf.Floor(offsetY / tileHeight) * tileHeight;
                transform.position = new Vector3(myPos.x, myPos.y + wrapY, myPos.z);
            }
        }
    }
}