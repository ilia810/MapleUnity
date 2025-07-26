using System;
using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameView
{
    /// <summary>
    /// Renders MapleStory characters with proper layered sprites
    /// </summary>
    public class MapleCharacterRenderer : MonoBehaviour
    {
        private Player player;
        private ICharacterDataProvider characterData;
        
        // Sprite layers in correct rendering order
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer headRenderer;
        private SpriteRenderer hairRenderer;
        private SpriteRenderer faceRenderer;
        private SpriteRenderer hatRenderer;
        private SpriteRenderer topRenderer;
        private SpriteRenderer bottomRenderer;
        private SpriteRenderer shoesRenderer;
        private SpriteRenderer weaponRenderer;
        private SpriteRenderer capeRenderer;
        private SpriteRenderer gloveRenderer;
        private SpriteRenderer shieldRenderer;
        
        // Animation state
        private CharacterState currentState = CharacterState.Stand;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private const float FRAME_DURATION = 0.1f; // 100ms per frame
        
        // Character appearance
        private int skinColor = 0;
        private int faceId = 20000; // Default face
        private int hairId = 30000; // Default hair
        
        public void Initialize(Player player, ICharacterDataProvider characterData)
        {
            this.player = player;
            this.characterData = characterData;
            
            CreateSpriteRenderers();
            UpdateAppearance();
            
            // Test loading a sprite and explore NX structure
            Debug.Log("Testing character sprite loading...");
            ExploreNxStructure();
            
            var testBody = characterData.GetBodySprite(0, CharacterState.Stand, 0);
            if (testBody != null)
            {
                Debug.Log($"Successfully loaded body sprite: {testBody.Width}x{testBody.Height}");
            }
            else
            {
                Debug.LogWarning("Failed to load test body sprite");
            }
        }
        
        private void CreateSpriteRenderers()
        {
            // Create sprite renderers in correct layer order
            bodyRenderer = CreateSpriteLayer("Body", 0);
            headRenderer = CreateSpriteLayer("Head", 1);
            
            // Back layers
            capeRenderer = CreateSpriteLayer("Cape", -1);
            
            // Face and hair
            faceRenderer = CreateSpriteLayer("Face", 2);
            hairRenderer = CreateSpriteLayer("Hair", 3);
            
            // Equipment layers
            bottomRenderer = CreateSpriteLayer("Bottom", 4);
            topRenderer = CreateSpriteLayer("Top", 5);
            shoesRenderer = CreateSpriteLayer("Shoes", 6);
            gloveRenderer = CreateSpriteLayer("Glove", 7);
            hatRenderer = CreateSpriteLayer("Hat", 8);
            
            // Weapon/shield
            weaponRenderer = CreateSpriteLayer("Weapon", 9);
            shieldRenderer = CreateSpriteLayer("Shield", -2); // Behind body
        }
        
        private SpriteRenderer CreateSpriteLayer(string layerName, int sortingOrder)
        {
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.parent = transform;
            layerObj.transform.localPosition = Vector3.zero;
            
            SpriteRenderer renderer = layerObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sortingOrder;
            
            return renderer;
        }
        
        void Update()
        {
            if (player == null) return;
            
            // Update position
            transform.position = new Vector3(player.Position.X / 100f, player.Position.Y / 100f, 0);
            
            // Update animation state
            CharacterState newState = GetCharacterState();
            if (newState != currentState)
            {
                currentState = newState;
                currentFrame = 0;
                animationTimer = 0f;
            }
            
            // Update animation frame
            animationTimer += Time.deltaTime;
            if (animationTimer >= FRAME_DURATION)
            {
                animationTimer -= FRAME_DURATION;
                currentFrame++;
                
                int frameCount = characterData.GetAnimationFrameCount(currentState);
                if (currentFrame >= frameCount)
                {
                    currentFrame = 0;
                }
                
                UpdateSprites();
            }
            
            // Flip sprites based on direction
            bool flipX = player.Velocity.X < 0;
            SetFlipX(flipX);
        }
        
        private CharacterState GetCharacterState()
        {
            switch (player.State)
            {
                case PlayerState.Walking:
                    return CharacterState.Walk;
                case PlayerState.Jumping:
                    return CharacterState.Jump;
                case PlayerState.Climbing:
                    return CharacterState.Ladder;
                case PlayerState.Crouching:
                    return CharacterState.Prone; // Or sit, depending on context
                default:
                    return CharacterState.Stand;
            }
        }
        
        private void UpdateSprites()
        {
            // Update body parts
            UpdateBodySprite();
            UpdateHeadSprite();
            UpdateFaceSprite();
            UpdateHairSprite();
            
            // Update equipment
            UpdateEquipmentSprites();
        }
        
        private void UpdateBodySprite()
        {
            var bodySpriteData = characterData.GetBodySprite(skinColor, currentState, currentFrame);
            if (bodySpriteData != null)
            {
                bodyRenderer.sprite = ConvertToUnitySprite(bodySpriteData);
            }
            else if (bodyRenderer.sprite == null)
            {
                // Create a simple colored sprite as fallback
                bodyRenderer.sprite = CreateColoredSprite(Color.blue, 32, 48, "Body");
            }
        }
        
        private void UpdateHeadSprite()
        {
            var headSpriteData = characterData.GetHeadSprite(skinColor, currentState, currentFrame);
            if (headSpriteData != null)
            {
                headRenderer.sprite = ConvertToUnitySprite(headSpriteData);
            }
        }
        
        private void UpdateFaceSprite()
        {
            // Face expressions could change based on context
            CharacterExpression expression = CharacterExpression.Default;
            
            var faceSpriteData = characterData.GetFaceSprite(faceId, expression);
            if (faceSpriteData != null)
            {
                faceRenderer.sprite = ConvertToUnitySprite(faceSpriteData);
            }
        }
        
        private void UpdateHairSprite()
        {
            var hairSpriteData = characterData.GetHairSprite(hairId, currentState, currentFrame);
            if (hairSpriteData != null)
            {
                hairRenderer.sprite = ConvertToUnitySprite(hairSpriteData);
            }
        }
        
        private void UpdateEquipmentSprites()
        {
            // Get equipped items from player
            var equippedItems = player.GetEquippedItems();
            
            foreach (var kvp in equippedItems)
            {
                var slot = kvp.Key;
                var itemId = kvp.Value;
                
                if (itemId <= 0) continue;
                
                var equipSprite = characterData.GetEquipSprite(itemId, currentState, currentFrame);
                if (equipSprite == null || equipSprite.Sprite == null) continue;
                
                var unitySprite = ConvertToUnitySprite(equipSprite.Sprite);
                if (unitySprite == null) continue;
                
                // Assign to appropriate renderer based on equipment slot
                switch (slot)
                {
                    case EquipSlot.Hat:
                        hatRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Top:
                        topRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Bottom:
                        bottomRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Shoes:
                        shoesRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Glove:
                        gloveRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Cape:
                        capeRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Weapon:
                        weaponRenderer.sprite = unitySprite;
                        break;
                    case EquipSlot.Shield:
                        shieldRenderer.sprite = unitySprite;
                        break;
                }
            }
        }
        
        private void SetFlipX(bool flip)
        {
            // Flip all sprite layers
            bodyRenderer.flipX = flip;
            headRenderer.flipX = flip;
            faceRenderer.flipX = flip;
            hairRenderer.flipX = flip;
            hatRenderer.flipX = flip;
            topRenderer.flipX = flip;
            bottomRenderer.flipX = flip;
            shoesRenderer.flipX = flip;
            gloveRenderer.flipX = flip;
            weaponRenderer.flipX = flip;
            capeRenderer.flipX = flip;
            shieldRenderer.flipX = flip;
        }
        
        public void UpdateAppearance()
        {
            // Called when equipment changes or character customization changes
            UpdateSprites();
        }
        
        public void SetCharacterAppearance(int skin, int face, int hair)
        {
            skinColor = skin;
            faceId = face;
            hairId = hair;
            UpdateAppearance();
        }
        
        private Sprite ConvertToUnitySprite(SpriteData spriteData)
        {
            if (spriteData == null || spriteData.ImageData == null) return null;
            
            // Create texture from byte array
            Texture2D texture = new Texture2D(spriteData.Width, spriteData.Height, TextureFormat.ARGB32, false);
            texture.LoadImage(spriteData.ImageData);
            texture.filterMode = FilterMode.Point; // Pixel perfect for MapleStory sprites
            
            // Create sprite from texture
            Vector2 pivot = new Vector2(
                spriteData.OriginX / (float)spriteData.Width,
                spriteData.OriginY / (float)spriteData.Height
            );
            
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                pivot,
                100f // Pixels per unit
            );
        }
        
        private Sprite CreateColoredSprite(Color color, int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), // Center pivot
                100f // Pixels per unit
            );
            sprite.name = name;
            return sprite;
        }
        
        private void ExploreNxStructure()
        {
            Debug.Log("=== Exploring NX File Structure ===");
            
            var loader = MapleClient.GameData.NXAssetLoader.Instance;
            var charFile = loader.GetNxFile("character");
            if (charFile != null && charFile.Root != null)
            {
                Debug.Log("Character NX file root children:");
                int count = 0;
                foreach (var child in charFile.Root.Children)
                {
                    Debug.Log($"  - {child.Name}");
                    if (count++ > 10) 
                    {
                        Debug.Log("  ... (more children)");
                        break;
                    }
                }
                
                // Look for body sprites specifically
                var bodyNode = charFile.GetNode("00002000.img");
                if (bodyNode != null)
                {
                    Debug.Log("Found 00002000.img (body sprites), children:");
                    count = 0;
                    foreach (var child in bodyNode.Children)
                    {
                        Debug.Log($"  - {child.Name}");
                        if (count++ > 5) 
                        {
                            Debug.Log("  ... (more children)");
                            break;
                        }
                    }
                    
                    // Try to explore stand animation - looks like animations are directly under bodyNode
                    var standNode = bodyNode["stand"];
                    if (standNode != null)
                    {
                        Debug.Log("Found stand animation directly under 00002000.img, children:");
                        count = 0;
                        foreach (var child in standNode.Children)
                        {
                            Debug.Log($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                            if (count++ > 5)
                            {
                                Debug.Log("  ... (more children)");
                                break;
                            }
                        }
                        
                        // Look at frame 0
                        var frame0 = standNode["0"];
                        if (frame0 != null)
                        {
                            Debug.Log("Found frame 0 of stand, children:");
                            foreach (var child in frame0.Children)
                            {
                                Debug.Log($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                            }
                            
                            // Look for body parts
                            var bodyPart = frame0["body"];
                            if (bodyPart != null)
                            {
                                Debug.Log("Found body part, exploring:");
                                Debug.Log($"  Value type: {bodyPart.Value?.GetType().Name ?? "null"}");
                                foreach (var child in bodyPart.Children)
                                {
                                    Debug.Log($"  - {child.Name} (value type: {child.Value?.GetType().Name ?? "null"})");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No stand node found directly under 00002000.img");
                    }
                }
            }
            else
            {
                Debug.LogError("Character NX file not loaded!");
            }
        }
    }
    
    // EquipSlot enum is now in MapleClient.GameLogic.Data
}