using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameLogic.Core
{
    public class GameWorld
    {
        private readonly IMapLoader mapLoader;
        private readonly IInputProvider inputProvider;
        private readonly INetworkClient networkClient;
        private readonly IAssetProvider assetProvider;
        private readonly PlayerSpawnManager spawnManager;
        private readonly PhysicsUpdateManager physicsManager;
        private readonly IFootholdService footholdService;
        private MapData currentMap;
        private Player player;
        private SkillManager skillManager;
        private List<Player> players; // Other players in the world
        private List<Monster> monsters;
        private List<DroppedItem> droppedItems;
        private Combat combat;
        private const float ItemPickupRange = 50f;

        public MapData CurrentMap => currentMap;
        public int CurrentMapId => currentMap?.MapId ?? -1;
        public Player Player => player;
        public SkillManager SkillManager => skillManager;
        public IReadOnlyList<Player> Players => players; // All players including local
        public IReadOnlyList<Monster> Monsters => monsters;
        public IReadOnlyList<DroppedItem> DroppedItems => droppedItems;

        public event Action<MapData> MapLoaded;
        public event Action<Monster> MonsterSpawned;
        public event Action<Monster> MonsterDied;
        public event Action<int, int> ItemPickedUp;
        public event Action<DroppedItem> ItemDropped;
        public event Action<ChatMessage> OnChatMessageReceived;

        public GameWorld(IInputProvider inputProvider, IMapLoader mapLoader, INetworkClient networkClient = null, IAssetProvider assetProvider = null, IFootholdService footholdService = null)
        {
            this.mapLoader = mapLoader;
            this.inputProvider = inputProvider;
            this.networkClient = networkClient;
            this.assetProvider = assetProvider;
            this.footholdService = footholdService ?? new FootholdService();
            this.spawnManager = new PlayerSpawnManager(this.footholdService);
            this.physicsManager = new PhysicsUpdateManager();
            this.player = new Player(this.footholdService);
            this.players = new List<Player>();
            this.monsters = new List<Monster>();
            this.droppedItems = new List<DroppedItem>();
            this.combat = new Combat();
            
            // Initialize skill manager if asset provider is available
            if (assetProvider != null)
            {
                this.skillManager = new SkillManager(player, assetProvider, networkClient);
            }
            
            // Register player with physics system
            this.physicsManager.RegisterPhysicsObject(this.player);
            
            // Listen for player landed event
            this.player.Landed += OnPlayerLanded;
            
            // Subscribe to network events if available
            if (networkClient != null)
            {
                SubscribeToNetworkEvents();
            }
        }

        private void SubscribeToNetworkEvents()
        {
            networkClient.OnPlayerJoin += HandlePlayerJoin;
            networkClient.OnPlayerMove += HandlePlayerMove;
            networkClient.OnPlayerLeave += HandlePlayerLeave;
            networkClient.OnMobSpawn += HandleMobSpawn;
            networkClient.OnMobDespawn += HandleMobDespawn;
            networkClient.OnMobMove += HandleMobMove;
            networkClient.OnItemDrop += HandleItemDrop;
            networkClient.OnItemPickup += HandleItemPickup;
            networkClient.OnChatMessage += HandleChatMessage;
            networkClient.OnPlayerHpMpUpdate += HandlePlayerHpMpUpdate;
            networkClient.OnMapChange += HandleMapChange;
        }

        public void LoadMap(int mapId)
        {
            var mapData = mapLoader.GetMap(mapId);
            if (mapData != null)
            {
                currentMap = mapData;
                OnMapLoaded();
            }
        }

        // Process input and game logic (called from Update)
        public void ProcessInput()
        {
            // Handle input
            if (inputProvider != null && player != null)
            {
                var previousPosition = player.Position;
                var wasJumping = player.IsJumping;
                
                // Movement - handle both inputs independently
                player.MoveLeft(inputProvider.IsLeftPressed);
                player.MoveRight(inputProvider.IsRightPressed);

                // Jump
                if (inputProvider.IsJumpPressed)
                {
                    player.Jump();
                    if (!wasJumping && player.IsJumping && networkClient != null)
                    {
                        networkClient.SendJump();
                    }
                }
                else
                {
                    // Release jump key when not pressed
                    player.ReleaseJump();
                }

                // Attack
                if (inputProvider.IsAttackPressed)
                {
                    combat.PerformBasicAttack(player, monsters, 100); // 100 pixel range
                    if (networkClient != null)
                    {
                        networkClient.SendAttack(0, new byte[0]); // Basic attack
                    }
                }
                
                // Send movement updates to network
                if (networkClient != null && (previousPosition.X != player.Position.X || previousPosition.Y != player.Position.Y))
                {
                    networkClient.SendMove(player.Position.X, player.Position.Y, new byte[0]);
                }

                // Crouch
                player.Crouch(inputProvider.IsDownPressed && player.IsGrounded);

                // Ladder climbing
                if (player.State != PlayerState.Climbing)
                {
                    // Check if player is at a ladder and pressing up
                    if (inputProvider.IsUpPressed && currentMap?.Ladders != null)
                    {
                        var ladder = currentMap.Ladders.FirstOrDefault(l => l.ContainsPosition(player.Position));
                        if (ladder != null)
                        {
                            player.StartClimbing(ladder);
                        }
                    }
                }
                else
                {
                    // Handle climbing input
                    player.ClimbUp(inputProvider.IsUpPressed);
                    player.ClimbDown(inputProvider.IsDownPressed);
                }

                // Check for portal interaction when not climbing
                if (player.State != PlayerState.Climbing && inputProvider.IsUpPressed && currentMap?.Portals != null)
                {
                    CheckPortalInteraction();
                }
            }
            
            // Update skill manager
            if (skillManager != null)
            {
                skillManager.Update(0.016f); // Use fixed timestep for skills
            }

            // Update monster AI (not physics)
            foreach (var monster in monsters.Where(m => !m.IsDead))
            {
                monster.Update(0.016f); // Use fixed timestep for AI
            }

            // Update combat system
            combat.Update(0.016f); // Use fixed timestep for combat

            // Update dropped items
            UpdateDroppedItems(0.016f); // Use fixed timestep

            // Check for item pickups
            if (networkClient != null)
            {
                CheckItemPickupsForNetwork();
            }
            else
            {
                CheckItemPickups();
            }

            // Remove dead monsters (in a real game, we'd handle this differently)
            var deadMonsters = monsters.Where(m => m.IsDead).ToList();
            foreach (var deadMonster in deadMonsters)
            {
                physicsManager.UnregisterPhysicsObject(deadMonster.PhysicsId);
                monsters.Remove(deadMonster);
            }
        }
        
        // Update physics at fixed timestep (called from FixedUpdate)
        public void UpdatePhysics(float fixedDeltaTime)
        {
            // Update physics using PhysicsUpdateManager for deterministic 60 FPS
            physicsManager.Update(fixedDeltaTime, currentMap);
        }
        
        // Combined update method for backwards compatibility
        public void Update(float deltaTime)
        {
            // If called with fixed timestep, just update physics
            if (System.Math.Abs(deltaTime - PhysicsUpdateManager.FIXED_TIMESTEP) < 0.0001f)
            {
                UpdatePhysics(deltaTime);
            }
            else if (deltaTime > 0)
            {
                // Otherwise process input
                ProcessInput();
            }
        }

        private void OnMapLoaded()
        {
            // Clear existing monsters and items
            monsters.Clear();
            droppedItems.Clear();
            
            // The FootholdService is now updated by NxMapLoader directly
            // Only update if footholds weren't already loaded (for backward compatibility)
            if (footholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).Count() == 0)
            {
                // Fallback: Convert platforms to footholds for the foothold service
                var footholds = new List<Foothold>();
                if (currentMap?.Platforms != null)
                {
                    foreach (var platform in currentMap.Platforms)
                    {
                        footholds.Add(new Foothold(
                            platform.Id,
                            platform.X1,
                            platform.Y1,
                            platform.X2,
                            platform.Y2
                        ));
                    }
                }
                footholdService.LoadFootholds(footholds);
            }

            // Spawn monsters from map data
            if (currentMap?.MonsterSpawns != null)
            {
                foreach (var spawn in currentMap.MonsterSpawns)
                {
                    SpawnMonster(spawn);
                }
            }

            // Use PlayerSpawnManager to find spawn point and position player
            if (currentMap != null)
            {
                System.Console.WriteLine($"[FOOTHOLD_COLLISION] OnMapLoaded: Player position before spawn: ({player.Position.X:F2}, {player.Position.Y:F2})");
                var spawnPoint = spawnManager.FindSpawnPoint(currentMap);
                System.Console.WriteLine($"[FOOTHOLD_COLLISION] OnMapLoaded: FindSpawnPoint returned: ({spawnPoint.X:F2}, {spawnPoint.Y:F2})");
                spawnManager.SpawnPlayer(player, spawnPoint);
                System.Console.WriteLine($"[FOOTHOLD_COLLISION] OnMapLoaded: Player position after spawn: ({player.Position.X:F2}, {player.Position.Y:F2})");
            }

            MapLoaded?.Invoke(currentMap);
        }

        private void SpawnMonster(MonsterSpawn spawn)
        {
            // For now, create a basic monster template
            // In a real implementation, this would load from NX data
            var template = new MonsterTemplate
            {
                MonsterId = spawn.MonsterId,
                Name = $"Monster {spawn.MonsterId}",
                MaxHP = 100,
                Level = 1,
                PhysicalDamage = 10,
                PhysicalDefense = 5,
                Speed = 30,
                DropTable = new List<DropInfo>
                {
                    new DropInfo { ItemId = 2000000, Quantity = 1, DropRate = 0.5f } // 50% chance for red potion
                }
            };

            var monster = new Monster(template, new Vector2(spawn.X, spawn.Y));
            monster.Died += OnMonsterDied;
            monster.ItemDropped += OnItemDropped;
            monsters.Add(monster);
            
            // Register monster with physics system
            physicsManager.RegisterPhysicsObject(monster);
            
            MonsterSpawned?.Invoke(monster);
        }

        private void OnMonsterDied(Monster monster)
        {
            // Unregister from physics system
            physicsManager.UnregisterPhysicsObject(monster.PhysicsId);
            
            MonsterDied?.Invoke(monster);
            // In a real game, we'd handle drops, respawn timer, etc.
        }

        private void OnItemDropped(int itemId, int quantity, Vector2 position)
        {
            var droppedItem = new DroppedItem(itemId, quantity, position);
            droppedItems.Add(droppedItem);
            ItemDropped?.Invoke(droppedItem);
        }

        private void UpdateDroppedItems(float deltaTime)
        {
            foreach (var item in droppedItems)
            {
                item.Update(deltaTime);
            }

            // Remove expired items
            droppedItems.RemoveAll(item => item.IsExpired);
        }

        private void CheckItemPickups()
        {
            if (player == null) return;

            var itemsToRemove = new List<DroppedItem>();

            foreach (var item in droppedItems)
            {
                var distance = Vector2.Distance(player.Position, item.Position);
                if (distance <= ItemPickupRange)
                {
                    player.Inventory.AddItem(item.ItemId, item.Quantity);
                    itemsToRemove.Add(item);
                    ItemPickedUp?.Invoke(item.ItemId, item.Quantity);
                }
            }

            foreach (var item in itemsToRemove)
            {
                droppedItems.Remove(item);
            }
        }

        // Test helpers
        public void SpawnMonsterForTesting(int monsterId, Vector2 position)
        {
            var spawn = new MonsterSpawn { MonsterId = monsterId, X = (int)position.X, Y = (int)position.Y };
            SpawnMonster(spawn);
        }

        public void AddDroppedItem(int itemId, int quantity, Vector2 position)
        {
            var droppedItem = new DroppedItem(itemId, quantity, position);
            droppedItems.Add(droppedItem);
            ItemDropped?.Invoke(droppedItem);
        }

        private void OnPlayerLanded()
        {
            // This event can be handled by the view layer
        }

        private void CheckPortalInteraction()
        {
            const float PortalInteractionRange = 50f;
            
            foreach (var portal in currentMap.Portals)
            {
                // Skip spawn and hidden portals
                if (portal.Type == PortalType.Spawn || portal.Type == PortalType.Hidden)
                    continue;
                
                var distance = Vector2.Distance(player.Position, new Vector2(portal.X, portal.Y));
                if (distance <= PortalInteractionRange)
                {
                    // Check if target map exists
                    var targetMap = mapLoader.GetMap(portal.TargetMapId);
                    if (targetMap != null)
                    {
                        LoadMap(portal.TargetMapId);
                        
                        // Position player at target portal or spawn
                        if (!string.IsNullOrEmpty(portal.TargetPortalName))
                        {
                            var targetPortal = currentMap.Portals.Find(p => p.Name == portal.TargetPortalName);
                            if (targetPortal != null)
                            {
                                var spawnPoint = new Vector2(targetPortal.X, targetPortal.Y);
                                spawnManager.SpawnPlayer(player, spawnPoint);
                            }
                        }
                        // else InitializePlayerAtSpawn was already called in OnMapLoaded
                    }
                    break;
                }
            }
        }

        // Network event handlers
        private void HandlePlayerJoin(int id, string name, int job, float x, float y)
        {
            // Don't add our own player again
            if (player != null && player.Id == id) return;
            
            var newPlayer = new Player
            {
                Id = id,
                Name = name,
                Position = new Vector2(x, y)
            };
            players.Add(newPlayer);
        }
        
        private void HandlePlayerMove(int id, float x, float y, byte[] movementData)
        {
            var targetPlayer = players.FirstOrDefault(p => p.Id == id);
            if (targetPlayer != null)
            {
                targetPlayer.Position = new Vector2(x, y);
            }
        }
        
        private void HandlePlayerLeave(int id)
        {
            players.RemoveAll(p => p.Id == id);
        }
        
        private void HandleMobSpawn(int id, int mobId, float x, float y)
        {
            var spawn = new MonsterSpawn { MonsterId = mobId, X = (int)x, Y = (int)y };
            var template = new MonsterTemplate
            {
                MonsterId = mobId,
                Name = $"Monster_{mobId}",
                MaxHP = 100,
                PhysicalDamage = 10,
                PhysicalDefense = 5,
                Speed = 50,
                DropTable = new List<DropInfo>()
            };

            var monster = new Monster(template, new Vector2(x, y));
            monster.Id = id;
            monster.Died += OnMonsterDied;
            monster.ItemDropped += OnItemDropped;
            monsters.Add(monster);
            
            // Register monster with physics system
            physicsManager.RegisterPhysicsObject(monster);
            
            MonsterSpawned?.Invoke(monster);
        }
        
        private void HandleMobDespawn(int id)
        {
            var monster = monsters.FirstOrDefault(m => m.Id == id);
            if (monster != null)
            {
                physicsManager.UnregisterPhysicsObject(monster.PhysicsId);
                monsters.Remove(monster);
            }
        }
        
        private void HandleMobMove(int id, float x, float y)
        {
            var monster = monsters.FirstOrDefault(m => m.Id == id);
            if (monster != null)
            {
                monster.Position = new Vector2(x, y);
            }
        }
        
        private void HandleItemDrop(int objectId, int itemId, float x, float y)
        {
            var droppedItem = new DroppedItem(itemId, 1, new Vector2(x, y));
            droppedItem.ObjectId = objectId;
            droppedItems.Add(droppedItem);
            ItemDropped?.Invoke(droppedItem);
        }
        
        private void HandleItemPickup(int objectId)
        {
            droppedItems.RemoveAll(item => item.ObjectId == objectId);
        }
        
        private void HandleChatMessage(ChatMessage message)
        {
            OnChatMessageReceived?.Invoke(message);
        }
        
        private void HandlePlayerHpMpUpdate(int hp, int mp)
        {
            if (player != null)
            {
                player.SetHPMP(hp, mp);
            }
        }
        
        private void HandleMapChange(int mapId, NetworkMapData mapData)
        {
            // Load the new map
            LoadMap(mapId);
            
            // Update player position from server data
            if (player != null && mapData != null)
            {
                player.Position = new Vector2(mapData.PlayerSpawnX, mapData.PlayerSpawnY);
            }
        }
        
        public void SendChatMessage(string message, ChatType type = ChatType.All)
        {
            if (networkClient != null)
            {
                networkClient.SendChat(message, type);
            }
        }
        
        public PhysicsDebugStats GetPhysicsDebugStats()
        {
            return physicsManager.GetDebugStats();
        }
        
        public float GetPhysicsInterpolationFactor()
        {
            return physicsManager.GetInterpolationFactor();
        }
        
        public void InitializePlayer(int id, string name, int hp, int mp, int maxHp, int maxMp, float x, float y)
        {
            player.Id = id;
            player.Name = name;
            player.MaxHP = maxHp;
            player.MaxMP = maxMp;
            player.SetHPMP(hp, mp);
            player.Position = new Vector2(x, y);
            
            System.Console.WriteLine($"[FOOTHOLD_COLLISION] InitializePlayer set position to ({x}, {y})");
            
            // Add to players list
            players.Clear();
            players.Add(player);
        }
        
        private void CheckItemPickupsForNetwork()
        {
            if (player == null || networkClient == null) return;

            foreach (var item in droppedItems)
            {
                var distance = Vector2.Distance(player.Position, item.Position);
                if (distance <= ItemPickupRange && item.ObjectId > 0)
                {
                    networkClient.SendPickupItem(item.ObjectId);
                }
            }
        }
    }
}