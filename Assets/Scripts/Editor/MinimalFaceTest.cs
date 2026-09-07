using UnityEngine;
using UnityEditor;

public class MinimalFaceTest
{
    [MenuItem("MapleUnity/Test/Minimal Face Test")]
    public static void RunTest()
    {
        Debug.Log("=== Minimal Face Offset Test ===");
        
        // Mock attachment point values from MockNxFile.cs
        Vector2 bodyNeck = new Vector2(-7, -31);
        Vector2 headNeck = new Vector2(14, 19);
        Vector2 headBrow = new Vector2(13, -2);
        
        // C++ formula: facePos = body.neck - head.neck + head.brow
        Vector2 faceOffsetPixels = bodyNeck - headNeck + headBrow;
        
        Debug.Log($"Attachment Points:");
        Debug.Log($"  Body neck: {bodyNeck}");
        Debug.Log($"  Head neck: {headNeck}");
        Debug.Log($"  Head brow: {headBrow}");
        Debug.Log($"Calculated face offset: ({faceOffsetPixels.x}, {faceOffsetPixels.y}) pixels");
        Debug.Log($"Expected C++ offset: (-8, -52) pixels");
        
        if (Mathf.Approximately(faceOffsetPixels.x, -8f) && Mathf.Approximately(faceOffsetPixels.y, -52f))
        {
            Debug.Log("SUCCESS: Mock data produces correct offset!");
        }
        else
        {
            Debug.LogError("FAILURE: Mock data produces incorrect offset!");
        }
        
        EditorApplication.Exit(0);
    }
}

// Create a static initializer to run the test
[InitializeOnLoad]
public static class MinimalFaceTestRunner
{
    static MinimalFaceTestRunner()
    {
        if (System.Environment.GetCommandLineArgs().Length > 0)
        {
            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg.Contains("MinimalFaceTest"))
                {
                    EditorApplication.delayCall += MinimalFaceTest.RunTest;
                    break;
                }
            }
        }
    }
}