using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using System.Collections.Generic;

namespace MapleClient.Editor
{
    /// <summary>
    /// Creates a test scene for MapleStory v83 physics testing and fine-tuning.
    /// Includes various platform types, UI controls, and performance monitoring.
    /// </summary>
    public static class PhysicsTestScene
    {
        [MenuItem("MapleUnity/Create Physics Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "PhysicsTestScene";
            
            // Configure physics
            CreatePhysicsConfiguration();
            
            // Create test map
            CreateTestMap();
            
            // Create test UI
            CreateTestUI();
            
            // Save scene
            string scenePath = "Assets/Scenes/PhysicsTestScene.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"Physics test scene created at: {scenePath}");
            EditorUtility.DisplayDialog("Physics Test Scene", 
                "Physics test scene created successfully!\n\n" +
                "Features:\n" +
                "- Various platform types for testing\n" +
                "- Real-time physics parameter adjustment\n" +
                "- Performance monitoring\n" +
                "- Visual debugging options\n\n" +
                "Press Play to start testing!", "OK");
        }
        
        private static void CreatePhysicsConfiguration()
        {
            // Create GameManager
            var gameManager = new GameObject("GameManager");
            
            // Add physics configuration
            gameManager.AddComponent<PhysicsConfiguration>();
            
            // Add physics test controller
            var testController = gameManager.AddComponent<PhysicsTestController>();
        }
        
        private static void CreateTestMap()
        {
            // Create map object
            var mapObject = new GameObject("TestMap");
            
            // Create platforms parent
            var platformsParent = new GameObject("Platforms");
            platformsParent.transform.SetParent(mapObject.transform);
            
            // Create various test platforms
            CreateTestPlatforms(platformsParent.transform);
            
            // Create visual grid for reference
            CreateReferenceGrid(mapObject.transform);
            
            // Set up camera
            SetupCamera();
        }
        
        private static void CreateTestPlatforms(Transform parent)
        {
            // Ground platform
            CreatePlatform(parent, "Ground", new Vector2(0, -2), new Vector2(20, 0.2f), Color.gray);
            
            // Regular platforms at different heights
            CreatePlatform(parent, "Platform1", new Vector2(-5, 0), new Vector2(4, 0.2f), Color.blue);
            CreatePlatform(parent, "Platform2", new Vector2(5, 1), new Vector2(4, 0.2f), Color.blue);
            CreatePlatform(parent, "Platform3", new Vector2(0, 2.5f), new Vector2(4, 0.2f), Color.blue);
            
            // One-way platforms
            CreatePlatform(parent, "OneWay1", new Vector2(-3, -0.5f), new Vector2(3, 0.1f), Color.green, true);
            CreatePlatform(parent, "OneWay2", new Vector2(3, 0.5f), new Vector2(3, 0.1f), Color.green, true);
            
            // Small platforms (edge case testing)
            CreatePlatform(parent, "SmallPlatform1", new Vector2(-7, 1.5f), new Vector2(0.5f, 0.1f), Color.yellow);
            CreatePlatform(parent, "SmallPlatform2", new Vector2(7, 1.5f), new Vector2(0.5f, 0.1f), Color.yellow);
            
            // Slopes
            CreateSlope(parent, "Slope1", new Vector2(-8, -2), new Vector2(-6, -1), Color.cyan);
            CreateSlope(parent, "Slope2", new Vector2(6, -2), new Vector2(8, -1), Color.cyan);
            
            // Moving platform placeholder
            CreatePlatform(parent, "MovingPlatform", new Vector2(0, -1), new Vector2(2, 0.1f), Color.magenta);
            
            // Create ladder
            CreateLadder(parent, "Ladder1", new Vector2(0, -2), 4f);
        }
        
        private static void CreatePlatform(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool oneWay = false)
        {
            var platform = new GameObject(name);
            platform.transform.SetParent(parent);
            platform.transform.position = position;
            
            // Visual representation
            var renderer = platform.AddComponent<SpriteRenderer>();
            var texture = new Texture2D((int)(size.x * 100), (int)(size.y * 100));
            var pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0.5f, 0.5f), 100);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;
            
            // Collider for visual reference (actual collision is handled by GameLogic)
            var collider = platform.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            
            // Tag for identification
            platform.tag = oneWay ? "OneWayPlatform" : "Platform";
        }
        
        private static void CreateSlope(Transform parent, string name, Vector2 start, Vector2 end, Color color)
        {
            var slope = new GameObject(name);
            slope.transform.SetParent(parent);
            slope.transform.position = (start + end) / 2;
            
            // Create line renderer for slope
            var lineRenderer = slope.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            lineRenderer.SetPositions(new Vector3[] { start, end });
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 0;
            
            // Edge collider for visual reference
            var collider = slope.AddComponent<EdgeCollider2D>();
            collider.points = new Vector2[] { start - (Vector2)slope.transform.position, end - (Vector2)slope.transform.position };
            collider.isTrigger = true;
            
            slope.tag = "Platform";
        }
        
        private static void CreateLadder(Transform parent, string name, Vector2 position, float height)
        {
            var ladder = new GameObject(name);
            ladder.transform.SetParent(parent);
            ladder.transform.position = position;
            
            // Visual representation
            var renderer = ladder.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(30, (int)(height * 100));
            var pixels = new Color[texture.width * texture.height];
            Color ladderColor = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            
            // Create ladder pattern
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (x < 5 || x >= 25 || y % 20 < 3)
                        pixels[y * texture.width + x] = ladderColor;
                    else
                        pixels[y * texture.width + x] = Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0.5f, 0f), 100);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -1;
            
            // Trigger collider
            var collider = ladder.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, height);
            collider.offset = new Vector2(0, height / 2);
            collider.isTrigger = true;
            
            ladder.tag = "Ladder";
        }
        
        private static void CreateReferenceGrid(Transform parent)
        {
            var grid = new GameObject("ReferenceGrid");
            grid.transform.SetParent(parent);
            
            // Create grid lines
            for (int x = -10; x <= 10; x++)
            {
                CreateGridLine(grid.transform, $"GridV{x}", 
                    new Vector2(x, -5), new Vector2(x, 5), 
                    x == 0 ? Color.red : new Color(0.3f, 0.3f, 0.3f, 0.3f));
            }
            
            for (int y = -5; y <= 5; y++)
            {
                CreateGridLine(grid.transform, $"GridH{y}", 
                    new Vector2(-10, y), new Vector2(10, y), 
                    y == 0 ? Color.green : new Color(0.3f, 0.3f, 0.3f, 0.3f));
            }
        }
        
        private static void CreateGridLine(Transform parent, string name, Vector2 start, Vector2 end, Color color)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent);
            
            var lineRenderer = line.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.SetPositions(new Vector3[] { start, end });
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = -10;
        }
        
        private static void SetupCamera()
        {
            var camera = Camera.main;
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
        }
        
        private static void CreateTestUI()
        {
            // Create Canvas
            var canvas = new GameObject("TestUI");
            var canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create EventSystem
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create UI panels
            CreatePhysicsControlPanel(canvas.transform);
            CreateDebugInfoPanel(canvas.transform);
            CreateInstructionsPanel(canvas.transform);
        }
        
        private static void CreatePhysicsControlPanel(Transform parent)
        {
            var panel = CreateUIPanel(parent, "PhysicsControls", 
                new Vector2(200, 400), new Vector2(10, -10), 
                new Vector2(0, 1), new Vector2(0, 1));
            
            // Will be populated by PhysicsTestController at runtime
        }
        
        private static void CreateDebugInfoPanel(Transform parent)
        {
            var panel = CreateUIPanel(parent, "DebugInfo", 
                new Vector2(300, 200), new Vector2(-10, -10), 
                new Vector2(1, 1), new Vector2(1, 1));
            
            // Will be populated by PhysicsTestController at runtime
        }
        
        private static void CreateInstructionsPanel(Transform parent)
        {
            var panel = CreateUIPanel(parent, "Instructions", 
                new Vector2(400, 150), new Vector2(0, -10), 
                new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            
            // Add instructions text
            var text = new GameObject("InstructionsText");
            text.transform.SetParent(panel.transform);
            var textComponent = text.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = "PHYSICS TEST CONTROLS\n\n" +
                                "Arrow Keys / WASD - Move\n" +
                                "Space - Jump\n" +
                                "Down + Space - Drop through platform\n" +
                                "Up/Down near ladder - Climb\n" +
                                "R - Reset position\n" +
                                "Tab - Toggle debug info";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            var rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(10, 10);
            rectTransform.offsetMax = new Vector2(-10, -10);
        }
        
        private static GameObject CreateUIPanel(Transform parent, string name, 
            Vector2 size, Vector2 position, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            
            var rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            
            return panel;
        }
    }
}