using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.Tests.GameLogic
{
    [TestFixture]
    public class PlayerSpawnManagerTests
    {
        private PlayerSpawnManager spawnManager;
        private MapData testMapData;
        private TestFootholdService footholdService;

        [SetUp]
        public void SetUp()
        {
            footholdService = new TestFootholdService();
            spawnManager = new PlayerSpawnManager(footholdService);
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
        public void FindSpawnPoint_WithSpawnPortal_ReturnsPortalPositionOnGround()
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
            
            // Set foothold service to return ground at Y=320 (in MapleStory coords)
            footholdService.SetGroundAt(500, 320);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert - Unity coords
            Assert.AreEqual(5f, spawnPoint.X); // 500 / 100
            Assert.AreEqual(-3.2f, spawnPoint.Y); // -320 / 100 (inverted Y)
        }

        [Test]
        public void FindSpawnPoint_WithoutSpawnPortal_UsesMapCenterWithFoothold()
        {
            // Arrange
            // No spawn portal, should use map center
            float mapCenterX = testMapData.Width / 2f; // 1500
            float mapCenterY = testMapData.Height / 2f; // 500
            
            // Set foothold service to return ground at Y=550
            footholdService.SetGroundAt(mapCenterX, 550);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert - Unity coords
            Assert.AreEqual(15f, spawnPoint.X); // 1500 / 100
            Assert.AreEqual(-5.5f, spawnPoint.Y); // -550 / 100
        }

        [Test]
        public void FindSpawnPoint_NoFootholdFound_ReturnsMapCenterWithDefaultHeight()
        {
            // Arrange
            float mapCenterX = testMapData.Width / 2f; // 1500
            float mapCenterY = testMapData.Height / 2f; // 500
            
            // Set foothold service to return no ground found
            // Don't set any ground - GetGroundBelow will return float.MaxValue

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert - Unity coords, uses map center Y when no foothold
            Assert.AreEqual(15f, spawnPoint.X); // 1500 / 100
            Assert.AreEqual(-5f, spawnPoint.Y); // -500 / 100
        }

        [Test]
        public void FindSpawnPoint_WithSpecificPortalId_UsesRequestedPortal()
        {
            // Arrange
            var portal1 = new Portal { Id = 0, Type = PortalType.Spawn, X = 500, Y = 300 };
            var portal2 = new Portal { Id = 5, Type = PortalType.Regular, X = 800, Y = 400 };
            testMapData.Portals.Add(portal1);
            testMapData.Portals.Add(portal2);
            
            // Set foothold for portal 2
            footholdService.SetGroundAt(800, 420);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData, portalId: 5);

            // Assert - Should use portal 2, not the spawn portal
            Assert.AreEqual(8f, spawnPoint.X); // 800 / 100
            Assert.AreEqual(-4.2f, spawnPoint.Y); // -420 / 100
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
        
        [Test]
        public void FindSpawnPoint_PlayerHeightOffset_PositionsPlayerExactlyOnGround()
        {
            // Arrange
            var portal = new Portal { Id = 0, Type = PortalType.Spawn, X = 500, Y = 300 };
            testMapData.Portals.Add(portal);
            
            // Ground at Y=350 in MapleStory coords
            footholdService.SetGroundAt(500, 350);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            // Player should be positioned exactly on ground (no floating)
            // In Unity: ground is at -3.5, player bottom should touch ground
            Assert.AreEqual(5f, spawnPoint.X);
            Assert.AreEqual(-3.5f, spawnPoint.Y); // Exactly on ground
        }
        
        [Test]
        public void FindSpawnPoint_WithPlatformFallback_SearchesFromHigherPosition()
        {
            // Arrange - no portals, using center fallback
            float mapCenterX = 1500f;
            float mapCenterY = 500f;
            
            // First search from center might not find ground - don't set any ground at center Y
            // Search from top of map should find ground
            footholdService.SetGroundAt(mapCenterX, 600);

            // Act
            var spawnPoint = spawnManager.FindSpawnPoint(testMapData);

            // Assert
            Assert.AreEqual(15f, spawnPoint.X);
            Assert.AreEqual(-6f, spawnPoint.Y); // Found ground at 600
        }
    }
}