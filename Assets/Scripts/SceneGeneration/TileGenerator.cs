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
            renderer.sortingLayerName = "Tiles";
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
                
                // C++ client: draws at pos - origin
                // With top-left pivot, we need to consider coordinate system differences:
                // - MapleStory: Y+ is down, origin from top-left, subtract origin = move up+left
                // - Unity: Y+ is up, pivot at top-left
                // Since tile Y is already inverted by CoordinateConverter, origin.y behavior is inverted too
                float offsetX = -origin.x / 100f;  // Move left by origin.x
                float offsetY = origin.y / 100f;   // Move up by origin.y (inverted due to coordinate flip)
                
                renderer.transform.localPosition = new Vector3(offsetX, offsetY, 0);
                
                // Store origin in tileData for debugging
                tileData.OriginX = (int)origin.x;
                tileData.OriginY = (int)origin.y;
                
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
            // C++ client: Layers are drawn 0 to 7 (0 first/back, 7 last/front)
            // Within each layer, sorted by z value (lower z draws first)
            
            // Get the actual z value (use zM if z is 0)
            int actualZ = tileData.Z;
            if (actualZ == 0 && tileData.ZM != 0)
            {
                actualZ = tileData.ZM;
            }
            
            // Ensure actualZ is in valid range (0-255 as uint8_t)
            actualZ = Mathf.Clamp(actualZ, 0, 255);
            
            // Layer base: layer 0 = 0, layer 7 = 7000
            // This ensures lower layers render behind higher layers
            int layerBase = tileData.Layer * 1000;
            
            // Within each layer, sort by z value
            // Objects are drawn before tiles in C++, so tiles need higher sorting order
            return layerBase + actualZ + 256; // Small offset to ensure tiles draw after objects at same z
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