using MapleClient.GameLogic.Interfaces;
using System;

namespace MapleClient.GameLogic.Core
{
    /// <summary>
    /// Basic speed modifier implementation
    /// </summary>
    public class SpeedModifier : IMovementModifier
    {
        public string Id { get; }
        public float SpeedMultiplier { get; }
        public float JumpMultiplier { get; }
        public float FrictionMultiplier { get; }
        public bool PreventsMovement { get; }
        public bool PreventsJumping { get; }
        public float Duration { get; private set; }

        public SpeedModifier(float speedMultiplier, float duration, string id = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            SpeedMultiplier = speedMultiplier;
            JumpMultiplier = 1.0f;
            FrictionMultiplier = 1.0f;
            PreventsMovement = false;
            PreventsJumping = false;
            Duration = duration;
        }

        public bool Update(float deltaTime)
        {
            if (Duration < 0) return true; // Permanent modifier
            
            Duration -= deltaTime;
            return Duration > 0;
        }
    }

    /// <summary>
    /// Ice/slippery surface modifier
    /// </summary>
    public class SlipperyModifier : IMovementModifier
    {
        public string Id => "slippery_surface";
        public float SpeedMultiplier => 1.0f;
        public float JumpMultiplier => 1.0f;
        public float FrictionMultiplier => 0.1f; // Very low friction
        public bool PreventsMovement => false;
        public bool PreventsJumping => false;
        public float Duration => -1f; // Permanent while on ice

        public bool Update(float deltaTime)
        {
            return true; // Always active
        }
    }

    /// <summary>
    /// Conveyor belt modifier
    /// </summary>
    public class ConveyorModifier : IMovementModifier
    {
        private float conveyorSpeed;
        
        public string Id => "conveyor_belt";
        public float SpeedMultiplier => 1.0f;
        public float JumpMultiplier => 1.0f;
        public float FrictionMultiplier => 1.0f;
        public bool PreventsMovement => false;
        public bool PreventsJumping => false;
        public float Duration => -1f; // Permanent while on conveyor
        
        public float ConveyorSpeed => conveyorSpeed;

        public ConveyorModifier(float speed)
        {
            conveyorSpeed = speed;
        }

        public bool Update(float deltaTime)
        {
            return true; // Always active
        }
    }

    /// <summary>
    /// Underwater/swimming modifier
    /// </summary>
    public class SwimmingModifier : IMovementModifier
    {
        public string Id => "swimming";
        public float SpeedMultiplier => MaplePhysics.SwimSpeed / MaplePhysics.WalkSpeed;
        public float JumpMultiplier => MaplePhysics.SwimJump / MaplePhysics.JumpSpeed / 100f;
        public float FrictionMultiplier => 0.5f; // Water resistance
        public bool PreventsMovement => false;
        public bool PreventsJumping => false;
        public float Duration => -1f; // Permanent while underwater

        public bool Update(float deltaTime)
        {
            return true; // Always active
        }
    }

    /// <summary>
    /// Stun/freeze modifier that prevents movement
    /// </summary>
    public class StunModifier : IMovementModifier
    {
        public string Id { get; }
        public float SpeedMultiplier => 0f;
        public float JumpMultiplier => 0f;
        public float FrictionMultiplier => 1.0f;
        public bool PreventsMovement => true;
        public bool PreventsJumping => true;
        public float Duration { get; private set; }

        public StunModifier(float duration)
        {
            Id = "stun_" + Guid.NewGuid().ToString();
            Duration = duration;
        }

        public bool Update(float deltaTime)
        {
            Duration -= deltaTime;
            return Duration > 0;
        }
    }
}