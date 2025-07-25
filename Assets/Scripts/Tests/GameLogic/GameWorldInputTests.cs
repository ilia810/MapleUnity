using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;

namespace MapleClient.Tests.GameLogic
{
    public class GameWorldInputTests
    {
        private GameWorld gameWorld;
        private TestInputProvider inputProvider;
        private TestMapLoader mapLoader;

        [SetUp]
        public void Setup()
        {
            inputProvider = new TestInputProvider();
            mapLoader = new TestMapLoader();
            gameWorld = new GameWorld(mapLoader, inputProvider);
            
            // Load a simple test map
            var testMap = new MapData
            {
                MapId = 1,
                Name = "Test Map",
                Platforms = new System.Collections.Generic.List<Platform>
                {
                    new Platform 
                    { 
                        Id = 1, 
                        X1 = -1000, 
                        Y1 = 0, 
                        X2 = 1000, 
                        Y2 = 0, 
                        Type = PlatformType.Normal 
                    }
                },
                Portals = new System.Collections.Generic.List<Portal>
                {
                    new Portal
                    {
                        Id = 1,
                        Name = "spawn",
                        X = 0,
                        Y = 50,
                        Type = PortalType.Spawn
                    }
                }
            };
            mapLoader.AddMap(1, testMap);
            gameWorld.LoadMap(1);
        }

        [Test]
        public void GameWorld_JumpInput_CausesPlayerToJump()
        {
            // Arrange
            var player = gameWorld.Player;
            player.Position = new Vector2(0, 0);
            player.IsGrounded = true;
            
            // Act
            inputProvider.PressJump();
            gameWorld.Update(0.016f);
            
            // Assert
            Assert.That(player.Velocity.Y, Is.GreaterThan(0));
            Assert.That(player.IsJumping, Is.True);
        }

        [Test]
        public void GameWorld_LeftInput_MovesPlayerLeft()
        {
            // Arrange
            var player = gameWorld.Player;
            float startX = player.Position.X;
            
            // Act
            inputProvider.PressLeft();
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(player.Position.X, Is.LessThan(startX));
            Assert.That(player.Velocity.X, Is.LessThan(0));
        }

        [Test]
        public void GameWorld_RightInput_MovesPlayerRight()
        {
            // Arrange
            var player = gameWorld.Player;
            float startX = player.Position.X;
            
            // Act
            inputProvider.PressRight();
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(player.Position.X, Is.GreaterThan(startX));
            Assert.That(player.Velocity.X, Is.GreaterThan(0));
        }

        [Test]
        public void GameWorld_NoInput_PlayerStopsMoving()
        {
            // Arrange
            var player = gameWorld.Player;
            inputProvider.PressRight();
            gameWorld.Update(0.1f);
            Assert.That(player.Velocity.X, Is.GreaterThan(0));
            
            // Act
            inputProvider.ReleaseAll();
            gameWorld.Update(0.016f);
            
            // Assert
            Assert.That(player.Velocity.X, Is.EqualTo(0));
        }

        [Test]
        public void GameWorld_DownInput_CausesPlayerToCrouch()
        {
            // Arrange
            var player = gameWorld.Player;
            player.IsGrounded = true;
            
            // Act
            inputProvider.PressDown();
            gameWorld.Update(0.016f);
            
            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));
        }

        [Test]
        public void GameWorld_JumpWhileCrouching_DoesNotJump()
        {
            // Arrange
            var player = gameWorld.Player;
            player.IsGrounded = true;
            
            // Act
            inputProvider.PressDown();
            gameWorld.Update(0.016f);
            inputProvider.PressJump();
            gameWorld.Update(0.016f);
            
            // Assert
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));
            Assert.That(player.Velocity.Y, Is.EqualTo(0));
        }

        private class TestInputProvider : IInputProvider
        {
            private bool left, right, jump, attack, up, down;
            
            public bool IsLeftPressed => left;
            public bool IsRightPressed => right;
            public bool IsJumpPressed => jump;
            public bool IsAttackPressed => attack;
            public bool IsUpPressed => up;
            public bool IsDownPressed => down;
            
            public void PressLeft() => left = true;
            public void PressRight() => right = true;
            public void PressJump() => jump = true;
            public void PressAttack() => attack = true;
            public void PressUp() => up = true;
            public void PressDown() => down = true;
            
            public void ReleaseAll()
            {
                left = right = jump = attack = up = down = false;
            }
        }
        
        private class TestMapLoader : IMapLoader
        {
            private System.Collections.Generic.Dictionary<int, MapData> maps = 
                new System.Collections.Generic.Dictionary<int, MapData>();
                
            public void AddMap(int id, MapData map)
            {
                maps[id] = map;
            }
            
            public MapData GetMap(int mapId)
            {
                return maps.TryGetValue(mapId, out var map) ? map : null;
            }
        }
    }
}