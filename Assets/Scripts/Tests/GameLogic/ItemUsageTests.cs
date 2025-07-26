using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class ItemUsageTests
    {
        private Player player;

        [SetUp]
        public void Setup()
        {
            player = new Player();
        }

        [Test]
        public void UseHealingPotion_RestoresHP()
        {
            // Arrange
            player.TakeDamage(50); // Reduce HP
            int initialHP = player.CurrentHP;
            
            player.Inventory.AddItem(2000000, 1); // Red Potion

            // Act
            bool used = player.UseItem(2000000);

            // Assert
            Assert.That(used, Is.True);
            Assert.That(player.CurrentHP, Is.EqualTo(initialHP + 50)); // Red potion heals 50 HP
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(0));
        }

        [Test]
        public void UseHealingPotion_DoesNotExceedMaxHP()
        {
            // Arrange
            player.TakeDamage(10); // Small damage
            player.Inventory.AddItem(2000000, 1); // Red Potion

            // Act
            player.UseItem(2000000);

            // Assert
            Assert.That(player.CurrentHP, Is.EqualTo(player.MaxHP));
        }

        [Test]
        public void UseItem_NotInInventory_ReturnsFalse()
        {
            // Act
            bool used = player.UseItem(2000000);

            // Assert
            Assert.That(used, Is.False);
            Assert.That(player.CurrentHP, Is.EqualTo(player.MaxHP));
        }

        [Test]
        public void UseManaPotion_RestoresMP()
        {
            // Arrange
            player.UseMana(30);
            int initialMP = player.CurrentMP;
            
            player.Inventory.AddItem(2000001, 1); // Orange Potion (MP)

            // Act
            bool used = player.UseItem(2000001);

            // Assert
            Assert.That(used, Is.True);
            Assert.That(player.CurrentMP, Is.EqualTo(initialMP + 30));
            Assert.That(player.Inventory.GetItemCount(2000001), Is.EqualTo(0));
        }

        [Test]
        public void UseUnknownItem_ReturnsFalse()
        {
            // Arrange
            player.Inventory.AddItem(9999999, 1); // Unknown item

            // Act
            bool used = player.UseItem(9999999);

            // Assert
            Assert.That(used, Is.False);
            Assert.That(player.Inventory.GetItemCount(9999999), Is.EqualTo(1)); // Item not consumed
        }
    }
}