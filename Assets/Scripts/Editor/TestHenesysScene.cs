using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.IO;
// using Unity.EditorCoroutines.Editor; // Commented out to fix compilation

public class TestHenesysScene : MonoBehaviour
{
    private static string logPath = "C:\\Users\\me\\MapleUnity\\debug-log.txt";
    private static float testDuration = 5f;
    private static GameObject player;
    
    [MenuItem("MapleUnity/Test Henesys Scene")]
    public static void RunHenesysTest()
    {
        Debug.Log("[TEST] Starting Henesys scene test...");
        
        // Clear previous log
        File.WriteAllText(logPath, "[TEST] Henesys Scene Test Starting...\n");
        
        // Open the henesys scene
        EditorSceneManager.OpenScene("Assets/henesys.unity");
        
        // Start play mode
        EditorApplication.isPlaying = true;
        
        // Start coroutine to run tests
        // EditorCoroutineUtility.StartCoroutine(RunTestSequence(), new object()); // Commented out to fix compilation
        Debug.LogError("[TEST] EditorCoroutines package is not available - cannot run test sequence");
    }
    
    private static IEnumerator RunTestSequence()
    {
        // Wait for play mode to fully start
        yield return new WaitForSeconds(1f);
        
        // Find the player
        player = GameObject.Find("Player");
        if (player == null)
        {
            LogToFile("[TEST] ERROR: Could not find Player GameObject!");
            EditorApplication.isPlaying = false;
            yield break;
        }
        
        // Log initial spawn position
        LogToFile($"[TEST] Player found at position: {player.transform.position}");
        LogToFile($"[TEST] Player spawn Y: {player.transform.position.y:F2} (expected around -1.5)");
        
        // Test if player is on ground after spawn
        yield return new WaitForSeconds(2f);
        LogToFile($"[TEST] Player position after 2 seconds: {player.transform.position}");
        
        // Get SimplePlayerController to check grounded state
        var playerController = player.GetComponent("SimplePlayerController");
        if (playerController != null)
        {
            // Use reflection to check grounded state
            var groundedField = playerController.GetType().GetField("isGrounded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (groundedField != null)
            {
                bool isGrounded = (bool)groundedField.GetValue(playerController);
                LogToFile($"[TEST] Player grounded state: {isGrounded}");
            }
        }
        
        // Test horizontal movement boundaries
        LogToFile("[TEST] Testing horizontal movement boundaries...");
        
        // Move player to the right
        player.transform.position = new Vector3(5f, player.transform.position.y, 0);
        yield return new WaitForSeconds(1f);
        LogToFile($"[TEST] Player at X=5: {player.transform.position}");
        
        // Move further right (should still be on platform)
        player.transform.position = new Vector3(40f, player.transform.position.y, 0);
        yield return new WaitForSeconds(1f);
        LogToFile($"[TEST] Player at X=40: {player.transform.position}");
        
        // Move to left boundary
        player.transform.position = new Vector3(-40f, player.transform.position.y, 0);
        yield return new WaitForSeconds(1f);
        LogToFile($"[TEST] Player at X=-40: {player.transform.position}");
        
        // Check if there are any foothold collision logs
        CheckFootholdLogs();
        
        // End test
        LogToFile("[TEST] Test completed. Stopping play mode...");
        EditorApplication.isPlaying = false;
    }
    
    private static void LogToFile(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logPath, message + "\n");
    }
    
    private static void CheckFootholdLogs()
    {
        // Check Unity console for foothold logs
        var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
        if (logEntries != null)
        {
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var getCountMethod = logEntries.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            
            if (getCountMethod != null)
            {
                int count = (int)getCountMethod.Invoke(null, null);
                LogToFile($"[TEST] Total console log entries: {count}");
            }
        }
    }
}