using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using MapleClient.GameLogic.Interfaces;
using UnityEngine;
using CharacterInfo = MapleClient.GameLogic.Interfaces.CharacterInfo;
using NetworkMapData = MapleClient.GameLogic.Interfaces.NetworkMapData;

namespace GameData.Network
{
    public class MapleStoryNetworkClient : INetworkClient
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private Thread receiveThread;
        private MapleAESCrypto sendCrypto;
        private MapleAESCrypto recvCrypto;
        private bool isRunning;
        private readonly Queue<Action> mainThreadActions = new Queue<Action>();
        
        // Connection info
        private string currentHost;
        private int currentPort;
        
        // INetworkClient implementation
        public bool IsConnected => tcpClient?.Connected ?? false;
        
        // Events
        public event Action<string> OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action<WorldInfo[]> OnWorldList;
        public event Action<ChannelInfo[]> OnChannelList;
        public event Action<CharacterInfo[]> OnCharacterList;
        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action<int, float, float> OnPlayerSpawn;
        public event Action<int, float, float, byte[]> OnPlayerMove;
        public event Action<int> OnPlayerLeave;
        public event Action<int, string, int, float, float> OnPlayerJoin;
        public event Action<int, int, float, float> OnMobSpawn;
        public event Action<int> OnMobDespawn;
        public event Action<int, int> OnMobDamage;
        public event Action<int, float, float> OnMobMove;
        public event Action<int, int, float, float> OnItemDrop;
        public event Action<int> OnItemPickup;
        public event Action<ChatMessage> OnChatMessage;
        public event Action<int, int> OnPlayerHpMpUpdate;
        public event Action<int> OnExpGain;
        public event Action OnLevelUp;
        public event Action<string> OnPartyInvite;
        public event Action<PartyMember[]> OnPartyUpdate;
        public event Action<int, NetworkMapData> OnMapChange;
        
        public void Connect(string host, int port)
        {
            try
            {
                currentHost = host;
                currentPort = port;
                
                tcpClient = new TcpClient();
                tcpClient.Connect(host, port);
                stream = tcpClient.GetStream();
                
                // Start receive thread
                isRunning = true;
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();
                
                Debug.Log($"Connected to MapleStory server at {host}:{port}");
                QueueMainThreadAction(() => OnConnected?.Invoke($"Connected to {host}:{port}"));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect: {e.Message}");
                QueueMainThreadAction(() => OnError?.Invoke($"Connection failed: {e.Message}"));
            }
        }
        
        public void Disconnect()
        {
            isRunning = false;
            
            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch { }
            
            receiveThread?.Join(1000);
            
            QueueMainThreadAction(() => OnDisconnected?.Invoke("Disconnected"));
        }
        
        private void ReceiveLoop()
        {
            try
            {
                while (isRunning && stream.CanRead)
                {
                    // Read packet header (4 bytes)
                    byte[] header = new byte[4];
                    int bytesRead = stream.Read(header, 0, 4);
                    if (bytesRead < 4) continue;
                    
                    // Get packet length
                    int length = GetPacketLength(header);
                    if (length < 2) continue;
                    
                    // Read packet data
                    byte[] data = new byte[length];
                    bytesRead = 0;
                    while (bytesRead < length)
                    {
                        int read = stream.Read(data, bytesRead, length - bytesRead);
                        if (read <= 0) break;
                        bytesRead += read;
                    }
                    
                    if (bytesRead == length)
                    {
                        // Decrypt if needed
                        if (recvCrypto != null)
                        {
                            data = recvCrypto.Decrypt(data);
                        }
                        
                        HandlePacket(data);
                    }
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"Receive error: {e.Message}");
                    QueueMainThreadAction(() => OnError?.Invoke($"Receive error: {e.Message}"));
                }
            }
            finally
            {
                if (isRunning)
                {
                    QueueMainThreadAction(() => OnDisconnected?.Invoke("Connection lost"));
                }
            }
        }
        
        private void HandlePacket(byte[] data)
        {
            var reader = new PacketReader(data);
            short opcode = reader.ReadShort();
            
            switch ((SendOpcode)opcode)
            {
                case SendOpcode.LOGIN_STATUS:
                    HandleLoginStatus(reader);
                    break;
                    
                case SendOpcode.SERVERLIST:
                    HandleServerList(reader);
                    break;
                    
                case SendOpcode.CHARLIST:
                    HandleCharList(reader);
                    break;
                    
                case SendOpcode.SERVER_IP:
                    HandleServerIP(reader);
                    break;
                    
                case SendOpcode.SET_FIELD:
                    HandleSetField(reader);
                    break;
                    
                case SendOpcode.SPAWN_PLAYER:
                    HandleSpawnPlayer(reader);
                    break;
                    
                case SendOpcode.REMOVE_PLAYER_FROM_MAP:
                    HandleRemovePlayer(reader);
                    break;
                    
                case SendOpcode.MOVE_PLAYER:
                    HandleMovePlayer(reader);
                    break;
                    
                case SendOpcode.CHATTEXT:
                    HandleChatText(reader);
                    break;
                    
                case SendOpcode.UPDATE_STATS:
                    HandleUpdateStats(reader);
                    break;
                    
                case SendOpcode.SPAWN_MONSTER:
                case SendOpcode.SPAWN_MONSTER_CONTROL:
                    HandleSpawnMonster(reader);
                    break;
                    
                case SendOpcode.KILL_MONSTER:
                    HandleKillMonster(reader);
                    break;
                    
                case SendOpcode.MOVE_MONSTER:
                    HandleMoveMonster(reader);
                    break;
                    
                case SendOpcode.DROP_ITEM_FROM_MAPOBJECT:
                    HandleDropItem(reader);
                    break;
                    
                case SendOpcode.REMOVE_ITEM_FROM_MAP:
                    HandleRemoveItem(reader);
                    break;
                    
                case SendOpcode.PING:
                    HandlePing();
                    break;
                    
                default:
                    // Unknown packet
                    break;
            }
        }
        
        // Packet handlers
        private void HandleLoginStatus(PacketReader reader)
        {
            byte status = reader.ReadByte();
            if (status == 0)
            {
                // Success
                int accountId = reader.ReadInt();
                byte gender = reader.ReadByte();
                bool isAdmin = reader.ReadByte() > 0;
                
                // Read encryption IV
                byte[] sendIv = reader.ReadBytes(4);
                byte[] recvIv = reader.ReadBytes(4);
                
                // Initialize crypto
                sendCrypto = new MapleAESCrypto(sendIv, 83);
                recvCrypto = new MapleAESCrypto(recvIv, 83);
                
                QueueMainThreadAction(() => OnLoginSuccess?.Invoke());
            }
            else
            {
                string message = GetLoginErrorMessage(status);
                QueueMainThreadAction(() => OnLoginFailed?.Invoke(message));
            }
        }
        
        // Send methods
        public void SendLogin(string username, string password)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.LOGIN_PASSWORD);
            packet.WriteString(username);
            packet.WriteString(password);
            packet.WriteBytes(new byte[6]); // MAC address placeholder
            packet.WriteInt(0); // GameRoomClient
            packet.WriteByte(0); // Unknown
            packet.WriteInt(0); // Unknown
            packet.WriteInt(0); // Unknown
            
            SendPacket(packet.ToArray());
        }
        
        public void SelectWorld(int worldId)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.CHARLIST_REQUEST);
            packet.WriteByte((byte)worldId);
            packet.WriteByte(0); // Channel
            
            SendPacket(packet.ToArray());
        }
        
        public void SelectChannel(int channelId)
        {
            // In MapleStory, channel is selected with world
        }
        
        public void SelectCharacter(int characterId)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.CHAR_SELECT);
            packet.WriteInt(characterId);
            packet.WriteString(""); // MAC address
            
            SendPacket(packet.ToArray());
        }
        
        public void SendMove(float x, float y, byte[] movementData)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.MOVE_PLAYER);
            packet.WriteByte(0); // Portal count
            packet.WriteInt(0); // Unknown
            packet.WriteBytes(movementData.Length > 0 ? movementData : CreateMovementData(x, y));
            
            SendPacket(packet.ToArray());
        }
        
        public void SendJump()
        {
            // Jump is handled through movement packets
        }
        
        public void SendAttack(int skillId, byte[] attackData)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.CLOSE_RANGE_ATTACK);
            packet.WriteByte(0); // Attack info
            packet.WriteByte(0); // Number of attacks
            packet.WriteByte(0); // Number of damaged mobs
            packet.WriteInt(skillId);
            
            SendPacket(packet.ToArray());
        }
        
        public void SendPickupItem(int objectId)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.ITEM_PICKUP);
            packet.WriteInt(0); // Timestamp
            packet.WritePosition(0, 0); // Position
            packet.WriteInt(objectId);
            
            SendPacket(packet.ToArray());
        }
        
        public void SendChat(string message, ChatType type = ChatType.All)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.GENERAL_CHAT);
            packet.WriteString(message);
            packet.WriteByte((byte)type);
            
            SendPacket(packet.ToArray());
        }
        
        public void SendWhisper(string targetPlayer, string message)
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.WHISPER);
            packet.WriteByte(6); // Find & whisper
            packet.WriteString(targetPlayer);
            packet.WriteString(message);
            
            SendPacket(packet.ToArray());
        }
        
        // Utility methods
        private void SendPacket(byte[] data)
        {
            if (!IsConnected) return;
            
            try
            {
                byte[] encrypted = data;
                if (sendCrypto != null)
                {
                    encrypted = sendCrypto.Encrypt(data);
                }
                
                // Send header
                byte[] header = CreatePacketHeader(encrypted.Length);
                stream.Write(header, 0, header.Length);
                
                // Send data
                stream.Write(encrypted, 0, encrypted.Length);
                stream.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"Send error: {e.Message}");
            }
        }
        
        private byte[] CreatePacketHeader(int length)
        {
            byte[] header = new byte[4];
            // MapleStory packet header format
            header[0] = (byte)(length & 0xFF);
            header[1] = (byte)((length >> 8) & 0xFF);
            return header;
        }
        
        private int GetPacketLength(byte[] header)
        {
            return header[0] | (header[1] << 8);
        }
        
        private byte[] CreateMovementData(float x, float y)
        {
            var writer = new PacketWriter();
            writer.WriteByte(1); // Number of movements
            writer.WriteByte(0); // Movement type (absolute)
            writer.WritePosition(x, y);
            writer.WriteShort(0); // X wobble
            writer.WriteShort(0); // Y wobble
            writer.WriteShort(0); // Unknown
            writer.WriteByte(0); // Foothold
            writer.WriteShort(0); // Stance
            writer.WriteShort(0); // Unknown
            
            return writer.ToArray();
        }
        
        private void QueueMainThreadAction(Action action)
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(action);
            }
        }
        
        public void ProcessMainThreadActions()
        {
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                {
                    mainThreadActions.Dequeue()?.Invoke();
                }
            }
        }
        
        // Additional send methods
        public void SendPartyInvite(string playerName) { }
        public void SendTradeRequest(string playerName) { }
        public void SendGuildMessage(string message) { }
        public void SendUseItem(int itemId) { }
        public void SendDropItem(int itemId, int quantity) { }
        public void SendEquipItem(int itemId) { }
        public void SendNpcTalk(int npcId) { }
        public void SendNpcReply(int npcId, byte response) { }
        public void SendBuyItem(int npcId, int itemId, int quantity) { }
        public void SendSellItem(int itemId, int quantity) { }
        public void SendUseSkill(int skillId, byte level) { }
        
        // Stub handlers
        private void HandleServerList(PacketReader reader) { }
        private void HandleCharList(PacketReader reader) { }
        private void HandleServerIP(PacketReader reader) { }
        private void HandleSetField(PacketReader reader) { }
        private void HandleSpawnPlayer(PacketReader reader) { }
        private void HandleRemovePlayer(PacketReader reader) { }
        private void HandleMovePlayer(PacketReader reader) { }
        private void HandleChatText(PacketReader reader) { }
        private void HandleUpdateStats(PacketReader reader) { }
        private void HandleSpawnMonster(PacketReader reader) { }
        private void HandleKillMonster(PacketReader reader) { }
        private void HandleMoveMonster(PacketReader reader) { }
        private void HandleDropItem(PacketReader reader) { }
        private void HandleRemoveItem(PacketReader reader) { }
        private void HandlePing() 
        {
            var packet = new PacketWriter();
            packet.WriteShort((short)RecvOpcode.PONG);
            SendPacket(packet.ToArray());
        }
        
        private string GetLoginErrorMessage(byte status)
        {
            switch (status)
            {
                case 3: return "Invalid username or password";
                case 4: return "Account banned";
                case 5: return "Account not found";
                case 7: return "Already logged in";
                default: return $"Login failed (code: {status})";
            }
        }
    }
}