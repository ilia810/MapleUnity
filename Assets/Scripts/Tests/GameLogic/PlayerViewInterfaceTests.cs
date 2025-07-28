using NUnit.Framework;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System;
using System.Collections.Generic;

namespace MapleClient.Tests.GameLogic
{
    [TestFixture]
    public class PlayerViewInterfaceTests
    {
        private Player player;
        private TestPlayerViewListener viewListener;
        
        private class TestPlayerViewListener : IPlayerViewListener
        {
            public int PositionUpdateCount { get; private set; }
            public int StateChangeCount { get; private set; }
            public int VelocityUpdateCount { get; private set; }
            public int GroundedStateChangeCount { get; private set; }
            public int AnimationEventCount { get; private set; }
            
            public Vector2 LastPosition { get; private set; }
            public PlayerState LastState { get; private set; }
            public Vector2 LastVelocity { get; private set; }
            public bool LastGroundedState { get; private set; }
            public PlayerAnimationEvent LastAnimationEvent { get; private set; }
            
            public void OnPositionChanged(Vector2 position)
            {
                PositionUpdateCount++;
                LastPosition = position;
            }
            
            public void OnStateChanged(PlayerState state)
            {
                StateChangeCount++;
                LastState = state;
            }
            
            public void OnVelocityChanged(Vector2 velocity)
            {
                VelocityUpdateCount++;
                LastVelocity = velocity;
            }
            
            public void OnGroundedStateChanged(bool isGrounded)
            {
                GroundedStateChangeCount++;
                LastGroundedState = isGrounded;
            }
            
            public void OnAnimationEvent(PlayerAnimationEvent animEvent)
            {
                AnimationEventCount++;
                LastAnimationEvent = animEvent;
            }
            
            public void OnMovementModifiersChanged(List<IMovementModifier> modifiers)
            {
                // For testing, we don't need to track this
            }
        }
        
        [SetUp]
        public void Setup()
        {
            player = new Player();
            viewListener = new TestPlayerViewListener();
        }
        
        [Test]
        public void Player_SupportsViewListener()
        {
            // Act
            player.AddViewListener(viewListener);
            
            // Assert
            Assert.That(player.HasViewListeners, Is.True);
        }
        
        [Test]
        public void Player_NotifiesPositionChanges()
        {
            // Arrange
            player.AddViewListener(viewListener);
            var newPosition = new Vector2(10f, 20f);
            
            // Act
            player.Position = newPosition;
            
            // Assert
            Assert.That(viewListener.PositionUpdateCount, Is.EqualTo(1));
            Assert.That(viewListener.LastPosition, Is.EqualTo(newPosition));
        }
        
        [Test]
        public void Player_NotifiesStateChanges()
        {
            // Arrange
            player.AddViewListener(viewListener);
            
            // Act
            player.Jump(); // Should change state to Jumping
            
            // Assert
            Assert.That(viewListener.StateChangeCount, Is.GreaterThan(0));
            Assert.That(viewListener.LastState, Is.EqualTo(PlayerState.Jumping));
        }
        
        [Test]
        public void Player_NotifiesVelocityChanges()
        {
            // Arrange
            player.AddViewListener(viewListener);
            
            // Act
            player.MoveRight(true);
            
            // Assert
            Assert.That(viewListener.VelocityUpdateCount, Is.GreaterThan(0));
            Assert.That(viewListener.LastVelocity.X, Is.GreaterThan(0));
        }
        
        [Test]
        public void Player_NotifiesGroundedStateChanges()
        {
            // Arrange
            player.AddViewListener(viewListener);
            player.Position = new Vector2(0, 10); // Start in air
            
            // Act - simulate landing
            var mapData = new MapData
            {
                Platforms = new System.Collections.Generic.List<Platform>
                {
                    new Platform { X1 = -100, Y1 = 0, X2 = 100, Y2 = 0, Type = PlatformType.Normal }
                }
            };
            
            // Simulate falling and landing
            for (int i = 0; i < 60; i++)
            {
                player.UpdatePhysics(0.016f, mapData);
            }
            
            // Assert
            Assert.That(viewListener.GroundedStateChangeCount, Is.GreaterThan(0));
            Assert.That(viewListener.LastGroundedState, Is.True);
        }
        
        [Test]
        public void Player_CanRemoveViewListener()
        {
            // Arrange
            player.AddViewListener(viewListener);
            
            // Act
            player.RemoveViewListener(viewListener);
            player.Position = new Vector2(100, 100);
            
            // Assert
            Assert.That(viewListener.PositionUpdateCount, Is.EqualTo(0));
            Assert.That(player.HasViewListeners, Is.False);
        }
        
        [Test]
        public void Player_SupportsMultipleViewListeners()
        {
            // Arrange
            var listener2 = new TestPlayerViewListener();
            player.AddViewListener(viewListener);
            player.AddViewListener(listener2);
            
            // Act
            player.Position = new Vector2(50, 50);
            
            // Assert
            Assert.That(viewListener.PositionUpdateCount, Is.EqualTo(1));
            Assert.That(listener2.PositionUpdateCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Player_NotifiesAnimationEvents()
        {
            // Arrange
            player.AddViewListener(viewListener);
            player.IsGrounded = true;
            
            // Act
            player.Jump();
            
            // Assert
            Assert.That(viewListener.AnimationEventCount, Is.GreaterThan(0));
            Assert.That(viewListener.LastAnimationEvent, Is.EqualTo(PlayerAnimationEvent.Jump));
        }
    }
}