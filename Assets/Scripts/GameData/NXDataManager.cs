using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Data;
using MapleClient.GameData;
using System.Linq;

namespace GameData
{
    /// <summary>
    /// Comprehensive NX data manager that provides all game assets
    /// </summary>
    public class NXDataManager : IAssetProvider
    {
        private readonly string dataPath;
        private readonly Dictionary<string, INxFile> loadedFiles;
        private readonly AssetCache cache;
        
        // Provider implementations
        private ItemDataProvider itemProvider;
        private MobDataProvider mobProvider;
        private SkillDataProvider skillProvider;
        private NpcDataProvider npcProvider;
        private MapDataProvider mapProvider;
        private CharacterDataProvider characterProvider;
        private SoundDataProvider soundProvider;
        
        public IItemDataProvider ItemData => itemProvider;
        public IMobDataProvider MobData => mobProvider;
        public ISkillDataProvider SkillData => skillProvider;
        public INpcDataProvider NpcData => npcProvider;
        public IMapDataProvider MapData => mapProvider;
        public ICharacterDataProvider CharacterData => characterProvider;
        public ISoundDataProvider SoundData => soundProvider;
        
        public NXDataManager(string dataPath = null)
        {
            // Use actual MapleStory NX files from HeavenClient
            this.dataPath = dataPath ?? @"C:\HeavenClient\MapleStory-Client\nx";
            this.loadedFiles = new Dictionary<string, INxFile>();
            this.cache = new AssetCache();
        }
        
        public void Initialize()
        {
            Debug.Log($"Initializing NXDataManager with path: {dataPath}");
            
            // Load NX files
            LoadNXFile("Character.nx");
            LoadNXFile("Map.nx");
            LoadNXFile("String.nx");
            LoadNXFile("Npc.nx");
            // TODO: Fix paths for these files before enabling
            // LoadNXFile("Item.nx");
            // LoadNXFile("Mob.nx");
            // LoadNXFile("Skill.nx");
            // LoadNXFile("Quest.nx");
            // LoadNXFile("Reactor.nx");
            // LoadNXFile("Sound.nx");
            // LoadNXFile("UI.nx");
            // LoadNXFile("Effect.nx");
            
            // Initialize providers
            itemProvider = new ItemDataProvider(this);
            mobProvider = new MobDataProvider(this);
            skillProvider = new SkillDataProvider(this);
            npcProvider = new NpcDataProvider(this);
            mapProvider = new MapDataProvider(this);
            characterProvider = new MapleClient.GameData.CharacterDataProvider();
            soundProvider = new SoundDataProvider(this);
            
            // Load essential data
            // TODO: Fix item/mob/skill loading paths
            // itemProvider.LoadAllItems();
            // mobProvider.LoadAllMobs();
            // skillProvider.LoadAllSkills();
            
            Debug.Log("NXDataManager initialized successfully");
        }
        
        public void Shutdown()
        {
            cache.Clear();
            loadedFiles.Clear();
        }
        
        private void LoadNXFile(string fileName)
        {
            string filePath = Path.Combine(dataPath, fileName);
            
            try
            {
                INxFile nxFile;
                
                if (File.Exists(filePath))
                {
                    // Use the real NX file implementation
                    nxFile = new RealNxFile(filePath);
                }
                else
                {
                    // Use mock data if file doesn't exist
                    Debug.LogWarning($"NX file not found: {filePath}, using mock data");
                    nxFile = new MockNxFile(fileName);
                }
                
                string fileKey = Path.GetFileNameWithoutExtension(fileName).ToLower();
                loadedFiles[fileKey] = nxFile;
                
                // Register with asset loader
                NXAssetLoader.Instance.RegisterNxFile(fileKey, nxFile);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load NX file {fileName}: {e.Message}");
                // Use mock data as fallback
                loadedFiles[Path.GetFileNameWithoutExtension(fileName).ToLower()] = new MockNxFile(fileName);
            }
        }
        
        public INxFile GetFile(string name)
        {
            loadedFiles.TryGetValue(name.ToLower(), out var file);
            return file;
        }
        
        public INxNode GetNode(string file, string path)
        {
            var nxFile = GetFile(file);
            return nxFile?.GetNode(path);
        }
        
        // Provider implementations
        private class ItemDataProvider : IItemDataProvider
        {
            private readonly NXDataManager manager;
            private readonly Dictionary<int, ItemInfo> items;
            
            public ItemDataProvider(NXDataManager manager)
            {
                this.manager = manager;
                this.items = new Dictionary<int, ItemInfo>();
            }
            
            public void LoadAllItems()
            {
                // Load items from String.nx for names/descriptions
                var stringFile = manager.GetFile("string");
                var itemFile = manager.GetFile("item");
                
                if (itemFile == null) return;
                
                // Load equipment
                LoadItemCategory(itemFile, stringFile, "Equip", ItemType.Equip);
                LoadItemCategory(itemFile, stringFile, "Consume", ItemType.Use);
                LoadItemCategory(itemFile, stringFile, "Install", ItemType.Setup);
                LoadItemCategory(itemFile, stringFile, "Etc", ItemType.Etc);
                LoadItemCategory(itemFile, stringFile, "Cash", ItemType.Cash);
                
                Debug.Log($"Loaded {items.Count} items");
            }
            
            private void LoadItemCategory(INxFile itemFile, INxFile stringFile, string category, ItemType type)
            {
                var categoryNode = itemFile.GetNode(category);
                if (categoryNode == null) return;
                
                foreach (var subCategory in categoryNode.Children)
                {
                    foreach (var itemNode in subCategory.Children)
                    {
                        if (int.TryParse(itemNode.Name.Replace(".img", ""), out int itemId))
                        {
                            var item = ParseItem(itemNode, itemId, type);
                            if (item != null)
                            {
                                // Get name from String.nx
                                var stringNode = stringFile?.GetNode($"Item.img/{itemId}");
                                if (stringNode != null)
                                {
                                    item.Name = stringNode["name"]?.GetValue<string>() ?? $"Item {itemId}";
                                    item.Description = stringNode["desc"]?.GetValue<string>() ?? "";
                                }
                                
                                items[itemId] = item;
                            }
                        }
                    }
                }
            }
            
            private ItemInfo ParseItem(INxNode node, int itemId, ItemType type)
            {
                var info = node["info"];
                if (info == null) return null;
                
                var item = new ItemInfo
                {
                    ItemId = itemId,
                    Type = type,
                    Price = info["price"]?.GetValue<int>() ?? 0,
                    MaxStack = info["slotMax"]?.GetValue<int>() ?? 1,
                    IsCash = info["cash"]?.GetValue<bool>() ?? false,
                    IsQuest = info["quest"]?.GetValue<bool>() ?? false,
                    IsTradeable = info["tradeBlock"]?.GetValue<bool>() ?? true == false,
                    IsOneOfAKind = info["only"]?.GetValue<bool>() ?? false,
                    Stats = new Dictionary<StatType, int>()
                };
                
                // Parse equipment stats
                if (type == ItemType.Equip)
                {
                    ParseEquipStats(info, item);
                }
                // Parse consumable effects
                else if (type == ItemType.Use)
                {
                    ParseConsumeEffects(info, item);
                }
                
                return item;
            }
            
            private void ParseEquipStats(INxNode info, ItemInfo item)
            {
                item.RequiredLevel = info["reqLevel"]?.GetValue<int>() ?? 0;
                item.RequiredStr = info["reqSTR"]?.GetValue<int>() ?? 0;
                item.RequiredDex = info["reqDEX"]?.GetValue<int>() ?? 0;
                item.RequiredInt = info["reqINT"]?.GetValue<int>() ?? 0;
                item.RequiredLuk = info["reqLUK"]?.GetValue<int>() ?? 0;
                item.Slots = info["tuc"]?.GetValue<int>() ?? 0;
                
                // Stats
                AddStat(item, StatType.STR, info["incSTR"]?.GetValue<int>());
                AddStat(item, StatType.DEX, info["incDEX"]?.GetValue<int>());
                AddStat(item, StatType.INT, info["incINT"]?.GetValue<int>());
                AddStat(item, StatType.LUK, info["incLUK"]?.GetValue<int>());
                AddStat(item, StatType.MaxHP, info["incMHP"]?.GetValue<int>());
                AddStat(item, StatType.MaxMP, info["incMMP"]?.GetValue<int>());
                AddStat(item, StatType.WeaponAttack, info["incPAD"]?.GetValue<int>());
                AddStat(item, StatType.MagicAttack, info["incMAD"]?.GetValue<int>());
                AddStat(item, StatType.WeaponDefense, info["incPDD"]?.GetValue<int>());
                AddStat(item, StatType.MagicDefense, info["incMDD"]?.GetValue<int>());
                AddStat(item, StatType.Accuracy, info["incACC"]?.GetValue<int>());
                AddStat(item, StatType.Avoidability, info["incEVA"]?.GetValue<int>());
                AddStat(item, StatType.Speed, info["incSpeed"]?.GetValue<int>());
                AddStat(item, StatType.Jump, info["incJump"]?.GetValue<int>());
            }
            
            private void ParseConsumeEffects(INxNode info, ItemInfo item)
            {
                item.Hp = info["hp"]?.GetValue<int>() ?? 0;
                item.Mp = info["mp"]?.GetValue<int>() ?? 0;
                item.HpRate = info["hpR"]?.GetValue<int>() ?? 0;
                item.MpRate = info["mpR"]?.GetValue<int>() ?? 0;
                item.Time = info["time"]?.GetValue<int>() ?? 0;
                item.Buffs = new Dictionary<BuffType, int>();
                
                // Parse buff effects
                AddBuff(item, BuffType.WeaponAttack, info["pad"]?.GetValue<int>());
                AddBuff(item, BuffType.MagicAttack, info["mad"]?.GetValue<int>());
                AddBuff(item, BuffType.WeaponDefense, info["pdd"]?.GetValue<int>());
                AddBuff(item, BuffType.MagicDefense, info["mdd"]?.GetValue<int>());
                AddBuff(item, BuffType.Accuracy, info["acc"]?.GetValue<int>());
                AddBuff(item, BuffType.Avoidability, info["eva"]?.GetValue<int>());
                AddBuff(item, BuffType.Speed, info["speed"]?.GetValue<int>());
                AddBuff(item, BuffType.Jump, info["jump"]?.GetValue<int>());
            }
            
            private void AddStat(ItemInfo item, StatType stat, int? value)
            {
                if (value.HasValue && value.Value != 0)
                    item.Stats[stat] = value.Value;
            }
            
            private void AddBuff(ItemInfo item, BuffType buff, int? value)
            {
                if (value.HasValue && value.Value != 0)
                    item.Buffs[buff] = value.Value;
            }
            
            public ItemInfo GetItem(int itemId)
            {
                items.TryGetValue(itemId, out var item);
                return item ?? CreateMockItem(itemId);
            }
            
            public Dictionary<int, ItemInfo> GetAllItems()
            {
                return new Dictionary<int, ItemInfo>(items);
            }
            
            public bool ItemExists(int itemId)
            {
                return items.ContainsKey(itemId);
            }
            
            private ItemInfo CreateMockItem(int itemId)
            {
                // Create mock item for testing
                return new ItemInfo
                {
                    ItemId = itemId,
                    Name = $"Item {itemId}",
                    Description = "Unknown item",
                    Type = ItemType.Etc,
                    Price = 100,
                    MaxStack = 100,
                    Stats = new Dictionary<StatType, int>(),
                    Buffs = new Dictionary<BuffType, int>()
                };
            }
        }
        
        private class MobDataProvider : IMobDataProvider
        {
            private readonly NXDataManager manager;
            private readonly Dictionary<int, MobInfo> mobs;
            
            public MobDataProvider(NXDataManager manager)
            {
                this.manager = manager;
                this.mobs = new Dictionary<int, MobInfo>();
            }
            
            public void LoadAllMobs()
            {
                var mobFile = manager.GetFile("mob");
                var stringFile = manager.GetFile("string");
                
                if (mobFile == null) return;
                
                foreach (var mobNode in mobFile.Root.Children)
                {
                    if (int.TryParse(mobNode.Name.Replace(".img", ""), out int mobId))
                    {
                        var mob = ParseMob(mobNode, mobId);
                        if (mob != null)
                        {
                            // Get name from String.nx
                            var stringNode = stringFile?.GetNode($"Mob.img/{mobId}");
                            if (stringNode != null)
                            {
                                mob.Name = stringNode["name"]?.GetValue<string>() ?? $"Monster {mobId}";
                            }
                            
                            mobs[mobId] = mob;
                        }
                    }
                }
                
                Debug.Log($"Loaded {mobs.Count} monsters");
            }
            
            private MobInfo ParseMob(INxNode node, int mobId)
            {
                var info = node["info"];
                if (info == null) return null;
                
                return new MobInfo
                {
                    MobId = mobId,
                    Level = info["level"]?.GetValue<int>() ?? 1,
                    HP = info["maxHP"]?.GetValue<int>() ?? 100,
                    MP = info["maxMP"]?.GetValue<int>() ?? 0,
                    Exp = info["exp"]?.GetValue<int>() ?? 0,
                    PADamage = info["PADamage"]?.GetValue<int>() ?? 10,
                    PDDamage = info["PDDamage"]?.GetValue<int>() ?? 10,
                    MADamage = info["MADamage"]?.GetValue<int>() ?? 10,
                    MDDamage = info["MDDamage"]?.GetValue<int>() ?? 10,
                    Accuracy = info["acc"]?.GetValue<int>() ?? 100,
                    Avoidability = info["eva"]?.GetValue<int>() ?? 0,
                    Speed = info["speed"]?.GetValue<int>() ?? 0,
                    IsBoss = info["boss"]?.GetValue<bool>() ?? false,
                    IsUndead = info["undead"]?.GetValue<bool>() ?? false,
                    CanFly = info["flySpeed"]?.GetValue<int>() > 0,
                    Skills = new List<int>(),
                    Drops = new Dictionary<int, DropInfo>()
                };
            }
            
            public MobInfo GetMob(int mobId)
            {
                mobs.TryGetValue(mobId, out var mob);
                return mob ?? CreateMockMob(mobId);
            }
            
            public Dictionary<int, MobInfo> GetAllMobs()
            {
                return new Dictionary<int, MobInfo>(mobs);
            }
            
            public bool MobExists(int mobId)
            {
                return mobs.ContainsKey(mobId);
            }
            
            private MobInfo CreateMockMob(int mobId)
            {
                // Create mock mob for testing
                return new MobInfo
                {
                    MobId = mobId,
                    Name = $"Monster {mobId}",
                    Level = 10,
                    HP = 1000,
                    Exp = 100,
                    PADamage = 50,
                    Skills = new List<int>(),
                    Drops = new Dictionary<int, DropInfo>()
                };
            }
        }
        
        // Complete skill data provider implementation
        private class SkillDataProvider : ISkillDataProvider
        {
            private readonly NXDataManager manager;
            private readonly Dictionary<int, SkillInfo> skills;
            private readonly Dictionary<int, List<int>> skillsByJob;
            
            public SkillDataProvider(NXDataManager manager)
            {
                this.manager = manager;
                this.skills = new Dictionary<int, SkillInfo>();
                this.skillsByJob = new Dictionary<int, List<int>>();
            }
            
            public void LoadAllSkills()
            {
                var skillFile = manager.GetFile("skill");
                var stringFile = manager.GetFile("string");
                
                if (skillFile == null) return;
                
                foreach (var jobNode in skillFile.Root.Children)
                {
                    if (!jobNode.Name.EndsWith(".img")) continue;
                    
                    string jobIdStr = jobNode.Name.Replace(".img", "");
                    if (!int.TryParse(jobIdStr, out int jobId)) continue;
                    
                    var skillList = new List<int>();
                    
                    // Parse skills in the skill subfolder
                    var skillFolder = jobNode["skill"];
                    if (skillFolder != null)
                    {
                        foreach (var skillNode in skillFolder.Children)
                        {
                            if (int.TryParse(skillNode.Name, out int skillId))
                            {
                                var skill = ParseSkill(skillNode, skillId, jobId);
                                if (skill != null)
                                {
                                    // Get name from String.nx
                                    var stringNode = stringFile?.GetNode($"Skill.img/{skillId}");
                                    if (stringNode != null)
                                    {
                                        skill.Name = stringNode["name"]?.GetValue<string>() ?? $"Skill {skillId}";
                                        skill.Description = stringNode["desc"]?.GetValue<string>() ?? "";
                                        skill.H1 = stringNode["h1"]?.GetValue<string>() ?? "";
                                    }
                                    
                                    skills[skillId] = skill;
                                    skillList.Add(skillId);
                                }
                            }
                        }
                    }
                    
                    if (skillList.Count > 0)
                        skillsByJob[jobId] = skillList;
                }
                
                Debug.Log($"Loaded {skills.Count} skills across {skillsByJob.Count} jobs");
            }
            
            private SkillInfo ParseSkill(INxNode node, int skillId, int jobId)
            {
                var skill = new SkillInfo
                {
                    SkillId = skillId,
                    JobId = jobId,
                    MaxLevel = node["maxLevel"]?.GetValue<int>() ?? 20,
                    IsPassive = false, // Will be determined by skill type
                    Type = SkillType.Attack, // Default, will be overridden
                    Element = ElementType.Physical,
                    Levels = new Dictionary<int, SkillInfo.LevelData>()
                };
                
                // Parse common properties
                var common = node["common"];
                if (common != null)
                {
                    skill.AttackCount = common["attackCount"]?.GetValue<int>() ?? 1;
                    skill.MobCount = common["mobCount"]?.GetValue<int>() ?? 1;
                    skill.BulletCount = common["bulletCount"]?.GetValue<int>() ?? 0;
                    skill.BulletConsume = common["bulletConsume"]?.GetValue<int>() ?? 0;
                    skill.ItemCon = common["itemCon"]?.GetValue<int>() ?? 0;
                    skill.ItemConNo = common["itemConNo"]?.GetValue<int>() ?? 0;
                }
                
                // Determine skill type
                DetermineSkillType(node, skill);
                
                // Parse level data
                ParseLevelData(node, skill);
                
                // Parse element
                var elemAttr = node["elemAttr"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(elemAttr))
                {
                    skill.Element = ParseElement(elemAttr);
                }
                
                return skill;
            }
            
            private void DetermineSkillType(INxNode node, SkillInfo skill)
            {
                // Check for passive skills
                if (node["psd"] != null)
                {
                    skill.IsPassive = true;
                    skill.Type = SkillType.Passive;
                    return;
                }
                
                // Check for summon skills
                if (node["summon"] != null)
                {
                    skill.Type = SkillType.Summon;
                    return;
                }
                
                // Check for buff skills
                var level1 = node["level"]?["1"];
                if (level1 != null)
                {
                    if (level1["time"] != null && level1["damage"] == null)
                    {
                        skill.Type = SkillType.Buff;
                        return;
                    }
                    
                    if (level1["hp"] != null || level1["hpR"] != null)
                    {
                        skill.Type = SkillType.Recovery;
                        return;
                    }
                }
                
                // Default to attack
                skill.Type = SkillType.Attack;
            }
            
            private void ParseLevelData(INxNode node, SkillInfo skill)
            {
                var levelNode = node["level"];
                if (levelNode == null) return;
                
                foreach (var level in levelNode.Children)
                {
                    if (int.TryParse(level.Name, out int levelNum))
                    {
                        var levelData = new SkillInfo.LevelData
                        {
                            MpCost = level["mpCon"]?.GetValue<int>() ?? 0,
                            Damage = level["damage"]?.GetValue<int>() ?? 0,
                            AttackCount = level["attackCount"]?.GetValue<int>() ?? skill.AttackCount,
                            MobCount = level["mobCount"]?.GetValue<int>() ?? skill.MobCount,
                            Range = level["range"]?.GetValue<int>() ?? 0,
                            Duration = level["time"]?.GetValue<int>() ?? 0,
                            Cooldown = level["cooltime"]?.GetValue<int>() ?? 0,
                            Mastery = level["mastery"]?.GetValue<int>() ?? 0,
                            Critical = level["critical"]?.GetValue<int>() ?? 0,
                            Buffs = new Dictionary<BuffType, int>()
                        };
                        
                        // Parse buff effects
                        ParseBuffEffects(level, levelData);
                        
                        // Special properties
                        levelData.Hp = level["hp"]?.GetValue<int>() ?? 0;
                        levelData.HpR = level["hpR"]?.GetValue<int>() ?? 0;
                        levelData.Mp = level["mp"]?.GetValue<int>() ?? 0;
                        levelData.MpR = level["mpR"]?.GetValue<int>() ?? 0;
                        levelData.Prop = level["prop"]?.GetValue<int>() ?? 100; // Success rate
                        levelData.X = level["x"]?.GetValue<int>() ?? 0; // Various uses
                        levelData.Y = level["y"]?.GetValue<int>() ?? 0;
                        levelData.Z = level["z"]?.GetValue<int>() ?? 0;
                        
                        skill.Levels[levelNum] = levelData;
                    }
                }
            }
            
            private void ParseBuffEffects(INxNode level, SkillInfo.LevelData levelData)
            {
                // Physical stats
                AddBuffIfExists(level, levelData, "pad", BuffType.WeaponAttack);
                AddBuffIfExists(level, levelData, "mad", BuffType.MagicAttack);
                AddBuffIfExists(level, levelData, "pdd", BuffType.WeaponDefense);
                AddBuffIfExists(level, levelData, "mdd", BuffType.MagicDefense);
                AddBuffIfExists(level, levelData, "acc", BuffType.Accuracy);
                AddBuffIfExists(level, levelData, "eva", BuffType.Avoidability);
                AddBuffIfExists(level, levelData, "speed", BuffType.Speed);
                AddBuffIfExists(level, levelData, "jump", BuffType.Jump);
                
                // Special buffs
                if (level["powerGuard"] != null)
                    levelData.Buffs[BuffType.PowerGuard] = level["powerGuard"].GetValue<int>();
                if (level["hyperBody"] != null)
                    levelData.Buffs[BuffType.HyperBody] = 1;
                if (level["mesoUp"] != null)
                    levelData.Buffs[BuffType.MesoUp] = level["mesoUp"].GetValue<int>();
                if (level["dropUp"] != null)
                    levelData.Buffs[BuffType.DropUp] = level["dropUp"].GetValue<int>();
            }
            
            private void AddBuffIfExists(INxNode level, SkillInfo.LevelData levelData, string key, BuffType buffType)
            {
                var value = level[key]?.GetValue<int>();
                if (value.HasValue && value.Value != 0)
                    levelData.Buffs[buffType] = value.Value;
            }
            
            private ElementType ParseElement(string elemAttr)
            {
                switch (elemAttr.ToLower())
                {
                    case "i": return ElementType.Ice;
                    case "f": return ElementType.Fire;
                    case "l": return ElementType.Lightning;
                    case "s": return ElementType.Poison;
                    case "h": return ElementType.Holy;
                    case "d": return ElementType.Dark;
                    case "p": return ElementType.Physical;
                    default: return ElementType.Neutral;
                }
            }
            
            public SkillInfo GetSkill(int skillId)
            {
                skills.TryGetValue(skillId, out var skill);
                return skill ?? CreateMockSkill(skillId);
            }
            
            public Dictionary<int, SkillInfo> GetSkillsForJob(int jobId)
            {
                var result = new Dictionary<int, SkillInfo>();
                
                if (skillsByJob.TryGetValue(jobId, out var skillIds))
                {
                    foreach (var skillId in skillIds)
                    {
                        if (skills.TryGetValue(skillId, out var skill))
                            result[skillId] = skill;
                    }
                }
                
                return result;
            }
            
            public bool SkillExists(int skillId)
            {
                return skills.ContainsKey(skillId);
            }
            
            private SkillInfo CreateMockSkill(int skillId)
            {
                // Create mock skill for testing
                var skill = new SkillInfo
                {
                    SkillId = skillId,
                    Name = $"Skill {skillId}",
                    Description = "Unknown skill",
                    MaxLevel = 10,
                    Type = SkillType.Attack,
                    Element = ElementType.Physical,
                    Levels = new Dictionary<int, SkillInfo.LevelData>()
                };
                
                // Add mock level data
                for (int i = 1; i <= skill.MaxLevel; i++)
                {
                    skill.Levels[i] = new SkillInfo.LevelData
                    {
                        MpCost = 10 + i * 2,
                        Damage = 100 + i * 20,
                        AttackCount = 1,
                        MobCount = 1,
                        Buffs = new Dictionary<BuffType, int>()
                    };
                }
                
                return skill;
            }
        }
        
        private class NpcDataProvider : INpcDataProvider
        {
            private readonly NXDataManager manager;
            
            public NpcDataProvider(NXDataManager manager)
            {
                this.manager = manager;
            }
            
            public NpcInfo GetNpc(int npcId) => null;
            public ShopInfo GetShop(int npcId) => null;
            public string[] GetNpcScript(int npcId) => null;
            public bool NpcExists(int npcId) => false;
        }
        
        private class MapDataProvider : IMapDataProvider
        {
            private readonly NXDataManager manager;
            
            public MapDataProvider(NXDataManager manager)
            {
                this.manager = manager;
            }
            
            public MapInfo GetMap(int mapId) => null;
            
            public IMapInfo GetMapInfo(int mapId)
            {
                // Convert map ID to file path format (e.g., 100000000 -> Map/Map1/100000000.img)
                string mapCategory = $"Map{mapId / 100000000}";
                string mapPath = $"Map/{mapCategory}/{mapId:D9}.img";
                
                var mapFile = manager.GetFile("map");
                if (mapFile == null)
                {
                    UnityEngine.Debug.LogError("Map NX file not loaded");
                    return null;
                }
                
                var mapNode = mapFile.GetNode(mapPath);
                if (mapNode == null)
                {
                    UnityEngine.Debug.LogError($"Map node not found: {mapPath}");
                    return null;
                }
                
                UnityEngine.Debug.Log($"Found map node for {mapId}");
                return new MapInfoImpl(mapId, mapNode);
            }
            
            public string GetMapName(int mapId)
            {
                var stringFile = manager.GetFile("string");
                if (stringFile == null) return $"Map {mapId}";
                
                var mapNameNode = stringFile.GetNode($"Map.img/maple/{mapId}/mapName");
                return mapNameNode?.GetValue<string>() ?? $"Map {mapId}";
            }
            
            public byte[] GetMapBackground(int mapId) => null;
            
            public string GetMapMusic(int mapId)
            {
                var mapInfo = GetMapInfo(mapId);
                if (mapInfo == null) return null;
                
                var infoNode = mapInfo.GetNode("info/bgm") as INxNode;
                return infoNode?.GetValue<string>();
            }
            
            public bool MapExists(int mapId)
            {
                string mapCategory = $"Map{mapId / 100000000}";
                string mapPath = $"Map/{mapCategory}/{mapId:D9}.img";
                
                var mapFile = manager.GetFile("map");
                if (mapFile == null) return false;
                
                return mapFile.GetNode(mapPath) != null;
            }
        }
        
        // CharacterDataProvider is now in its own file
        
        private class SoundDataProvider : ISoundDataProvider
        {
            private readonly NXDataManager manager;
            
            public SoundDataProvider(NXDataManager manager)
            {
                this.manager = manager;
            }
            
            public byte[] GetBackgroundMusic(string name) => null;
            public byte[] GetSoundEffect(string name) => null;
            public Dictionary<string, byte[]> GetSkillSounds(int skillId) => new Dictionary<string, byte[]>();
        }
    }
    
    /// <summary>
    /// Simple asset cache for frequently accessed data
    /// </summary>
    public class AssetCache
    {
        private readonly Dictionary<string, object> cache = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> lastAccess = new Dictionary<string, DateTime>();
        private readonly int maxSize = 1000;
        private readonly TimeSpan expiration = TimeSpan.FromMinutes(10);
        
        public T Get<T>(string key) where T : class
        {
            if (cache.TryGetValue(key, out var value) && value is T)
            {
                lastAccess[key] = DateTime.Now;
                return (T)value;
            }
            return null;
        }
        
        public void Set(string key, object value)
        {
            // Clean up old entries if needed
            if (cache.Count >= maxSize)
            {
                CleanUp();
            }
            
            cache[key] = value;
            lastAccess[key] = DateTime.Now;
        }
        
        public void Clear()
        {
            cache.Clear();
            lastAccess.Clear();
        }
        
        private void CleanUp()
        {
            var now = DateTime.Now;
            var toRemove = lastAccess
                .Where(kvp => now - kvp.Value > expiration)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in toRemove)
            {
                cache.Remove(key);
                lastAccess.Remove(key);
            }
        }
    }
    
    /// <summary>
    /// Implementation of IMapInfo that reads from NX data
    /// </summary>
    internal class MapInfoImpl : IMapInfo
    {
        private readonly int mapId;
        private readonly INxNode mapNode;
        
        public int MapId => mapId;
        public string Name { get; private set; }
        
        public MapInfoImpl(int mapId, INxNode mapNode)
        {
            this.mapId = mapId;
            this.mapNode = mapNode;
            this.Name = mapNode["info"]?["mapName"]?.GetValue<string>() ?? $"Map {mapId}";
        }
        
        public object GetNode(string path)
        {
            return mapNode.GetNode(path);
        }
        
        public IEnumerable<IBackgroundInfo> GetBackgrounds()
        {
            var backNode = mapNode["back"];
            if (backNode == null) yield break;
            
            foreach (var bgNode in backNode.Children)
            {
                yield return new BackgroundInfoImpl(bgNode);
            }
        }
        
        public IEnumerable<ITileInfo> GetTiles()
        {
            // MapleStory uses layers of tiles
            for (int layer = 0; layer < 8; layer++)
            {
                var layerNode = mapNode[$"{layer}"];
                if (layerNode == null) continue;
                
                var tileNode = layerNode["tile"];
                if (tileNode == null) continue;
                
                foreach (var tile in tileNode.Children)
                {
                    yield return new TileInfoImpl(tile, layer);
                }
            }
        }
        
        public IEnumerable<IObjectInfo> GetObjects()
        {
            // Objects are scattered across layers
            for (int layer = 0; layer < 8; layer++)
            {
                var layerNode = mapNode[$"{layer}"];
                if (layerNode == null) continue;
                
                var objNode = layerNode["obj"];
                if (objNode == null) continue;
                
                foreach (var obj in objNode.Children)
                {
                    yield return new ObjectInfoImpl(obj, layer);
                }
            }
        }
        
        public IEnumerable<IForegroundInfo> GetForegrounds()
        {
            // Foreground elements from front node
            var frontNode = mapNode["front"];
            if (frontNode == null) yield break;
            
            foreach (var fgNode in frontNode.Children)
            {
                yield return new ForegroundInfoImpl(fgNode);
            }
        }
        
        public IMapBounds GetBounds()
        {
            var info = mapNode["info"];
            if (info == null) return new MapBoundsImpl(-1000, 1000, 1000, -1000);
            
            return new MapBoundsImpl(
                info["VRLeft"]?.GetValue<float>() ?? -1000f,
                info["VRRight"]?.GetValue<float>() ?? 1000f,
                info["VRTop"]?.GetValue<float>() ?? 1000f,
                info["VRBottom"]?.GetValue<float>() ?? -1000f
            );
        }
    }
    
    internal class BackgroundInfoImpl : IBackgroundInfo
    {
        public string Name { get; }
        public SpriteData Sprite { get; }
        public float X { get; }
        public float Y { get; }
        public float ScrollRate { get; }
        public int Type { get; }
        
        public BackgroundInfoImpl(INxNode node)
        {
            Name = node["bS"]?.GetValue<string>() ?? "";
            X = node["x"]?.GetValue<float>() ?? 0f;
            Y = node["y"]?.GetValue<float>() ?? 0f;
            Type = node["type"]?.GetValue<int>() ?? 0;
            
            // Calculate scroll rate based on type
            ScrollRate = Type switch
            {
                0 => 0f,    // Static
                1 => 0.5f,  // Slow parallax
                2 => 0.3f,  // Medium parallax
                3 => 0.1f,  // Fast parallax
                _ => 0f
            };
            
            // Load sprite using asset loader
            var unitySprite = NXAssetLoader.Instance.LoadMapBackground(Name);
            Sprite = SpriteHelper.ConvertToSpriteData(unitySprite);
        }
    }
    
    internal class TileInfoImpl : ITileInfo
    {
        public int Id { get; }
        public SpriteData Sprite { get; }
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public bool IsSolid { get; }
        
        public TileInfoImpl(INxNode node, int layer)
        {
            Id = int.Parse(node.Name);
            X = node["x"]?.GetValue<float>() ?? 0f;
            Y = node["y"]?.GetValue<float>() ?? 0f;
            
            var tileName = node["u"]?.GetValue<string>() ?? "";
            var tileSet = node["tS"]?.GetValue<string>() ?? "";
            
            // Load sprite using asset loader
            if (!string.IsNullOrEmpty(tileSet) && !string.IsNullOrEmpty(tileName))
            {
                var unitySprite = NXAssetLoader.Instance.LoadMapTile(tileSet, tileName);
                Sprite = SpriteHelper.ConvertToSpriteData(unitySprite);
                
                if (Sprite == null && layer == 0) // Only log for first layer to reduce spam
                {
                    UnityEngine.Debug.LogWarning($"Failed to load tile sprite - tileSet: {tileSet}, tileName: {tileName}");
                }
            }
            
            // Tiles in certain layers are solid (platforms)
            IsSolid = layer == 0 || layer == 1;
            Width = 60f; // Standard tile size
            Height = 60f;
        }
    }
    
    internal class ObjectInfoImpl : IObjectInfo
    {
        public int Id { get; }
        public string Name { get; }
        public SpriteData Sprite { get; }
        public float X { get; }
        public float Y { get; }
        public int Z { get; }
        public bool IsAnimated { get; }
        public SpriteData[] AnimationFrames { get; }
        
        public ObjectInfoImpl(INxNode node, int layer)
        {
            Id = int.Parse(node.Name);
            Name = node["oS"]?.GetValue<string>() ?? "";
            X = node["x"]?.GetValue<float>() ?? 0f;
            Y = node["y"]?.GetValue<float>() ?? 0f;
            Z = node["z"]?.GetValue<int>() ?? layer;
            
            var objSet = node["oS"]?.GetValue<string>() ?? "";
            var objName = node["l0"]?.GetValue<string>() ?? "";
            
            // Check if animated
            IsAnimated = node["a0"] != null || node["a1"] != null;
            
            // Load sprites using asset loader
            if (!string.IsNullOrEmpty(objSet) && !string.IsNullOrEmpty(objName))
            {
                var unitySprite = NXAssetLoader.Instance.LoadMapObject(objSet, objName, 0);
                Sprite = SpriteHelper.ConvertToSpriteData(unitySprite);
                
                // Load animation frames if animated
                if (IsAnimated)
                {
                    var frames = new List<SpriteData>();
                    for (int i = 0; i < 20; i++) // Max 20 frames
                    {
                        var frame = NXAssetLoader.Instance.LoadMapObject(objSet, objName, i);
                        if (frame == null) break;
                        frames.Add(SpriteHelper.ConvertToSpriteData(frame));
                    }
                    AnimationFrames = frames.Count > 0 ? frames.ToArray() : null;
                }
            }
        }
    }
    
    internal class ForegroundInfoImpl : IForegroundInfo
    {
        public int Id { get; }
        public SpriteData Sprite { get; }
        public float X { get; }
        public float Y { get; }
        
        public ForegroundInfoImpl(INxNode node)
        {
            Id = int.Parse(node.Name);
            X = node["x"]?.GetValue<float>() ?? 0f;
            Y = node["y"]?.GetValue<float>() ?? 0f;
            
            // TODO: Load sprite
            Sprite = null;
        }
    }
    
    internal class MapBoundsImpl : IMapBounds
    {
        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
        
        public MapBoundsImpl(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
    
    // Helper class for sprite conversions
    internal static class SpriteHelper
    {
        public static SpriteData ConvertToSpriteData(Sprite sprite)
        {
            if (sprite == null) return null;
            
            var texture = sprite.texture;
            if (texture == null) return null;
            
            // For now, return a simple SpriteData with texture info
            // In production, we'd extract the actual pixel data
            return new SpriteData
            {
                Width = (int)sprite.rect.width,
                Height = (int)sprite.rect.height,
                OriginX = (int)sprite.pivot.x,
                OriginY = (int)sprite.pivot.y,
                Name = sprite.name,
                ImageData = new byte[0] // Placeholder - would need actual PNG data
            };
        }
    }
}