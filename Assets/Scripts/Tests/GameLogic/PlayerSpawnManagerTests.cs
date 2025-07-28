using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;

namespace MapleClient.Tests.GameLogic
{
    [TestFixture]
    public class PlayerSpawnManagerTests
    {
        private PlayerSpawnManager spawnManager;
        private MapData testMapData;

        [SetUp]
        public void SetUp()
        {
            spawnManager = new PlayerSpawnManager();
            testMapData = new MapData
            {
                MapId = 100000000,
                Name = "Henesys",
                Width = 3000,
                Height = 1000,
                Portals = new List<Portal>(),
                Platforms = new List<Platform>()
            };
        }

        [Test]
        public void FindSpawnPoint_WithSpawnPortal_ReturnsPortalPosition()
        {
            // Arrange
            var spawnPortal = new Portal
            {
                Id = 1,
                Name = "sp",
                X = 500,
                Y = 300,
                Type = PortalType.Spawn
            };
            testMapData.Portals.Add(spawnPortal);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            Assert.AreEqual(500, spawnPoint.X);
            Assert.AreEqual(300, spawnPoint.Y);
        }

        [Test]
        public void FindSpawnPoint_WithoutSpawnPortal_ReturnsDefaultSpawn()
        {
            // Arrange
            // Add a platform at the center
            var platform = new Platform
            {
                Id = 1,
                X1 = 1400,
                Y1 = 500,
                X2 = 1600,
                Y2 = 500,
                Type = PlatformType.Normal
            };
            testMapData.Platforms.Add(platform);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            // Should spawn at center of map on the platform
            Assert.AreEqual(1500, spawnPoint.X, 50); // Allow some tolerance
            Assert.AreEqual(500, spawnPoint.Y);
        }

        [Test]
        public void FindSpawnPoint_NoPortalsOrPlatforms_ReturnsMapCenter()
        {
            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            Assert.AreEqual(1500, spawnPoint.X); // Width/2
            Assert.AreEqual(500, spawnPoint.Y);  // Height/2
        }

        [Test]
        public void FindSpawnPoint_WithPortal_AddsHeightOffset()
        {
            // Arrange
            var portal = new Portal { Id = 0, Type = 0, X = 500, Y = 300 };
            testMapData.Portals.Add(portal);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            // Should spawn above the portal position (with height offset)
            Assert.Greater(spawnPoint.Y, portal.Y / 100f);
            Assert.AreEqual(5f, spawnPoint.X); // Portal X converted to units
        }

        [Test]
        public void IsValidSpawnPoint_ValidPoint_ReturnsTrue()
        {
            // Arrange
            var spawnPoint = new Vector2(5f, 3f); // In units, not pixels
            var platform = new Platform
            {
                Id = 1,
                X1 = 400,
                Y1 = 200,
                X2 = 600,
                Y2 = 200,
                Type = PlatformType.Normal
            };
            testMapData.Platforms.Add(platform);
            testMapData.Width = 1000;
            testMapData.Height = 1000;

            // Act
            var isValid = spawnManager.IsValidSpawnPoint(spawnPoint, testMapData);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void IsValidSpawnPoint_PointOutOfBounds_ReturnsFalse()
        {
            // Arrange
            var spawnPoint = new Vector2(-100, 300); // Negative X

            // Act
            var isValid = spawnManager.IsValidSpawnPoint(spawnPoint, testMapData);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void SpawnPlayer_SetsCorrectPosition()
        {
            // Arrange
            var player = new Player();
            var spawnPoint = new Vector2(500, 300);

            // Act
            spawnManager.SpawnPlayer(player, spawnPoint);

            // Assert
            Assert.AreEqual(500, player.Position.X);
            Assert.AreEqual(300, player.Position.Y);
        }

        [Test]
        public void SpawnPlayer_ResetsVelocity()
        {
            // Arrange
            var player = new Player();
            player.Velocity = new Vector2(100, -50); // Some initial velocity
            var spawnPoint = new Vector2(500, 300);

            // Act
            spawnManager.SpawnPlayer(player, spawnPoint);

            // Assert
            Assert.AreEqual(0, player.Velocity.X);
            Assert.AreEqual(0, player.Velocity.Y);
        }

        [Test]
        public void SpawnPlayer_SetsGroundedFalse()
        {
            // Arrange
            var player = new Player();
            player.IsGrounded = true;
            var spawnPoint = new Vector2(500, 300);

            // Act
            spawnManager.SpawnPlayer(player, spawnPoint);

            // Assert
            Assert.IsFalse(player.IsGrounded);
        }


        [Test]
        public void SpawnPlayer_CompleteFlow()
        {
            // Arrange
            var player = new Player();
            var spawnPoint = new Vector2(5f, 3.5f); // 500, 350 in pixels

            // Act
            spawnManager.SpawnPlayer(player, spawnPoint);

            // Assert
            Assert.AreEqual(5f, player.Position.X);
            Assert.AreEqual(3.5f, player.Position.Y);
            Assert.AreEqual(0, player.Velocity.X);
            Assert.AreEqual(0, player.Velocity.Y);
            Assert.IsFalse(player.IsGrounded);
        }
    }
}