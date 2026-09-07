using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class WeaponAssetRewriteTests
    {
        [TestCase(1302000, false, CharacterState.Stand, CharacterState.Walk, 1, 4)]
        [TestCase(1382000, true, CharacterState.Stand, CharacterState.Walk, 6, 8)]
        [TestCase(1402009, true, CharacterState.Stand2, CharacterState.Walk, 5, 5)]
        [TestCase(1442079, true, CharacterState.Stand2, CharacterState.Walk2, 2, 8)]
        [TestCase(1452000, false, CharacterState.Stand, CharacterState.Walk, 3, 5)]
        [TestCase(1462000, true, CharacterState.Stand2, CharacterState.Walk2, 4, 6)]
        public void WeaponMetadataHonorsExplicitPosesAndSourceTwoHandedExceptions(int id, bool twoHanded, CharacterState stand, CharacterState walk, int attack, int speed)
        {
            var item = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(id);
            Assert.That(item.IsTwoHanded, Is.EqualTo(twoHanded));
            Assert.That(item.Weapon.Stand, Is.EqualTo(stand)); Assert.That(item.Weapon.Walk, Is.EqualTo(walk));
            Assert.That(item.Weapon.AttackType, Is.EqualTo(attack)); Assert.That(item.Weapon.AttackSpeed, Is.EqualTo(speed));
            Assert.That(item.Weapon.UsesTwoHandedDrawOrder(CharacterState.Stand), Is.False);
            Assert.That(item.Weapon.UsesTwoHandedDrawOrder(CharacterState.Walk2), Is.True);
            Assert.That(item.Weapon.UsesTwoHandedDrawOrder(CharacterState.Jump), Is.EqualTo(twoHanded));
        }

        [Test]
        public void SwordAndSlowPolearmUseRealBodyDelaysAndQuantizedSourceAttackSpeed()
        {
            var items = NXDataManagerSingleton.Instance.DataManager.ItemData;
            var sword = items.GetItem(1302000).Weapon;
            Assert.That(sword.FrameDelays[CharacterState.Attack1], Is.EqualTo(new[] { 350, 450 }));
            Assert.That(sword.FrameDelays[CharacterState.SwingO3], Is.EqualTo(new[] { 300, 150, 350 }));
            var fast = sword.CreateAttack(0, false);
            for (int i = 0; i < 79; i++) fast.Advance(.008f);
            Assert.That(fast.IsComplete, Is.False); fast.Advance(.008f); Assert.That(fast.IsComplete, Is.True);
            var slow = items.GetItem(1442079).Weapon.CreateAttack(0, false);
            Assert.That(slow.Stance, Is.EqualTo(CharacterState.StabT1));
            for (int i = 0; i < 107; i++) slow.Advance(.008f);
            Assert.That(slow.IsComplete, Is.False); slow.Advance(.008f); Assert.That(slow.IsComplete, Is.True);
            Assert.That(sword.CreateAttack(0, true).Stance, Is.EqualTo(CharacterState.ProneStab));
        }

        [TestCase(1302000)] [TestCase(1312000)] [TestCase(1322000)] [TestCase(1332000)] [TestCase(1372000)]
        [TestCase(1382000)] [TestCase(1402009)] [TestCase(1412000)] [TestCase(1422000)] [TestCase(1432000)] [TestCase(1442079)]
        public void MeleeArtworkMatchesAuthoredFramesIncludingEmptyWeaponFrames(int id)
        {
            var weapon = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(id).Weapon;
            var loader = NXAssetLoader.Instance;
            var pool = WeaponProfile.AttackStances(weapon.AttackType);
            Assert.That(pool.Count, Is.GreaterThan(0));
            foreach (var state in pool.Concat(new[] { CharacterState.ProneStab, weapon.Stand, weapon.Walk }).Distinct())
            {
                string stance = CharacterStances.Name(state);
                var body = loader.GetNxFile("character").GetNode("00002000.img/" + stance);
                for (int frame = 0; body[frame.ToString()] != null; frame++)
                {
                    loader.LoadCharacterBodyParts(0, stance, frame, out var attachments);
                    var parts = NxEquipmentFrames.Load(id, stance, frame, attachments);
                    // Triangular Zamadar swingO1 has no frame 0 in this NX pack.
                    // Clear the old weapon on that frame; never borrow a neighboring pose.
                    var authored = loader.GetNxFile("character").GetNode($"Weapon/{id:D8}.img/{stance}/{frame}");
                    if (authored == null) { Assert.That(parts, Is.Empty, $"{id}/{stance}/{frame}"); continue; }
                    Assert.That(parts, Is.Not.Empty, $"{id}/{stance}/{frame}");
                    Assert.That(parts.All(p => p.Sprite != null && p.Sprite.rect.width > 0), Is.True);
                }
            }
        }
    }
}
