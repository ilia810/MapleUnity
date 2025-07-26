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
        
        // Movement constants (units per second, adjusted for Unity scale)
        public const float WalkSpeed = 3.0f; // Increased for better feel
        public const float WalkForce = 140f / PIXELS_TO_UNITS;
        public const float WalkDrag = 800f; // Reduced for smoother movement
        
        // Jump constants
        public const float JumpSpeed = 8.0f; // Increased for proper jumping
        public const float DoubleJumpSpeed = 8.0f;
        public const float DoubleJumpModifier = 0.7f; // Second jump is 70% of first
        
        // Gravity and falling
        public const float Gravity = 25f; // Adjusted for good game feel
        public const float MaxFallSpeed = 10f; // Reasonable falling speed
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
        /// Apply movement acceleration (MapleStory uses instant acceleration)
        /// </summary>
        public static float ApplyMovementAcceleration(float currentVelocity, float targetVelocity, float deltaTime)
        {
            // MapleStory has nearly instant acceleration
            float acceleration = 10000f / PIXELS_TO_UNITS; // Very high acceleration in units/s²
            
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
        /// Apply friction when stopping
        /// </summary>
        public static float ApplyFriction(float velocity, float deltaTime)
        {
            if (velocity == 0) return 0;
            
            float friction = WalkDrag * deltaTime;
            
            if (velocity > 0)
            {
                velocity -= friction;
                if (velocity < 0) velocity = 0;
            }
            else
            {
                velocity += friction;
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