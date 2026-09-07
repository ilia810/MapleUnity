using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
namespace MapleClient.Tests.GameData
{
    public class PassiveSkillIntegrationTests
    {
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private static Player Equipped(out SkillManager skills)
        {
            var player = new Player { JobId = 110 }; player.SetItemData(Assets.ItemData);
            player.Inventory.AddItem(1302000, 1); Assert.That(player.TryEquipItem(1302000, out _), Is.True);
            skills = new SkillManager(player, Assets); return player;
        }
        [Test]
        public void MasteryRecalculatesAcrossLevelsGearJobsAndRemovalWithoutChangingHp()
        {
            var p = Equipped(out var skills); int hp = p.MaxHP;
            Assert.That(p.PhysicalAttackStats.Minimum, Is.EqualTo(2));
            Assert.That(skills.SetSkillLevel(1100000, 20), Is.True);
            Assert.That(p.CombatMastery, Is.EqualTo(.6f)); Assert.That(p.Accuracy, Is.EqualTo(39));
            Assert.That(p.PhysicalAttackStats.Minimum, Is.EqualTo(8)); Assert.That(p.PhysicalAttackStats.Maximum, Is.EqualTo(12));
            for (int i = 0; i < 5; i++) Assert.That(skills.SetSkillLevel(1100000, 20), Is.True);
            Assert.That(p.MaxHP, Is.EqualTo(hp)); Assert.That(p.Accuracy, Is.EqualTo(39));
            skills.SetSkillLevel(1100000, 5); Assert.That(p.Accuracy, Is.EqualTo(24)); Assert.That(p.CombatMastery, Is.EqualTo(.53f).Within(1e-6));
            p.JobId = 112; Assert.That(p.Accuracy, Is.EqualTo(24));
            p.JobId = 120; Assert.That(p.Accuracy, Is.EqualTo(19)); Assert.That(p.CombatMastery, Is.Zero);
            p.JobId = 110; p.TryUnequipItem(EquipSlot.Weapon, out _); Assert.That(p.Accuracy, Is.EqualTo(19));
            p.TryEquipItem(1302000, out _); Assert.That(p.Accuracy, Is.EqualTo(24));
            skills.SetSkillLevel(1100000, 0); Assert.That(p.CombatMastery, Is.Zero); Assert.That(p.MaxHP, Is.EqualTo(hp));
        }
        [Test]
        public void SourcePrerequisitesAndAncestryReplaceInventedLevelRules()
        {
            var p = Equipped(out var skills);
            Assert.That(skills.CanLearnSkill(1101004), Is.False);
            skills.SetSkillLevel(1100000, 4); Assert.That(skills.CanLearnSkill(1101004), Is.False);
            Assert.That(skills.LevelUpSkill(1100000), Is.True); Assert.That(skills.CanLearnSkill(1101004), Is.True);
            Assert.That(skills.LearnSkill(1101004), Is.True);
            p.JobId = 112; Assert.That(skills.GetAvailableSkills().ContainsKey(1101004), Is.True);
            p.JobId = 120; Assert.That(skills.UseSkill(1101004).Success, Is.False);
            Assert.That(skills.SetSkillLevel(1100000, 21), Is.False); Assert.That(skills.GetSkillLevel(1100000), Is.EqualTo(5));
            Assert.That(skills.SetSkillLevel(9999999, 1), Is.False);
        }
        [Test]
        public void BerserkFollowsCurrentHpIncludingHealingAndMaxHpChanges()
        {
            var p = Equipped(out var skills); p.JobId = 132; p.MaxHP = 101;
            skills.SetSkillLevel(1320006, 30); Assert.That(p.CombatDamagePercent, Is.Zero);
            p.TakeDamage(50); Assert.That(p.CurrentHP, Is.EqualTo(50)); Assert.That(p.CombatDamagePercent, Is.EqualTo(1));
            p.Heal(1); Assert.That(p.CombatDamagePercent, Is.Zero);
            p.MaxHP = 102; Assert.That(p.CombatDamagePercent, Is.EqualTo(1));
            p.JobId = 131; Assert.That(p.CombatDamagePercent, Is.Zero);
        }
        [Test]
        public void BlessingAndPotionContributionsDoNotLeakIntoBaseOrEquipmentStats()
        {
            var p = Equipped(out var skills); skills.SetSkillLevel(12, 20);
            Assert.That(p.WeaponAttack, Is.EqualTo(37)); Assert.That(p.MagicAttack, Is.EqualTo(40)); Assert.That(p.Accuracy, Is.EqualTo(39));
            p.ApplyStatBuffs(99, "Potion", new Dictionary<BuffType,int> { [BuffType.WeaponAttack] = 5 }, 16);
            Assert.That(p.WeaponAttack, Is.EqualTo(42));
            p.WeaponAttack = 30; skills.SetSkillLevel(12, 0); Assert.That(p.WeaponAttack, Is.EqualTo(52));
            p.RemoveStatBuffs(99); Assert.That(p.WeaponAttack, Is.EqualTo(47));
            p.TryUnequipItem(EquipSlot.Weapon, out _); Assert.That(p.WeaponAttack, Is.EqualTo(30));
        }
        [TestCase(1120004,112)] [TestCase(1220005,122)] [TestCase(1320005,132)]
        public void AchillesKeepsTheSourcesExistingReductionValue(int id, int job)
        {
            var p = Equipped(out var skills); p.JobId = job; skills.SetSkillLevel(id, 30);
            Assert.That(p.PassiveDamageReduction, Is.EqualTo(.85f)); p.JobId = 110; Assert.That(p.PassiveDamageReduction, Is.Zero);
        }
        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void BoosterConsumesCostsOnceAndCastsOnSimulationTime(float interval)
        {
            var p = Equipped(out var skills); skills.SetSkillLevel(1101004, 20);
            int hp = p.CurrentHP, mp = p.CurrentMP;
            var result = skills.UseSkill(1101004); Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(p.CurrentHP, Is.EqualTo(hp - 10)); Assert.That(p.CurrentMP, Is.EqualTo(mp - 10));
            Assert.That(p.AttackSpeedModifier, Is.EqualTo(-2)); Assert.That(p.ActiveBuffs.Single().RemainingMilliseconds, Is.EqualTo(200000));
            Assert.That(p.BasicAttack.Stance, Is.EqualTo(CharacterState.Alert)); Assert.That(p.SkillEffect, Is.Not.Null);
            Assert.That(skills.UseSkill(1101004).Success, Is.False);
            float elapsed = 0;
            while (elapsed + 1e-6 < .384f) { skills.Update(interval); elapsed += interval; }
            Assert.That(p.IsBasicAttacking, Is.True); skills.Update(.016f); Assert.That(p.IsBasicAttacking, Is.False);
            Assert.That(p.SkillEffect, Is.Not.Null, "Use effect continues after the body action.");
            p.ResetMovementForMap(); Assert.That(p.SkillEffect, Is.Null); Assert.That(p.AttackSpeedModifier, Is.EqualTo(-2));
        }
        [Test]
        public void RejectedSourceCastsNeverSpendResourcesOrStartEffects()
        {
            var p = Equipped(out var skills); skills.SetSkillLevel(1101004, 1);
            p.CurrentMP = 28; int hp = p.CurrentHP;
            Assert.That(skills.UseSkill(1101004).Success, Is.False); Assert.That(p.CurrentHP, Is.EqualTo(hp)); Assert.That(p.SkillEffect, Is.Null);
            p.CurrentMP = 50; p.TakeDamage(p.CurrentHP - 29);
            Assert.That(skills.UseSkill(1101004).Success, Is.False); Assert.That(p.CurrentMP, Is.EqualTo(50));
            p.Heal(1); Assert.That(skills.UseSkill(1101004).Success, Is.True); Assert.That(p.CurrentHP, Is.EqualTo(1));
            p.ResetMovementForMap(); skills.SetSkillLevel(1001004, 20);
            Assert.That(skills.UseSkill(1001004).Success, Is.False, "Loaded attack metadata does not enable the legacy fake damage path.");
        }
        [Test]
        public void UnarmedCastingIgnoresBoosterInTheSourceSpeedQuery()
        {
            var p = Equipped(out var skills); p.TryUnequipItem(EquipSlot.Weapon,out _); skills.SetSkillLevel(1101004,20);
            Assert.That(skills.UseSkill(1101004).Success,Is.True); Assert.That(p.AttackSpeedModifier,Is.EqualTo(-2));
            Assert.That(p.EffectiveAttackSpeed,Is.Zero);
            for(int i=0;i<46;i++)skills.Update(.008f); Assert.That(p.IsBasicAttacking,Is.True);
            skills.Update(.008f);Assert.That(p.IsBasicAttacking,Is.False);
        }
        [TestCase(0)] [TestCase(10)]
        public void ContactAppliesAchillesOnlyAfterTheSourcesNonzeroDefenseBranch(int defense)
        {
            var p=Equipped(out var skills);p.JobId=112;p.WeaponDefense=defense;p.Position=new MapleClient.GameLogic.Vector2(0,.3f);
            skills.SetSkillLevel(1120004,30);
            var monster=new Monster(new MapleClient.GameLogic.MonsterTemplate {MaxHP=100,PhysicalDamage=20,BodyAttack=true,
                ContactAnimations=new Dictionary<string,MapleClient.GameLogic.MobContactAnimation>{["stand"]=new MapleClient.GameLogic.MobContactAnimation(new[]{
                    new MapleClient.GameLogic.MobContactFrame{Left=-15,Right=15,Top=-30,Bottom=0,DelayMilliseconds=100}},false)}},MapleClient.GameLogic.Vector2.Zero);
            int hp=p.CurrentHP;Assert.That(new Combat().CheckContact(p,new[]{monster}),Is.SameAs(monster));
            if(defense==0)Assert.That(hp-p.CurrentHP,Is.InRange(16,20));else Assert.That(hp-p.CurrentHP,Is.EqualTo(2));
        }
    }
}
