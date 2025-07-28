using System.Collections.Generic;
using UnityEngine;

namespace MapleClient.SceneGeneration
{
    /// <summary>
    /// Optimized object pool for background tiles to prevent constant instantiation/destruction
    /// </summary>
    public class BackgroundTilePool : MonoBehaviour
    {
        private static BackgroundTilePool instance;
        public static BackgroundTilePool Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject poolObj = new GameObject("BackgroundTilePool");
                    instance = poolObj.AddComponent<BackgroundTilePool>();
                    // Only use DontDestroyOnLoad in play mode
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(poolObj);
                    }
                }
                return instance;
            }
        }
        
        private Dictionary<Sprite, Queue<GameObject>> tilePools = new Dictionary<Sprite, Queue<GameObject>>();
        private Transform poolContainer;
        private int totalPooledTiles = 0;
        private const int MAX_POOL_SIZE_PER_SPRITE = 100;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // Create container for pooled objects
            poolContainer = new GameObject("PooledTiles").transform;
            poolContainer.parent = transform;
            poolContainer.gameObject.SetActive(false); // Hide pooled objects
        }
        
        public GameObject GetTile(Sprite sprite, string sortingLayer, int sortingOrder, float alpha = 1f, bool flipX = false)
        {
            if (sprite == null) return null;
            
            GameObject tile = null;
            
            // Check if we have a pool for this sprite
            if (!tilePools.ContainsKey(sprite))
            {
                tilePools[sprite] = new Queue<GameObject>();
            }
            
            Queue<GameObject> pool = tilePools[sprite];
            
            // Try to get from pool
            while (pool.Count > 0)
            {
                tile = pool.Dequeue();
                if (tile != null)
                {
                    // Reactivate and configure
                    tile.transform.parent = null;
                    tile.SetActive(true);
                    
                    SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
                    spriteRenderer.sortingLayerName = sortingLayer;
                    spriteRenderer.sortingOrder = sortingOrder;
                    
                    // Apply alpha
                    Color spriteColor = Color.white;
                    spriteColor.a = alpha;
                    spriteRenderer.color = spriteColor;
                    
                    // Apply flip
                    tile.transform.localScale = new Vector3(flipX ? -1 : 1, 1, 1);
                    
                    return tile;
                }
            }
            
            // Create new tile if none available
            tile = new GameObject("BackgroundTile");
            SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = sprite;
            tileRenderer.sortingLayerName = sortingLayer;
            tileRenderer.sortingOrder = sortingOrder;
            
            // Apply alpha
            Color tileColor = Color.white;
            tileColor.a = alpha;
            tileRenderer.color = tileColor;
            
            // Apply flip
            tile.transform.localScale = new Vector3(flipX ? -1 : 1, 1, 1);
            
            totalPooledTiles++;
            
            return tile;
        }
        
        public void ReturnTile(GameObject tile, Sprite sprite)
        {
            if (tile == null || sprite == null) return;
            
            // Reset tile state
            tile.SetActive(false);
            tile.transform.parent = poolContainer;
            tile.transform.position = Vector3.zero;
            tile.transform.rotation = Quaternion.identity;
            
            // Return to pool
            if (!tilePools.ContainsKey(sprite))
            {
                tilePools[sprite] = new Queue<GameObject>();
            }
            
            Queue<GameObject> pool = tilePools[sprite];
            
            // Only pool if we haven't exceeded the limit
            if (pool.Count < MAX_POOL_SIZE_PER_SPRITE)
            {
                pool.Enqueue(tile);
            }
            else
            {
                // Destroy excess tiles
                Destroy(tile);
                totalPooledTiles--;
            }
        }
        
        public void Clear()
        {
            foreach (var pool in tilePools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject tile = pool.Dequeue();
                    if (tile != null)
                    {
                        Destroy(tile);
                    }
                }
            }
            tilePools.Clear();
            totalPooledTiles = 0;
        }
        
        private void OnDestroy()
        {
            Clear();
            if (instance == this)
            {
                instance = null;
            }
        }
        
        public void LogPoolStats()
        {
            Debug.Log($"BackgroundTilePool Stats:");
            Debug.Log($"  Total pooled tiles: {totalPooledTiles}");
            Debug.Log($"  Sprite pools: {tilePools.Count}");
            foreach (var kvp in tilePools)
            {
                Debug.Log($"    {kvp.Key.name}: {kvp.Value.Count} tiles pooled");
            }
        }
    }
}