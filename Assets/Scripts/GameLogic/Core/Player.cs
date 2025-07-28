using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    public class Player : IPhysicsObject
    {
        public event Action Landed;
        
        // View listener management
        private readonly List<IPlayerViewListener> viewListeners = new List<IPlayerViewListener>();
        public bool HasViewListeners => viewListeners.Count > 0;
        
        // Player dimensions (in units)
        private const float PLAYER_HEIGHT = 0.6f; // 60 pixels / 100
        private const float PLAYER_WIDTH = 0.3f;  // 30 pixels / 100
        
        // Movement state
        private float actualWalkSpeed;
        private float actualJumpPower;
        private bool jumpKeyPressed = false; // Track jump key state for subsequent jumps
        private bool droppingThroughPlatform = false; // For one-way platform drop-down
        private float dropThroughTimer = 0f; // Timer to prevent immediate re-landing
        
        // Special movement
        private bool hasDoubleJump = false;
        private bool hasFlashJump = false;
        private int jumpCount = 0; // 0 = no jumps used, 1 = double jump used
        private float flashJumpCooldown = 0f;
        private const float FLASH_JUMP_COOLDOWN = 1f; // 1 second cooldown
        private const float FLASH_JUMP_DISTANCE = 1.5f; // 150 pixels / 100
        
        // Movement modifiers
        private readonly List<IMovementModifier> movementModifiers = new List<IMovementModifier>();

        public int Id { get; set; }
        public string Name { get; set; } = "Player";
        
        private Vector2 position;
        public Vector2 Position 
        { 
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    NotifyViewListeners(l => l.OnPositionChanged(value));
                }
            }
        }
        
        private Vector2 velocity;
        public Vector2 Velocity 
        { 
            get => velocity;
            set
            {
                if (velocity != value)
                {
                    velocity = value;
                    NotifyViewListeners(l => l.OnVelocityChanged(value));
                }
            }
        }
        
        private bool isGrounded;
        public bool IsGrounded 
        { 
            get => isGrounded;
            set
            {
                if (isGrounded != value)
                {
                    isGrounded = value;
                    NotifyViewListeners(l => l.OnGroundedStateChanged(value));
                }
            }
        }
        
        public bool IsJumping { get; set; }
        
        public LadderInfo GetCurrentLadder() => currentLadder;
        
        private PlayerState state;
        public PlayerState State 
        { 
            get => state;
            private set
            {
                if (state != value)
                {
                    state = value;
                    NotifyViewListeners(l => l.OnStateChanged(value));
                }
            }
        }

        // Combat stats
        private int baseDamage = 20;
        private int level = 1;
        private int hp = 100;
        private int maxHp = 100;
        private int mp = 50;
        private int maxMp = 50;
        
        // Character stats
        public int STR { get; set; } = 15;
        public int DEX { get; set; } = 15;
        public int INT { get; set; } = 15;
        public int LUK { get; set; } = 15;
        public int WeaponAttack { get; set; } = 20;
        public int MagicAttack { get; set; } = 0;
        public int WeaponDefense { get; set; } = 10;
        public int MagicDefense { get; set; } = 10;
        public int Accuracy { get; set; } = 100;
        public int Avoidability { get; set; } = 0;
        public int Speed { get; set; } = 100;
        public int JumpPower { get; set; } = 120;
        public int JobId { get; set; } = 0; // Beginner

        private bool isMovingLeft;
        private bool isMovingRight;
        private bool isClimbingUp;
        private bool isClimbingDown;
        private LadderInfo currentLadder;
        
        // IPhysicsObject implementation
        private static int nextPhysicsId = 1;
        private int physicsId;
        
        public int PhysicsId => physicsId;
        public bool UseGravity => State != PlayerState.Climbing;
        public bool IsPhysicsActive => true; // Player is always active

        public Player()
        {
            physicsId = nextPhysicsId++;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            IsGrounded = false; // Start not grounded to let gravity work
            State = PlayerState.Standing;
            
            // Initialize movement speeds based on stats
            UpdateMovementSpeeds();
        }
        
        public Player(int id, string name, int level, int jobId)
        {
            physicsId = nextPhysicsId++;
            Id = id;
            Name = name;
            this.level = level;
            JobId = jobId;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            IsGrounded = false;
            State = PlayerState.Standing;
            
            // Initialize movement speeds based on stats
            UpdateMovementSpeeds();
        }

        public void MoveLeft(bool active)
        {
            isMovingLeft = active;
            UpdateHorizontalVelocity();
        }

        public void MoveRight(bool active)
        {
            isMovingRight = active;
            UpdateHorizontalVelocity();
        }

        private void UpdateHorizontalVelocity()
        {
            // Don't move horizontally when crouching or climbing
            if (State == PlayerState.Crouching || State == PlayerState.Climbing)
            {
                Velocity = new Vector2(0, Velocity.Y);
                return;
            }

            // Check if movement is prevented
            bool movementPrevented = false;
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsMovement)
                {
                    movementPrevented = true;
                    break;
                }
            }

            if (movementPrevented)
            {
                // Apply friction to stop movement
                float velocityX = Velocity.X;
                velocityX = MaplePhysics.ApplyFriction(velocityX, 1f/60f, IsGrounded);
                Velocity = new Vector2(velocityX, Velocity.Y);
                return;
            }

            // Get modified walk speed
            float modifiedWalkSpeed = GetModifiedWalkSpeed();

            // Determine target velocity based on input
            float targetVelocityX = 0f;
            if (isMovingLeft && !isMovingRight)
            {
                targetVelocityX = -modifiedWalkSpeed;
            }
            else if (isMovingRight && !isMovingLeft)
            {
                targetVelocityX = modifiedWalkSpeed;
            }

            // Apply acceleration or friction
            float currentVelocityX = Velocity.X;
            if (targetVelocityX != 0)
            {
                // Accelerate towards target velocity
                currentVelocityX = MaplePhysics.ApplyMovementAcceleration(currentVelocityX, targetVelocityX, MaplePhysics.FIXED_TIMESTEP, IsGrounded);
                
                // Update state to walking if we're moving
                if (IsGrounded && State != PlayerState.Walking && State != PlayerState.Swimming && System.Math.Abs(currentVelocityX) > 0.1f)
                {
                    State = PlayerState.Walking;
                    TriggerAnimationEvent(PlayerAnimationEvent.StartWalk);
                }
            }
            else
            {
                // Get friction multiplier from modifiers
                float frictionMultiplier = 1f;
                foreach (var modifier in movementModifiers)
                {
                    frictionMultiplier *= modifier.FrictionMultiplier;
                }
                
                // Apply modified friction when no input (only on ground)
                if (IsGrounded && frictionMultiplier > 0)
                {
                    float friction = MaplePhysics.WalkFriction * frictionMultiplier;
                    float deceleration = friction * MaplePhysics.FIXED_TIMESTEP;
                    
                    if (currentVelocityX > 0)
                    {
                        currentVelocityX -= deceleration;
                        if (currentVelocityX < 0) currentVelocityX = 0;
                    }
                    else if (currentVelocityX < 0)
                    {
                        currentVelocityX += deceleration;
                        if (currentVelocityX > 0) currentVelocityX = 0;
                    }
                }
                
                // Update state to standing when stopped
                if (IsGrounded && (State == PlayerState.Walking || State == PlayerState.Swimming) && System.Math.Abs(currentVelocityX) < 0.01f)
                {
                    State = State == PlayerState.Swimming ? PlayerState.Swimming : PlayerState.Standing;
                    TriggerAnimationEvent(PlayerAnimationEvent.StopWalk);
                }
            }

            Velocity = new Vector2(currentVelocityX, Velocity.Y);
        }

        public void Jump()
        {
            // Check if jump key was previously released (for subsequent jumps)
            if (jumpKeyPressed)
            {
                return; // Can't jump again until key is released
            }
            
            // Check if jumping is prevented
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsJumping)
                {
                    return;
                }
            }
            
            jumpKeyPressed = true;
            float modifiedJumpPower = GetModifiedJumpPower();
            
            if (State == PlayerState.Climbing)
            {
                // Jump off ladder - apply horizontal velocity if moving
                StopClimbing();
                float horizontalVelocity = 0f;
                if (isMovingLeft) horizontalVelocity = -GetModifiedWalkSpeed();
                else if (isMovingRight) horizontalVelocity = GetModifiedWalkSpeed();
                
                Velocity = new Vector2(horizontalVelocity, modifiedJumpPower);
                IsJumping = true;
                State = PlayerState.Jumping;
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
            }
            else if (IsGrounded && State != PlayerState.Crouching)
            {
                Velocity = new Vector2(Velocity.X, modifiedJumpPower);
                IsJumping = true;
                IsGrounded = false;
                State = PlayerState.Jumping;
                jumpCount = 0; // Reset jump count
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
            }
            else if (!IsGrounded && hasDoubleJump && jumpCount == 0)
            {
                // Double jump
                jumpCount = 1;
                float doubleJumpPower = modifiedJumpPower * MaplePhysics.DoubleJumpModifier;
                Velocity = new Vector2(Velocity.X, doubleJumpPower);
                State = PlayerState.DoubleJumping;
                TriggerAnimationEvent(PlayerAnimationEvent.Jump);
            }
        }
        
        public void ReleaseJump()
        {
            jumpKeyPressed = false;
        }

        public void UpdatePhysics(float deltaTime, MapData mapData)
        {
            // Update movement modifiers
            UpdateMovementModifiers(deltaTime);
            
            // Update flash jump cooldown
            if (flashJumpCooldown > 0)
                flashJumpCooldown -= deltaTime;
            
            // Handle environmental effects
            UpdateEnvironmentalEffects(mapData);
            
            // Handle climbing physics separately
            if (State == PlayerState.Climbing)
            {
                UpdateClimbingPhysics(deltaTime);
                return;
            }
            
            // Update drop-through timer
            if (droppingThroughPlatform)
            {
                dropThroughTimer -= deltaTime;
                if (dropThroughTimer <= 0)
                {
                    droppingThroughPlatform = false;
                }
            }
            
            // Update horizontal velocity based on input and physics
            UpdateHorizontalVelocity();

            // Apply gravity if not grounded
            if (!IsGrounded)
            {
                bool inWater = mapData?.IsUnderwater ?? false;
                var newVelocityY = MaplePhysics.ApplyGravity(Velocity.Y, deltaTime, inWater);
                Velocity = new Vector2(Velocity.X, newVelocityY);
            }

            // Update position
            var newPosition = Position + Velocity * deltaTime;

            // Check for platform collision
            if (Velocity.Y <= 0 && !droppingThroughPlatform) // Only check when falling and not dropping through
            {
                var platformBelow = GetPlatformBelow(newPosition, mapData);
                if (platformBelow != null)
                {
                    // Skip one-way platforms if we're dropping through
                    if (droppingThroughPlatform && platformBelow.Type == PlatformType.OneWay)
                    {
                        // Continue falling through
                    }
                    else
                    {
                        // Platform Y is in pixels, convert to units
                        var platformYPixels = platformBelow.GetYAtX(newPosition.X * 100f);
                        if (!float.IsNaN(platformYPixels))
                        {
                            var platformY = platformYPixels / 100f;
                            // Check if we're falling through the platform (account for player height)
                            var playerBottom = newPosition.Y - PLAYER_HEIGHT / 2;
                            var prevPlayerBottom = Position.Y - PLAYER_HEIGHT / 2;
                            
                            // Check for collision: previous position above platform, new position at or below
                            if (prevPlayerBottom >= platformY - 0.01f && playerBottom <= platformY)
                            {
                                // Land on platform - position player so their bottom touches the platform
                                newPosition = new Vector2(newPosition.X, platformY + PLAYER_HEIGHT / 2);
                                Velocity = new Vector2(Velocity.X, 0);
                                
                                bool wasInAir = !IsGrounded;
                                IsGrounded = true;
                                IsJumping = false;
                                
                                if (wasInAir)
                                {
                                    jumpCount = 0; // Reset jump count on landing
                                    Landed?.Invoke();
                                    TriggerAnimationEvent(PlayerAnimationEvent.Land);
                                }
                                
                                // Update state when landing based on horizontal movement
                                if (State == PlayerState.Jumping)
                                {
                                    if (System.Math.Abs(Velocity.X) > 0.01f && (isMovingLeft || isMovingRight))
                                    {
                                        State = PlayerState.Walking;
                                    }
                                    else
                                    {
                                        State = PlayerState.Standing;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // No platform below, we're falling
                    if (IsGrounded)
                    {
                        IsGrounded = false;
                        if (State != PlayerState.Jumping)
                        {
                            State = PlayerState.Jumping; // Falling state
                        }
                    }
                }
            }
            
            // Check if we walked off a platform or need to adjust Y for slopes
            if (IsGrounded && mapData?.Platforms != null)
            {
                var currentPlatform = GetPlatformBelow(newPosition, mapData);
                if (currentPlatform == null)
                {
                    // We've moved off the platform
                    IsGrounded = false;
                    State = PlayerState.Jumping; // Start falling
                }
                else
                {
                    // Adjust Y position to stay on sloped platforms
                    var platformYPixels = currentPlatform.GetYAtX(newPosition.X * 100f);
                    if (!float.IsNaN(platformYPixels))
                    {
                        var platformY = platformYPixels / 100f;
                        // Keep player on the platform surface
                        newPosition = new Vector2(newPosition.X, platformY + PLAYER_HEIGHT / 2);
                    }
                }
            }

            Position = newPosition;
            
            // Update state for falling
            if (!IsGrounded && Velocity.Y < 0 && State != PlayerState.Jumping && State != PlayerState.DoubleJumping && State != PlayerState.FlashJumping)
            {
                State = PlayerState.Falling;
            }
        }

        private Platform GetPlatformBelow(Vector2 position, MapData mapData)
        {
            if (mapData?.Platforms == null || mapData.Platforms.Count == 0)
            {
                // No platforms available - this should be logged by the GameView layer
                return null;
            }

            // Convert position to pixels for platform comparison (use player's bottom)
            float posX = position.X * 100f;
            float posY = (position.Y - PLAYER_HEIGHT / 2) * 100f; // Player's bottom position

            Platform closestPlatform = null;
            float closestDistance = float.MaxValue;

            // Find the closest platform below the player
            foreach (var platform in mapData.Platforms)
            {
                // Only consider landable platforms
                if (platform.Type != PlatformType.Normal && platform.Type != PlatformType.OneWay)
                    continue;

                // Check if player X is within platform range
                if (posX < platform.X1 || posX > platform.X2)
                    continue;

                // Get platform Y at player's X position
                float platformY = platform.GetYAtX(posX);
                if (float.IsNaN(platformY))
                    continue;

                // In MapleStory coordinates, larger Y = lower position
                // Platform must be below player: platformY > posY
                float distance = platformY - posY;
                
                // Only consider platforms below the player (positive distance in MS coords) or very close
                // Allow some tolerance for floating point precision
                if (distance >= -5f && distance < closestDistance)
                {
                    closestPlatform = platform;
                    closestDistance = distance;
                }
            }

            // Only return platform if it's within a reasonable distance (not too far below)
            if (closestPlatform != null && closestDistance <= 100f) // 1 unit in pixels
            {
                return closestPlatform;
            }

            return null;
        }

        // Combat methods
        public int GetBaseDamage()
        {
            return baseDamage;
        }

        public void SetBaseDamage(int damage)
        {
            baseDamage = damage;
        }

        public int CurrentHP => hp;
        public int MaxHP
        {
            get => maxHp;
            set => maxHp = value;
        }
        public int CurrentMP
        {
            get => mp;
            set => mp = System.Math.Max(0, System.Math.Min(value, maxMp));
        }
        public int MaxMP
        {
            get => maxMp;
            set => maxMp = value;
        }
        public int Level
        {
            get => level;
            set => level = value;
        }
        
        // Inventory
        private Inventory inventory = new Inventory();
        public Inventory Inventory => inventory;
        private Dictionary<EquipSlot, int> equippedItems = new Dictionary<EquipSlot, int>();

        public void TakeDamage(int damage)
        {
            hp = System.Math.Max(0, hp - damage);
        }

        public void Heal(int amount)
        {
            hp = System.Math.Min(maxHp, hp + amount);
        }

        public void UseMana(int amount)
        {
            mp = System.Math.Max(0, mp - amount);
        }

        public void RestoreMana(int amount)
        {
            mp = System.Math.Min(maxMp, mp + amount);
        }
        
        // For network synchronization
        public void SetHPMP(int newHp, int newMp)
        {
            hp = System.Math.Max(0, System.Math.Min(newHp, maxHp));
            mp = System.Math.Max(0, System.Math.Min(newMp, maxMp));
        }

        public bool UseItem(int itemId)
        {
            if (!inventory.HasItem(itemId))
                return false;

            bool itemUsed = false;

            // Handle different item types
            switch (itemId)
            {
                case 2000000: // Red Potion - restores 50 HP
                    if (hp < maxHp)
                    {
                        Heal(50);
                        itemUsed = true;
                    }
                    break;

                case 2000001: // Orange Potion - restores 30 MP
                    if (mp < maxMp)
                    {
                        RestoreMana(30);
                        itemUsed = true;
                    }
                    break;

                // Add more items as needed
                default:
                    return false; // Unknown item
            }

            if (itemUsed)
            {
                inventory.RemoveItem(itemId, 1);
            }

            return itemUsed;
        }

        // Crouching methods
        public void Crouch(bool active)
        {
            if (active && IsGrounded && State != PlayerState.Climbing)
            {
                State = PlayerState.Crouching;
                Velocity = new Vector2(0, Velocity.Y); // Stop horizontal movement
                TriggerAnimationEvent(PlayerAnimationEvent.Crouch);
            }
            else if (!active && State == PlayerState.Crouching)
            {
                State = PlayerState.Standing;
                TriggerAnimationEvent(PlayerAnimationEvent.StandUp);
            }
        }
        
        // Drop through one-way platforms (called when down+jump is pressed)
        public void DropThroughPlatform()
        {
            if (IsGrounded && State != PlayerState.Climbing)
            {
                // Set the flag to drop through platforms
                // The actual check happens in UpdatePhysics where we have access to mapData
                droppingThroughPlatform = true;
                dropThroughTimer = 0.3f; // 300ms to fall through
                IsGrounded = false;
                State = PlayerState.Jumping;
                Velocity = new Vector2(Velocity.X, -0.5f); // Small downward velocity to start
            }
        }

        // Climbing methods
        public void StartClimbing(LadderInfo ladder)
        {
            if (ladder == null || !ladder.ContainsPosition(Position))
                return;

            currentLadder = ladder;
            State = PlayerState.Climbing;
            IsGrounded = false;
            IsJumping = false;
            Velocity = Vector2.Zero; // Stop all movement
            
            // Snap to ladder X position
            Position = new Vector2(ladder.X, Position.Y);
            TriggerAnimationEvent(PlayerAnimationEvent.StartClimb);
        }

        public void StopClimbing()
        {
            if (State == PlayerState.Climbing)
            {
                State = PlayerState.Standing;
                currentLadder = null;
                // Will start falling due to gravity
                TriggerAnimationEvent(PlayerAnimationEvent.StopClimb);
            }
        }

        public void ClimbUp(bool active)
        {
            if (State == PlayerState.Climbing)
            {
                isClimbingUp = active;
                UpdateClimbingVelocity();
            }
        }

        public void ClimbDown(bool active)
        {
            if (State == PlayerState.Climbing)
            {
                isClimbingDown = active;
                UpdateClimbingVelocity();
            }
        }

        private void UpdateClimbingVelocity()
        {
            if (State != PlayerState.Climbing)
                return;

            if (isClimbingUp && !isClimbingDown)
            {
                Velocity = new Vector2(0, MaplePhysics.ClimbSpeed);
            }
            else if (isClimbingDown && !isClimbingUp)
            {
                Velocity = new Vector2(0, -MaplePhysics.ClimbSpeed);
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }

        private void UpdateClimbingPhysics(float deltaTime)
        {
            if (currentLadder == null)
            {
                StopClimbing();
                return;
            }

            // Update position
            var newPosition = Position + Velocity * deltaTime;

            // Clamp to ladder bounds
            if (newPosition.Y > currentLadder.Y2)
            {
                newPosition = new Vector2(newPosition.X, currentLadder.Y2);
                Velocity = Vector2.Zero;
            }
            else if (newPosition.Y < currentLadder.Y1)
            {
                newPosition = new Vector2(newPosition.X, currentLadder.Y1);
                Velocity = Vector2.Zero;
            }

            Position = newPosition;
        }
        
        public Dictionary<EquipSlot, int> GetEquippedItems()
        {
            return new Dictionary<EquipSlot, int>(equippedItems);
        }
        
        public void EquipItem(int itemId, EquipSlot slot)
        {
            equippedItems[slot] = itemId;
        }
        
        public void UnequipItem(EquipSlot slot)
        {
            if (equippedItems.ContainsKey(slot))
            {
                equippedItems.Remove(slot);
            }
        }
        
        // Update movement speeds based on character stats
        private void UpdateMovementSpeeds()
        {
            actualWalkSpeed = MaplePhysics.GetWalkSpeed(Speed);
            actualJumpPower = MaplePhysics.GetJumpPower(JumpPower);
        }
        
        // Call this when Speed or JumpPower stats change
        public void OnStatsChanged()
        {
            UpdateMovementSpeeds();
        }
        
        // Special movement methods
        public void TryStartClimbing(MapData mapData, bool upPressed)
        {
            if (mapData?.Ladders == null) return;
            
            // Find nearest ladder within range
            foreach (var ladder in mapData.Ladders)
            {
                if (ladder.ContainsPosition(Position))
                {
                    StartClimbing(ladder);
                    return;
                }
            }
        }
        
        public void EnableDoubleJump(bool enabled)
        {
            hasDoubleJump = enabled;
        }
        
        public void EnableFlashJump(bool enabled)
        {
            hasFlashJump = enabled;
        }
        
        public int GetJumpCount()
        {
            return jumpCount;
        }
        
        public void SetJumpCount(int count)
        {
            jumpCount = count;
        }
        
        public float GetWalkSpeed()
        {
            return actualWalkSpeed;
        }
        
        public bool CanFlashJump()
        {
            return hasFlashJump && flashJumpCooldown <= 0 && !IsGrounded && (isMovingLeft || isMovingRight);
        }
        
        public void FlashJump()
        {
            if (!CanFlashJump()) return;
            
            // Teleport horizontally in movement direction
            float teleportDirection = isMovingRight ? 1f : -1f;
            Position = new Vector2(Position.X + (FLASH_JUMP_DISTANCE * teleportDirection), Position.Y);
            
            // Apply boost to velocity
            Velocity = new Vector2(actualWalkSpeed * 2f * teleportDirection, MaplePhysics.JumpSpeed * 0.3f);
            
            State = PlayerState.FlashJumping;
            flashJumpCooldown = FLASH_JUMP_COOLDOWN;
            TriggerAnimationEvent(PlayerAnimationEvent.Jump);
        }
        
        // Movement modifier methods
        public void AddMovementModifier(IMovementModifier modifier)
        {
            if (modifier != null && !movementModifiers.Contains(modifier))
            {
                movementModifiers.Add(modifier);
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public void RemoveMovementModifier(IMovementModifier modifier)
        {
            if (movementModifiers.Remove(modifier))
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public void RemoveMovementModifierById(string id)
        {
            int removed = movementModifiers.RemoveAll(m => m.Id == id);
            if (removed > 0)
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public bool HasActiveMovementModifier()
        {
            return movementModifiers.Count > 0;
        }
        
        public float GetModifiedWalkSpeed()
        {
            float speed = actualWalkSpeed;
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsMovement) return 0f;
                speed *= modifier.SpeedMultiplier;
            }
            return speed;
        }
        
        public float GetModifiedJumpPower()
        {
            float power = actualJumpPower;
            foreach (var modifier in movementModifiers)
            {
                if (modifier.PreventsJumping) return 0f;
                power *= modifier.JumpMultiplier;
            }
            return power;
        }
        
        public void UpdateMovementModifiers(float deltaTime)
        {
            int countBefore = movementModifiers.Count;
            movementModifiers.RemoveAll(modifier => !modifier.Update(deltaTime));
            if (movementModifiers.Count != countBefore)
            {
                NotifyViewListeners(l => l.OnMovementModifiersChanged(new List<IMovementModifier>(movementModifiers)));
            }
        }
        
        public List<IMovementModifier> GetActiveModifiers()
        {
            return new List<IMovementModifier>(movementModifiers);
        }
        
        private void UpdateEnvironmentalEffects(MapData mapData)
        {
            if (mapData == null) return;
            
            // Remove existing environmental modifiers
            RemoveMovementModifierById("slippery_surface");
            RemoveMovementModifierById("conveyor_belt");
            RemoveMovementModifierById("swimming");
            
            // Check for underwater
            if (mapData.IsUnderwater)
            {
                AddMovementModifier(new SwimmingModifier());
                if (State != PlayerState.Climbing)
                {
                    State = PlayerState.Swimming;
                }
            }
            
            // Check current platform for special properties
            if (IsGrounded)
            {
                var platform = GetCurrentPlatform(mapData);
                if (platform != null)
                {
                    if (platform.IsSlippery)
                    {
                        AddMovementModifier(new SlipperyModifier());
                    }
                    if (platform.IsConveyor)
                    {
                        var conveyorMod = new ConveyorModifier(platform.ConveyorSpeed);
                        AddMovementModifier(conveyorMod);
                        // Apply conveyor velocity
                        Velocity = new Vector2(Velocity.X + platform.ConveyorSpeed, Velocity.Y);
                    }
                }
            }
        }
        
        // IPhysicsObject implementation
        public void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal)
        {
            // Handle terrain collision
            // For now, this is handled in UpdatePhysics, but we can expand this later
            // for more complex collision responses
        }
        
        // View listener methods
        public void AddViewListener(IPlayerViewListener listener)
        {
            if (listener != null && !viewListeners.Contains(listener))
            {
                viewListeners.Add(listener);
            }
        }
        
        public void RemoveViewListener(IPlayerViewListener listener)
        {
            viewListeners.Remove(listener);
        }
        
        private void NotifyViewListeners(Action<IPlayerViewListener> action)
        {
            foreach (var listener in viewListeners)
            {
                action(listener);
            }
        }
        
        private void TriggerAnimationEvent(PlayerAnimationEvent animEvent)
        {
            NotifyViewListeners(l => l.OnAnimationEvent(animEvent));
        }
        
        // Public helper methods for platform queries
        public bool IsOnOneWayPlatform(MapData mapData)
        {
            if (!IsGrounded || mapData == null) return false;
            
            var platform = GetPlatformBelow(Position, mapData);
            return platform != null && platform.Type == PlatformType.OneWay;
        }
        
        public Platform GetCurrentPlatform(MapData mapData)
        {
            if (!IsGrounded || mapData == null) return null;
            return GetPlatformBelow(Position, mapData);
        }
    }
}