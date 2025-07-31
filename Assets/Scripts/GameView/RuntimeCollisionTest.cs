using UnityEngine;
using System.Collections;
using System.IO;

namespace MapleClient.GameView
{
    public class RuntimeCollisionTest : MonoBehaviour
    {
        private string logPath = @"C:\Users\me\MapleUnity\debug-log.txt";
        private GameObject player;
        private float testStartTime;
        private bool testComplete = false;
        
        void Start()
        {
            Debug.Log("[RUNTIME_TEST] Starting collision test...");
            File.AppendAllText(logPath, "\n\n=== RUNTIME COLLISION TEST ===\n");
            testStartTime = Time.time;
            StartCoroutine(RunTests());
        }
        
        IEnumerator RunTests()
        {
            // Wait a frame for everything to initialize
            yield return null;
            
            // Find player
            player = GameObject.Find("Player");
            if (player == null)
            {
                LogTest("ERROR: Could not find Player GameObject!");
                yield break;
            }
            
            LogTest($"Player found at position: {player.transform.position}");
            LogTest($"Player spawn Y: {player.transform.position.y:F2} (expected around -1.5)");
            
            // Wait for physics to settle
            yield return new WaitForSeconds(1f);
            
            LogTest($"Player position after 1 second: {player.transform.position}");
            LogTest($"Player Y after physics: {player.transform.position.y:F2} (should be around -2.0 if on ground)");
            
            // Check SimplePlayerController state
            var playerController = player.GetComponent<SimplePlayerController>();
            if (playerController != null)
            {
                LogTest("SimplePlayerController found on player");
            }
            
            // Test platform boundaries
            LogTest("\n--- Testing Platform Boundaries ---");
            
            // Save original position
            Vector3 originalPos = player.transform.position;
            
            // Test right boundary
            player.transform.position = new Vector3(30f, originalPos.y, 0);
            yield return new WaitForSeconds(0.5f);
            LogTest($"Moved to X=30: Y={player.transform.position.y:F2} (should stay on platform)");
            
            player.transform.position = new Vector3(45f, originalPos.y, 0);
            yield return new WaitForSeconds(0.5f);
            LogTest($"Moved to X=45: Y={player.transform.position.y:F2} (should stay on platform)");
            
            // Test left boundary
            player.transform.position = new Vector3(-30f, originalPos.y, 0);
            yield return new WaitForSeconds(0.5f);
            LogTest($"Moved to X=-30: Y={player.transform.position.y:F2} (should stay on platform)");
            
            player.transform.position = new Vector3(-45f, originalPos.y, 0);
            yield return new WaitForSeconds(0.5f);
            LogTest($"Moved to X=-45: Y={player.transform.position.y:F2} (should stay on platform)");
            
            // Test far boundary (should now work with extended platform)
            player.transform.position = new Vector3(49f, originalPos.y, 0);
            yield return new WaitForSeconds(1f);
            LogTest($"Moved to X=49: Y={player.transform.position.y:F2} (near edge, should stay on platform)");
            
            // Return to center
            player.transform.position = originalPos;
            
            LogTest("\n--- Test Complete ---");
            LogTest($"Total test duration: {Time.time - testStartTime:F1} seconds");
            
            // Check for foothold logs in console
            CheckConsoleLogs();
            
            testComplete = true;
        }
        
        void CheckConsoleLogs()
        {
            // Count foothold collision logs
            int footholdLogCount = 0;
            string consoleOutput = "";
            
            // Note: In a real test we'd capture console output properly
            LogTest($"\nNote: Check Unity console for [FOOTHOLD_COLLISION] logs");
            LogTest("If collision is working, you should see logs when the player moves/falls");
        }
        
        void LogTest(string message)
        {
            string logMessage = $"[RUNTIME_TEST] {message}";
            Debug.Log(logMessage);
            File.AppendAllText(logPath, logMessage + "\n");
        }
        
        void OnDestroy()
        {
            if (!testComplete)
            {
                LogTest("Test interrupted before completion");
            }
        }
    }
}