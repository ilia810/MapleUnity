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
                    var sprite = SpriteLoader.LoadSprite(partNode, $"{path}/{partName}");
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
                return SpriteLoader.LoadSprite(headPart, $"head/{skin}/{state}/{frame}");
            }
            
            // Try loading the frame directly
            return SpriteLoader.LoadSprite(frameNode, $"head/{skin}/{state}/{frame}");
        }
        
        /// <summary>
        /// Load face sprite
        /// </summary>
        public Sprite LoadFace(int faceId, string expression = "default")
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Face sprites are in Character/Face/{faceId:D8}.img/{expression}/0
            var faceNode = charFile.GetNode($"Face/{faceId:D8}.img/{expression}/0");
            
            return faceNode != null ? SpriteLoader.LoadSprite(faceNode) : null;
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
            
            return hairNode != null ? SpriteLoader.LoadSprite(hairNode) : null;
        }
        
        /// <summary>
        /// Load equipment sprite
        /// </summary>
        public Sprite LoadEquipment(int itemId, string category, string state, int frame)
        {
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            
            // Equipment sprites are in Character/{Category}/{itemId:D8}.img/{state}/{frame}
            var equipNode = charFile.GetNode($"{category}/{itemId:D8}.img/{state}/{frame}");
            
            return equipNode != null ? SpriteLoader.LoadSprite(equipNode) : null;
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