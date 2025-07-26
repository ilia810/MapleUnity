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
            // In C++ client, tiles are drawn at their exact position without z-axis manipulation
            // Sorting is handled entirely by sortingOrder, not z position
            Vector3 position = CoordinateConverter.ToUnityPosition(tileData.X, tileData.Y, 0);
            tile.transform.position = position;
            
            // Add tile component
            MapTile mapTile = tile.AddComponent<MapTile>();
            mapTile.tileSet = tileData.TileSet;
            mapTile.variant = tileData.Variant;
            mapTile.tileNumber = tileData.No;
            mapTile.layer = tileData.Layer;
            mapTile.z = tileData.Z;
            mapTile.zM = tileData.ZM;
            
            // Create sprite object
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.parent = tile.transform;
            spriteObj.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Tiles"; // Tiles should be on their own layer
            renderer.sortingOrder = CalculateSortingOrder(tileData);
            
            // Set the sorting order on the MapTile component after renderer is created
            mapTile.sortingOrder = renderer.sortingOrder;
            
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
                
                // The sprite is already created with the origin as its pivot point in SpriteLoader
                // No additional offset needed - the pivot handles the positioning correctly
                // Setting localPosition to zero ensures the sprite renders at the tile position
                renderer.transform.localPosition = Vector3.zero;
                
                // Store origin in tileData for debugging
                tileData.OriginX = (int)origin.x;
                tileData.OriginY = (int)origin.y;
                
                // Debug logging
                if (Random.Range(0, 100) == 0) // Log 1 in 100 tiles to avoid spam
                {
                    Debug.Log($"Tile L{tileData.Layer} {tileData.Variant}/{tileData.No}: " +
                             $"pos=({tileData.X},{tileData.Y}), origin=({origin.x},{origin.y}), " +
                             $"z={tileData.Z}, zM={tileData.ZM}");
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
            // Based on C++ client implementation:
            // - Tiles use z value for sorting, with zM as fallback when z is 0
            // - Layer 7 is background, layer 0 is foreground
            // - Within a layer, tiles are sorted by their depth value
            
            // Get the actual z value used by C++ client
            int actualZ = tileData.Z;
            if (actualZ == 0)
            {
                actualZ = tileData.ZM;
            }
            
            // Layer priority: layer 0 (front) gets highest base value
            // Using larger multiplier to ensure layers don't overlap
            int layerPriority = (7 - tileData.Layer) * 1000000;
            
            // Within layer, sort by z value
            // C++ client uses uint8_t (0-255) for z values
            int depthPriority = actualZ * 1000;
            
            // Use insertion order as tiebreaker (approximated by negative Y)
            // This maintains the order tiles were added to the multimap
            int tiebreaker = -tileData.Y;
            
            // Final sorting order
            return layerPriority + depthPriority + tiebreaker;
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
        public int layer;
        public int z;           // Base depth from tile image
        public int zM;          // Map-specific depth offset
        public int sortingOrder; // Final calculated sorting order
    }
}