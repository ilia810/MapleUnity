using System;
using System.Collections.Generic;
using System.Diagnostics;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.GameLogic.Core
{
    /// <summary>
    /// Manages physics updates at a fixed 60 FPS timestep to match MapleStory v83.
    /// Ensures deterministic, frame-perfect physics calculations.
    /// </summary>
    public class PhysicsUpdateManager
    {
        // Fixed timestep for 60 FPS (1/60 = 0.01667 seconds)
        public const float FIXED_TIMESTEP = 1f / 60f;
        public const int TARGET_FPS = 60;
        
        private readonly Dictionary<int, IPhysicsObject> physicsObjects;
        private readonly List<IPhysicsObject> activeObjects;
        private readonly Stopwatch frameTimer;
        
        private int nextPhysicsId = 1;
        private float accumulator = 0f;
        private long totalFrames = 0;
        private long totalPhysicsSteps = 0;
        private float currentFrameTime = 0f;
        private float averageFrameTime = 0f;
        
        // Debug metrics
        public long TotalFrames => totalFrames;
        public long TotalPhysicsSteps => totalPhysicsSteps;
        public float CurrentFrameTime => currentFrameTime;
        public float AverageFrameTime => averageFrameTime;
        public int ActiveObjectCount => activeObjects.Count;
        public float Accumulator => accumulator;
        
        // Events
        public event Action<long> PhysicsStepCompleted;
        public event Action<float> FrameTimeExceeded;
        public event Action<float, int> FrameCompleted;
        
        public PhysicsUpdateManager()
        {
            physicsObjects = new Dictionary<int, IPhysicsObject>();
            activeObjects = new List<IPhysicsObject>();
            frameTimer = new Stopwatch();
        }
        
        /// <summary>
        /// Register a physics object with the system
        /// </summary>
        public int RegisterPhysicsObject(IPhysicsObject physicsObject)
        {
            if (physicsObject == null)
                throw new ArgumentNullException(nameof(physicsObject));
            
            int id = nextPhysicsId++;
            physicsObjects[id] = physicsObject;
            
            if (physicsObject.IsPhysicsActive)
            {
                activeObjects.Add(physicsObject);
            }
            
            return id;
        }
        
        /// <summary>
        /// Unregister a physics object from the system
        /// </summary>
        public void UnregisterPhysicsObject(int physicsId)
        {
            if (physicsObjects.TryGetValue(physicsId, out var physicsObject))
            {
                physicsObjects.Remove(physicsId);
                activeObjects.Remove(physicsObject);
            }
        }
        
        /// <summary>
        /// Update physics object active state
        /// </summary>
        public void SetPhysicsObjectActive(int physicsId, bool active)
        {
            if (physicsObjects.TryGetValue(physicsId, out var physicsObject))
            {
                if (active && !activeObjects.Contains(physicsObject))
                {
                    activeObjects.Add(physicsObject);
                }
                else if (!active)
                {
                    activeObjects.Remove(physicsObject);
                }
            }
        }
        
        /// <summary>
        /// Process physics updates using fixed timestep with accumulator pattern.
        /// This ensures consistent 60 FPS physics regardless of rendering framerate.
        /// </summary>
        /// <param name="deltaTime">Time since last update (from Unity or game loop)</param>
        /// <param name="mapData">Current map data for collision detection</param>
        public void Update(float deltaTime, MapData mapData)
        {
            totalFrames++;
            frameTimer.Restart();
            
            // Clamp deltaTime to prevent spiral of death
            deltaTime = Math.Min(deltaTime, 0.25f); // Max 250ms per frame
            
            // Add to accumulator
            accumulator += deltaTime;
            
            // Process fixed timesteps
            int stepsThisFrame = 0;
            while (accumulator >= FIXED_TIMESTEP)
            {
                // Update all active physics objects
                UpdatePhysicsStep(mapData);
                
                accumulator -= FIXED_TIMESTEP;
                totalPhysicsSteps++;
                stepsThisFrame++;
                
                // Prevent infinite loops - max 4 physics steps per frame
                if (stepsThisFrame >= 4)
                {
                    accumulator = 0f;
                    break;
                }
            }
            
            // Update timing metrics
            frameTimer.Stop();
            currentFrameTime = (float)frameTimer.Elapsed.TotalSeconds;
            
            // Update rolling average (last 60 frames)
            averageFrameTime = (averageFrameTime * 59f + currentFrameTime) / 60f;
            
            // Warn if frame time exceeds target
            if (currentFrameTime > FIXED_TIMESTEP * 1.5f)
            {
                FrameTimeExceeded?.Invoke(currentFrameTime);
            }
            
            // Notify frame completion
            FrameCompleted?.Invoke(deltaTime, stepsThisFrame);
        }
        
        /// <summary>
        /// Perform one physics step at fixed timestep
        /// </summary>
        private void UpdatePhysicsStep(MapData mapData)
        {
            // Create a copy of active objects to avoid modification during iteration
            var objectsToUpdate = new List<IPhysicsObject>(activeObjects);
            
            foreach (var physicsObject in objectsToUpdate)
            {
                if (physicsObject.IsPhysicsActive)
                {
                    physicsObject.UpdatePhysics(FIXED_TIMESTEP, mapData);
                }
            }
            
            PhysicsStepCompleted?.Invoke(totalPhysicsSteps);
        }
        
        /// <summary>
        /// Get interpolation factor for smooth rendering between physics frames
        /// </summary>
        /// <returns>Value between 0 and 1 representing position between physics frames</returns>
        public float GetInterpolationFactor()
        {
            return accumulator / FIXED_TIMESTEP;
        }
        
        /// <summary>
        /// Reset the physics system (useful for testing or scene changes)
        /// </summary>
        public void Reset()
        {
            physicsObjects.Clear();
            activeObjects.Clear();
            accumulator = 0f;
            totalFrames = 0;
            totalPhysicsSteps = 0;
            currentFrameTime = 0f;
            averageFrameTime = 0f;
            nextPhysicsId = 1;
        }
        
        /// <summary>
        /// Get debug statistics about the physics system
        /// </summary>
        public PhysicsDebugStats GetDebugStats()
        {
            return new PhysicsDebugStats
            {
                TotalFrames = totalFrames,
                TotalPhysicsSteps = totalPhysicsSteps,
                CurrentFrameTime = currentFrameTime,
                AverageFrameTime = averageFrameTime,
                ActiveObjectCount = activeObjects.Count,
                TotalObjectCount = physicsObjects.Count,
                Accumulator = accumulator,
                StepsPerSecond = totalFrames > 0 ? (float)totalPhysicsSteps / (totalFrames * averageFrameTime) : 0f
            };
        }
    }
    
    /// <summary>
    /// Debug statistics for physics system
    /// </summary>
    public struct PhysicsDebugStats
    {
        public long TotalFrames;
        public long TotalPhysicsSteps;
        public float CurrentFrameTime;
        public float AverageFrameTime;
        public int ActiveObjectCount;
        public int TotalObjectCount;
        public float Accumulator;
        public float StepsPerSecond;
    }
}