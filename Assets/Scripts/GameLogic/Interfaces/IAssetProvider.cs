using System.Collections.Generic;
using MapleClient.GameLogic.Data;

namespace MapleClient.GameLogic.Interfaces
{
    /// <summary>
    /// Main interface for accessing all game assets
    /// </summary>
    public interface IAssetProvider
    {
        IItemDataProvider ItemData { get; }
        IMobDataProvider MobData { get; }
        ISkillDataProvider SkillData { get; }
        INpcDataProvider NpcData { get; }
        IMapDataProvider MapData { get; }
        ICharacterDataProvider CharacterData { get; }
        ISoundDataProvider SoundData { get; }
        
        void Initialize();
        void Shutdown();
    }
    
    /// <summary>
    /// Provides item information
    /// </summary>
    public interface IItemDataProvider
    {
        ItemInfo GetItem(int itemId);
        Dictionary<int, ItemInfo> GetAllItems();
        bool ItemExists(int itemId);
    }
    
    /// <summary>
    /// Provides monster/mob information
    /// </summary>
    public interface IMobDataProvider
    {
        MobInfo GetMob(int mobId);
        Dictionary<int, MobInfo> GetAllMobs();
        bool MobExists(int mobId);
    }
    
    /// <summary>
    /// Provides skill information
    /// </summary>
    public interface ISkillDataProvider
    {
        SkillInfo GetSkill(int skillId);
        Dictionary<int, SkillInfo> GetSkillsForJob(int jobId);
        bool SkillExists(int skillId);
    }
    
    /// <summary>
    /// Provides NPC information
    /// </summary>
    public interface INpcDataProvider
    {
        NpcInfo GetNpc(int npcId);
        ShopInfo GetShop(int npcId);
        string[] GetNpcScript(int npcId);
        bool NpcExists(int npcId);
    }
    
    /// <summary>
    /// Enhanced map data provider
    /// </summary>
    public interface IMapDataProvider
    {
        MapInfo GetMap(int mapId);
        IMapInfo GetMapInfo(int mapId);
        string GetMapName(int mapId);
        byte[] GetMapBackground(int mapId);
        string GetMapMusic(int mapId);
        bool MapExists(int mapId);
    }
    
    /// <summary>
    /// Provides character appearance data
    /// </summary>
    public interface ICharacterDataProvider
    {
        SpriteData GetBodySprite(int skin, CharacterState state, int frame);
        SpriteData GetHeadSprite(int skin, CharacterState state, int frame);
        SpriteData GetHairSprite(int hairId, CharacterState state, int frame);
        SpriteData GetFaceSprite(int faceId, CharacterExpression expression);
        EquipSprite GetEquipSprite(int itemId, CharacterState state, int frame);
        byte[] GetHairSpriteData(int hairId, CharacterState state, int frame);
        byte[] GetFaceSpriteData(int faceId, CharacterExpression expression);
        int GetAnimationFrameCount(CharacterState state);
    }
    
    /// <summary>
    /// Provides sound and music data
    /// </summary>
    public interface ISoundDataProvider
    {
        byte[] GetBackgroundMusic(string name);
        byte[] GetSoundEffect(string name);
        Dictionary<string, byte[]> GetSkillSounds(int skillId);
    }
    
    // Data structures
    public class ItemInfo
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int Price { get; set; }
        public bool IsCash { get; set; }
        public bool IsQuest { get; set; }
        public bool IsTradeable { get; set; }
        public bool IsOneOfAKind { get; set; }
        public int MaxStack { get; set; }
        
        // Equipment specific
        public Dictionary<StatType, int> Stats { get; set; }
        public int RequiredLevel { get; set; }
        public int RequiredStr { get; set; }
        public int RequiredDex { get; set; }
        public int RequiredInt { get; set; }
        public int RequiredLuk { get; set; }
        public JobType RequiredJob { get; set; }
        public int Slots { get; set; }
        
        // Consumable specific
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int HpRate { get; set; }
        public int MpRate { get; set; }
        public int Time { get; set; } // Buff duration
        public Dictionary<BuffType, int> Buffs { get; set; }
        
        // Visual
        public string IconPath { get; set; }
        public byte[] IconData { get; set; }
    }
    
    public class MobInfo
    {
        public int MobId { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public int MP { get; set; }
        public int Exp { get; set; }
        public int PADamage { get; set; }
        public int PDDamage { get; set; }
        public int MADamage { get; set; }
        public int MDDamage { get; set; }
        public int Accuracy { get; set; }
        public int Avoidability { get; set; }
        public int Speed { get; set; }
        public bool IsBoss { get; set; }
        public bool IsUndead { get; set; }
        public bool CanFly { get; set; }
        public ElementType Element { get; set; }
        public Dictionary<int, float> ElementalDamage { get; set; }
        public List<int> Skills { get; set; }
        public Dictionary<int, DropInfo> Drops { get; set; }
        public string SpritePath { get; set; }
    }
    
    public class SkillInfo
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string H1 { get; set; } // Additional description
        public int JobId { get; set; }
        public int MaxLevel { get; set; }
        public bool IsPassive { get; set; }
        public SkillType Type { get; set; }
        public ElementType Element { get; set; }
        
        // Common properties
        public int AttackCount { get; set; }
        public int MobCount { get; set; }
        public int BulletCount { get; set; }
        public int BulletConsume { get; set; }
        public int ItemCon { get; set; }
        public int ItemConNo { get; set; }
        
        // Per level data
        public class LevelData
        {
            public int MpCost { get; set; }
            public int Damage { get; set; }
            public int AttackCount { get; set; }
            public int MobCount { get; set; }
            public int Range { get; set; }
            public int Duration { get; set; }
            public int Cooldown { get; set; }
            public Dictionary<BuffType, int> Buffs { get; set; }
            public int Mastery { get; set; }
            public int Critical { get; set; }
            
            // Recovery
            public int Hp { get; set; }
            public int HpR { get; set; }
            public int Mp { get; set; }
            public int MpR { get; set; }
            
            // Other properties
            public int Prop { get; set; } // Success rate
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }
        
        public Dictionary<int, LevelData> Levels { get; set; }
        public string IconPath { get; set; }
        public string EffectPath { get; set; }
        public Dictionary<string, byte[]> SoundEffects { get; set; }
    }
    
    public class NpcInfo
    {
        public int NpcId { get; set; }
        public string Name { get; set; }
        public string Function { get; set; }
        public bool IsShop { get; set; }
        public bool IsStorage { get; set; }
        public bool IsGuildRank { get; set; }
        public List<int> Quests { get; set; }
        public string SpritePath { get; set; }
    }
    
    public class ShopInfo
    {
        public int ShopId { get; set; }
        public int NpcId { get; set; }
        public List<ShopItem> Items { get; set; }
        public bool CanRecharge { get; set; }
    }
    
    public class ShopItem
    {
        public int ItemId { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; } // -1 for unlimited
        public int MaxPerSlot { get; set; }
        public int Period { get; set; } // For limited time items
        public float SellbackRate { get; set; }
    }
    
    public class MapInfo
    {
        public int MapId { get; set; }
        public string Name { get; set; }
        public string StreetName { get; set; }
        public MapType Type { get; set; }
        public int ReturnMap { get; set; }
        public int ForcedReturn { get; set; }
        public float MobRate { get; set; }
        public bool IsTown { get; set; }
        public bool CanVipRock { get; set; }
        public bool CanDecorate { get; set; }
        public int FieldLimit { get; set; }
        public int TimeLimit { get; set; }
        public string BGM { get; set; }
        public List<PortalInfo> Portals { get; set; }
        public List<FootholdInfo> Footholds { get; set; }
        public List<LifeInfo> Life { get; set; } // NPCs and Mobs
        public Rectangle Bounds { get; set; }
        public List<BackgroundLayer> Backgrounds { get; set; }
    }
    
    public class DropInfo
    {
        public int ItemId { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public int Chance { get; set; } // Out of 1000000
        public int QuestId { get; set; } // Quest requirement
    }
    
    public class EquipSprite
    {
        public byte[] SpriteData { get; set; }
        public int OriginX { get; set; }
        public int OriginY { get; set; }
        public int Z { get; set; } // Layer order
        public Dictionary<string, int> Map { get; set; } // Body part mappings
        public SpriteData Sprite { get; set; } // Sprite data reference
    }
    
    public class PortalInfo
    {
        public int PortalId { get; set; }
        public string Name { get; set; }
        public PortalType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int TargetMapId { get; set; }
        public string TargetPortalName { get; set; }
        public string Script { get; set; }
    }
    
    public class FootholdInfo
    {
        public int Id { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int Next { get; set; }
        public int Prev { get; set; }
    }
    
    public class LifeInfo
    {
        public LifeType Type { get; set; } // NPC or Mob
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Foothold { get; set; }
        public int RespawnTime { get; set; }
        public int MobTime { get; set; }
        public bool Flip { get; set; }
    }
    
    public class BackgroundLayer
    {
        public string SpritePath { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public BackgroundType Type { get; set; }
        public int CX { get; set; } // Parallax
        public int CY { get; set; }
        public int RX { get; set; } // Repeat
        public int RY { get; set; }
        public bool Flip { get; set; }
        public int Opacity { get; set; }
    }
    
    // Enums
    public enum ItemType
    {
        Equip,
        Use,
        Setup,
        Etc,
        Cash,
        Pet
    }
    
    public enum StatType
    {
        STR, DEX, INT, LUK,
        MaxHP, MaxMP,
        WeaponAttack, MagicAttack,
        WeaponDefense, MagicDefense,
        Accuracy, Avoidability,
        Hands, Speed, Jump,
        CraftLevel
    }
    
    public enum JobType
    {
        Beginner = 0,
        Warrior = 100,
        Magician = 200,
        Bowman = 300,
        Thief = 400,
        Pirate = 500
    }
    
    public enum BuffType
    {
        WeaponAttack, MagicAttack,
        WeaponDefense, MagicDefense,
        Accuracy, Avoidability,
        Speed, Jump,
        MesoUp, DropUp,
        HPRecovery, MPRecovery,
        PowerGuard, HyperBody,
        Invincible, Hide
    }
    
    public enum ElementType
    {
        Physical = 0,
        Ice = 1,
        Fire = 2,
        Lightning = 3,
        Poison = 4,
        Holy = 5,
        Dark = 6,
        Neutral = 7
    }
    
    public enum SkillType
    {
        Attack,
        Buff,
        Summon,
        Recovery,
        Movement,
        Passive
    }
    
    public enum CharacterState
    {
        Stand, Walk, Jump, Fall,
        Alert, Prone, Fly,
        Ladder, Rope,
        Attack1, Attack2,
        Skill
    }
    
    public enum CharacterExpression
    {
        Default, Blink, Hit, Smile,
        Troubled, Cry, Angry,
        Bewildered, Stunned,
        Vomit, Oops
    }
    
    public enum MapType
    {
        Regular,
        PQ, // Party Quest
        Event,
        GMMap,
        CashShop,
        FreeMarket
    }
    
    public enum PortalType
    {
        Spawn,
        Regular,
        Script,
        ScriptInvisible,
        Collision,
        Hidden,
        ScriptHidden,
        VerticalSpring,
        HorizontalSpring,
        TownPortalPoint
    }
    
    public enum LifeType
    {
        NPC,
        Mob
    }
    
    public enum BackgroundType
    {
        Regular,
        HorizontalTile,
        VerticalTile,
        BothTile,
        HorizontalScroll,
        VerticalScroll,
        BothScroll
    }
    
    public struct Rectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}