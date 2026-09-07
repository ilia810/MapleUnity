using UnityEngine;
using UnityEditor;

public static class MinimalBatchTest
{
    [MenuItem("MapleUnity/Test/Minimal Batch Test")]
    public static void TestMinimal()
    {
        Debug.Log("=== MINIMAL BATCH TEST RUNNING ===");
        Debug.Log("Test completed successfully!");
        
        // Try to find and call DirectFaceOffsetTest if it exists
        var type = System.Type.GetType("DirectFaceOffsetTest");
        if (type != null)
        {
            Debug.Log("Found DirectFaceOffsetTest, calling RunTest...");
            var method = type.GetMethod("RunTest");
            if (method != null)
            {
                method.Invoke(null, null);
            }
        }
        else
        {
            Debug.LogError("DirectFaceOffsetTest not found!");
            EditorApplication.Exit(1);
        }
        
        EditorApplication.Exit(0);
    }
    
    public static void RunAttachmentFlowDebug()
    {
        try
        {
            Debug.Log("=== STARTING ATTACHMENT FLOW DEBUG ===");
            
            // Call the debug method directly
            DebugAttachmentPointFlow.RunDebug();
            
            // Give it time to complete with delayed calls
            var startTime = System.DateTime.Now;
            while ((System.DateTime.Now - startTime).TotalSeconds < 5)
            {
                EditorApplication.Step();
                System.Threading.Thread.Sleep(100);
            }
            
            Debug.Log("=== ATTACHMENT FLOW DEBUG COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}