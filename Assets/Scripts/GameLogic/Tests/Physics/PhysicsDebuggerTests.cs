using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Physics;
using System;

namespace MapleClient.GameLogic.Tests.Physics
{
    [TestFixture]
    public class PhysicsDebuggerTests
    {
        private PhysicsDebugger debugger;
        private PhysicsUpdateManager manager;

        [SetUp]
        public void SetUp()
        {
            manager = new PhysicsUpdateManager();
            debugger = new PhysicsDebugger(manager);
        }

        [Test]
        public void Constructor_WithNullManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PhysicsDebugger(null));
        }

        [Test]
        public void StartRecording_SetsIsRecordingTrue()
        {
            debugger.StartRecording();
            Assert.IsTrue(debugger.IsRecording);
        }

        [Test]
        public void StopRecording_SetsIsRecordingFalse()
        {
            debugger.StartRecording();
            debugger.StopRecording();
            Assert.IsFalse(debugger.IsRecording);
        }

        [Test]
        public void StartRecording_ResetsFrameData()
        {
            // Record some frames
            debugger.StartRecording();
            manager.Update(0.016f, null);
            manager.Update(0.016f, null);
            
            // Start recording again
            debugger.StartRecording();
            
            var frameData = debugger.GetFrameData();
            Assert.AreEqual(0, frameData.Count);
        }

        [Test]
        public void GetFrameData_WhenNotRecording_ReturnsEmptyList()
        {
            var frameData = debugger.GetFrameData();
            Assert.IsNotNull(frameData);
            Assert.AreEqual(0, frameData.Count);
        }

        [Test]
        public void GetFrameData_WhenRecording_RecordsFrames()
        {
            debugger.StartRecording();
            
            // Simulate some frames
            manager.Update(0.016f, null);
            manager.Update(0.020f, null);
            manager.Update(0.033f, null);
            
            debugger.StopRecording();
            
            var frameData = debugger.GetFrameData();
            Assert.AreEqual(3, frameData.Count);
        }

        [Test]
        public void GetFrameData_RecordsCorrectDeltaTimes()
        {
            debugger.StartRecording();
            
            manager.Update(0.016f, null);
            manager.Update(0.020f, null);
            
            debugger.StopRecording();
            
            var frameData = debugger.GetFrameData();
            Assert.AreEqual(0.016f, frameData[0].DeltaTime, 0.0001f);
            Assert.AreEqual(0.020f, frameData[1].DeltaTime, 0.0001f);
        }

        [Test]
        public void GetFrameData_RecordsPhysicsStepsPerFrame()
        {
            debugger.StartRecording();
            
            // Small deltaTime = 1 physics step
            manager.Update(0.010f, null);
            // Large deltaTime = multiple physics steps
            manager.Update(0.040f, null);
            
            debugger.StopRecording();
            
            var frameData = debugger.GetFrameData();
            Assert.AreEqual(0, frameData[0].PhysicsStepsThisFrame); // < 16.67ms
            Assert.AreEqual(2, frameData[1].PhysicsStepsThisFrame); // > 33.33ms
        }

        [Test]
        public void GetAverageFrameTime_WithNoFrames_ReturnsZero()
        {
            var avgTime = debugger.GetAverageFrameTime();
            Assert.AreEqual(0f, avgTime);
        }

        [Test]
        public void GetAverageFrameTime_CalculatesCorrectly()
        {
            debugger.StartRecording();
            
            manager.Update(0.010f, null);
            manager.Update(0.020f, null);
            manager.Update(0.030f, null);
            
            debugger.StopRecording();
            
            var avgTime = debugger.GetAverageFrameTime();
            Assert.AreEqual(0.020f, avgTime, 0.0001f);
        }

        [Test]
        public void GetAveragePhysicsStepsPerSecond_CalculatesCorrectly()
        {
            debugger.StartRecording();
            
            // Run for exactly 1 second worth of frames (60 frames at 16.67ms each)
            for (int i = 0; i < 60; i++)
            {
                manager.Update(PhysicsUpdateManager.FIXED_TIMESTEP, null);
            }
            
            debugger.StopRecording();
            
            var stepsPerSecond = debugger.GetAveragePhysicsStepsPerSecond();
            // Should be close to 60 steps per second
            Assert.AreEqual(60f, stepsPerSecond, 1f);
        }

        [Test]
        public void GetFrameTimeDeviation_WithConsistentFrames_ReturnsLowDeviation()
        {
            debugger.StartRecording();
            
            // Consistent frame times
            for (int i = 0; i < 10; i++)
            {
                manager.Update(0.0167f, null);
            }
            
            debugger.StopRecording();
            
            var deviation = debugger.GetFrameTimeDeviation();
            Assert.Less(deviation, 0.001f);
        }

        [Test]
        public void GetFrameTimeDeviation_WithInconsistentFrames_ReturnsHighDeviation()
        {
            debugger.StartRecording();
            
            // Inconsistent frame times
            manager.Update(0.010f, null);
            manager.Update(0.030f, null);
            manager.Update(0.015f, null);
            manager.Update(0.025f, null);
            
            debugger.StopRecording();
            
            var deviation = debugger.GetFrameTimeDeviation();
            Assert.Greater(deviation, 0.005f);
        }

        [Test]
        public void GetDebugReport_GeneratesProperReport()
        {
            debugger.StartRecording();
            
            manager.Update(0.016f, null);
            manager.Update(0.020f, null);
            manager.Update(0.033f, null);
            
            debugger.StopRecording();
            
            var report = debugger.GetDebugReport();
            
            Assert.IsTrue(report.Contains("Physics Debug Report"));
            Assert.IsTrue(report.Contains("Total Frames Recorded: 3"));
            Assert.IsTrue(report.Contains("Average Frame Time:"));
            Assert.IsTrue(report.Contains("Frame Time Deviation:"));
            Assert.IsTrue(report.Contains("Average Physics Steps/Second:"));
        }

        [Test]
        public void MaxRecordedFrames_LimitsFrameData()
        {
            debugger.StartRecording();
            debugger.MaxRecordedFrames = 5;
            
            // Record more than max frames
            for (int i = 0; i < 10; i++)
            {
                manager.Update(0.016f, null);
            }
            
            debugger.StopRecording();
            
            var frameData = debugger.GetFrameData();
            Assert.AreEqual(5, frameData.Count);
        }

        [Test]
        public void GetFrameTimeHistogram_GeneratesCorrectBuckets()
        {
            debugger.StartRecording();
            
            // Add frames in different time buckets
            manager.Update(0.010f, null); // 10ms bucket
            manager.Update(0.015f, null); // 10ms bucket
            manager.Update(0.016f, null); // 16ms bucket
            manager.Update(0.017f, null); // 16ms bucket
            manager.Update(0.025f, null); // 20ms bucket
            manager.Update(0.035f, null); // 30ms bucket
            
            debugger.StopRecording();
            
            var histogram = debugger.GetFrameTimeHistogram();
            
            Assert.AreEqual(2, histogram[10]); // 10-15ms
            Assert.AreEqual(2, histogram[16]); // 16-20ms
            Assert.AreEqual(1, histogram[20]); // 20-30ms
            Assert.AreEqual(1, histogram[30]); // 30-40ms
        }
    }
}