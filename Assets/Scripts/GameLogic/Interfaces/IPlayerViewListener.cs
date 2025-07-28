using MapleClient.GameLogic.Core;

namespace MapleClient.GameLogic.Interfaces
{
    /// <summary>
    /// Interface for objects that want to listen to player state changes.
    /// This provides a clean separation between GameLogic and GameView layers.
    /// </summary>
    public interface IPlayerViewListener
    {
        /// <summary>
        /// Called when the player's position changes
        /// </summary>
        void OnPositionChanged(Vector2 position);
        
        /// <summary>
        /// Called when the player's state changes (Standing, Walking, Jumping, etc.)
        /// </summary>
        void OnStateChanged(PlayerState state);
        
        /// <summary>
        /// Called when the player's velocity changes
        /// </summary>
        void OnVelocityChanged(Vector2 velocity);
        
        /// <summary>
        /// Called when the player's grounded state changes
        /// </summary>
        void OnGroundedStateChanged(bool isGrounded);
        
        /// <summary>
        /// Called when an animation event occurs
        /// </summary>
        void OnAnimationEvent(PlayerAnimationEvent animEvent);
        
        /// <summary>
        /// Called when movement modifiers change
        /// </summary>
        void OnMovementModifiersChanged(System.Collections.Generic.List<IMovementModifier> modifiers);
    }
    
    /// <summary>
    /// Animation events that can be triggered by player actions
    /// </summary>
    public enum PlayerAnimationEvent
    {
        Jump,
        Land,
        Attack,
        StartWalk,
        StopWalk,
        StartClimb,
        StopClimb,
        Crouch,
        StandUp
    }
}