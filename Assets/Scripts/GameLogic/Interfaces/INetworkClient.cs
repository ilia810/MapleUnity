using System;

namespace MapleClient.GameLogic.Interfaces
{
    public interface INetworkClient
    {
        // Connection management
        bool IsConnected { get; }
        void Connect(string host, int port);
        void Disconnect();
        
        // Login/Authentication
        void SendLogin(string username, string password);
        void SelectWorld(int worldId);
        void SelectChannel(int channelId);
        void SelectCharacter(int characterId);
        
        // Movement & Actions
        void SendMove(float x, float y, byte[] movementData);
        void SendJump();
        void SendAttack(int skillId, byte[] attackData);
        void SendPickupItem(int objectId);
        
        // Chat
        void SendChat(string message, ChatType type = ChatType.All);
        void SendWhisper(string targetPlayer, string message);
        
        // Social
        void SendPartyInvite(string playerName);
        void SendTradeRequest(string playerName);
        void SendGuildMessage(string message);
        
        // Items & Inventory
        void SendUseItem(int itemId);
        void SendDropItem(int itemId, int quantity);
        void SendEquipItem(int itemId);
        
        // NPCs & Shops
        void SendNpcTalk(int npcId);
        void SendNpcReply(int npcId, byte response);
        void SendBuyItem(int npcId, int itemId, int quantity);
        void SendSellItem(int itemId, int quantity);
        
        // Skills
        void SendUseSkill(int skillId, byte level);
        
        // Events - These will be raised when data arrives from server
        event Action<string> OnConnected;
        event Action<string> OnDisconnected;
        event Action<string> OnError;
        
        // Login Events
        event Action<WorldInfo[]> OnWorldList;
        event Action<ChannelInfo[]> OnChannelList;
        event Action<CharacterInfo[]> OnCharacterList;
        event Action OnLoginSuccess;
        event Action<string> OnLoginFailed;
        
        // Game Events
        event Action<int, float, float> OnPlayerSpawn;
        event Action<int, float, float, byte[]> OnPlayerMove;
        event Action<int> OnPlayerLeave;
        event Action<int, string, int, float, float> OnPlayerJoin; // id, name, job, x, y
        
        event Action<int, int, float, float> OnMobSpawn; // id, mobId, x, y
        event Action<int> OnMobDespawn;
        event Action<int, int> OnMobDamage; // mobId, damage
        event Action<int, float, float> OnMobMove;
        
        event Action<int, int, float, float> OnItemDrop; // objectId, itemId, x, y
        event Action<int> OnItemPickup;
        
        event Action<ChatMessage> OnChatMessage;
        event Action<int, int> OnPlayerHpMpUpdate; // hp, mp
        event Action<int> OnExpGain;
        event Action OnLevelUp;
        
        // Party Events
        event Action<string> OnPartyInvite;
        event Action<PartyMember[]> OnPartyUpdate;
        
        // Map Events
        event Action<int, NetworkMapData> OnMapChange; // mapId, mapData
    }
    
    public enum ChatType
    {
        All,
        Whisper,
        Party,
        Guild,
        System
    }
    
    public struct WorldInfo
    {
        public int Id;
        public string Name;
        public int Flag;
        public string EventMessage;
        public int ChannelCount;
    }
    
    public struct ChannelInfo
    {
        public int Id;
        public string Name;
        public int UserCount;
        public int UserLimit;
    }
    
    public struct CharacterInfo
    {
        public int Id;
        public string Name;
        public byte Level;
        public short Job;
        public int MapId;
    }
    
    public struct ChatMessage
    {
        public ChatType Type;
        public string Sender;
        public string Message;
        public DateTime Timestamp;
    }
    
    public struct PartyMember
    {
        public int Id;
        public string Name;
        public int Level;
        public int Job;
        public int CurrentHp;
        public int MaxHp;
        public int CurrentMp;
        public int MaxMp;
        public int MapId;
    }
    
    public class NetworkMapData
    {
        public string Name { get; set; }
        public float PlayerSpawnX { get; set; }
        public float PlayerSpawnY { get; set; }
        // Additional map data will be added as needed
    }
}