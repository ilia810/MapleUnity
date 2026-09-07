using System;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;

namespace MapleClient.GameLogic.Core
{
    public partial class GameWorld
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
        private readonly List<Monster> skillEffectMonsters = new List<Monster>();
        private const float ItemPickupRange = 0.5f; // 50 MapleStory pixels
        private readonly Dictionary<Monster, int> monsterPhysicsHandles = new Dictionary<Monster, int>();
        private bool upWasPressed;
        private bool portalRequested;
        private readonly Func<MapData> mapForTick;
        private double mapBottom = double.PositiveInfinity;
        private NormalTerrain monsterTerrain;
        private int nextMonsterId = 1;
        private bool attackHeld;
        private readonly Dictionary<Monster, MonsterSpawn> monsterOrigins = new Dictionary<Monster, MonsterSpawn>();
        private sealed class PendingMonster { public MonsterSpawn Spawn; public double Remaining; }
        private readonly List<PendingMonster> pendingMonsters = new List<PendingMonster>();
        private long lastDropTick = -1;
        private Vector2 lastDropOrigin;
        private int dropSpread;

        public MapData CurrentMap => currentMap;
        public int CurrentMapId => currentMap?.MapId ?? -1;
        public long MapSimulationTicks { get; private set; }
        public long MapRevision { get; private set; }
        public Player Player => player;
        public SkillManager SkillManager => skillManager;
        public bool CanRevivePlayer => networkClient == null && player.IsDead && currentMap != null;
        public IReadOnlyList<Player> Players => players; // All players including local
        public IReadOnlyList<Monster> Monsters => monsters;
        public IReadOnlyList<DroppedItem> DroppedItems => droppedItems;
        public IEnumerable<Data.SkillProjectile> Projectiles => combat.Projectiles;

        public event Action<MapData> MapLoaded;
        public event Action PlayerRecovered;
        public event Action PlayerTeleported;
        public event Action<Monster> MonsterSpawned;
        public event Action<Monster> MonsterDied;
        public event Action<Monster, Data.AttackHit> AttackResolved;
        public event Action<int, int> ItemPickedUp;
        public event Action<DroppedItem> ItemDropped;
        public event Action<ChatMessage> OnChatMessageReceived;

        public GameWorld(IInputProvider inputProvider, IMapLoader mapLoader, INetworkClient networkClient = null, IAssetProvider assetProvider = null, IFootholdService footholdService = null)
        {
            this.mapLoader = mapLoader;
            this.mapForTick = () => currentMap;
            this.inputProvider = inputProvider;
            this.networkClient = networkClient;
            this.assetProvider = assetProvider;
            this.footholdService = footholdService ?? new FootholdService();
            this.spawnManager = new PlayerSpawnManager(this.footholdService);
            this.physicsManager = new PhysicsUpdateManager();
            this.player = new Player(this.footholdService);
            this.player.LocalProgressionEnabled = networkClient == null;
            this.player.SetItemData(assetProvider?.ItemData);
            Quests = new OfflineQuestLog(player, networkClient == null ? assetProvider as Data.IQuestDataProvider : null);
            this.player.StanceAnimation.SetData(assetProvider?.CharacterData as Data.IStanceDataProvider);
            this.player.FaceAnimation.SetData((assetProvider?.CharacterData as Data.IFaceDataProvider)?.GetFaceAnimation(20000));
            this.players = new List<Player>();
            this.monsters = new List<Monster>();
            this.droppedItems = new List<DroppedItem>();
            this.combat = new Combat(consumeAmmunition: networkClient == null);
            this.combat.AttackResolved += (attacker, target, hit) => {
                if (target.SkillHitEffect != null && !skillEffectMonsters.Contains(target)) skillEffectMonsters.Add(target);
                if (ReferenceEquals(attacker, player)) AttackResolved?.Invoke(target, hit);
            };
            this.combat.MonsterDefeated += (attacker, monster) => {
                if (this.networkClient == null && ReferenceEquals(attacker, player))
                { Quests.CreditKill(monster.MonsterId); player.AddExperience(monster.Template.Exp); }
            };
            
            // Initialize skill manager if asset provider is available
            if (assetProvider != null)
            {
                this.skillManager = new SkillManager(player, assetProvider, networkClient);
                this.skillManager.StartAttackSkill = (skill, level) => combat.PerformSkillAttack(player, monsters, skill, level);
            }
            
            // Register player with physics system
            this.physicsManager.RegisterPhysicsObject(this.player);
            this.physicsManager.PhysicsStepCompleted += AdvanceSimulation;
            
            // Listen for player landed event
            this.player.Landed += OnPlayerLanded;
            this.player.Jumped += () => networkClient?.SendJump();
            
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

        private bool practiceSuppliesGranted;
        public bool HasClaimedPracticeSupplies => practiceSuppliesGranted;
        private bool practiceSkillsGranted;
        public bool CanRequestPracticeSkills => networkClient == null && !practiceSkillsGranted && !player.IsDead && !player.IsBasicAttacking && skillManager != null;
        public bool RequestPracticeSkills(out string message)
        {
            if (!CanRequestPracticeSkills) { message = "Fighter practice is available once per character, while idle."; return false; }
            var mastery = assetProvider.SkillData.GetSkill(1100000); var booster = assetProvider.SkillData.GetSkill(1101004);
            if (mastery?.IsSourceData != true || booster?.IsSourceData != true || !mastery.Levels.ContainsKey(20) ||
                !booster.Levels.ContainsKey(20) || booster.ActionDelays == null ||
                new[] {1001004,1001005}.Any(id => assetProvider.SkillData.GetSkill(id)?.Levels.ContainsKey(20) != true))
            { message = "Fighter skill data is unavailable."; return false; }
            player.JobId = 110;
            skillManager.SetSkillLevel(1100000, 20); skillManager.SetSkillLevel(1101004, 20);
            skillManager.SetSkillLevel(1001004, 20); skillManager.SetSkillLevel(1001005, 20);
            practiceSkillsGranted = true;
            message = "Fighter practice ready. Equip a sword to use its mastery bonus.";
            return true;
        }
        public bool CanRequestPracticeSupplies => networkClient == null && !practiceSuppliesGranted && !player.IsDead;
        private bool practiceMagicGranted;
        public bool CanRequestPracticeMagicSkills => networkClient == null && !practiceMagicGranted && !player.IsDead && !player.IsBasicAttacking && skillManager != null;
        public bool RequestPracticeMagicSkills(out string message)
        {
            if (!CanRequestPracticeMagicSkills) { message = "Magician practice is available once per character, while idle."; return false; }
            if (player.GetItemInfo(1372005) == null || player.GetItemInfo(2000003) == null ||
                new[] { 2001004, 2001005 }.Any(id => assetProvider.SkillData.GetSkill(id)?.Levels.ContainsKey(20) != true))
            { message = "Magician practice data is unavailable."; return false; }
            if (!player.Inventory.TryExchange(null, new Dictionary<int, int> { [1372005] = 1, [2000003] = 10 }))
            { message = "Make room for the wand and potions first."; return false; }
            // Explicit practice policy: the Wooden Wand requires level 8. Keep earned
            // EXP, attributes and equipment; put the wand in the bag for normal equipping.
            if (player.Level < 8) { long earned = player.Experience; player.Level = 8; player.AddExperience(earned); }
            player.JobId = 200;
            skillManager.SetSkillLevel(2001004, 20); skillManager.SetSkillLevel(2001005, 20);
            practiceMagicGranted = true;
            message = "Magician ready. Equip the Wooden Wand from your bag.";
            return true;
        }
        public bool RequestPracticeSupplies(out string message)
        {
            if (!CanRequestPracticeSupplies) { message = "Practice supplies are available once per character."; return false; }
            var supplies = new Dictionary<int, int> { [2000000] = 10, [2000001] = 5, [2000003] = 10, [2000004] = 3, [2002001] = 3, [2002004] = 3,
                [1040002] = 1, [1060002] = 1, [1072001] = 1, [1302000] = 1,
                [1402009] = 1, [1442079] = 1, [1092003] = 1 };
            if (supplies.Keys.Any(id => player.GetItemInfo(id) == null)) { message = "Practice item data is unavailable."; return false; }
            if (!player.Inventory.TryExchange(null, supplies)) { message = "Make room in your bag for the practice supplies."; return false; }
            practiceSuppliesGranted = true;
            message = "Practice supplies added. Select an item to use or equip it.";
            return true;
        }
        private readonly HashSet<int> weaponPracticeKits = new HashSet<int>();
        public bool CanRequestRangedPractice => networkClient == null && skillManager != null && !player.IsDead && !player.IsBasicAttacking && player.State != PlayerState.Climbing;
        public static int RangedPracticeWeapon(int type)
            => PracticeWeapon(type);
        public static int PracticeWeapon(int type)
        {
            switch (type) { case 133: return 1332005; case 145: return 1452002; case 146: return 1462001; case 147: return 1472000; case 149: return 1492000; default: return 0; }
        }
        public bool RequestRangedPractice(int weaponType, out string message)
            => RequestWeaponPractice(weaponType, out message);
        public bool RequestWeaponPractice(int weaponType, out string message)
        {
            int id = PracticeWeapon(weaponType), ammoId = Data.AmmunitionRules.Prefix(weaponType) * 1000;
            bool dagger = weaponType == 133; int rank = dagger ? 1 : 20;
            if (!CanRequestRangedPractice || id == 0) { message = "Choose a weapon setup while idle in local play."; return false; }
            var weapon = player.GetItemInfo(id); var ammo = dagger ? null : player.GetItemInfo(ammoId);
            var practiceSkills = SourceSkillRules.WeaponPracticeSkills(weaponType);
            if (weapon?.Weapon == null || !dagger && !(ammo?.Ammunition?.Frames.Length > 0) || player.GetItemInfo(2000003) == null ||
                practiceSkills.Any(skill => assetProvider?.SkillData?.GetSkill(skill)?.Levels.ContainsKey(rank) != true))
            { message = "Weapon practice data is unavailable."; return false; }
            var supplies = new Dictionary<int,int> { [id] = 1, [2000003] = 10 };
            if (!dagger) supplies[ammoId] = 100;
            if (!weaponPracticeKits.Contains(weaponType) && !player.Inventory.TryExchange(null,
                supplies))
            { message = "Make room for the practice supplies first."; return false; }
            // Re-selecting a setup changes the practice job without duplicating supplies.
            if (player.Level < weapon.RequiredLevel) { long exp = player.Experience; player.Level = weapon.RequiredLevel; player.AddExperience(exp); }
            player.JobId = dagger || weaponType == 147 ? 400 : weaponType <= 146 ? 300 : 500;
            foreach (int skill in practiceSkills) skillManager.SetSkillLevel(skill, Math.Max(rank, skillManager.GetSkillLevel(skill)));
            weaponPracticeKits.Add(weaponType);
            message = $"Equip {weapon.Name}. Cast using the assigned quickslots or skill book.";
            return true;
        }
        public bool UseInventoryItem(int id, out string message)
        {
            if (networkClient != null) { message = "Inventory actions are currently available in local play."; return false; }
            return player.GetItemInfo(id)?.Type == MapleClient.GameLogic.Interfaces.ItemType.Equip ?
                player.TryEquipItem(id, out message) : player.TryUseItem(id, out message);
        }
        public bool UseInventorySlot(int category, int slot, int expectedItemId, out string message)
        {
            if (!IsLocal) { message = "Inventory actions are currently available in local play."; return false; }
            if (category != expectedItemId / 1000000 || slot < 1 || slot > Inventory.SlotsPerCategory ||
                player.Inventory.GetStack(category, slot)?.ItemId != expectedItemId)
            { message = "This item is no longer in the selected bag slot."; return false; }
            return player.GetItemInfo(expectedItemId)?.Type == MapleClient.GameLogic.Interfaces.ItemType.Equip ?
                player.TryEquipItem(expectedItemId, out message, slot) : player.TryUseItem(expectedItemId, out message, slot);
        }
        public bool MoveInventoryStack(int category, int source, int destination, int expectedItemId, long revision, out string message)
        {
            if (!IsLocal || player.IsDead || player.IsBasicAttacking)
            { message = "Rearrange your bag while alive and between attacks in local play."; return false; }
            if (revision != player.Inventory.Revision)
            { message = "Your bag changed. Drag the item again."; return false; }
            if (!player.Inventory.TryMoveStack(category, source, destination, expectedItemId))
            { message = "That move is unavailable. Matching stacks need room to combine."; return false; }
            message = source == destination ? "" : "Moved item."; return true;
        }
        public bool RemoveEquipment(MapleClient.GameLogic.Data.EquipSlot slot, out string message)
        {
            if (networkClient != null) { message = "Inventory actions are currently available in local play."; return false; }
            return player.TryUnequipItem(slot, out message);
        }

        public void LoadMap(int mapId) => LoadMap(mapId, null);

        public void LoadMap(int mapId, string targetPortalName)
        {
            var mapData = mapLoader.GetMap(mapId);
            if (mapData != null)
            {
                currentMap = mapData;
                int portalId = mapData.Portals?.Find(p => p.Name == targetPortalName)?.Id ?? -1;
                OnMapLoaded(portalId);
            }
        }

        // Process input and game logic (called from Update)
        public void ProcessInput()
        {
            if (player.IsDead) { attackHeld = false; portalRequested = false; return; }
            // Handle input
            if (inputProvider != null && player != null)
            {
                portalRequested |= inputProvider.IsUpPressed && !upWasPressed;
                upWasPressed = inputProvider.IsUpPressed;
                if (player.IsBasicAttacking && !upWasPressed) portalRequested = false;
                
                // Movement - handle both inputs independently
                player.MoveLeft(inputProvider.IsLeftPressed);
                player.MoveRight(inputProvider.IsRightPressed);

                player.Crouch(inputProvider.IsDownPressed);
                player.ClimbUp(inputProvider.IsUpPressed);
                player.ClimbDown(inputProvider.IsDownPressed);

                // Jump
                if (inputProvider.IsJumpPressed)
                {
                    player.Jump();
                }
                else
                {
                    // Release jump key when not pressed
                    player.ReleaseJump();
                }

                attackHeld = inputProvider.IsAttackPressed;
                var expression = (inputProvider as IExpressionInputProvider)?.ExpressionPressed;
                if (expression.HasValue) player.RequestExpression(expression.Value);
            }
        }

        // Timers and AI advance once for every completed simulation step.
        private void AdvanceSimulation(long completedStep)
        {
            if (currentMap != null) MapSimulationTicks++;
            const float deltaTime = PhysicsUpdateManager.FIXED_TIMESTEP;
            RecoverPlayerIfNeeded();
            // Update skill manager
            if (skillManager != null)
            {
                skillManager.Update(deltaTime);
            }

            // Update monster AI (not physics)
            foreach (var monster in monsters.Where(m => !m.IsDead))
            {
                monster.Update(deltaTime);
            }
            foreach (var monster in skillEffectMonsters)
                if (monster.SkillHitEffect != null)
                {
                    monster.SkillHitEffect.Advance(deltaTime);
                    if (monster.SkillHitEffect.Sample(out _) == null) monster.SkillHitEffect = null;
                }
            skillEffectMonsters.RemoveAll(m => m.SkillHitEffect == null);

            // Update combat system
            combat.Update(deltaTime);
            // Stage resolves completed attacks and ladder entry before portal use.
            // An Up press during a swing waits only while the key remains held.
            if (portalRequested && !player.IsBasicAttacking)
            {
                portalRequested = false;
                if (player.State != PlayerState.Climbing && currentMap?.Portals != null)
                    CheckPortalInteraction();
            }
            if (attackHeld && player.State != PlayerState.Climbing && combat.CanPlayerAttack(player))
            {
                combat.PerformBasicAttack(player, monsters, 1f);
                networkClient?.SendAttack(0, new byte[0]);
            }
            if (networkClient == null) combat.CheckContact(player, monsters);
            for (int i = pendingMonsters.Count - 1; i >= 0; i--)
            {
                var pending = pendingMonsters[i];
                pending.Remaining -= deltaTime;
                if (pending.Remaining > 0) continue;
                pendingMonsters.RemoveAt(i);
                SpawnMonster(pending.Spawn);
            }

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
            var deadMonsters = monsters.Where(m => m.IsDead).ToList();
            foreach (var deadMonster in deadMonsters)
            {
                UnregisterMonster(deadMonster);
                monsters.Remove(deadMonster);
            }
        }
        
        // Advance the fixed simulation from elapsed host time.
        public void UpdatePhysics(float fixedDeltaTime)
        {
            var previousPosition = player.Position;
            physicsManager.Update(fixedDeltaTime, currentMap, mapForTick);
            if (networkClient != null && player.Position != previousPosition)
                networkClient.SendMove(player.Position.X, player.Position.Y, new byte[0]);
        }
        
        // Convenience update for hosts that do not have a separate fixed loop.
        public void Update(float deltaTime)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return;
            ProcessInput();
            UpdatePhysics(deltaTime);
        }
        private void OnMapLoaded(int portalId = -1, Vector2? serverPosition = null)
        {
            MapSimulationTicks = 0;
            lastDropTick = -1;
            MapRevision++;
            combat.Clear();
            foreach (var effectMonster in skillEffectMonsters) effectMonster.SkillHitEffect = null;
            skillEffectMonsters.Clear();
            foreach (var oldMonster in monsters)
            {
                UnregisterMonster(oldMonster);
                oldMonster.Died -= OnMonsterDied;
                oldMonster.ItemDropped -= OnItemDropped;
            }
            monsters.Clear();
            monsterOrigins.Clear();
            pendingMonsters.Clear();
            droppedItems.Clear();
            attackHeld = false;

            // MapData uses MapleStory pixels (positive Y down). Replace even empty maps.
            var footholds = new List<Foothold>();
            foreach (var platform in currentMap.Platforms)
            {
                if (platform.Type == PlatformType.Ladder || platform.Type == PlatformType.Rope)
                    continue;
                footholds.Add(new Foothold(platform.Id, platform.X1, platform.Y1, platform.X2, platform.Y2)
                {
                    IsWall = platform.X1 == platform.X2,
                    PreviousId = platform.PreviousId,
                    NextId = platform.NextId,
                    Layer = platform.Layer,
                    IsSlippery = platform.IsSlippery,
                    IsConveyor = platform.IsConveyor,
                    ConveyorSpeed = platform.ConveyorSpeed
                });
            }
            foreach (var foothold in footholds)
            {
                if (currentMap.Platforms.Any(p => p.Id == foothold.Id && p.HasSourceTopology)) continue;
                var next = footholds.Find(f => f.Id != foothold.Id && f.X1 == foothold.X2 && f.Y1 == foothold.Y2);
                var previous = footholds.Find(f => f.Id != foothold.Id && f.X2 == foothold.X1 && f.Y2 == foothold.Y1);
                foothold.NextId = next?.Id ?? 0;
                foothold.PreviousId = previous?.Id ?? 0;
            }
            footholdService.LoadFootholds(footholds);
            monsterTerrain = new NormalTerrain(footholds);
            mapBottom = monsterTerrain.Bottom;
            spawnManager.SpawnPlayer(player, serverPosition ?? spawnManager.FindSpawnPoint(currentMap, portalId));
            portalRequested = false;

            // Views clear the previous map before any new monster views spawn.
            MapLoaded?.Invoke(currentMap);
            if (networkClient == null)
                foreach (var spawn in currentMap.MonsterSpawns) SpawnMonster(spawn);
        }

        public bool RevivePlayer()
        {
            if (!CanRevivePlayer) return false;
            var destination = currentMap.ReturnMapId >= 0 && currentMap.ReturnMapId != 999999999 &&
                currentMap.ReturnMapId != currentMap.MapId ? mapLoader.GetMap(currentMap.ReturnMapId) : null;
            // Require actual safe terrain before committing to a return map.
            if (destination != null && new PlayerSpawnManager(null).TryFindSafeSpawn(destination, out _))
            {
                player.Revive();
                currentMap = destination;
                OnMapLoaded();
            }
            else
            {
                if (!spawnManager.TryFindSafeSpawn(currentMap, out var spawn)) return false;
                player.Revive();
                spawnManager.SpawnPlayer(player, spawn);
            }
            attackHeld = false; portalRequested = false;
            PlayerRecovered?.Invoke();
            return true;
        }

        private void RecoverPlayerIfNeeded()
        {
            if (currentMap == null || player.IsDead) return;
            bool invalid = float.IsNaN(player.Position.X) || float.IsNaN(player.Position.Y) ||
                float.IsInfinity(player.Position.X) || float.IsInfinity(player.Position.Y) ||
                float.IsNaN(player.Velocity.X) || float.IsNaN(player.Velocity.Y) ||
                float.IsInfinity(player.Velocity.X) || float.IsInfinity(player.Velocity.Y);
            if (!invalid && player.State == PlayerState.Climbing) return;
            double feetY = -(player.Position.Y - Player.Height / 2) * 100.0;
            if (!invalid && feetY + 0.001 < mapBottom) return;
            if (!spawnManager.TryFindSafeSpawn(currentMap, out var spawn)) return;
            spawnManager.SpawnPlayer(player, spawn);
            PlayerRecovered?.Invoke();
        }

        private void UnregisterMonster(Monster monster)
        {
            if (monsterPhysicsHandles.TryGetValue(monster, out int handle))
            {
                physicsManager.UnregisterPhysicsObject(handle);
                monsterPhysicsHandles.Remove(monster);
            }
        }
        private void SpawnMonster(MonsterSpawn spawn)
        {
            var data = assetProvider?.MobData?.GetMob(spawn.MonsterId);
            // Missing runtime source data cannot create a fabricated NX monster.
            if (assetProvider != null && data == null) return;
            var template = data == null ? new MonsterTemplate {
                MonsterId = spawn.MonsterId, Name = $"Monster {spawn.MonsterId}", MaxHP = 100,
                Level = 1, PhysicalDamage = 10, PhysicalDefense = 5, Speed = 0,
                DropTable = new List<DropInfo>()
            } : new MonsterTemplate {
                MonsterId = data.MobId, Name = data.Name, MaxHP = data.HP > 0 ? data.HP : 100,
                MaxMP = data.MP, Level = data.Level, Exp = data.Exp,
                PhysicalDamage = data.PADamage, MagicDamage = data.MADamage,
                PhysicalDefense = data.PDDamage, MagicDefense = data.MDDamage,
                Accuracy = data.Accuracy, Avoidability = data.Avoidability,
                Speed = data.Speed, CanMove = data.CanMove, CanFly = data.CanFly, NoFlip = data.NoFlip,
                KnockbackThreshold = data.KnockbackThreshold, BodyAttack = data.BodyAttack,
                ContactAnimations = data.ContactAnimations,
                // Explicit offline rules; Mob.nx does not contain server drop tables.
                DropTable = networkClient == null ? OfflineEconomy.Drops(data.MobId, data.Level, player.GetItemInfo) : new List<DropInfo>()
            };
            var monster = new Monster(template, MapleCoordinateConverter.MapleToUnity(spawn.X, spawn.Y)) { Id = nextMonsterId++ };
            if (monsterTerrain != null) monster.ConfigureTerrain(monsterTerrain, spawn.FacingRight);
            if (template.CanMove && !template.CanFly) monster.SetMovementPattern(MovementPattern.Patrol);
            if (networkClient == null) monsterOrigins[monster] = spawn;
            monster.Died += OnMonsterDied;
            monster.ItemDropped += OnItemDropped;
            monsters.Add(monster);
            
            // Register monster with physics system
            monsterPhysicsHandles[monster] = physicsManager.RegisterPhysicsObject(monster);
            
            MonsterSpawned?.Invoke(monster);
        }

        private void OnMonsterDied(Monster monster)
        {
            // Unregister from physics system
            UnregisterMonster(monster);
            
            MonsterDied?.Invoke(monster);
            if (monsterOrigins.TryGetValue(monster, out var spawn))
            {
                monsterOrigins.Remove(monster);
                if (spawn.SpawnInterval >= 0)
                    pendingMonsters.Add(new PendingMonster {
                        Spawn = spawn, Remaining = spawn.SpawnInterval > 0 ? spawn.SpawnInterval : 7
                    }); // Local play default; live spawn timing belongs to the server.
            }
            monster.Died -= OnMonsterDied;
            monster.ItemDropped -= OnItemDropped;
        }

        private void OnItemDropped(int itemId, int quantity, Vector2 position)
        {
            // Local destination policy: spread a monster's loot on nearby terrain so icons remain readable.
            if (lastDropTick != MapSimulationTicks || lastDropOrigin != position) dropSpread = 0;
            lastDropTick = MapSimulationTicks; lastDropOrigin = position;
            int index = dropSpread++;
            float x = position.X + (index == 0 ? 0 : ((index + 1) / 2) * (index % 2 == 1 ? .24f : -.24f));
            float ground = footholdService.GetGroundBelow(x * 100, -position.Y * 100 - 10);
            if (ground != float.MaxValue && Math.Abs(ground + position.Y * 100) < 30) position = new Vector2(x, -ground / 100);
            AddDroppedItem(itemId, quantity, position);
        }

        private void UpdateDroppedItems(float deltaTime)
        {
            foreach (var item in droppedItems)
            {
                item.Update(deltaTime);
            }

            // Remove expired items
            foreach (var expired in droppedItems.Where(item => item.IsExpired).ToArray())
            { droppedItems.Remove(expired); DropRemoved?.Invoke(expired); }
        }

        private void CheckItemPickups()
        {
            if (player == null || player.IsDead) return;

            var itemsToRemove = new List<DroppedItem>();

            foreach (var item in droppedItems)
            {
                var distance = Vector2.Distance(player.Position, item.Position);
                if (distance <= ItemPickupRange)
                {
                    if (!item.IsMeso && assetProvider != null && player.GetItemInfo(item.ItemId) == null) continue;
                    if (!(item.IsMeso ? player.TryGainMesos(item.Quantity) : player.Inventory.TryAddItem(item.ItemId, item.Quantity))) continue;
                    itemsToRemove.Add(item);
                    ItemPickedUp?.Invoke(item.ItemId, item.Quantity);
                }
            }

            foreach (var item in itemsToRemove)
            {
                droppedItems.Remove(item);
                DropRemoved?.Invoke(item);
            }
        }

        // Test helpers
        public void SpawnMonsterForTesting(int monsterId, Vector2 position)
        {
            var spawn = new MonsterSpawn { MonsterId = monsterId, X = (int)(position.X * 100f), Y = (int)(-position.Y * 100f) };
            SpawnMonster(spawn);
        }

        public void AddDroppedItem(int itemId, int quantity, Vector2 position)
        {
            if (itemId < 0 || quantity <= 0) return;
            var droppedItem = new DroppedItem(itemId, quantity, position);
            droppedItem.FootholdLayer = footholdService.FindNearestFoothold(position.X * 100, -position.Y * 100, 100)?.Layer ?? 0;
            droppedItems.Add(droppedItem);
            ItemDropped?.Invoke(droppedItem);
        }

        private void OnPlayerLanded()
        {
            // This event can be handled by the view layer
        }

        private void CheckPortalInteraction()
        {
            const float PortalInteractionRange = 0.5f;
            
            foreach (var portal in currentMap.Portals)
            {
                // Scripted portals need a server-side script handler.
                if (portal.Type == PortalType.Spawn || portal.Type == PortalType.Script || portal.TargetMapId >= 999999999)
                    continue;
                
                var distance = Vector2.Distance(player.Position, MapleCoordinateConverter.MapleToUnity(portal.X, portal.Y));
                if (distance <= PortalInteractionRange)
                {
                    if (portal.TargetMapId == currentMap.MapId)
                    {
                        var target = currentMap.Portals.Find(p => p.Name == portal.TargetPortalName);
                        if (target != null)
                        {
                            // Stage::check_portals only respawns the player for an
                            // intramap warp. Preserve the map's mobs and drops.
                            spawnManager.SpawnPlayer(player, spawnManager.FindSpawnPoint(currentMap, target.Id));
                            PlayerTeleported?.Invoke();
                        }
                    }
                    else
                        LoadMap(portal.TargetMapId, portal.TargetPortalName);
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
            monsterPhysicsHandles[monster] = physicsManager.RegisterPhysicsObject(monster);
            
            MonsterSpawned?.Invoke(monster);
        }
        
        private void HandleMobDespawn(int id)
        {
            var monster = monsters.FirstOrDefault(m => m.Id == id);
            if (monster != null)
            {
                UnregisterMonster(monster);
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
            var destination = mapLoader.GetMap(mapId);
            if (destination == null) return;
            currentMap = destination;
            Vector2? position = mapData != null ? new Vector2(mapData.PlayerSpawnX, mapData.PlayerSpawnY) : (Vector2?)null;
            OnMapLoaded(serverPosition: position);
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
