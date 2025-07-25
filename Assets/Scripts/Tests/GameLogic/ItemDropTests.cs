using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Tests.Fakes;
using System.Linq;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class ItemDropTests
    {
        private GameWorld gameWorld;
        private FakeMapLoader mapLoader;
        private Player player;

        [SetUp]
        public void Setup()
        {
            mapLoader = new FakeMapLoader();
            var testMap = new MapData { MapId = 100000000 };
            mapLoader.AddMap(100000000, testMap);
            
            gameWorld = new GameWorld(mapLoader);
            gameWorld.LoadMap(100000000);
            player = gameWorld.Player;
            player.Position = new Vector2(0, 0);
        }

        [Test]
        public void DroppedItem_UpdatesLifetime()
        {
            // Arrange
            var droppedItem = new DroppedItem(2000000, 1, new Vector2(100, 0));
            float initialLifetime = droppedItem.LifeTime;

            // Act
            droppedItem.Update(1.0f);

            // Assert
            Assert.That(droppedItem.LifeTime, Is.EqualTo(initialLifetime - 1.0f));
        }

        [Test]
        public void DroppedItem_ExpiresAfterLifetime()
        {
            // Arrange
            var droppedItem = new DroppedItem(2000000, 1, new Vector2(100, 0));
            droppedItem.LifeTime = 1.0f;

            // Act
            droppedItem.Update(1.5f);

            // Assert
            Assert.That(droppedItem.IsExpired, Is.True);
        }

        [Test]
        public void Monster_DropsItemOnDeath()
        {
            // Arrange
            var template = new MonsterTemplate
            {
                MonsterId = 100100,
                Name = "Snail",
                MaxHP = 10,
                DropTable = new System.Collections.Generic.List<DropInfo>
                {
                    new DropInfo { ItemId = 2000000, Quantity = 1, DropRate = 1.0f } // 100% drop rate
                }
            };
            
            var monster = new Monster(template, new Vector2(100, 0));
            int droppedItemCount = 0;
            monster.ItemDropped += (itemId, quantity, position) => droppedItemCount++;

            // Act
            monster.TakeDamage(20); // Kill it

            // Assert
            Assert.That(monster.IsDead, Is.True);
            Assert.That(droppedItemCount, Is.EqualTo(1));
        }

        [Test]
        public void GameWorld_AddsDroppedItemWhenMonsterDies()
        {
            // Arrange
            gameWorld.SpawnMonsterForTesting(100100, new Vector2(100, 0));
            var monster = gameWorld.Monsters.First();

            // Act
            monster.TakeDamage(999); // Kill it

            // Assert
            Assert.That(gameWorld.DroppedItems.Count, Is.GreaterThan(0));
            var drop = gameWorld.DroppedItems.First();
            Assert.That(drop.Position.X, Is.EqualTo(100));
        }

        [Test]
        public void Player_PicksUpItemWhenNearby()
        {
            // Arrange
            player.Position = new Vector2(90, 0); // Close to drop
            gameWorld.AddDroppedItem(2000000, 5, new Vector2(100, 0));
            
            int initialCount = player.Inventory.GetItemCount(2000000);

            // Act
            gameWorld.Update(0.1f); // Should pick up

            // Assert
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(initialCount + 5));
            Assert.That(gameWorld.DroppedItems.Count, Is.EqualTo(0));
        }

        [Test]
        public void Player_DoesNotPickUpItemWhenFarAway()
        {
            // Arrange
            player.Position = new Vector2(0, 0);
            gameWorld.AddDroppedItem(2000000, 5, new Vector2(500, 0)); // Far away
            
            int initialCount = player.Inventory.GetItemCount(2000000);

            // Act
            gameWorld.Update(0.1f);

            // Assert
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(initialCount));
            Assert.That(gameWorld.DroppedItems.Count, Is.EqualTo(1));
        }

        [Test]
        public void GameWorld_RemovesExpiredItems()
        {
            // Arrange
            gameWorld.AddDroppedItem(2000000, 1, new Vector2(100, 0));
            var drop = gameWorld.DroppedItems.First();
            drop.LifeTime = 0.5f;

            // Act
            gameWorld.Update(1.0f); // Should expire

            // Assert
            Assert.That(gameWorld.DroppedItems.Count, Is.EqualTo(0));
        }

        [Test]
        public void ItemPickup_RaisesEvent()
        {
            // Arrange
            player.Position = new Vector2(100, 0);
            gameWorld.AddDroppedItem(2000000, 1, new Vector2(100, 0));
            
            int eventItemId = 0;
            int eventQuantity = 0;
            gameWorld.ItemPickedUp += (itemId, quantity) =>
            {
                eventItemId = itemId;
                eventQuantity = quantity;
            };

            // Act
            gameWorld.Update(0.1f);

            // Assert
            Assert.That(eventItemId, Is.EqualTo(2000000));
            Assert.That(eventQuantity, Is.EqualTo(1));
        }
    }
}