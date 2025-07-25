using UnityEngine;
using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameData;
using MapleClient.GameView.UI;

namespace MapleClient.GameView
{
    public class GameManager : MonoBehaviour
    {
        private GameWorld gameWorld;
        private IMapLoader mapLoader;
        private IInputProvider inputProvider;

        [SerializeField] private PlayerView playerViewPrefab;
        private PlayerView currentPlayerView;
        
        private Dictionary<Monster, MonsterView> monsterViews = new Dictionary<Monster, MonsterView>();
        private Dictionary<DroppedItem, DroppedItemView> droppedItemViews = new Dictionary<DroppedItem, DroppedItemView>();
        
        public Player Player => gameWorld?.Player;

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Initialize data layer
            mapLoader = new NxMapLoader(); // Now uses NX file data (or mock data if files not found)
            
            // Initialize input
            inputProvider = new UnityInputProvider();
            
            // Initialize game logic
            gameWorld = new GameWorld(mapLoader, inputProvider);
            gameWorld.MapLoaded += OnMapLoaded;
            gameWorld.MonsterSpawned += OnMonsterSpawned;
            gameWorld.MonsterDied += OnMonsterDied;
            gameWorld.ItemDropped += OnItemDropped;
            gameWorld.ItemPickedUp += OnItemPickedUp;
            
            // Listen to player events
            gameWorld.Player.Landed += OnPlayerLanded;
            
            // Create UI
            CreateUI();
            
            // Load initial map (Henesys)
            gameWorld.LoadMap(100000000);
        }

        private void Update()
        {
            if (gameWorld != null)
            {
                gameWorld.Update(Time.deltaTime);
            }
        }

        private void OnMapLoaded(GameLogic.MapData mapData)
        {
            Debug.Log($"Map loaded: {mapData.Name} (ID: {mapData.MapId})");
            
            // Clean up old map visuals
            CleanupMapVisuals();
            
            // Create visual representation
            if (currentPlayerView == null)
            {
                GameObject playerObject = new GameObject("Player");
                currentPlayerView = playerObject.AddComponent<PlayerView>();
                currentPlayerView.SetPlayer(gameWorld.Player);
                
                // Position player at spawn point
                var spawnPortal = mapData.Portals.Find(p => p.Type == GameLogic.PortalType.Spawn);
                if (spawnPortal != null)
                {
                    gameWorld.Player.Position = new GameLogic.Vector2(spawnPortal.X, spawnPortal.Y);
                }
                
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
            
            // Create platform visuals
            CreatePlatformVisuals(mapData);
            
            // Create ladder visuals
            CreateLadderVisuals(mapData);
            
            // Create portal visuals
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

        private void OnDestroy()
        {
            if (gameWorld != null)
            {
                gameWorld.MapLoaded -= OnMapLoaded;
                gameWorld.MonsterSpawned -= OnMonsterSpawned;
                gameWorld.MonsterDied -= OnMonsterDied;
                gameWorld.ItemDropped -= OnItemDropped;
                gameWorld.ItemPickedUp -= OnItemPickedUp;
                
                if (gameWorld.Player != null)
                {
                    gameWorld.Player.Landed -= OnPlayerLanded;
                }
            }
        }
    }
}