using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class PlayerMovementPhysicsTests
    {
        private Player player;
        private MapData testMap;
        private const float FIXED_TIMESTEP = 1f / 60f; // 60 FPS fixed timestep
        private const float EPSILON = 0.001f; // For float comparisons

        [SetUp]
        public void Setup()
        {
            player = new Player();
            testMap = new MapData
            {
                MapId = 100000000,
                Platforms = new List<Platform>
                {
                    new Platform { Id = 1, X1 = -500, Y1 = 0, X2 = 500, Y2 = 0, Type = PlatformType.Normal }
                }
            };
            
            // Place player on ground
            player.Position = new Vector2(0, 0.3f); // Player height is 0.6, so center is 0.3 above ground
            player.IsGrounded = true;
        }

        #region Horizontal Movement Tests

        [Test]
        public void HorizontalMovement_InitialWalkSpeed_Is125UnitsPerSecond()
        {
            // Arrange
            player.Speed = 100; // 100% speed stat

            // Act
            player.MoveRight(true);

            // Assert - initial walk speed should be 1.25 units/s (125 pixels/s)
            Assert.That(player.Velocity.X, Is.EqualTo(1.25f).Within(EPSILON));
        }

        [Test]
        public void HorizontalMovement_AccelerationToMaxSpeed_Takes125Milliseconds()
        {
            // Arrange
            player.Speed = 100;
            player.Velocity = new Vector2(0, 0);
            
            // Act - apply acceleration over time
            player.MoveRight(true);
            float elapsedTime = 0;
            while (elapsedTime < 0.125f) // 125ms
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
                elapsedTime += FIXED_TIMESTEP;
            }

            // Assert - should reach max walk speed (1.25 units/s)
            Assert.That(player.Velocity.X, Is.EqualTo(1.25f).Within(EPSILON));
        }

        [Test]
        public void HorizontalMovement_WalkAcceleration_Is1000UnitsPerSecondSquared()
        {
            // Arrange
            player.Speed = 100;
            player.Velocity = new Vector2(0, 0);
            
            // Act - apply one frame of acceleration
            player.MoveRight(true);
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - velocity should increase by acceleration * deltaTime
            // 1000 units/s² * (1/60)s = 16.667 units/s
            float expectedVelocity = 10f * FIXED_TIMESTEP; // Using 1000 units/s² acceleration
            Assert.That(player.Velocity.X, Is.EqualTo(expectedVelocity).Within(EPSILON));
        }

        [Test]
        public void HorizontalMovement_StoppingFriction_Is1000UnitsPerSecondSquared()
        {
            // Arrange
            player.Speed = 100;
            player.Velocity = new Vector2(1.25f, 0); // Max walk speed
            
            // Act - release movement key
            player.MoveRight(false);
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - velocity should decrease by friction * deltaTime
            // 1000 units/s² * (1/60)s = 16.667 units/s
            float expectedVelocity = 1.25f - (10f * FIXED_TIMESTEP);
            Assert.That(player.Velocity.X, Is.EqualTo(expectedVelocity).Within(EPSILON));
        }

        [Test]
        public void HorizontalMovement_FullStopTime_Is125Milliseconds()
        {
            // Arrange
            player.Speed = 100;
            player.Velocity = new Vector2(1.25f, 0); // Max walk speed
            
            // Act - release movement and apply friction until stopped
            player.MoveRight(false);
            float elapsedTime = 0;
            while (player.Velocity.X > 0 && elapsedTime < 0.2f) // Safety limit
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
                elapsedTime += FIXED_TIMESTEP;
            }

            // Assert - should stop in approximately 125ms
            Assert.That(player.Velocity.X, Is.EqualTo(0).Within(EPSILON));
            Assert.That(elapsedTime, Is.EqualTo(0.125f).Within(0.02f)); // Allow 20ms tolerance
        }

        [Test]
        public void HorizontalMovement_AirControl_Is80PercentOfGroundControl()
        {
            // Arrange
            player.Speed = 100;
            player.IsGrounded = false; // In air
            player.Velocity = new Vector2(0, 0);
            
            // Act - apply acceleration in air
            player.MoveRight(true);
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - acceleration should be 80% of ground acceleration
            float expectedVelocity = 8f * FIXED_TIMESTEP; // 800 units/s² (80% of 1000)
            Assert.That(player.Velocity.X, Is.EqualTo(expectedVelocity).Within(EPSILON));
        }

        [Test]
        public void HorizontalMovement_NoAirFriction_MaintainsMomentum()
        {
            // Arrange
            player.Speed = 100;
            player.IsGrounded = false; // In air
            player.Velocity = new Vector2(1.25f, -2f); // Moving right while falling
            
            // Act - release movement key while in air
            player.MoveRight(false);
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - horizontal velocity should remain unchanged (no air friction)
            Assert.That(player.Velocity.X, Is.EqualTo(1.25f).Within(EPSILON));
        }

        #endregion

        #region Jump Mechanics Tests

        [Test]
        public void Jump_InitialVelocity_Is555UnitsPerSecond()
        {
            // Arrange
            player.JumpPower = 100; // 100% jump stat
            player.IsGrounded = true;
            
            // Act
            player.Jump();

            // Assert
            Assert.That(player.Velocity.Y, Is.EqualTo(5.55f).Within(EPSILON));
        }

        [Test]
        public void Jump_RequiresKeyReleaseForSubsequentJumps()
        {
            // Arrange
            player.JumpPower = 100;
            player.IsGrounded = true;
            
            // Act - first jump
            player.Jump();
            float firstJumpVelocity = player.Velocity.Y;
            
            // Try to jump again without releasing (simulate holding key)
            player.Jump();
            float secondAttemptVelocity = player.Velocity.Y;

            // Assert - second jump should not happen
            Assert.That(firstJumpVelocity, Is.EqualTo(5.55f).Within(EPSILON));
            Assert.That(secondAttemptVelocity, Is.EqualTo(firstJumpVelocity)); // No change
        }

        [Test]
        public void Jump_NoVariableHeight_ConstantInitialVelocity()
        {
            // Test that jump height doesn't vary based on how long key is held
            // This is tested by the fact that Jump() sets a fixed velocity
            // rather than applying a force over time
            
            // Arrange
            player.JumpPower = 100;
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            float initialVelocity = player.Velocity.Y;
            
            // Simulate holding jump for several frames
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
            }

            // Assert - initial velocity was fixed, gravity reduces it naturally
            Assert.That(initialVelocity, Is.EqualTo(5.55f).Within(EPSILON));
            Assert.That(player.Velocity.Y, Is.LessThan(initialVelocity)); // Gravity applied
        }

        [Test]
        public void Jump_CanJumpImmediatelyUponLanding()
        {
            // Arrange - player falling and about to land
            player.Position = new Vector2(0, 0.5f);
            player.Velocity = new Vector2(0, -2f);
            player.IsGrounded = false;
            
            // Act - simulate landing
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);
            
            // Now try to jump immediately
            if (player.IsGrounded)
            {
                player.Jump();
            }

            // Assert - should be able to jump
            Assert.That(player.IsGrounded, Is.True, "Player should have landed");
            Assert.That(player.Velocity.Y, Is.EqualTo(5.55f).Within(EPSILON));
        }

        #endregion

        #region Gravity System Tests

        [Test]
        public void Gravity_AccelerationRate_Is2000UnitsPerSecondSquared()
        {
            // Arrange
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, 0);
            
            // Act - apply one frame of gravity
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - velocity should decrease by gravity * deltaTime
            // 2000 units/s² * (1/60)s = 33.333 units/s
            float expectedVelocity = -20f * FIXED_TIMESTEP;
            Assert.That(player.Velocity.Y, Is.EqualTo(expectedVelocity).Within(EPSILON));
        }

        [Test]
        public void Gravity_TerminalVelocity_Is750UnitsPerSecond()
        {
            // Arrange
            player.IsGrounded = false;
            player.Position = new Vector2(0, 100); // High up
            player.Velocity = new Vector2(0, 0);
            
            // Act - fall for a long time
            for (int i = 0; i < 120; i++) // 2 seconds
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
            }

            // Assert - should reach terminal velocity
            Assert.That(player.Velocity.Y, Is.EqualTo(-7.5f).Within(EPSILON));
        }

        [Test]
        public void Gravity_CrispPhysics_NoFloatyFeeling()
        {
            // Test that gravity creates expected fall curve
            // After 0.5 seconds of falling, should have fallen specific distance
            
            // Arrange
            player.IsGrounded = false;
            player.Position = new Vector2(0, 10);
            player.Velocity = new Vector2(0, 0);
            float startY = player.Position.Y;
            
            // Act - fall for 0.5 seconds
            for (int i = 0; i < 30; i++) // 30 frames = 0.5 seconds
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
            }
            
            // Calculate expected distance using physics formula
            // d = v₀t + ½at² (but capped by terminal velocity)
            // For first part before terminal velocity: d = 0 + ½(20)(t²)
            float distanceFallen = startY - player.Position.Y;
            
            // Assert - should have fallen expected distance
            Assert.That(distanceFallen, Is.GreaterThan(4f)); // Significant fall distance
            Assert.That(distanceFallen, Is.LessThan(6f)); // But not excessive
        }

        #endregion

        #region State Machine Tests

        [Test]
        public void StateMachine_IdleState_WhenNotMoving()
        {
            // Arrange
            player.IsGrounded = true;
            player.Velocity = new Vector2(0, 0);
            
            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
        }

        [Test]
        public void StateMachine_WalkingState_WhenMovingHorizontally()
        {
            // Arrange
            player.IsGrounded = true;
            
            // Act
            player.MoveRight(true);

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Walking));
        }

        [Test]
        public void StateMachine_JumpingState_WhenInAir()
        {
            // Arrange
            player.IsGrounded = true;
            
            // Act
            player.Jump();

            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
        }

        [Test]
        public void StateMachine_FallingState_WhenMovingDownward()
        {
            // Note: MapleStory v83 uses Jumping state for both ascending and descending
            // This test verifies we maintain Jumping state while falling
            
            // Arrange - make player jump first to get into jumping state
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            player.Jump();
            
            // Now simulate falling
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, -2f);
            
            // Act
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert - should still be in Jumping state
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
        }

        [Test]
        public void StateMachine_TransitionToStanding_OnLanding()
        {
            // Arrange - make player jump first
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            player.Jump();
            
            // Now position for landing
            player.Position = new Vector2(0, 0.4f);
            player.Velocity = new Vector2(0, -2f);
            player.IsGrounded = false;
            
            // Act - land on platform
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
        }

        [Test]
        public void StateMachine_TransitionToWalking_WhenMovingAfterLanding()
        {
            // Arrange - make player jump while moving
            player.Position = new Vector2(0, 0.3f);
            player.IsGrounded = true;
            player.MoveRight(true);
            player.Jump();
            
            // Now position for landing
            player.Position = new Vector2(0, 0.4f);
            player.Velocity = new Vector2(1.25f, -2f);
            player.IsGrounded = false;
            
            // Act - land on platform
            player.UpdatePhysics(FIXED_TIMESTEP, testMap);

            // Assert
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerState.Walking));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_JumpArc_MatchesMapleStoryV83()
        {
            // Test complete jump arc matches expected behavior
            // Jump should reach peak in ~0.27 seconds, total air time ~0.82 seconds
            
            // Arrange
            player.JumpPower = 100;
            player.IsGrounded = true;
            float startY = player.Position.Y;
            
            // Act - perform jump
            player.Jump();
            
            float peakY = startY;
            float peakTime = 0;
            float airTime = 0;
            
            // Simulate until landing
            while (!player.IsGrounded && airTime < 2f) // Safety limit
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
                airTime += FIXED_TIMESTEP;
                
                if (player.Position.Y > peakY)
                {
                    peakY = player.Position.Y;
                    peakTime = airTime;
                }
            }
            
            // Assert
            Assert.That(peakTime, Is.EqualTo(0.277f).Within(0.02f)); // ~0.27s to peak
            Assert.That(airTime, Is.EqualTo(0.82f).Within(0.05f)); // ~0.82s total air time
            float jumpHeight = peakY - startY;
            Assert.That(jumpHeight, Is.EqualTo(0.77f).Within(0.05f)); // Expected jump height
        }

        [Test]
        public void Integration_MovementDistance_MatchesExpectedSpeed()
        {
            // Test that movement over time matches expected distance
            
            // Arrange
            player.Speed = 100;
            player.IsGrounded = true;
            float startX = player.Position.X;
            
            // Act - move right for 1 second
            player.MoveRight(true);
            for (int i = 0; i < 60; i++) // 60 frames = 1 second
            {
                player.UpdatePhysics(FIXED_TIMESTEP, testMap);
            }
            
            float distance = player.Position.X - startX;
            
            // Assert - should move ~1.25 units in 1 second (accounting for acceleration time)
            Assert.That(distance, Is.EqualTo(1.19f).Within(0.05f)); // Slightly less due to acceleration
        }

        #endregion
    }
}