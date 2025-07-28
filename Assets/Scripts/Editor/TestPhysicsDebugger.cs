using UnityEngine;
using UnityEditor;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Physics;

public class TestPhysicsDebugger : EditorWindow
{
    [MenuItem("MapleStory Debug/Test Physics Debugger")]
    static void ShowWindow()
    {
        GetWindow<TestPhysicsDebugger>("Physics Debugger Test");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Run Physics Debugger Test"))
        {
            TestDebugger();
        }
    }

    void TestDebugger()
    {
        Debug.Log("=== Testing Physics Debugger ===");

        var manager = new PhysicsUpdateManager();
        var debugger = new PhysicsDebugger(manager);

        // Test 1: Basic recording
        Debug.Log("Test 1: Basic recording");
        debugger.StartRecording();
        
        // Simulate some frames
        manager.Update(0.016f, null); // Perfect frame
        manager.Update(0.020f, null); // Slightly slow
        manager.Update(0.033f, null); // Skip frame
        manager.Update(0.016f, null); // Perfect frame
        
        debugger.StopRecording();
        
        var frameData = debugger.GetFrameData();
        Debug.Log($"Recorded {frameData.Count} frames");
        Debug.Log($"Average frame time: {debugger.GetAverageFrameTime() * 1000:F2}ms");
        Debug.Log($"Frame time deviation: {debugger.GetFrameTimeDeviation() * 1000:F2}ms");
        Debug.Log($"Physics steps per second: {debugger.GetAveragePhysicsStepsPerSecond():F1}");

        // Test 2: Debug report
        Debug.Log("\nTest 2: Debug report");
        Debug.Log(debugger.GetDebugReport());

        // Test 3: Frame time histogram
        Debug.Log("\nTest 3: Frame time histogram");
        var histogram = debugger.GetFrameTimeHistogram();
        foreach (var bucket in histogram)
        {
            Debug.Log($"  {bucket.Key}-{bucket.Key + 10}ms: {bucket.Value} frames");
        }

        // Test 4: Stress test with many frames
        Debug.Log("\nTest 4: Stress test");
        debugger.StartRecording();
        debugger.MaxRecordedFrames = 100;
        
        // Simulate 200 frames with variable timing
        for (int i = 0; i < 200; i++)
        {
            float deltaTime = 0.016f + Random.Range(-0.005f, 0.010f);
            manager.Update(deltaTime, null);
        }
        
        debugger.StopRecording();
        Debug.Log($"After stress test - recorded frames: {debugger.GetFrameData().Count}");
        Debug.Log($"Physics stats: {manager.GetDebugStats().StepsPerSecond:F1} steps/sec");

        Debug.Log("\n=== Physics Debugger Test Complete ===");
    }
}