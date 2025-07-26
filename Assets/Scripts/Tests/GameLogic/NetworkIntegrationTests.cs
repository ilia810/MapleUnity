using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.GameLogic
{
    [TestFixture]
    public class NetworkIntegrationTests
    {
        private GameWorld gameWorld;
        private MockNetworkClient mockNetwork;
        private MockInputProvider mockInput;
        private MockMapLoader mockMapLoader;

        [SetUp]
        public void Setup()
        {
            mockNetwork = new MockNetworkClient();
            mockInput = new MockInputProvider();
            mockMapLoader = new MockMapLoader();
            gameWorld = new GameWorld(mockInput, mockMapLoader, mockNetwork);
        }

        [Test]
        public void GameWorld_SendsMovementToNetwork_WhenPlayerMoves()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockInput.IsLeftPressed = true;

            // Act
            gameWorld.Update(1f);

            // Assert
            Assert.That(mockNetwork.MovementsSent.Count, Is.EqualTo(1));
            var movement = mockNetwork.MovementsSent[0];
            Assert.That(movement.X, Is.LessThan(100f)); // Player moved left
            Assert.That(movement.Y, Is.EqualTo(100f));
        }

        [Test]
        public void GameWorld_SendsJumpToNetwork_WhenPlayerJumps()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockInput.IsJumpPressed = true;

            // Act
            gameWorld.Update(0.1f);

            // Assert
            Assert.That(mockNetwork.JumpsSent, Is.EqualTo(1));
        }

        [Test]
        public void GameWorld_SendsAttackToNetwork_WhenPlayerAttacks()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockInput.IsAttackPressed = true;

            // Act
            gameWorld.Update(0.1f);

            // Assert
            Assert.That(mockNetwork.AttacksSent.Count, Is.EqualTo(1));
            Assert.That(mockNetwork.AttacksSent[0].SkillId, Is.EqualTo(0)); // Basic attack
        }

        [Test]
        public void GameWorld_HandlesPlayerJoinEvent_FromNetwork()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);

            // Act
            mockNetwork.SimulatePlayerJoin(2, "OtherPlayer", 100, 200, 200);

            // Assert
            Assert.That(gameWorld.Players.Count, Is.EqualTo(2));
            var otherPlayer = gameWorld.Players[1];
            Assert.That(otherPlayer.Id, Is.EqualTo(2));
            Assert.That(otherPlayer.Name, Is.EqualTo("OtherPlayer"));
            Assert.That(otherPlayer.Position.X, Is.EqualTo(200));
            Assert.That(otherPlayer.Position.Y, Is.EqualTo(200));
        }

        [Test]
        public void GameWorld_HandlesPlayerMoveEvent_FromNetwork()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockNetwork.SimulatePlayerJoin(2, "OtherPlayer", 100, 200, 200);

            // Act
            mockNetwork.SimulatePlayerMove(2, 250, 200, new byte[0]);

            // Assert
            var otherPlayer = gameWorld.Players[1];
            Assert.That(otherPlayer.Position.X, Is.EqualTo(250));
            Assert.That(otherPlayer.Position.Y, Is.EqualTo(200));
        }

        [Test]
        public void GameWorld_HandlesPlayerLeaveEvent_FromNetwork()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockNetwork.SimulatePlayerJoin(2, "OtherPlayer", 100, 200, 200);

            // Act
            mockNetwork.SimulatePlayerLeave(2);

            // Assert
            Assert.That(gameWorld.Players.Count, Is.EqualTo(1));
            Assert.That(gameWorld.Players[0].Id, Is.EqualTo(1)); // Only local player remains
        }

        [Test]
        public void GameWorld_HandlesMobSpawnEvent_FromNetwork()
        {
            // Arrange
            gameWorld.LoadMap(1);

            // Act
            mockNetwork.SimulateMobSpawn(101, 1000, 300, 300);

            // Assert
            Assert.That(gameWorld.Monsters.Count, Is.EqualTo(1));
            var monster = gameWorld.Monsters[0];
            Assert.That(monster.Id, Is.EqualTo(101));
            Assert.That(monster.Position.X, Is.EqualTo(300));
            Assert.That(monster.Position.Y, Is.EqualTo(300));
        }

        [Test]
        public void GameWorld_HandlesChatMessage_FromNetwork()
        {
            // Arrange
            var chatMessages = new List<ChatMessage>();
            gameWorld.OnChatMessageReceived += msg => chatMessages.Add(msg);

            // Act
            mockNetwork.SimulateChatMessage(ChatType.All, "TestUser", "Hello World!");

            // Assert
            Assert.That(chatMessages.Count, Is.EqualTo(1));
            Assert.That(chatMessages[0].Sender, Is.EqualTo("TestUser"));
            Assert.That(chatMessages[0].Message, Is.EqualTo("Hello World!"));
        }

        [Test]
        public void GameWorld_SendsChatMessage_ToNetwork()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);

            // Act
            gameWorld.SendChatMessage("Test message", ChatType.All);

            // Assert
            Assert.That(mockNetwork.ChatMessagesSent.Count, Is.EqualTo(1));
            Assert.That(mockNetwork.ChatMessagesSent[0].Message, Is.EqualTo("Test message"));
            Assert.That(mockNetwork.ChatMessagesSent[0].Type, Is.EqualTo(ChatType.All));
        }

        [Test]
        public void GameWorld_HandlesItemDropEvent_FromNetwork()
        {
            // Arrange
            gameWorld.LoadMap(1);

            // Act
            mockNetwork.SimulateItemDrop(201, 5000, 400, 400);

            // Assert
            Assert.That(gameWorld.DroppedItems.Count, Is.EqualTo(1));
            var item = gameWorld.DroppedItems[0];
            Assert.That(item.ObjectId, Is.EqualTo(201));
            Assert.That(item.ItemId, Is.EqualTo(5000));
            Assert.That(item.Position.X, Is.EqualTo(400));
        }

        [Test]
        public void GameWorld_SendsPickupRequest_WhenNearItem()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);
            mockNetwork.SimulateItemDrop(201, 5000, 105, 100); // Drop item near player

            // Act
            gameWorld.Update(0.1f); // Should auto-pickup

            // Assert
            Assert.That(mockNetwork.PickupRequestsSent.Count, Is.EqualTo(1));
            Assert.That(mockNetwork.PickupRequestsSent[0], Is.EqualTo(201));
        }

        [Test]
        public void GameWorld_UpdatesPlayerStats_OnHpMpUpdate()
        {
            // Arrange
            gameWorld.InitializePlayer(1, "TestPlayer", 100, 100, 50, 50, 100, 100);

            // Act
            mockNetwork.SimulateHpMpUpdate(75, 40);

            // Assert
            Assert.That(gameWorld.Player.CurrentHP, Is.EqualTo(75));
            Assert.That(gameWorld.Player.CurrentMP, Is.EqualTo(40));
        }

        private class MockNetworkClient : INetworkClient
        {
            public bool IsConnected { get; private set; } = true;
            public List<(float X, float Y, byte[] Data)> MovementsSent = new List<(float, float, byte[])>();
            public int JumpsSent = 0;
            public List<(int SkillId, byte[] Data)> AttacksSent = new List<(int, byte[])>();
            public List<(string Message, ChatType Type)> ChatMessagesSent = new List<(string, ChatType)>();
            public List<int> PickupRequestsSent = new List<int>();

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

            public void Connect(string host, int port) { }
            public void Disconnect() { IsConnected = false; }

            public void SendLogin(string username, string password) { }
            public void SelectWorld(int worldId) { }
            public void SelectChannel(int channelId) { }
            public void SelectCharacter(int characterId) { }

            public void SendMove(float x, float y, byte[] movementData)
            {
                MovementsSent.Add((x, y, movementData));
            }

            public void SendJump()
            {
                JumpsSent++;
            }

            public void SendAttack(int skillId, byte[] attackData)
            {
                AttacksSent.Add((skillId, attackData));
            }

            public void SendPickupItem(int objectId)
            {
                PickupRequestsSent.Add(objectId);
            }

            public void SendChat(string message, ChatType type = ChatType.All)
            {
                ChatMessagesSent.Add((message, type));
            }

            public void SendWhisper(string targetPlayer, string message) { }
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

            // Helper methods for testing
            public void SimulatePlayerJoin(int id, string name, int job, float x, float y)
            {
                OnPlayerJoin?.Invoke(id, name, job, x, y);
            }

            public void SimulatePlayerMove(int id, float x, float y, byte[] data)
            {
                OnPlayerMove?.Invoke(id, x, y, data);
            }

            public void SimulatePlayerLeave(int id)
            {
                OnPlayerLeave?.Invoke(id);
            }

            public void SimulateMobSpawn(int id, int mobId, float x, float y)
            {
                OnMobSpawn?.Invoke(id, mobId, x, y);
            }

            public void SimulateChatMessage(ChatType type, string sender, string message)
            {
                OnChatMessage?.Invoke(new ChatMessage
                {
                    Type = type,
                    Sender = sender,
                    Message = message,
                    Timestamp = DateTime.Now
                });
            }

            public void SimulateItemDrop(int objectId, int itemId, float x, float y)
            {
                OnItemDrop?.Invoke(objectId, itemId, x, y);
            }

            public void SimulateHpMpUpdate(int hp, int mp)
            {
                OnPlayerHpMpUpdate?.Invoke(hp, mp);
            }
        }
        
        private class MockInputProvider : IInputProvider
        {
            public bool IsLeftPressed { get; set; }
            public bool IsRightPressed { get; set; }
            public bool IsUpPressed { get; set; }
            public bool IsDownPressed { get; set; }
            public bool IsJumpPressed { get; set; }
            public bool IsAttackPressed { get; set; }
        }
        
        private class MockMapLoader : IMapLoader
        {
            public MapData GetMap(int mapId)
            {
                return new MapData
                {
                    MapId = mapId,
                    Name = "Test Map",
                    Platforms = new List<Platform>
                    {
                        new Platform { Id = 1, X1 = -500, Y1 = 0, X2 = 500, Y2 = 0, Type = PlatformType.Normal }
                    },
                    MonsterSpawns = new List<MonsterSpawn>(),
                    Portals = new List<Portal>()
                };
            }
        }
    }
}