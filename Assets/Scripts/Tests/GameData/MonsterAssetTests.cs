using GameData;
using MapleClient.GameData;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class MonsterAssetTests
    {
        [Test]
        public void BasicAttackUsesTheTwoFramesPresentInCharacterNx()
        {
            var data = NXDataManagerSingleton.Instance.DataManager;
            Assert.That(data.CharacterData.GetAnimationFrameCount(MapleClient.GameLogic.Interfaces.CharacterState.Attack1), Is.EqualTo(2));
            Assert.That(data.GetNode("character", "00002000.img/stabO1/2"), Is.Null);
        }

        [Test]
        public void RealBlueSnailLoadsSourceStatsWithoutPlaceholderDefaults()
        {
            var data = NXDataManagerSingleton.Instance.DataManager;
            var mob = data.MobData.GetMob(100101);
            Assert.That(mob, Is.Not.Null);
            Assert.That(mob.HP, Is.EqualTo(15));
            Assert.That(mob.Speed, Is.EqualTo(-50));
            Assert.That(mob.PDDamage, Is.EqualTo(0));
            Assert.That(mob.CanMove, Is.True);
            Assert.That(mob.CanFly, Is.False);
            Assert.That(mob.Drops, Is.Empty, "Mob.nx does not supply the server's loot table.");
            Assert.That(data.MobData.GetMob(int.MaxValue), Is.Null);
        }

        [Test]
        public void ContactBoundsComeFromFrameMetadataAndRespectLinksAndZigzag()
        {
            var data = NXDataManagerSingleton.Instance.DataManager;
            var mob = data.MobData.GetMob(100101);
            Assert.That(mob.BodyAttack, Is.True);
            var animation = mob.ContactAnimations["move"];
            Assert.That(animation.Frames.Length, Is.EqualTo(4));
            Assert.That(animation.Zigzag, Is.True);
            var sequence = new[] { 0, 1, 2, 3, 2, 1, 0 };
            for (int i = 0; i < sequence.Length; i++)
            {
                var frame = animation.Sample(i * 120 + 1);
                var node = data.GetNode("mob", $"0100101.img/move/{sequence[i]}");
                var lt = node["lt"].GetValue<UnityEngine.Vector2>();
                var rb = node["rb"].GetValue<UnityEngine.Vector2>();
                Assert.That(frame.Left, Is.EqualTo((int)lt.x));
                Assert.That(frame.Top, Is.EqualTo((int)lt.y));
                Assert.That(frame.Right, Is.EqualTo((int)rb.x));
                Assert.That(frame.Bottom, Is.EqualTo((int)rb.y));
                Assert.That(frame.Right - frame.Left, Is.GreaterThan(0));
                Assert.That(frame.Bottom - frame.Top, Is.GreaterThan(0));
            }
            var linked = data.MobData.GetMob(3000002).ContactAnimations["stand"].Frames;
            var original = data.MobData.GetMob(3000001).ContactAnimations["stand"].Frames;
            Assert.That(linked, Is.EqualTo(original));
        }

        [Test]
        public void LinkedMobKeepsItsStatsAndUsesLinkedArtwork()
        {
            var data = NXDataManagerSingleton.Instance.DataManager;
            Assert.That(data.MobData.GetMob(3000002).HP, Is.EqualTo(800));
            var animation = new NxMobAnimations(data).Get(3000002, "stand");
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.Frames[0].Path, Does.StartWith("mob/3000001.img/stand/"));
        }

        [Test]
        public void SnailZigzagAnimationUsesNumericFramesAndSourceDelays()
        {
            var animation = new NxMobAnimations(NXDataManagerSingleton.Instance.DataManager).Get(100101, "move");
            Assert.That(animation.Frames.Count, Is.EqualTo(4));
            Assert.That(animation.Zigzag, Is.True);
            Assert.That(animation.DurationSeconds, Is.EqualTo(.72).Within(.00001));
            var order = new[] { 0, 1, 2, 3, 2, 1, 0 };
            for (int i = 0; i < order.Length; i++)
                Assert.That(animation.Sample(.001 + .12 * i, true, out _), Is.EqualTo(order[i]));
            Assert.That(animation.Frames[0].Image.Origin.x, Is.EqualTo(16));
            Assert.That(animation.Frames[0].Image.Origin.y, Is.EqualTo(34));
            Assert.That(animation.Frames[0].Image.Sprite.texture.filterMode, Is.EqualTo(UnityEngine.FilterMode.Point));
        }

        [Test]
        public void DeathAnimationKeepsItsAuthoredFadeAndLastFrame()
        {
            var animation = new NxMobAnimations(NXDataManagerSingleton.Instance.DataManager).Get(1110100, "die1");
            Assert.That(animation.DurationSeconds, Is.EqualTo(.84).Within(.00001));
            int index = animation.Sample(.69, false, out float fraction);
            Assert.That(index, Is.EqualTo(3));
            Assert.That(fraction, Is.EqualTo(.5f).Within(.00001));
            Assert.That(animation.Frames[index].StartAlpha, Is.EqualTo(1));
            Assert.That(animation.Frames[index].EndAlpha, Is.EqualTo(0));
            Assert.That(animation.Sample(100, false, out fraction), Is.EqualTo(3));
            Assert.That(fraction, Is.EqualTo(1));
        }
    }
}
