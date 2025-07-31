using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.GameData;
using MapleClient.GameData.Adapters;
// Use GameLogic Vector2 for this test
using Vector2 = MapleClient.GameLogic.Vector2;
using UnityVector2 = UnityEngine.Vector2;

namespace MapleClient.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete foothold collision pipeline
    /// Tests from NX data loading through player physics
    /// </summary>
    [TestFixture]
    public class FootholdCollisionIntegrationTests
    {
        private GameWorld gameWorld;
        private FootholdService footholdService;
        private Player player;
        private NxMapLoader mapLoader;
        private MapData testMapData;
        
        [SetUp]
        public void SetUp()
        {
            // Create foothold service
            footholdService = new FootholdService();
            
            // Create map loader with foothold service
            mapLoader = new NxMapLoader("", footholdService);
            
            // Create game world with real services
            var inputProvider = new TestInputProvider();
            gameWorld = new GameWorld(inputProvider, mapLoader, null, null, footholdService);
            
            // Initialize player
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 100, 100, 0, 2);
            player = gameWorld.Player;
            
            // Create test map data
            CreateTestMapData();
        }
        
        private void CreateTestMapData()
        {
            testMapData = new MapData
            {
                MapId = 999999,
                Name = "Test Map",
                Width = 2000,
                Height = 1000,
                Platforms = new List<Platform>()
            };
            
            // Create test platforms (in MapleStory coordinates)
            testMapData.Platforms = new List<Platform>
            {
                // Flat platform at Y=-200
                new Platform { Id = 1, X1 = 0, Y1 = -200, X2 = 500, Y2 = -200, Type = PlatformType.Normal },
                new Platform { Id = 2, X1 = 500, Y1 = -200, X2 = 1000, Y2 = -200, Type = PlatformType.Normal },
                
                // Sloped platform from Y=-200 to Y=-300
                new Platform { Id = 3, X1 = 1000, Y1 = -200, X2 = 1500, Y2 = -300, Type = PlatformType.Normal },
                
                // Platform with gap
                new Platform { Id = 4, X1 = 1700, Y1 = -250, X2 = 2000, Y2 = -250, Type = PlatformType.Normal },
                
                // Higher platform
                new Platform { Id = 5, X1 = 200, Y1 = -100, X2 = 400, Y2 = -100, Type = PlatformType.Normal }
            };
            
            // Convert platforms to footholds for the foothold service
            var footholds = FootholdDataAdapter.ConvertPlatformsToFootholds(testMapData.Platforms);
            footholdService.LoadFootholds(footholds);
        }
        
        [Test]
        public void Player_SpawnsOnGroundHeight_NotFloating()
        {
            // Arrange
            player.Position = new Vector2(2.5f, 5f); // Unity coords, above ground
            player.IsGrounded = false;
            
            // Act - let player fall to ground
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                if (player.IsGrounded) break;
            }
            
            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should be grounded after falling");
            
            // Player bottom should be at Y=2 (ground), so center is at 2.3
            float expectedY = 2.3f; // Ground at 2 + half height (0.3)
            Assert.AreEqual(expectedY, player.Position.Y, 0.05f, "Player should be standing on ground, not floating");
            
            // Verify no floating - player should not be above ground
            float groundY = footholdService.GetGroundBelow(250f, -100f); // MapleCoords at X=2.5
            Assert.AreEqual(-201f, groundY, 1f, "Ground should be at expected position");
        }
        
        [Test]
        public void Player_WalksOnPlatform_WithoutFallingThrough()
        {
            // Arrange - place player on flat platform
            player.Position = new Vector2(3f, 2.3f); // On ground at Y=2
            player.IsGrounded = true;
            
            // Act - walk right for several frames
            player.MoveRight(true);
            Vector2 startPos = player.Position;
            
            for (int i = 0; i < 30; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
            }
            
            player.MoveRight(false);
            
            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should remain grounded while walking");
            Assert.Greater(player.Position.X, startPos.X, "Player should have moved right");
            Assert.AreEqual(2.3f, player.Position.Y, 0.05f, "Player should stay at same height on flat platform");
            Assert.AreEqual(0f, player.Velocity.Y, 0.01f, "Player should have no vertical velocity");
        }
        
        [Test]
        public void Player_FallsOffEdge_WhenWalkingOffPlatform()
        {
            // Arrange - place player near edge of first platform
            player.Position = new Vector2(9.8f, 2.3f); // Near end of platform at X=10
            player.IsGrounded = true;
            
            // Act - walk off edge
            player.MoveRight(true);
            
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
            }
            
            player.MoveRight(false);
            
            // Assert
            Assert.IsFalse(player.IsGrounded, "Player should not be grounded after walking off edge");
            Assert.Greater(player.Position.X, 10f, "Player should be past platform edge");
            Assert.Less(player.Velocity.Y, 0, "Player should be falling (negative Y velocity)");
        }
        
        [Test]
        public void Player_LandsCorrectly_AfterJumping()
        {
            // Arrange
            player.Position = new Vector2(5f, 2.3f); // On ground
            player.IsGrounded = true;
            float groundY = player.Position.Y;
            
            // Act - jump and land
            player.Jump();
            Assert.IsTrue(player.IsJumping, "Player should be jumping");
            Assert.Greater(player.Velocity.Y, 0, "Player should have upward velocity");
            
            // Simulate jump arc
            float maxHeight = player.Position.Y;
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                maxHeight = Mathf.Max(maxHeight, player.Position.Y);
                
                if (player.IsGrounded && i > 10) // Ensure we've actually jumped
                    break;
            }
            
            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should land back on ground");
            Assert.IsFalse(player.IsJumping, "Player should not be jumping after landing");
            Assert.Greater(maxHeight, groundY + 1f, "Player should have jumped at least 1 unit high");
            Assert.AreEqual(groundY, player.Position.Y, 0.1f, "Player should return to ground level");
            Assert.AreEqual(0f, player.Velocity.Y, 0.01f, "Vertical velocity should be zero on ground");
        }
        
        [Test]
        public void Player_WalksOnSlope_MaintainsGroundContact()
        {
            // Arrange - place player at start of slope
            player.Position = new Vector2(10f, 2.3f); // Start of slope
            player.IsGrounded = true;
            
            // Act - walk down slope
            player.MoveRight(true);
            float startY = player.Position.Y;
            
            List<float> yPositions = new List<float>();
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                yPositions.Add(player.Position.Y);
            }
            
            player.MoveRight(false);
            
            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should remain grounded on slope");
            Assert.Greater(player.Position.X, 10f, "Player should have moved right");
            
            // Check that Y position increases smoothly (going down slope)
            float lastY = startY;
            int increasingCount = 0;
            foreach (float y in yPositions)
            {
                if (y > lastY) increasingCount++;
                lastY = y;
            }
            
            Assert.Greater(increasingCount, yPositions.Count / 2, "Y position should increase as player walks down slope");
            Assert.Greater(player.Position.Y, startY, "Player should be lower after walking down slope");
        }
        
        [Test]
        public void Player_HandlesGapBetweenPlatforms()
        {
            // Arrange - place player before gap
            player.Position = new Vector2(14.5f, 3.3f); // Near end of sloped platform
            player.IsGrounded = true;
            
            // Act - try to walk across gap
            player.MoveRight(true);
            
            bool fellInGap = false;
            float lowestY = player.Position.Y;
            
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                
                // Check if player fell below both platforms
                if (player.Position.Y > 3.5f && player.Position.X > 15f && player.Position.X < 17f)
                {
                    fellInGap = true;
                }
                
                lowestY = Mathf.Max(lowestY, player.Position.Y); // Max because Y increases downward in Unity
            }
            
            // Assert
            Assert.IsTrue(fellInGap, "Player should fall into gap between platforms");
            Assert.Greater(lowestY, 3.5f, "Player should fall below platform level");
        }
        
        [Test]
        public void FootholdService_CorrectlyConvertsCoordinates()
        {
            // Test that foothold service correctly handles coordinate conversion
            
            // Unity position (5, 2.3) -> Maple position (500, -230)
            float unityX = 5f;
            float unityY = 2.3f;
            Vector2 unityPos = new Vector2(unityX, unityY);
            Vector2 maplePos = MaplePhysicsConverter.UnityToMaple(unityPos);
            
            Assert.AreEqual(500f, maplePos.X, 0.01f, "X conversion should multiply by 100");
            Assert.AreEqual(-230f, maplePos.Y, 0.01f, "Y conversion should negate and multiply by 100");
            
            // Test ground detection at this position
            float groundY = footholdService.GetGroundBelow(maplePos.X, maplePos.Y);
            Assert.AreNotEqual(float.MaxValue, groundY, "Should find ground below position");
            Assert.AreEqual(-201f, groundY, 1f, "Ground should be at expected Maple Y position");
            
            // Convert back to Unity
            float unityGroundY = MaplePhysicsConverter.MapleToUnityY(groundY + 1); // Add 1 back
            Assert.AreEqual(2f, unityGroundY, 0.01f, "Ground should be at Unity Y=2");
        }
        
        [Test]
        public void Player_RespawnsCorrectly_AfterFallingOffMap()
        {
            // Arrange - place player on platform
            player.Position = new Vector2(5f, 2.3f);
            player.IsGrounded = true;
            
            // Force player to fall off map
            player.Position = new Vector2(5f, 20f); // Way below any platform
            player.Velocity = new Vector2(0, -10f);
            player.IsGrounded = false;
            
            // Act - update physics and check for respawn
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                
                // In a real game, there would be a death/respawn check
                if (player.Position.Y > 50f) // Arbitrary "death" threshold
                {
                    // Simulate respawn
                    player.Position = new Vector2(5f, 5f); // Respawn above ground
                    player.Velocity = MapleClient.GameLogic.Vector2.Zero;
                    player.IsGrounded = false;
                    break;
                }
            }
            
            // Let player fall to ground after respawn
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                if (player.IsGrounded) break;
            }
            
            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should be grounded after respawn");
            Assert.AreEqual(2.3f, player.Position.Y, 0.1f, "Player should be on ground after respawn");
        }
        
        [Test]
        public void MultipleFootholds_HandleConnectionsCorrectly()
        {
            // Test that connected footholds work properly
            var foothold1 = footholdService.GetFootholdAt(250f, -200f); // Middle of first foothold
            var foothold2 = footholdService.GetConnectedFoothold(foothold1, true); // Get next
            
            Assert.IsNotNull(foothold1, "Should find first foothold");
            Assert.IsNotNull(foothold2, "Should find connected foothold");
            Assert.AreEqual(2, foothold2.Id, "Connected foothold should be ID 2");
            
            // Test reverse connection
            var foothold2Direct = footholdService.GetFootholdAt(750f, -200f);
            var foothold1FromNext = footholdService.GetConnectedFoothold(foothold2Direct, false); // Get previous
            
            Assert.IsNotNull(foothold1FromNext, "Should find previous foothold");
            Assert.AreEqual(1, foothold1FromNext.Id, "Previous foothold should be ID 1");
        }
        
        [Test]
        public void Player_JumpsToHigherPlatform()
        {
            // Arrange - player below higher platform
            player.Position = new Vector2(3f, 2.3f); // On ground below higher platform
            player.IsGrounded = true;
            
            // Act - jump towards higher platform
            player.Jump();
            player.MoveRight(true);
            
            bool landedOnHigherPlatform = false;
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, testMapData);
                
                // Check if on higher platform (Y=1, player center at 1.3)
                if (player.IsGrounded && player.Position.Y < 1.5f && player.Position.X >= 2f && player.Position.X <= 4f)
                {
                    landedOnHigherPlatform = true;
                    break;
                }
            }
            
            player.MoveRight(false);
            
            // Assert
            Assert.IsTrue(landedOnHigherPlatform, "Player should be able to jump to higher platform");
            Assert.AreEqual(1.3f, player.Position.Y, 0.1f, "Player should be on higher platform");
        }
        
        // Test helper class
        private class TestInputProvider : IInputProvider
        {
            public bool IsLeftPressed => false;
            public bool IsRightPressed => false;
            public bool IsJumpPressed => false;
            public bool IsAttackPressed => false;
            public bool IsUpPressed => false;
            public bool IsDownPressed => false;
        }
    }
}