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
        private bool isFacingLeft = false; // Track current facing direction
        
        // Attachment points for current frame (neck, navel, hand, etc.)
        private Dictionary<string, Vector2> currentAttachmentPoints = new Dictionary<string, Vector2>();
        
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
            
            // Set initial facing direction - MapleStory sprites face right by default
            SetFlipX(false);
            
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
            // Create sprite renderers in correct layer order matching C++ client CharLook::draw()
            // The order here matches the actual drawing order in MapleStory
            
            // Back layers (behind body)
            shieldRenderer = CreateSpriteLayer("Shield", -3);
            capeRenderer = CreateSpriteLayer("Cape", -2);
            backBodyRenderer = CreateSpriteLayer("BackBody", -1);
            
            // Main body
            bodyRenderer = CreateSpriteLayer("Body", 0);
            
            // Arm below head (drawn after body but before head)
            armRenderer = CreateSpriteLayer("Arm", 1);
            
            // Equipment on body
            shoesRenderer = CreateSpriteLayer("Shoes", 2);
            bottomRenderer = CreateSpriteLayer("Bottom", 3);
            topRenderer = CreateSpriteLayer("Top", 4);
            
            // Gloves (first layer)
            gloveRenderer = CreateSpriteLayer("Glove", 5);
            
            // Head layer - MUST be higher than body and initial equipment
            headRenderer = CreateSpriteLayer("Head", 10);
            
            // Face and hair - drawn AFTER head
            faceRenderer = CreateSpriteLayer("Face", 11);
            hairRenderer = CreateSpriteLayer("Hair", 12);
            
            // Hat/cap layer
            hatRenderer = CreateSpriteLayer("Hat", 13);
            
            // Arm/hand layers that go over hair
            armOverHairRenderer = CreateSpriteLayer("ArmOverHair", 14);
            handRenderer = CreateSpriteLayer("Hand", 15);
            
            // Weapon on top
            weaponRenderer = CreateSpriteLayer("Weapon", 16);
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
            
            // Apply attachment offsets after all sprites are loaded
            // This ensures we have all the necessary sprites before computing offsets
            ApplyAttachmentOffsets();
        }
        
        private void UpdateBodySprite()
        {
            // Clear all body part sprites first
            bodyRenderer.sprite = null;
            armRenderer.sprite = null;
            backBodyRenderer.sprite = null;
            armOverHairRenderer.sprite = null;
            handRenderer.sprite = null;
            
            // Reset positions to origin before applying new offsets
            bodyRenderer.transform.localPosition = Vector3.zero;
            armRenderer.transform.localPosition = Vector3.zero;
            backBodyRenderer.transform.localPosition = Vector3.zero;
            armOverHairRenderer.transform.localPosition = Vector3.zero;
            handRenderer.transform.localPosition = Vector3.zero;
            
            // Ensure renderers are enabled
            bodyRenderer.enabled = true;
            armRenderer.enabled = true;
            
            // Load all body parts from NXAssetLoader
            string stateName = ConvertStateToAnimationName(currentState);
            Dictionary<string, Vector2> attachmentPoints;
            var bodyParts = NXAssetLoader.Instance.LoadCharacterBodyParts(skinColor, stateName, currentFrame, out attachmentPoints);
            
            if (bodyParts != null && bodyParts.Count > 0)
            {
                Debug.Log($"Loaded {bodyParts.Count} body parts for state:{stateName} frame:{currentFrame}");
                
                // Assign sprites to appropriate renderers
                foreach (var part in bodyParts)
                {
                    string partKey = part.Key.ToLower();
                    Debug.Log($"Processing part '{part.Key}' (lowercase: '{partKey}')");
                    
                    switch (partKey)
                    {
                        case "body":
                            bodyRenderer.sprite = part.Value;
                            Debug.Log($"  - Body: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        case "arm":
                        case "armbelowhead": // Some animations might use this name
                            armRenderer.sprite = part.Value;
                            Debug.Log($"  - Arm: {part.Value.rect.width}x{part.Value.rect.height}, pivot: {part.Value.pivot}, bounds: {part.Value.bounds}");
                            // Keep at origin - sprite pivot handles positioning
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
                        case "lhand":
                        case "rhand":
                        case "handbelowweapon":
                            handRenderer.sprite = part.Value;
                            Debug.Log($"  - Hand: {part.Value.rect.width}x{part.Value.rect.height}");
                            break;
                            
                        default:
                            Debug.LogWarning($"  - Unknown part '{part.Key}': {part.Value.rect.width}x{part.Value.rect.height}");
                            // Try to assign unknown parts that might be arm-related
                            if (partKey.Contains("arm") && armRenderer.sprite == null)
                            {
                                armRenderer.sprite = part.Value;
                                Debug.Log($"    Assigned to arm renderer as fallback");
                            }
                            break;
                    }
                }
                
                // Store attachment points (offsets will be applied after all sprites are loaded)
                currentAttachmentPoints = attachmentPoints ?? new Dictionary<string, Vector2>();
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
        
        private void ApplyAttachmentOffsets()
        {
            // According to research6.txt, MapleStory uses attachment points to position parts relative to each other
            // The C++ client computes offsets like:
            // - Head position: body.map["neck"] - head.map["neck"]
            // - Arm position: arm.map["hand"] - arm.map["navel"] + body.map["navel"]
            
            Debug.Log("=== Applying Attachment Offsets ===");
            Debug.Log($"Total attachment points found: {currentAttachmentPoints.Count}");
            foreach (var kvp in currentAttachmentPoints)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
            
            // Get body attachment points
            Vector2 bodyNeck = GetAttachmentPoint("body.map.neck", "body.neck", "neck");
            Vector2 bodyNavel = GetAttachmentPoint("body.map.navel", "body.navel", "navel");
            
            Debug.Log($"Body neck point: {bodyNeck}");
            Debug.Log($"Body navel point: {bodyNavel}");
            
            // Apply head offset based on neck attachment
            if (bodyNeck != Vector2.zero)
            {
                // According to research6.txt: headPos = body.map["neck"] - head.map["neck"]
                // Get head's neck attachment point
                Vector2 headNeck = GetAttachmentPoint("head.map.neck", "head.neck");
                
                // Calculate offset to align head's neck with body's neck
                Vector3 headOffset = new Vector3(
                    (bodyNeck.x - headNeck.x) / 100f,
                    -(bodyNeck.y - headNeck.y) / 100f, // Flip Y for Unity
                    0
                );
                Debug.Log($"Head positioning: body neck {bodyNeck} - head neck {headNeck} = offset {headOffset}");
                UpdateHeadPosition(headOffset);
            }
            else
            {
                // Default head position if no attachment points
                Debug.Log("No neck attachment found, using default head position");
                UpdateHeadPosition(new Vector3(0, 0.32f, 0));
            }
            
            // Apply arm offset based on navel attachment
            if (armRenderer.sprite != null)
            {
                if (bodyNavel != Vector2.zero)
                {
                    // Get arm's navel point (shoulder connection)
                    Vector2 armNavel = GetAttachmentPoint("arm.map.navel", "arm.navel");
                    Debug.Log($"Arm navel point: {armNavel}");
                    
                    // According to research6.txt: arm shift = body.navel - arm.navel
                    // This aligns the arm's navel (shoulder) to the body's navel
                    Vector3 armOffset = new Vector3(
                        (bodyNavel.x - armNavel.x) / 100f,
                        -(bodyNavel.y - armNavel.y) / 100f, // Flip Y
                        0
                    );
                    
                    Debug.Log($"Applying arm offset: body navel {bodyNavel} - arm navel {armNavel} = {armOffset}");
                    armRenderer.transform.localPosition = armOffset;
                }
                else
                {
                    // Default arm position if no navel attachment
                    Debug.Log("No navel attachment found, using default arm position");
                    armRenderer.transform.localPosition = new Vector3(0, 0.2f, 0);
                }
            }
            
            // Apply hand offset if separate from arm
            if (handRenderer.sprite != null)
            {
                Vector2 armHand = GetAttachmentPoint("arm.map.hand", "arm.hand", "hand");
                if (armHand != Vector2.zero)
                {
                    Vector3 handOffset = new Vector3(
                        armHand.x / 100f,
                        -armHand.y / 100f,
                        0
                    );
                    Debug.Log($"Applying hand offset: {armHand} -> {handOffset}");
                    handRenderer.transform.localPosition = handOffset;
                }
            }
            
            Debug.Log("=== Attachment Offsets Complete ===");
        }
        
        private Vector2 GetAttachmentPoint(params string[] keys)
        {
            // Try each key in order until we find a valid attachment point
            foreach (var key in keys)
            {
                if (currentAttachmentPoints.TryGetValue(key, out Vector2 point))
                {
                    return point;
                }
            }
            return Vector2.zero;
        }
        
        private void UpdateHeadSprite()
        {
            // Load head sprite directly from asset loader
            string stateName = ConvertStateToAnimationName(currentState);
            Dictionary<string, Vector2> headAttachmentPoints;
            var headSprite = NXAssetLoader.Instance.LoadCharacterHead(skinColor, stateName, currentFrame, out headAttachmentPoints);
            
            if (headSprite != null)
            {
                headRenderer.sprite = headSprite;
                
                // Merge head attachment points into current attachment points
                if (headAttachmentPoints != null)
                {
                    foreach (var kvp in headAttachmentPoints)
                    {
                        currentAttachmentPoints[kvp.Key] = kvp.Value;
                    }
                    Debug.Log($"Loaded {headAttachmentPoints.Count} head attachment points");
                }
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
        
        private void UpdateHeadPosition(Vector3 position)
        {
            // According to research6.txt:
            // - Head is positioned at body's neck attachment point
            // - Face uses head's brow attachment point for positioning
            // - Hair also uses the brow attachment point
            
            if (headRenderer != null && headRenderer.gameObject != null)
            {
                headRenderer.transform.localPosition = position;
            }
            
            // Face and hair need additional offsets based on head's brow attachment
            Vector2 headBrow = GetAttachmentPoint("head.map.brow", "head.brow", "brow");
            
            if (faceRenderer != null && faceRenderer.gameObject != null)
            {
                // According to research6.txt: facePos = body.neck - head.neck + head.brow
                // Since head is already positioned at body.neck - head.neck, 
                // face just needs head.brow offset relative to head position
                if (headBrow != Vector2.zero)
                {
                    Vector3 faceOffset = position + new Vector3(
                        headBrow.x / 100f,
                        -headBrow.y / 100f,
                        0
                    );
                    faceRenderer.transform.localPosition = faceOffset;
                    Debug.Log($"Face positioned at head position + brow offset: {headBrow} -> {faceOffset}");
                }
                else
                {
                    // Default: align with head
                    faceRenderer.transform.localPosition = position;
                }
            }
            
            if (hairRenderer != null && hairRenderer.gameObject != null)
            {
                if (headBrow != Vector2.zero)
                {
                    // Hair is also positioned relative to head's brow point
                    Vector3 hairOffset = position + new Vector3(
                        headBrow.x / 100f,
                        -headBrow.y / 100f,
                        0
                    );
                    hairRenderer.transform.localPosition = hairOffset;
                }
                else
                {
                    // Default: align with head
                    hairRenderer.transform.localPosition = position;
                }
            }
            
            if (hatRenderer != null && hatRenderer.gameObject != null)
            {
                // Hat is positioned relative to head
                hatRenderer.transform.localPosition = position;
            }
            
            Debug.Log($"Updated head position to: {position}");
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
        
        public void OnPositionChanged(MapleClient.GameLogic.Vector2 position)
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
        
        public void OnVelocityChanged(MapleClient.GameLogic.Vector2 velocity)
        {
            // Use velocity for facing direction
            // MapleStory sprites face RIGHT by default
            // Flip when moving LEFT (negative X velocity)
            if (velocity.X != 0)
            {
                bool shouldFlip = velocity.X < 0;
                if (shouldFlip != isFacingLeft)
                {
                    isFacingLeft = shouldFlip;
                    SetFlipX(shouldFlip);
                    Debug.Log($"[MapleCharacterRenderer] Facing direction changed. Velocity: {velocity.X}, Facing left: {shouldFlip}");
                }
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