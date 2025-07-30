using System;
using System.Collections.Generic;
using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;
using MapleClient.GameData;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Renders MapleStory characters with proper layered sprites
    /// </summary>
    public class MapleCharacterRenderer : MonoBehaviour, IPlayerViewListener
    {
        private Player player;
        private ICharacterDataProvider characterData;
        
        // Sprite layers in correct rendering order
        private SpriteRenderer backBodyRenderer; // Behind body
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer armRenderer;      // Body's arm part
        private SpriteRenderer armOverHairRenderer; // Arm that goes over hair
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
        private SpriteRenderer handRenderer;     // Hand part (over gloves)
        
        // Animation state
        private CharacterState currentState = CharacterState.Stand;
        private int currentFrame = 0;
        private float animationTimer = 0f;
        private const float FRAME_DURATION = 0.1f; // 100ms per frame
        
        // Head attachment point for current frame
        private Vector2? currentHeadAttachPoint = null;
        
        // Character appearance
        private int skinColor = 0;
        private int faceId = 20000; // Default face
        private int hairId = 30000; // Default hair
        
        public void Initialize(Player player, ICharacterDataProvider characterData)
        {
            this.player = player;
            this.characterData = characterData;
            
            // Register as a listener to player events
            if (player != null)
            {
                player.AddViewListener(this);
            }
            
            CreateSpriteRenderers();
            
            // Initialize sprites immediately
            UpdateSprites();
            
            // Debug sprite positioning
            Debug.Log($"MapleCharacterRenderer initialized at: {transform.position}");
            Debug.Log($"  Parent (PlayerView) position: {transform.parent?.position ?? Vector3.zero}");
            Debug.Log($"  Local position: {transform.localPosition}");
            Debug.Log($"  Body renderer position: {bodyRenderer.transform.position}, local: {bodyRenderer.transform.localPosition}");
            
            // Test loading a sprite and explore NX structure
            Debug.Log("Testing character sprite loading...");
            ExploreNxStructure();
            
            // Test direct sprite loading
            var testBody = NXAssetLoader.Instance.LoadCharacterBody(0, "stand1", 0);
            if (testBody != null)
            {
                Debug.Log($"Successfully loaded body sprite: {testBody.name} ({testBody.rect.width}x{testBody.rect.height})");
                Debug.Log($"  Sprite pivot: {testBody.pivot}, pixels per unit: {testBody.pixelsPerUnit}");
            }
            else
            {
                Debug.LogWarning("Failed to load test body sprite");
            }
        }
        
        private void CreateSpriteRenderers()
        {
            // Create sprite renderers in correct layer order (based on C++ client layers_by_name)
            // Back layers (negative sorting order)
            shieldRenderer = CreateSpriteLayer("Shield", -3);
            capeRenderer = CreateSpriteLayer("Cape", -2);
            backBodyRenderer = CreateSpriteLayer("BackBody", -1);
            
            // Main body parts
            bodyRenderer = CreateSpriteLayer("Body", 0);
            armRenderer = CreateSpriteLayer("Arm", 1);
            
            // Equipment on body
            bottomRenderer = CreateSpriteLayer("Bottom", 2);
            topRenderer = CreateSpriteLayer("Top", 3);
            shoesRenderer = CreateSpriteLayer("Shoes", 4);
            
            // Head and face
            headRenderer = CreateSpriteLayer("Head", 5);
            faceRenderer = CreateSpriteLayer("Face", 6);
            
            // Hair layers
            hairRenderer = CreateSpriteLayer("Hair", 7);
            
            // Over-hair layers
            armOverHairRenderer = CreateSpriteLayer("ArmOverHair", 8);
            gloveRenderer = CreateSpriteLayer("Glove", 9);
            handRenderer = CreateSpriteLayer("Hand", 10);
            hatRenderer = CreateSpriteLayer("Hat", 11);
            
            // Weapon on top
            weaponRenderer = CreateSpriteLayer("Weapon", 12);
        }
        
        private SpriteRenderer CreateSpriteLayer(string layerName, int sortingOrder)
        {
            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(transform, false); // Use SetParent with worldPositionStays = false
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localScale = Vector3.one;
            layerObj.transform.localRotation = Quaternion.identity;
            
            SpriteRenderer renderer = layerObj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sortingOrder;
            
            // Enable the renderer
            renderer.enabled = true;
            
            Debug.Log($"Created sprite layer: {layerName} with sorting order {sortingOrder} on layer 'Player' at local position {layerObj.transform.localPosition}");
            
            return renderer;
        }
        
        void Update()
        {
            if (player == null) return;
            
            // DON'T update position here - PlayerView handles that
            // The character renderer is a child of PlayerView GameObject
            
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
            
            // Flip sprites based on direction - fixed logic
            // In MapleStory, positive X velocity means facing right (no flip)
            // Negative X velocity means facing left (flip)
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
            // Clear all body part sprites first
            bodyRenderer.sprite = null;
            armRenderer.sprite = null;
            backBodyRenderer.sprite = null;
            armOverHairRenderer.sprite = null;
            handRenderer.sprite = null;
            
            // Load all body parts from NXAssetLoader
            string stateName = ConvertStateToAnimationName(currentState);
            Vector2? headAttachPoint;
            var bodyParts = NXAssetLoader.Instance.LoadCharacterBodyParts(skinColor, stateName, currentFrame, out headAttachPoint);
            
            if (bodyParts != null && bodyParts.Count > 0)
            {
                Debug.Log($"Loaded {bodyParts.Count} body parts for state:{stateName} frame:{currentFrame}");
                
                // Assign sprites to appropriate renderers
                foreach (var part in bodyParts)
                {
                    switch (part.Key.ToLower())
                    {
                        case "body":
                            bodyRenderer.sprite = part.Value;
                            Debug.Log($"  - Body: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        case "arm":
                            armRenderer.sprite = part.Value;
                            Debug.Log($"  - Arm: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        case "backbody":
                        case "backBody":
                            backBodyRenderer.sprite = part.Value;
                            Debug.Log($"  - BackBody: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        case "armoverhair":
                        case "armOverHair":
                            armOverHairRenderer.sprite = part.Value;
                            Debug.Log($"  - ArmOverHair: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        case "hand":
                        case "lHand":
                        case "rHand":
                            handRenderer.sprite = part.Value;
                            Debug.Log($"  - Hand: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        default:
                            Debug.Log($"  - Unknown part '{part.Key}': {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                    }
                }
                
                // Store head attachment point
                currentHeadAttachPoint = headAttachPoint;
                if (headAttachPoint.HasValue)
                {
                    Debug.Log($"Head attachment point: {headAttachPoint.Value}");
                    // Convert MapleStory coordinates to Unity coordinates
                    // Head attachment is relative to the character origin, so we just scale it
                    Vector3 headPos = new Vector3(
                        headAttachPoint.Value.x / 100f, 
                        -headAttachPoint.Value.y / 100f, // Invert Y for Unity
                        0
                    );
                    // Update head/face/hair positions
                    UpdateHeadPosition(headPos);
                }
            }
            else
            {
                // Fallback to single body sprite through character data provider
                var bodySpriteData = characterData.GetBodySprite(skinColor, currentState, currentFrame);
                
                if (bodySpriteData != null && bodySpriteData is UnitySpriteData unityData && unityData.UnitySprite != null)
                {
                    bodyRenderer.sprite = unityData.UnitySprite;
                    Debug.Log($"Body sprite loaded (single): {unityData.UnitySprite.name}");
                }
                else
                {
                    // Create a simple colored sprite as fallback
                    Debug.LogWarning($"No body sprites found for skin:{skinColor} state:{stateName} frame:{currentFrame}");
                    bodyRenderer.sprite = CreateColoredSprite(Color.blue, 32, 48, "Body");
                }
            }
        }
        
        private void UpdateHeadSprite()
        {
            // Load head sprite directly from asset loader
            string stateName = ConvertStateToAnimationName(currentState);
            var headSprite = NXAssetLoader.Instance.LoadCharacterHead(skinColor, stateName, currentFrame);
            
            if (headSprite != null)
            {
                headRenderer.sprite = headSprite;
            }
        }
        
        private void UpdateFaceSprite()
        {
            // Face expressions could change based on context
            CharacterExpression expression = CharacterExpression.Default;
            string expressionName = ConvertExpressionToName(expression);
            
            var faceSprite = NXAssetLoader.Instance.LoadFace(faceId, expressionName);
            if (faceSprite != null)
            {
                faceRenderer.sprite = faceSprite;
            }
        }
        
        private void UpdateHairSprite()
        {
            // Load hair sprite directly from asset loader
            string stateName = ConvertStateToAnimationName(currentState);
            var hairSprite = NXAssetLoader.Instance.LoadHair(hairId, stateName, currentFrame);
            
            if (hairSprite != null)
            {
                hairRenderer.sprite = hairSprite;
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
                
                // Load equipment sprites directly from asset loader
                string stateName = ConvertStateToAnimationName(currentState);
                string category = GetEquipmentCategory(itemId);
                var equipSprite = NXAssetLoader.Instance.LoadEquipment(itemId, category, stateName, currentFrame);
                
                if (equipSprite == null) continue;
                
                // Assign to appropriate renderer based on equipment slot
                switch (slot)
                {
                    case EquipSlot.Hat:
                        hatRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Top:
                        topRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Bottom:
                        bottomRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Shoes:
                        shoesRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Glove:
                        gloveRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Cape:
                        capeRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Weapon:
                        weaponRenderer.sprite = equipSprite;
                        break;
                    case EquipSlot.Shield:
                        shieldRenderer.sprite = equipSprite;
                        break;
                }
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
        
        private void SetFlipX(bool flip)
        {
            // Flip all sprite layers
            if (backBodyRenderer != null) backBodyRenderer.flipX = flip;
            if (bodyRenderer != null) bodyRenderer.flipX = flip;
            if (armRenderer != null) armRenderer.flipX = flip;
            if (armOverHairRenderer != null) armOverHairRenderer.flipX = flip;
            if (headRenderer != null) headRenderer.flipX = flip;
            if (faceRenderer != null) faceRenderer.flipX = flip;
            if (hairRenderer != null) hairRenderer.flipX = flip;
            if (hatRenderer != null) hatRenderer.flipX = flip;
            if (topRenderer != null) topRenderer.flipX = flip;
            if (bottomRenderer != null) bottomRenderer.flipX = flip;
            if (shoesRenderer != null) shoesRenderer.flipX = flip;
            if (gloveRenderer != null) gloveRenderer.flipX = flip;
            if (handRenderer != null) handRenderer.flipX = flip;
            if (weaponRenderer != null) weaponRenderer.flipX = flip;
            if (capeRenderer != null) capeRenderer.flipX = flip;
            if (shieldRenderer != null) shieldRenderer.flipX = flip;
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
        
        private void UpdateHeadPosition(Vector3 headAttachPoint)
        {
            // Position head-related layers relative to the attachment point
            // The attachment point is relative to the character's origin (usually feet)
            // We need to position these as children maintain their own local positions
            
            if (headRenderer != null && headRenderer.gameObject != null)
            {
                headRenderer.transform.localPosition = headAttachPoint;
            }
            
            if (faceRenderer != null && faceRenderer.gameObject != null)
            {
                // Face is positioned relative to head
                faceRenderer.transform.localPosition = headAttachPoint;
            }
            
            if (hairRenderer != null && hairRenderer.gameObject != null)
            {
                // Hair is positioned relative to head
                hairRenderer.transform.localPosition = headAttachPoint;
            }
            
            if (hatRenderer != null && hatRenderer.gameObject != null)
            {
                // Hat is positioned relative to head
                hatRenderer.transform.localPosition = headAttachPoint;
            }
            
            Debug.Log($"Updated head position to: {headAttachPoint}");
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
        
        #region IPlayerViewListener Implementation
        
        public void OnPositionChanged(Vector2 position)
        {
            // Position is handled by parent PlayerView
        }
        
        public void OnStateChanged(PlayerState state)
        {
            // Map PlayerState to CharacterState for animations
            Debug.Log($"[MapleCharacterRenderer] State changed to: {state}");
            
            CharacterState oldState = currentState;
            CharacterState newState = GetCharacterState();
            
            if (oldState != newState)
            {
                currentState = newState;
                currentFrame = 0;
                animationTimer = 0f;
                UpdateSprites();
            }
        }
        
        public void OnVelocityChanged(Vector2 velocity)
        {
            // Use velocity for facing direction
            if (velocity.X != 0)
            {
                bool shouldFlip = velocity.X < 0;
                SetFlipX(shouldFlip);
            }
        }
        
        public void OnGroundedStateChanged(bool isGrounded)
        {
            // Can use this for landing detection
            if (isGrounded && currentState == CharacterState.Jump)
            {
                Debug.Log("[MapleCharacterRenderer] Landed!");
                // Landing will be handled by state change or animation event
            }
        }
        
        public void OnAnimationEvent(PlayerAnimationEvent animEvent)
        {
            Debug.Log($"[MapleCharacterRenderer] Animation event: {animEvent}");
            
            switch (animEvent)
            {
                case PlayerAnimationEvent.Jump:
                    // Immediately switch to jump animation
                    currentState = CharacterState.Jump;
                    currentFrame = 0;
                    animationTimer = 0f;
                    UpdateSprites();
                    break;
                    
                case PlayerAnimationEvent.Land:
                    // Return to standing after landing
                    currentState = CharacterState.Stand;
                    currentFrame = 0;
                    animationTimer = 0f;
                    UpdateSprites();
                    // TODO: Add landing effect
                    break;
                    
                case PlayerAnimationEvent.StartWalk:
                    // Start walk cycle
                    currentState = CharacterState.Walk;
                    currentFrame = 0;
                    animationTimer = 0f;
                    break;
                    
                case PlayerAnimationEvent.StopWalk:
                    // Return to standing
                    currentState = CharacterState.Stand;
                    currentFrame = 0;
                    animationTimer = 0f;
                    break;
                    
                case PlayerAnimationEvent.Attack:
                    // TODO: Implement attack animations
                    break;
                    
                case PlayerAnimationEvent.StartClimb:
                    currentState = CharacterState.Ladder;
                    currentFrame = 0;
                    animationTimer = 0f;
                    break;
                    
                case PlayerAnimationEvent.StopClimb:
                    currentState = CharacterState.Stand;
                    currentFrame = 0;
                    animationTimer = 0f;
                    break;
                    
                case PlayerAnimationEvent.Crouch:
                    currentState = CharacterState.Prone;
                    currentFrame = 0;
                    animationTimer = 0f;
                    UpdateSprites();
                    break;
                    
                case PlayerAnimationEvent.StandUp:
                    currentState = CharacterState.Stand;
                    currentFrame = 0;
                    animationTimer = 0f;
                    UpdateSprites();
                    break;
            }
        }
        
        public void OnMovementModifiersChanged(System.Collections.Generic.List<IMovementModifier> modifiers)
        {
            // Handle movement modifiers if needed (ice, slow, etc.)
            Debug.Log($"[MapleCharacterRenderer] Movement modifiers changed: {modifiers?.Count ?? 0} modifiers");
        }
        
        #endregion
        
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