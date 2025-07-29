using UnityEngine;
using UnityEditor;
using MapleClient.GameView;

public class TestCompilation
{
    [MenuItem("MapleUnity/Test Compilation")]
    public static void TestDebugWrapperCompilation()
    {
        // Test that our Debug wrapper works
        Debug.Log("Testing Debug.Log wrapper");
        Debug.LogWarning("Testing Debug.LogWarning wrapper");
        Debug.LogError("Testing Debug.LogError wrapper");
        
        // Test drawing methods
        Debug.DrawLine(Vector3.zero, Vector3.one, Color.red);
        Debug.DrawRay(Vector3.zero, Vector3.up, Color.green);
        
        UnityEngine.Debug.Log("Compilation test complete - Debug wrapper is working!");
    }
}