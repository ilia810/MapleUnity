using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.Tests.GameLogic
{
    // Test implementation of IFootholdService
    public class TestFootholdService : IFootholdService
    {
        private readonly Dictionary<float, float> groundYByX = new Dictionary<float, float>();
        private readonly List<Foothold> footholds = new List<Foothold>();
        
        public void SetGroundAt(float x, float mapleY)
        {
            groundYByX[x] = mapleY;
        }
        
        public void AddFoothold(Foothold foothold)
        {
            footholds.Add(foothold);
        }
        
        public float GetGroundBelow(float x, float y)
        {
            // Check if we have a specific ground set for this X
            if (groundYByX.ContainsKey(x))
            {
                float groundY = groundYByX[x];
                if (groundY >= y) // Ground is below (larger Y in Maple coords)
                    return groundY - 1; // Subtract 1 like real implementation
            }
            
            // Check footholds
            foreach (var fh in footholds)
            {
                float minX = System.Math.Min(fh.X1, fh.X2);
                float maxX = System.Math.Max(fh.X1, fh.X2);
                
                if (x >= minX && x <= maxX)
                {
                    float groundY = fh.GetYAtX(x);
                    if (!float.IsNaN(groundY) && groundY >= y)
                    {
                        return groundY - 1;
                    }
                }
            }
            
            return float.MaxValue;
        }
        
        public bool IsOnGround(float x, float y, float tolerance = 1f)
        {
            float groundY = GetGroundBelow(x, y - tolerance);
            if (groundY == float.MaxValue) return false;
            float actualGroundY = groundY + 1;
            return System.Math.Abs(y - actualGroundY) <= tolerance;
        }
        
        public Foothold GetFootholdAt(float x, float y)
        {
            return footholds.FirstOrDefault(fh => fh.ContainsPoint(x, y, 5f));
        }
        
        public Foothold GetFootholdBelow(float x, float y)
        {
            return footholds
                .Where(fh => 
                {
                    float minX = System.Math.Min(fh.X1, fh.X2);
                    float maxX = System.Math.Max(fh.X1, fh.X2);
                    if (x < minX || x > maxX) return false;
                    float groundY = fh.GetYAtX(x);
                    return !float.IsNaN(groundY) && groundY >= y;
                })
                .OrderBy(fh => fh.GetYAtX(x))
                .FirstOrDefault();
        }
        
        public IEnumerable<Foothold> GetFootholdsInArea(float minX, float minY, float maxX, float maxY)
        {
            return footholds.Where(fh =>
            {
                float fhMinX = System.Math.Min(fh.X1, fh.X2);
                float fhMaxX = System.Math.Max(fh.X1, fh.X2);
                float fhMinY = System.Math.Min(fh.Y1, fh.Y2);
                float fhMaxY = System.Math.Max(fh.Y1, fh.Y2);
                return fhMaxX >= minX && fhMinX <= maxX && fhMaxY >= minY && fhMinY <= maxY;
            });
        }
        
        public Foothold GetConnectedFoothold(Foothold currentFoothold, bool movingRight)
        {
            if (currentFoothold == null) return null;
            int targetId = movingRight ? currentFoothold.NextId : currentFoothold.PreviousId;
            if (targetId == 0) return null;
            return footholds.FirstOrDefault(fh => fh.Id == targetId);
        }
        
        public Foothold FindNearestFoothold(float x, float y, float maxDistance = 1000f)
        {
            return footholds
                .Select(fh => new { Foothold = fh, Distance = GetDistanceToFoothold(fh, x, y) })
                .Where(item => item.Distance <= maxDistance)
                .OrderBy(item => item.Distance)
                .Select(item => item.Foothold)
                .FirstOrDefault();
        }
        
        private float GetDistanceToFoothold(Foothold fh, float x, float y)
        {
            // Simple distance calculation - could be improved
            float centerX = (fh.X1 + fh.X2) / 2;
            float centerY = (fh.Y1 + fh.Y2) / 2;
            float dx = x - centerX;
            float dy = y - centerY;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }
        
        public bool IsWall(Foothold foothold)
        {
            if (foothold == null) return false;
            return foothold.IsWall || System.Math.Abs(foothold.X2 - foothold.X1) < 0.1f;
        }
        
        public float GetSlopeAt(Foothold foothold, float x)
        {
            if (foothold == null) return 0f;
            float minX = System.Math.Min(foothold.X1, foothold.X2);
            float maxX = System.Math.Max(foothold.X1, foothold.X2);
            if (x < minX || x > maxX) return 0f;
            return foothold.GetSlope();
        }
        
        public void LoadFootholds(List<Foothold> footholdData)
        {
            footholds.Clear();
            if (footholdData != null)
            {
                footholds.AddRange(footholdData);
            }
        }
        
        public void UpdateFoothold(Foothold foothold)
        {
            if (foothold == null) return;
            
            var existing = footholds.FirstOrDefault(f => f.Id == foothold.Id);
            if (existing != null)
            {
                footholds.Remove(existing);
            }
            footholds.Add(foothold);
        }
    }
    
    [TestFixture]
    public class PlayerFootholdTests
    {
        private Player player;
        private MapData mapData;
        private TestFootholdService footholdService;
        private const float DELTA_TIME = 0.016f; // 60 FPS

        [SetUp]
        public void SetUp()
        {
            footholdService = new TestFootholdService();
            player = new Player(footholdService);
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

        // New tests for FootholdService integration
        [Test]
        public void Player_UsesFootholdService_WhenCheckingGround()
        {
            // Arrange
            player.Position = new Vector2(5, 3); // Unity coords
            player.IsGrounded = false;
            
            // Expected conversion: Unity (5, 3) -> Maple (500, -300)
            // With player height 0.6, bottom is at Y=2.7 -> Maple Y=-270
            footholdService.SetGroundAt(500f, -200f); // Ground at Maple Y=-200 (Unity Y=2)

            // Act
            float initialY = player.Position.Y;
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert - player should have moved towards ground
            Assert.Less(player.Position.Y, initialY, "Player should fall when not grounded");
        }

        [Test]
        public void Player_LandsOnFoothold_WhenFalling()
        {
            // Arrange
            player.Position = new Vector2(5, 3); // Unity coords
            player.Velocity = new Vector2(0, -2); // Falling
            player.IsGrounded = false;
            
            // Create a foothold at Unity Y=2 (Maple Y=-200)
            var foothold = new Foothold(1, 0, -200, 1000, -200);
            footholdService.AddFoothold(foothold);

            // Act - simulate multiple frames to ensure landing
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
                if (player.IsGrounded) break;
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should be grounded");
            Assert.AreEqual(0, player.Velocity.Y, "Vertical velocity should be zero");
            // Player bottom should be at ground level (Y=2), so center is at 2.3 (half height above)
            Assert.AreEqual(2.3f, player.Position.Y, 0.05f);
        }

        [Test]
        public void Player_StaysOnFoothold_WhenWalking()
        {
            // Arrange
            player.Position = new Vector2(5, 2.3f); // On ground at Y=2
            player.IsGrounded = true;
            player.MoveRight(true);
            
            // Create a wide foothold at same level
            var foothold = new Foothold(1, 0, -200, 1000, -200);
            footholdService.AddFoothold(foothold);

            // Act
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should remain grounded");
            Assert.Greater(player.Position.X, 5, "Player should have moved right");
            Assert.AreEqual(2.3f, player.Position.Y, 0.01f, "Player should stay at same height");
        }

        [Test]
        public void Player_FallsOffFoothold_WhenNoGroundBelow()
        {
            // Arrange
            player.Position = new Vector2(5, 2.3f);
            player.IsGrounded = true;
            player.MoveRight(true);
            
            // No footholds added - no ground below

            // Act
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert
            Assert.IsFalse(player.IsGrounded, "Player should not be grounded");
            Assert.Less(player.Velocity.Y, 0, "Player should be falling");
        }

        [Test]
        public void Player_HandlesSlope_WhenWalkingOnFoothold()
        {
            // Arrange - player on sloped foothold
            player.Position = new Vector2(5, 2.3f);
            player.IsGrounded = true;
            player.MoveRight(true);
            
            // Create a sloped foothold that goes down as we move right
            // Unity Y=2 at X=5 (Maple: -200 at 500), Unity Y=2.5 at X=10 (Maple: -250 at 1000)
            var foothold = new Foothold(1, 500, -200, 1000, -250);
            footholdService.AddFoothold(foothold);

            // Act - move player multiple frames
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(DELTA_TIME, mapData);
            }

            // Assert
            Assert.IsTrue(player.IsGrounded, "Player should remain grounded on slope");
            Assert.Greater(player.Position.X, 5, "Player should have moved right");
            Assert.Greater(player.Position.Y, 2.3f, "Player Y should increase going down slope");
        }

        [Test]
        public void Player_ConvertsCoordinatesCorrectly_WhenUsingFootholdService()
        {
            // Arrange
            player.Position = new Vector2(10, 5); // Unity position
            player.IsGrounded = false;
            
            // Add a foothold far below so player doesn't land
            var foothold = new Foothold(1, 0, -1000, 2000, -1000); // Very low ground
            footholdService.AddFoothold(foothold);

            // Act
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert - verify coordinate conversion by checking player physics
            // Player should be falling (no ground nearby)
            Assert.IsFalse(player.IsGrounded, "Player should not be grounded");
            Assert.Less(player.Velocity.Y, 0, "Player should be falling");
        }

        [Test]
        public void Player_DoesNotGetGroundedInMidAir_BugFix()
        {
            // This tests the "grounded while floating" bug fix
            // Arrange
            player.Position = new Vector2(5, 10); // High in the air
            player.IsGrounded = false;
            player.Velocity = new Vector2(0, -1); // Falling slowly
            
            // Ground is far below at Unity Y=2
            var foothold = new Foothold(1, 0, -200, 1000, -200);
            footholdService.AddFoothold(foothold);

            // Act - single physics update
            player.UpdatePhysics(DELTA_TIME, mapData);

            // Assert
            Assert.IsFalse(player.IsGrounded, "Player should not be grounded when far above ground");
            Assert.Less(player.Velocity.Y, 0, "Player should still be falling");
        }
    }
}