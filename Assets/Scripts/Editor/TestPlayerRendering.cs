using UnityEngine;
using UnityEditor;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;

public class TestPlayerRendering : EditorWindow
{
    [MenuItem("MapleUnity/Test Player Rendering")]
    static void ShowWindow()
    {
        GetWindow<TestPlayerRendering>("Test Player Rendering");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("Create Test Player"))
        {
            CreateTestPlayer();
        }
        
        if (GUILayout.Button("Debug Player Position"))
        {
            DebugPlayerPosition();
        }
        
        if (GUILayout.Button("Create Simple Test Sprite"))
        {
            CreateSimpleTestSprite();
        }
    }
    
    void CreateTestPlayer()
    {
        // Find or create player
        var playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
        }
        
        // Position at center of view
        playerObj.transform.position = new Vector3(5, 5, 0);
        
        // Add PlayerView if missing
        var playerView = playerObj.GetComponent<PlayerView>();
        if (playerView == null)
        {
            playerView = playerObj.AddComponent<PlayerView>();
        }
        
        // Create a test player instance
        var player = new Player("TestPlayer", 1, 0);
        player.Position = new Vector2(5, 5);
        playerView.SetPlayer(player);
        
        Debug.Log($"Created test player at position {playerObj.transform.position}");
    }
    
    void DebugPlayerPosition()
    {
        var playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("No player found!");
            return;
        }
        
        Debug.Log($"=== Player Debug Info ===");
        Debug.Log($"Player GameObject position: {playerObj.transform.position}");
        Debug.Log($"Player GameObject local position: {playerObj.transform.localPosition}");
        Debug.Log($"Player GameObject parent: {playerObj.transform.parent?.name ?? "None"}");
        
        // Check all child objects
        Debug.Log($"Child objects ({playerObj.transform.childCount}):");
        for (int i = 0; i < playerObj.transform.childCount; i++)
        {
            var child = playerObj.transform.GetChild(i);
            Debug.Log($"  - {child.name}: position={child.position}, localPos={child.localPosition}");
            
            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Debug.Log($"    SpriteRenderer: enabled={renderer.enabled}, sprite={renderer.sprite?.name ?? "null"}, sortingLayer={renderer.sortingLayerName}, order={renderer.sortingOrder}");
                if (renderer.sprite != null)
                {
                    Debug.Log($"    Sprite info: size={renderer.sprite.rect.size}, pivot={renderer.sprite.pivot}, pixelsPerUnit={renderer.sprite.pixelsPerUnit}");
                }
            }
        }
        
        // Check camera
        var cam = Camera.main;
        if (cam != null)
        {
            Debug.Log($"Camera position: {cam.transform.position}, orthographic size: {cam.orthographicSize}");
            var screenPos = cam.WorldToScreenPoint(playerObj.transform.position);
            Debug.Log($"Player screen position: {screenPos}");
        }
    }
    
    void CreateSimpleTestSprite()
    {
        var playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            Debug.LogError("Create player first!");
            return;
        }
        
        // Create a simple colored square as test
        var testObj = new GameObject("TestSprite");
        testObj.transform.SetParent(playerObj.transform, false);
        testObj.transform.localPosition = Vector3.zero;
        
        var renderer = testObj.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 10;
        
        // Create a red square sprite
        var texture = new Texture2D(32, 48);
        var colors = new Color[32 * 48];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.red;
        }
        texture.SetPixels(colors);
        texture.Apply();
        
        var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0.5f), 100f);
        renderer.sprite = sprite;
        
        Debug.Log($"Created test sprite at player position");
    }
}