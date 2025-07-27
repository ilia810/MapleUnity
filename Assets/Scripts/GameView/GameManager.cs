using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using MapleClient.GameData;
using MapleClient.GameView.UI;
using GameData.Network;
using GameData;

namespace MapleClient.GameView
{
    public class GameManager : MonoBehaviour
    {
        private GameWorld gameWorld;
        private IMapLoader mapLoader;
        private IInputProvider inputProvider;
        private IAssetProvider assetProvider;
        private MapleStoryNetworkClient networkClient;
        
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
        
        public Player Player => gameWorld?.Player;
        public SkillManager SkillManager => gameWorld?.SkillManager;

        private void Start()
        {
            // Add test component temporarily
            gameObject.AddComponent<MapleClient.GameData.TestReNX>();
            
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Initialize asset provider
            assetProvider = new NXDataManager();
            assetProvider.Initialize();
            
            // Initialize data layer
            mapLoader = new NxMapLoader(); // Now uses NX file data (or mock data if files not found)
            
            // Initialize input
            inputProvider = new UnityInputProvider();
            
            // Initialize map renderer
            GameObject mapRendererObject = new GameObject("MapRenderer");
            mapRenderer = mapRendererObject.AddComponent<MapRenderer>();
            mapRenderer.Initialize(assetProvider);
            
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
            
            // Initialize game logic
            gameWorld = new GameWorld(inputProvider, mapLoader, useNetworking ? networkClient : null, assetProvider);
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
                gameWorld.InitializePlayer(1, "Player", 100, 100, 100, 100, 0, 2);
                
                // Load initial map (Henesys)
                gameWorld.LoadMap(100000000);
            }
        }

        private void Update()
        {
            if (gameWorld != null)
            {
                gameWorld.Update(Time.deltaTime);
                UpdateOtherPlayers();
            }
            
            // Process network events on main thread
            if (networkClient != null)
            {
                networkClient.ProcessMainThreadActions();
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
            
            // Create visual representation
            if (currentPlayerView == null)
            {
                GameObject playerObject = new GameObject("Player");
                currentPlayerView = playerObject.AddComponent<PlayerView>();
                currentPlayerView.SetCharacterDataProvider(assetProvider.CharacterData);
                currentPlayerView.SetPlayer(gameWorld.Player);
                
                // Add fallback renderer to ensure player is always visible
                playerObject.AddComponent<PlayerFallbackRenderer>();
                
                // Add debug visual component temporarily
                playerObject.AddComponent<PlayerDebugVisual>();
                
                // Add text debugger to find "12345678" issue
                playerObject.AddComponent<PlayerTextDebugger>();
                
                // Player position is already set by GameWorld's PlayerSpawnManager
                Debug.Log($"Player spawned at: ({gameWorld.Player.Position.X}, {gameWorld.Player.Position.Y})");
                Debug.Log($"Player GameObject position: {playerObject.transform.position}");
                
                // Setup camera to follow player
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    CameraController cameraController = mainCamera.GetComponent<CameraController>();
                    if (cameraController == null)
                    {
                        cameraController = mainCamera.gameObject.AddComponent<CameraController>();
                    }
                    cameraController.SetTarget(playerObject.transform);
                }
            }
            
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
            }
        }

        private void OnPlayerLanded()
        {
            // Player landed event - can be used for effects or sounds
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