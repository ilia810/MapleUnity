using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Interfaces;
using PortalType = MapleClient.GameLogic.PortalType;

namespace MapleClient.Tests.GameLogic
{
    public class JumpIntegrationTests
    {
        private GameWorld gameWorld;
        private SimulatedInputProvider inputProvider;
        
        [SetUp]
        public void Setup()
        {
            inputProvider = new SimulatedInputProvider();
            var mapLoader = new SimpleMapLoader();
            gameWorld = new GameWorld(inputProvider, mapLoader);
            gameWorld.LoadMap(1);
        }
        
        [Test]
        public void FullJumpCycle_FromInputToLanding()
        {
            // Arrange
            var player = gameWorld.Player;
            Assert.That(player.IsGrounded, Is.True, "Player should start grounded");
            float startY = player.Position.Y;
            
            // Act - Simulate pressing jump
            inputProvider.SimulateJumpPress();
            gameWorld.Update(0.016f);
            inputProvider.SimulateJumpRelease();
            
            // Assert - Player should have jumped
            Assert.That(player.Velocity.Y, Is.GreaterThan(0), "Player should have upward velocity");
            Assert.That(player.IsJumping, Is.True);
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
            
            // Continue simulation until landing
            float maxHeight = player.Position.Y;
            int frameCount = 0;
            const int maxFrames = 300; // 5 seconds at 60fps
            
            while (!player.IsGrounded && frameCount < maxFrames)
            {
                gameWorld.Update(0.016f);
                if (player.Position.Y > maxHeight)
                    maxHeight = player.Position.Y;
                frameCount++;
            }
            
            // Assert final state
            Assert.That(player.IsGrounded, Is.True, "Player should land back on ground");
            Assert.That(player.IsJumping, Is.False, "Player should not be jumping after landing");
            Assert.That(maxHeight - startY, Is.GreaterThan(50f), "Jump should reach reasonable height");
            Assert.That(frameCount, Is.LessThan(maxFrames), "Jump should complete in reasonable time");
        }
        
        [Test]
        public void RapidJumpInputs_OnlyJumpsWhenGrounded()
        {
            // Arrange
            var player = gameWorld.Player;
            int jumpCount = 0;
            
            // Count actual jumps
            player.Jump(); // Direct call to establish baseline
            if (player.IsJumping) jumpCount++;
            
            // Reset
            player.Position = new Vector2(0, 0);
            player.Velocity = Vector2.Zero;
            player.IsGrounded = true;
            player.IsJumping = false;
            jumpCount = 0;
            
            // Act - Rapidly press jump multiple times
            for (int i = 0; i < 10; i++)
            {
                inputProvider.SimulateJumpPress();
                gameWorld.Update(0.016f);
                if (player.IsJumping && player.Velocity.Y > 0)
                    jumpCount++;
                inputProvider.SimulateJumpRelease();
                gameWorld.Update(0.016f);
            }
            
            // Assert
            Assert.That(jumpCount, Is.EqualTo(1), "Should only jump once while airborne");
        }
        
        [Test]
        public void Jump_WhileMoving_MaintainsHorizontalVelocity()
        {
            // Arrange
            var player = gameWorld.Player;
            inputProvider.SimulateRightPress();
            gameWorld.Update(0.1f); // Let player start moving
            float horizontalSpeed = player.Velocity.X;
            Assert.That(horizontalSpeed, Is.GreaterThan(0), "Player should be moving right");
            
            // Act - Jump while moving
            inputProvider.SimulateJumpPress();
            gameWorld.Update(0.016f);
            
            // Assert
            Assert.That(player.Velocity.X, Is.EqualTo(horizontalSpeed), "Horizontal velocity should be maintained");
            Assert.That(player.Velocity.Y, Is.GreaterThan(0), "Should also have jump velocity");
        }
        
        private class SimulatedInputProvider : IInputProvider
        {
            private bool jumpPressed;
            private bool leftPressed;
            private bool rightPressed;
            
            public bool IsLeftPressed => leftPressed;
            public bool IsRightPressed => rightPressed;
            public bool IsJumpPressed => jumpPressed;
            public bool IsAttackPressed => false;
            public bool IsUpPressed => false;
            public bool IsDownPressed => false;
            
            public void SimulateJumpPress() => jumpPressed = true;
            public void SimulateJumpRelease() => jumpPressed = false;
            public void SimulateLeftPress() => leftPressed = true;
            public void SimulateRightPress() => rightPressed = true;
            public void ReleaseAll() 
            {
                jumpPressed = leftPressed = rightPressed = false;
            }
        }
        
        private class SimpleMapLoader : IMapLoader
        {
            public MapData GetMap(int mapId)
            {
                return new MapData
                {
                    MapId = mapId,
                    Name = "Test Map",
                    Width = 2000,
                    Height = 1000,
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
                    },
                    MonsterSpawns = new System.Collections.Generic.List<MonsterSpawn>(),
                    Ladders = new System.Collections.Generic.List<LadderInfo>()
                };
            }
        }
    }
}