using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using MapleClient.GameData;
using MapleClient.GameView.UI;
using GameData.Network;
using GameData;

using Debug = UnityEngine.Debug;

namespace MapleClient.GameView
{
    public class GameManager : MonoBehaviour
    {
        private GameWorld gameWorld;
        private IMapLoader mapLoader;
        private IInputProvider inputProvider;
        private IAssetProvider assetProvider;
        private MapleStoryNetworkClient networkClient;
        private IFootholdService footholdService;
        
        [Header("Network Settings")]
        [SerializeField] private bool useNetworking = false;
        [SerializeField] private string serverHost = "localhost";
        [SerializeField] private int loginPort = 8484;

        [SerializeField] private PlayerView playerViewPrefab;
        private PlayerView currentPlayerView;
        
        private Dictionary<Monster, MonsterView> monsterViews = new Dictionary<Monster, MonsterView>();
        private Dictionary<DroppedItem, DroppedItemView> droppedItemViews = new Dictionary<DroppedItem, DroppedItemView>();
        private Dictionary<Player, PlayerView> otherPlayerViews = new Dictionary<Player, PlayerView>();
        
        private MapRenderer mapRenderer;
        private SimplePlatformBridge platformBridge;
        private SimplePlayerController playerController;
        
        public Player Player => gameWorld?.Player;
        public SkillManager SkillManager => gameWorld?.SkillManager;
        public IFootholdService FootholdService => footholdService;

        private void Awake()
        {
            // Configure physics for 60 FPS as early as possible
            if (!GetComponent<PhysicsConfiguration>())
            {
                gameObject.AddComponent<PhysicsConfiguration>();
            }
            
            // Add physics debugger
            if (!GetComponent<PhysicsDebugger>())
            {
                gameObject.AddComponent<PhysicsDebugger>();
            }
            
            // Add foothold debug logger to capture console output
            if (!GetComponent<MapleClient.GameView.Debugging.FootholdDebugLogger>())
            {
                gameObject.AddComponent<MapleClient.GameView.Debugging.FootholdDebugLogger>();
            }
        }

        private void Start()
        {
            // Lock FPS to 60
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1; // Enable VSync (60Hz on most monitors)
            
            Debug.Log($"[GameManager] FPS locked to 60 - Target: {Application.targetFrameRate}, VSync: {QualitySettings.vSyncCount}");
            
            // Add test component temporarily
            gameObject.AddComponent<MapleClient.GameData.TestReNX>();
            
            // Add runtime collision test
            // gameObject.AddComponent<RuntimeCollisionTest>();
            
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Initialize asset provider
            assetProvider = new NXDataManager();
            assetProvider.Initialize();
            
            // Create FootholdService
            footholdService = new FootholdService();
            
            // Initialize data layer with FootholdService
            mapLoader = new NxMapLoader("", footholdService); // Now uses NX file data (or mock data if files not found)
            
            // Initialize input
            inputProvider = new UnityInputProvider();
            
            // Initialize map renderer
            GameObject mapRendererObject = new GameObject("MapRenderer");
            mapRenderer = mapRendererObject.AddComponent<MapRenderer>();
            mapRenderer.Initialize(assetProvider);
            
            // Create VisualEffectManager
            GameObject effectManagerObj = new GameObject("VisualEffectManager");
            effectManagerObj.AddComponent<VisualEffectManager>();
            
            // Initialize platform bridge for physics
            GameObject platformBridgeObject = new GameObject("PlatformBridge");
            platformBridge = platformBridgeObject.AddComponent<SimplePlatformBridge>();
            
            // Initialize network if enabled
            if (useNetworking)
            {
                networkClient = new MapleStoryNetworkClient();
                networkClient.OnError += OnNetworkError;
                networkClient.OnConnected += OnNetworkConnected;
                networkClient.OnDisconnected += OnNetworkDisconnected;
            }
            
            // Initialize game logic with FootholdService
            gameWorld = new GameWorld(inputProvider, mapLoader, useNetworking ? networkClient : null, assetProvider, footholdService);
            gameWorld.MapLoaded += OnMapLoaded;
            gameWorld.MonsterSpawned += OnMonsterSpawned;
            gameWorld.MonsterDied += OnMonsterDied;
            gameWorld.ItemDropped += OnItemDropped;
            gameWorld.ItemPickedUp += OnItemPickedUp;
            gameWorld.OnChatMessageReceived += OnChatMessageReceived;
            
            // Listen to player events
            gameWorld.Player.Landed += OnPlayerLanded;
            
            // Create UI
            CreateUI();
            
            // Start the game
            if (useNetworking)
            {
                // Connect to server
                ConnectToServer();
            }
            else
            {
                // Start in offline mode - spawn at origin
                // Platform will be adjusted to Y=200 in MapleStory coordinates
                // Spawn player higher up so they don't gain too much velocity before hitting ground
                // Y=150 MapleStory = Y=-1.5 Unity
                Debug.Log($"[FOOTHOLD_COLLISION] GameManager: Calling InitializePlayer with Y=-1.5");
                gameWorld.InitializePlayer(1, "Player", 100, 100, 100, 100, 0, -1.5f);
                
                // Load initial map (Henesys)
                Debug.Log($"[FOOTHOLD_COLLISION] GameManager: Loading map 100000000");
                gameWorld.LoadMap(100000000);
            }
        }

        private void Update()
        {
            if (gameWorld != null)
            {
                // Process input and non-physics game logic in Update
                gameWorld.ProcessInput();
                UpdateOtherPlayers();
                
                // Update visual interpolation for smooth rendering
                UpdateVisualInterpolation();
                
                // Check for ladder proximity
                CheckLadderProximity();
            }
            
            // Process network events on main thread
            if (networkClient != null)
            {
                networkClient.ProcessMainThreadActions();
            }
        }
        
        private void FixedUpdate()
        {
            if (gameWorld != null)
            {
                // Physics updates happen at fixed 60 FPS timestep
                gameWorld.UpdatePhysics(Time.fixedDeltaTime);
            }
        }
        
        private void UpdateVisualInterpolation()
        {
            // Get interpolation factor from physics system
            float interpolationFactor = gameWorld.GetPhysicsInterpolationFactor();
            
            // Apply interpolation to player view
            if (currentPlayerView != null && gameWorld.Player != null)
            {
                currentPlayerView.SetInterpolationFactor(interpolationFactor);
            }
            
            // Apply interpolation to monster views
            foreach (var kvp in monsterViews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetInterpolationFactor(interpolationFactor);
                }
            }
            
            // Apply interpolation to other player views
            foreach (var kvp in otherPlayerViews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetInterpolationFactor(interpolationFactor);
                }
            }
        }

        private void OnMapLoaded(GameLogic.MapData mapData)
        {
            Debug.Log($"Map loaded: {mapData.Name} (ID: {mapData.MapId})");
            
            // Clean up old map visuals
            CleanupMapVisuals();
            
            // Render the map using actual MapleStory assets
            if (mapRenderer != null)
            {
                mapRenderer.RenderMap(mapData);
            }
            
            // Initialize platform bridge
            if (platformBridge != null)
            {
                // Extract platforms from the Unity scene
                platformBridge.ExtractPlatformsFromScene(mapData);
            }
            
            // Create SIMPLE WORKING player instead of broken MapleStory player
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                playerObject = new GameObject("Player");
                
                // Use simple working player controller
                playerController = playerObject.AddComponent<SimplePlayerController>();
                playerController.SetGameLogicPlayer(gameWorld.Player);
                playerController.SetGameWorld(gameWorld);
                
                // Position at spawn point
                var spawnPos = gameWorld.Player.Position;
                playerObject.transform.position = new Vector3(spawnPos.X, spawnPos.Y, 0);
                
                Debug.Log($"Simple player created at: ({spawnPos.X}, {spawnPos.Y})");
                
                // Setup camera to follow player
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // Remove ALL existing camera controllers to avoid conflicts
                    var cameraController = mainCamera.GetComponent<CameraController>();
                    if (cameraController != null)
                    {
                        DestroyImmediate(cameraController);
                    }
                    
                    var existingFollow = mainCamera.GetComponent<SimpleCameraFollow>();
                    if (existingFollow != null)
                    {
                        DestroyImmediate(existingFollow);
                    }
                    
                    // Add simple camera follow ONCE
                    var cameraFollow = mainCamera.gameObject.AddComponent<SimpleCameraFollow>();
                    cameraFollow.target = playerObject.transform;
                    cameraFollow.offset = new Vector3(0, 0, -10); // Keep camera at same Y level as player
                    cameraFollow.smoothSpeed = 8f; // Faster for less lag
                    cameraFollow.enableSmoothing = true;
                    cameraFollow.useCameraBounds = false; // Disable bounds temporarily
                    cameraFollow.enableLookahead = false; // Disable lookahead to reduce jitter
                    
                    // Reset camera to target position
                    cameraFollow.ResetToTarget();
                    
                    Debug.Log($"[GameManager] Camera follow setup complete. Target: {cameraFollow.target.name}, Camera position: {mainCamera.transform.position}");
                    
                    // Start coroutine to verify camera is working
                    StartCoroutine(VerifyCameraFollow(cameraFollow, playerObject.transform));
                }
                else
                {
                    Debug.LogError("[GameManager] No main camera found! Cannot setup camera follow.");
                }
            }
            
            // Add foothold debug visualizer if in editor
            #if UNITY_EDITOR
            if (GameObject.Find("FootholdDebugVisualizer") == null)
            {
                GameObject debugObj = new GameObject("FootholdDebugVisualizer");
                var visualizer = debugObj.AddComponent<MapleClient.GameView.Debugging.FootholdDebugVisualizer>();
                visualizer.SetFootholdService(footholdService);
                Debug.Log("[GameManager] Created FootholdDebugVisualizer for debugging");
            }
            #endif
            
            // Platform, ladder and portal visuals are now handled by MapRenderer
            // which uses actual MapleStory sprites from NX files
            
            // Still create portal interaction zones
            CreatePortalVisuals(mapData);
        }
        
        private void CreatePlatformVisuals(GameLogic.MapData mapData)
        {
            GameObject platformContainer = GameObject.Find("Platforms");
            if (platformContainer == null)
            {
                platformContainer = new GameObject("Platforms");
            }
            
            foreach (var platform in mapData.Platforms)
            {
                GameObject platformObject = new GameObject($"Platform_{platform.Id}");
                platformObject.transform.parent = platformContainer.transform;
                
                LineRenderer lineRenderer = platformObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(platform.X1 / 100f, platform.Y1 / 100f, 0));
                lineRenderer.SetPosition(1, new Vector3(platform.X2 / 100f, platform.Y2 / 100f, 0));
            }
        }
        
        private void CreateLadderVisuals(GameLogic.MapData mapData)
        {
            if (mapData.Ladders == null || mapData.Ladders.Count == 0)
                return;
                
            GameObject ladderContainer = GameObject.Find("Ladders");
            if (ladderContainer == null)
            {
                ladderContainer = new GameObject("Ladders");
            }
            
            int ladderIndex = 0;
            foreach (var ladder in mapData.Ladders)
            {
                GameObject ladderObject = new GameObject($"Ladder_{ladderIndex++}");
                ladderObject.transform.parent = ladderContainer.transform;
                
                LadderView ladderView = ladderObject.AddComponent<LadderView>();
                ladderView.SetLadder(ladder);
            }
        }

        private void CreatePortalVisuals(GameLogic.MapData mapData)
        {
            if (mapData.Portals == null || mapData.Portals.Count == 0)
                return;
                
            GameObject portalContainer = GameObject.Find("Portals");
            if (portalContainer == null)
            {
                portalContainer = new GameObject("Portals");
            }
            
            foreach (var portal in mapData.Portals)
            {
                GameObject portalObject = new GameObject($"Portal_{portal.Name}");
                portalObject.transform.parent = portalContainer.transform;
                
                PortalView portalView = portalObject.AddComponent<PortalView>();
                portalView.SetPortal(portal);
            }
        }

        private void OnMonsterSpawned(Monster monster)
        {
            GameObject monsterObject = new GameObject($"Monster_{monster.MonsterId}");
            MonsterView monsterView = monsterObject.AddComponent<MonsterView>();
            monsterView.SetMonster(monster);
            
            monsterViews[monster] = monsterView;
        }

        private void OnMonsterDied(Monster monster)
        {
            if (monsterViews.TryGetValue(monster, out MonsterView view))
            {
                monsterViews.Remove(monster);
                // The MonsterView handles its own destruction animation
            }
        }

        private void OnItemDropped(DroppedItem item)
        {
            GameObject itemObject = new GameObject($"DroppedItem_{item.ItemId}");
            DroppedItemView itemView = itemObject.AddComponent<DroppedItemView>();
            itemView.SetDroppedItem(item);
            
            droppedItemViews[item] = itemView;
        }

        private void OnItemPickedUp(int itemId, int quantity)
        {
            // Find and remove the dropped item view
            DroppedItem itemToRemove = null;
            foreach (var kvp in droppedItemViews)
            {
                if (kvp.Key.ItemId == itemId)
                {
                    itemToRemove = kvp.Key;
                    break;
                }
            }

            if (itemToRemove != null && droppedItemViews.TryGetValue(itemToRemove, out DroppedItemView view))
            {
                droppedItemViews.Remove(itemToRemove);
                Destroy(view.gameObject);
            }
        }

        private void CreateUI()
        {
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Add InventoryView
            if (canvas.GetComponent<InventoryView>() == null)
            {
                canvas.gameObject.AddComponent<InventoryView>();
            }
            
            // Add StatusBar
            if (canvas.GetComponent<StatusBar>() == null)
            {
                canvas.gameObject.AddComponent<StatusBar>();
            }
            
            // Add ExperienceBar
            if (canvas.GetComponent<ExperienceBar>() == null)
            {
                canvas.gameObject.AddComponent<ExperienceBar>();
            }
            
            // Add SkillMenu
            if (canvas.GetComponent<SkillMenu>() == null)
            {
                canvas.gameObject.AddComponent<SkillMenu>();
            }
            
            // Add SkillBar
            if (canvas.GetComponent<SkillBar>() == null)
            {
                canvas.gameObject.AddComponent<SkillBar>();
                
                // Add movement state UI
                canvas.gameObject.AddComponent<MovementStateUI>();
                
                // Add input prompt manager
                canvas.gameObject.AddComponent<InputPromptManager>();
            }
        }

        private void OnPlayerLanded()
        {
            // Player landed event - can be used for effects or sounds
        }
        
        private void CheckLadderProximity()
        {
            if (playerController == null || gameWorld?.CurrentMap?.Ladders == null) return;
            
            var playerPos = gameWorld.Player.Position;
            bool nearLadder = false;
            
            foreach (var ladder in gameWorld.CurrentMap.Ladders)
            {
                // Check if player is within ladder bounds
                float ladderX = ladder.X / 100f;
                float distance = System.Math.Abs(playerPos.X - ladderX);
                
                if (distance < 0.3f && // Within 30 pixels horizontally
                    playerPos.Y >= ladder.Y2 / 100f && // Above bottom
                    playerPos.Y <= ladder.Y1 / 100f) // Below top
                {
                    nearLadder = true;
                    break;
                }
            }
            
            playerController.ShowLadderPrompt(nearLadder && gameWorld.Player.State != PlayerState.Climbing);
        }

        private void CleanupMapVisuals()
        {
            // Clean up platforms
            GameObject platformContainer = GameObject.Find("Platforms");
            if (platformContainer != null)
            {
                foreach (Transform child in platformContainer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clean up ladders
            GameObject ladderContainer = GameObject.Find("Ladders");
            if (ladderContainer != null)
            {
                foreach (Transform child in ladderContainer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clean up portals
            GameObject portalContainer = GameObject.Find("Portals");
            if (portalContainer != null)
            {
                foreach (Transform child in portalContainer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clean up monsters
            foreach (var kvp in monsterViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            monsterViews.Clear();
            
            // Clean up dropped items
            foreach (var kvp in droppedItemViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            droppedItemViews.Clear();
        }

        // Network event handlers
        private void OnNetworkConnected(string message)
        {
            Debug.Log($"Network connected: {message}");
            
            // If we have login UI, enable login
            // For now, auto-login for testing
            if (networkClient != null)
            {
                networkClient.SendLogin("test", "test");
            }
        }
        
        private void OnNetworkDisconnected(string message)
        {
            Debug.Log($"Network disconnected: {message}");
        }
        
        private void OnNetworkError(string error)
        {
            Debug.LogError($"Network error: {error}");
        }
        
        private void OnChatMessageReceived(GameLogic.Interfaces.ChatMessage message)
        {
            // TODO: Display in chat UI
            Debug.Log($"[{message.Type}] {message.Sender}: {message.Message}");
        }
        
        // Other player management
        private void UpdateOtherPlayers()
        {
            // Remove views for players that left
            var playersToRemove = new List<Player>();
            foreach (var kvp in otherPlayerViews)
            {
                if (!gameWorld.Players.Contains(kvp.Key))
                {
                    playersToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var player in playersToRemove)
            {
                if (otherPlayerViews.TryGetValue(player, out PlayerView view))
                {
                    otherPlayerViews.Remove(player);
                    Destroy(view.gameObject);
                }
            }
            
            // Add views for new players
            foreach (var player in gameWorld.Players)
            {
                if (player != gameWorld.Player && !otherPlayerViews.ContainsKey(player))
                {
                    GameObject playerObject = Instantiate(playerViewPrefab.gameObject);
                    playerObject.name = $"OtherPlayer_{player.Name}";
                    PlayerView view = playerObject.GetComponent<PlayerView>();
                    view.SetPlayer(player);
                    otherPlayerViews[player] = view;
                }
            }
        }
        
        public void ConnectToServer()
        {
            if (networkClient != null && !networkClient.IsConnected)
            {
                networkClient.Connect(serverHost, loginPort);
            }
        }
        
        private IEnumerator VerifyCameraFollow(SimpleCameraFollow cameraFollow, Transform playerTransform)
        {
            yield return new WaitForSeconds(0.5f); // Wait half a second
            
            if (cameraFollow == null)
            {
                Debug.LogError("[GameManager] Camera follow component was destroyed!");
                yield break;
            }
            
            if (cameraFollow.target == null)
            {
                Debug.LogWarning("[GameManager] Camera follow lost its target, reassigning...");
                cameraFollow.target = playerTransform;
            }
            
            Debug.Log($"[GameManager] Camera follow verification: Target = {cameraFollow.target?.name ?? "null"}, Camera pos = {cameraFollow.transform.position}");
        }
        
        private void OnDestroy()
        {
            // Disconnect network
            if (networkClient != null)
            {
                networkClient.Disconnect();
            }
            
            // Shutdown asset provider
            if (assetProvider != null)
            {
                assetProvider.Shutdown();
            }
            
            if (gameWorld != null)
            {
                gameWorld.MapLoaded -= OnMapLoaded;
                gameWorld.MonsterSpawned -= OnMonsterSpawned;
                gameWorld.MonsterDied -= OnMonsterDied;
                gameWorld.ItemDropped -= OnItemDropped;
                gameWorld.ItemPickedUp -= OnItemPickedUp;
                gameWorld.OnChatMessageReceived -= OnChatMessageReceived;
                
                if (gameWorld.Player != null)
                {
                    gameWorld.Player.Landed -= OnPlayerLanded;
                }
            }
        }
    }
}