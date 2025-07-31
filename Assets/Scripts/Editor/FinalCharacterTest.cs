using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Linq;
using System.Reflection;

public static class FinalCharacterTest
{
    private static string resultPath = @"C:\Users\me\MapleUnity\character-test-results.txt";
    
    public static void RunTest()
    {
        try
        {
            File.WriteAllText(resultPath, "[TEST] Starting character rendering verification test\n");
            File.AppendAllText(resultPath, $"[TEST] Time: {DateTime.Now}\n\n");
            
            // Open the henesys scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            File.AppendAllText(resultPath, $"[TEST] Opened scene: {scene.name}\n");
            
            // Find GameManager
            var gameManager = GameObject.Find("GameManager");
            if (gameManager == null)
            {
                File.AppendAllText(resultPath, "[ERROR] GameManager not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get GameManager component
            var gmComponent = gameManager.GetComponent<MonoBehaviour>();
            File.AppendAllText(resultPath, $"[TEST] GameManager component type: {gmComponent?.GetType().Name}\n");
            
            // Force initialization
            InvokeMethod(gmComponent, "Awake");
            InvokeMethod(gmComponent, "Start");
            File.AppendAllText(resultPath, "[TEST] Called GameManager initialization methods\n");
            
            // Get GameWorld from GameManager
            var gameWorldField = gmComponent.GetType().GetField("gameWorld", 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var gameWorld = gameWorldField?.GetValue(gmComponent);
            
            if (gameWorld == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Could not get GameWorld from GameManager\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "[TEST] Got GameWorld instance\n");
            
            // Get Player from GameWorld
            var playerProp = gameWorld.GetType().GetProperty("Player");
            var player = playerProp?.GetValue(gameWorld);
            
            if (player == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Could not get Player from GameWorld\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "[TEST] Got Player instance from GameWorld\n");
            
            // Find Player GameObject
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                File.AppendAllText(resultPath, "[ERROR] Player GameObject not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, $"[TEST] Player GameObject found at: {playerGO.transform.position}\n");
            
            // Get SimplePlayerController
            var simpleController = playerGO.GetComponent<MonoBehaviour>();
            MonoBehaviour controllerComp = null;
            foreach (var comp in playerGO.GetComponents<MonoBehaviour>())
            {
                if (comp.GetType().Name == "SimplePlayerController")
                {
                    controllerComp = comp;
                    break;
                }
            }
            
            if (controllerComp == null)
            {
                File.AppendAllText(resultPath, "[ERROR] SimplePlayerController not found\n");
                EditorApplication.Exit(1);
                return;
            }
            
            File.AppendAllText(resultPath, "[TEST] Found SimplePlayerController\n");
            
            // Force Awake on SimplePlayerController first
            InvokeMethod(controllerComp, "Awake");
            File.AppendAllText(resultPath, "[TEST] Called SimplePlayerController.Awake()\n");
            
            // Call SetGameLogicPlayer and SetGameWorld to initialize the controller
            var setPlayerMethod = controllerComp.GetType().GetMethod("SetGameLogicPlayer");
            var setWorldMethod = controllerComp.GetType().GetMethod("SetGameWorld");
            
            if (setPlayerMethod != null && setWorldMethod != null)
            {
                setWorldMethod.Invoke(controllerComp, new object[] { gameWorld });
                setPlayerMethod.Invoke(controllerComp, new object[] { player });
                File.AppendAllText(resultPath, "[TEST] Called SetGameWorld and SetGameLogicPlayer on SimplePlayerController\n");
                
                // Force initialization of MapleCharacterRenderer
                var charRendererField = controllerComp.GetType().GetField("characterRenderer", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var charRenderer = charRendererField?.GetValue(controllerComp);
                
                if (charRenderer != null)
                {
                    File.AppendAllText(resultPath, "[TEST] Found MapleCharacterRenderer instance\n");
                    
                    // Check if it's initialized
                    var initMethod = charRenderer.GetType().GetMethod("Initialize");
                    if (initMethod != null)
                    {
                        // Create character data provider
                        var providerType = System.Type.GetType("MapleClient.GameData.CharacterDataProvider, Assembly-CSharp");
                        if (providerType != null)
                        {
                            var provider = System.Activator.CreateInstance(providerType);
                            initMethod.Invoke(charRenderer, new object[] { player, provider });
                            File.AppendAllText(resultPath, "[TEST] Manually called MapleCharacterRenderer.Initialize\n");
                        }
                    }
                }
                else
                {
                    File.AppendAllText(resultPath, "[ERROR] MapleCharacterRenderer field not found\n");
                }
            }
            else
            {
                File.AppendAllText(resultPath, $"[ERROR] Methods not found - SetGameLogicPlayer: {setPlayerMethod != null}, SetGameWorld: {setWorldMethod != null}\n");
            }
            
            // Now check for sprite renderers
            File.AppendAllText(resultPath, $"\n[TEST] Checking Player children after initialization:\n");
            for (int i = 0; i < playerGO.transform.childCount; i++)
            {
                var child = playerGO.transform.GetChild(i);
                File.AppendAllText(resultPath, $"  - {child.name} at {child.localPosition}\n");
            }
            
            // Get all sprite renderers
            var renderers = playerGO.GetComponentsInChildren<SpriteRenderer>(true);
            File.AppendAllText(resultPath, $"\n[TEST] Found {renderers.Length} SpriteRenderers:\n");
            
            foreach (var renderer in renderers)
            {
                File.AppendAllText(resultPath, $"  - {renderer.name}: active={renderer.gameObject.activeSelf}, enabled={renderer.enabled}, sprite={(renderer.sprite != null ? renderer.sprite.name : "null")}, flipX={renderer.flipX}\n");
            }
            
            // Run character checks
            if (renderers.Length > 0)
            {
                File.AppendAllText(resultPath, "\n[TEST] Running character rendering checks...\n");
                CheckHeadPosition(renderers);
                CheckFacingDirection(renderers);
                CheckFaceFeatures(renderers);
            }
            else
            {
                File.AppendAllText(resultPath, "\n[ERROR] No sprite renderers found after initialization\n");
            }
            
            File.AppendAllText(resultPath, $"\n[TEST] Test completed at: {DateTime.Now}\n");
            Debug.Log($"Test results written to: {resultPath}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"\n[ERROR] Exception: {e.Message}\n{e.StackTrace}\n");
            EditorApplication.Exit(1);
        }
    }
    
    private static void InvokeMethod(object obj, string methodName)
    {
        try
        {
            if (obj == null) return;
            var method = obj.GetType().GetMethod(methodName, 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(obj, null);
            }
        }
        catch (Exception e)
        {
            File.AppendAllText(resultPath, $"[WARNING] Failed to invoke {methodName}: {e.Message}\n");
        }
    }
    
    private static void CheckHeadPosition(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== HEAD POSITION CHECK ===\n");
        
        var headRenderer = renderers.FirstOrDefault(r => r.name == "Head");
        var bodyRenderer = renderers.FirstOrDefault(r => r.name == "Body");
        
        if (headRenderer != null && bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body position: {bodyRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head position: {headRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Head local position: {headRenderer.transform.localPosition}\n");
            
            float yDiff = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
            File.AppendAllText(resultPath, $"Head Y offset from body: {yDiff}\n");
            
            if (yDiff > 0.01f) // Small tolerance for floating point
            {
                File.AppendAllText(resultPath, "[PASS] Head is ABOVE body (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Head is BELOW or at same level as body (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, $"[FAIL] Could not find renderers - Head found: {headRenderer != null}, Body found: {bodyRenderer != null}\n");
        }
    }
    
    private static void CheckFacingDirection(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== FACING DIRECTION CHECK ===\n");
        
        var bodyRenderer = renderers.FirstOrDefault(r => r.name == "Body");
        
        if (bodyRenderer != null)
        {
            File.AppendAllText(resultPath, $"Body flipX: {bodyRenderer.flipX}\n");
            File.AppendAllText(resultPath, "Expected: false (facing right by default)\n");
            
            if (!bodyRenderer.flipX)
            {
                File.AppendAllText(resultPath, "[PASS] Character facing right by default (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Character facing left by default (incorrect)\n");
            }
            
            // Check all body parts have consistent facing
            File.AppendAllText(resultPath, "\nChecking all sprite parts for consistent facing:\n");
            foreach (var renderer in renderers.Where(r => r.sprite != null))
            {
                File.AppendAllText(resultPath, $"  - {renderer.name}: flipX={renderer.flipX}\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Body renderer not found\n");
        }
    }
    
    private static void CheckFaceFeatures(SpriteRenderer[] renderers)
    {
        File.AppendAllText(resultPath, "\n=== FACE FEATURES CHECK ===\n");
        
        var faceRenderer = renderers.FirstOrDefault(r => r.name == "Face");
        
        if (faceRenderer != null)
        {
            File.AppendAllText(resultPath, "Face renderer found\n");
            File.AppendAllText(resultPath, $"Face sprite: {(faceRenderer.sprite != null ? faceRenderer.sprite.name : "NULL")}\n");
            File.AppendAllText(resultPath, $"Face enabled: {faceRenderer.enabled}\n");
            File.AppendAllText(resultPath, $"Face active: {faceRenderer.gameObject.activeSelf}\n");
            File.AppendAllText(resultPath, $"Face position: {faceRenderer.transform.position}\n");
            File.AppendAllText(resultPath, $"Face local position: {faceRenderer.transform.localPosition}\n");
            File.AppendAllText(resultPath, $"Face sorting order: {faceRenderer.sortingOrder}\n");
            
            if (faceRenderer.sprite != null && faceRenderer.enabled && faceRenderer.gameObject.activeSelf)
            {
                File.AppendAllText(resultPath, "[PASS] Face has sprite and is properly enabled (correct)\n");
            }
            else
            {
                File.AppendAllText(resultPath, "[FAIL] Face missing sprite or not properly enabled (incorrect)\n");
            }
        }
        else
        {
            File.AppendAllText(resultPath, "[FAIL] Face renderer not found\n");
        }
    }
}