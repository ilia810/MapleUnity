using MapleClient.GameLogic.Core;

namespace MapleClient.GameLogic.Interfaces
{
    /// <summary>
    /// Interface for modifying player movement properties
    /// </summary>
    public interface IMovementModifier
    {
        /// <summary>
        /// Unique identifier for this modifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Speed multiplier (1.0 = normal speed)
        /// </summary>
        float SpeedMultiplier { get; }

        /// <summary>
        /// Jump power multiplier (1.0 = normal jump)
        /// </summary>
        float JumpMultiplier { get; }

        /// <summary>
        /// Friction multiplier for ground movement (1.0 = normal friction)
        /// </summary>
        float FrictionMultiplier { get; }

        /// <summary>
        /// Whether this modifier prevents movement
        /// </summary>
        bool PreventsMovement { get; }

        /// <summary>
        /// Whether this modifier prevents jumping
        /// </summary>
        bool PreventsJumping { get; }

        /// <summary>
        /// Duration remaining in seconds (-1 for permanent)
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// Update the modifier (for duration tracking)
        /// </summary>
        /// <param name="deltaTime">Time elapsed in seconds</param>
        /// <returns>True if modifier is still active</returns>
        bool Update(float deltaTime);
    }

    /// <summary>
    /// Interface for movement skills
    /// </summary>
    public interface IMovementSkill
    {
        /// <summary>
        /// Skill ID
        /// </summary>
        int SkillId { get; }

        /// <summary>
        /// Whether the skill can be used
        /// </summary>
        bool CanUse(Player player);

        /// <summary>
        /// Use the movement skill
        /// </summary>
        void Use(Player player);

        /// <summary>
        /// Update skill cooldowns
        /// </summary>
        void Update(float deltaTime);
    }
}