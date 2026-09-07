using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using AssetItemType = MapleClient.GameLogic.Interfaces.ItemType;

namespace MapleClient.Tests.GameLogic
{
    public class WeaponRewriteTests
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
        private ItemInfo Add(int id, EquipSlot slot, bool twoHanded = false)
        {
            var item = new ItemInfo { ItemId = id, Name = "Test gear", Type = AssetItemType.Equip, EquipmentSlot = slot,
                IsTwoHanded = twoHanded, Stats = new Dictionary<StatType, int>() };
            if (slot == EquipSlot.Weapon)
            {
                item.Weapon = new WeaponProfile { AttackType = 1, AttackSpeed = 4, IsTwoHanded = twoHanded };
                foreach (var state in WeaponProfile.AttackStances(1)) item.Weapon.FrameDelays[state] = new[] { 350, 450 };
            }
            data.Values[id] = item; player.Inventory.AddItem(id, 1); return item;
        }

        [Test]
        public void TwoHandedAndShieldSwapsReturnAllConflictingItemsWithoutDuplicatingStats()
        {
            Add(1302000, EquipSlot.Weapon).Stats[StatType.WeaponAttack] = 17;
            Add(1092003, EquipSlot.Shield).Stats[StatType.WeaponDefense] = 5;
            Add(1402009, EquipSlot.Weapon, true).Stats[StatType.WeaponAttack] = 20;
            for (int i = 0; i < 3; i++)
            {
                Assert.That(player.TryEquipItem(1302000, out _), Is.True);
                Assert.That(player.TryEquipItem(1092003, out _), Is.True);
                Assert.That(player.WeaponDefense, Is.EqualTo(15));
                Assert.That(player.TryEquipItem(1402009, out _), Is.True);
                Assert.That(player.GetEquippedItems().ContainsKey(EquipSlot.Shield), Is.False);
                Assert.That(player.Inventory.GetItemCount(1302000), Is.EqualTo(1));
                Assert.That(player.Inventory.GetItemCount(1092003), Is.EqualTo(1));
                Assert.That(player.WeaponAttack, Is.EqualTo(20)); Assert.That(player.WeaponDefense, Is.EqualTo(10));
                Assert.That(player.TryEquipItem(1092003, out _), Is.True);
                Assert.That(player.GetEquippedItems().ContainsKey(EquipSlot.Weapon), Is.False);
                Assert.That(player.Inventory.GetItemCount(1402009), Is.EqualTo(1));
                Assert.That(player.WeaponAttack, Is.EqualTo(0));
                Assert.That(player.TryUnequipItem(EquipSlot.Shield, out _), Is.True);
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void RequirementsCannotBorrowStatsFromConflictingShieldOrWeapon(bool equipWeapon)
        {
            var shield = Add(1092003, EquipSlot.Shield);
            var weapon = Add(1402009, EquipSlot.Weapon, true);
            var retained = equipWeapon ? shield : weapon;
            var candidate = equipWeapon ? weapon : shield;
            retained.Stats[StatType.STR] = 10;
            Assert.That(player.TryEquipItem(retained.ItemId, out _), Is.True);
            candidate.RequiredStr = 20;
            Assert.That(player.TryEquipItem(candidate.ItemId, out _), Is.False);
            Assert.That(player.GetEquippedItems()[retained.EquipmentSlot.Value], Is.EqualTo(retained.ItemId));
            Assert.That(player.Inventory.GetItemCount(candidate.ItemId), Is.EqualTo(1));
        }

        [TestCase(1312000)] [TestCase(1322000)] [TestCase(1332000)] [TestCase(1372000)] [TestCase(1382000)]
        [TestCase(1402000)] [TestCase(1412000)] [TestCase(1422000)] [TestCase(1432000)] [TestCase(1442000)]
        [TestCase(1452000)] [TestCase(1462000)] [TestCase(1472000)] [TestCase(1492000)]
        public void SupportedWeaponFamiliesCanBeEquippedWhenRequirementsAreMet(int id)
        {
            Add(id, EquipSlot.Weapon, WeaponProfile.UsesTwoHands(id));
            Assert.That(player.TryEquipItem(id, out _), Is.True);
        }
        [TestCase(1482000)]
        public void UnportedKnuckleWeaponsStayInTheBag(int id)
        {
            Add(id, EquipSlot.Weapon);
            Assert.That(player.TryEquipItem(id, out _), Is.False);
            Assert.That(player.Inventory.GetItemCount(id), Is.EqualTo(1)); Assert.That(player.GetEquippedItems(), Is.Empty);
        }

        [Test]
        public void AuthoredSwingControlsCombatReadinessAndEquipmentLock()
        {
            Add(1302000, EquipSlot.Weapon); Add(1402009, EquipSlot.Weapon, true);
            Assert.That(player.TryEquipItem(1302000, out _), Is.True);
            var combat = new Combat(); combat.PerformBasicAttack(player, new List<Monster>(), 1);
            var swing = player.BasicAttack;
            Assert.That(combat.CanPlayerAttack(player), Is.False);
            Assert.That(player.TryEquipItem(1402009, out _), Is.False);
            Assert.That(player.TryUnequipItem(EquipSlot.Weapon, out _), Is.False);
            for (int tick = 0; tick < 34; tick++) combat.Update(.008f);
            Assert.That(swing.Frame, Is.EqualTo(0)); combat.Update(.008f); Assert.That(swing.Frame, Is.EqualTo(1));
            for (int tick = 35; tick < 79; tick++) combat.Update(.008f);
            combat.PerformBasicAttack(player, new List<Monster>(), 1);
            Assert.That(player.BasicAttack, Is.SameAs(swing)); Assert.That(combat.CanPlayerAttack(player), Is.False);
            combat.Update(.008f);
            Assert.That(combat.CanPlayerAttack(player), Is.True); Assert.That(player.TryEquipItem(1402009, out _), Is.True);
        }

        [Test]
        public void SimulationFrameBatchingDoesNotChangeSwingAndMapResetCancelsIt()
        {
            var single = new BasicAttackMotion(CharacterState.SwingT1, new[] { 300, 150, 350 }, 7);
            var batch = new BasicAttackMotion(CharacterState.SwingT1, new[] { 300, 150, 350 }, 7);
            for (int tick = 0; tick < 114; tick++) single.Advance(.008f);
            batch.Advance(.912f);
            Assert.That(single.Frame, Is.EqualTo(2)); Assert.That(batch.Frame, Is.EqualTo(single.Frame));
            Assert.That(single.IsComplete, Is.False); Assert.That(batch.IsComplete, Is.False);
            single.Advance(.008f); batch.Advance(.008f);
            Assert.That(single.IsComplete && batch.IsComplete, Is.True);
            player.PlayBasicAttackAnimation(); player.ResetMovementForMap(); Assert.That(player.IsBasicAttacking, Is.False);
            player.PlayBasicAttackAnimation(); player.TakeDamage(1000); Assert.That(player.IsBasicAttacking, Is.False);
        }
    }
}
