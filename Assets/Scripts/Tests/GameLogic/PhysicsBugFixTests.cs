using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;
using System;

namespace MapleClient.GameLogic.Tests
{
    /// <summary>
    /// Tests to verify fixes for known physics bugs:
    /// - Player falling through platforms
    /// - Camera jumping between positions  
    /// - Movement feeling "floaty" or wrong
    /// - Edge cases with platform collisions
    /// </summary>
    [TestFixture]
    public class PhysicsBugFixTests
    {
        private Player player;
        private MapData testMap;
        private PhysicsUpdateManager physicsManager;
        private const float EPSILON = 0.001f;
        
        [SetUp]
        public void Setup()
        {
            player = new Player();
            physicsManager = new PhysicsUpdateManager();
            physicsManager.RegisterPhysicsObject(player);
            
            testMap = new MapData
            {
                MapId = 100000000,
                Platforms = new List<Platform>
                {
                    // Ground platform
                    new Platform { Id = 1, X1 = -1000, Y1 = 0, X2 = 1000, Y2 = 0, Type = PlatformType.Normal },
                    // Small platform (edge case testing)
                    new Platform { Id = 2, X1 = 100, Y1 = 100, X2 = 102, Y2 = 100, Type = PlatformType.Normal },
                    // One-way platform
                    new Platform { Id = 3, X1 = -200, Y1 = 150, X2 = 200, Y2 = 150, Type = PlatformType.OneWay },
                    // Steep slope
                    new Platform { Id = 4, X1 = 300, Y1 = 0, X2 = 350, Y2 = 100, Type = PlatformType.Normal }
                }
            };
        }
        
        #region Platform Collision Bug Fixes
        
        [Test]
        public void NeverFallThroughSolidPlatform()
        {
            // Test that player never falls through solid platforms even at high speeds
            
            // Start high with very fast downward velocity
            player.Position = new Vector2(0, 10f);
            player.Velocity = new Vector2(0, -50f); // Very fast fall
            player.IsGrounded = false;
            
            // Run physics until grounded or timeout
            int frameCount = 0;
            float lowestY = player.Position.Y;
            
            while (!player.IsGrounded && frameCount++ < 200)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                lowestY = Math.Min(lowestY, player.Position.Y);
            }
            
            // Player should have landed on ground platform (Y=0)
            Assert.That(player.IsGrounded, Is.True, "Player should have landed");
            float playerBottom = player.Position.Y - 0.3f; // Half height
            Assert.That(playerBottom, Is.EqualTo(0f).Within(EPSILON), "Player should be exactly on platform");
            Assert.That(lowestY - 0.3f, Is.GreaterThanOrEqualTo(-EPSILON), "Player should never go below platform");
        }
        
        [Test]
        public void SmallPlatformCollision_WorksReliably()
        {
            // Test collision with very small platforms
            
            // Position above small platform
            player.Position = new Vector2(1.01f, 2f); // Above platform at X=100-102, Y=100
            player.Velocity = new Vector2(0, -3f);
            player.IsGrounded = false;
            
            // Fall onto small platform
            while (!player.IsGrounded && player.Position.Y > 0.5f)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
            }
            
            // Should land on small platform
            Assert.That(player.IsGrounded, Is.True);
            float expectedY = 1f + 0.3f; // Platform Y + half player height
            Assert.That(player.Position.Y, Is.EqualTo(expectedY).Within(0.01f));
        }
        
        [Test]
        public void PlatformEdgeStability_NoJitter()
        {
            // Test that player doesn't jitter or fall off platform edges
            
            // Position near platform edge
            player.Position = new Vector2(9.99f, 0.3f); // Near right edge of ground
            player.IsGrounded = true;
            player.Velocity = new Vector2(0.1f, 0); // Slight movement towards edge
            
            // Track position over time
            var positions = new List<float>();
            
            for (int i = 0; i < 60; i++) // 1 second
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                positions.Add(player.Position.Y);
            }
            
            // Y position should remain stable (no jitter)
            float minY = positions[0];
            float maxY = positions[0];
            foreach (float y in positions)
            {
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
            
            float jitter = maxY - minY;
            Assert.That(jitter, Is.LessThan(0.01f), "Y position should not jitter");
            Assert.That(player.IsGrounded, Is.True, "Player should remain grounded");
        }
        
        #endregion
        
        #region Movement Feel Bug Fixes
        
        [Test]
        public void Movement_NotFloaty_SnappyAcceleration()
        {
            // Test that movement acceleration feels snappy, not floaty
            
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            player.Velocity = Vector2.Zero;
            
            // Start moving right
            player.MoveRight(true);
            
            // Track velocity over first 10 frames
            var velocities = new List<float>();
            for (int i = 0; i < 10; i++)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                velocities.Add(player.Velocity.X);
            }
            
            // Acceleration should be consistent and reach near max quickly
            // With 14 units/s² acceleration, should gain ~0.233 units/s per frame
            for (int i = 1; i < velocities.Count; i++)
            {
                float deltaV = velocities[i] - velocities[i-1];
                Assert.That(deltaV, Is.EqualTo(0.233f).Within(0.01f), $"Frame {i} acceleration incorrect");
            }
            
            // Should reach max speed (1.25) in about 5-6 frames
            Assert.That(velocities[5], Is.GreaterThan(1.2f), "Should reach near max speed quickly");
        }
        
        [Test]
        public void Movement_StopsQuickly_NotSlippery()
        {
            // Test that movement stops quickly when input released
            
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            player.Velocity = new Vector2(1.25f, 0); // At max speed
            
            // Stop moving
            player.MoveRight(false);
            
            // Track stopping distance
            float startX = player.Position.X;
            int frameCount = 0;
            
            while (Math.Abs(player.Velocity.X) > 0.01f && frameCount++ < 20)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
            }
            
            float stoppingDistance = player.Position.X - startX;
            
            // Should stop in about 9-10 frames with 8 units/s² friction
            Assert.That(frameCount, Is.LessThan(11), "Should stop quickly");
            Assert.That(stoppingDistance, Is.LessThan(0.2f), "Should not slide far");
            Assert.That(player.Velocity.X, Is.EqualTo(0f).Within(EPSILON), "Should come to complete stop");
        }
        
        [Test]
        public void Jump_FeelsCrisp_NotFloaty()
        {
            // Test that jumps feel crisp and responsive
            
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            
            // Jump
            player.Jump();
            
            // Initial velocity should be exactly jump speed
            Assert.That(player.Velocity.Y, Is.EqualTo(5.55f).Within(EPSILON));
            
            // Track jump arc
            float peakY = player.Position.Y;
            int peakFrame = 0;
            
            for (int frame = 0; frame < 60; frame++)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                
                if (player.Position.Y > peakY)
                {
                    peakY = player.Position.Y;
                    peakFrame = frame;
                }
                
                if (player.IsGrounded) break;
            }
            
            // Peak should be reached quickly (around frame 17)
            Assert.That(peakFrame, Is.EqualTo(17).Within(1), "Jump peak timing incorrect");
            
            // Fall speed should cap at terminal velocity
            Assert.That(player.Velocity.Y, Is.GreaterThanOrEqualTo(-6.7f), "Fall speed should be capped");
        }
        
        #endregion
        
        #region Camera and Visual Stability
        
        [Test] 
        public void PositionUpdates_Smooth_NoCameraJumping()
        {
            // Test that position updates are smooth with no sudden jumps
            
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            
            // Move and jump
            player.MoveRight(true);
            player.Jump();
            
            var positions = new List<Vector2>();
            
            // Track positions for 2 seconds
            for (int i = 0; i < 120; i++)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                positions.Add(player.Position);
                
                // Change direction mid-air
                if (i == 30)
                {
                    player.MoveRight(false);
                    player.MoveLeft(true);
                }
            }
            
            // Check for sudden position jumps
            for (int i = 1; i < positions.Count; i++)
            {
                float deltaX = Math.Abs(positions[i].X - positions[i-1].X);
                float deltaY = Math.Abs(positions[i].Y - positions[i-1].Y);
                
                // Position changes should be small and smooth
                Assert.That(deltaX, Is.LessThan(0.2f), $"X jump at frame {i}");
                Assert.That(deltaY, Is.LessThan(0.3f), $"Y jump at frame {i}");
            }
        }
        
        #endregion
        
        #region Edge Case Fixes
        
        [Test]
        public void CornerCollision_NoGettingStuck()
        {
            // Test that player doesn't get stuck on platform corners
            
            // Position at corner of platform
            player.Position = new Vector2(2f, 1.8f); // Near corner of one-way platform
            player.Velocity = new Vector2(-1f, -1f); // Moving diagonally into corner
            player.IsGrounded = false;
            
            // Run physics
            for (int i = 0; i < 60; i++)
            {
                float prevX = player.Position.X;
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
                
                // Player should keep moving, not get stuck
                if (player.IsGrounded && i > 10)
                {
                    Assert.That(player.Position.X, Is.Not.EqualTo(prevX).Within(0.0001f), 
                        "Player got stuck at corner");
                }
            }
        }
        
        [Test]
        public void SteepSlope_HandledCorrectly()
        {
            // Test movement on steep slopes
            
            // Position on steep slope
            player.Position = new Vector2(3.25f, 1f); // On slope from (300,0) to (350,100)
            player.IsGrounded = true;
            
            // Move up slope
            player.MoveRight(true);
            
            float startX = player.Position.X;
            float startY = player.Position.Y;
            
            // Move for 1 second
            for (int i = 0; i < 60; i++)
            {
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
            }
            
            // Should have moved up the slope
            Assert.That(player.Position.X, Is.GreaterThan(startX));
            Assert.That(player.Position.Y, Is.GreaterThan(startY), "Should move up slope");
            Assert.That(player.IsGrounded, Is.True, "Should stay on slope");
        }
        
        [Test]
        public void RapidDirectionChanges_Stable()
        {
            // Test that rapid direction changes don't cause issues
            
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            
            // Rapidly change directions
            for (int i = 0; i < 60; i++)
            {
                if (i % 4 == 0)
                {
                    player.MoveRight(true);
                    player.MoveLeft(false);
                }
                else if (i % 4 == 2)
                {
                    player.MoveRight(false);
                    player.MoveLeft(true);
                }
                
                physicsManager.Update(MaplePhysics.FIXED_TIMESTEP, testMap);
            }
            
            // Physics should remain stable
            Assert.That(float.IsNaN(player.Position.X), Is.False);
            Assert.That(float.IsNaN(player.Velocity.X), Is.False);
            Assert.That(player.IsGrounded, Is.True);
        }
        
        #endregion
    }
}