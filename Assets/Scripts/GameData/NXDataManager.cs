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
    public class NXDataManager : IAssetProvider, IQuestDataProvider
    {
        private readonly string dataPath;
        private readonly Dictionary<string, INxFile> loadedFiles;
        private readonly AssetCache cache;
        
        // Provider implementations
        private NxItemDataProvider itemProvider;
        private MobDataProvider mobProvider;
        private SkillCatalog skillProvider;
        public MapleClient.GameLogic.Skills.SkillCastEffects SkillEffects { get; } = new MapleClient.GameLogic.Skills.SkillCastEffects();
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
        private IReadOnlyList<QuestDefinition> quests;
        public IReadOnlyList<QuestDefinition> Quests => quests ?? (quests = NxQuestData.Read(GetFile("quest")));
        
        public NXDataManager(string dataPath = null)
        {
            // Use actual MapleStory NX files from HeavenClient
            this.dataPath = ResolveDataPath(dataPath);
            this.loadedFiles = new Dictionary<string, INxFile>();
            this.cache = new AssetCache();
        }
        
        public string DataPath => dataPath;

        private static string ResolveDataPath(string configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath;
            string environmentPath = Environment.GetEnvironmentVariable("MAPLE_NX_PATH");
            if (!string.IsNullOrWhiteSpace(environmentPath))
                return environmentPath;
            string projectPath = Path.Combine(Application.streamingAssetsPath, "NX");
            if (Directory.Exists(projectPath))
                return projectPath;
            // Preserve the original development setup while allowing portable installs.
            const string legacyPath = @"C:\HeavenClient\MapleStory-Client\nx";
            return Directory.Exists(legacyPath) ? legacyPath : projectPath;
        }

        public void Initialize()
        {
            quests = null;
            Debug.Log($"Initializing NXDataManager with path: {dataPath}");
            
            // Load NX files
            LoadNXFile("Character.nx");
            LoadNXFile("Map.nx");
            LoadNXFile("String.nx");
            LoadNXFile("Npc.nx");
            LoadNXFile("Mob.nx");
            LoadNXFile("Item.nx");
            // These providers remain separate rewrite milestones.
            LoadNXFile("Skill.nx");
            LoadNXFile("Quest.nx");
            // LoadNXFile("Reactor.nx");
            // LoadNXFile("Sound.nx");
            LoadNXFile("UI.nx");
            LoadNXFile("Effect.nx");
            
            // Initialize providers
            itemProvider = new NxItemDataProvider(this);
            mobProvider = new MobDataProvider(this);
            skillProvider = new SkillCatalog(new NxSkillDataProvider(this), SkillEffects);
            foreach (var asset in Resources.LoadAll<CustomSkillAsset>("Skills"))
                if (!skillProvider.TryRegister(asset, out var error)) Debug.LogError($"Skill '{asset.name}': {error}");
            npcProvider = new NpcDataProvider(this);
            mapProvider = new MapDataProvider(this);
            characterProvider = new MapleClient.GameData.CharacterDataProvider();
            soundProvider = new SoundDataProvider(this);
            
            // Load essential data
            // Item and mob metadata load lazily by ID.
            // mobProvider.LoadAllMobs();
            // Skill metadata also loads lazily by ID/job.
            
            Debug.Log("NXDataManager initialized successfully");
        }
        
        public void Shutdown()
        {
            quests = null;
            cache.Clear();
            foreach (var entry in loadedFiles)
            {
                NXAssetLoader.Instance.UnregisterNxFile(entry.Key, entry.Value);
                (entry.Value as IDisposable)?.Dispose();
            }
            loadedFiles.Clear();
            MapleClient.GameData.SpriteLoader.ClearCache();
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
                    CanFly = node["fly"]?.Children.Any() == true,
                    CanMove = node["move"]?.Children.Any() == true || node["fly"]?.Children.Any() == true,
                    BodyAttack = (info["bodyAttack"]?.GetValue<int>() ?? 0) != 0,
                    ContactAnimations = NxMobContactLoader.Load(manager, mobId),
                    NoFlip = (info["noFlip"]?.GetValue<int>() ?? 0) != 0,
                    KnockbackThreshold = info["pushed"]?.GetValue<int>() ?? 0,
                    Skills = new List<int>(),
                    Drops = new Dictionary<int, DropInfo>()
                };
            }
            
            public MobInfo GetMob(int mobId)
            {
                if (mobs.TryGetValue(mobId, out var mob)) return mob;
                var node = manager.GetNode("mob", mobId.ToString("D7") + ".img");
                if (node == null) return null;
                mob = ParseMob(node, mobId);
                if (mob == null) return null;
                mob.Name = manager.GetNode("string", $"Mob.img/{mobId}/name")?.GetValue<string>() ?? $"Monster {mobId}";
                mob.SpritePath = mobId.ToString("D7") + ".img";
                mobs[mobId] = mob;
                return mob;
            }
            
            public Dictionary<int, MobInfo> GetAllMobs()
            {
                return new Dictionary<int, MobInfo>(mobs);
            }
            
            public bool MobExists(int mobId)
            {
                return GetMob(mobId) != null;
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
                
                return NxMapNames.Name(stringFile.GetNode("Map.img"), mapId);
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
