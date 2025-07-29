using UnityEngine;
using UnityEditor;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameData;
using MapleClient.GameData.Adapters;
using MapleClient.GameView;
using MapleClient.GameView.Debugging;
using System.Collections.Generic;
using System.Linq;
// Disambiguate Vector2 references
using Vector2 = UnityEngine.Vector2;

namespace MapleClient.Editor
{
    /// <summary>
    /// Debug tool for testing and visualizing foothold collisions
    /// </summary>
    public class FootholdCollisionDebugTool : EditorWindow
    {
        private GameWorld gameWorld;
        private FootholdService footholdService;
        private NxMapLoader mapLoader;
        private Player testPlayer;
        private MapData currentMapData;
        
        // Test settings
        private int mapId = 100000000;
        private MapleClient.GameLogic.Vector2 spawnPosition = new MapleClient.GameLogic.Vector2(5f, 5f);
        private bool autoUpdate = true;
        private float timeScale = 1f;
        
        // Debug display
        private Vector2 scrollPos;
        private bool showFootholds = true;
        private bool showPlayerInfo = true;
        private bool showCollisionInfo = true;
        private bool showPhysicsInfo = true;
        
        // Test scenarios
        private enum TestScenario
        {
            None,
            WalkOnFlat,
            JumpAndLand,
            WalkOnSlope,
            FallOffEdge,
            MultipleJumps,
            StressTest
        }
        
        private TestScenario currentScenario = TestScenario.None;
        private float scenarioTimer = 0f;
        
        [MenuItem("MapleUnity/Debug/Foothold Collision Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<FootholdCollisionDebugTool>("Foothold Collision Debug");
            window.minSize = new Vector2(400, 600);
        }
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            CleanupTest();
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space();
            
            DrawSetupSection();
            EditorGUILayout.Space();
            
            if (gameWorld != null)
            {
                DrawControlsSection();
                EditorGUILayout.Space();
                
                DrawDebugInfoSection();
                EditorGUILayout.Space();
                
                DrawTestScenariosSection();
                EditorGUILayout.Space();
                
                DrawVisualizationSection();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Foothold Collision Debug Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool allows you to test and debug foothold collision in real-time.", MessageType.Info);
        }
        
        private void DrawSetupSection()
        {
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            
            mapId = EditorGUILayout.IntField("Map ID:", mapId);
            var unitySpawnPos = new Vector2(spawnPosition.X, spawnPosition.Y);
            unitySpawnPos = EditorGUILayout.Vector2Field("Spawn Position:", unitySpawnPos);
            spawnPosition = new MapleClient.GameLogic.Vector2(unitySpawnPos.x, unitySpawnPos.y);
            
            EditorGUILayout.BeginHorizontal();
            
            if (gameWorld == null)
            {
                if (GUILayout.Button("Initialize Test", GUILayout.Height(30)))
                {
                    InitializeTest();
                }
            }
            else
            {
                if (GUILayout.Button("Reset Test", GUILayout.Height(30)))
                {
                    CleanupTest();
                    InitializeTest();
                }
                
                if (GUILayout.Button("Stop Test", GUILayout.Height(30)))
                {
                    CleanupTest();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawControlsSection()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            autoUpdate = EditorGUILayout.Toggle("Auto Update Physics", autoUpdate);
            timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0f, 2f);
            
            EditorGUILayout.BeginHorizontal();
            
            // Manual movement controls
            if (GUILayout.Button("← Move Left"))
            {
                testPlayer.MoveLeft(true);
                EditorApplication.delayCall += () => testPlayer.MoveLeft(false);
            }
            
            if (GUILayout.Button("→ Move Right"))
            {
                testPlayer.MoveRight(true);
                EditorApplication.delayCall += () => testPlayer.MoveRight(false);
            }
            
            if (GUILayout.Button("↑ Jump"))
            {
                testPlayer.Jump();
            }
            
            if (GUILayout.Button("↓ Drop"))
            {
                testPlayer.DropThroughPlatform();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Position controls
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset Position"))
            {
                testPlayer.Position = spawnPosition;
                testPlayer.Velocity = MapleClient.GameLogic.Vector2.Zero;
                testPlayer.IsGrounded = false;
            }
            
            if (GUILayout.Button("Teleport Up"))
            {
                testPlayer.Position = new MapleClient.GameLogic.Vector2(testPlayer.Position.X, testPlayer.Position.Y - 2f);
            }
            
            if (GUILayout.Button("Teleport Down"))
            {
                testPlayer.Position = new MapleClient.GameLogic.Vector2(testPlayer.Position.X, testPlayer.Position.Y + 2f);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDebugInfoSection()
        {
            EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);
            
            // Player info
            showPlayerInfo = EditorGUILayout.Foldout(showPlayerInfo, "Player State");
            if (showPlayerInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Position: {testPlayer.Position}");
                EditorGUILayout.LabelField($"Velocity: {testPlayer.Velocity}");
                EditorGUILayout.LabelField($"State: {testPlayer.State}");
                EditorGUILayout.LabelField($"Grounded: {testPlayer.IsGrounded}");
                EditorGUILayout.LabelField($"Jumping: {testPlayer.IsJumping}");
                EditorGUI.indentLevel--;
            }
            
            // Collision info
            showCollisionInfo = EditorGUILayout.Foldout(showCollisionInfo, "Collision Information");
            if (showCollisionInfo)
            {
                EditorGUI.indentLevel++;
                
                // Get ground below player
                MapleClient.GameLogic.Vector2 playerBottom = new MapleClient.GameLogic.Vector2(testPlayer.Position.X, testPlayer.Position.Y - 0.3f);
                MapleClient.GameLogic.Vector2 maplePos = MaplePhysicsConverter.UnityToMaple(playerBottom);
                float groundY = footholdService.GetGroundBelow(maplePos.X, maplePos.Y);
                
                if (groundY != float.MaxValue)
                {
                    float unityGroundY = MaplePhysicsConverter.MapleToUnityY(groundY + 1);
                    EditorGUILayout.LabelField($"Ground Below: Y={unityGroundY:F3} (Unity)");
                    EditorGUILayout.LabelField($"Distance to Ground: {playerBottom.Y - unityGroundY:F3}");
                    
                    // Get current foothold
                    var foothold = footholdService.GetFootholdAt(maplePos.X, maplePos.Y);
                    if (foothold != null)
                    {
                        EditorGUILayout.LabelField($"Current Foothold: ID={foothold.Id}");
                        EditorGUILayout.LabelField($"Foothold Range: X=[{foothold.X1/100f:F2}, {foothold.X2/100f:F2}]");
                        EditorGUILayout.LabelField($"Foothold Slope: {foothold.GetSlope():F3}");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No ground below player");
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Physics info
            showPhysicsInfo = EditorGUILayout.Foldout(showPhysicsInfo, "Physics Information");
            if (showPhysicsInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Walk Speed: {testPlayer.GetWalkSpeed():F3}");
                EditorGUILayout.LabelField($"Gravity: {(testPlayer.IsGrounded ? "0" : MaplePhysics.Gravity.ToString("F3"))}");
                EditorGUILayout.LabelField($"Terminal Velocity: {MaplePhysics.MaxFallSpeed:F3}");
                EditorGUILayout.LabelField($"Fixed Timestep: {MaplePhysics.FIXED_TIMESTEP:F4}");
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawTestScenariosSection()
        {
            EditorGUILayout.LabelField("Test Scenarios", EditorStyles.boldLabel);
            
            currentScenario = (TestScenario)EditorGUILayout.EnumPopup("Current Scenario:", currentScenario);
            
            if (currentScenario != TestScenario.None)
            {
                EditorGUILayout.LabelField($"Scenario Timer: {scenarioTimer:F2}s");
                
                if (GUILayout.Button("Stop Scenario"))
                {
                    currentScenario = TestScenario.None;
                    scenarioTimer = 0f;
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Walk Test"))
                {
                    StartScenario(TestScenario.WalkOnFlat);
                }
                
                if (GUILayout.Button("Jump Test"))
                {
                    StartScenario(TestScenario.JumpAndLand);
                }
                
                if (GUILayout.Button("Slope Test"))
                {
                    StartScenario(TestScenario.WalkOnSlope);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Edge Test"))
                {
                    StartScenario(TestScenario.FallOffEdge);
                }
                
                if (GUILayout.Button("Multi-Jump"))
                {
                    StartScenario(TestScenario.MultipleJumps);
                }
                
                if (GUILayout.Button("Stress Test"))
                {
                    StartScenario(TestScenario.StressTest);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawVisualizationSection()
        {
            EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
            
            showFootholds = EditorGUILayout.Toggle("Show Footholds", showFootholds);
            
            if (GUILayout.Button("Create Debug Visualizer in Scene"))
            {
                CreateSceneVisualizer();
            }
            
            if (GUILayout.Button("Log Foothold Data"))
            {
                LogFootholdData();
            }
            
            if (GUILayout.Button("Export Collision Report"))
            {
                ExportCollisionReport();
            }
        }
        
        private void InitializeTest()
        {
            Debug.Log("[FootholdDebug] Initializing test environment...");
            
            // Create services
            footholdService = new FootholdService();
            mapLoader = new NxMapLoader("", footholdService);
            
            // Create game world
            var inputProvider = new DebugInputProvider();
            gameWorld = new GameWorld(inputProvider, mapLoader, null, null, footholdService);
            
            // Initialize player
            gameWorld.InitializePlayer(1, "DebugPlayer", 100, 100, 100, 100, 0, 2);
            testPlayer = gameWorld.Player;
            
            // Load map
            currentMapData = mapLoader.GetMap(mapId);
            if (currentMapData != null)
            {
                gameWorld.LoadMap(mapId);
                Debug.Log($"[FootholdDebug] Loaded map: {currentMapData.Name} with {footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).Count()} footholds");
            }
            else
            {
                Debug.LogWarning($"[FootholdDebug] Failed to load map ID: {mapId}");
            }
            
            // Set spawn position
            testPlayer.Position = spawnPosition;
            testPlayer.Velocity = MapleClient.GameLogic.Vector2.Zero;
            testPlayer.IsGrounded = false;
            
            Debug.Log("[FootholdDebug] Test environment initialized");
        }
        
        private void CleanupTest()
        {
            gameWorld = null;
            footholdService = null;
            mapLoader = null;
            testPlayer = null;
            currentMapData = null;
            currentScenario = TestScenario.None;
            scenarioTimer = 0f;
            
            Debug.Log("[FootholdDebug] Test environment cleaned up");
        }
        
        private void OnEditorUpdate()
        {
            if (gameWorld == null || testPlayer == null || !autoUpdate)
                return;
            
            // Update physics
            float deltaTime = MaplePhysics.FIXED_TIMESTEP * timeScale;
            testPlayer.UpdatePhysics(deltaTime, currentMapData);
            
            // Update test scenarios
            if (currentScenario != TestScenario.None)
            {
                UpdateScenario(deltaTime);
            }
            
            // Force repaint
            Repaint();
        }
        
        private void StartScenario(TestScenario scenario)
        {
            currentScenario = scenario;
            scenarioTimer = 0f;
            
            // Setup initial conditions for each scenario
            switch (scenario)
            {
                case TestScenario.WalkOnFlat:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(2f, 5f);
                    testPlayer.Velocity = MapleClient.GameLogic.Vector2.Zero;
                    testPlayer.IsGrounded = false;
                    break;
                    
                case TestScenario.JumpAndLand:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(5f, 2.3f);
                    testPlayer.IsGrounded = true;
                    break;
                    
                case TestScenario.WalkOnSlope:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(10f, 2.3f);
                    testPlayer.IsGrounded = true;
                    break;
                    
                case TestScenario.FallOffEdge:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(9f, 2.3f);
                    testPlayer.IsGrounded = true;
                    break;
                    
                case TestScenario.MultipleJumps:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(5f, 2.3f);
                    testPlayer.IsGrounded = true;
                    testPlayer.EnableDoubleJump(true);
                    break;
                    
                case TestScenario.StressTest:
                    testPlayer.Position = new MapleClient.GameLogic.Vector2(0f, 10f);
                    testPlayer.Velocity = new MapleClient.GameLogic.Vector2(5f, 0f);
                    break;
            }
        }
        
        private void UpdateScenario(float deltaTime)
        {
            scenarioTimer += deltaTime;
            
            switch (currentScenario)
            {
                case TestScenario.WalkOnFlat:
                    if (testPlayer.IsGrounded && scenarioTimer < 3f)
                    {
                        testPlayer.MoveRight(true);
                    }
                    else
                    {
                        testPlayer.MoveRight(false);
                        if (scenarioTimer > 4f)
                            currentScenario = TestScenario.None;
                    }
                    break;
                    
                case TestScenario.JumpAndLand:
                    if (scenarioTimer < 0.1f)
                    {
                        testPlayer.Jump();
                    }
                    else if (testPlayer.IsGrounded && scenarioTimer > 1f)
                    {
                        currentScenario = TestScenario.None;
                    }
                    break;
                    
                case TestScenario.WalkOnSlope:
                    if (scenarioTimer < 3f)
                    {
                        testPlayer.MoveRight(true);
                    }
                    else
                    {
                        testPlayer.MoveRight(false);
                        currentScenario = TestScenario.None;
                    }
                    break;
                    
                case TestScenario.FallOffEdge:
                    if (scenarioTimer < 2f)
                    {
                        testPlayer.MoveRight(true);
                    }
                    else
                    {
                        testPlayer.MoveRight(false);
                        if (scenarioTimer > 4f)
                            currentScenario = TestScenario.None;
                    }
                    break;
                    
                case TestScenario.MultipleJumps:
                    if (scenarioTimer < 0.1f)
                    {
                        testPlayer.Jump();
                    }
                    else if (scenarioTimer > 0.5f && scenarioTimer < 0.6f && !testPlayer.IsGrounded)
                    {
                        testPlayer.Jump(); // Double jump
                    }
                    else if (testPlayer.IsGrounded && scenarioTimer > 2f)
                    {
                        testPlayer.EnableDoubleJump(false);
                        currentScenario = TestScenario.None;
                    }
                    break;
                    
                case TestScenario.StressTest:
                    // Random movements
                    if (Random.Range(0f, 1f) < 0.1f)
                    {
                        if (Random.Range(0f, 1f) < 0.5f)
                            testPlayer.MoveLeft(true);
                        else
                            testPlayer.MoveRight(true);
                    }
                    if (Random.Range(0f, 1f) < 0.05f)
                    {
                        testPlayer.MoveLeft(false);
                        testPlayer.MoveRight(false);
                    }
                    if (Random.Range(0f, 1f) < 0.05f && testPlayer.IsGrounded)
                    {
                        testPlayer.Jump();
                    }
                    
                    if (scenarioTimer > 10f)
                        currentScenario = TestScenario.None;
                    break;
            }
        }
        
        private void CreateSceneVisualizer()
        {
            // Check if already exists
            var existing = GameObject.Find("FootholdCollisionVisualizer");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }
            
            // Create new visualizer
            var visualizerObj = new GameObject("FootholdCollisionVisualizer");
            var visualizer = visualizerObj.AddComponent<FootholdDebugVisualizer>();
            visualizer.SetFootholdService(footholdService);
            
            // Create player visualizer
            var playerObj = new GameObject("DebugPlayer");
            playerObj.transform.position = new Vector3(testPlayer.Position.X, testPlayer.Position.Y, 0);
            
            var playerRenderer = playerObj.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(30, 60);
            var pixels = new Color[30 * 60];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.blue;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            playerRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 30, 60), new Vector2(0.5f, 0.5f), 100);
            playerRenderer.sortingOrder = 100;
            
            // Add update component
            var updater = playerObj.AddComponent<DebugPlayerUpdater>();
            updater.SetPlayer(testPlayer);
            
            Debug.Log("[FootholdDebug] Created scene visualizers");
        }
        
        private void LogFootholdData()
        {
            var footholds = footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToList();
            
            Debug.Log($"[FootholdDebug] === Foothold Data ({footholds.Count} footholds) ===");
            
            foreach (var fh in footholds.Take(10)) // Log first 10
            {
                Debug.Log($"Foothold {fh.Id}: Unity[({fh.X1/100f:F2},{fh.Y1/100f:F2}) -> ({fh.X2/100f:F2},{fh.Y2/100f:F2})] " +
                         $"Maple[({fh.X1:F0},{fh.Y1:F0}) -> ({fh.X2:F0},{fh.Y2:F0})] " +
                         $"Prev={fh.PreviousId} Next={fh.NextId}");
            }
            
            if (footholds.Count > 10)
            {
                Debug.Log($"[FootholdDebug] ... and {footholds.Count - 10} more footholds");
            }
        }
        
        private void ExportCollisionReport()
        {
            string report = "Foothold Collision Debug Report\n";
            report += $"Generated: {System.DateTime.Now}\n\n";
            
            report += "=== Player State ===\n";
            report += $"Position: {testPlayer.Position}\n";
            report += $"Velocity: {testPlayer.Velocity}\n";
            report += $"State: {testPlayer.State}\n";
            report += $"Grounded: {testPlayer.IsGrounded}\n\n";
            
            report += "=== Map Data ===\n";
            report += $"Map ID: {mapId}\n";
            report += $"Map Name: {currentMapData?.Name ?? "Unknown"}\n";
            
            var footholds = footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToList();
            report += $"Total Footholds: {footholds.Count}\n\n";
            
            report += "=== Nearby Footholds ===\n";
            var playerMaple = MaplePhysicsConverter.UnityToMaple(testPlayer.Position);
            var nearby = footholdService.GetFootholdsInArea(
                playerMaple.X - 500, playerMaple.Y - 500,
                playerMaple.X + 500, playerMaple.Y + 500
            ).ToList();
            
            foreach (var fh in nearby)
            {
                report += $"Foothold {fh.Id}: ({fh.X1},{fh.Y1}) -> ({fh.X2},{fh.Y2})\n";
            }
            
            // Save to file
            string path = EditorUtility.SaveFilePanel("Save Collision Report", "", "collision_report.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                Debug.Log($"[FootholdDebug] Report saved to: {path}");
            }
        }
        
        // Helper classes
        private class DebugInputProvider : IInputProvider
        {
            public bool IsLeftPressed => false;
            public bool IsRightPressed => false;
            public bool IsJumpPressed => false;
            public bool IsAttackPressed => false;
            public bool IsUpPressed => false;
            public bool IsDownPressed => false;
        }
        
        // Component for updating debug player position in scene
        public class DebugPlayerUpdater : MonoBehaviour
        {
            private Player player;
            
            public void SetPlayer(Player p)
            {
                player = p;
            }
            
            void Update()
            {
                if (player != null)
                {
                    transform.position = new Vector3(player.Position.X, player.Position.Y, 0);
                }
            }
        }
    }
}