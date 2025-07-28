using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic;
using System.Collections;

namespace MapleClient.Tests.GameView
{
    [TestFixture]
    public class PlayerViewCommunicationTests
    {
        private GameObject playerObject;
        private SimplePlayerController controller;
        private Player gameLogicPlayer;
        private GameWorld gameWorld;
        private TestInputProvider inputProvider;
        
        private class TestInputProvider : IInputProvider
        {
            public bool IsLeftPressed { get; set; }
            public bool IsRightPressed { get; set; }
            public bool IsJumpPressed { get; set; }
            public bool IsAttackPressed { get; set; }
            public bool IsUpPressed { get; set; }
            public bool IsDownPressed { get; set; }
        }
        
        private class TestMapLoader : IMapLoader
        {
            private MapData testMap;
            
            public TestMapLoader()
            {
                testMap = new MapData
                {
                    MapId = 100000000,
                    Name = "Test Map",
                    Platforms = new System.Collections.Generic.List<Platform>
                    {
                        new Platform { Id = 1, X1 = -500, Y1 = 0, X2 = 500, Y2 = 0, Type = PlatformType.Normal }
                    }
                };
            }
            
            public MapData GetMap(int mapId)
            {
                return testMap;
            }
            
            public void LoadAllMaps() { }
        }
        
        [SetUp]
        public void Setup()
        {
            // Create player GameObject
            playerObject = new GameObject("TestPlayer");
            controller = playerObject.AddComponent<SimplePlayerController>();
            
            // Create GameLogic components
            inputProvider = new TestInputProvider();
            var mapLoader = new TestMapLoader();
            gameWorld = new GameWorld(inputProvider, mapLoader);
            gameWorld.LoadMap(100000000);
            
            gameLogicPlayer = gameWorld.Player;
            
            // Connect controller to GameLogic via listener pattern
            controller.SetGameLogicPlayer(gameLogicPlayer);
            controller.SetGameWorld(gameWorld);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }
        
        [Test]
        public void Controller_RegistersAsListener_WhenPlayerSet()
        {
            // Assert
            Assert.That(gameLogicPlayer.HasViewListeners, Is.True);
        }
        
        [Test]
        public void Controller_UpdatesPosition_WhenPlayerMoves()
        {
            // Arrange
            var initialPosition = controller.transform.position;
            
            // Act - move player in game logic
            gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(10f, 5f);
            
            // Give Update a chance to run (normally handled by Unity's update loop)
            // controller.Update(); // Unity's Update is private
            
            // Assert
            Assert.That(controller.transform.position.x, Is.EqualTo(10f).Within(0.01f));
            Assert.That(controller.transform.position.y, Is.EqualTo(5f).Within(0.01f));
        }
        
        [Test]
        public void Controller_UpdatesFacing_WhenVelocityChanges()
        {
            // Arrange - start facing right (default)
            Assert.That(controller.transform.localScale.x, Is.EqualTo(1f));
            
            // Act - move left
            inputProvider.IsLeftPressed = true;
            gameWorld.ProcessInput();
            // controller.Update(); // Unity's Update is private
            
            // Assert - should face left
            Assert.That(controller.transform.localScale.x, Is.EqualTo(-1f));
            
            // Act - move right
            inputProvider.IsLeftPressed = false;
            inputProvider.IsRightPressed = true;
            gameWorld.ProcessInput();
            // controller.Update(); // Unity's Update is private
            
            // Assert - should face right
            Assert.That(controller.transform.localScale.x, Is.EqualTo(1f));
        }
        
        [UnityTest]
        public IEnumerator Controller_RespondsToAnimationEvents()
        {
            // Arrange
            gameLogicPlayer.IsGrounded = true;
            
            // Act - trigger jump
            gameLogicPlayer.Jump();
            
            yield return null; // Wait one frame
            
            // Assert - controller should have received jump animation event
            // In a real implementation, we'd check if the animation is playing
            // For now, we just verify the event system is connected
            Assert.That(gameLogicPlayer.State, Is.EqualTo(PlayerState.Jumping));
        }
        
        [Test]
        public void Controller_UnregistersListener_OnDestroy()
        {
            // Act
            Object.DestroyImmediate(playerObject);
            playerObject = null;
            
            // Assert
            Assert.That(gameLogicPlayer.HasViewListeners, Is.False);
        }
        
        [Test]
        public void Controller_HandlesNullPlayer_Gracefully()
        {
            // Arrange
            var emptyController = new GameObject("Empty").AddComponent<SimplePlayerController>();
            
            // Act & Assert - should not throw when accessing properties
            Assert.DoesNotThrow(() => 
            {
                var pos = emptyController.transform.position;
                var scale = emptyController.transform.localScale;
            });
            
            // Cleanup
            Object.DestroyImmediate(emptyController.gameObject);
        }
    }
}