using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameData;
using GameData;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Generates map tiles (ground tiles, platforms, etc.)
    /// </summary>
    public class TileGenerator
    {
        private NXDataManagerSingleton nxManager;
        
        public TileGenerator()
        {
            nxManager = NXDataManagerSingleton.Instance;
        }
        
        public GameObject GenerateTiles(List<TileData> tiles, Transform parent)
        {
            GameObject tileContainer = new GameObject("Tiles");
            tileContainer.transform.parent = parent;
            
            // Group tiles by their tile set
            var tileGroups = new Dictionary<string, List<TileData>>();
            foreach (var tile in tiles)
            {
                if (!tileGroups.ContainsKey(tile.TileSet))
                    tileGroups[tile.TileSet] = new List<TileData>();
                tileGroups[tile.TileSet].Add(tile);
            }
            
            // Create tiles for each tile set
            foreach (var kvp in tileGroups)
            {
                // Handle empty tileset name (becomes .img in C++ client)
                string tileSetName = string.IsNullOrEmpty(kvp.Key) ? ".img" : kvp.Key;
                GameObject tileSetContainer = new GameObject($"TileSet_{tileSetName}");
                tileSetContainer.transform.parent = tileContainer.transform;
                
                foreach (var tile in kvp.Value)
                {
                    CreateTile(tile, tileSetContainer.transform);
                }
            }
            
            return tileContainer;
        }
        
        private void CreateTile(TileData tileData, Transform parent)
        {
            // Handle empty tileset name for display
            string displayTileSet = string.IsNullOrEmpty(tileData.TileSet) ? ".img" : tileData.TileSet;
            GameObject tile = new GameObject($"Tile_L{tileData.Layer}_{displayTileSet}_{tileData.Variant}_{tileData.No}");
            tile.transform.parent = parent;
            
            // Set position
            Vector3 position = CoordinateConverter.ToUnityPosition(tileData.X, tileData.Y, 0);
            
            // Use Z value for depth sorting
            float zOrder = -tileData.Z * 0.01f; // Negative so higher Z values appear behind
            position.z = zOrder;
            
            tile.transform.position = position;
            
            // Add tile component
            MapTile mapTile = tile.AddComponent<MapTile>();
            mapTile.tileSet = tileData.TileSet;
            mapTile.variant = tileData.Variant;
            mapTile.tileNumber = tileData.No;
            mapTile.zOrder = tileData.Z;
            mapTile.zModifier = tileData.ZM;
            
            // Create sprite object
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.parent = tile.transform;
            spriteObj.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Tiles"; // Tiles should be on their own layer
            renderer.sortingOrder = CalculateSortingOrder(tileData);
            
            // Load sprite
            LoadTileSprite(tileData, renderer);
        }
        
        private void LoadTileSprite(TileData tileData, SpriteRenderer renderer)
        {
            // Use the specialized tile sprite loader with origin
            var (sprite, origin) = nxManager.GetTileSpriteWithOrigin(tileData.TileSet, tileData.Variant, tileData.No);
            
            if (sprite != null)
            {
                renderer.sprite = sprite;
                
                // Apply origin offset to the tile position
                // In MapleStory, the origin defines where the tile's anchor point is
                // The tile is positioned such that this origin point sits at the tile's coordinates
                // For example, a tile with origin (45,30) will have its (45,30) pixel at the tile position
                if (origin != Vector2.zero)
                {
                    // The sprite is already created with the origin as its pivot in SpriteLoader
                    // But we need to offset the position to account for the difference between
                    // the default center pivot and the actual origin
                    
                    // Calculate the offset from center to origin
                    float centerX = sprite.rect.width / 2f;
                    float centerY = sprite.rect.height / 2f;
                    float offsetX = (centerX - origin.x) / 100f;  // Convert pixels to Unity units
                    float offsetY = (origin.y - centerY) / 100f;  // Y is flipped in Unity
                    
                    // Apply offset to the sprite object
                    renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
                    
                    if (Random.Range(0, 20) == 0) // Log some tiles with origins
                    {
                        Debug.Log($"Tile {tileData.TileSet}/{tileData.Variant}/{tileData.No} has origin ({origin.x},{origin.y}), sprite size ({sprite.rect.width},{sprite.rect.height}), offset ({offsetX},{offsetY})");
                    }
                }
                
                // Store origin in tileData for debugging
                tileData.OriginX = (int)origin.x;
                tileData.OriginY = (int)origin.y;
                
                // More detailed logging
                if (Random.Range(0, 50) == 0) // Log 1 in 50 tiles to avoid spam
                {
                    Debug.Log($"Loaded tile L{tileData.Layer}: {tileData.TileSet}/{tileData.Variant}/{tileData.No} at ({tileData.X},{tileData.Y}) origin ({origin.x},{origin.y})");
                }
            }
            else
            {
                Debug.LogWarning($"Tile sprite not found: {tileData.TileSet}/{tileData.Variant}/{tileData.No}");
                // Create placeholder
                renderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        
        private int CalculateSortingOrder(TileData tileData)
        {
            // Calculate sorting order based on layer and Z value
            // Layer 0 is the bottom layer, layer 7 is the top
            // Each layer gets 1000 sorting order units
            int layerOrder = tileData.Layer * 1000;
            
            // Within each layer, use Z value for fine-grained sorting
            int zOrder = tileData.Z + tileData.ZM;
            
            // Base order puts tiles behind objects (-5000) but above backgrounds
            int baseOrder = -5000;
            
            return baseOrder + layerOrder + zOrder;
        }
    }
    
    /// <summary>
    /// Component for map tiles
    /// </summary>
    public class MapTile : MonoBehaviour
    {
        public string tileSet;
        public string variant;
        public int tileNumber;
        public int zOrder;
        public int zModifier;
    }
}