using MapleClient.GameLogic.Core;

namespace MapleClient.GameLogic.Interfaces
{
    /// <summary>
    /// Interface for all objects that participate in the physics system.
    /// All physics calculations are performed at a fixed 60 FPS timestep.
    /// </summary>
    public interface IPhysicsObject
    {
        /// <summary>
        /// Unique identifier for this physics object
        /// </summary>
        int PhysicsId { get; }
        
        /// <summary>
        /// Current position in game world (units)
        /// </summary>
        Vector2 Position { get; set; }
        
        /// <summary>
        /// Current velocity (units per second)
        /// </summary>
        Vector2 Velocity { get; set; }
        
        /// <summary>
        /// Whether this object is affected by gravity
        /// </summary>
        bool UseGravity { get; }
        
        /// <summary>
        /// Whether this object is currently active in physics simulation
        /// </summary>
        bool IsPhysicsActive { get; }
        
        /// <summary>
        /// Update physics for exactly one fixed timestep (1/60 second)
        /// This method should be deterministic and frame-perfect.
        /// </summary>
        /// <param name="fixedDeltaTime">The fixed timestep (always 0.01667 for 60 FPS)</param>
        /// <param name="mapData">Current map data for collision detection</param>
        void UpdatePhysics(float fixedDeltaTime, MapData mapData);
        
        /// <summary>
        /// Called when physics object collides with terrain
        /// </summary>
        /// <param name="collisionPoint">Point of collision</param>
        /// <param name="collisionNormal">Normal vector at collision point</param>
        void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal);
    }
}