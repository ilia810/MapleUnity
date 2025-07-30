using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapleClient.GameData
{
    /// <summary>
    /// Centralized asset loader that manages access to NX files
    /// </summary>
    public class NXAssetLoader
    {
        private readonly Dictionary<string, INxFile> nxFiles;
        private static NXAssetLoader instance;
        
        public static NXAssetLoader Instance
        {
            get
            {
                if (instance == null)
                    instance = new NXAssetLoader();
                return instance;
            }
        }
        
        private NXAssetLoader()
        {
            nxFiles = new Dictionary<string, INxFile>();
        }
        
        public void RegisterNxFile(string name, INxFile file)
        {
            nxFiles[name.ToLower()] = file;
        }
        
        public INxFile GetNxFile(string name)
        {
            nxFiles.TryGetValue(name.ToLower(), out var file);
            return file;
        }
        
        /// <summary>
        /// Load a map background sprite
        /// </summary>
        public Sprite LoadMapBackground(string backgroundName)
        {
            if (string.IsNullOrEmpty(backgroundName))
                return null;
                
            var mapFile = GetNxFile("map");
            if (mapFile == null) return null;
            
            Debug.Log($"Loading map background: {backgroundName}");
            
            // Map backgrounds are in Map/Back/{name}.img
            var bgNode = mapFile.GetNode($"Back/{backgroundName}.img");
            if (bgNode == null)
            {
                // Try without .img
                bgNode = mapFile.GetNode($"Back/{backgroundName}");
                if (bgNode == null)
                {
                    Debug.LogWarning($"Background node not found: Back/{backgroundName}");
                    return null;
                }
            }
            
            // Background might have animation frames or be a single image
            return SpriteLoader.LoadSprite(bgNode, $"background/{backgroundName}");
        }
        
        /// <summary>
        /// Load a map tile sprite
        /// </summary>
        public Sprite LoadMapTile(string tileSet, string tileName)
        {
            if (string.IsNullOrEmpty(tileSet) || string.IsNullOrEmpty(tileName))
                return null;
                
            var mapFile = GetNxFile("map");
            if (mapFile == null) return null;
            
            // Tiles are in Map/Tile/{tileSet}.img/{tileName}
            var tileNode = mapFile.GetNode($"Tile/{tileSet}.img/{tileName}");
            if (tileNode == null)
            {
                tileNode = mapFile.GetNode($"Tile/{tileSet}/{tileName}");
            }
            
            return tileNode != null ? SpriteLoader.LoadSprite(tileNode) : null;
        }
        
        /// <summary>
        /// Load a map object sprite
        /// </summary>
        public Sprite LoadMapObject(string objSet, string objName, int frame = 0)
        {
            if (string.IsNullOrEmpty(objSet) || string.IsNullOrEmpty(objName))
                return null;
                
            var mapFile = GetNxFile("map");
            if (mapFile == null) return null;
            
            // Objects are in Map/Obj/{objSet}.img/{objName}/{frame}
            var objNode = mapFile.GetNode($"Obj/{objSet}.img/{objName}/{frame}");
            if (objNode == null)
            {
                objNode = mapFile.GetNode($"Obj/{objSet}/{objName}/{frame}");
            }
            
            return objNode != null ? SpriteLoader.LoadSprite(objNode) : null;
        }
        
        /// <summary>
        /// Load character body sprite
        /// </summary>
        public Sprite LoadCharacterBody(int skin, string state, int frame)
        {
            var charFile = GetNxFile("character");
            if (charFile == null)
            {
                Debug.LogError("Character NX file not found");
                return null;
            }
            
            // Body sprites follow C++ client structure: Character/00002000.img/{state}/{frame}
            // Each frame contains part nodes like "body", "arm", etc.
            // For different skins, use different files (00002000.img for skin 0, 00002001.img for skin 1, etc.)
            string skinPadded = skin.ToString("D2");
            string bodyFile = $"000020{skinPadded}.img";
            
            // Try skin-specific file first
            string path = $"{bodyFile}/{state}/{frame}";
            var frameNode = charFile.GetNode(path);
            
            // Fall back to default skin 0 if skin-specific file doesn't exist
            if (frameNode == null && skin != 0)
            {
                Debug.Log($"Skin {skin} not found, falling back to default skin 0");
                path = $"00002000.img/{state}/{frame}";
                frameNode = charFile.GetNode(path);
            }
            
            if (frameNode == null)
            {
                // Try without frame number first to see what's there
                var stateNode = charFile.GetNode($"{bodyFile}/{state}");
                if (stateNode == null)
                {
                    // Maybe animations are structured differently in reNX
                    // Let's check what's actually in the body file
                    var bodyImgNode = charFile.GetNode(bodyFile);
                    if (bodyImgNode != null)
                    {
                        Debug.LogWarning($"Character frame node not found at: {path}. Available children in {bodyFile}:");
                        int count = 0;
                        foreach (var child in bodyImgNode.Children)
                        {
                            Debug.Log($"  - {child.Name}");
                            if (++count >= 10) 
                            {
                                Debug.Log("  ... (more children)");
                                break;
                            }
                        }
                        
                        // Check if animations might be under a different structure
                        // Try common MapleStory body parts or animation names without numbers
                        string[] possiblePaths = { "body", "Body", "0", state.Replace("1", ""), state.Replace("2", "") };
                        foreach (var testPath in possiblePaths)
                        {
                            var testNode = bodyImgNode[testPath];
                            if (testNode != null)
                            {
                                Debug.Log($"Found node at {bodyFile}/{testPath}, exploring...");
                                count = 0;
                                foreach (var subchild in testNode.Children)
                                {
                                    Debug.Log($"    - {subchild.Name}");
                                    if (++count >= 5) break;
                                }
                            }
                        }
                        
                        // V92 structure might have animations under categories
                        // Try to find our animation under each category
                        Debug.Log($"Checking if animations are under categories in v92 structure...");
                        foreach (var category in bodyImgNode.Children.Take(10))
                        {
                            var animNode = category[state];
                            if (animNode == null && state.EndsWith("1"))
                            {
                                // Try without the number suffix
                                animNode = category[state.Substring(0, state.Length - 1)];
                            }
                            
                            if (animNode != null)
                            {
                                Debug.Log($"Found animation '{state}' under category '{category.Name}'!");
                                Debug.Log($"Checking for frame {frame}...");
                                var frameInCategory = animNode[frame.ToString()];
                                if (frameInCategory != null)
                                {
                                    Debug.Log($"Found frame! Returning node from {bodyFile}/{category.Name}/{state}/{frame}");
                                    // Update path and return this frame node
                                    path = $"{bodyFile}/{category.Name}/{state}/{frame}";
                                    frameNode = frameInCategory;
                                    return LoadBodyPartsFromFrame(frameNode, path, charFile);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Found state node but no frame {frame}. Available frames:");
                    foreach (var child in stateNode.Children)
                    {
                        Debug.Log($"  - {child.Name}");
                    }
                }
                return null;
            }
            
            return LoadBodyPartsFromFrame(frameNode, path, charFile);
        }
        
        private Sprite LoadBodyPartsFromFrame(INxNode frameNode, string path, INxFile charFile)
        {
            // Look for body parts within the frame - matching C++ client behavior
            // The C++ client iterates through all parts in the frame
            foreach (var partNode in frameNode.Children)
            {
                string partName = partNode.Name;
                
                // Skip non-body parts
                if (partName == "delay" || partName == "face")
                    continue;
                    
                // Body-related parts based on C++ layers_by_name map
                if (partName == "body" || partName == "backBody" || partName == "arm" || 
                    partName.StartsWith("arm") || partName.Contains("hand") || partName.Contains("Hand"))
                {
                    Debug.Log($"Found body part '{partName}' at: {path}/{partName}");
                    
                    // Handle link resolution
                    var resolvedNode = ResolveLinks(partNode, charFile);
                    if (resolvedNode == null)
                    {
                        Debug.LogWarning($"Could not resolve links for body part: {partName}");
                        continue;
                    }
                    
                    // Get the origin for this body part
                    Vector2 origin = Vector2.zero;
                    var originNode = resolvedNode["origin"];
                    if (originNode != null && originNode.Value is Vector2 vec)
                    {
                        origin = vec;
                    }
                    
                    var sprite = SpriteLoader.ConvertCharacterNodeToSprite(resolvedNode, $"{path}/{partName}", origin);
                    if (sprite != null)
                    {
                        // For now, return the first valid body part sprite
                        // Later we'll need to handle all parts and layer them properly
                        return sprite;
                    }
                }
            }
            
            Debug.LogWarning($"No valid body parts found in frame: {path}");
            return null;
        }
        
        /// <summary>
        /// Load all body parts for a character frame
        /// </summary>
        public Dictionary<string, Sprite> LoadCharacterBodyParts(int skin, string state, int frame, out Vector2? headAttachPoint)
        {
            headAttachPoint = null; // Initialize out parameter
            
            var charFile = GetNxFile("character");
            if (charFile == null)
            {
                Debug.LogError("Character NX file not found");
                return null;
            }
            
            // Body sprites follow C++ client structure
            string skinPadded = skin.ToString("D2");
            string bodyFile = $"000020{skinPadded}.img";
            
            // Try skin-specific file first
            string path = $"{bodyFile}/{state}/{frame}";
            var frameNode = charFile.GetNode(path);
            
            // Fall back to default skin 0 if skin-specific file doesn't exist
            if (frameNode == null && skin != 0)
            {
                Debug.Log($"Skin {skin} not found, falling back to default skin 0");
                path = $"00002000.img/{state}/{frame}";
                frameNode = charFile.GetNode(path);
            }
            
            if (frameNode == null)
            {
                // Try to find the frame under category structure (v92)
                var bodyImgNode = charFile.GetNode(bodyFile);
                if (bodyImgNode != null)
                {
                    foreach (var category in bodyImgNode.Children)
                    {
                        var animNode = category[state];
                        if (animNode == null && state.EndsWith("1"))
                        {
                            animNode = category[state.Substring(0, state.Length - 1)];
                        }
                        
                        if (animNode != null)
                        {
                            var frameInCategory = animNode[frame.ToString()];
                            if (frameInCategory != null)
                            {
                                path = $"{bodyFile}/{category.Name}/{state}/{frame}";
                                frameNode = frameInCategory;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (frameNode == null)
            {
                Debug.LogWarning($"Character frame node not found at: {path}");
                headAttachPoint = null;
                return null;
            }
            
            return LoadAllPartsFromFrame(frameNode, path, charFile, out headAttachPoint);
        }
        
        private Dictionary<string, Sprite> LoadAllPartsFromFrame(INxNode frameNode, string path, INxFile charFile, out Vector2? headAttachPoint)
        {
            var parts = new Dictionary<string, Sprite>();
            headAttachPoint = null;
            
            // Look for all body parts within the frame
            foreach (var partNode in frameNode.Children)
            {
                string partName = partNode.Name;
                
                // Check for head attachment point
                if (partName == "head")
                {
                    if (partNode.Value is Vector2 headPos)
                    {
                        headAttachPoint = headPos;
                        Debug.Log($"Found head attachment point at: {headPos}");
                    }
                    continue;
                }
                
                // Skip non-sprite parts
                if (partName == "delay" || partName == "face")
                    continue;
                
                Debug.Log($"Processing part '{partName}' at: {path}/{partName}");
                
                // Handle link resolution
                var resolvedNode = ResolveLinks(partNode, charFile);
                if (resolvedNode == null)
                {
                    Debug.LogWarning($"Could not resolve links for part: {partName}");
                    continue;
                }
                
                // Get the origin for this part
                Vector2 origin = Vector2.zero;
                var originNode = resolvedNode["origin"];
                if (originNode != null && originNode.Value is Vector2 vec)
                {
                    origin = vec;
                }
                
                var sprite = SpriteLoader.ConvertCharacterNodeToSprite(resolvedNode, $"{path}/{partName}", origin);
                if (sprite != null)
                {
                    parts[partName] = sprite;
                    Debug.Log($"Loaded part '{partName}': {sprite.rect.width}x{sprite.rect.height}");
                }
            }
            
            return parts;
        }
        
        /// <summary>
        /// Resolve _inlink and _outlink references to get the actual image data
        /// </summary>
        private INxNode ResolveLinks(INxNode node, INxFile file)
        {
            if (node == null) return null;
            
            // Check if node has direct image data
            if (node.Value is byte[])
            {
                return node;
            }
            
            // Check for _inlink (reference within same file)
            var inlinkNode = node["_inlink"];
            if (inlinkNode != null && inlinkNode.Value is string inlinkPath)
            {
                Debug.Log($"[NXAssetLoader] Following _inlink: {inlinkPath}");
                var linkedNode = file.GetNode(inlinkPath);
                if (linkedNode != null)
                {
                    // Recursively resolve in case the linked node also has links
                    return ResolveLinks(linkedNode, file);
                }
            }
            
            // Check for _outlink (reference to another file)
            var outlinkNode = node["_outlink"];
            if (outlinkNode != null && outlinkNode.Value is string outlinkPath)
            {
                Debug.Log($"[NXAssetLoader] Following _outlink: {outlinkPath}");
                // Outlinks usually reference paths like "Map/Tile/grassySoil.img/bsc/0"
                // We need to resolve this through the appropriate NX file
                
                // Extract the file type from the path
                string[] parts = outlinkPath.Split('/');
                if (parts.Length > 0)
                {
                    string fileType = parts[0].ToLower();
                    INxFile targetFile = GetNxFile(fileType);
                    
                    if (targetFile != null)
                    {
                        // Remove the file prefix from the path
                        string nodePath = string.Join("/", parts, 1, parts.Length - 1);
                        var linkedNode = targetFile.GetNode(nodePath);
                        if (linkedNode != null)
                        {
                            return ResolveLinks(linkedNode, targetFile);
                        }
                    }
                }
            }
            
            // Return the original node if no links found
            return node;
        }
        
        /// <summary>
        /// Load character head sprite
        /// </summary>
        public Sprite LoadCharacterHead(int skin, string state, int frame)
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Head sprites follow same structure as body
            string path = $"00012000.img/{state}/{frame}";
            var frameNode = charFile.GetNode(path);
            
            if (frameNode == null) return null;
            
            // Look for head part within the frame
            var headPart = frameNode["head"];
            if (headPart != null)
            {
                var resolvedNode = ResolveLinks(headPart, charFile);
                return SpriteLoader.LoadSprite(resolvedNode, $"head/{skin}/{state}/{frame}");
            }
            
            // Try loading the frame directly
            var resolvedFrame = ResolveLinks(frameNode, charFile);
            return SpriteLoader.LoadSprite(resolvedFrame, $"head/{skin}/{state}/{frame}");
        }
        
        /// <summary>
        /// Load face sprite
        /// </summary>
        public Sprite LoadFace(int faceId, string expression = "default")
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Face sprites can be in different locations:
            // 1. Character/Face/{faceId:D8}.img/{expression}/face
            // 2. Character/Face/{faceId:D8}.img/{expression} (directly)
            // 3. Character/Face/{faceId:D8}.img/{expression}/0 (legacy)
            
            string basePath = $"Face/{faceId:D8}.img/{expression}";
            
            // Try path 1: face subdirectory
            var faceNode = charFile.GetNode($"{basePath}/face");
            
            // Try path 2: expression node directly
            if (faceNode == null)
            {
                faceNode = charFile.GetNode(basePath);
                // Only use it if it contains image data
                if (faceNode != null && !(faceNode.Value is byte[]))
                {
                    faceNode = null;
                }
            }
            
            // Try path 3: legacy /0 path
            if (faceNode == null)
            {
                faceNode = charFile.GetNode($"{basePath}/0");
            }
            
            if (faceNode == null) return null;
            
            var resolvedNode = ResolveLinks(faceNode, charFile);
            return SpriteLoader.LoadSprite(resolvedNode, $"face/{faceId}/{expression}");
        }
        
        /// <summary>
        /// Load hair sprite
        /// </summary>
        public Sprite LoadHair(int hairId, string state, int frame)
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Hair sprites are in Character/Hair/{hairId:D8}.img/{state}/{frame}
            var hairNode = charFile.GetNode($"Hair/{hairId:D8}.img/{state}/{frame}");
            
            if (hairNode == null) return null;
            
            var resolvedNode = ResolveLinks(hairNode, charFile);
            return SpriteLoader.LoadSprite(resolvedNode, $"hair/{hairId}/{state}/{frame}");
        }
        
        /// <summary>
        /// Load equipment sprite
        /// </summary>
        public Sprite LoadEquipment(int itemId, string category, string state, int frame)
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Equipment sprites are in Character/{Category}/{itemId:D8}.img/{state}/{frame}
            string equipPath = $"{category}/{itemId:D8}.img/{state}/{frame}";
            var frameNode = charFile.GetNode(equipPath);
            
            if (frameNode == null) 
            {
                Debug.LogWarning($"Equipment frame not found at: {equipPath}");
                return null;
            }
            
            // Equipment frames often have multiple parts (similar to body frames)
            // Look for the actual sprite part within the frame
            foreach (var partNode in frameNode.Children)
            {
                string partName = partNode.Name;
                
                // Skip non-sprite nodes
                if (partName == "delay" || partName == "origin") continue;
                
                Debug.Log($"Processing equipment part '{partName}' at: {equipPath}/{partName}");
                
                // Handle link resolution
                var resolvedNode = ResolveLinks(partNode, charFile);
                if (resolvedNode != null)
                {
                    // Get the origin for this part
                    Vector2 origin = Vector2.zero;
                    var originNode = partNode["origin"];
                    if (originNode != null && originNode.Value is Vector2 vec)
                    {
                        origin = vec;
                    }
                    
                    var sprite = SpriteLoader.ConvertCharacterNodeToSprite(resolvedNode, $"{equipPath}/{partName}", origin);
                    if (sprite != null)
                    {
                        return sprite; // Return first valid sprite part
                    }
                }
            }
            
            // If no parts found, try loading the frame directly (older format)
            var resolvedFrame = ResolveLinks(frameNode, charFile);
            return SpriteLoader.LoadSprite(resolvedFrame, equipPath);
        }
        
        /// <summary>
        /// Load item icon
        /// </summary>
        public Sprite LoadItemIcon(int itemId)
        {
            var itemFile = GetNxFile("item");
            if (itemFile == null) return null;
            
            // Determine item category
            string category = GetItemCategory(itemId);
            
            // Icons are in Item/{category}/{itemId:D8}.img/info/icon
            var iconNode = itemFile.GetNode($"{category}/{itemId:D8}.img/info/icon");
            
            return iconNode != null ? SpriteLoader.LoadSprite(iconNode) : null;
        }
        
        /// <summary>
        /// Load mob sprite
        /// </summary>
        public Sprite LoadMobSprite(int mobId, string action, int frame)
        {
            var mobFile = GetNxFile("mob");
            if (mobFile == null) return null;
            
            // Mob sprites are in Mob/{mobId:D7}.img/{action}/{frame}
            var mobNode = mobFile.GetNode($"{mobId:D7}.img/{action}/{frame}");
            
            return mobNode != null ? SpriteLoader.LoadSprite(mobNode) : null;
        }
        
        /// <summary>
        /// Load skill effect sprite
        /// </summary>
        public Sprite LoadSkillEffect(int skillId, string effect, int frame)
        {
            var skillFile = GetNxFile("skill");
            if (skillFile == null) return null;
            
            // Skill effects are in Skill/{jobId}.img/skill/{skillId}/{effect}/{frame}
            int jobId = skillId / 10000;
            var effectNode = skillFile.GetNode($"{jobId:D3}.img/skill/{skillId}/{effect}/{frame}");
            
            return effectNode != null ? SpriteLoader.LoadSprite(effectNode) : null;
        }
        
        /// <summary>
        /// Load UI element
        /// </summary>
        public Sprite LoadUIElement(string category, string element)
        {
            var uiFile = GetNxFile("ui");
            if (uiFile == null) return null;
            
            // UI elements are in UI/UIWindow.img/{category}/{element}
            var uiNode = uiFile.GetNode($"UIWindow.img/{category}/{element}");
            if (uiNode == null)
            {
                // Try Basic.img
                uiNode = uiFile.GetNode($"Basic.img/{category}/{element}");
            }
            
            return uiNode != null ? SpriteLoader.LoadSprite(uiNode) : null;
        }
        
        private string GetItemCategory(int itemId)
        {
            int type = itemId / 1000000;
            switch (type)
            {
                case 1: return "Eqp"; // Equipment
                case 2: return "Consume"; // Consumables
                case 3: return "Install"; // Setup
                case 4: return "Etc"; // Etc
                case 5: return "Cash"; // Cash
                default: return "Etc";
            }
        }
    }
}