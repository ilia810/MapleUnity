namespace MapleClient.GameLogic.Core
{
    /// <summary>
    /// MapleStory v83 physics constants and calculations
    /// Based on actual game values, converted to Unity units (1 unit = 100 pixels)
    /// </summary>
    public static class MaplePhysics
    {
        // Pixel to Unity unit conversion
        private const float PIXELS_TO_UNITS = 100f;
        
        // Fixed timestep for 60 FPS physics
        public const float FIXED_TIMESTEP = 1f / 60f;
        
        // Movement constants (units per second, adjusted for Unity scale)
        // MapleStory v83 typical walk speed is ~125 pixels/second at 100% speed
        // In Unity units: 125 pixels / 100 = 1.25 units/second base
        public const float WalkSpeed = 1.25f; // Base walk speed in units/second
        public const float WalkAcceleration = 14f; // 1400 pixels/s² = 14 units/s² (from research3.txt "Walk Force ~1.4 units/s²")
        public const float WalkFriction = 8f; // 800 pixels/s² = 8 units/s² (from research3.txt "Walk Drag ~80 in internal units")
        public const float AirControlFactor = 0.8f; // 80% control in air
        
        // Jump constants
        // MapleStory v83 jump velocity is ~555 pixels/second at 120% jump
        // Base jump at 100% would be ~462 pixels/second = 4.62 units/second
        public const float JumpSpeed = 5.55f; // Jump velocity in units/second  
        public const float DoubleJumpSpeed = 5.55f;
        public const float DoubleJumpModifier = 0.7f; // Second jump is 70% of first
        
        // Gravity and falling
        // MapleStory v83 gravity is ~2000 pixels/second² = 20 units/second²
        public const float Gravity = 20f; // Gravity acceleration in units/second²
        public const float MaxFallSpeed = 6.7f; // Terminal velocity ~670 pixels/second (from research3.txt)
        public const float FallDrag = 0f; // No air resistance in MapleStory
        
        // Climbing
        public const float ClimbSpeed = 120f / PIXELS_TO_UNITS;
        public const float RopeSpeed = 120f / PIXELS_TO_UNITS;
        
        // Swimming (if in water)
        public const float SwimSpeed = 140f / PIXELS_TO_UNITS;
        public const float SwimSpeedSlow = 28f / PIXELS_TO_UNITS;
        public const float SwimJump = 308f / PIXELS_TO_UNITS;
        public const float SwimGravity = 280f / PIXELS_TO_UNITS;
        
        // Flying
        public const float FlySpeed = 200f / PIXELS_TO_UNITS;
        public const float FlyJumpSpeed = 300f / PIXELS_TO_UNITS;
        public const float FlyGravity = 600f / PIXELS_TO_UNITS;
        
        // Misc
        public const float SlipForce = 60f / PIXELS_TO_UNITS; // Ice physics
        public const float FlipDuration = 0.9f; // Time for jump flip animation
        
        /// <summary>
        /// Calculate actual walk speed based on character stats
        /// </summary>
        public static float GetWalkSpeed(int speedStat)
        {
            // Base speed + (speed stat * multiplier)
            // Speed stat of 100 = 100% speed, 140 = 140% speed
            return WalkSpeed * (speedStat / 100f);
        }
        
        /// <summary>
        /// Calculate actual jump power based on character stats
        /// </summary>
        public static float GetJumpPower(int jumpStat)
        {
            // Similar to walk speed
            return JumpSpeed * (jumpStat / 100f);
        }
        
        /// <summary>
        /// Apply movement acceleration with proper MapleStory v83 physics
        /// </summary>
        public static float ApplyMovementAcceleration(float currentVelocity, float targetVelocity, float deltaTime, bool isGrounded)
        {
            // Use different acceleration rates for ground vs air
            float acceleration = isGrounded ? WalkAcceleration : (WalkAcceleration * AirControlFactor);
            
            if (currentVelocity < targetVelocity)
            {
                currentVelocity += acceleration * deltaTime;
                if (currentVelocity > targetVelocity)
                    currentVelocity = targetVelocity;
            }
            else if (currentVelocity > targetVelocity)
            {
                currentVelocity -= acceleration * deltaTime;
                if (currentVelocity < targetVelocity)
                    currentVelocity = targetVelocity;
            }
            
            return currentVelocity;
        }
        
        /// <summary>
        /// Apply friction when stopping (only on ground, no air friction)
        /// </summary>
        public static float ApplyFriction(float velocity, float deltaTime, bool isGrounded)
        {
            if (velocity == 0 || !isGrounded) return velocity; // No friction in air
            
            float deceleration = WalkFriction * deltaTime;
            
            if (velocity > 0)
            {
                velocity -= deceleration;
                if (velocity < 0) velocity = 0;
            }
            else
            {
                velocity += deceleration;
                if (velocity > 0) velocity = 0;
            }
            
            return velocity;
        }
        
        /// <summary>
        /// Apply gravity with terminal velocity
        /// </summary>
        public static float ApplyGravity(float velocityY, float deltaTime, bool inWater = false)
        {
            float gravity = inWater ? SwimGravity : Gravity;
            float maxFall = inWater ? SwimSpeed : MaxFallSpeed;
            
            velocityY -= gravity * deltaTime;
            
            if (velocityY < -maxFall)
                velocityY = -maxFall;
                
            return velocityY;
        }
        
        /// <summary>
        /// Get knockback velocity from damage
        /// </summary>
        public static Vector2 GetKnockback(int damage, bool fromLeft)
        {
            // Knockback is based on damage amount (converted to units)
            float power = System.Math.Min(damage * 0.5f, 300f) / PIXELS_TO_UNITS;
            float x = fromLeft ? power : -power;
            float y = 200f / PIXELS_TO_UNITS; // Always some upward component
            
            return new Vector2(x, y);
        }
    }
}