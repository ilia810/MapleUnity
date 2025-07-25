using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class PlayerTests
    {
        private Player player;
        private MapData testMap;

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
        }

        [Test]
        public void Player_InitialPosition_IsZero()
        {
            Assert.That(player.Position.X, Is.EqualTo(0));
            Assert.That(player.Position.Y, Is.EqualTo(0));
        }

        [Test]
        public void MoveLeft_SetsNegativeHorizontalVelocity()
        {
            // Act
            player.MoveLeft(true);

            // Assert
            Assert.That(player.Velocity.X, Is.LessThan(0));
        }

        [Test]
        public void MoveRight_SetsPositiveHorizontalVelocity()
        {
            // Act
            player.MoveRight(true);

            // Assert
            Assert.That(player.Velocity.X, Is.GreaterThan(0));
        }

        [Test]
        public void StopMoving_SetsHorizontalVelocityToZero()
        {
            // Arrange
            player.MoveRight(true);

            // Act
            player.MoveRight(false);

            // Assert
            Assert.That(player.Velocity.X, Is.EqualTo(0));
        }

        [Test]
        public void Jump_WhenOnGround_SetsPositiveVerticalVelocity()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;

            // Act
            player.Jump();

            // Assert
            Assert.That(player.Velocity.Y, Is.GreaterThan(0));
            Assert.That(player.IsJumping, Is.True);
        }

        [Test]
        public void Jump_WhenInAir_DoesNotJump()
        {
            // Arrange
            player.Position = new Vector2(0, 100);
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, 0);

            // Act
            player.Jump();

            // Assert
            Assert.That(player.Velocity.Y, Is.EqualTo(0));
        }

        [Test]
        public void UpdatePhysics_AppliesGravity()
        {
            // Arrange
            player.Position = new Vector2(0, 100);
            player.IsGrounded = false;
            var initialVelocityY = player.Velocity.Y;

            // Act
            player.UpdatePhysics(0.1f, testMap);

            // Assert
            Assert.That(player.Velocity.Y, Is.LessThan(initialVelocityY));
        }

        [Test]
        public void UpdatePhysics_WhenFalling_LandsOnPlatform()
        {
            // Arrange
            player.Position = new Vector2(0, 100);
            player.Velocity = new Vector2(0, -50);
            player.IsGrounded = false;

            // Act - simulate multiple physics updates
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(0.02f, testMap);
            }

            // Assert
            Assert.That(player.Position.Y, Is.EqualTo(0).Within(0.1f));
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.Velocity.Y, Is.EqualTo(0));
        }

        [Test]
        public void UpdatePhysics_WithHorizontalMovement_UpdatesPosition()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            player.MoveRight(true);
            var initialX = player.Position.X;

            // Act
            player.UpdatePhysics(0.1f, testMap);

            // Assert
            Assert.That(player.Position.X, Is.GreaterThan(initialX));
        }
    }
}