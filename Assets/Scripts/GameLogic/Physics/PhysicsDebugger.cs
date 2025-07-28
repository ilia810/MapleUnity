using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapleClient.GameLogic.Core;

namespace MapleClient.GameLogic.Physics
{
    /// <summary>
    /// Debug tool for monitoring and analyzing physics system performance.
    /// Provides timing verification and frame analysis for the fixed timestep physics.
    /// </summary>
    public class PhysicsDebugger
    {
        private readonly PhysicsUpdateManager physicsManager;
        private readonly List<FrameDebugData> frameData;
        private bool isRecording;
        private long lastPhysicsSteps;
        private long lastFrameCount;
        private int maxRecordedFrames = 1000;
        private float pendingDeltaTime;

        public bool IsRecording => isRecording;
        public int MaxRecordedFrames
        {
            get => maxRecordedFrames;
            set => maxRecordedFrames = Math.Max(1, value);
        }

        public PhysicsDebugger(PhysicsUpdateManager manager)
        {
            physicsManager = manager ?? throw new ArgumentNullException(nameof(manager));
            frameData = new List<FrameDebugData>();
            
            // Subscribe to physics events
            physicsManager.PhysicsStepCompleted += OnPhysicsStepCompleted;
            physicsManager.FrameTimeExceeded += OnFrameTimeExceeded;
            physicsManager.FrameCompleted += OnFrameCompleted;
        }

        /// <summary>
        /// Start recording frame data for analysis
        /// </summary>
        public void StartRecording()
        {
            isRecording = true;
            frameData.Clear();
            lastPhysicsSteps = physicsManager.TotalPhysicsSteps;
            lastFrameCount = physicsManager.TotalFrames;
        }

        /// <summary>
        /// Stop recording frame data
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
        }

        /// <summary>
        /// Get recorded frame data
        /// </summary>
        public List<FrameDebugData> GetFrameData()
        {
            return new List<FrameDebugData>(frameData);
        }

        /// <summary>
        /// Calculate average frame time from recorded data
        /// </summary>
        public float GetAverageFrameTime()
        {
            if (frameData.Count == 0)
                return 0f;

            return frameData.Average(f => f.DeltaTime);
        }

        /// <summary>
        /// Calculate frame time standard deviation
        /// </summary>
        public float GetFrameTimeDeviation()
        {
            if (frameData.Count < 2)
                return 0f;

            float avg = GetAverageFrameTime();
            float sumSquaredDiff = frameData.Sum(f => (f.DeltaTime - avg) * (f.DeltaTime - avg));
            return (float)Math.Sqrt(sumSquaredDiff / frameData.Count);
        }

        /// <summary>
        /// Calculate average physics steps per second
        /// </summary>
        public float GetAveragePhysicsStepsPerSecond()
        {
            if (frameData.Count == 0)
                return 0f;

            float totalTime = frameData.Sum(f => f.DeltaTime);
            int totalSteps = frameData.Sum(f => f.PhysicsStepsThisFrame);

            if (totalTime <= 0)
                return 0f;

            return totalSteps / totalTime;
        }

        /// <summary>
        /// Generate a text report of physics performance
        /// </summary>
        public string GetDebugReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== Physics Debug Report =====");
            sb.AppendLine($"Total Frames Recorded: {frameData.Count}");
            
            if (frameData.Count > 0)
            {
                sb.AppendLine($"Average Frame Time: {GetAverageFrameTime() * 1000:F2}ms");
                sb.AppendLine($"Frame Time Deviation: {GetFrameTimeDeviation() * 1000:F2}ms");
                sb.AppendLine($"Average Physics Steps/Second: {GetAveragePhysicsStepsPerSecond():F1}");
                
                var histogram = GetFrameTimeHistogram();
                sb.AppendLine("\nFrame Time Distribution:");
                foreach (var bucket in histogram.OrderBy(kvp => kvp.Key))
                {
                    sb.AppendLine($"  {bucket.Key}-{bucket.Key + 10}ms: {bucket.Value} frames");
                }
            }
            
            sb.AppendLine($"\nCurrent Physics Stats:");
            var stats = physicsManager.GetDebugStats();
            sb.AppendLine($"  Total Frames: {stats.TotalFrames}");
            sb.AppendLine($"  Total Physics Steps: {stats.TotalPhysicsSteps}");
            sb.AppendLine($"  Active Objects: {stats.ActiveObjectCount}");
            sb.AppendLine($"  Current Accumulator: {stats.Accumulator:F4}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Get frame time histogram in 10ms buckets
        /// </summary>
        public Dictionary<int, int> GetFrameTimeHistogram()
        {
            var histogram = new Dictionary<int, int>();
            
            foreach (var frame in frameData)
            {
                int bucket = (int)(frame.DeltaTime * 1000) / 10 * 10;
                if (frame.DeltaTime * 1000 >= 16 && frame.DeltaTime * 1000 < 20)
                {
                    bucket = 16; // Special bucket for target frame time
                }
                
                if (!histogram.ContainsKey(bucket))
                    histogram[bucket] = 0;
                histogram[bucket]++;
            }
            
            return histogram;
        }

        private void OnPhysicsStepCompleted(long totalSteps)
        {
            if (!isRecording)
                return;

            // This is called after each physics step, but we want to record per frame
            // We'll track this in the Update method instead
        }

        private void OnFrameTimeExceeded(float frameTime)
        {
            // Log warning about frame time exceeding target
            // In a real implementation, this would go to a logging system
        }
        
        private void OnFrameCompleted(float deltaTime, int stepsThisFrame)
        {
            if (!isRecording)
                return;
                
            frameData.Add(new FrameDebugData
            {
                FrameNumber = physicsManager.TotalFrames,
                DeltaTime = deltaTime,
                PhysicsStepsThisFrame = stepsThisFrame,
                AccumulatorValue = physicsManager.Accumulator,
                ActiveObjectCount = physicsManager.ActiveObjectCount
            });
            
            // Limit recorded frames
            if (frameData.Count > maxRecordedFrames)
            {
                frameData.RemoveAt(0);
            }
        }

        /// <summary>
        /// Call this after each PhysicsUpdateManager.Update() to record frame data
        /// </summary>
        public void RecordFrame(float deltaTime)
        {
            if (!isRecording)
                return;

            var currentSteps = physicsManager.TotalPhysicsSteps;
            var stepsThisFrame = (int)(currentSteps - lastPhysicsSteps);
            
            frameData.Add(new FrameDebugData
            {
                FrameNumber = physicsManager.TotalFrames,
                DeltaTime = deltaTime,
                PhysicsStepsThisFrame = stepsThisFrame,
                AccumulatorValue = physicsManager.Accumulator,
                ActiveObjectCount = physicsManager.ActiveObjectCount
            });

            lastPhysicsSteps = currentSteps;

            // Limit recorded frames
            if (frameData.Count > maxRecordedFrames)
            {
                frameData.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Data captured for each frame during debugging
    /// </summary>
    public struct FrameDebugData
    {
        public long FrameNumber;
        public float DeltaTime;
        public int PhysicsStepsThisFrame;
        public float AccumulatorValue;
        public int ActiveObjectCount;
    }
}