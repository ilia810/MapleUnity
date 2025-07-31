using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class TestCharacterRenderingSimple
{
    public static void RunTest()
    {
        try
        {
            Debug.Log("=== Testing Character Rendering with C++ Formulas ===");
            
            // Load test scene
            var scene = EditorSceneManager.OpenScene("Assets/henesys.unity");
            Debug.Log($"Loaded scene: {scene.name}");
            
            // Find all GameObjects in scene
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            Debug.Log($"Total GameObjects in scene: {allObjects.Length}");
            
            // Look for MapleCharacterRenderer in all objects
            var characterRenderers = GameObject.FindObjectsOfType<MapleClient.GameView.MapleCharacterRenderer>();
            Debug.Log($"Found {characterRenderers.Length} MapleCharacterRenderer(s)");
            
            if (characterRenderers.Length == 0)
            {
                // Try to find GameManager and check if it has any character-related components
                var gameManager = GameObject.Find("GameManager");
                if (gameManager != null)
                {
                    Debug.Log("Found GameManager, checking components...");
                    var components = gameManager.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        Debug.Log($"  - Component: {comp.GetType().Name}");
                    }
                    
                    // Check children
                    Debug.Log("Checking GameManager children...");
                    foreach (Transform child in gameManager.transform)
                    {
                        Debug.Log($"  - Child: {child.name}");
                        var childComps = child.GetComponents<Component>();
                        foreach (var comp in childComps)
                        {
                            Debug.Log($"    - Component: {comp.GetType().Name}");
                        }
                    }
                }
                
                // List some interesting GameObjects
                Debug.Log("\nSome GameObjects in scene:");
                var interesting = allObjects.Where(go => 
                    go.name.Contains("Player") || 
                    go.name.Contains("Character") || 
                    go.name.Contains("Avatar") ||
                    go.name.Contains("Hero") ||
                    go.GetComponent<SpriteRenderer>() != null && go.transform.parent == null
                ).Take(20);
                
                foreach (var go in interesting)
                {
                    Debug.Log($"  - {go.name} (parent: {(go.transform.parent ? go.transform.parent.name : "none")})");
                }
                
                Debug.LogWarning("No MapleCharacterRenderer found. The scene may need to be set up first.");
                EditorApplication.Exit(0);
                return;
            }
            
            // Analyze the first character renderer found
            var characterRenderer = characterRenderers[0];
            Debug.Log($"Analyzing MapleCharacterRenderer on GameObject: {characterRenderer.gameObject.name}");
            
            // Get all sprite renderers to check their positions
            var renderers = characterRenderer.GetComponentsInChildren<SpriteRenderer>();
            Debug.Log($"\nSprite Renderer Positions (Total: {renderers.Length}):");
            
            foreach (var renderer in renderers)
            {
                if (renderer.sprite != null)
                {
                    var worldPos = renderer.transform.position;
                    var localPos = renderer.transform.localPosition;
                    
                    Debug.Log($"\n{renderer.name}:");
                    Debug.Log($"  - Sprite: {renderer.sprite.name}");
                    Debug.Log($"  - Local Position: {localPos}");
                    Debug.Log($"  - World Position: {worldPos}");
                    Debug.Log($"  - Sorting Order: {renderer.sortingOrder}");
                }
            }
            
            // Check specific body parts
            Debug.Log("\n=== Body Part Analysis ===");
            
            var body = characterRenderer.transform.Find("Body");
            var head = characterRenderer.transform.Find("Head");
            var arm = characterRenderer.transform.Find("Arm");
            var face = characterRenderer.transform.Find("Face");
            
            if (body != null)
            {
                Debug.Log($"\nBody: localPosition = {body.localPosition}");
            }
            
            if (head != null)
            {
                Debug.Log($"Head: localPosition = {head.localPosition}");
            }
            
            if (arm != null)
            {
                Debug.Log($"Arm: localPosition = {arm.localPosition}");
            }
            
            if (face != null)
            {
                Debug.Log($"Face: localPosition = {face.localPosition}");
            }
            
            Debug.Log("\n=== Test Complete ===");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Test failed: {e.Message}");
            Debug.LogError(e.StackTrace);
            EditorApplication.Exit(1);
        }
    }
}