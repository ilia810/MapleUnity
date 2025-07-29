using UnityEngine;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    /// <summary>
    /// Visual player controller that syncs with GameLogic physics
    /// This controller acts as a visual representation only - all physics are handled by GameLogic
    /// </summary>
    public class SimplePlayerController : MonoBehaviour, IPlayerViewListener
    {
        [Header("Components")]
        private BoxCollider2D col;
        private SpriteRenderer spriteRenderer;
        
        // Reference to game logic components
        private Player gameLogicPlayer;
        private GameWorld gameWorld;
        
        // Visual state
        private bool facingRight = true;
        private PlayerState currentState = PlayerState.Standing;
        private PlayerState previousState = PlayerState.Standing;
        
        // Interpolation for smooth rendering
        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private bool useInterpolation = true;
        
        // Animation state tracking
        private bool wasGrounded = true;
        
        // Debug tracking
        private float lastDebugTime = 0f;
        private float debugInterval = 1f; // Log position every second
        
        // Visual feedback components
        private GameObject stateIndicator;
        private TextMesh stateText;
        private GameObject ladderPrompt;
        private GameObject modifierDisplay;
        private List<GameObject> activeEffects = new List<GameObject>();
        private Dictionary<string, GameObject> activeModifierEffects = new Dictionary<string, GameObject>();
        
        // Colors for different states
        private readonly Color normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        private readonly Color jumpingColor = new Color(0.3f, 0.5f, 1f, 1f);
        private readonly Color doubleJumpColor = new Color(0.5f, 0.7f, 1f, 1f);
        private readonly Color flashJumpColor = new Color(0.8f, 0.9f, 1f, 1f);
        private readonly Color climbingColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        private readonly Color fallingColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        private readonly Color crouchingColor = new Color(0.1f, 0.2f, 0.4f, 1f);
        
        void Awake()
        {
            // Add collider as trigger only (no physics collision)
            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.3f, 0.6f);
            col.isTrigger = true; // Set as trigger to prevent physics interactions
            
            Debug.Log($"[SimplePlayerController] Created as kinematic visual controller");
            
            // Create simple blue sprite
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(30, 60);
            var pixels = new Color[30 * 60];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = normalColor;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 30, 60), new Vector2(0.5f, 0.5f), 100);
            spriteRenderer.sortingLayerName = "Player";
            spriteRenderer.sortingOrder = 100;
            
            // Set layer for rendering
            gameObject.layer = LayerMask.NameToLayer("Default");
            
            // Create state indicator
            CreateStateIndicator();
            
            // Create ladder prompt (initially hidden)
            CreateLadderPrompt();
            
            // Create modifier display
            CreateModifierDisplay();
        }
        
        public void SetGameLogicPlayer(Player player)
        {
            // Unregister from previous player
            if (gameLogicPlayer != null)
            {
                gameLogicPlayer.RemoveViewListener(this);
            }
            
            gameLogicPlayer = player;
            
            // Register as listener and sync initial state
            if (player != null)
            {
                player.AddViewListener(this);
                
                // Sync initial position
                var pos = new Vector3(player.Position.X, player.Position.Y, 0);
                transform.position = pos;
                previousPosition = pos;
                currentPosition = pos;
                
                // Sync initial state
                currentState = player.State;
                wasGrounded = player.IsGrounded;
                facingRight = player.Velocity.X >= 0;
                
                Debug.Log($"[SimplePlayerController] SetGameLogicPlayer - Initial sync: Position={pos}, State={currentState}, Grounded={wasGrounded}");
            }
        }
        
        public void SetGameWorld(GameWorld world)
        {
            gameWorld = world;
        }
        
        void FixedUpdate()
        {
            // In batch mode, force position sync in FixedUpdate as well
            if (Application.isBatchMode && gameLogicPlayer != null)
            {
                transform.position = new Vector3(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y, 0);
            }
        }
        
        void Update()
        {
            if (gameLogicPlayer == null) return;
            
            // In batch mode, always use direct position sync
            if (Application.isBatchMode)
            {
                transform.position = new Vector3(gameLogicPlayer.Position.X, gameLogicPlayer.Position.Y, 0);
            }
            else
            {
                // Interpolate position for smooth visual movement
                if (useInterpolation && gameWorld != null)
                {
                    float interpolationFactor = gameWorld.GetPhysicsInterpolationFactor();
                    transform.position = Vector3.Lerp(previousPosition, currentPosition, interpolationFactor);
                }
                else
                {
                    // Fallback to direct position sync
                    transform.position = currentPosition;
                }
            }
            
            // Debug logging
            if (Time.time - lastDebugTime > debugInterval)
            {
                lastDebugTime = Time.time;
                Debug.Log($"[SimplePlayerController] Visual Position: {transform.position}, " +
                         $"Current Position: {currentPosition}, " +
                         $"GameLogic Position: {(gameLogicPlayer != null ? gameLogicPlayer.Position.ToString() : "null")}, " +
                         $"State: {currentState}, " +
                         $"Grounded: {wasGrounded}, " +
                         $"Interpolation: {(useInterpolation ? "ON" : "OFF")}");
            }
        }
        
        // IPlayerViewListener implementation
        public void OnPositionChanged(MapleClient.GameLogic.Vector2 position)
        {
            // Store positions for interpolation
            previousPosition = currentPosition;
            currentPosition = new Vector3(position.X, position.Y, 0);
        }
        
        public void OnStateChanged(PlayerState state)
        {
            previousState = currentState;
            currentState = state;
            UpdateStateVisuals();
        }
        
        public void OnVelocityChanged(MapleClient.GameLogic.Vector2 velocity)
        {
            // Update facing direction based on velocity
            if (velocity.X != 0)
            {
                facingRight = velocity.X > 0;
                transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
            }
        }
        
        public void OnGroundedStateChanged(bool isGrounded)
        {
            wasGrounded = isGrounded;
        }
        
        public void OnAnimationEvent(PlayerAnimationEvent animEvent)
        {
            switch (animEvent)
            {
                case PlayerAnimationEvent.Jump:
                    PlayJumpAnimation();
                    break;
                case PlayerAnimationEvent.Land:
                    PlayLandAnimation();
                    break;
                case PlayerAnimationEvent.Attack:
                    PlayAttackAnimation();
                    break;
                case PlayerAnimationEvent.StartWalk:
                    // TODO: Start walk animation
                    break;
                case PlayerAnimationEvent.StopWalk:
                    // TODO: Stop walk animation
                    break;
                case PlayerAnimationEvent.StartClimb:
                    // TODO: Start climb animation
                    break;
                case PlayerAnimationEvent.StopClimb:
                    // TODO: Stop climb animation
                    break;
                case PlayerAnimationEvent.Crouch:
                    // TODO: Crouch animation
                    break;
                case PlayerAnimationEvent.StandUp:
                    // TODO: Stand up animation
                    break;
            }
        }
        
        public void OnMovementModifiersChanged(List<IMovementModifier> modifiers)
        {
            UpdateMovementModifiers(modifiers);
        }
        
        // Visual effects methods (can be expanded later)
        private void PlayJumpAnimation()
        {
            Debug.Log("[SimplePlayerController] Jump animation triggered");
            CreateJumpEffect();
        }
        
        private void PlayLandAnimation()
        {
            Debug.Log("[SimplePlayerController] Land animation triggered");
            CreateLandingEffect();
        }
        
        private void PlayAttackAnimation()
        {
            Debug.Log("[SimplePlayerController] Attack animation triggered");
        }
        
        // Visual feedback methods
        private void CreateStateIndicator()
        {
            stateIndicator = new GameObject("StateIndicator");
            stateIndicator.transform.SetParent(transform);
            stateIndicator.transform.localPosition = new Vector3(0, 0.5f, 0);
            
            stateText = stateIndicator.AddComponent<TextMesh>();
            stateText.text = "Standing";
            stateText.fontSize = 12;
            stateText.color = Color.white;
            stateText.anchor = TextAnchor.MiddleCenter;
            stateText.alignment = TextAlignment.Center;
            stateText.characterSize = 0.05f;
            
            // Add renderer for sorting
            var renderer = stateIndicator.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = 1000;
        }
        
        private void CreateLadderPrompt()
        {
            ladderPrompt = new GameObject("LadderPrompt");
            ladderPrompt.transform.SetParent(transform);
            ladderPrompt.transform.localPosition = new Vector3(0, 0.8f, 0);
            
            var promptText = ladderPrompt.AddComponent<TextMesh>();
            promptText.text = "[UP/DOWN] Climb";
            promptText.fontSize = 10;
            promptText.color = new Color(1f, 1f, 0.5f, 0.8f);
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.alignment = TextAlignment.Center;
            promptText.characterSize = 0.04f;
            
            var renderer = ladderPrompt.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = 1001;
            
            ladderPrompt.SetActive(false);
        }
        
        private void CreateModifierDisplay()
        {
            modifierDisplay = new GameObject("ModifierDisplay");
            modifierDisplay.transform.SetParent(transform);
            modifierDisplay.transform.localPosition = new Vector3(0, -0.5f, 0);
        }
        
        private void UpdateStateVisuals()
        {
            // Update state text
            if (stateText != null)
            {
                stateText.text = currentState.ToString();
                
                // Update text color based on state
                switch (currentState)
                {
                    case PlayerState.Jumping:
                        stateText.color = new Color(0.5f, 0.8f, 1f);
                        break;
                    case PlayerState.DoubleJumping:
                        stateText.color = new Color(0.8f, 0.5f, 1f);
                        break;
                    case PlayerState.FlashJumping:
                        stateText.color = new Color(1f, 0.8f, 0.5f);
                        break;
                    case PlayerState.Climbing:
                        stateText.color = new Color(0.5f, 1f, 0.5f);
                        break;
                    case PlayerState.Falling:
                        stateText.color = new Color(1f, 0.5f, 0.5f);
                        break;
                    case PlayerState.Crouching:
                        stateText.color = new Color(0.7f, 0.7f, 0.7f);
                        break;
                    default:
                        stateText.color = Color.white;
                        break;
                }
            }
            
            // Update sprite color
            UpdateSpriteColor();
            
            // Trigger state-specific effects
            if (previousState != currentState)
            {
                switch (currentState)
                {
                    case PlayerState.FlashJumping:
                        CreateFlashJumpEffect();
                        break;
                    case PlayerState.DoubleJumping:
                        CreateDoubleJumpEffect();
                        break;
                }
            }
        }
        
        private void UpdateSpriteColor()
        {
            if (spriteRenderer == null) return;
            
            Color targetColor = normalColor;
            switch (currentState)
            {
                case PlayerState.Jumping:
                    targetColor = jumpingColor;
                    break;
                case PlayerState.DoubleJumping:
                    targetColor = doubleJumpColor;
                    break;
                case PlayerState.FlashJumping:
                    targetColor = flashJumpColor;
                    break;
                case PlayerState.Climbing:
                    targetColor = climbingColor;
                    break;
                case PlayerState.Falling:
                    targetColor = fallingColor;
                    break;
                case PlayerState.Crouching:
                    targetColor = crouchingColor;
                    break;
            }
            
            // Update sprite texture with new color
            var texture = new Texture2D(30, 60);
            var pixels = new Color[30 * 60];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = targetColor;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 30, 60), new Vector2(0.5f, 0.5f), 100);
        }
        
        // Effect creation methods
        private void CreateJumpEffect()
        {
            var effect = CreateParticleEffect("JumpEffect", new Vector3(0, -0.3f, 0), 
                new Color(0.8f, 0.8f, 1f, 0.5f), 5, 0.2f);
            activeEffects.Add(effect);
        }
        
        private void CreateDoubleJumpEffect()
        {
            var effect = CreateParticleEffect("DoubleJumpEffect", new Vector3(0, -0.2f, 0), 
                new Color(0.5f, 0.8f, 1f, 0.7f), 8, 0.3f);
            activeEffects.Add(effect);
        }
        
        private void CreateFlashJumpEffect()
        {
            // Create a more dramatic effect for flash jump
            var effect = CreateParticleEffect("FlashJumpEffect", Vector3.zero, 
                new Color(1f, 0.9f, 0.5f, 0.8f), 12, 0.5f);
            activeEffects.Add(effect);
            
            // Use VisualEffectManager for trail effect
            if (VisualEffectManager.Instance != null)
            {
                var trail = VisualEffectManager.Instance.SpawnEffect("FlashJumpTrail", transform.position, transform, 0.5f);
                if (trail != null)
                {
                    activeEffects.Add(trail);
                }
            }
        }
        
        private void CreateLandingEffect()
        {
            var effect = CreateParticleEffect("LandingEffect", new Vector3(0, -0.3f, 0), 
                new Color(0.6f, 0.6f, 0.6f, 0.5f), 6, 0.25f);
            activeEffects.Add(effect);
        }
        
        private GameObject CreateParticleEffect(string name, Vector3 localPosition, Color color, int particleCount, float duration)
        {
            var effect = new GameObject(name);
            effect.transform.position = transform.position + localPosition;
            
            // Simple particle simulation using sprites
            for (int i = 0; i < particleCount; i++)
            {
                var particle = new GameObject($"Particle_{i}");
                particle.transform.SetParent(effect.transform);
                particle.transform.localPosition = Vector3.zero;
                
                var particleRenderer = particle.AddComponent<SpriteRenderer>();
                var particleTexture = new Texture2D(4, 4);
                var particlePixels = new Color[16];
                for (int j = 0; j < particlePixels.Length; j++)
                {
                    particlePixels[j] = color;
                }
                particleTexture.SetPixels(particlePixels);
                particleTexture.Apply();
                
                particleRenderer.sprite = Sprite.Create(particleTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
                particleRenderer.sortingLayerName = "Player";
                particleRenderer.sortingOrder = 98;
                
                // Animate particle
                StartCoroutine(AnimateParticle(particle, duration));
            }
            
            Destroy(effect, duration + 0.1f);
            return effect;
        }
        
        private System.Collections.IEnumerator AnimateParticle(GameObject particle, float duration)
        {
            float elapsed = 0f;
            Vector3 velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), 0f);
            Vector3 startPos = particle.transform.localPosition;
            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
            Color startColor = renderer.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Move particle
                particle.transform.localPosition = startPos + velocity * elapsed;
                velocity.y -= 5f * Time.deltaTime; // Gravity
                
                // Fade out
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = color;
                
                yield return null;
            }
        }
        
        public void ShowLadderPrompt(bool show)
        {
            if (ladderPrompt != null)
            {
                ladderPrompt.SetActive(show);
            }
        }
        
        public void UpdateMovementModifiers(List<IMovementModifier> modifiers)
        {
            // Clear old modifier displays
            foreach (Transform child in modifierDisplay.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Clear old modifier effects
            foreach (var effect in activeModifierEffects.Values)
            {
                if (effect != null && VisualEffectManager.Instance != null)
                {
                    VisualEffectManager.Instance.ReturnToPool(effect, "ModifierEffect");
                }
            }
            activeModifierEffects.Clear();
            
            // Display active modifiers
            float yOffset = 0f;
            foreach (var modifier in modifiers)
            {
                // Create text display
                var modifierText = new GameObject($"Modifier_{modifier.Id}");
                modifierText.transform.SetParent(modifierDisplay.transform);
                modifierText.transform.localPosition = new Vector3(0, yOffset, 0);
                
                var text = modifierText.AddComponent<TextMesh>();
                text.fontSize = 8;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.03f;
                
                // Set text based on modifier type
                string modifierInfo = "";
                Color textColor = Color.white;
                
                if (modifier.Id == "slippery_surface")
                {
                    modifierInfo = "ICE";
                    textColor = new Color(0.5f, 0.8f, 1f);
                }
                else if (modifier.Id == "swimming")
                {
                    modifierInfo = "SWIM";
                    textColor = new Color(0.2f, 0.5f, 1f);
                }
                else if (modifier.Id.StartsWith("stun_"))
                {
                    modifierInfo = $"STUN {modifier.Duration:F1}s";
                    textColor = new Color(1f, 0.5f, 0.5f);
                }
                else if (modifier.SpeedMultiplier > 1f)
                {
                    modifierInfo = $"SPEED x{modifier.SpeedMultiplier:F1}";
                    textColor = new Color(0.5f, 1f, 0.5f);
                }
                else if (modifier.SpeedMultiplier < 1f)
                {
                    modifierInfo = $"SLOW x{modifier.SpeedMultiplier:F1}";
                    textColor = new Color(1f, 0.8f, 0.5f);
                }
                
                text.text = modifierInfo;
                text.color = textColor;
                
                var renderer = modifierText.GetComponent<MeshRenderer>();
                renderer.sortingLayerName = "UI";
                renderer.sortingOrder = 1002;
                
                // Show visual effect for modifier
                if (VisualEffectManager.Instance != null)
                {
                    VisualEffectManager.Instance.ShowMovementModifierEffect(modifier, transform);
                }
                
                yOffset -= 0.15f;
            }
        }
        
        void OnDestroy()
        {
            // Clean up listener registration
            if (gameLogicPlayer != null)
            {
                gameLogicPlayer.RemoveViewListener(this);
            }
            
            // Clean up active effects
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                    Destroy(effect);
            }
            activeEffects.Clear();
        }
    }
}