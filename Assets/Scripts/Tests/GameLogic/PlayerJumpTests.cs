using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;

namespace MapleClient.Tests.GameLogic
{
    public class PlayerJumpTests
    {
        private Player player;
        private MapData testMap;

        [SetUp]
        public void Setup()
        {
            player = new Player();
            
            // Create a simple test map with a ground platform
            testMap = new MapData
            {
                MapId = 1,
                Name = "Test Map",
                Platforms = new System.Collections.Generic.List<Platform>
                {
                    new Platform 
                    { 
                        Id = 1, 
                        X1 = -1000, 
                        Y1 = 0, 
                        X2 = 1000, 
                        Y2 = 0, 
                        Type = PlatformType.Normal 
                    }
                }
            };
        }

        [Test]
        public void Jump_WhenGrounded_SetsPositiveVelocity()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(player.Velocity.Y, Is.GreaterThan(0));
            Assert.That(player.IsJumping, Is.True);
            Assert.That(player.IsGrounded, Is.False);
        }

        [Test]
        public void Jump_WhenNotGrounded_DoesNotJump()
        {
            // Arrange
            player.Position = new Vector2(0, 100);
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, -100); // Falling
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(player.Velocity.Y, Is.EqualTo(-100)); // Velocity unchanged
            Assert.That(player.IsJumping, Is.False);
        }

        [Test]
        public void Jump_WhenCrouching_DoesNotJump()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            player.Crouch(true);
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(player.Velocity.Y, Is.EqualTo(0));
            Assert.That(player.IsJumping, Is.False);
            Assert.That(player.IsGrounded, Is.True);
        }

        [Test]
        public void Jump_StateChangesToJumping()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
        }

        [Test]
        public void Jump_ReachesExpectedHeight()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            float startY = player.Position.Y;
            
            // Act - Jump and simulate physics until apex
            player.Jump();
            float maxHeight = startY;
            float deltaTime = 0.016f; // ~60 FPS
            
            for (int i = 0; i < 100; i++) // Simulate up to 100 frames
            {
                player.UpdatePhysics(deltaTime, testMap);
                if (player.Position.Y > maxHeight)
                {
                    maxHeight = player.Position.Y;
                }
                if (player.Velocity.Y <= 0) // Started falling
                {
                    break;
                }
            }
            
            // Assert - Jump should reach a reasonable height
            float jumpHeight = maxHeight - startY;
            Assert.That(jumpHeight, Is.GreaterThan(100f)); // At least 100 units high
            Assert.That(jumpHeight, Is.LessThan(200f)); // But not too high
        }

        [Test]
        public void Jump_LandsBackOnGround()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act - Jump and simulate full arc
            player.Jump();
            float deltaTime = 0.016f;
            
            for (int i = 0; i < 200; i++) // Simulate up to 200 frames
            {
                player.UpdatePhysics(deltaTime, testMap);
                if (player.IsGrounded && i > 10) // Landed after jumping
                {
                    break;
                }
            }
            
            // Assert
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.Position.Y, Is.EqualTo(0).Within(0.1f));
            Assert.That(player.IsJumping, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
        }

        [Test]
        public void Jump_FromLadder_Works()
        {
            // Arrange
            var ladder = new LadderInfo { X = 0, Y1 = 0, Y2 = 200 };
            player.Position = new Vector2(0, 100);
            player.StartClimbing(ladder);
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
            Assert.That(player.Velocity.Y, Is.GreaterThan(0));
        }

        [Test]
        public void Jump_MultipleCallsWhileAirborne_OnlyJumpsOnce()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            float firstJumpVelocity = player.Velocity.Y;
            
            // Try to jump again while in air
            player.Jump();
            player.Jump();
            
            // Assert
            Assert.That(player.Velocity.Y, Is.EqualTo(firstJumpVelocity));
        }

        [Test]
        public void Jump_RaisesLandedEvent()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            bool landedEventRaised = false;
            player.Landed += () => landedEventRaised = true;
            
            // Act - Jump and land
            player.Jump();
            float deltaTime = 0.016f;
            
            for (int i = 0; i < 200; i++)
            {
                player.UpdatePhysics(deltaTime, testMap);
                if (player.IsGrounded && i > 10)
                {
                    break;
                }
            }
            
            // Assert
            Assert.That(landedEventRaised, Is.True);
        }

        [Test]
        public void Jump_VelocityAffectedByGravity()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            float initialVelocity = player.Velocity.Y;
            player.UpdatePhysics(0.1f, testMap);
            float velocityAfterGravity = player.Velocity.Y;
            
            // Assert
            Assert.That(velocityAfterGravity, Is.LessThan(initialVelocity));
        }
    }
}