using NUnit.Framework;
using MapleClient.GameLogic.Core;
using System.Collections.Generic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class InventoryTests
    {
        private Inventory inventory;

        [SetUp]
        public void Setup()
        {
            inventory = new Inventory();
        }

        [Test]
        public void AddItem_NewItem_AddsToInventory()
        {
            // Arrange
            int itemId = 2000000; // Red Potion
            int quantity = 5;

            // Act
            inventory.AddItem(itemId, quantity);

            // Assert
            Assert.That(inventory.GetItemCount(itemId), Is.EqualTo(5));
        }

        [Test]
        public void AddItem_ExistingItem_IncreasesQuantity()
        {
            // Arrange
            int itemId = 2000000;
            inventory.AddItem(itemId, 3);

            // Act
            inventory.AddItem(itemId, 2);

            // Assert
            Assert.That(inventory.GetItemCount(itemId), Is.EqualTo(5));
        }

        [Test]
        public void RemoveItem_SufficientQuantity_RemovesFromInventory()
        {
            // Arrange
            int itemId = 2000000;
            inventory.AddItem(itemId, 10);

            // Act
            bool removed = inventory.RemoveItem(itemId, 3);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(inventory.GetItemCount(itemId), Is.EqualTo(7));
        }

        [Test]
        public void RemoveItem_InsufficientQuantity_ReturnsFalse()
        {
            // Arrange
            int itemId = 2000000;
            inventory.AddItem(itemId, 3);

            // Act
            bool removed = inventory.RemoveItem(itemId, 5);

            // Assert
            Assert.That(removed, Is.False);
            Assert.That(inventory.GetItemCount(itemId), Is.EqualTo(3));
        }

        [Test]
        public void RemoveItem_ExactQuantity_RemovesItemCompletely()
        {
            // Arrange
            int itemId = 2000000;
            inventory.AddItem(itemId, 5);

            // Act
            bool removed = inventory.RemoveItem(itemId, 5);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(inventory.HasItem(itemId), Is.False);
            Assert.That(inventory.GetItemCount(itemId), Is.EqualTo(0));
        }

        [Test]
        public void HasItem_ItemExists_ReturnsTrue()
        {
            // Arrange
            int itemId = 2000000;
            inventory.AddItem(itemId, 1);

            // Act & Assert
            Assert.That(inventory.HasItem(itemId), Is.True);
        }

        [Test]
        public void HasItem_ItemDoesNotExist_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(inventory.HasItem(2000000), Is.False);
        }

        [Test]
        public void GetAllItems_ReturnsAllItemsInInventory()
        {
            // Arrange
            inventory.AddItem(2000000, 5); // Red Potion
            inventory.AddItem(2000001, 3); // Orange Potion
            inventory.AddItem(2040002, 1); // 10% helmet for DEF

            // Act
            var items = inventory.GetAllItems();

            // Assert
            Assert.That(items.Count, Is.EqualTo(3));
            Assert.That(items.ContainsKey(2000000), Is.True);
            Assert.That(items[2000000], Is.EqualTo(5));
            Assert.That(items[2000001], Is.EqualTo(3));
            Assert.That(items[2040002], Is.EqualTo(1));
        }

        [Test]
        public void Inventory_RaisesItemAddedEvent()
        {
            // Arrange
            int eventItemId = 0;
            int eventQuantity = 0;
            inventory.ItemAdded += (itemId, quantity) =>
            {
                eventItemId = itemId;
                eventQuantity = quantity;
            };

            // Act
            inventory.AddItem(2000000, 5);

            // Assert
            Assert.That(eventItemId, Is.EqualTo(2000000));
            Assert.That(eventQuantity, Is.EqualTo(5));
        }

        [Test]
        public void Inventory_RaisesItemRemovedEvent()
        {
            // Arrange
            inventory.AddItem(2000000, 10);
            int eventItemId = 0;
            int eventQuantity = 0;
            inventory.ItemRemoved += (itemId, quantity) =>
            {
                eventItemId = itemId;
                eventQuantity = quantity;
            };

            // Act
            inventory.RemoveItem(2000000, 3);

            // Assert
            Assert.That(eventItemId, Is.EqualTo(2000000));
            Assert.That(eventQuantity, Is.EqualTo(3));
        }
    }
}