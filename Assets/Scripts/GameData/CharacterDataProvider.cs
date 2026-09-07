using System;
using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameData
{
    /// <summary>
    /// Provides character sprite data from NX files
    /// </summary>
    public class CharacterDataProvider : ICharacterDataProvider, IStanceDataProvider, IFaceDataProvider
    {
        private readonly NXAssetLoader assetLoader;
        private readonly Dictionary<string, int> animationFrameCounts;
        private readonly Dictionary<string, int> sourceFrameCounts = new Dictionary<string, int>();
        private INxFile frameCountSource;
        private readonly Dictionary<int, FaceAnimationData> faceAnimations = new Dictionary<int, FaceAnimationData>();
        private readonly Dictionary<string, IReadOnlyList<int>> stanceDelays = new Dictionary<string, IReadOnlyList<int>>();
        
        public CharacterDataProvider()
        {
            assetLoader = NXAssetLoader.Instance;
            
            // MapleStory v83 animation frame counts (using C++ client names)
            animationFrameCounts = new Dictionary<string, int>
            {
                { "stand1", 3 },   // Standing animation has 3 frames
                { "stand2", 3 },   // Alternative standing animation
                { "walk1", 4 },    // Walking animation has 4 frames
                { "walk2", 4 },    // Alternative walking animation
                { "jump", 1 },     // Jumping is a single frame
                { "alert", 3 },    // Alert stance has 3 frames
                { "prone", 1 },    // Prone/lying down is single frame
                { "proneStab", 1 },// Prone stabbing animation
                { "fly", 2 },      // Flying has 2 frames
                { "ladder", 2 },   // Ladder climbing has 2 frames
                { "rope", 2 },     // Rope climbing has 2 frames
                { "stabO1", 3 },   // Stab one-hand animation
                { "stabO2", 3 },   // Stab one-hand animation 2
                { "swingO1", 3 },  // Swing one-hand animation
                { "swingO2", 3 },  // Swing one-hand animation 2
                { "swingO3", 3 },  // Swing one-hand animation 3
                { "shot", 3 },     // Shooting animation
                { "sit", 1 },      // Sitting animation
                { "heal", 3 }      // Healing animation
            };
        }
        
        public SpriteData GetBodySprite(int skin, CharacterState state, int frame)
        {
            string stateName = ConvertStateToAnimationName(state);
            var sprite = assetLoader.LoadCharacterBody(skin, stateName, frame);
            return ConvertToSpriteData(sprite);
        }
        
        public SpriteData GetHeadSprite(int skin, CharacterState state, int frame)
        {
            string stateName = ConvertStateToAnimationName(state);
            var sprite = assetLoader.LoadCharacterHead(skin, stateName, frame);
            return ConvertToSpriteData(sprite);
        }
        
        public SpriteData GetHairSprite(int hairId, CharacterState state, int frame)
        {
            string stateName = ConvertStateToAnimationName(state);
            var sprite = assetLoader.LoadHair(hairId, stateName, frame);
            return ConvertToSpriteData(sprite);
        }
        
        public SpriteData GetFaceSprite(int faceId, CharacterExpression expression)
        {
            string expressionName = ConvertExpressionToName(expression);
            var sprite = assetLoader.LoadFace(faceId, expressionName);
            return ConvertToSpriteData(sprite);
        }
        
        public EquipSprite GetEquipSprite(int itemId, CharacterState state, int frame)
        {
            string stateName = ConvertStateToAnimationName(state);
            string category = GetEquipmentCategory(itemId);
            
            var sprite = assetLoader.LoadEquipment(itemId, category, stateName, frame);
            if (sprite == null) return null;
            
            // Create EquipSprite with proper layer information
            return new EquipSprite
            {
                Sprite = ConvertToSpriteData(sprite),
                Z = GetEquipmentZIndex(category)
            };
        }
        
        public byte[] GetHairSpriteData(int hairId, CharacterState state, int frame)
        {
            // For network synchronization - return raw sprite data
            var spriteData = GetHairSprite(hairId, state, frame);
            return spriteData?.ImageData;
        }
        
        public byte[] GetFaceSpriteData(int faceId, CharacterExpression expression)
        {
            // For network synchronization - return raw sprite data
            var spriteData = GetFaceSprite(faceId, expression);
            return spriteData?.ImageData;
        }
        
        public int GetAnimationFrameCount(CharacterState state)
        {
            string stateName = ConvertStateToAnimationName(state);
            var source = assetLoader.GetNxFile("character");
            if (!ReferenceEquals(source, frameCountSource))
            {
                frameCountSource = source;
                sourceFrameCounts.Clear();
                stanceDelays.Clear();
                faceAnimations.Clear();
            }
            if (source != null)
            {
                if (sourceFrameCounts.TryGetValue(stateName, out int authoredCount)) return authoredCount;
                var node = source.GetNode("00002000.img/" + stateName);
                int count = 0;
                while (node?[count.ToString()] != null) count++;
                if (count > 0) { sourceFrameCounts[stateName] = count; return count; }
            }
            return animationFrameCounts.TryGetValue(stateName, out int fallbackCount) ? fallbackCount : 1;
        }
        
        public IReadOnlyList<int> GetStanceDelays(CharacterState state)
        {
            int count = GetAnimationFrameCount(state); // Also invalidates caches after source replacement.
            string name = ConvertStateToAnimationName(state);
            if (stanceDelays.TryGetValue(name, out var cached)) return cached;
            var frames = new int[count];
            var source = assetLoader.GetNxFile("character");
            for (int frame = 0; frame < count; frame++)
            {
                // BodyDrawInfo imports a signed 16-bit delay, defaulting nonpositive entries to 100.
                short delay = unchecked((short)(source?.GetNode($"00002000.img/{name}/{frame}/delay")?.GetValue<int>() ?? 100));
                frames[frame] = delay > 0 ? delay : 100;
            }
            var result = Array.AsReadOnly(frames);
            if (source != null) stanceDelays[name] = result;
            return result;
        }

        private string ConvertStateToAnimationName(CharacterState state) => MapleClient.GameLogic.Data.CharacterStances.Name(state);

        public FaceAnimationData GetFaceAnimation(int faceId)
        {
            GetAnimationFrameCount(CharacterState.Stand); // Invalidate all metadata after replacing the NX source.
            if (faceAnimations.TryGetValue(faceId, out var cached)) return cached;
            var file = assetLoader.GetNxFile("character");
            var root = assetLoader.FaceRoot(file, faceId, out int resolvedFaceId);
            if (root == null) return null;
            var expressions = new Dictionary<CharacterExpression, int[]>();
            foreach (var expression in CharacterExpressions.SourceOrder)
            {
                var delays = new List<int>();
                string name = CharacterExpressions.Name(expression);
                for (int frame = 0; frame < 256; frame++)
                {
                    var node = assetLoader.FaceFrame(root, file, name, frame);
                    if (node == null && expression != CharacterExpression.Default) break;
                    ushort delay = unchecked((ushort)(node?["delay"]?.GetValue<int>() ?? 0));
                    delays.Add(delay == 0 ? 2500 : delay);
                    if (expression == CharacterExpression.Default) break;
                }
                expressions[expression] = delays.ToArray();
            }
            var result = new FaceAnimationData(resolvedFaceId, expressions);
            faceAnimations[faceId] = result;
            return result;
        }

        private string ConvertExpressionToName(CharacterExpression expression) => CharacterExpressions.Name(expression);

        private string GetEquipmentCategory(int itemId) => MapleClient.GameLogic.Data.ItemPaths.EquipmentCategory(itemId);

        private int GetEquipmentZIndex(string category)
        {
            // Layer ordering for equipment sprites
            switch (category)
            {
                case "Shield": return -2; // Behind body
                case "Cape": return -1; // Behind body
                case "Weapon": return 9; // Above most equipment
                case "Glove": return 7;
                case "Shoes": return 6;
                case "Pants": return 4;
                case "Coat":
                case "Longcoat": return 5;
                case "Cap": return 8;
                case "FaceAccessory": return 2;
                case "EyeAccessory": return 3;
                case "Earring": return 1;
                default: return 0;
            }
        }
        
        private SpriteData ConvertToSpriteData(UnityEngine.Sprite sprite)
        {
            if (sprite == null) return null;
            
            var texture = sprite.texture;
            if (texture == null) return null;
            
            // Store the Unity sprite reference in a special SpriteData subclass
            return new UnitySpriteData
            {
                Width = (int)sprite.rect.width,
                Height = (int)sprite.rect.height,
                OriginX = (int)(sprite.pivot.x * sprite.rect.width),
                OriginY = (int)(sprite.pivot.y * sprite.rect.height),
                Name = sprite.name,
                ImageData = null, // We'll use Unity sprites directly, no need to encode
                UnitySprite = sprite // Store the actual Unity sprite reference
            };
        }
    }
    
    /// <summary>
    /// Unity-specific SpriteData that holds a reference to the actual Unity sprite
    /// </summary>
    public class UnitySpriteData : SpriteData
    {
        public UnityEngine.Sprite UnitySprite { get; set; }
    }
}
