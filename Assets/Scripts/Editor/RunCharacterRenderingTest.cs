using UnityEngine;
using UnityEditor;

public static class RunCharacterRenderingTest
{
    public static void Execute()
    {
        Debug.Log("[BATCH_TEST] Starting character rendering test...");
        
        try
        {
            // Execute the menu item
            EditorApplication.ExecuteMenuItem("MapleUnity/Tests/Test and Fix Character Rendering");
            
            Debug.Log("[BATCH_TEST] Test executed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BATCH_TEST] Failed to execute test: {e.Message}");
            EditorApplication.Exit(1);
            return;
        }
        
        // Note: The test itself handles EditorApplication.Exit
        // We don't exit here because the test runs asynchronously
    }
}