using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MapleClient.GameView;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic;
using System.Collections;
using System.Collections.Generic;

namespace MapleClient.Tests.GameView
{
    [TestFixture]
    public class SimplePlayerControllerTests
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
                    Platforms = new List<Platform>
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
            
            // Connect controller to GameLogic
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
        public void SimplePlayerController_CreatesAsTriggerCollider()
        {
            // Assert
            var collider = playerObject.GetComponent<BoxCollider2D>();
            Assert.That(collider, Is.Not.Null, "Should have BoxCollider2D");
            Assert.That(collider.isTrigger, Is.True, "Collider should be a trigger");
        }
        
        [Test]
        public void SimplePlayerController_HasNoRigidbody()
        {
            // Assert
            var rigidbody = playerObject.GetComponent<Rigidbody2D>();
            Assert.That(rigidbody, Is.Null, "Should not have Rigidbody2D component");
        }
        
        [Test]
        public void SimplePlayerController_SyncsPositionFromGameLogic()
        {
            // Arrange
            gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(5f, 10f);
            
            // Act
            // controller.Update(); // Unity's Update is private and called automatically
            
            // Assert
            Assert.That(playerObject.transform.position.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(playerObject.transform.position.y, Is.EqualTo(10f).Within(0.01f));
            Assert.That(playerObject.transform.position.z, Is.EqualTo(0f));
        }
        
        [Test]
        public void SimplePlayerController_UpdatesFacingDirection()
        {
            // Arrange - face right
            gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(0, 0);
            inputProvider.IsRightPressed = true;
            gameWorld.Update(0.016f); // Simulate one frame
            
            // Act
            // controller.Update(); // Unity's Update is private and called automatically
            
            // Assert
            Assert.That(playerObject.transform.localScale.x, Is.EqualTo(1f), "Should face right");
            
            // Arrange - face left
            inputProvider.IsRightPressed = false;
            inputProvider.IsLeftPressed = true;
            gameWorld.Update(0.016f);
            
            // Act
            // controller.Update(); // Unity's Update is private and called automatically
            
            // Assert
            Assert.That(playerObject.transform.localScale.x, Is.EqualTo(-1f), "Should face left");
        }
        
        [UnityTest]
        public IEnumerator SimplePlayerController_PhysicsHandledByGameLogic()
        {
            // Arrange - start player in air
            gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(0, 5);
            gameLogicPlayer.IsGrounded = false;
            
            // Act - simulate multiple physics frames
            float startY = gameLogicPlayer.Position.Y;
            for (int i = 0; i < 60; i++) // 1 second at 60fps
            {
                gameWorld.Update(1f/60f);
                // controller.Update(); // Unity's Update is private
                yield return null;
            }
            
            // Assert - player should have fallen due to GameLogic gravity
            Assert.That(gameLogicPlayer.Position.Y, Is.LessThan(startY), "Player should fall due to gravity");
            Assert.That(gameLogicPlayer.IsGrounded, Is.True, "Player should be grounded after falling");
            
            // Visual should match GameLogic position
            Assert.That(playerObject.transform.position.y, Is.EqualTo(gameLogicPlayer.Position.Y).Within(0.01f));
        }
        
        [Test]
        public void SimplePlayerController_InputRoutedThroughGameWorld()
        {
            // Arrange
            gameLogicPlayer.Position = new MapleClient.GameLogic.Vector2(0, 0.3f);
            gameLogicPlayer.IsGrounded = true;
            float startX = gameLogicPlayer.Position.X;
            
            // Act - simulate right movement
            inputProvider.IsRightPressed = true;
            for (int i = 0; i < 10; i++)
            {
                gameWorld.Update(0.016f);
                // controller.Update(); // Unity's Update is private
            }
            
            // Assert
            Assert.That(gameLogicPlayer.Position.X, Is.GreaterThan(startX), "Player should move right");
            Assert.That(playerObject.transform.position.x, Is.EqualTo(gameLogicPlayer.Position.X).Within(0.01f), 
                "Visual position should match GameLogic position");
        }
    }
}