using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameData
{
    public class AfterimageAssetTests
    {
        [Test]
        public void SwordAfterimageReadsSourceBoundsHitFrameAndFadeWithoutUsingBitmapDimensionsAsReach()
        {
            var weapon = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(1302000).Weapon;
            var effect = weapon.Afterimages[CharacterState.Attack1];
            Assert.That(effect.Path, Is.EqualTo("Afterimage/swordOL.img/0/stabO1")); Assert.That(effect.FirstFrame, Is.EqualTo(1));
            Assert.That(new[] { effect.Bounds.Left, effect.Bounds.Top, effect.Bounds.Right, effect.Bounds.Bottom }, Is.EqualTo(new[] { -84, -30, -20, -15 }));
            Assert.That(effect.Frames.Select(f => f.DelayMilliseconds), Is.EqualTo(new[] { 30, 200 }));
            Assert.That(effect.Sample(0, out _), Is.Zero); Assert.That(effect.Sample(30, out float start), Is.EqualTo(1)); Assert.That(start, Is.Zero);
            Assert.That(effect.Sample(130, out float middle), Is.EqualTo(1));
            Assert.That(Mathf.Lerp(effect.Frames[1].StartAlpha, effect.Frames[1].EndAlpha, middle), Is.EqualTo(.5f).Within(.001f));
            Assert.That(effect.Sample(230, out _), Is.EqualTo(-1)); Assert.That(effect.Sample(10000, out _), Is.EqualTo(-1));
            Assert.That(weapon.CreateAttack(0, false).HitDelayMilliseconds, Is.EqualTo(269));
            Assert.That(weapon.CreateAttack(2, false).HitDelayMilliseconds, Is.EqualTo(346));
        }

        [Test]
        public void WeaponRequiredLevelSelectsEffectTierAndPolearmsReachBeyondSwordStabs()
        {
            var items = NXDataManagerSingleton.Instance.DataManager.ItemData;
            Assert.That(items.GetItem(1402000).Weapon.Afterimages[CharacterState.Attack1].Path, Does.Contain("/2/"));
            var polearm = items.GetItem(1442079).Weapon;
            var effect = polearm.Afterimages[CharacterState.StabT1];
            Assert.That(effect.Path, Is.EqualTo("Afterimage/poleArm.img/0/stabT1"));
            Assert.That(effect.Bounds.Left, Is.EqualTo(-130)); Assert.That(effect.Bounds.Top, Is.EqualTo(-33));
            Assert.That(effect.FirstFrame, Is.EqualTo(2));
            Assert.That(polearm.CreateAttack(0, false).HitDelayMilliseconds, Is.EqualTo(444));
        }

        [TestCase(1302000)] [TestCase(1312000)] [TestCase(1322000)] [TestCase(1332000)] [TestCase(1372000)]
        [TestCase(1382000)] [TestCase(1402009)] [TestCase(1412000)] [TestCase(1422000)] [TestCase(1432000)] [TestCase(1442079)]
        public void MeleeAfterimagesHaveUsableBoundsAndOriginAlignedSpritesAcrossTheAttackPool(int id)
        {
            var weapon = NXDataManagerSingleton.Instance.DataManager.ItemData.GetItem(id).Weapon;
            foreach (var stance in WeaponProfile.AttackStances(weapon.AttackType).Concat(new[] { CharacterState.ProneStab }))
            {
                var effect = weapon.Afterimages[stance];
                Assert.That(effect.HasBounds, Is.True); Assert.That(effect.Bounds.Right, Is.GreaterThan(effect.Bounds.Left));
                Assert.That(effect.FirstFrame, Is.LessThan(weapon.FrameDelays[stance].Length)); Assert.That(effect.Frames, Is.Not.Empty);
                foreach (var frame in effect.Frames)
                {
                    var sprite = NxAfterimageSprites.Load(frame.Path); Assert.That(sprite, Is.Not.Null, frame.Path);
                    var origin = NXAssetLoader.Instance.GetNxFile("character").GetNode(frame.Path)["origin"].GetValue<Vector2>();
                    Assert.That(sprite.pivot.x, Is.EqualTo(origin.x).Within(.001));
                    Assert.That(sprite.rect.height - sprite.pivot.y, Is.EqualTo(origin.y).Within(.001));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100));
                }
            }
        }
    }
}
