using UnityEngine;
using UnityEditor;

public static class RunAttachmentFlowTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== STARTING ATTACHMENT FLOW TEST ===");
            
            // Call the debug method directly
            DebugAttachmentPointFlow.RunDebug();
            
            // Give it time to complete with delayed calls
            var startTime = System.DateTime.Now;
            while ((System.DateTime.Now - startTime).TotalSeconds < 5)
            {
                EditorApplication.Step();
                System.Threading.Thread.Sleep(100);
            }
            
            Debug.Log("=== ATTACHMENT FLOW TEST COMPLETED ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}