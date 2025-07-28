using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;

namespace MapleClient.GameLogic.Tests
{
    /// <summary>
    /// Performance tests to ensure physics maintains 60 FPS and smooth gameplay.
    /// Tests for memory allocations, frame timing, and computational efficiency.
    /// </summary>
    [TestFixture]
    public class PhysicsPerformanceTests
    {
        private PhysicsUpdateManager physicsManager;
        private List<Player> players;
        private MapData complexMap;
        private const int FRAME_COUNT = 600; // 10 seconds at 60 FPS
        private const float TARGET_FRAME_TIME = 16.67f; // milliseconds for 60 FPS
        private const float ACCEPTABLE_FRAME_TIME = 20f; // Allow up to 20ms
        
        [SetUp]
        public void Setup()
        {
            physicsManager = new PhysicsUpdateManager();
            players = new List<Player>();
            
            // Create a complex map with many platforms
            complexMap = new MapData
            {
                MapId = 100000000,
                Platforms = new List<Platform>(),
                Ladders = new List<LadderInfo>()
            };
            
            // Add 100 platforms of various types
            for (int i = 0; i < 100; i++)
            {
                float y = i * 50f;
                complexMap.Platforms.Add(new Platform
                {
                    Id = i,
                    X1 = -500 + (i % 10) * 100,
                    Y1 = y,
                    X2 = -400 + (i % 10) * 100,
                    Y2 = y + (i % 3 == 0 ? 20 : 0), // Some slopes
                    Type = i % 3 == 0 ? PlatformType.OneWay : PlatformType.Normal
                });
            }
            
            // Add some ladders
            for (int i = 0; i < 20; i++)
            {
                complexMap.Ladders.Add(new LadderInfo
                {
                    X = i * 50,
                    Y1 = 0,
                    Y2 = 500
                });
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            physicsManager.Reset();
            players.Clear();
        }
        
        #region Frame Time Performance Tests
        
        [Test]
        public void SinglePlayerPhysics_Maintains60FPS()
        {
            // Test single player physics performance
            
            var player = new Player();
            physicsManager.RegisterPhysicsObject(player);
            players.Add(player);
            
            var stopwatch = new Stopwatch();
            var frameTimes = new List<double>();
            
            // Simulate player movement
            player.MoveRight(true);
            player.Jump();
            
            for (int i = 0; i < FRAME_COUNT; i++)
            {
                stopwatch.Restart();
                
                // Simulate input changes
                if (i % 60 == 0) // Every second
                {
                    player.Jump();
                    player.MoveRight(i % 120 < 60);
                    player.MoveLeft(i % 120 >= 60);
                }
                
                physicsManager.Update(1f / 60f, complexMap);
                
                stopwatch.Stop();
                frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            
            // Analyze performance
            double averageFrameTime = frameTimes.Average();
            double maxFrameTime = frameTimes.Max();
            double percentile95 = GetPercentile(frameTimes, 0.95);
            
            Console.WriteLine($"Single Player Performance:");
            Console.WriteLine($"  Average frame time: {averageFrameTime:F2}ms");
            Console.WriteLine($"  Max frame time: {maxFrameTime:F2}ms");
            Console.WriteLine($"  95th percentile: {percentile95:F2}ms");
            
            // Assert performance requirements
            Assert.That(averageFrameTime, Is.LessThan(TARGET_FRAME_TIME));
            Assert.That(percentile95, Is.LessThan(ACCEPTABLE_FRAME_TIME));
        }
        
        [Test]
        public void MultiplePlayersPhysics_ScalesWell()
        {
            // Test physics performance with multiple players
            
            int[] playerCounts = { 1, 5, 10, 20, 50 };
            var results = new Dictionary<int, double>();
            
            foreach (int playerCount in playerCounts)
            {
                physicsManager.Reset();
                players.Clear();
                
                // Create players
                for (int i = 0; i < playerCount; i++)
                {
                    var player = new Player();
                    player.Position = new Vector2(i * 10, 0.3f);
                    physicsManager.RegisterPhysicsObject(player);
                    players.Add(player);
                }
                
                var stopwatch = new Stopwatch();
                var frameTimes = new List<double>();
                
                // Run physics for 100 frames
                for (int frame = 0; frame < 100; frame++)
                {
                    stopwatch.Restart();
                    
                    // Simulate some players moving
                    foreach (var player in players.Take(playerCount / 2))
                    {
                        player.MoveRight(frame % 20 < 10);
                        if (frame % 30 == 0) player.Jump();
                    }
                    
                    physicsManager.Update(1f / 60f, complexMap);
                    
                    stopwatch.Stop();
                    frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                }
                
                results[playerCount] = frameTimes.Average();
            }
            
            // Verify scaling is reasonable (not exponential)
            Console.WriteLine("Multi-player scaling:");
            foreach (var kvp in results)
            {
                Console.WriteLine($"  {kvp.Key} players: {kvp.Value:F2}ms average");
            }
            
            // Performance should scale linearly or better
            double scalingFactor = results[50] / results[1];
            Assert.That(scalingFactor, Is.LessThan(60), "Performance should not degrade by more than 60x for 50x players");
            
            // Even with 50 players, should maintain playable framerate
            Assert.That(results[50], Is.LessThan(ACCEPTABLE_FRAME_TIME * 2));
        }
        
        #endregion
        
        #region Memory Allocation Tests
        
        [Test]
        public void PhysicsUpdate_MinimalAllocations()
        {
            // Test that physics updates don't allocate excessive garbage
            
            var player = new Player();
            physicsManager.RegisterPhysicsObject(player);
            player.MoveRight(true);
            
            // Warm up
            for (int i = 0; i < 60; i++)
            {
                physicsManager.Update(1f / 60f, complexMap);
            }
            
            // Measure allocations
            long memBefore = GC.GetTotalMemory(true);
            
            // Run physics for 1 second (60 frames)
            for (int i = 0; i < 60; i++)
            {
                physicsManager.Update(1f / 60f, complexMap);
            }
            
            long memAfter = GC.GetTotalMemory(false);
            long allocated = memAfter - memBefore;
            
            Console.WriteLine($"Memory allocated in 60 frames: {allocated} bytes ({allocated / 60} bytes/frame)");
            
            // Should allocate less than 1KB per frame on average
            Assert.That(allocated / 60, Is.LessThan(1024), "Physics should not allocate more than 1KB per frame");
        }
        
        [Test]
        public void PlatformCollision_EfficientChecking()
        {
            // Test that platform collision checking is efficient
            
            var player = new Player();
            player.Position = new Vector2(0, 50); // Start high
            player.Velocity = new Vector2(1.25f, -5f); // Moving and falling
            
            var stopwatch = new Stopwatch();
            var checkTimes = new List<double>();
            
            // Test collision checking performance
            for (int i = 0; i < 1000; i++)
            {
                stopwatch.Restart();
                
                // This simulates what happens in UpdatePhysics
                var platform = GetPlatformBelow(player.Position, complexMap);
                
                stopwatch.Stop();
                checkTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                
                // Move player for next check
                player.Position = player.Position + new Vector2(0.1f, -0.1f);
            }
            
            double avgCheckTime = checkTimes.Average();
            Console.WriteLine($"Average platform check time: {avgCheckTime:F4}ms");
            
            // Platform checks should be very fast (sub-millisecond)
            Assert.That(avgCheckTime, Is.LessThan(0.1), "Platform collision checks should take less than 0.1ms");
        }
        
        // Helper method that mirrors Player.GetPlatformBelow for testing
        private Platform GetPlatformBelow(Vector2 position, MapData mapData)
        {
            float posX = position.X * 100f;
            float posY = (position.Y - 0.3f) * 100f; // Player bottom
            
            Platform closestPlatform = null;
            float closestDistance = float.MaxValue;
            
            foreach (var platform in mapData.Platforms)
            {
                if (platform.Type != PlatformType.Normal && platform.Type != PlatformType.OneWay)
                    continue;
                    
                if (posX < platform.X1 || posX > platform.X2)
                    continue;
                    
                float platformY = platform.GetYAtX(posX);
                if (float.IsNaN(platformY))
                    continue;
                    
                float distance = platformY - posY;
                if (distance >= -5f && distance < closestDistance)
                {
                    closestPlatform = platform;
                    closestDistance = distance;
                }
            }
            
            return closestDistance <= 100f ? closestPlatform : null;
        }
        
        #endregion
        
        #region Interpolation Performance Tests
        
        [Test]
        public void InterpolationCalculation_Efficient()
        {
            // Test that interpolation factor calculation is fast
            
            var stopwatch = new Stopwatch();
            var times = new List<double>();
            
            for (int i = 0; i < 10000; i++)
            {
                // Simulate various accumulator states
                physicsManager.Update((i % 100) / 6000f, complexMap);
                
                stopwatch.Restart();
                float factor = physicsManager.GetInterpolationFactor();
                stopwatch.Stop();
                
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
                
                // Verify factor is valid
                Assert.That(factor, Is.InRange(0f, 1f));
            }
            
            double avgTime = times.Average();
            Console.WriteLine($"Average interpolation calculation time: {avgTime * 1000:F2} microseconds");
            
            // Should be essentially instant (less than 1 microsecond)
            Assert.That(avgTime, Is.LessThan(0.001));
        }
        
        #endregion
        
        #region Stress Tests
        
        [Test]
        public void StressTest_ManyObjectsWithModifiers()
        {
            // Stress test with many objects and movement modifiers
            
            physicsManager.Reset();
            
            // Create 100 players with various modifiers
            for (int i = 0; i < 100; i++)
            {
                var player = new Player();
                player.Position = new Vector2(i * 2, i * 0.5f);
                
                // Add random modifiers
                if (i % 3 == 0) player.AddMovementModifier(new SlipperyModifier());
                if (i % 5 == 0) player.AddMovementModifier(new StunModifier(2f));
                if (i % 7 == 0) player.AddMovementModifier(new SpeedModifier(1.5f, 5f, "boost"));
                
                physicsManager.RegisterPhysicsObject(player);
                players.Add(player);
            }
            
            var stopwatch = new Stopwatch();
            var frameTimes = new List<double>();
            
            // Run for 300 frames (5 seconds)
            for (int frame = 0; frame < 300; frame++)
            {
                stopwatch.Restart();
                
                // Simulate activity
                int playerIndex = 0;
                foreach (var player in players)
                {
                    if (frame % 60 == playerIndex % 60)
                    {
                        player.Jump();
                        player.MoveRight(frame % 2 == 0);
                    }
                    playerIndex++;
                }
                
                physicsManager.Update(1f / 60f, complexMap);
                
                stopwatch.Stop();
                frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            
            // Even under stress, should maintain reasonable performance
            double avgTime = frameTimes.Average();
            double maxTime = frameTimes.Max();
            
            Console.WriteLine($"Stress test results:");
            Console.WriteLine($"  100 players with modifiers");
            Console.WriteLine($"  Average frame time: {avgTime:F2}ms");
            Console.WriteLine($"  Max frame time: {maxTime:F2}ms");
            
            // Should still be playable (under 33ms for 30 FPS minimum)
            Assert.That(avgTime, Is.LessThan(33));
            Assert.That(maxTime, Is.LessThan(50));
        }
        
        #endregion
        
        #region Helper Methods
        
        private double GetPercentile(List<double> values, double percentile)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int index = (int)(sorted.Count * percentile);
            return sorted[Math.Min(index, sorted.Count - 1)];
        }
        
        #endregion
    }
}