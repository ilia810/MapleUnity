using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class UtilitySkillIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => new MapData { MapId = id,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } };
        }
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private GameWorld world;
        private Player p;
        private SkillManager skills;
        [SetUp] public void Setup()
        { world = new GameWorld(null, new Maps(), assetProvider: Assets); world.LoadMap(42); p = world.Player; skills = world.SkillManager; Step(100); }
        private void Step(int ticks) { for (int i = 0; i < ticks; i++) world.UpdatePhysics(.008f); }
        private void Cast(int id, int rank)
        {
            p.JobId = Assets.SkillData.GetSkill(id).JobId; Assert.That(skills.SetSkillLevel(id, rank), Is.True);
            var result = skills.UseSkill(id); Assert.That(result.Success, Is.True, result.ErrorMessage);
        }
        private void EarnTo(int level) { while (p.Level < level) p.AddExperience(p.ExperienceToNextLevel - p.Experience); }
        [TestCase(3001003,1,8,70000,1)] [TestCase(3001003,20,16,300000,20)]
        [TestCase(4001003,1,24,10000,-30)] [TestCase(4001003,20,5,200000,0)]
        public void UtilityCastsUseOriginalCostsDurationsActionAndNormalizedEffects(int id,int rank,int cost,int time,int value)
        {
            int mp = p.CurrentMP, accuracy = p.Accuracy; Cast(id,rank);
            Assert.That(p.CurrentMP, Is.EqualTo(mp-cost)); Assert.That(p.CurrentHP, Is.EqualTo(100));
            Assert.That(p.BasicAttack.Stance, Is.EqualTo(CharacterState.Alert)); Assert.That(p.SkillEffect, Is.Not.Null);
            Assert.That(p.ActiveBuffs.All(b=>b.RemainingMilliseconds==time), Is.True);
            if(id==3001003)
            {
                Assert.That(p.Accuracy, Is.EqualTo(accuracy+value)); Assert.That(p.Avoidability, Is.EqualTo(value));
                Assert.That(p.PhysicalAttackStats.Accuracy, Is.EqualTo(p.Accuracy));
                Assert.That(p.MagicSkillAttackStats.Accuracy, Is.EqualTo(p.Accuracy));
            }
            else { Assert.That(p.IsHidden, Is.True); Assert.That(p.Speed, Is.EqualTo(100+value)); }
        }
        [TestCase(3000000,1,1,0)] [TestCase(3000000,16,16,0)]
        [TestCase(4000000,1,1,1)] [TestCase(4000000,20,20,20)]
        public void UtilityPassivesUseRankValuesAndJobMetadataWithoutBaseStatMutation(int id,int rank,int accuracy,int avoid)
        {
            p.JobId = id / 10000; int original = p.Accuracy;
            Assert.That(skills.SetSkillLevel(id,rank), Is.True);
            Assert.That(p.Accuracy, Is.EqualTo(original+accuracy)); Assert.That(p.Avoidability, Is.EqualTo(avoid));
            Assert.That(skills.UseSkill(id).Success, Is.False);
            p.JobId = 200; Assert.That(p.Accuracy, Is.EqualTo(original)); Assert.That(p.Avoidability, Is.Zero);
            p.JobId = id/10000; Assert.That(p.Accuracy, Is.EqualTo(original+accuracy));
            skills.SetSkillLevel(id,0); Assert.That(p.Accuracy, Is.EqualTo(original)); Assert.That(p.Avoidability, Is.Zero);
        }
        [Test] public void EarnedAmazonRanksUnlockFocusAndDarkSightStillRequiresDisorderRanks()
        {
            EarnTo(10); Assert.That(world.TryAdvanceFirstJob(300,out _), Is.True); EarnTo(11);
            int sp = p.SkillPoints;
            Assert.That(skills.TrySpendSkillPoint(3001003,out _), Is.False); Assert.That(p.SkillPoints, Is.EqualTo(sp));
            for(int i=0;i<3;i++) Assert.That(skills.TrySpendSkillPoint(3000000,out _), Is.True);
            Assert.That(skills.TrySpendSkillPoint(3001003,out var error), Is.True,error); Assert.That(p.SkillPoints, Is.Zero);
            Assert.That(skills.UseSkill(3001003).Success, Is.True);
            Step(100); p.JobId=400;
            Assert.That(skills.CanLearnSkill(4001003), Is.False); Assert.That(skills.CanSpendSkillPoint(4001002,out _), Is.False);
            Assert.That(world.RequestSupportPractice(400,out error), Is.True,error);
            Assert.That(skills.GetSkillLevel(4001003), Is.EqualTo(1)); Assert.That(skills.GetSkillLevel(4001002), Is.Zero);
        }
        [TestCase(300)] [TestCase(400)]
        public void NewPracticePresetsDoNotGrantResourcesOrLowerLearnedRanks(int job)
        {
            int id = GameWorld.SupportPracticeSkills(job).Single(); p.TakeDamage(10); p.CurrentMP=30;
            skills.SetSkillLevel(id,10); var before=world.CaptureProgress(); int items=p.Inventory.GetItemCount(2000003);
            Assert.That(world.RequestSupportPractice(job,out _), Is.True); Assert.That(world.RequestSupportPractice(job,out _), Is.True);
            Assert.That(skills.GetSkillLevel(id), Is.EqualTo(10)); Assert.That(p.CurrentHP, Is.EqualTo(90)); Assert.That(p.CurrentMP, Is.EqualTo(30));
            Assert.That(p.Inventory.GetItemCount(2000003), Is.EqualTo(items)); Assert.That(p.AbilityPoints, Is.EqualTo(before.Player.AbilityPoints));
        }
        [Test] public void FocusAndRealPotionsReplaceOnlyTheirOwnStatContributions()
        {
            Cast(3001003,20); int original=p.Accuracy-20;
            foreach(int id in new[]{2002000,2002005})
            {
                Assert.That(Assets.ItemData.GetItem(id).IsStatBuffConsumable, Is.True);
                p.Inventory.AddItem(id,1); Assert.That(p.TryUseItem(id,out var error), Is.True,error);
            }
            Assert.That(p.Accuracy, Is.EqualTo(original+5)); Assert.That(p.Avoidability, Is.EqualTo(5));
            Assert.That(skills.IsBuffActive(3001003), Is.False);
            Assert.That(skills.TryCancelBuff(-2002005), Is.True); Assert.That(p.Accuracy, Is.EqualTo(original));
            Assert.That(p.Avoidability, Is.EqualTo(5));
        }
        [Test] public void FocusAccuracyChangesAttackHitChanceAndKeepsTheSourceCapOrder()
        {
            var target=new MonsterTemplate { Level=1,Avoidability=10 };
            float before=p.PhysicalAttackStats.HitChance(target); int derivedAccuracy=p.Accuracy; Cast(3001003,20);
            Assert.That(p.PhysicalAttackStats.HitChance(target), Is.GreaterThan(before));
            p.Accuracy=998; Assert.That(p.Accuracy, Is.EqualTo(999+derivedAccuracy));
            Assert.That(p.PhysicalAttackStats.Accuracy, Is.EqualTo(p.Accuracy));
            skills.TryCancelBuff(3001003); Assert.That(p.Accuracy, Is.EqualTo(998+derivedAccuracy));
        }
        private sealed class Rolls : Random
        {
            private readonly Queue<double> values;
            public int Calls;
            public Rolls(params double[] values) { this.values=new Queue<double>(values); }
            public override double NextDouble() { Calls++; return values.Count>0?values.Dequeue():.5; }
        }
        private static Monster ContactMob() => new Monster(new MonsterTemplate { MaxHP=100, Level=1, Accuracy=10, PhysicalDamage=20, BodyAttack=true,
            ContactAnimations=new Dictionary<string,MobContactAnimation> { ["stand"]=new MobContactAnimation(new[]{new MobContactFrame {
                Left=-20,Top=-35,Right=20,Bottom=0,DelayMilliseconds=100 }},false) } },new Vector2(0,0));
        [Test] public void EvasionProducesMissFeedbackWithoutHpMpOrKnockbackAndUsesTheNormalGracePeriod()
        {
            p.ApplyStatBuffs(123,"Evasion",new Dictionary<BuffType,int>{{BuffType.Avoidability,20},{BuffType.MagicGuard,80}},10000);
            var rolls=new Rolls(.99,0,.5); var combat=new Combat(contactRandom:rolls); var mob=ContactMob();
            var hits=new List<int>();p.DamageTaken+=hits.Add;int hp=p.CurrentHP,mp=p.CurrentMP;
            Assert.That(combat.CheckContact(p,new[]{mob}), Is.SameAs(mob)); Assert.That(hits, Is.EqualTo(new[]{0}));
            Assert.That(p.CurrentHP, Is.EqualTo(hp)); Assert.That(p.CurrentMP, Is.EqualTo(mp)); Assert.That(p.InvulnerableMilliseconds, Is.EqualTo(2000));
            Assert.That(combat.CheckContact(p,new[]{mob}), Is.Null); Assert.That(rolls.Calls, Is.EqualTo(1));
            Step(250); Assert.That(p.Velocity.X, Is.Zero); Assert.That(p.Velocity.Y, Is.Zero);
            Assert.That(combat.CheckContact(p,new[]{mob}), Is.SameAs(mob));
            Assert.That(p.CurrentHP, Is.LessThan(hp)); Assert.That(p.CurrentMP, Is.LessThan(mp)); Assert.That(hits.Count, Is.EqualTo(2));
        }
        [Test] public void ContactChanceUsesBothLevelsAndAvoidabilityWithAnExplicitMissingAccuracyFallback()
        {
            var mob=ContactMob().Template; Assert.That(Combat.ContactHitChance(p,mob), Is.EqualTo(1));
            p.Avoidability=20; float chance=Combat.ContactHitChance(p,mob);
            Assert.That(chance, Is.EqualTo(10/(1.84f*20+1)).Within(.000001));
            p.Level=30; Assert.That(Combat.ContactHitChance(p,mob), Is.LessThan(chance));
            mob.Accuracy=0; Assert.That(Combat.ContactHitChance(p,mob), Is.EqualTo(1));
        }
        [Test] public void DarkSightRejectsContactAndBasicOrSkillAttacksWithoutSpendingAnyResource()
        {
            Assert.That(world.RequestRangedPractice(147,out _), Is.True);
            int weapon=GameWorld.RangedPracticeWeapon(147); Assert.That(p.TryEquipItem(weapon,out _), Is.True);
            Cast(4001003,20); Step(30); int hp=p.CurrentHP,mp=p.CurrentMP,ammo=p.AmmunitionCount;
            var rolls=new Rolls();var combat=new Combat(contactRandom:rolls);var mob=ContactMob();
            Assert.That(combat.CheckContact(p,new[]{mob}), Is.Null); Assert.That(rolls.Calls, Is.Zero);
            Assert.That(p.ReceiveContactDamage(30,false), Is.False); Assert.That(p.InvulnerableMilliseconds, Is.Zero);
            Assert.That(p.PlayBasicAttackAnimation(), Is.False); Assert.That(combat.PerformBasicAttack(p,new List<Monster>{mob},1), Is.Empty);
            var attempt=skills.UseSkill(4001344);Assert.That(attempt.Success, Is.False);Assert.That(attempt.ErrorMessage, Does.Contain("concealment"));
            Assert.That(p.CurrentHP, Is.EqualTo(hp));Assert.That(p.CurrentMP, Is.EqualTo(mp));Assert.That(p.AmmunitionCount, Is.EqualTo(ammo));
            Assert.That(skills.TryCancelBuff(4001003), Is.True);Assert.That(skills.TryCancelBuff(4001003), Is.False);
            Assert.That(skills.UseSkill(4001344).Success, Is.True);
        }
        [Test] public void ConcealmentIsIndependentOfSkillIdAndExpiresOnTheSimulationClock()
        {
            p.ApplyStatBuffs(190000000,"Custom concealment",new Dictionary<BuffType,int>{{BuffType.Hide,1},{BuffType.Speed,-30}},16);
            Assert.That(p.IsHidden, Is.True); Assert.That(p.ConcealmentOpacity, Is.EqualTo(.35f));Assert.That(p.Speed, Is.EqualTo(70));
            skills.Update(1);Assert.That(p.IsHidden, Is.True); Step(1); Assert.That(p.IsHidden, Is.True);
            Step(1);Assert.That(p.IsHidden, Is.False);Assert.That(p.Speed, Is.EqualTo(100));Assert.That(p.ConcealmentOpacity, Is.EqualTo(1));
            Assert.That(p.ApplyStatBuffs(1,"Invalid",new Dictionary<BuffType,int>{{BuffType.Hide,0}},100), Is.False);
        }
        [Test] public void CancellationPreservesReplacementSpeedAndOtherBuffs()
        {
            Cast(4001003,1);p.ApplyStatBuffs(-2002001,"Speed Potion",new Dictionary<BuffType,int>{{BuffType.Speed,8}},1000);
            Assert.That(p.IsHidden, Is.True);Assert.That(p.Speed, Is.EqualTo(108));
            Assert.That(skills.TryCancelBuff(4001003), Is.True);Assert.That(p.IsHidden, Is.False);Assert.That(p.Speed, Is.EqualTo(108));
        }
        [Test] public void ConcealmentSlowsWalkingSurvivesTravelButClearsOnLoadAndDeath()
        {
            p.MoveRight(true);Step(150);float ordinarySpeed=p.Velocity.X;
            Cast(4001003,1);Step(180);
            Assert.That(p.Velocity.X, Is.GreaterThan(0).And.LessThan(ordinarySpeed));
            p.MoveRight(false);Step(100);var save=world.CaptureProgress();
            world.LoadMap(43);Assert.That(p.IsHidden, Is.True);Assert.That(p.Speed, Is.EqualTo(70));
            Assert.That(world.TryRestoreProgress(save,out var error), Is.True,error);Assert.That(p.IsHidden, Is.False);Assert.That(p.Speed, Is.EqualTo(100));
            Step(100);p.CurrentMP=p.MaxMP;Cast(4001003,1);p.TakeDamage(1);Assert.That(p.CurrentHP, Is.EqualTo(p.MaxHP-1));
            p.TakeDamage(int.MaxValue);Assert.That(p.IsDead, Is.True);Assert.That(p.IsHidden, Is.False);Assert.That(p.ActiveBuffs, Is.Empty);
        }
    }
}
