using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
namespace MapleClient.Tests.GameData
{
    public class SkillSourceAssetTests
    {
        private static ISkillDataProvider Provider => NXDataManagerSingleton.Instance.DataManager.SkillData;
        [TestCase(12,"Blessing of the Fairy",20)] [TestCase(1100000,"Sword Mastery",20)]
        [TestCase(1101004,"Sword Booster",20)] [TestCase(1320006,"Berserk",30)] [TestCase(1120004,"Achilles",30)]
        public void SourceNamesLevelsAndIconsUsePaddedKeys(int id, string name, int max)
        {
            var skill = Provider.GetSkill(id); Assert.That(skill, Is.Not.Null); Assert.That(skill.Name, Is.EqualTo(name));
            Assert.That(skill.IsSourceData, Is.True); Assert.That(skill.MaxLevel, Is.EqualTo(max));
            Assert.That(skill.IsPassive, Is.EqualTo(id % 10000 / 1000 == 0));
            Assert.That(Provider.SkillExists(id), Is.True); Assert.That(Provider.GetSkill(id), Is.SameAs(skill));
            Assert.That(SpriteLoader.LoadSprite(NXAssetLoader.Instance.GetNxFile("skill").GetNode(skill.IconPath), "skill/" + skill.IconPath), Is.Not.Null);
        }
        [Test]
        public void BoosterUsesSecondsAndRealActionAliases()
        {
            var skill = Provider.GetSkill(1101004); var data = skill.Levels[20];
            Assert.That(data.Duration, Is.EqualTo(200000)); Assert.That(data.HpCost, Is.EqualTo(10)); Assert.That(data.MpCost, Is.EqualTo(10));
            Assert.That(data.Buffs.Single().Value, Is.EqualTo(-2)); Assert.That(skill.RequiredSkills[1100000], Is.EqualTo(5));
            Assert.That(skill.Action, Is.EqualTo("alert2")); Assert.That(skill.ActionStances, Is.All.EqualTo(CharacterState.Alert));
            Assert.That(skill.ActionFrames, Is.EqualTo(new[] {0,1,2})); Assert.That(skill.ActionDelays, Is.EqualTo(new[] {200,200,200}));
            Assert.That(skill.EffectFrames.Length, Is.EqualTo(9));
            foreach (var frame in skill.EffectFrames) Assert.That(SpriteLoader.LoadSpriteWithShift(NXAssetLoader.Instance.GetNxFile("skill").GetNode(frame.Path), UnityEngine.Vector2.zero, "skill/" + frame.Path), Is.Not.Null);
        }
        [Test]
        public void JobQueriesAndMissingSkillsRemainReal()
        {
            Assert.That(Provider.GetSkillsForJob(110).ContainsKey(1100000), Is.True);
            Assert.That(Provider.GetSkillsForJob(110).ContainsKey(1001004), Is.False);
            Assert.That(Provider.GetSkillsForJob(0).ContainsKey(12), Is.True);
            Assert.That(Provider.GetSkill(9999999), Is.Null); Assert.That(Provider.GetSkill(-1), Is.Null); Assert.That(Provider.SkillExists(9999999), Is.False);
            Assert.That(Provider.GetSkill(1001005).Levels[20].MobCount, Is.EqualTo(6));
            Assert.That(Provider.GetSkill(1001005).Levels[20].Range, Is.EqualTo(150));
            Assert.That((double)Provider.GetSkill(1001005).Levels[20].DamageMultiplier, Is.EqualTo(1.2999999523162842));
            Assert.That((double)Provider.GetSkill(1001004).Levels[20].DamageMultiplier, Is.EqualTo(2.5999999046325684));
        }
    }
}
