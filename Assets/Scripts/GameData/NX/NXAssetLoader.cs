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
        
        public void UnregisterNxFile(string name, INxFile file)
        {
            string key = name.ToLower();
            if (nxFiles.TryGetValue(key, out var registered) && ReferenceEquals(registered, file))
                nxFiles.Remove(key);
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
        public Dictionary<string, Sprite> LoadCharacterBodyParts(int skin, string state, int frame, out Dictionary<string, Vector2> attachmentPoints)
        {
            attachmentPoints = new Dictionary<string, Vector2>(); // Initialize out parameter
            
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
                attachmentPoints = null;
                return null;
            }
            
            return LoadAllPartsFromFrame(frameNode, path, charFile, out attachmentPoints);
        }
        
        private Dictionary<string, Sprite> LoadAllPartsFromFrame(INxNode frameNode, string path, INxFile charFile, out Dictionary<string, Vector2> attachmentPoints)
        {
            var parts = new Dictionary<string, Sprite>();
            attachmentPoints = new Dictionary<string, Vector2>();
            var nodes = new Dictionary<string, INxNode>();
            foreach (var part in frameNode.Children)
            {
                if (part.Name == "delay" || part.Name == "face") continue;
                var node = ResolveLinks(part, charFile);
                if (node == null) continue;
                nodes[part.Name] = node;
                var map = node["map"];
                if (map == null) continue;
                foreach (var point in map.Children)
                    if (point.Value is Vector2 position)
                        attachmentPoints[$"{part.Name}.map.{point.Name}"] = position;
            }
            Vector2 bodyPosition = attachmentPoints.TryGetValue("body.map.navel", out var navel) ? navel : Vector2.zero;
            Vector2 handPosition = Vector2.zero;
            foreach (var entry in nodes)
                if (entry.Value["z"]?.GetValue<string>() == "handBelowWeapon")
                    handPosition = entry.Value["map"]?["handMove"]?.GetValue<Vector2>() ?? Vector2.zero;
            attachmentPoints["source.handPosition"] = handPosition;
            foreach (var entry in nodes)
            {
                if (entry.Key == "head") continue;
                var node = entry.Value;
                string layer = node["z"]?.GetValue<string>() ?? entry.Key;
                if (layer == "backBody") layer = "body";
                Vector2 shift = layer == "handBelowWeapon"
                    ? handPosition - (node["map"]?["handMove"]?.GetValue<Vector2>() ?? Vector2.zero)
                    : bodyPosition - (node["map"]?["navel"]?.GetValue<Vector2>() ?? Vector2.zero);
                var sprite = SpriteLoader.LoadSpriteWithShift(node, shift, $"{path}/{entry.Key}");
                if (sprite != null && !parts.ContainsKey(layer)) parts[layer] = sprite;
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
        /// Load character head sprite with attachment points
        /// </summary>
        public Sprite LoadCharacterHead(int skin, string state, int frame, out Dictionary<string, Vector2> attachmentPoints)
        {
            attachmentPoints = new Dictionary<string, Vector2>();

            var charFile = GetNxFile("character");
            if (charFile == null) return null;

            // Head sprites follow same structure as body
            string path = $"00012000.img/{state}/{frame}";
            var frameNode = charFile.GetNode(path);

            if (frameNode == null) return null;

            // Extract head attachment points from the frame
            ExtractAttachmentPoints(frameNode, attachmentPoints, "head");

            // Look for head part within the frame
            var headPart = frameNode["head"];
            if (headPart != null)
            {
                var resolvedNode = ResolveLinks(headPart, charFile);

                // Also extract attachment points from the head part itself
                ExtractAttachmentPoints(resolvedNode, attachmentPoints, "head");

                // Check for map node which may contain additional attachment points
                var mapNode = resolvedNode["map"];
                if (mapNode != null)
                {
                    ExtractAttachmentPoints(mapNode, attachmentPoints, "head.map");
                }

                return SpriteLoader.LoadSprite(resolvedNode, $"head/{skin}/{state}/{frame}");
            }

            // Try loading the frame directly
            var resolvedFrame = ResolveLinks(frameNode, charFile);
            return SpriteLoader.LoadSprite(resolvedFrame, $"head/{skin}/{state}/{frame}");
        }

        /// <summary>
        /// Load character head sprite with shift applied (C++ style)
        /// The shift is calculated as: body.neck - head.neck
        /// </summary>
        public Sprite LoadCharacterHeadWithShift(int skin, string state, int frame, Vector2 headShift, out Dictionary<string, Vector2> attachmentPoints)
        {
            attachmentPoints = new Dictionary<string, Vector2>();

            var charFile = GetNxFile("character");
            if (charFile == null) return null;

            string path = $"00012000.img/{state}/{frame}";
            var frameNode = charFile.GetNode(path);

            if (frameNode == null) return null;

            ExtractAttachmentPoints(frameNode, attachmentPoints, "head");

            var headPart = frameNode["head"];
            if (headPart != null)
            {
                var resolvedNode = ResolveLinks(headPart, charFile);
                ExtractAttachmentPoints(resolvedNode, attachmentPoints, "head");

                var mapNode = resolvedNode["map"];
                if (mapNode != null)
                {
                    ExtractAttachmentPoints(mapNode, attachmentPoints, "head.map");
                }

                Debug.Log($"[NXAssetLoader] Loading head with shift: {headShift}");
                return SpriteLoader.LoadSpriteWithShift(resolvedNode, headShift, $"head/{skin}/{state}/{frame}");
            }

            var resolvedFrame = ResolveLinks(frameNode, charFile);
            return SpriteLoader.LoadSpriteWithShift(resolvedFrame, headShift, $"head/{skin}/{state}/{frame}");
        }
        
        /// <summary>
        /// Load face sprite
        /// </summary>
        public Sprite LoadFace(int faceId, string expression = "default", int frame = 0)
        {
            var charFile = GetNxFile("character");
            var faceNode = FaceBitmap(charFile, faceId, expression, frame, out int resolvedFaceId);
            return faceNode == null ? null : SpriteLoader.LoadSprite(faceNode, $"face/{resolvedFaceId}/{expression}/{frame}");
        }

        internal INxNode FaceRoot(INxFile file, int faceId, out int resolvedFaceId)
        {
            resolvedFaceId = faceId;
            var root = file?.GetNode($"Face/{faceId:D8}.img");
            if (root == null && faceId != 20000)
            {
                resolvedFaceId = 20000;
                root = file?.GetNode("Face/00020000.img");
            }
            return ResolveLinks(root, file);
        }

        internal INxNode FaceFrame(INxNode root, INxFile file, string expression, int frame)
        {
            if (root == null || frame < 0) return null;
            var exp = ResolveLinks(root[expression], file);
            return expression == "default" ? (frame == 0 ? exp : null) : ResolveLinks(exp?[frame.ToString()], file);
        }

        private INxNode FaceBitmap(INxFile file, int faceId, string expression, int frame, out int resolvedFaceId)
        {
            var root = FaceRoot(file, faceId, out resolvedFaceId);
            var node = FaceFrame(root, file, expression, frame);
            if (node == null) return null;
            var bitmap = node["face"];
            // Retain the older direct-bitmap/default frame-zero layout when present.
            if (bitmap == null && expression == "default") bitmap = node["0"]?["face"] ?? node["0"];
            return ResolveLinks(bitmap ?? node, file);
        }

        /// <summary>Face.h shifts each bitmap by its own brow; CharLook adds the current head brow.</summary>
        public Sprite LoadFaceWithShift(int faceId, string expression, Vector2 faceShift,
            out Dictionary<string, Vector2> attachmentPoints, int frame = 0)
        {
            attachmentPoints = new Dictionary<string, Vector2>();
            var faceNode = FaceBitmap(GetNxFile("character"), faceId, expression, frame, out int resolvedFaceId);
            if (faceNode == null) return null;
            ExtractAttachmentPoints(faceNode, attachmentPoints, "face");
            var mapNode = faceNode["map"];
            if (mapNode != null) ExtractAttachmentPoints(mapNode, attachmentPoints, "face.map");
            Vector2 faceBrow = mapNode?["brow"]?.GetValue<Vector2>() ?? Vector2.zero;
            return SpriteLoader.LoadSpriteWithShift(faceNode, faceShift - faceBrow, $"face/{resolvedFaceId}/{expression}/{frame}");
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
        /// Load hair sprite with shift applied (C++ style)
        /// C++ formula: hair_position = head.brow - head.neck + body.neck
        /// Each layer shift = hair_position - layer.map.brow
        /// </summary>
        public Sprite LoadHairWithShift(int hairId, string state, int frame, Vector2 hairShift,
            out Dictionary<string, Vector2> attachmentPoints, string layerName = "hair")
        {
            attachmentPoints = new Dictionary<string, Vector2>();
            var charFile = GetNxFile("character");
            if (charFile == null) return null;
            string basePath = $"Hair/{hairId:D8}.img/{state}/{frame}";
            var hairNode = charFile.GetNode(basePath) ?? charFile.GetNode($"Hair/{hairId:D8}.img/{state}");
            if (hairNode == null) return null;
            var partNode = hairNode[layerName];
            if (partNode == null)
            {
                if (layerName != "hair") return null;
                partNode = hairNode;
            }
            // ReNX may expose a container's first bitmap as Value; retain the
            // bitmap node itself so its authored origin is not lost.
            if (layerName == "hairShade") partNode = partNode["0"] ?? partNode;
            var resolvedNode = ResolveLinks(partNode, charFile);
            if (resolvedNode != null && !(resolvedNode.Value is byte[]))
                resolvedNode = ResolveLinks(resolvedNode["0"], charFile);
            if (resolvedNode == null) return null;
            ExtractAttachmentPoints(resolvedNode, attachmentPoints, layerName);
            var mapNode = resolvedNode["map"];
            if (mapNode != null) ExtractAttachmentPoints(mapNode, attachmentPoints, $"{layerName}.map");
            // HeavenClient Hair.cpp aligns each layer's brow with the head.
            Vector2 brow = mapNode?["brow"]?.GetValue<Vector2>() ?? Vector2.zero;
            return SpriteLoader.LoadSpriteWithShift(resolvedNode, hairShift - brow,
                $"hair/{hairId}/{state}/{frame}/{layerName}");
        }

        /// <summary>
        /// Load equipment sprite
        /// </summary>
        internal INxNode ResolveEquipmentNode(INxNode node, INxFile file) => ResolveLinks(node, file);

        public Sprite LoadEquipment(int itemId, string category, string state, int frame, Dictionary<string, Vector2> attachments = null)
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
                    
                    Vector2 shift = Vector2.zero;
                    if (attachments != null)
                    {
                        string anchorName = null;
                        Vector2 anchor = Vector2.zero;
                        var map = resolvedNode["map"];
                        if (map != null)
                            foreach (var point in map.Children)
                                if (point.Value is Vector2 position) { anchorName = point.Name; anchor = position; }
                        Vector2 body = attachments.TryGetValue("body.map.navel", out var b) ? b : Vector2.zero;
                        Vector2 bodyNeck = attachments.TryGetValue("body.map.neck", out var bn) ? bn : Vector2.zero;
                        Vector2 headNeck = attachments.TryGetValue("head.map.neck", out var hn) ? hn : Vector2.zero;
                        Vector2 headBrow = attachments.TryGetValue("head.map.brow", out var hb) ? hb : Vector2.zero;
                        Vector2 target = body;
                        if (category == "Cap" || category == "Earring" || category == "EyeAccessory" || category == "FaceAccessory")
                            target = bodyNeck - headNeck + headBrow;
                        else if (category == "Weapon" || category == "Shield")
                        {
                            if (anchorName == "handMove")
                                target = attachments.TryGetValue("lHand.map.handMove", out var hm) ? hm : Vector2.zero;
                            else if (anchorName == "hand")
                            {
                                Vector2 armHand = attachments.TryGetValue("arm.map.hand", out var ah) ? ah : Vector2.zero;
                                Vector2 armNavel = attachments.TryGetValue("arm.map.navel", out var an) ? an : Vector2.zero;
                                target = body + armHand - armNavel;
                            }
                        }
                        shift = target - anchor;
                    }
                    var sprite = SpriteLoader.LoadSpriteWithShift(resolvedNode, shift, $"{equipPath}/{partName}");
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
            string path = MapleClient.GameLogic.Data.ItemPaths.Node(itemId);
            if (path == null) return null;
            string file = MapleClient.GameLogic.Data.ItemPaths.File(itemId);
            var node = GetNxFile(file)?.GetNode(path + "/info/icon");
            return node != null ? SpriteLoader.LoadSprite(node, $"{file}/{path}/info/icon") : null;
        }
        public Sprite LoadMesoIcon(int amount, int frame)
        {
            int kind = amount > 999 ? 3 : amount > 99 ? 2 : amount > 49 ? 1 : 0;
            string path = $"Special/0900.img/0900000{kind}/iconRaw/{frame}";
            var node = GetNxFile("item")?.GetNode(path);
            return node == null ? null : SpriteLoader.LoadSpriteWithShift(node, Vector2.zero, "item/" + path);
        }
        public Sprite LoadDroppedItemIcon(int id)
        {
            string path = MapleClient.GameLogic.Data.ItemPaths.Node(id);
            if (path == null) return null;
            string file = MapleClient.GameLogic.Data.ItemPaths.File(id);
            var node = GetNxFile(file)?.GetNode(path + "/info/iconRaw");
            return node == null ? null : SpriteLoader.LoadSpriteWithShift(node, Vector2.zero, file + "/" + path + "/info/iconRaw");
        }
        public int MesoFrameAt(int amount, float seconds)
        {
            int kind = amount > 999 ? 3 : amount > 99 ? 2 : amount > 49 ? 1 : 0;
            var node = GetNxFile("item")?.GetNode($"Special/0900.img/0900000{kind}/iconRaw");
            if (node == null) return 0;
            int duration = 0, count = 0;
            while (count < 256 && node[count.ToString()] != null)
            { duration += Math.Max(1, node[count.ToString()]["delay"]?.GetValue<int>() ?? 100); count++; }
            if (duration == 0) return 0;
            int time = (int)(Math.Max(0, seconds) * 1000) % duration;
            for (int frame = 0; frame < count; frame++)
            { time -= Math.Max(1, node[frame.ToString()]["delay"]?.GetValue<int>() ?? 100); if (time < 0) return frame; }
            return 0;
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
        
        /// <summary>
        /// Extract attachment points from a node (neck, navel, hand, brow, etc.)
        /// </summary>
        private void ExtractAttachmentPoints(INxNode node, Dictionary<string, Vector2> attachmentPoints, string prefix)
        {
            if (node == null) return;
            
            // Common attachment point names in MapleStory
            string[] commonAttachmentNames = { "neck", "navel", "hand", "brow", "head", "ear" };
            
            foreach (var attachmentName in commonAttachmentNames)
            {
                var attachmentNode = node[attachmentName];
                if (attachmentNode != null && attachmentNode.Value is Vector2 attachmentPos)
                {
                    string key = string.IsNullOrEmpty(prefix) ? attachmentName : $"{prefix}.{attachmentName}";
                    attachmentPoints[key] = attachmentPos;
                    Debug.Log($"Found attachment point '{key}': {attachmentPos}");
                }
            }
            
            // Also check for any Vector2 children that might be attachment points
            foreach (var child in node.Children)
            {
                if (child.Value is Vector2 vec)
                {
                    string key = string.IsNullOrEmpty(prefix) ? child.Name : $"{prefix}.{child.Name}";
                    // Only add if not already found
                    if (!attachmentPoints.ContainsKey(key))
                    {
                        attachmentPoints[key] = vec;
                        Debug.Log($"Found potential attachment point '{key}': {vec}");
                    }
                }
            }
        }
    }
}
