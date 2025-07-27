using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Core
{
    public class Player
    {
        public event Action Landed;
        
        // Player dimensions (in units)
        private const float PLAYER_HEIGHT = 0.6f; // 60 pixels / 100
        private const float PLAYER_WIDTH = 0.3f;  // 30 pixels / 100
        
        // Movement state
        private float actualWalkSpeed;
        private float actualJumpPower;

        public int Id { get; set; }
        public string Name { get; set; } = "Player";
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsJumping { get; set; }
        public PlayerState State { get; private set; }

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

        public Player()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            IsGrounded = false; // Start not grounded to let gravity work
            State = PlayerState.Standing;
            
            // Initialize movement speeds based on stats
            UpdateMovementSpeeds();
        }
        
        public Player(int id, string name, int level, int jobId)
        {
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

            if (isMovingLeft && !isMovingRight)
            {
                Velocity = new Vector2(-actualWalkSpeed, Velocity.Y);
                State = PlayerState.Walking;
            }
            else if (isMovingRight && !isMovingLeft)
            {
                Velocity = new Vector2(actualWalkSpeed, Velocity.Y);
                State = PlayerState.Walking;
            }
            else
            {
                // Apply friction when stopping
                float newVelocityX = MaplePhysics.ApplyFriction(Velocity.X, 0.016f); // 60 FPS deltaTime
                Velocity = new Vector2(newVelocityX, Velocity.Y);
                if (IsGrounded && State == PlayerState.Walking && System.Math.Abs(newVelocityX) < 1f)
                {
                    State = PlayerState.Standing;
                }
            }
        }

        public void Jump()
        {
            if (State == PlayerState.Climbing)
            {
                // Jump off ladder
                StopClimbing();
                Velocity = new Vector2(Velocity.X, actualJumpPower);
                IsJumping = true;
                State = PlayerState.Jumping;
            }
            else if (IsGrounded && State != PlayerState.Crouching)
            {
                Velocity = new Vector2(Velocity.X, actualJumpPower);
                IsJumping = true;
                IsGrounded = false;
                State = PlayerState.Jumping;
            }
        }

        public void UpdatePhysics(float deltaTime, MapData mapData)
        {
            // Handle climbing physics separately
            if (State == PlayerState.Climbing)
            {
                UpdateClimbingPhysics(deltaTime);
                return;
            }

            // Apply gravity if not grounded
            if (!IsGrounded)
            {
                var newVelocityY = MaplePhysics.ApplyGravity(Velocity.Y, deltaTime);
                Velocity = new Vector2(Velocity.X, newVelocityY);
            }

            // Update position
            var newPosition = Position + Velocity * deltaTime;

            // Check for platform collision when falling
            if (Velocity.Y <= 0) // Only check when falling or stationary
            {
                var platformBelow = GetPlatformBelow(newPosition, mapData);
                if (platformBelow != null)
                {
                    // Platform Y is in pixels, convert to units
                    var platformYPixels = platformBelow.GetYAtX(newPosition.X * 100f);
                    if (!float.IsNaN(platformYPixels))
                    {
                        var platformY = platformYPixels / 100f;
                        // Check if we're falling through the platform (account for player height)
                        var playerBottom = newPosition.Y - PLAYER_HEIGHT / 2;
                        var prevPlayerBottom = Position.Y - PLAYER_HEIGHT / 2;
                        
                        if (prevPlayerBottom >= platformY && playerBottom <= platformY)
                        {
                            // Land on platform - position player so their bottom touches the platform
                            newPosition = new Vector2(newPosition.X, platformY + PLAYER_HEIGHT / 2);
                            Velocity = new Vector2(Velocity.X, 0);
                            
                            bool wasInAir = !IsGrounded;
                            IsGrounded = true;
                            IsJumping = false;
                            
                            if (wasInAir)
                            {
                                Landed?.Invoke();
                            }
                            
                            // Update state when landing
                            if (State == PlayerState.Jumping)
                            {
                                State = PlayerState.Standing;
                            }
                        }
                    }
                }
            }
            else
            {
                // No platform below, we're in the air
                if (IsGrounded && Velocity.Y <= 0)
                {
                    IsGrounded = false;
                }
            }

            Position = newPosition;
        }

        private Platform GetPlatformBelow(Vector2 position, MapData mapData)
        {
            if (mapData?.Platforms == null)
            {
                // No platforms available - this should be logged by the GameView layer
                return null;
            }

            // Convert position to pixels for platform comparison (use player's bottom)
            float posX = position.X * 100f;
            float posY = (position.Y - PLAYER_HEIGHT / 2) * 100f; // Player's bottom position

            // Find all platforms that the player could land on
            var candidatePlatforms = mapData.Platforms
                .Where(p => p.Type == PlatformType.Normal || p.Type == PlatformType.OneWay)
                .Where(p => posX >= p.X1 && posX <= p.X2)
                .Select(p => new { Platform = p, Y = p.GetYAtX(posX) })
                .Where(p => !float.IsNaN(p.Y))
                .Where(p => p.Y <= posY + 50) // Look for platforms within reasonable range (in pixels)
                .OrderByDescending(p => p.Y);

            return candidatePlatforms.FirstOrDefault()?.Platform;
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
            }
            else if (!active && State == PlayerState.Crouching)
            {
                State = PlayerState.Standing;
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
        }

        public void StopClimbing()
        {
            if (State == PlayerState.Climbing)
            {
                State = PlayerState.Standing;
                currentLadder = null;
                // Will start falling due to gravity
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
    }
}