using NUnit.Framework;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;
using System.Collections.Generic;

namespace MapleClient.Tests.GameLogic
{
    [TestFixture]
    public class PhysicsUpdateManagerTests
    {
        private PhysicsUpdateManager physicsManager;
        private MapData testMapData;
        
        [SetUp]
        public void SetUp()
        {
            physicsManager = new PhysicsUpdateManager();
            testMapData = new MapData
            {
                MapId = 100000000,
                Name = "Test Map",
                Platforms = new System.Collections.Generic.List<Platform>()
            };
        }
        
        [TearDown]
        public void TearDown()
        {
            physicsManager.Reset();
        }
        
        [Test]
        public void FixedTimestep_ShouldBe_60FPS()
        {
            Assert.AreEqual(1f / 60f, PhysicsUpdateManager.FIXED_TIMESTEP, 0.0001f);
            Assert.AreEqual(60, PhysicsUpdateManager.TARGET_FPS);
        }
        
        [Test]
        public void RegisterPhysicsObject_ShouldReturnUniqueId()
        {
            var obj1 = new MockPhysicsObject { IsPhysicsActive = true };
            var obj2 = new MockPhysicsObject { IsPhysicsActive = true };
            
            int id1 = physicsManager.RegisterPhysicsObject(obj1);
            int id2 = physicsManager.RegisterPhysicsObject(obj2);
            
            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(2, physicsManager.ActiveObjectCount);
        }
        
        [Test]
        public void UnregisterPhysicsObject_ShouldRemoveFromActive()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            int id = physicsManager.RegisterPhysicsObject(obj);
            
            Assert.AreEqual(1, physicsManager.ActiveObjectCount);
            
            physicsManager.UnregisterPhysicsObject(id);
            
            Assert.AreEqual(0, physicsManager.ActiveObjectCount);
        }
        
        [Test]
        public void Update_WithExactTimestep_ShouldCallPhysicsOnce()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            physicsManager.Update(PhysicsUpdateManager.FIXED_TIMESTEP, testMapData);
            
            Assert.AreEqual(1, obj.UpdateCount);
            Assert.AreEqual(0f, physicsManager.Accumulator, 0.0001f);
        }
        
        [Test]
        public void Update_WithDoubleTimestep_ShouldCallPhysicsTwice()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            physicsManager.Update(PhysicsUpdateManager.FIXED_TIMESTEP * 2f, testMapData);
            
            Assert.AreEqual(2, obj.UpdateCount);
            Assert.AreEqual(0f, physicsManager.Accumulator, 0.0001f);
        }
        
        [Test]
        public void Update_WithPartialTimestep_ShouldAccumulate()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            float halfTimestep = PhysicsUpdateManager.FIXED_TIMESTEP / 2f;
            
            physicsManager.Update(halfTimestep, testMapData);
            Assert.AreEqual(0, obj.UpdateCount);
            Assert.AreEqual(halfTimestep, physicsManager.Accumulator, 0.0001f);
            
            physicsManager.Update(halfTimestep, testMapData);
            Assert.AreEqual(1, obj.UpdateCount);
            Assert.AreEqual(0f, physicsManager.Accumulator, 0.0001f);
        }
        
        [Test]
        public void Update_WithVariableFramerate_ShouldMaintain60FPSPhysics()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            // Simulate 1 second of variable framerate
            float totalTime = 0f;
            float[] frameTimes = { 0.016f, 0.020f, 0.012f, 0.018f, 0.022f, 0.015f }; // Variable frame times
            
            foreach (float frameTime in frameTimes)
            {
                physicsManager.Update(frameTime, testMapData);
                totalTime += frameTime;
            }
            
            // After ~0.103 seconds, we should have ~6 physics steps (at 60 FPS)
            int expectedSteps = (int)(totalTime / PhysicsUpdateManager.FIXED_TIMESTEP);
            Assert.AreEqual(expectedSteps, obj.UpdateCount);
        }
        
        [Test]
        public void Update_WithLargeDeltaTime_ShouldClampSteps()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            // Simulate a huge frame spike (300ms)
            physicsManager.Update(0.3f, testMapData);
            
            // Should be clamped to prevent spiral of death
            Assert.LessOrEqual(obj.UpdateCount, 4); // Max 4 steps per frame
        }
        
        [Test]
        public void GetInterpolationFactor_ShouldReturnCorrectValue()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            float partialTime = PhysicsUpdateManager.FIXED_TIMESTEP * 0.75f;
            physicsManager.Update(partialTime, testMapData);
            
            float interpolation = physicsManager.GetInterpolationFactor();
            Assert.AreEqual(0.75f, interpolation, 0.01f);
        }
        
        [Test]
        public void InactiveObjects_ShouldNotReceiveUpdates()
        {
            var activeObj = new MockPhysicsObject { IsPhysicsActive = true };
            var inactiveObj = new MockPhysicsObject { IsPhysicsActive = false };
            
            physicsManager.RegisterPhysicsObject(activeObj);
            physicsManager.RegisterPhysicsObject(inactiveObj);
            
            physicsManager.Update(PhysicsUpdateManager.FIXED_TIMESTEP, testMapData);
            
            Assert.AreEqual(1, activeObj.UpdateCount);
            Assert.AreEqual(0, inactiveObj.UpdateCount);
        }
        
        [Test]
        public void PhysicsStepCompleted_Event_ShouldFire()
        {
            var obj = new MockPhysicsObject { IsPhysicsActive = true };
            physicsManager.RegisterPhysicsObject(obj);
            
            long stepCount = 0;
            physicsManager.PhysicsStepCompleted += (count) => stepCount = count;
            
            physicsManager.Update(PhysicsUpdateManager.FIXED_TIMESTEP * 3, testMapData);
            
            Assert.AreEqual(3, stepCount);
        }
        
        [Test]
        public void GetDebugStats_ShouldReturnCorrectMetrics()
        {
            var obj1 = new MockPhysicsObject { IsPhysicsActive = true };
            var obj2 = new MockPhysicsObject { IsPhysicsActive = false };
            
            physicsManager.RegisterPhysicsObject(obj1);
            physicsManager.RegisterPhysicsObject(obj2);
            
            physicsManager.Update(PhysicsUpdateManager.FIXED_TIMESTEP * 2.5f, testMapData);
            
            var stats = physicsManager.GetDebugStats();
            
            Assert.AreEqual(1, stats.TotalFrames);
            Assert.AreEqual(2, stats.TotalPhysicsSteps);
            Assert.AreEqual(1, stats.ActiveObjectCount);
            Assert.AreEqual(2, stats.TotalObjectCount);
            Assert.Greater(stats.Accumulator, 0f); // Should have leftover time
        }
        
        // Mock physics object for testing
        private class MockPhysicsObject : IPhysicsObject
        {
            public int UpdateCount { get; private set; }
            public int PhysicsId => GetHashCode();
            public Vector2 Position { get; set; }
            public Vector2 Velocity { get; set; }
            public bool UseGravity => true;
            public bool IsPhysicsActive { get; set; }
            
            public void UpdatePhysics(float fixedDeltaTime, MapData mapData)
            {
                UpdateCount++;
                // Verify we're always called with fixed timestep
                Assert.AreEqual(PhysicsUpdateManager.FIXED_TIMESTEP, fixedDeltaTime, 0.0001f);
            }
            
            public void OnTerrainCollision(Vector2 collisionPoint, Vector2 collisionNormal)
            {
                // Not used in tests
            }
        }
    }
}