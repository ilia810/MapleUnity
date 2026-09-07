using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class ItemAssetRewriteTests
    {
        [TestCase(2000000, "Red Potion", 50, 0)]
        [TestCase(2000001, "Orange Potion", 150, 0)]
        [TestCase(2000003, "Blue Potion", 0, 100)]
        [TestCase(2000006, "Mana Elixir", 0, 300)]
        public void PotionNamesAndEffectsUseRealItemAndStringFiles(int id, string name, int hp, int mp)
        {
            var item = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(id);
            Assert.That(item.Name, Is.EqualTo(name)); Assert.That(item.Hp, Is.EqualTo(hp)); Assert.That(item.Mp, Is.EqualTo(mp));
            Assert.That(item.IsRecoveryConsumable, Is.True); Assert.That(NXAssetLoader.Instance.LoadItemIcon(id), Is.Not.Null);
        }
        [Test]
        public void ElixirsReadPercentageEffectsAndMissingItemsAreNotFabricated()
        {
            var provider = NXDataManagerSingleton.Instance.DataManager.ItemData;
            Assert.That(provider.GetItem(2000004).HpRate, Is.EqualTo(50)); Assert.That(provider.GetItem(2000004).MpRate, Is.EqualTo(50));
            Assert.That(provider.GetItem(2000005).HpRate, Is.EqualTo(100)); Assert.That(provider.GetItem(int.MaxValue), Is.Null);
            Assert.That(provider.GetItem(2040002).IsRecoveryConsumable, Is.False);
        }
        [Test]
        public void EquipmentReadsCharacterStatsRequirementsAndIcons()
        {
            var provider = NXDataManagerSingleton.Instance.DataManager.ItemData;
            var shirt = provider.GetItem(1040002);
            Assert.That(shirt.Name, Is.EqualTo("White Undershirt")); Assert.That(shirt.Stats[StatType.WeaponDefense], Is.EqualTo(3));
            Assert.That(shirt.EquipmentSlot, Is.EqualTo(EquipSlot.Top)); Assert.That(shirt.Gender, Is.EqualTo(0));
            Assert.That(provider.GetItem(1302000).Stats[StatType.WeaponAttack], Is.EqualTo(17));
            Assert.That(provider.GetItem(1050000).RequiredJobMask, Is.EqualTo(1)); Assert.That(provider.GetItem(1050000).RequiredStr, Is.EqualTo(110));
            Assert.That(NXAssetLoader.Instance.LoadItemIcon(1040002), Is.Not.Null);
        }
        [TestCase("stand1")] [TestCase("walk1")] [TestCase("stabO1")] [TestCase("jump")]
        public void ShirtLoadsBothChestAndSleeveWithIndependentSourceOrigins(string stance)
        {
            var loader = NXAssetLoader.Instance; _ = NXDataManagerSingleton.Instance.DataManager;
            loader.LoadCharacterBodyParts(0, stance, 0, out var attachments);
            var parts = NxEquipmentFrames.Load(1040002, stance, 0, attachments);
            Assert.That(parts.Select(p => p.Name), Is.EquivalentTo(new[] { "mail", "mailArm" }));
            foreach (var part in parts)
            {
                Assert.That(part.Sprite, Is.Not.Null);
                var node = loader.GetNxFile("character").GetNode($"Coat/01040002.img/{stance}/0/{part.Name}");
                var origin = node["origin"].GetValue<UnityEngine.Vector2>();
                var anchor = node["map"]["navel"].GetValue<UnityEngine.Vector2>();
                var target = attachments["body.map.navel"];
                var expectedOrigin = origin - target + anchor;
                Assert.That(part.Sprite.pivot.x, Is.EqualTo(expectedOrigin.x).Within(.001));
                Assert.That(part.Sprite.rect.height - part.Sprite.pivot.y, Is.EqualTo(expectedOrigin.y).Within(.001));
            }
        }
    }
}
