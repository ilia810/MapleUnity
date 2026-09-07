using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.Tests.GameLogic
{
    public class ItemInventoryRewriteTests
    {
        private sealed class Items : IItemDataProvider
        {
            public readonly Dictionary<int, ItemInfo> Values = new Dictionary<int, ItemInfo>();
            public ItemInfo GetItem(int id) => Values.TryGetValue(id, out var item) ? item : null;
            public bool ItemExists(int id) => Values.ContainsKey(id);
            public Dictionary<int, ItemInfo> GetAllItems() => new Dictionary<int, ItemInfo>(Values);
        }
        private Items data;
        private Player player;
        [SetUp] public void Setup() { data = new Items(); player = new Player(); player.SetItemData(data); }
        private ItemInfo Gear(int id, EquipSlot slot, StatType stat, int bonus)
        {
            var item = new ItemInfo { ItemId = id, Name = "Gear", Type = AssetItemType.Equip, EquipmentSlot = slot,
                Stats = new Dictionary<StatType, int> { [stat] = bonus } };
            data.Values[id] = item; player.Inventory.AddItem(id, 1); return item;
        }
        private ItemInfo Potion(int id = 2000001)
        {
            var item = new ItemInfo { ItemId = id, Name = "Recovery", Type = AssetItemType.Use, IsRecoveryConsumable = true, Hp = 150 };
            data.Values[id] = item; player.Inventory.AddItem(id, 2); return item;
        }

        [Test]
        public void RecoveryUsesDataAndDoesNotWasteItemsAtFullHealth()
        {
            Potion(); player.MaxHP = 300;
            player.TakeDamage(50); player.CurrentMP = 10;
            Assert.That(player.TryUseItem(2000001, out _), Is.True);
            Assert.That(player.CurrentHP, Is.EqualTo(200)); Assert.That(player.CurrentMP, Is.EqualTo(10));
            Assert.That(player.Inventory.GetItemCount(2000001), Is.EqualTo(1));
            Assert.That(player.TryUseItem(2000001, out _), Is.True); Assert.That(player.CurrentHP, Is.EqualTo(300));
            player.Inventory.AddItem(2000001, 1);
            Assert.That(player.TryUseItem(2000001, out _), Is.False); Assert.That(player.Inventory.GetItemCount(2000001), Is.EqualTo(1));
        }
        [Test]
        public void PercentageRecoveryUsesMaximumStatsAndConsumesExactlyOne()
        {
            var potion = Potion(); potion.Hp = 0; potion.HpRate = potion.MpRate = 50;
            player.MaxHP = 200; player.MaxMP = 100; player.SetHPMP(10, 20);
            Assert.That(player.UseItem(potion.ItemId), Is.True);
            Assert.That(player.CurrentHP, Is.EqualTo(110)); Assert.That(player.CurrentMP, Is.EqualTo(70));
            Assert.That(player.Inventory.GetItemCount(potion.ItemId), Is.EqualTo(1));
        }
        [Test]
        public void UnknownUnsupportedAndDeadUseDoNotConsumeItems()
        {
            var potion = Potion(); potion.IsRecoveryConsumable = false; player.TakeDamage(20);
            Assert.That(player.UseItem(potion.ItemId), Is.False);
            player.Inventory.AddItem(1234567, 1); Assert.That(player.UseItem(1234567), Is.False);
            potion.IsRecoveryConsumable = true; player.TakeDamage(1000); Assert.That(player.UseItem(potion.ItemId), Is.False);
            Assert.That(player.Inventory.GetItemCount(potion.ItemId), Is.EqualTo(2));
        }
        [Test]
        public void EquipSwapAndUnequipPreserveItemsAndNeverAccumulateBonuses()
        {
            Gear(1040002, EquipSlot.Top, StatType.WeaponDefense, 3); Gear(1040003, EquipSlot.Top, StatType.WeaponDefense, 7);
            for (int i = 0; i < 5; i++)
            {
                Assert.That(player.TryEquipItem(1040002, out _), Is.True); Assert.That(player.WeaponDefense, Is.EqualTo(13));
                Assert.That(player.TryEquipItem(1040003, out _), Is.True); Assert.That(player.WeaponDefense, Is.EqualTo(17));
                Assert.That(player.TryUnequipItem(EquipSlot.Top, out _), Is.True); Assert.That(player.WeaponDefense, Is.EqualTo(10));
            }
            Assert.That(player.Inventory.GetItemCount(1040002), Is.EqualTo(1)); Assert.That(player.Inventory.GetItemCount(1040003), Is.EqualTo(1));
        }
        [Test]
        public void RequirementsExcludeTheReplacedItemsBonus()
        {
            Gear(1040002, EquipSlot.Top, StatType.STR, 10);
            var replacement = Gear(1040003, EquipSlot.Top, StatType.STR, 15); replacement.RequiredStr = 20;
            player.TryEquipItem(1040002, out _);
            Assert.That(player.TryEquipItem(1040003, out _), Is.False);
            Assert.That(player.GetEquippedItems()[EquipSlot.Top], Is.EqualTo(1040002));
            Assert.That(player.Inventory.GetItemCount(1040003), Is.EqualTo(1));
        }
        [TestCase(10, 0, 2)] [TestCase(0, 1, 2)] [TestCase(0, 0, 1)]
        public void LevelJobAndGenderFailuresLeaveInventoryUnchanged(int level, int job, int gender)
        {
            var item = Gear(1040002, EquipSlot.Top, StatType.WeaponDefense, 3);
            item.RequiredLevel = level; item.RequiredJobMask = job; item.Gender = gender;
            Assert.That(player.TryEquipItem(item.ItemId, out _), Is.False); Assert.That(player.GetEquippedItems(), Is.Empty);
            Assert.That(player.Inventory.GetItemCount(item.ItemId), Is.EqualTo(1));
        }
        [Test]
        public void OverallAndSeparateClothingExchangeBothCoveredSlots()
        {
            Gear(1040002, EquipSlot.Top, StatType.WeaponDefense, 3); Gear(1060002, EquipSlot.Bottom, StatType.WeaponDefense, 2);
            var overall = Gear(1050000, EquipSlot.Top, StatType.WeaponDefense, 7); overall.IsOverall = true;
            player.TryEquipItem(1040002, out _); player.TryEquipItem(1060002, out _);
            Assert.That(player.TryEquipItem(1050000, out _), Is.True);
            Assert.That(player.GetEquippedItems().Count, Is.EqualTo(1)); Assert.That(player.WeaponDefense, Is.EqualTo(17));
            Assert.That(player.Inventory.GetItemCount(1040002), Is.EqualTo(1)); Assert.That(player.Inventory.GetItemCount(1060002), Is.EqualTo(1));
            Assert.That(player.TryEquipItem(1060002, out _), Is.True);
            Assert.That(player.GetEquippedItems().ContainsKey(EquipSlot.Top), Is.False); Assert.That(player.Inventory.GetItemCount(1050000), Is.EqualTo(1));
        }
        [Test]
        public void GearChangesClampHpWithoutHealingAndPreserveBaseStatEdits()
        {
            Gear(1040002, EquipSlot.Top, StatType.MaxHP, 50);
            Assert.That(player.TryEquipItem(1040002, out _), Is.True); Assert.That(player.MaxHP, Is.EqualTo(150));
            Assert.That(player.CurrentHP, Is.EqualTo(100)); player.Heal(50);
            player.MaxHP = 200; Assert.That(player.MaxHP, Is.EqualTo(250));
            player.TryUnequipItem(EquipSlot.Top, out _); Assert.That(player.MaxHP, Is.EqualTo(200));
            Assert.That(player.CurrentHP, Is.EqualTo(150));
        }
        [Test]
        public void WeaponBonusAffectsAttackAndDeadEquipmentActionsAreRejected()
        {
            Gear(1302000, EquipSlot.Weapon, StatType.WeaponAttack, 17);
            player.TryEquipItem(1302000, out _); Assert.That(player.GetBaseDamage(), Is.EqualTo(37));
            player.TakeDamage(1000); Assert.That(player.TryUnequipItem(EquipSlot.Weapon, out _), Is.False);
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon], Is.EqualTo(1302000));
        }
    }
}
