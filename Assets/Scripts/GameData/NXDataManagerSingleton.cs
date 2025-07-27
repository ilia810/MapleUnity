using UnityEngine;
using MapleClient.GameData;
using System;
using System.Linq;

namespace GameData
{
    // Force Unity recompile
    /// <summary>
    /// Singleton wrapper for NXDataManager to provide easy access across the application
    /// </summary>
    public class NXDataManagerSingleton : MonoBehaviour
    {
        private static NXDataManagerSingleton instance;
        private NXDataManager dataManager;
        
        public static NXDataManagerSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    // In editor mode, find existing instance first
                    if (!Application.isPlaying)
                    {
                        instance = FindObjectOfType<NXDataManagerSingleton>();
                        if (instance != null)
                        {
                            // Clean up existing instance in editor
                            DestroyImmediate(instance.gameObject);
                            instance = null;
                        }
                    }
                    
                    GameObject go = new GameObject("NXDataManager");
                    instance = go.AddComponent<NXDataManagerSingleton>();
                    
                    // Only use DontDestroyOnLoad in play mode
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                    else
                    {
                        // Hide in editor hierarchy
                        go.hideFlags = HideFlags.HideAndDontSave;
                    }
                    
                    instance.Initialize();
                }
                return instance;
            }
        }
        
        public NXDataManager DataManager => dataManager;
        
        private void Initialize()
        {
            dataManager = new NXDataManager();
            dataManager.Initialize();
            Debug.Log("NXDataManager initialized");
        }
        
        private void OnDestroy()
        {
            if (dataManager != null)
            {
                dataManager.Shutdown();
            }
        }
        
        /// <summary>
        /// Get a sprite from the Map.nx file
        /// </summary>
        public Sprite GetMapSprite(string path)
        {
            var node = dataManager.GetNode("map", path);
            if (node != null)
            {
                return SpriteLoader.LoadSprite(node, path);
            }
            return null;
        }
        
        /// <summary>
        /// Get a background sprite
        /// </summary>
        public Sprite GetBackgroundSprite(string bgName)
        {
            if (string.IsNullOrEmpty(bgName))
            {
                Debug.LogWarning("Empty background name provided");
                return null;
            }
            
            // Background paths in MapleStory can be:
            // 1. Just the name (e.g., "grassySoil")
            // 2. With subdirectory (e.g., "grassySoil/back/0")
            
            // Try different path patterns
            string[] possiblePaths = {
                $"Back/{bgName}.img/back/0",      // Most common: Back/grassySoil.img/back/0
                $"Back/{bgName}.img/0",           // Without 'back' subfolder
                $"Back/{bgName}/back/0",          // Without .img extension
                $"Back/{bgName}/0",               // Simple pattern with frame 0
                $"Back/{bgName}.img",             // Just the .img node
                $"Back/{bgName}",                 // Direct path
                bgName                            // Already includes full path
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found background at path: {path}");
                    
                    // If we got a container node (like grassySoil.img), try to get the actual sprite
                    if (node.Children.Any())
                    {
                        // Try to find the sprite in common sub-paths
                        var spriteNode = node["back"]?["0"] ?? node["0"] ?? node["back"] ?? node;
                        if (spriteNode != null)
                        {
                            var sprite = SpriteLoader.LoadSprite(spriteNode, path);
                            if (sprite != null) return sprite;
                        }
                    }
                    else
                    {
                        // Direct sprite node
                        var sprite = SpriteLoader.LoadSprite(node, path);
                        if (sprite != null) return sprite;
                    }
                }
            }
            
            // If not found in Back folder, try Tile folder (some backgrounds are tiles)
            string[] tilePaths = {
                $"Tile/{bgName}.img/bsc/0",          // Tile with bsc subfolder
                $"Tile/{bgName}/bsc/0",              // Without .img
                $"Tile/{bgName}.img/0",              // Direct frame
                $"Tile/{bgName}/0",                  // Simple path
                $"Tile/{bgName}.img",                // Just the tile container
                $"Tile/{bgName}"                     // Without extension
            };
            
            foreach (var path in tilePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found background in Tile folder at path: {path}");
                    
                    // If this is a container node, look for bsc/0 or any numbered child
                    if (node.Children.Any() && !IsImageNode(node))
                    {
                        // Try bsc subfolder first
                        var bscNode = node["bsc"];
                        if (bscNode != null && bscNode["0"] != null)
                        {
                            var sprite = SpriteLoader.LoadSprite(bscNode["0"], path + "/bsc/0");
                            if (sprite != null) return sprite;
                        }
                        
                        // Try direct numbered children
                        foreach (var child in node.Children)
                        {
                            if (int.TryParse(child.Name, out _))
                            {
                                var sprite = SpriteLoader.LoadSprite(child, path + "/" + child.Name);
                                if (sprite != null) 
                                {
                                    Debug.Log($"Found tile sprite at {path}/{child.Name}");
                                    return sprite;
                                }
                            }
                        }
                    }
                    else
                    {
                        var sprite = SpriteLoader.LoadSprite(node, path);
                        if (sprite != null) return sprite;
                    }
                }
            }
            
            Debug.LogWarning($"Background not found in any path for: {bgName}");
            Debug.Log($"Tried Back paths: {string.Join(", ", possiblePaths)}");
            Debug.Log($"Tried Tile paths: {string.Join(", ", tilePaths)}");
            return null;
        }
        
        /// <summary>
        /// Get an object sprite
        /// </summary>
        public Sprite GetObjectSprite(string objPath)
        {
            var (sprite, _) = GetObjectSpriteWithOrigin(objPath);
            return sprite;
        }
        
        /// <summary>
        /// Get an object sprite with origin information
        /// </summary>
        public (Sprite sprite, Vector2 origin) GetObjectSpriteWithOrigin(string objPath)
        {
            // Objects are in Map.nx/Obj/
            // objPath comes in like "Obj/guide.img/common/sign/0"
            
            // Clean up the path
            string cleanPath = objPath;
            if (cleanPath.StartsWith("Obj/"))
            {
                cleanPath = cleanPath.Substring(4); // Remove "Obj/" prefix
            }
            
            string[] possiblePaths = {
                $"Obj/{cleanPath}",                 // Full path under Obj
                cleanPath,                          // Direct path (if already includes Obj)
                objPath                             // Original path as-is
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found object at path: {path}");
                    
                    // Check if this is directly an image node
                    if (IsImageNode(node))
                    {
                        var result = SpriteLoader.LoadSpriteWithOrigin(node, path, dataManager);
                        if (result != null) return (result.Sprite, result.Origin);
                    }
                    // If we got a container node, it might have the actual sprite as a child
                    else if (node.Children.Any())
                    {
                        // For objects, sprites are often directly under the node
                        var firstChild = node.Children.FirstOrDefault();
                        if (firstChild != null && IsImageNode(firstChild))
                        {
                            var result = SpriteLoader.LoadSpriteWithOrigin(firstChild, path, dataManager);
                            if (result != null) return (result.Sprite, result.Origin);
                        }
                        
                        // Sometimes the sprite is nested deeper, try to find any image node
                        var imageNode = FindFirstImageNode(node);
                        if (imageNode != null)
                        {
                            var result = SpriteLoader.LoadSpriteWithOrigin(imageNode, path, dataManager);
                            if (result != null) return (result.Sprite, result.Origin);
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Object sprite not found for: {objPath}");
            return (null, Vector2.zero);
        }
        
        private INxNode FindFirstImageNode(INxNode node)
        {
            if (node == null) return null;
            
            if (IsImageNode(node)) return node;
            
            foreach (var child in node.Children)
            {
                var result = FindFirstImageNode(child);
                if (result != null) return result;
            }
            
            return null;
        }
        
        private bool IsImageNode(INxNode node)
        {
            // Check if this node represents an image
            // In NX files, image nodes typically have specific properties
            if (node == null) return false;
            
            // Check if node has image data
            try
            {
                var value = node.GetValue<object>();
                if (value is byte[]) return true;
                
                // Some nodes store the image data differently
                if (node["_inlink"] != null || node["_outlink"] != null) return true;
                
                // Check for image properties
                if (node["origin"] != null || node["z"] != null) return true;
                
                // Check if it's a PNG reference
                if (node.Name.EndsWith(".png")) return true;
                
                // Check if node has canvas properties (width/height)
                if (node["width"] != null && node["height"] != null) return true;
            }
            catch { }
            
            return false;
        }
        
        /// <summary>
        /// Get map node data
        /// </summary>
        public INxNode GetMapNode(int mapId)
        {
            string mapIdStr = mapId.ToString("D9");
            string imgName = mapIdStr + ".img";
            
            // Try different path patterns that MapleStory uses
            string[] possiblePaths = {
                $"Map/Map{mapIdStr[0]}/{imgName}",
                $"Map{mapIdStr[0]}/{imgName}",
                $"{imgName}",
                $"Map/{imgName}"
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found map {mapId} at path: {path}");
                    return node;
                }
            }
            
            Debug.LogError($"Could not find map {mapId} in any of these paths: {string.Join(", ", possiblePaths)}");
            return null;
        }
        
        /// <summary>
        /// Debug method to explore Map.nx structure
        /// </summary>
        public void DebugMapStructure()
        {
            var mapFile = dataManager.GetFile("map");
            if (mapFile == null)
            {
                Debug.LogError("Map.nx not loaded!");
                return;
            }
            
            var root = mapFile.Root;
            if (root == null)
            {
                Debug.LogError("Map.nx root is null!");
                return;
            }
            
            Debug.Log("Map.nx root children:");
            int count = 0;
            foreach (var child in root.Children)
            {
                Debug.Log($"  - {child.Name}");
                if (count++ > 20) 
                {
                    Debug.Log("  ... (truncated)");
                    break;
                }
            }
            
            // Check if there's a Map subfolder
            var mapNode = root["Map"];
            if (mapNode != null)
            {
                Debug.Log("Found 'Map' subfolder, children:");
                count = 0;
                foreach (var child in mapNode.Children)
                {
                    Debug.Log($"    - {child.Name}");
                    if (count++ > 10) 
                    {
                        Debug.Log("    ... (truncated)");
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Debug a specific background in detail
        /// </summary>
        public void DebugBackgroundDetail(string bgName)
        {
            var backNode = dataManager.GetNode("map", "Back");
            if (backNode == null)
            {
                Debug.LogError("Back node not found in Map.nx!");
                return;
            }
            
            var bgNode = backNode[$"{bgName}.img"];
            if (bgNode == null)
            {
                Debug.LogError($"Background {bgName}.img not found!");
                return;
            }
            
            Debug.Log($"Found {bgName}.img, exploring structure:");
            
            // Check for 'back' subfolder
            var backSubNode = bgNode["back"];
            if (backSubNode != null)
            {
                Debug.Log("  Has 'back' subfolder with children:");
                foreach (var child in backSubNode.Children)
                {
                    Debug.Log($"    - {child.Name} (Type: {child.GetType().Name}, Has Value: {child.Value != null})");
                    
                    // Check if it's frame 0
                    if (child.Name == "0")
                    {
                        Debug.Log("      Frame 0 details:");
                        Debug.Log($"        - Has Value: {child.Value != null}");
                        Debug.Log($"        - Value type: {child.Value?.GetType().Name ?? "null"}");
                        
                        // Check for image data
                        try
                        {
                            var bytes = child.GetValue<byte[]>();
                            if (bytes != null)
                            {
                                Debug.Log($"        - GetValue<byte[]>: {bytes.Length} bytes");
                                // Check PNG header
                                if (bytes.Length > 8)
                                {
                                    bool isPng = bytes[0] == 0x89 && bytes[1] == 0x50 && 
                                                bytes[2] == 0x4E && bytes[3] == 0x47;
                                    Debug.Log($"        - Is PNG: {isPng}");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log($"        - GetValue<byte[]> failed: {e.Message}");
                        }
                        
                        // Check children
                        if (child.Children.Any())
                        {
                            Debug.Log($"        - Has {child.Children.Count()} children");
                        }
                    }
                }
            }
            
            // Check for 'ani' subfolder (animated backgrounds)
            var aniNode = bgNode["ani"];
            if (aniNode != null)
            {
                Debug.Log("  Has 'ani' subfolder (animated background)");
            }
        }
        
        /// <summary>
        /// Debug method to explore Back folder structure
        /// </summary>
        public void DebugBackgroundStructure(string bgName = null)
        {
            var backNode = dataManager.GetNode("map", "Back");
            if (backNode == null)
            {
                Debug.LogError("Back node not found in Map.nx!");
                return;
            }
            
            if (bgName != null)
            {
                Debug.Log($"Looking for specific background: {bgName}");
                
                // Try direct lookup
                var bgNode = backNode[bgName];
                if (bgNode != null)
                {
                    Debug.Log($"Found background '{bgName}' directly, exploring structure:");
                    int count = 0;
                    foreach (var child in bgNode.Children)
                    {
                        Debug.Log($"  - {child.Name} (has {child.Children.Count()} children)");
                        if (count++ > 10) break;
                    }
                }
                else
                {
                    // Try with .img extension
                    bgNode = backNode[$"{bgName}.img"];
                    if (bgNode != null)
                    {
                        Debug.Log($"Found background '{bgName}.img', exploring structure:");
                        int count = 0;
                        foreach (var child in bgNode.Children)
                        {
                            Debug.Log($"  - {child.Name} (has {child.Children.Count()} children)");
                            if (count++ > 10) break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Background '{bgName}' not found in Back folder");
                        Debug.Log("Available backgrounds in Back folder:");
                        int count = 0;
                        foreach (var child in backNode.Children)
                        {
                            if (child.Name.ToLower().Contains(bgName.ToLower()))
                            {
                                Debug.Log($"  - POSSIBLE MATCH: {child.Name}");
                            }
                            else if (count < 10)
                            {
                                Debug.Log($"  - {child.Name}");
                                count++;
                            }
                        }
                    }
                }
            }
            else
            {
                // List all backgrounds
                Debug.Log("Backgrounds in Back folder:");
                int count = 0;
                foreach (var child in backNode.Children)
                {
                    Debug.Log($"  - {child.Name}");
                    if (count++ > 20) 
                    {
                        Debug.Log("  ... (truncated)");
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Search for backgrounds containing a keyword
        /// </summary>
        public void SearchBackgroundsContaining(string keyword)
        {
            var backNode = dataManager.GetNode("map", "Back");
            if (backNode == null)
            {
                Debug.LogError("Back node not found in Map.nx!");
                return;
            }
            
            Debug.Log($"Searching for backgrounds containing '{keyword}':");
            int foundCount = 0;
            foreach (var child in backNode.Children)
            {
                if (child.Name.ToLower().Contains(keyword.ToLower()))
                {
                    Debug.Log($"  - FOUND: {child.Name}");
                    foundCount++;
                    
                    // Show structure of first match
                    if (foundCount == 1 && child.Children.Any())
                    {
                        Debug.Log("    Structure:");
                        int subCount = 0;
                        foreach (var subChild in child.Children)
                        {
                            Debug.Log($"      - {subChild.Name}");
                            if (subCount++ > 5) break;
                        }
                    }
                }
            }
            
            if (foundCount == 0)
            {
                Debug.Log($"No backgrounds found containing '{keyword}'");
                
                // Also check in Obj folder
                var objNode = dataManager.GetNode("map", "Obj");
                if (objNode != null)
                {
                    Debug.Log($"Checking Obj folder for '{keyword}':");
                    int objCount = 0;
                    foreach (var child in objNode.Children)
                    {
                        if (child.Name.ToLower().Contains(keyword.ToLower()))
                        {
                            Debug.Log($"  - FOUND IN OBJ: {child.Name}");
                            foundCount++;
                            objCount++;
                        }
                    }
                    if (objCount == 0 && foundCount == 0)
                    {
                        // First few Obj entries for reference
                        Debug.Log("First few entries in Obj folder:");
                        int count = 0;
                        foreach (var child in objNode.Children)
                        {
                            Debug.Log($"    - {child.Name}");
                            if (count++ > 10) break;
                        }
                    }
                }
                
                // Also check in Tile folder
                var tileNode = dataManager.GetNode("map", "Tile");
                if (tileNode != null)
                {
                    Debug.Log($"Checking Tile folder for '{keyword}':");
                    foreach (var child in tileNode.Children)
                    {
                        if (child.Name.ToLower().Contains(keyword.ToLower()))
                        {
                            Debug.Log($"  - FOUND IN TILE: {child.Name}");
                            foundCount++;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get a tile sprite with origin information
        /// </summary>
        public (Sprite sprite, Vector2 origin) GetTileSpriteWithOrigin(string tileSet, string variant, int no)
        {
            // Handle empty tileset - C++ client appends .img to tS value
            // So empty tS becomes ".img"
            string actualTileSet = string.IsNullOrEmpty(tileSet) ? "" : tileSet;
            
            // Tiles are stored in Map.nx under Tile/{tileSet}.img/{variant}/{no}
            string[] possiblePaths = {
                $"Tile/{actualTileSet}.img/{variant}/{no}",     // Standard path (empty tS becomes Tile/.img)
                $"Tile/{actualTileSet}/{variant}/{no}",         // Without .img
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found tile sprite at path: {path}");
                    var result = SpriteLoader.LoadSpriteWithOrigin(node, path, dataManager);
                    if (result != null) return (result.Sprite, result.Origin);
                }
            }
            
            // Try fallback tile numbers for missing variants
            if (no > 0)
            {
                // For missing tile numbers, try variant 0 as fallback
                string[] fallbackPaths = {
                    $"Tile/{actualTileSet}.img/{variant}/0",
                    $"Tile/{actualTileSet}/{variant}/0",
                };
                
                foreach (var path in fallbackPaths)
                {
                    var node = dataManager.GetNode("map", path);
                    if (node != null)
                    {
                        Debug.LogWarning($"Using fallback tile {actualTileSet}/{variant}/0 for missing {no}");
                        var sprite = SpriteLoader.LoadSprite(node, path, dataManager);
                        var origin = SpriteLoader.GetOrigin(node, dataManager);
                        if (sprite != null) return (sprite, origin);
                    }
                }
            }
            
            Debug.LogWarning($"Tile sprite not found: {actualTileSet}/{variant}/{no}");
            return (null, Vector2.zero);
        }
        
        /// <summary>
        /// Get a tile sprite
        /// </summary>
        public Sprite GetTileSprite(string tileSet, string variant, int no)
        {
            // Handle empty tileset - C++ client appends .img to tS value
            // So empty tS becomes ".img"
            string actualTileSet = string.IsNullOrEmpty(tileSet) ? "" : tileSet;
            
            // Tiles are stored in Map.nx under Tile/{tileSet}.img/{variant}/{no}
            string[] possiblePaths = {
                $"Tile/{actualTileSet}.img/{variant}/{no}",     // Standard path (empty tS becomes Tile/.img)
                $"Tile/{actualTileSet}/{variant}/{no}",         // Without .img
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("map", path);
                if (node != null)
                {
                    Debug.Log($"Found tile sprite at path: {path}");
                    var sprite = SpriteLoader.LoadSprite(node, path);
                    if (sprite != null) return sprite;
                }
            }
            
            // Try fallback tile numbers for missing variants
            if (no > 0)
            {
                // For missing tile numbers, try variant 0 as fallback
                string[] fallbackPaths = {
                    $"Tile/{tileSet}.img/{variant}/0",
                    $"Tile/{tileSet}/{variant}/0",
                };
                
                foreach (var path in fallbackPaths)
                {
                    var node = dataManager.GetNode("map", path);
                    if (node != null)
                    {
                        Debug.LogWarning($"Using fallback tile {tileSet}/{variant}/0 for missing {no}");
                        var sprite = SpriteLoader.LoadSprite(node, path);
                        if (sprite != null) return sprite;
                    }
                }
                
                // For edge tiles, try the previous number
                if ((variant.StartsWith("ed") || variant.StartsWith("en")) && no > 1)
                {
                    string[] prevPaths = {
                        $"Tile/{tileSet}.img/{variant}/{no - 1}",
                        $"Tile/{tileSet}/{variant}/{no - 1}",
                    };
                    
                    foreach (var path in prevPaths)
                    {
                        var node = dataManager.GetNode("map", path);
                        if (node != null)
                        {
                            Debug.LogWarning($"Using fallback tile {tileSet}/{variant}/{no-1} for missing {no}");
                            var sprite = SpriteLoader.LoadSprite(node, path);
                            if (sprite != null) return sprite;
                        }
                    }
                }
            }
            
            // Try fallback tilesets if primary fails
            // This is useful when a tileset is missing some variants
            if (tileSet == "DeepgrassySoil" || tileSet == "brownBrick" || tileSet == "goldTempleTownTH")
            {
                // List of fallback tilesets to try
                string[] fallbackTileSets = { "grassySoil", "brownBrick", "DeepgrassySoil", "goldTempleTownTH" };
                
                foreach (var fallbackSet in fallbackTileSets)
                {
                    if (fallbackSet == tileSet) continue; // Skip the primary tileset
                    
                    string[] fallbackPaths = {
                        $"Tile/{fallbackSet}.img/{variant}/{no}",
                        $"Tile/{fallbackSet}/{variant}/{no}",
                    };
                    
                    foreach (var path in fallbackPaths)
                    {
                        var node = dataManager.GetNode("map", path);
                        if (node != null)
                        {
                            Debug.Log($"Using fallback tile sprite from {fallbackSet}: {path}");
                            var sprite = SpriteLoader.LoadSprite(node, path);
                            if (sprite != null) return sprite;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Tile sprite not found: {tileSet}/{variant}/{no}");
            Debug.LogWarning($"Tried paths: {string.Join(", ", possiblePaths)}");
            return null;
        }
        
        /// <summary>
        /// Debug method to check if .img tileset exists
        /// </summary>
        public bool CheckImgTilesetExists()
        {
            var imgTileset = dataManager.GetNode("map", "Tile/.img");
            if (imgTileset != null)
            {
                Debug.Log("FOUND .img tileset in NX data!");
                int variantCount = 0;
                foreach (var variant in imgTileset.Children)
                {
                    variantCount++;
                    if (variantCount <= 10)
                    {
                        Debug.Log($"  Variant: {variant.Name}");
                    }
                }
                Debug.Log($"Total variants in .img tileset: {variantCount}");
                return true;
            }
            else
            {
                Debug.LogError(".img tileset NOT FOUND in NX data!");
                return false;
            }
        }
        
        /// <summary>
        /// Get an NPC sprite
        /// </summary>
        public Sprite GetNPCSprite(string npcId)
        {
            var (sprite, _) = GetNPCSpriteWithOrigin(npcId);
            return sprite;
        }
        
        /// <summary>
        /// Get an NPC sprite with origin information
        /// </summary>
        public (Sprite sprite, Vector2 origin) GetNPCSpriteWithOrigin(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning("Empty NPC ID provided");
                return (null, Vector2.zero);
            }
            
            // NPCs are in Npc.nx/{npcId}.img/stand/0
            string[] possiblePaths = {
                $"{npcId}.img/stand/0",
                $"{npcId}/stand/0",
                $"{npcId}.img/stand/0/0",  // Sometimes has additional frame number
                $"{npcId}/stand/0/0"
            };
            
            foreach (var path in possiblePaths)
            {
                var node = dataManager.GetNode("npc", path);
                if (node != null)
                {
                    Debug.Log($"Found NPC sprite at path: {path}");
                    
                    // Check if this is directly an image node
                    if (IsImageNode(node))
                    {
                        var sprite = SpriteLoader.LoadSprite(node, path, dataManager);
                        var origin = SpriteLoader.GetOrigin(node, dataManager);
                        if (sprite != null) return (sprite, origin);
                    }
                    // If we got a container node, find the image
                    else if (node.Children.Any())
                    {
                        var imageNode = FindFirstImageNode(node);
                        if (imageNode != null)
                        {
                            var sprite = SpriteLoader.LoadSprite(imageNode, path, dataManager);
                            var origin = SpriteLoader.GetOrigin(imageNode, dataManager);
                            if (sprite != null) return (sprite, origin);
                        }
                    }
                }
            }
            
            Debug.LogWarning($"NPC sprite not found for ID: {npcId}");
            return (null, Vector2.zero);
        }
        
        /// <summary>
        /// Debug method to explore NPC structure
        /// </summary>
        public void DebugNPCStructure(string npcId = null)
        {
            var npcFile = dataManager.GetFile("npc");
            if (npcFile == null)
            {
                Debug.LogError("Npc.nx not loaded!");
                return;
            }
            
            if (npcId != null)
            {
                Debug.Log($"Looking for NPC: {npcId}");
                
                // Try to find the NPC
                var npcNode = npcFile.GetNode($"{npcId}.img");
                if (npcNode != null)
                {
                    Debug.Log($"Found NPC '{npcId}.img', exploring structure:");
                    int count = 0;
                    foreach (var child in npcNode.Children)
                    {
                        Debug.Log($"  - {child.Name} (has {child.Children.Count()} children)");
                        if (child.Name == "stand" && child.Children.Any())
                        {
                            Debug.Log("    Stand frames:");
                            foreach (var frame in child.Children)
                            {
                                Debug.Log($"      - {frame.Name}");
                            }
                        }
                        if (count++ > 10) break;
                    }
                }
                else
                {
                    Debug.LogWarning($"NPC '{npcId}' not found");
                }
            }
            else
            {
                // List first few NPCs
                Debug.Log("NPCs in Npc.nx:");
                int count = 0;
                foreach (var child in npcFile.Root.Children)
                {
                    Debug.Log($"  - {child.Name}");
                    if (count++ > 20) 
                    {
                        Debug.Log("  ... (truncated)");
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Debug a specific sprite path
        /// </summary>
        public void DebugSpritePath(string fullPath)
        {
            Debug.Log($"Debugging sprite path: {fullPath}");
            
            // Try to parse the path
            string file = "map"; // default
            string path = fullPath;
            
            if (fullPath.ToLower().Contains("npc") || 
                (fullPath.Length > 0 && char.IsDigit(fullPath[0]) && fullPath.Contains(".img")))
            {
                file = "npc";
                path = fullPath.Replace("Npc/", "").Replace("npc/", "");
            }
            
            var node = dataManager.GetNode(file, path);
            if (node == null)
            {
                Debug.LogWarning($"Node not found at: {file}/{path}");
                return;
            }
            
            Debug.Log($"Found node: {node.Name}");
            Debug.Log($"Node type: {node.GetType().Name}");
            Debug.Log($"Has Value: {node.Value != null}");
            Debug.Log($"Value type: {node.Value?.GetType().Name ?? "null"}");
            
            // Try to get value as byte[]
            try
            {
                var bytes = node.GetValue<byte[]>();
                Debug.Log($"GetValue<byte[]> returned: {(bytes != null ? $"{bytes.Length} bytes" : "null")}");
            }
            catch (Exception e)
            {
                Debug.Log($"GetValue<byte[]> failed: {e.Message}");
            }
            
            // Check children
            Debug.Log($"Children count: {node.Children.Count()}");
            if (node.Children.Any())
            {
                Debug.Log("First 5 children:");
                int count = 0;
                foreach (var child in node.Children)
                {
                    Debug.Log($"  - {child.Name} (Value: {child.Value != null}, Type: {child.Value?.GetType().Name ?? "null"})");
                    if (count++ >= 5) break;
                }
            }
            
            // Check for image-related properties
            var originNode = node["origin"];
            Debug.Log($"Has origin: {originNode != null}");
            
            var widthNode = node["width"];
            var heightNode = node["height"];
            Debug.Log($"Has width/height: {widthNode != null}/{heightNode != null}");
        }
        
        /// <summary>
        /// Clean up singleton instance (for editor use)
        /// </summary>
        public static void Cleanup()
        {
            if (instance != null)
            {
                if (instance.dataManager != null)
                {
                    instance.dataManager.Shutdown();
                }
                
                if (!Application.isPlaying)
                {
                    DestroyImmediate(instance.gameObject);
                }
                else
                {
                    Destroy(instance.gameObject);
                }
                
                instance = null;
            }
        }
    }
}