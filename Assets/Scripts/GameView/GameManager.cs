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
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static System.Func<int, GameObject> MapSceneFactory { get; set; }
        public event System.Action PlayerViewRepositioned;
        private GameObject activeMapVisuals;
        private GameObject runtimePortals;
        [SerializeField] private bool enableDiagnostics = false;
        [SerializeField] private int startingMapId = 100000000;
        public int StartingMapId { get => startingMapId; set => startingMapId = value; }
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
        
        private NxMobAnimations mobAnimations;
        private Dictionary<Monster, MonsterView> monsterViews = new Dictionary<Monster, MonsterView>();
        private Dictionary<DroppedItem, DroppedItemView> droppedItemViews = new Dictionary<DroppedItem, DroppedItemView>();
        private Dictionary<Player, PlayerView> otherPlayerViews = new Dictionary<Player, PlayerView>();
        
        private MapRenderer mapRenderer;
        private SimplePlatformBridge platformBridge;
        private SimplePlayerController playerController;
        
        public GameWorld World => gameWorld;
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
            if (enableDiagnostics && !GetComponent<PhysicsDebugger>())
            {
                gameObject.AddComponent<PhysicsDebugger>();
            }
            
            // Add foothold debug logger to capture console output
            if (enableDiagnostics && !GetComponent<MapleClient.GameView.Debugging.FootholdDebugLogger>())
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

            
            // Add runtime collision test
            // gameObject.AddComponent<RuntimeCollisionTest>();
            
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Initialize asset provider
            assetProvider = NXDataManagerSingleton.Instance.DataManager;
            mobAnimations = new NxMobAnimations(NXDataManagerSingleton.Instance.DataManager);
            
            // Create FootholdService
            footholdService = new FootholdService();
            
            // Initialize data layer with FootholdService
            mapLoader = new NxMapLoader("", footholdService); // Now uses NX file data (or mock data if files not found)
            
            // Initialize input
            inputProvider = new UnityInputProvider();
            
            // Create VisualEffectManager
            GameObject effectManagerObj = new GameObject("VisualEffectManager");
            effectManagerObj.AddComponent<VisualEffectManager>();
            
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
            gameObject.AddComponent<SkillProjectileView>().Bind(gameWorld);
            gameWorld.MapLoaded += OnMapLoaded;
            gameWorld.PlayerRecovered += SnapPlayerView;
            gameWorld.PlayerTeleported += SnapPlayerView;
            gameWorld.MonsterSpawned += OnMonsterSpawned;
            gameWorld.ItemDropped += OnItemDropped;
            gameWorld.DropRemoved += OnDropRemoved;
            gameWorld.OnChatMessageReceived += OnChatMessageReceived;
            
            // Listen to player events
            gameWorld.Player.Landed += OnPlayerLanded;
            
            // Create UI
            if (!useNetworking) gameObject.AddComponent<LocalProgressController>();
            CreateUI();
            
            // Start the game
            if (useNetworking)
            {
                // Connect to server
                ConnectToServer();
            }
            else
            {
                gameWorld.InitializePlayer(1, "Player", 100, 100, 100, 100, 0, 0);
                gameWorld.LoadMap(startingMapId);
            }
        }

        private void Update()
        {
            if (gameWorld != null)
            {
                // One clock owns simulation and interpolation. Unity FixedUpdate is unrelated.
                gameWorld.ProcessInput();
                gameWorld.UpdatePhysics(Time.deltaTime);
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
            
            if (activeMapVisuals != null && activeMapVisuals.name != $"Map_{mapData.MapId}")
            {
                activeMapVisuals.SetActive(false);
                Destroy(activeMapVisuals);
                activeMapVisuals = null;
            }
            activeMapVisuals = GameObject.Find($"Map_{mapData.MapId}");
            if (activeMapVisuals == null && MapSceneFactory != null)
                activeMapVisuals = MapSceneFactory(mapData.MapId);
            if (activeMapVisuals == null)
            {
                if (mapRenderer == null)
                {
                    mapRenderer = new GameObject("MapRenderer").AddComponent<MapRenderer>();
                    mapRenderer.Initialize(assetProvider);
                }
                mapRenderer.RenderMap(mapData);
            }
            // Keep the same player and camera listeners across map changes.
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
            {
                playerObject = new GameObject("Player");
                playerController = playerObject.AddComponent<SimplePlayerController>();
                playerController.SetGameLogicPlayer(gameWorld.Player);
                playerController.SetGameWorld(gameWorld);
            }
            else
                playerController = playerObject.GetComponent<SimplePlayerController>();

            var camera = Camera.main;
            (playerObject.GetComponent<WorldLabelAnchor>() ?? playerObject.AddComponent<WorldLabelAnchor>()).Bind(gameWorld.Player);
            if (camera != null)
            {
                // HeavenClient clears to black; uncovered space around small interiors is not sky.
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                var legacyController = camera.GetComponent<CameraController>();
                if (legacyController != null)
                {
                    legacyController.enabled = false;
                    Destroy(legacyController);
                }
                var follow = camera.GetComponent<SimpleCameraFollow>();
                if (follow == null) follow = camera.gameObject.AddComponent<SimpleCameraFollow>();
                follow.target = playerObject.transform;
                follow.offset = new Vector3(0, 0, -10);
                follow.smoothSpeed = 8f;
                follow.enableSmoothing = true;
                follow.useCameraBounds = false; // The generated map owns its VR bounds.
                follow.enableLookahead = false;
            }
            SnapPlayerView();

            // Add foothold debug visualizer if in editor
            #if UNITY_EDITOR
            if (enableDiagnostics && GameObject.Find("FootholdDebugVisualizer") == null)
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
            if (activeMapVisuals == null)
                CreatePortalVisuals(mapData);
        }
        
        private void SnapPlayerView()
        {
            if (playerController == null || gameWorld?.Player == null) return;
            var position = gameWorld.Player.Position;
            playerController.transform.position = new Vector3(position.X, position.Y, 0);
            Camera.main?.GetComponent<SimpleCameraFollow>()?.ResetToTarget();
            PlayerViewRepositioned?.Invoke();
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
                
            runtimePortals = new GameObject("RuntimePortals");
            GameObject portalContainer = runtimePortals;
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
            monsterView.SetMonster(monster, mobAnimations);
            monsterView.Removed += OnMonsterViewRemoved;
            
            monsterViews[monster] = monsterView;
        }

        private void OnMonsterViewRemoved(MonsterView view)
        {
            if (view.Model != null && monsterViews.TryGetValue(view.Model, out var current) && current == view)
                monsterViews.Remove(view.Model);
        }

        private void OnItemDropped(DroppedItem item)
        {
            GameObject itemObject = new GameObject($"DroppedItem_{item.ItemId}");
            DroppedItemView itemView = itemObject.AddComponent<DroppedItemView>();
            itemView.SetDroppedItem(item);
            
            droppedItemViews[item] = itemView;
        }

        private void OnDropRemoved(DroppedItem item)
        {
            if (droppedItemViews.TryGetValue(item, out DroppedItemView view))
            {
                droppedItemViews.Remove(item);
                if (view != null) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
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

            canvas.pixelPerfect = true;
            if (canvas.GetComponent<ClassicWorldLabels>() == null) canvas.gameObject.AddComponent<ClassicWorldLabels>();
            if (canvas.GetComponent<ClassicMonsterHealthView>() == null) canvas.gameObject.AddComponent<ClassicMonsterHealthView>();
            (canvas.GetComponent<ClassicMinimapView>() ?? canvas.gameObject.AddComponent<ClassicMinimapView>()).Bind(gameWorld);
            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

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
            
            var combatFeedback = canvas.GetComponent<PlayerCombatFeedback>() ?? canvas.gameObject.AddComponent<PlayerCombatFeedback>();
            combatFeedback.Bind(gameWorld);
            if (!useNetworking && canvas.GetComponent<CharacterProgressionView>() == null)
                canvas.gameObject.AddComponent<CharacterProgressionView>();
            if (!useNetworking) (canvas.GetComponent<LocalPlayMenu>() ?? canvas.gameObject.AddComponent<LocalPlayMenu>()).Bind(this);
            if (!useNetworking)
            {
                (canvas.GetComponent<ClassicQuestView>() ?? canvas.gameObject.AddComponent<ClassicQuestView>()).Bind(gameWorld);
                (canvas.GetComponent<ClassicNpcDialogue>() ?? canvas.gameObject.AddComponent<ClassicNpcDialogue>()).Bind(gameWorld);
                (canvas.GetComponent<ClassicQuestIndicators>() ?? canvas.gameObject.AddComponent<ClassicQuestIndicators>()).Bind(gameWorld);
                (canvas.GetComponent<ClassicNpcBubbles>() ?? canvas.gameObject.AddComponent<ClassicNpcBubbles>()).Bind(gameWorld);
                if(canvas.GetComponent<ClassicCursorView>()==null)canvas.gameObject.AddComponent<ClassicCursorView>();
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
                if (enableDiagnostics) canvas.gameObject.AddComponent<MovementStateUI>();
                
                // Add input prompt manager
                canvas.gameObject.AddComponent<InputPromptManager>();
            }
            ((UnityInputProvider)inputProvider).Quickslots = canvas.GetComponent<SkillBar>();
        }

        private void OnPlayerLanded()
        {
            // Player landed event - can be used for effects or sounds
        }
        
        private void CheckLadderProximity()
        {
            if (playerController == null || gameWorld?.CurrentMap?.Ladders == null) return;
            
            var player = gameWorld.Player;
            bool nearLadder = player.CanClimb && gameWorld.CurrentMap.Ladders.Any(ladder =>
                ladder.CanEnter(player.Position, true) || ladder.CanEnter(player.Position, false));
            playerController.ShowLadderPrompt(nearLadder && player.State != PlayerState.Climbing);
        }

        private void CleanupMapVisuals()
        {
            if (runtimePortals != null)
            {
                runtimePortals.SetActive(false);
                Destroy(runtimePortals);
                runtimePortals = null;
            }
            // Clean up monsters
            foreach (var kvp in monsterViews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                    Destroy(kvp.Value.gameObject);
                }
            }
            monsterViews.Clear();
            
            // Clean up dropped items
            foreach (var kvp in droppedItemViews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                    Destroy(kvp.Value.gameObject);
                }
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
            
            // The persistent NXDataManagerSingleton owns shared asset lifetime.
            
            if (gameWorld != null)
            {
                gameWorld.MapLoaded -= OnMapLoaded;
                gameWorld.PlayerRecovered -= SnapPlayerView;
                gameWorld.PlayerTeleported -= SnapPlayerView;
                gameWorld.MonsterSpawned -= OnMonsterSpawned;
                gameWorld.ItemDropped -= OnItemDropped;
                gameWorld.DropRemoved -= OnDropRemoved;
                gameWorld.OnChatMessageReceived -= OnChatMessageReceived;
                
                if (gameWorld.Player != null)
                {
                    gameWorld.Player.Landed -= OnPlayerLanded;
                }
            }
        }
    }
}
