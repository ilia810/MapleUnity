using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class CombatBuffAssetTests
    {
        [TestCase(2002000, "Dexterity Potion", BuffType.Avoidability, 5, 180000)]
        [TestCase(2002001, "Speed Potion", BuffType.Speed, 8, 180000)]
        [TestCase(2002002, "Magic Potion", BuffType.MagicAttack, 5, 180000)]
        [TestCase(2002003, "Wizard Potion", BuffType.MagicAttack, 10, 180000)]
        [TestCase(2002004, "Warrior Potion", BuffType.WeaponAttack, 5, 180000)]
        [TestCase(2002005, "Sniper Potion", BuffType.Accuracy, 5, 300000)]
        public void RealBuffItemsUseSourceValuesDurationAndIcons(int id, string name, BuffType type, int amount, int duration)
        {
            var item = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(id);
            Assert.That(item.Name, Is.EqualTo(name)); Assert.That(item.IsStatBuffConsumable, Is.True);
            Assert.That(item.Buffs.Single().Key, Is.EqualTo(type)); Assert.That(item.Buffs[type], Is.EqualTo(amount));
            Assert.That(item.Time, Is.EqualTo(duration)); Assert.That(NXAssetLoader.Instance.LoadItemIcon(id), Is.Not.Null);
            var player = new Player(); player.SetItemData(NXDataManagerSingleton.Instance.DataManager.ItemData);
            player.Inventory.AddItem(id, 1); Assert.That(player.TryUseItem(id, out _), Is.True);
            Assert.That(player.ActiveBuffs.Single().RemainingMilliseconds, Is.EqualTo(duration));
        }
        [TestCase(2040002)]
        public void EffectsOutsideSourceActiveBuffMappingStayUnconsumed(int id)
        {
            var provider = NXDataManagerSingleton.Instance.DataManager.ItemData;
            var item = provider.GetItem(id); Assert.That(item.IsStatBuffConsumable, Is.False);
            var player = new Player(); player.SetItemData(provider); player.Inventory.AddItem(id, 1);
            Assert.That(player.TryUseItem(id, out _), Is.False); Assert.That(player.Inventory.GetItemCount(id), Is.EqualTo(1));
        }
    }
}
