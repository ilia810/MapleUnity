using UnityEngine;
using UnityEditor;

public static class RunAttachmentTest
{
    public static void Execute()
    {
        Debug.Log("=== STARTING ATTACHMENT TEST EXECUTION ===");
        
        try
        {
            // Call the actual test
            CharacterAttachmentTest.RunBatchAttachmentTest();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to run attachment test: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}