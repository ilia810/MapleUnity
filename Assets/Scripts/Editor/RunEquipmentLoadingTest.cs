using UnityEngine;
using UnityEditor;

public static class RunEquipmentLoadingTest
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Starting Equipment Loading Test ===");
            
            // Execute the test directly since we know the class exists
            TestEquipmentLoading.RunTest();
            
            Debug.Log("=== Equipment Loading Test Complete ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Equipment loading test failed: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
}