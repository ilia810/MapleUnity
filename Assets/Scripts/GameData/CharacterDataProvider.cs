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
    public class CharacterDataProvider : ICharacterDataProvider
    {
        private readonly NXAssetLoader assetLoader;
        private readonly Dictionary<string, int> animationFrameCounts;
        
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
            return animationFrameCounts.TryGetValue(stateName, out int count) ? count : 1;
        }
        
        private string ConvertStateToAnimationName(CharacterState state)
        {
            // Based on C++ client Stance.cpp, animation names are different
            switch (state)
            {
                case CharacterState.Stand: return "stand1"; // C++ uses stand1/stand2
                case CharacterState.Walk: return "walk1"; // C++ uses walk1/walk2
                case CharacterState.Jump: return "jump";
                case CharacterState.Fall: return "jump"; // Fall uses jump animation
                case CharacterState.Alert: return "alert";
                case CharacterState.Prone: return "prone";
                case CharacterState.Fly: return "fly";
                case CharacterState.Ladder: return "ladder";
                case CharacterState.Rope: return "rope";
                case CharacterState.Attack1: return "stabO1"; // Stab one-hand
                case CharacterState.Attack2: return "swingO1"; // Swing one-hand
                case CharacterState.Skill: return "skill";
                default: return "stand1";
            }
        }
        
        private string ConvertExpressionToName(CharacterExpression expression)
        {
            switch (expression)
            {
                case CharacterExpression.Default: return "default";
                case CharacterExpression.Blink: return "blink";
                case CharacterExpression.Hit: return "hit";
                case CharacterExpression.Smile: return "smile";
                case CharacterExpression.Troubled: return "troubled";
                case CharacterExpression.Cry: return "cry";
                case CharacterExpression.Angry: return "angry";
                case CharacterExpression.Bewildered: return "bewildered";
                case CharacterExpression.Stunned: return "stunned";
                case CharacterExpression.Vomit: return "vomit";
                case CharacterExpression.Oops: return "oops";
                default: return "default";
            }
        }
        
        private string GetEquipmentCategory(int itemId)
        {
            // MapleStory equipment categories based on item ID ranges
            int subtype = (itemId / 1000) % 100;
            
            switch (subtype)
            {
                case 0: return "Cap"; // Hats
                case 1: return "FaceAccessory"; // Face accessories
                case 2: return "EyeAccessory"; // Eye accessories
                case 3: return "Earring"; // Earrings
                case 4: return "Coat"; // Top/Overall
                case 5: return "Longcoat"; // Overall
                case 6: return "Pants"; // Bottom
                case 7: return "Shoes"; // Shoes
                case 8: return "Glove"; // Gloves
                case 9: return "Shield"; // Shields
                case 10: return "Cape"; // Capes
                case 11: return "Ring"; // Rings
                case 12: return "Pendant"; // Pendants
                case 13: return "Belt"; // Belts
                case 14: return "Medal"; // Medals
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                case 49: return "Weapon"; // Various weapon types
                default: return "Etc";
            }
        }
        
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
            
            // Create SpriteData - ImageData can be null for Unity-only rendering
            return new SpriteData
            {
                Width = (int)sprite.rect.width,
                Height = (int)sprite.rect.height,
                OriginX = (int)(sprite.pivot.x * sprite.rect.width),
                OriginY = (int)(sprite.pivot.y * sprite.rect.height),
                Name = sprite.name,
                ImageData = null // We'll use Unity sprites directly, no need to encode
            };
        }
    }
}