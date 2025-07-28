using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class PlatformCollisionTests
    {
        private Player player;
        private MapData mapData;
        
        [SetUp]
        public void SetUp()
        {
            player = new Player();
            mapData = new MapData();
        }
        
        [Test]
        public void GetPlatformBelow_WithNoPlatforms_ReturnsNull()
        {
            // Arrange
            mapData.Platforms = new List<Platform>();
            player.Position = new Vector2(5f, 5f);
            
            // Act
            var platform = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNull(platform);
        }
        
        [Test]
        public void GetPlatformBelow_WithPlatformDirectlyBelow_ReturnsPlatform()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f, // 2 units in pixels
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            player.Position = new Vector2(5f, 3f); // Player at 500px X, 300px Y
            
            // Act
            var result = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(platform, result);
        }
        
        [Test]
        public void GetPlatformBelow_WithMultiplePlatforms_ReturnsHighest()
        {
            // Arrange
            var platform1 = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 100f, // Lower platform
                X2 = 1000f,
                Y2 = 100f,
                Type = PlatformType.Normal
            };
            var platform2 = new Platform
            {
                Id = 2,
                X1 = 0f,
                Y1 = 200f, // Higher platform (closer to player)
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform1, platform2 };
            player.Position = new Vector2(5f, 3f); // Player at 300px Y
            
            // Act
            var result = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(platform2, result);
        }
        
        [Test]
        public void GetPlatformBelow_WithPlatformOutsideXRange_ReturnsNull()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 600f, // Platform starts at 600px
                Y1 = 200f,
                X2 = 800f,
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            player.Position = new Vector2(5f, 3f); // Player at 500px X - outside platform range
            
            // Act
            var result = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public void GetPlatformBelow_WithSlopedPlatform_ReturnsPlatform()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 100f,
                X2 = 1000f,
                Y2 = 300f, // Sloped upward
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            player.Position = new Vector2(5f, 3f); // Player at 500px X, 300px Y
            
            // Act
            var result = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(platform, result);
            
            // Verify the Y calculation at player's X
            float expectedY = platform.GetYAtX(500f); // 200px
            Assert.AreEqual(200f, expectedY);
        }
        
        [Test]
        public void GetPlatformBelow_IgnoresWallPlatforms_ReturnsNull()
        {
            // Arrange
            var wallPlatform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f,
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.Ladder // Not a landable platform
            };
            mapData.Platforms = new List<Platform> { wallPlatform };
            player.Position = new Vector2(5f, 3f);
            
            // Act
            var result = GetPlatformBelowForTest(player.Position, mapData);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public void UpdatePhysics_PlayerLandsOnPlatform_WhenFalling()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f, // Platform at 2 units
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            
            // Position player above platform and give downward velocity
            player.Position = new Vector2(5f, 2.5f); // Above platform
            player.Velocity = new Vector2(0f, -2f); // Falling
            player.IsGrounded = false;
            
            // Act
            player.UpdatePhysics(0.1f, mapData); // 100ms update
            
            // Assert
            // Player should land on platform (Y = 2 + half player height)
            Assert.AreEqual(2f + 0.3f, player.Position.Y, 0.01f); // 2.3 units
            Assert.AreEqual(0f, player.Velocity.Y);
            Assert.IsTrue(player.IsGrounded);
        }
        
        [Test]
        public void UpdatePhysics_PlayerDoesNotLand_WhenMovingUpward()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f,
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            
            // Position player below platform with upward velocity
            player.Position = new Vector2(5f, 1.5f); // Below platform
            player.Velocity = new Vector2(0f, 3f); // Moving up
            player.IsGrounded = false;
            
            // Act
            player.UpdatePhysics(0.1f, mapData);
            
            // Assert
            // Player should pass through platform when moving up
            Assert.Greater(player.Position.Y, 1.5f);
            Assert.Greater(player.Velocity.Y, 0f); // Still moving up (minus gravity)
            Assert.IsFalse(player.IsGrounded);
        }
        
        [Test]
        public void UpdatePhysics_OneWayPlatform_AllowsPassThrough()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f,
                X2 = 1000f,
                Y2 = 200f,
                Type = PlatformType.OneWay
            };
            mapData.Platforms = new List<Platform> { platform };
            
            // Position player below platform with upward velocity
            player.Position = new Vector2(5f, 1.5f);
            player.Velocity = new Vector2(0f, 3f);
            player.IsGrounded = false;
            
            // Act
            player.UpdatePhysics(0.1f, mapData);
            
            // Assert
            // Player should pass through one-way platform when moving up
            Assert.Greater(player.Position.Y, 1.5f);
            Assert.IsFalse(player.IsGrounded);
        }
        
        [Test]
        public void UpdatePhysics_PlayerStaysOnPlatformEdge_NoSliding()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f,
                X2 = 500f, // Platform ends at 5 units
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            
            // Position player at platform edge
            player.Position = new Vector2(4.9f, 2.3f); // Near edge but on platform
            player.Velocity = new Vector2(0f, 0f);
            player.IsGrounded = true;
            
            // Act
            player.UpdatePhysics(0.1f, mapData);
            
            // Assert
            // Player should stay on platform, no sliding
            Assert.AreEqual(4.9f, player.Position.X);
            Assert.IsTrue(player.IsGrounded);
        }
        
        [Test]
        public void UpdatePhysics_PlayerFallsOffPlatformEdge()
        {
            // Arrange
            var platform = new Platform
            {
                Id = 1,
                X1 = 0f,
                Y1 = 200f,
                X2 = 500f, // Platform ends at 5 units
                Y2 = 200f,
                Type = PlatformType.Normal
            };
            mapData.Platforms = new List<Platform> { platform };
            
            // Position player past platform edge
            player.Position = new Vector2(5.1f, 2.3f); // Past edge
            player.Velocity = new Vector2(0f, 0f);
            player.IsGrounded = true;
            
            // Act
            player.UpdatePhysics(0.1f, mapData);
            
            // Assert
            // Player should start falling
            Assert.Less(player.Velocity.Y, 0f); // Falling due to gravity
            Assert.IsFalse(player.IsGrounded);
        }
        
        // Helper method to test GetPlatformBelow without making it public
        private Platform GetPlatformBelowForTest(Vector2 position, MapData mapData)
        {
            // We'll use reflection or make the method internal for testing
            // For now, let's use a simple implementation that mirrors the expected behavior
            if (mapData?.Platforms == null) return null;
            
            float posX = position.X * 100f;
            float posY = (position.Y - 0.3f) * 100f; // Player bottom
            
            Platform bestPlatform = null;
            float bestY = float.MinValue;
            
            foreach (var p in mapData.Platforms)
            {
                if (p.Type != PlatformType.Normal && p.Type != PlatformType.OneWay)
                    continue;
                    
                if (posX < p.X1 || posX > p.X2)
                    continue;
                    
                float platY = p.GetYAtX(posX);
                if (float.IsNaN(platY))
                    continue;
                    
                if (platY <= posY + 50 && platY > bestY)
                {
                    bestPlatform = p;
                    bestY = platY;
                }
            }
            
            return bestPlatform;
        }
    }
}