using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

public static class CompleteCharacterRenderingTest
{
    private static string logPath = @"C:\Users\me\MapleUnity\complete-character-test.log";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(logPath, "=== COMPLETE CHARACTER RENDERING TEST ===\n");
            File.AppendAllText(logPath, $"Time: {DateTime.Now}\n\n");
            
            // Load the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(logPath, $"Scene loaded: {scene.name}\n");
            
            // Find Player GameObject
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(logPath, "[ERROR] Player GameObject not found in scene\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(logPath, $"\n[INFO] Player found at position: {playerGO.transform.position}\n");
            File.AppendAllText(logPath, $"Player scale: {playerGO.transform.localScale}\n");
            
            // Initialize GameManager if needed
            var gameManager = GameObject.Find("GameManager");
            if (gameManager != null)
            {
                var components = gameManager.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        // Try to call Start method
                        var startMethod = comp.GetType().GetMethod("Start", 
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (startMethod != null)
                        {
                            try
                            {
                                startMethod.Invoke(comp, null);
                                File.AppendAllText(logPath, $"[INFO] Called Start on {comp.GetType().Name}\n");
                            }
                            catch (Exception e)
                            {
                                File.AppendAllText(logPath, $"[WARNING] Failed to call Start: {e.Message}\n");
                            }
                        }
                    }
                }
            }
            
            // TEST 1: Check sprite renderers
            File.AppendAllText(logPath, "\n=== TEST 1: SPRITE RENDERER ANALYSIS ===\n");
            var renderers = playerGO.GetComponentsInChildren<SpriteRenderer>(true);
            File.AppendAllText(logPath, $"Total sprite renderers found: {renderers.Length}\n\n");
            
            foreach (var renderer in renderers)
            {
                File.AppendAllText(logPath, $"[{renderer.name}]\n");
                File.AppendAllText(logPath, $"  - GameObject Active: {renderer.gameObject.activeSelf}\n");
                File.AppendAllText(logPath, $"  - Component Enabled: {renderer.enabled}\n");
                File.AppendAllText(logPath, $"  - Has Sprite: {renderer.sprite != null}\n");
                if (renderer.sprite != null)
                {
                    File.AppendAllText(logPath, $"  - Sprite Name: {renderer.sprite.name}\n");
                }
                File.AppendAllText(logPath, $"  - Local Position: {renderer.transform.localPosition}\n");
                File.AppendAllText(logPath, $"  - World Position: {renderer.transform.position}\n");
                File.AppendAllText(logPath, $"  - Scale: {renderer.transform.localScale}\n");
                File.AppendAllText(logPath, $"  - FlipX: {renderer.flipX}\n");
                File.AppendAllText(logPath, $"  - Sorting Order: {renderer.sortingOrder}\n\n");
            }
            
            // TEST 2: Check body part alignment
            File.AppendAllText(logPath, "=== TEST 2: BODY PART ALIGNMENT ===\n");
            
            var bodyRenderer = renderers.FirstOrDefault(r => r.name == "Body");
            var headRenderer = renderers.FirstOrDefault(r => r.name == "Head");
            var armRenderer = renderers.FirstOrDefault(r => r.name == "Arm");
            var faceRenderer = renderers.FirstOrDefault(r => r.name == "Face");
            
            if (bodyRenderer != null && headRenderer != null)
            {
                float headYOffset = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
                File.AppendAllText(logPath, $"\nHead Y offset from Body: {headYOffset:F3} units\n");
                File.AppendAllText(logPath, $"Expected: ~0.45 units\n");
                File.AppendAllText(logPath, $"Status: {(headYOffset > 0.4f && headYOffset < 0.5f ? "PASS - Head correctly above body" : "FAIL - Head position incorrect")}\n");
            }
            else
            {
                File.AppendAllText(logPath, $"\n[WARNING] Missing renderers - Body: {bodyRenderer != null}, Head: {headRenderer != null}\n");
            }
            
            if (bodyRenderer != null && armRenderer != null)
            {
                float armYOffset = armRenderer.transform.position.y - bodyRenderer.transform.position.y;
                File.AppendAllText(logPath, $"\nArm Y offset from Body: {armYOffset:F3} units\n");
                File.AppendAllText(logPath, $"Expected: 0 units (same level)\n");
                File.AppendAllText(logPath, $"Status: {(Math.Abs(armYOffset) < 0.01f ? "PASS - Arm aligned with body" : "FAIL - Arm misaligned")}\n");
            }
            
            if (headRenderer != null && faceRenderer != null)
            {
                float faceXOffset = faceRenderer.transform.position.x - headRenderer.transform.position.x;
                float faceYOffset = faceRenderer.transform.position.y - headRenderer.transform.position.y;
                File.AppendAllText(logPath, $"\nFace offset from Head: ({faceXOffset:F3}, {faceYOffset:F3})\n");
                File.AppendAllText(logPath, $"Expected: (0, 0)\n");
                File.AppendAllText(logPath, $"Status: {(Math.Abs(faceXOffset) < 0.01f && Math.Abs(faceYOffset) < 0.01f ? "PASS - Face aligned with head" : "FAIL - Face misaligned")}\n");
            }
            
            // TEST 3: Check scale-based flipping
            File.AppendAllText(logPath, "\n=== TEST 3: SCALE-BASED FLIPPING TEST ===\n");
            File.AppendAllText(logPath, $"Player Scale: {playerGO.transform.localScale}\n");
            File.AppendAllText(logPath, $"Expected for facing right: (1, 1, 1)\n");
            File.AppendAllText(logPath, $"Expected for facing left: (-1, 1, 1)\n");
            
            bool facingRight = playerGO.transform.localScale.x > 0;
            File.AppendAllText(logPath, $"Current facing direction: {(facingRight ? "RIGHT" : "LEFT")}\n");
            
            // Check that no sprites have flipX = true when using scale-based flipping
            bool anyFlipped = false;
            foreach (var renderer in renderers.Where(r => r.sprite != null))
            {
                if (renderer.flipX)
                {
                    anyFlipped = true;
                    File.AppendAllText(logPath, $"[WARNING] {renderer.name} has flipX=true (should use scale instead)\n");
                }
            }
            
            File.AppendAllText(logPath, $"\nScale-based flipping status: {(!anyFlipped ? "PASS - Using scale correctly" : "FAIL - Some sprites using flipX")}\n");
            
            // TEST 4: Simulate direction changes
            File.AppendAllText(logPath, "\n=== TEST 4: DIRECTION CHANGE SIMULATION ===\n");
            
            // Test facing left
            playerGO.transform.localScale = new Vector3(-1, 1, 1);
            File.AppendAllText(logPath, "\nSet player to face LEFT (scale.x = -1)\n");
            File.AppendAllText(logPath, "Body part positions after facing left:\n");
            
            foreach (var renderer in new[] { bodyRenderer, headRenderer, armRenderer, faceRenderer }.Where(r => r != null))
            {
                File.AppendAllText(logPath, $"  - {renderer.name}: {renderer.transform.position}\n");
            }
            
            // Test facing right
            playerGO.transform.localScale = new Vector3(1, 1, 1);
            File.AppendAllText(logPath, "\nSet player to face RIGHT (scale.x = 1)\n");
            File.AppendAllText(logPath, "Body part positions after facing right:\n");
            
            foreach (var renderer in new[] { bodyRenderer, headRenderer, armRenderer, faceRenderer }.Where(r => r != null))
            {
                File.AppendAllText(logPath, $"  - {renderer.name}: {renderer.transform.position}\n");
            }
            
            // TEST 5: Check character renderer component
            File.AppendAllText(logPath, "\n=== TEST 5: CHARACTER RENDERER COMPONENT ===\n");
            
            var characterRendererComponent = playerGO.GetComponent("MapleCharacterRenderer");
            if (characterRendererComponent != null)
            {
                File.AppendAllText(logPath, "MapleCharacterRenderer component found\n");
                
                // Check if it has the expected child objects
                Transform playerTransform = playerGO.transform;
                File.AppendAllText(logPath, $"Child objects under Player ({playerTransform.childCount} total):\n");
                for (int i = 0; i < playerTransform.childCount; i++)
                {
                    var child = playerTransform.GetChild(i);
                    File.AppendAllText(logPath, $"  - {child.name} at local position {child.localPosition}\n");
                }
            }
            else
            {
                File.AppendAllText(logPath, "[WARNING] MapleCharacterRenderer component not found\n");
            }
            
            // SUMMARY
            File.AppendAllText(logPath, "\n=== TEST SUMMARY ===\n");
            
            int testsRun = 0;
            int testsPassed = 0;
            
            // Count test results
            testsRun++;
            if (headRenderer != null && bodyRenderer != null)
            {
                float headOffset = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
                if (headOffset > 0.4f && headOffset < 0.5f) testsPassed++;
            }
            
            testsRun++;
            if (armRenderer != null && bodyRenderer != null)
            {
                float armOffset = armRenderer.transform.position.y - bodyRenderer.transform.position.y;
                if (Math.Abs(armOffset) < 0.01f) testsPassed++;
            }
            
            testsRun++;
            if (faceRenderer != null && headRenderer != null)
            {
                float faceXOffset = faceRenderer.transform.position.x - headRenderer.transform.position.x;
                float faceYOffset = faceRenderer.transform.position.y - headRenderer.transform.position.y;
                if (Math.Abs(faceXOffset) < 0.01f && Math.Abs(faceYOffset) < 0.01f) testsPassed++;
            }
            
            testsRun++;
            if (!anyFlipped) testsPassed++;
            
            File.AppendAllText(logPath, $"\nTests passed: {testsPassed}/{testsRun}\n");
            File.AppendAllText(logPath, $"Overall status: {(testsPassed == testsRun ? "ALL TESTS PASSED" : "SOME TESTS FAILED")}\n");
            
            File.AppendAllText(logPath, $"\nTest completed at: {DateTime.Now}\n");
            Debug.Log($"Complete character rendering test finished. Results saved to: {logPath}");
            
            EditorApplication.Exit(testsPassed == testsRun ? 0 : 1);
        }
        catch (Exception e)
        {
            File.AppendAllText(logPath, $"\n[ERROR] Exception occurred: {e.Message}\n{e.StackTrace}\n");
            Debug.LogError($"Test failed with exception: {e.Message}");
            EditorApplication.Exit(1);
        }
    }
}