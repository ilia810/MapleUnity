using System;
using System.Linq;

namespace MapleClient.GameLogic.Core
{
    public class Player
    {
        public event Action Landed;
        private const float MoveSpeed = 125f;
        private const float ClimbSpeed = 100f;
        private const float JumpPower = 555f;
        private const float Gravity = 2000f;
        private const float MaxFallSpeed = 670f;

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
                Velocity = new Vector2(-MoveSpeed, Velocity.Y);
                State = PlayerState.Walking;
            }
            else if (isMovingRight && !isMovingLeft)
            {
                Velocity = new Vector2(MoveSpeed, Velocity.Y);
                State = PlayerState.Walking;
            }
            else
            {
                Velocity = new Vector2(0, Velocity.Y);
                if (IsGrounded && State == PlayerState.Walking)
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
                Velocity = new Vector2(Velocity.X, JumpPower);
                IsJumping = true;
                State = PlayerState.Jumping;
            }
            else if (IsGrounded && State != PlayerState.Crouching)
            {
                Velocity = new Vector2(Velocity.X, JumpPower);
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
                var newVelocityY = Velocity.Y - (Gravity * deltaTime);
                if (newVelocityY < -MaxFallSpeed)
                {
                    newVelocityY = -MaxFallSpeed;
                }
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
                    var platformY = platformBelow.GetYAtX(newPosition.X);
                    if (!float.IsNaN(platformY))
                    {
                        // Check if we're falling through the platform
                        if (Position.Y >= platformY && newPosition.Y <= platformY)
                        {
                            // Land on platform
                            newPosition = new Vector2(newPosition.X, platformY);
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
                return null;

            // Find all platforms that the player could land on
            var candidatePlatforms = mapData.Platforms
                .Where(p => p.Type == PlatformType.Normal || p.Type == PlatformType.OneWay)
                .Where(p => position.X >= p.X1 && position.X <= p.X2)
                .Select(p => new { Platform = p, Y = p.GetYAtX(position.X) })
                .Where(p => !float.IsNaN(p.Y))
                .Where(p => p.Y <= position.Y + 50) // Look for platforms within reasonable range
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

        public int HP => hp;
        public int MaxHP => maxHp;
        public int MP => mp;
        public int MaxMP => maxMp;
        public int Level => level;
        
        // Inventory
        private Inventory inventory = new Inventory();
        public Inventory Inventory => inventory;

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
                Velocity = new Vector2(0, ClimbSpeed);
            }
            else if (isClimbingDown && !isClimbingUp)
            {
                Velocity = new Vector2(0, -ClimbSpeed);
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
    }
}