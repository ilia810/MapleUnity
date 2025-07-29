using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MapleClient.GameView;
using MapleClient.GameView.Debugging;

namespace MapleClient.Editor
{
    /// <summary>
    /// Creates a test scene specifically for testing foothold collision
    /// </summary>
    public class CreateFootholdTestScene : EditorWindow
    {
        private string sceneName = "FootholdCollisionTest";
        private bool addDebugTools = true;
        private bool addVisualizers = true;
        private bool createTestPlatforms = true;
        private int testMapId = 100000000;
        
        [MenuItem("MapleUnity/Test/Create Foothold Test Scene")]
        public static void ShowWindow()
        {
            GetWindow<CreateFootholdTestScene>("Create Test Scene");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Foothold Collision Test Scene Setup", EditorStyles.boldLabel);
            
            sceneName = EditorGUILayout.TextField("Scene Name:", sceneName);
            addDebugTools = EditorGUILayout.Toggle("Add Debug Tools", addDebugTools);
            addVisualizers = EditorGUILayout.Toggle("Add Visualizers", addVisualizers);
            createTestPlatforms = EditorGUILayout.Toggle("Create Test Platforms", createTestPlatforms);
            testMapId = EditorGUILayout.IntField("Test Map ID:", testMapId);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Test Scene", GUILayout.Height(30)))
            {
                CreateScene();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will create a new scene with:\n" +
                "• GameManager configured for testing\n" +
                "• Debug visualization tools\n" +
                "• Test platform configurations\n" +
                "• Camera setup for testing", 
                MessageType.Info);
        }
        
        private void CreateScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = sceneName;
            
            // Setup camera
            SetupCamera();
            
            // Create GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Create debug tools
            if (addDebugTools)
            {
                CreateDebugTools(gameManager);
            }
            
            // Create visualizers
            if (addVisualizers)
            {
                CreateVisualizers();
            }
            
            // Create test platforms
            if (createTestPlatforms)
            {
                CreateTestPlatformMarkers();
            }
            
            // Create UI
            CreateTestUI();
            
            // Save scene
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"Created test scene: {scenePath}");
            
            // Select GameManager for easy configuration
            Selection.activeGameObject = gameManagerObj;
        }
        
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            
            // Position camera for good test view
            mainCamera.transform.position = new Vector3(5, 3, -10);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5;
            mainCamera.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
            
            // Add audio listener
            if (!mainCamera.GetComponent<AudioListener>())
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
            }
        }
        
        private void CreateDebugTools(GameManager gameManager)
        {
            // Foothold Collision Debugger
            GameObject debuggerObj = new GameObject("FootholdCollisionDebugger");
            FootholdCollisionDebugger debugger = debuggerObj.AddComponent<FootholdCollisionDebugger>();
            
            // Configure debugger
            SerializedObject debuggerSO = new SerializedObject(debugger);
            debuggerSO.FindProperty("gameManager").objectReferenceValue = gameManager;
            debuggerSO.FindProperty("enableDebugView").boolValue = true;
            debuggerSO.FindProperty("showFootholds").boolValue = true;
            debuggerSO.FindProperty("showPlayerCollision").boolValue = true;
            debuggerSO.FindProperty("showGroundRays").boolValue = true;
            debuggerSO.ApplyModifiedProperties();
            
            // Foothold Debug Visualizer
            GameObject visualizerObj = new GameObject("FootholdDebugVisualizer");
            FootholdDebugVisualizer visualizer = visualizerObj.AddComponent<FootholdDebugVisualizer>();
            
            // Configure visualizer
            SerializedObject visualizerSO = new SerializedObject(visualizer);
            visualizerSO.FindProperty("showFootholds").boolValue = true;
            visualizerSO.FindProperty("showConnections").boolValue = true;
            visualizerSO.FindProperty("showIds").boolValue = true;
            visualizerSO.ApplyModifiedProperties();
            
            Debug.Log("Created debug tools");
        }
        
        private void CreateVisualizers()
        {
            // Create container for visual markers
            GameObject visualsContainer = new GameObject("VisualMarkers");
            
            // Ground level indicator
            GameObject groundLine = new GameObject("GroundLevelIndicator");
            groundLine.transform.parent = visualsContainer.transform;
            LineRenderer groundRenderer = groundLine.AddComponent<LineRenderer>();
            groundRenderer.positionCount = 2;
            groundRenderer.SetPositions(new Vector3[] { 
                new Vector3(-10, 2, 0), 
                new Vector3(20, 2, 0) 
            });
            groundRenderer.startWidth = 0.05f;
            groundRenderer.endWidth = 0.05f;
            groundRenderer.material = new Material(Shader.Find("Sprites/Default"));
            groundRenderer.startColor = new Color(0, 1, 0, 0.5f);
            groundRenderer.endColor = new Color(0, 1, 0, 0.5f);
            
            // Height markers
            for (int y = 0; y <= 10; y++)
            {
                GameObject marker = new GameObject($"HeightMarker_{y}");
                marker.transform.parent = visualsContainer.transform;
                marker.transform.position = new Vector3(-9.5f, y, 0);
                
                TextMesh text = marker.AddComponent<TextMesh>();
                text.text = $"Y={y}";
                text.fontSize = 20;
                text.color = new Color(1, 1, 1, 0.5f);
                text.anchor = TextAnchor.MiddleRight;
            }
            
            Debug.Log("Created visual markers");
        }
        
        private void CreateTestPlatformMarkers()
        {
            GameObject platformContainer = new GameObject("TestPlatformMarkers");
            
            // Flat platform marker
            CreatePlatformMarker(platformContainer, "FlatPlatform", 
                new Vector3(0, 2, 0), new Vector3(10, 2, 0), Color.green);
            
            // Sloped platform marker
            CreatePlatformMarker(platformContainer, "SlopedPlatform", 
                new Vector3(10, 2, 0), new Vector3(15, 3, 0), Color.yellow);
            
            // Gap marker
            CreatePlatformMarker(platformContainer, "GapStart", 
                new Vector3(15, 3, 0), new Vector3(15, 3, 0), Color.red);
            CreatePlatformMarker(platformContainer, "GapEnd", 
                new Vector3(17, 2.5f, 0), new Vector3(20, 2.5f, 0), Color.green);
            
            // High platform marker
            CreatePlatformMarker(platformContainer, "HighPlatform", 
                new Vector3(2, 1, 0), new Vector3(4, 1, 0), Color.cyan);
            
            // Add labels
            AddPlatformLabel(platformContainer, "Flat", new Vector3(5, 2.5f, 0));
            AddPlatformLabel(platformContainer, "Slope", new Vector3(12.5f, 2.8f, 0));
            AddPlatformLabel(platformContainer, "Gap", new Vector3(16, 3.5f, 0));
            AddPlatformLabel(platformContainer, "High", new Vector3(3, 1.5f, 0));
            
            Debug.Log("Created test platform markers");
        }
        
        private void CreatePlatformMarker(GameObject parent, string name, Vector3 start, Vector3 end, Color color)
        {
            GameObject marker = new GameObject(name);
            marker.transform.parent = parent.transform;
            
            LineRenderer line = marker.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPositions(new Vector3[] { start, end });
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
        }
        
        private void AddPlatformLabel(GameObject parent, string text, Vector3 position)
        {
            GameObject label = new GameObject($"Label_{text}");
            label.transform.parent = parent.transform;
            label.transform.position = position;
            
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }
        
        private void CreateTestUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Create debug panel background
            GameObject panelObj = new GameObject("DebugPanel");
            panelObj.transform.SetParent(canvasObj.transform);
            UnityEngine.UI.Image panelImg = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(400, 300);
            
            // Instructions text
            GameObject instructionsObj = new GameObject("Instructions");
            instructionsObj.transform.SetParent(canvasObj.transform);
            UnityEngine.UI.Text instructions = instructionsObj.AddComponent<UnityEngine.UI.Text>();
            instructions.text = "Foothold Collision Test Scene\n\n" +
                              "Controls:\n" +
                              "• Arrow Keys / WASD - Move\n" +
                              "• Space - Jump\n" +
                              "• Up/Down - Climb ladders\n" +
                              "• Down + Jump - Drop through platform\n\n" +
                              "Debug:\n" +
                              "• See debug panel for collision info\n" +
                              "• Gizmos show foothold visualization";
            instructions.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructions.fontSize = 14;
            instructions.color = Color.white;
            
            RectTransform instructRect = instructionsObj.GetComponent<RectTransform>();
            instructRect.anchorMin = new Vector2(1, 1);
            instructRect.anchorMax = new Vector2(1, 1);
            instructRect.pivot = new Vector2(1, 1);
            instructRect.anchoredPosition = new Vector2(-10, -10);
            instructRect.sizeDelta = new Vector2(300, 200);
            
            Debug.Log("Created test UI");
        }
    }
}