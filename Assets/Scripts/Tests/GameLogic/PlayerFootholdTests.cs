using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;

namespace MapleClient.Tests.GameLogic
{
    [TestFixture]
    public class PlayerFootholdTests
    {
        private Player player;
        private MapData mapData;
        private const float DELTA_TIME = 0.016f; // 60 FPS

        [SetUp]
        public void SetUp()
        {
            player = new Player();
            mapData = new MapData
            {
                MapId = 100000000,
                Name = "Test Map",
                Width = 2000,
                Height = 1000,
                Platforms = new List<Platform>()
            };
        }

        [Test]
        public void Player_FallsWithGravity_WhenNotGrounded()
        {
            // Arrange
            player.Position = new Vector2(500, 500);
            player.IsGrounded = false;
            float initialY = player.Position.Y;

            // Act
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert
            Assert.Less(player.Position.Y, initialY, "Player should fall down (Y decreases)");
            Assert.Less(player.Velocity.Y, 0, "Velocity should be negative (falling)");
        }

        [Test]
        public void Player_LandsOnPlatform_WhenFalling()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 300,
                X2 = 600,
                Y2 = 300,
                Type = PlatformType.Normal
            };
            mapData.Platforms.Add(platform);

            player.Position = new Vector2(500, 350); // Above platform
            player.Velocity = new Vector2(0, -100); // Falling
            player.IsGrounded = false;

            // Act - simulate falling onto platform
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.IsGrounded) break;
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should be grounded on platform");
            Assert.AreEqual(0, player.Velocity.Y, "Vertical velocity should be zero when landed");
            // Player bottom should be on platform (accounting for player height)
            Assert.AreEqual(300 + 30, player.Position.Y, 5); // 30 is half player height (0.6 * 100 / 2)
        }

        [Test]
        public void Player_CanWalkOnSlope()
        {
            // Arrange
            var slopedPlatform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 300,
                X2 = 600,
                Y2 = 400, // Slopes down from left to right
                Type = PlatformType.Normal
            };
            mapData.Platforms.Add(slopedPlatform);

            player.Position = new Vector2(450, 330);
            player.IsGrounded = false;

            // Let player fall to platform
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.IsGrounded) break;
            }

            // Act - Move right on slope
            player.MoveRight(true);
            var startY = player.Position.Y;
            
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should stay grounded on slope");
            Assert.Greater(player.Position.X, 450, "Player should have moved right");
            // Y position should change as player moves on slope
            Assert.Greater(player.Position.Y, startY, "Player Y should increase going down slope");
        }

        [Test]
        public void Player_IgnoresOneWayPlatformWhenJumpingUp()
        {
            // Arrange
            var oneWayPlatform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 400,
                X2 = 600,
                Y2 = 400,
                Type = PlatformType.OneWay
            };
            mapData.Platforms.Add(oneWayPlatform);

            player.Position = new Vector2(500, 450); // Below platform
            player.IsGrounded = true;

            // Act - Jump up through platform
            player.Jump();
            Assert.IsTrue(player.IsJumping);

            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.Position.Y < 350) break; // Jumped above platform
            }

            // Assert
            Assert.Less(player.Position.Y, 400, "Player should have jumped through one-way platform");
        }

        [Test]
        public void Player_LandsOnOneWayPlatformWhenFalling()
        {
            // Arrange
            var oneWayPlatform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 400,
                X2 = 600,
                Y2 = 400,
                Type = PlatformType.OneWay
            };
            mapData.Platforms.Add(oneWayPlatform);

            player.Position = new Vector2(500, 350); // Above platform
            player.Velocity = new Vector2(0, -50); // Falling
            player.IsGrounded = false;

            // Act
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.IsGrounded) break;
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should land on one-way platform when falling");
        }

        [Test]
        public void Player_FallsOffPlatformEdge()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 300,
                X2 = 600,
                Y2 = 300,
                Type = PlatformType.Normal
            };
            mapData.Platforms.Add(platform);

            // Start on platform
            player.Position = new Vector2(590, 330);
            player.IsGrounded = true;

            // Act - Walk off edge
            player.MoveRight(true);
            for (int i = 0; i < 30; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
            }

            // Assert
            Assert.IsFalse(player.IsGrounded, "Player should fall off platform edge");
            Assert.Less(player.Velocity.Y, 0, "Player should be falling");
            Assert.Greater(player.Position.X, 600, "Player should be past platform edge");
        }

        [Test]
        public void Player_StaysGroundedOnConnectedPlatforms()
        {
            // Arrange - Two connected platforms
            var platform1 = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 300,
                X2 = 600,
                Y2 = 300,
                Type = PlatformType.Normal
            };
            var platform2 = new Platform
            {
                Id = 2,
                X1 = 600,
                Y1 = 300,
                X2 = 800,
                Y2 = 300,
                Type = PlatformType.Normal
            };
            mapData.Platforms.Add(platform1);
            mapData.Platforms.Add(platform2);

            player.Position = new Vector2(590, 330);
            player.IsGrounded = true;

            // Act - Walk across platform boundary
            player.MoveRight(true);
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should stay grounded on connected platforms");
            Assert.Greater(player.Position.X, 600, "Player should have crossed to second platform");
        }

        [Test]
        public void Player_CorrectlyHandlesMultiplePlatforms()
        {
            // Arrange - Multiple platforms at different heights
            mapData.Platforms.Add(new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 500,
                X2 = 600,
                Y2 = 500,
                Type = PlatformType.Normal
            });
            mapData.Platforms.Add(new Platform
            {
                Id = 2,
                X1 = 400,
                Y1 = 300,
                X2 = 600,
                Y2 = 300,
                Type = PlatformType.Normal
            });

            player.Position = new Vector2(500, 250); // Above highest platform
            player.IsGrounded = false;

            // Act - Fall
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.IsGrounded) break;
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should land on platform");
            // Should land on the higher platform (300), not fall through to lower one (500)
            Assert.AreEqual(300 + 30, player.Position.Y, 5); // 30 is half player height
        }
    }
}