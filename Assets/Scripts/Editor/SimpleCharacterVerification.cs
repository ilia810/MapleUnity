using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using MapleClient.SceneGeneration;
using MapleClient.GameView;
using System.Linq;

public static class SimpleCharacterVerification
{
    public static void RunTest()
    {
        Debug.Log("[VERIFY_FIXES] Starting character rendering verification...");
        
        try
        {
            // Create test scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create MapSceneGenerator
            GameObject generatorObj = new GameObject("MapSceneGenerator");
            MapSceneGenerator generator = generatorObj.AddComponent<MapSceneGenerator>();
            generator.InitializeGenerators();
            
            // Generate map
            GameObject mapRoot = generator.GenerateMapScene(100000000);
            if (mapRoot == null)
            {
                Debug.LogError("[VERIFY_FIXES] Failed to generate map!");
                EditorApplication.Exit(1);
                return;
            }
            
            GameObject.DestroyImmediate(generatorObj);
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Find player
            var playerGO = GameObject.Find("Player");
            if (playerGO == null)
            {
                Debug.LogError("[VERIFY_FIXES] Player not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log($"[VERIFY_FIXES] Player found at: {playerGO.transform.position}");
            
            // Get PlayerView
            var playerView = playerGO.GetComponent<PlayerView>();
            if (playerView == null)
            {
                Debug.LogError("[VERIFY_FIXES] PlayerView not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            // Get character renderer
            var characterRenderer = playerGO.GetComponentInChildren<MapleCharacterRenderer>();
            if (characterRenderer == null)
            {
                Debug.LogError("[VERIFY_FIXES] MapleCharacterRenderer not found!");
                EditorApplication.Exit(1);
                return;
            }
            
            Debug.Log("[VERIFY_FIXES] MapleCharacterRenderer found!");
            
            // Run all checks
            CheckHeadPosition(characterRenderer);
            CheckFacingDirection(characterRenderer);
            CheckFaceFeatures(characterRenderer);
            TestMovement(playerGO);
            
            Debug.Log("\n[VERIFY_FIXES] === TEST SUMMARY ===");
            Debug.Log("[VERIFY_FIXES] All character rendering checks completed!");
            Debug.Log("[VERIFY_FIXES] Check the log above for detailed results.");
            
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VERIFY_FIXES] Test failed with exception: {e}");
            Debug.LogError($"[VERIFY_FIXES] Stack trace: {e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }
    
    private static void CheckHeadPosition(MapleCharacterRenderer renderer)
    {
        Debug.Log("\n[VERIFY_FIXES] === HEAD POSITION CHECK ===");
        
        var headRenderer = FindRenderer(renderer, "Head");
        var bodyRenderer = FindRenderer(renderer, "Body");
        
        if (headRenderer != null && bodyRenderer != null)
        {
            Debug.Log($"Body position: {bodyRenderer.transform.position}");
            Debug.Log($"Head position: {headRenderer.transform.position}");
            Debug.Log($"Head local position: {headRenderer.transform.localPosition}");
            
            float yDiff = headRenderer.transform.position.y - bodyRenderer.transform.position.y;
            Debug.Log($"Head Y offset from body: {yDiff}");
            
            if (yDiff > 0)
            {
                Debug.Log("[PASS] Head is ABOVE body (correct)");
            }
            else
            {
                Debug.LogError("[FAIL] Head is BELOW body (incorrect)");
            }
        }
        else
        {
            Debug.LogError($"[FAIL] Could not find renderers - Head: {headRenderer != null}, Body: {bodyRenderer != null}");
        }
    }
    
    private static void CheckFacingDirection(MapleCharacterRenderer renderer)
    {
        Debug.Log("\n[VERIFY_FIXES] === FACING DIRECTION CHECK ===");
        
        var bodyRenderer = FindRenderer(renderer, "Body");
        if (bodyRenderer != null)
        {
            Debug.Log($"Body flipX: {bodyRenderer.flipX}");
            Debug.Log($"Expected: false (facing right by default)");
            
            if (!bodyRenderer.flipX)
            {
                Debug.Log("[PASS] Character facing right by default (correct)");
            }
            else
            {
                Debug.LogError("[FAIL] Character facing left by default (incorrect)");
            }
        }
        else
        {
            Debug.LogError("[FAIL] Body renderer not found!");
        }
    }
    
    private static void CheckFaceFeatures(MapleCharacterRenderer renderer)
    {
        Debug.Log("\n[VERIFY_FIXES] === FACE FEATURES CHECK ===");
        
        var faceRenderer = FindRenderer(renderer, "Face");
        if (faceRenderer != null)
        {
            Debug.Log($"Face renderer found");
            Debug.Log($"Face sprite: {(faceRenderer.sprite != null ? faceRenderer.sprite.name : "NULL")}");
            Debug.Log($"Face enabled: {faceRenderer.enabled}");
            Debug.Log($"Face position: {faceRenderer.transform.position}");
            Debug.Log($"Face sorting order: {faceRenderer.sortingOrder}");
            
            if (faceRenderer.sprite != null && faceRenderer.enabled)
            {
                Debug.Log("[PASS] Face has sprite and is enabled (correct)");
            }
            else
            {
                Debug.LogError("[FAIL] Face missing sprite or disabled (incorrect)");
            }
        }
        else
        {
            Debug.LogError("[FAIL] Face renderer not found!");
        }
    }
    
    private static void TestMovement(GameObject playerGO)
    {
        Debug.Log("\n[VERIFY_FIXES] === MOVEMENT TEST ===");
        
        var playerView = playerGO.GetComponent<PlayerView>();
        var playerField = playerView.GetType().GetField("player", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var player = playerField?.GetValue(playerView) as MapleClient.GameLogic.Core.Player;
        
        if (player == null)
        {
            Debug.LogError("[FAIL] Could not access Player!");
            return;
        }
        
        // Test right movement
        Debug.Log("\n[Movement] Testing RIGHT movement...");
        player.MoveRight(true);
        
        // Simulate some frames
        for (int i = 0; i < 5; i++)
        {
            player.UpdatePhysics(Time.fixedDeltaTime, null);
        }
        
        CheckSpriteFacing(playerGO, "right");
        
        // Stop
        player.MoveRight(false);
        Debug.Log("[Movement] Stopped - should maintain facing");
        CheckSpriteFacing(playerGO, "right (maintained)");
        
        // Test left movement
        Debug.Log("\n[Movement] Testing LEFT movement...");
        player.MoveLeft(true);
        
        // Simulate some frames
        for (int i = 0; i < 5; i++)
        {
            player.UpdatePhysics(Time.fixedDeltaTime, null);
        }
        
        CheckSpriteFacing(playerGO, "left");
        
        // Stop
        player.MoveLeft(false);
    }
    
    private static void CheckSpriteFacing(GameObject playerGO, string expectedDirection)
    {
        var bodyRenderer = playerGO.GetComponentsInChildren<SpriteRenderer>()
            .FirstOrDefault(sr => sr.name == "Body");
            
        if (bodyRenderer != null)
        {
            Debug.Log($"Body flipX: {bodyRenderer.flipX} (expected: {expectedDirection})");
            
            bool isLeft = expectedDirection.Contains("left");
            if (bodyRenderer.flipX == isLeft)
            {
                Debug.Log($"[PASS] Character facing {expectedDirection}");
            }
            else
            {
                Debug.LogError($"[FAIL] Character not facing {expectedDirection}");
            }
        }
    }
    
    private static SpriteRenderer FindRenderer(MapleCharacterRenderer renderer, string name)
    {
        var renderers = renderer.GetComponentsInChildren<SpriteRenderer>();
        return System.Array.Find(renderers, r => r.name == name);
    }
}