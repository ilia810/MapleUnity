using NUnit.Framework;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;
using System.Diagnostics;

namespace MapleClient.Tests.GameLogic
{
    /// <summary>
    /// Edge case and performance tests for foothold collision system
    /// </summary>
    [TestFixture]
    public class FootholdCollisionEdgeCaseTests
    {
        private FootholdService footholdService;
        private Player player;
        private MapData mapData;
        
        [SetUp]
        public void SetUp()
        {
            footholdService = new FootholdService();
            player = new Player(footholdService);
            mapData = new MapData
            {
                MapId = 999999,
                Name = "Edge Case Test Map",
                Width = 10000,
                Height = 10000,
                Platforms = new List<Platform>()
            };
        }
        
        [Test]
        public void Player_HandlesVerticalWalls_Correctly()
        {
            // Create vertical wall foothold
            var wallFoothold = new Foothold(1, 500, -100, 500, -500)
            {
                IsWall = true
            };
            footholdService.LoadFootholds(new List<Foothold> { wallFoothold });
            
            // Place player next to wall
            player.Position = new Vector2(4.8f, 3f);
            player.Velocity = new Vector2(2f, 0); // Moving towards wall
            player.IsGrounded = false;
            
            // Update physics
            for (int i = 0; i < 10; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
            }
            
            // Player should not pass through wall
            Assert.Less(player.Position.X, 5.1f, "Player should not pass through vertical wall");
        }
        
        [Test]
        public void Player_HandlesExtremeSlopes_WithoutGlitching()
        {
            // Create very steep slope (almost vertical)
            var steepSlope = new Foothold(1, 500, -200, 520, -400); // 89.5 degree slope
            footholdService.LoadFootholds(new List<Foothold> { steepSlope });
            
            // Place player on slope
            player.Position = new Vector2(5.1f, 2.5f);
            player.IsGrounded = true;
            
            // Try to walk on steep slope
            player.MoveRight(true);
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
            }
            
            // Player should slide down or not climb steep slope
            Assert.IsTrue(player.Position.Y >= 2.5f || player.Velocity.X < 0.1f, 
                "Player should not climb extremely steep slopes normally");
        }
        
        [Test]
        public void Player_HandlesZeroLengthFootholds()
        {
            // Create degenerate foothold (point)
            var pointFoothold = new Foothold(1, 500, -200, 500, -200);
            footholdService.LoadFootholds(new List<Foothold> { pointFoothold });
            
            // Place player exactly on point
            player.Position = new Vector2(5f, 2.5f);
            player.IsGrounded = false;
            
            // Update physics - should not crash
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                }
            });
        }
        
        [Test]
        public void Player_HandlesConcaveFootholdArrangements()
        {
            // Create V-shaped foothold arrangement
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, -200, 500, -400), // Left slope down
                new Foothold(2, 500, -400, 1000, -200) // Right slope up
            };
            footholds[0].NextId = 2;
            footholds[1].PreviousId = 1;
            footholdService.LoadFootholds(footholds);
            
            // Place player at valley bottom
            player.Position = new Vector2(5f, 4.3f);
            player.IsGrounded = true;
            
            // Walk through valley
            player.MoveRight(true);
            List<float> yPositions = new List<float>();
            
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                yPositions.Add(player.Position.Y);
            }
            
            // Should go down then up
            float minY = float.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < yPositions.Count; i++)
            {
                if (yPositions[i] < minY)
                {
                    minY = yPositions[i];
                    minIndex = i;
                }
            }
            
            Assert.Greater(minIndex, 10, "Should reach valley bottom after some movement");
            Assert.Less(minIndex, 90, "Should climb out of valley before end");
        }
        
        [Test]
        public void Player_HandlesOverlappingFootholds()
        {
            // Create overlapping footholds at different heights
            var footholds = new List<Foothold>
            {
                new Foothold(1, 400, -200, 600, -200), // Higher platform
                new Foothold(2, 300, -300, 700, -300)  // Lower platform (overlaps X range)
            };
            footholdService.LoadFootholds(footholds);
            
            // Drop player from above
            player.Position = new Vector2(5f, 1f);
            player.IsGrounded = false;
            
            // Fall
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                if (player.IsGrounded) break;
            }
            
            // Should land on higher platform, not fall through to lower
            Assert.IsTrue(player.IsGrounded, "Should land on a platform");
            Assert.AreEqual(2.3f, player.Position.Y, 0.1f, "Should land on higher platform");
        }
        
        [Test]
        public void Player_HandlesFootholdGapsCorrectly()
        {
            // Create footholds with small gap
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, -200, 499, -200),    // Ends at 499
                new Foothold(2, 501, -200, 1000, -200)  // Starts at 501 (2 pixel gap)
            };
            footholdService.LoadFootholds(footholds);
            
            // Place player before gap
            player.Position = new Vector2(4.8f, 2.3f);
            player.IsGrounded = true;
            
            // Walk across gap
            player.MoveRight(true);
            bool fellInGap = false;
            
            for (int i = 0; i < 50; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                
                // Check if fell through gap
                if (!player.IsGrounded && player.Position.X > 4.99f && player.Position.X < 5.01f)
                {
                    fellInGap = true;
                }
            }
            
            // Small gaps might be crossed due to movement speed
            // This tests that the system handles them consistently
            Assert.IsTrue(fellInGap || player.IsGrounded, "Should either fall in gap or cross it");
        }
        
        [Test]
        public void Player_HandlesHighSpeedCollisions()
        {
            // Create simple platform
            var foothold = new Foothold(1, 0, -200, 1000, -200);
            footholdService.LoadFootholds(new List<Foothold> { foothold });
            
            // Place player high above with extreme downward velocity
            player.Position = new Vector2(5f, 10f);
            player.Velocity = new Vector2(0, -50f); // Very fast falling
            player.IsGrounded = false;
            
            // Update physics
            for (int i = 0; i < 20; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                if (player.IsGrounded) break;
            }
            
            // Should not tunnel through platform
            Assert.IsTrue(player.IsGrounded, "Should land on platform even at high speed");
            Assert.GreaterOrEqual(player.Position.Y, 2f, "Should not tunnel through platform");
        }
        
        [Test]
        public void Player_HandlesTeleportationCorrectly()
        {
            // Create platforms
            var footholds = new List<Foothold>
            {
                new Foothold(1, 0, -200, 500, -200),
                new Foothold(2, 1000, -500, 1500, -500)
            };
            footholdService.LoadFootholds(footholds);
            
            // Start on first platform
            player.Position = new Vector2(2.5f, 2.3f);
            player.IsGrounded = true;
            
            // Teleport to above second platform
            player.Position = new Vector2(12.5f, 4f);
            player.IsGrounded = false;
            
            // Update physics
            for (int i = 0; i < 30; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                if (player.IsGrounded) break;
            }
            
            // Should land on second platform
            Assert.IsTrue(player.IsGrounded, "Should find ground after teleport");
            Assert.AreEqual(5.3f, player.Position.Y, 0.1f, "Should land on second platform");
        }
        
        [Test]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void FootholdService_PerformanceWithManyFootholds(int footholdCount)
        {
            // Create many footholds
            var footholds = new List<Foothold>();
            for (int i = 0; i < footholdCount; i++)
            {
                float x = i * 100;
                footholds.Add(new Foothold(i, x, -200, x + 90, -200));
            }
            footholdService.LoadFootholds(footholds);
            
            // Measure query performance
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Perform many ground queries
            int queryCount = 1000;
            for (int i = 0; i < queryCount; i++)
            {
                float x = UnityEngine.Random.Range(0, footholdCount * 100);
                float y = UnityEngine.Random.Range(-500, 0);
                footholdService.GetGroundBelow(x, y);
            }
            
            stopwatch.Stop();
            float avgQueryTime = stopwatch.ElapsedMilliseconds / (float)queryCount;
            
            UnityEngine.Debug.Log($"Average query time with {footholdCount} footholds: {avgQueryTime:F3}ms");
            
            // Performance assertion
            Assert.Less(avgQueryTime, 1f, $"Queries should be fast even with {footholdCount} footholds");
        }
        
        [Test]
        public void Player_HandlesMovingPlatforms()
        {
            // Note: This test is for future moving platform support
            // Currently tests that system doesn't break with changing footholds
            
            var foothold = new Foothold(1, 500, -200, 700, -200);
            footholdService.LoadFootholds(new List<Foothold> { foothold });
            
            player.Position = new Vector2(6f, 2.3f);
            player.IsGrounded = true;
            
            // Simulate moving platform by updating foothold
            for (int i = 0; i < 10; i++)
            {
                // Move platform up
                foothold.Y1 -= 10;
                foothold.Y2 -= 10;
                footholdService.UpdateFoothold(foothold);
                
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
            }
            
            // Player should follow platform or fall
            Assert.IsTrue(player.Position.Y <= 2.3f || !player.IsGrounded, 
                "Player should either follow platform up or fall off");
        }
        
        [Test]
        public void Player_HandlesNaNAndInfinityGracefully()
        {
            var foothold = new Foothold(1, 0, -200, 1000, -200);
            footholdService.LoadFootholds(new List<Foothold> { foothold });
            
            // Test NaN position
            player.Position = new Vector2(float.NaN, float.NaN);
            
            Assert.DoesNotThrow(() =>
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
            }, "Should handle NaN position without crashing");
            
            // Test infinity velocity
            player.Position = new Vector2(5f, 5f);
            player.Velocity = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
            
            Assert.DoesNotThrow(() =>
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
            }, "Should handle infinite velocity without crashing");
        }
        
        [Test]
        public void Player_MaintainsConsistentStateAcrossFrames()
        {
            // Create simple platform
            var foothold = new Foothold(1, 0, -300, 1000, -300);
            footholdService.LoadFootholds(new List<Foothold> { foothold });
            
            // Place player on ground
            player.Position = new Vector2(5f, 3.3f);
            player.IsGrounded = true;
            
            // Record states across many frames
            List<bool> groundedStates = new List<bool>();
            List<float> yPositions = new List<float>();
            
            for (int i = 0; i < 100; i++)
            {
                player.UpdatePhysics(MaplePhysics.FIXED_TIMESTEP, mapData);
                groundedStates.Add(player.IsGrounded);
                yPositions.Add(player.Position.Y);
            }
            
            // Check consistency
            foreach (bool grounded in groundedStates)
            {
                Assert.IsTrue(grounded, "Should remain grounded on flat platform");
            }
            
            float yVariance = 0f;
            foreach (float y in yPositions)
            {
                yVariance += System.Math.Abs(y - yPositions[0]);
            }
            yVariance /= yPositions.Count;
            
            Assert.Less(yVariance, 0.01f, "Y position should be stable when standing still");
        }
    }
}