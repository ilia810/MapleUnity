using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Threading;

public class RunRuntimeScaleTest
{
    public static void RunTest()
    {
        Debug.Log("Starting runtime scale test");
        
        try
        {
            // Open the main scene
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            
            // Create test runner GameObject
            var testRunner = new GameObject("RuntimeScaleTestRunner");
            
            // Add the test component dynamically
            var testType = System.Type.GetType("MapleClient.RuntimeScaleTest");
            if (testType == null)
            {
                Debug.LogError("Could not find RuntimeScaleTest type");
                EditorApplication.Exit(1);
                return;
            }
            
            testRunner.AddComponent(testType);
            
            // Save the scene temporarily
            var scenePath = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            
            // Enter play mode
            EditorApplication.isPlaying = true;
            
            // Wait for test to complete (max 30 seconds)
            float startTime = Time.realtimeSinceStartup;
            while (EditorApplication.isPlaying && (Time.realtimeSinceStartup - startTime) < 30f)
            {
                Thread.Sleep(100);
            }
            
            // Exit
            Debug.Log("Runtime test completed");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e}");
            EditorApplication.Exit(1);
        }
    }
}