using System;
using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Renders MapleStory maps using actual game assets from NX files
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        private IAssetProvider assetProvider;
        private MapData currentMapData;
        
        // Map layers
        private GameObject backgroundLayer;
        private GameObject tileLayer;
        private GameObject objectLayer;
        private GameObject foregroundLayer;
        
        // Cached sprites and materials
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        
        private Sprite ConvertToUnitySprite(MapleClient.GameLogic.Data.SpriteData spriteData)
        {
            if (spriteData == null || spriteData.ImageData == null) return null;
            
            // Check cache first
            if (spriteData.Name != null && spriteCache.ContainsKey(spriteData.Name))
                return spriteCache[spriteData.Name];
            
            Texture2D texture = new Texture2D(spriteData.Width, spriteData.Height, TextureFormat.ARGB32, false);
            texture.LoadImage(spriteData.ImageData);
            texture.filterMode = FilterMode.Point; // Pixel-perfect rendering
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new UnityEngine.Vector2(spriteData.OriginX / (float)texture.width, spriteData.OriginY / (float)texture.height),
                100f // Pixels per unit
            );
            
            // Cache the sprite
            if (spriteData.Name != null)
                spriteCache[spriteData.Name] = sprite;
            
            return sprite;
        }
        
        private Sprite CreateColoredSprite(Color color, int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new UnityEngine.Vector2(0.5f, 0.5f), // Center pivot
                100f // Pixels per unit
            );
            sprite.name = name;
            return sprite;
        }
        
        public void Initialize(IAssetProvider assetProvider)
        {
            this.assetProvider = assetProvider;
            
            // Create layer containers
            backgroundLayer = new GameObject("BackgroundLayer");
            backgroundLayer.transform.parent = transform;
            
            tileLayer = new GameObject("TileLayer");
            tileLayer.transform.parent = transform;
            
            objectLayer = new GameObject("ObjectLayer");
            objectLayer.transform.parent = transform;
            
            foregroundLayer = new GameObject("ForegroundLayer");
            foregroundLayer.transform.parent = transform;
        }
        
        public void RenderMap(MapData mapData)
        {
            currentMapData = mapData;
            ClearMap();
            
            Debug.Log($"Rendering map: {mapData.Name} (ID: {mapData.MapId})");
            
            // Load map data from NX
            var mapInfo = assetProvider.MapData.GetMapInfo(mapData.MapId);
            if (mapInfo == null)
            {
                Debug.LogError($"Failed to load map info for map ID: {mapData.MapId}");
                // Fall back to rendering platforms from mapData
                RenderPlatformsFromMapData(mapData);
                return;
            }
            
            // Render backgrounds
            RenderBackgrounds(mapInfo);
            
            // Render tiles (platforms)
            RenderTiles(mapInfo);
            
            Debug.Log($"Tiles rendered: {tileLayer.transform.childCount}, Platforms in mapData: {mapData.Platforms.Count}");
            
            // Always render platforms for collision visualization
            if (mapData.Platforms.Count > 0)
            {
                Debug.Log($"Rendering {mapData.Platforms.Count} collision platforms");
                RenderPlatformsFromMapData(mapData);
            }
            
            // Render objects (decorations, etc.)
            RenderObjects(mapInfo);
            
            // Render foreground elements
            RenderForeground(mapInfo);
            
            // Set map bounds for camera
            SetupMapBounds(mapInfo);
        }
        
        private void RenderBackgrounds(IMapInfo mapInfo)
        {
            // MapleStory maps have multiple background layers with parallax scrolling
            var backgrounds = mapInfo.GetBackgrounds();
            
            int layerIndex = 0;
            foreach (var bg in backgrounds)
            {
                GameObject bgObject = new GameObject($"Background_{layerIndex}");
                bgObject.transform.parent = backgroundLayer.transform;
                
                SpriteRenderer spriteRenderer = bgObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = ConvertToUnitySprite(bg.Sprite);
                spriteRenderer.sortingLayerName = "Background";
                spriteRenderer.sortingOrder = layerIndex;
                
                // Position based on background data
                bgObject.transform.position = new UnityEngine.Vector3(bg.X / 100f, bg.Y / 100f, 10f + layerIndex);
                
                // Add parallax scrolling component if needed
                if (bg.ScrollRate != 0)
                {
                    ParallaxScroll parallax = bgObject.AddComponent<ParallaxScroll>();
                    parallax.scrollRate = bg.ScrollRate;
                }
                
                layerIndex++;
            }
        }
        
        private void RenderTiles(IMapInfo mapInfo)
        {
            // MapleStory uses tile-based maps
            var tiles = mapInfo.GetTiles();
            
            foreach (var tile in tiles)
            {
                GameObject tileObject = new GameObject($"Tile_{tile.Id}");
                tileObject.transform.parent = tileLayer.transform;
                
                SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
                var sprite = ConvertToUnitySprite(tile.Sprite);
                spriteRenderer.sprite = sprite;
                
                // If no sprite loaded, create a colored square for debugging
                if (sprite == null)
                {
                    // Use smaller tile size for debugging
                    sprite = CreateColoredSprite(new Color(0.3f, 0.3f, 0.3f, 0.5f), 32, 32, "TileDebug");
                    spriteRenderer.sprite = sprite;
                }
                
                spriteRenderer.sortingLayerName = "Default"; // Use Default layer for now
                
                // Position tile
                tileObject.transform.position = new UnityEngine.Vector3(tile.X / 100f, tile.Y / 100f, 0f);
                
                // Add collider for platforms
                if (tile.IsSolid)
                {
                    BoxCollider2D collider = tileObject.AddComponent<BoxCollider2D>();
                    collider.size = new UnityEngine.Vector2(tile.Width / 100f, tile.Height / 100f);
                }
            }
        }
        
        private void RenderObjects(IMapInfo mapInfo)
        {
            // Render decorative objects, NPCs spawn points, etc.
            var objects = mapInfo.GetObjects();
            
            foreach (var obj in objects)
            {
                GameObject objObject = new GameObject($"Object_{obj.Id}");
                objObject.transform.parent = objectLayer.transform;
                
                SpriteRenderer spriteRenderer = objObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = ConvertToUnitySprite(obj.Sprite);
                spriteRenderer.sortingLayerName = "Objects";
                spriteRenderer.sortingOrder = obj.Z; // Layer depth
                
                objObject.transform.position = new UnityEngine.Vector3(obj.X / 100f, obj.Y / 100f, obj.Z * 0.01f);
                
                // Some objects might be animated
                if (obj.IsAnimated)
                {
                    MapleAnimator animator = objObject.AddComponent<MapleAnimator>();
                    // Convert SpriteData[] to Sprite[]
                    Sprite[] unityFrames = null;
                    if (obj.AnimationFrames != null)
                    {
                        unityFrames = new Sprite[obj.AnimationFrames.Length];
                        for (int i = 0; i < obj.AnimationFrames.Length; i++)
                        {
                            unityFrames[i] = ConvertToUnitySprite(obj.AnimationFrames[i]);
                        }
                    }
                    animator.SetAnimation(unityFrames);
                }
            }
        }
        
        private void RenderForeground(IMapInfo mapInfo)
        {
            // Render foreground elements that appear in front of characters
            var foregrounds = mapInfo.GetForegrounds();
            
            foreach (var fg in foregrounds)
            {
                GameObject fgObject = new GameObject($"Foreground_{fg.Id}");
                fgObject.transform.parent = foregroundLayer.transform;
                
                SpriteRenderer spriteRenderer = fgObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = ConvertToUnitySprite(fg.Sprite);
                spriteRenderer.sortingLayerName = "Foreground";
                
                fgObject.transform.position = new UnityEngine.Vector3(fg.X / 100f, fg.Y / 100f, -5f);
            }
        }
        
        private void SetupMapBounds(IMapInfo mapInfo)
        {
            // Set up camera bounds based on map size
            var bounds = mapInfo.GetBounds();
            
            Debug.Log($"Map bounds - Left: {bounds.Left}, Right: {bounds.Right}, Top: {bounds.Top}, Bottom: {bounds.Bottom}");
            
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Also check camera position
                Debug.Log($"Camera position: {mainCamera.transform.position}");
                
                CameraController cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.SetMapBounds(
                        bounds.Left / 100f,
                        bounds.Right / 100f,
                        bounds.Bottom / 100f,
                        bounds.Top / 100f
                    );
                }
            }
        }
        
        private void RenderPlatformsFromMapData(MapData mapData)
        {
            Debug.Log($"Creating {mapData.Platforms.Count} invisible collision platforms from MapData");
            
            // Debug: Log platform data to understand coordinate system
            foreach (var platform in mapData.Platforms)
            {
                Debug.Log($"[Platform Data] ID: {platform.Id}, X: [{platform.X1}, {platform.X2}], Y: [{platform.Y1}, {platform.Y2}]");
            }
            
            // Create invisible collision platforms - visuals come from the tileset
            foreach (var platform in mapData.Platforms)
            {
                GameObject platformObject = new GameObject($"Platform_{platform.Id}_Collision");
                platformObject.transform.parent = tileLayer.transform;
                
                // Don't create visual components - platforms are invisible
                // The actual platform visuals come from the tile sprites
                
                // Add a collider for physics
                BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
                float width = Mathf.Abs(platform.X2 - platform.X1) / 100f;
                float height = 0.5f; // Make collider thicker for better collision detection
                
                // Calculate the center position between the two points
                float centerX = (platform.X1 + platform.X2) / 200f; // Average and convert to units
                // In MapleStory, Y coordinates are inverted (negative Y is up)
                float centerY = -(platform.Y1 + platform.Y2) / 200f; // Negate Y for Unity's coordinate system
                
                // Position at the center of the platform line
                platformObject.transform.position = new UnityEngine.Vector3(centerX, centerY, 0);
                
                // Set the collider size and no offset since we're positioning at center
                collider.size = new UnityEngine.Vector2(width, height);
                collider.offset = UnityEngine.Vector2.zero;
                
                // Set layer to Default (which both player and platforms use)
                platformObject.layer = LayerMask.NameToLayer("Default");
                
                Debug.Log($"[COLLISION_DEBUG] Created invisible collision platform: {platformObject.name} at {platformObject.transform.position} with collider size: {collider.size}");
            }
        }
        
        private void ClearMap()
        {
            // Clear all child objects
            foreach (Transform child in backgroundLayer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (Transform child in tileLayer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (Transform child in objectLayer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (Transform child in foregroundLayer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Simple parallax scrolling for backgrounds
    /// </summary>
    public class ParallaxScroll : MonoBehaviour
    {
        public float scrollRate = 0.5f;
        private UnityEngine.Vector3 startPosition;
        private Transform cameraTransform;
        
        void Start()
        {
            startPosition = transform.position;
            cameraTransform = Camera.main.transform;
        }
        
        void LateUpdate()
        {
            if (cameraTransform != null)
            {
                float distance = cameraTransform.position.x * scrollRate;
                transform.position = new UnityEngine.Vector3(startPosition.x + distance, startPosition.y, startPosition.z);
            }
        }
    }
    
    /// <summary>
    /// Handles MapleStory sprite animations
    /// </summary>
    public class MapleAnimator : MonoBehaviour
    {
        private Sprite[] frames;
        private int currentFrame;
        private float frameTime = 0.1f; // 100ms per frame (typical for MapleStory)
        private float timer;
        private SpriteRenderer spriteRenderer;
        
        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void SetAnimation(Sprite[] animationFrames)
        {
            frames = animationFrames;
            currentFrame = 0;
            timer = 0;
            
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }
        
        void Update()
        {
            if (frames == null || frames.Length <= 1)
                return;
                
            timer += Time.deltaTime;
            
            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame = (currentFrame + 1) % frames.Length;
                spriteRenderer.sprite = frames[currentFrame];
            }
        }
    }
}