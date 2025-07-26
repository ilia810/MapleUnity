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

        public GameWorld(IInputProvider inputProvider, IMapLoader mapLoader, INetworkClient networkClient = null, IAssetProvider assetProvider = null)
        {
            this.mapLoader = mapLoader;
            this.inputProvider = inputProvider;
            this.networkClient = networkClient;
            this.assetProvider = assetProvider;
            this.player = new Player();
            this.players = new List<Player>();
            this.monsters = new List<Monster>();
            this.droppedItems = new List<DroppedItem>();
            this.combat = new Combat();
            
            // Initialize skill manager if asset provider is available
            if (assetProvider != null)
            {
                this.skillManager = new SkillManager(player, assetProvider, networkClient);
            }
            
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

        public void Update(float deltaTime)
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

            // Update physics
            if (player != null && currentMap != null)
            {
                player.UpdatePhysics(deltaTime, currentMap);
            }
            
            // Update skill manager
            if (skillManager != null)
            {
                skillManager.Update(deltaTime);
            }

            // Update monsters
            foreach (var monster in monsters.Where(m => !m.IsDead))
            {
                monster.Update(deltaTime);
            }

            // Update combat system
            combat.Update(deltaTime);

            // Update dropped items
            UpdateDroppedItems(deltaTime);

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
            monsters.RemoveAll(m => m.IsDead);
        }

        private void OnMapLoaded()
        {
            // Clear existing monsters and items
            monsters.Clear();
            droppedItems.Clear();

            // Spawn monsters from map data
            if (currentMap?.MonsterSpawns != null)
            {
                foreach (var spawn in currentMap.MonsterSpawns)
                {
                    SpawnMonster(spawn);
                }
            }

            // Position player at spawn portal
            var spawnPortal = currentMap?.Portals?.Find(p => p.Type == PortalType.Spawn);
            if (spawnPortal != null)
            {
                player.Position = new Vector2(spawnPortal.X, spawnPortal.Y);
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
            
            MonsterSpawned?.Invoke(monster);
        }

        private void OnMonsterDied(Monster monster)
        {
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
                            var targetPortal = targetMap.Portals.Find(p => p.Name == portal.TargetPortalName);
                            if (targetPortal != null)
                            {
                                player.Position = new Vector2(targetPortal.X, targetPortal.Y);
                            }
                        }
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
            
            MonsterSpawned?.Invoke(monster);
        }
        
        private void HandleMobDespawn(int id)
        {
            monsters.RemoveAll(m => m.Id == id);
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
        
        public void InitializePlayer(int id, string name, int hp, int mp, int maxHp, int maxMp, float x, float y)
        {
            player.Id = id;
            player.Name = name;
            player.MaxHP = maxHp;
            player.MaxMP = maxMp;
            player.SetHPMP(hp, mp);
            player.Position = new Vector2(x, y);
            
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