using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.GameData
{
    public class ThiefSkillIntegrationTests
    {
        private sealed class Rolls : Random
        {
            private readonly double value; public int Draws;
            public Rolls(double value = .5) { this.value = value; }
            public override int Next() => 0;
            public override double NextDouble() { Draws++; return value; }
        }
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => new MapData { MapId = id,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } };
        }
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private GameWorld world; private Player p; private SkillManager skills;
        private readonly List<CustomSkillAsset> custom = new List<CustomSkillAsset>();
        [SetUp] public void Setup()
        {
            world = new GameWorld(null, new Maps(), assetProvider: Assets); world.LoadMap(42); p = world.Player; skills = world.SkillManager;
            Step(100); p.JobId = 400; p.Level = 10; p.Inventory.AddItem(1332005, 1);
            Assert.That(p.TryEquipItem(1332005, out var error), Is.True, error);
        }
        [TearDown] public void Cleanup()
        { foreach (var a in custom) { ((SkillCatalog)Assets.SkillData).RemoveCustom(a.Id); Object.DestroyImmediate(a); } custom.Clear(); }
        private void Step(int ticks) { for (int i = 0; i < ticks; i++) world.UpdatePhysics(.008f); }
        private static void Finish(Combat combat) { for (int i = 0; i < 200; i++) combat.Update(.008f); }
        private Monster Mob(float distance = .3f, MonsterTemplate shared = null) => new Monster(shared ?? new MonsterTemplate {
            Name = "Target", MaxHP = 10000, Level = 1, PhysicalDamage = 100, PhysicalDefense = 30, BodyAttack = true,
            ContactAnimations = new Dictionary<string,MobContactAnimation> { ["stand"] = new MobContactAnimation(new[] {
                new MobContactFrame { Left = -10, Top = -30, Right = 10, Bottom = 0, DelayMilliseconds = 100 }
            }, false) } }, new Vector2(p.Position.X + distance, p.Position.Y - Player.Height / 2));
        private static MonsterDebuffDefinition Weakening(int time = 1000) => new MonsterDebuffDefinition {
            PhysicalAttackChange = -20, PhysicalDefenseChange = -20, DurationMilliseconds = time, RejectSameSource = true };

        [TestCase(4001002,1,5,7000)] [TestCase(4001002,20,10,60000)]
        [TestCase(4001334,1,8,0)] [TestCase(4001334,20,14,0)]
        public void OriginalRankDataHasExecutableCostsAndCapabilities(int id,int rank,int cost,int duration)
        {
            var info = Assets.SkillData.GetSkill(id); var data = info.Levels[rank];
            Assert.That(SkillValidation.Definition(info, Assets.SkillEffects, out var error), Is.True, error);
            Assert.That(data.MpCost, Is.EqualTo(cost)); Assert.That(data.Duration, Is.EqualTo(duration));
            Assert.That(info.Behavior.IsAttack, Is.True); Assert.That(info.IconPath, Does.Contain(id.ToString()));
            if (id == 4001002)
            {
                Assert.That(info.Behavior.DamagePolicy, Is.EqualTo(SkillDamagePolicy.None));
                Assert.That(data.TargetDebuff.PhysicalAttackChange, Is.EqualTo(-rank));
                Assert.That(data.TargetDebuff.PhysicalDefenseChange, Is.EqualTo(-rank));
                Assert.That(data.TargetDebuff.Visual.Frames.Length, Is.EqualTo(10));
                Assert.That(data.TargetDebuff.Visual.Frames[0].Path, Does.EndWith("4001002/mob/0"));
            }
            else { Assert.That(data.AttackCount, Is.EqualTo(2)); Assert.That(data.Damage, Is.EqualTo(rank == 1 ? 98 : 140)); Assert.That(info.RequiredWeaponType, Is.EqualTo(133)); }
        }
        [TestCase(10,10)] [TestCase(15,10)] [TestCase(16,15)] [TestCase(26,25)]
        public void DoubleStabUsesCharacterLevelHitArtwork(int level,int folder)
        {
            var effect = SourceSkillRules.EffectsFor(Assets.SkillData.GetSkill(4001334), level);
            Assert.That(effect.Hit.Frames.Length, Is.EqualTo(5));
            Assert.That(effect.Hit.Frames[0].Path, Does.Contain("/CharLevel/"+folder+"/hit/0/"));
        }
        [Test] public void EarnedDisorderThreeUnlocksDarkSightWithoutPractice()
        {
            // A fresh character follows normal level-up and first-job transactions.
            world = new GameWorld(null, new Maps(), assetProvider: Assets); world.LoadMap(42); p=world.Player; skills=world.SkillManager; Step(100);
            while (p.Level<10) p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(400,out _),Is.True); p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(skills.CanSpendSkillPoint(4001003,out _),Is.False);
            for(int i=0;i<3;i++)Assert.That(skills.TrySpendSkillPoint(4001002,out _),Is.True);
            Assert.That(skills.TrySpendSkillPoint(4001003,out _),Is.True);Assert.That(p.SkillPoints,Is.Zero);
            Assert.That(skills.UseSkill(4001003).Success,Is.True);Assert.That(p.IsHidden,Is.True);
        }
        [Test] public void DisorderWaitsForImpactChangesNoHpAndDoesNotRefreshAnAffectedTarget()
        {
            var combat=new Combat(new Rolls(),new Rolls());var first=Mob();var second=Mob(.5f);var victims=new List<Monster>{second,first};
            var info=Assets.SkillData.GetSkill(4001002);int damageEvents=0;
            combat.DamageDealt+=(_,__,___)=>damageEvents++;
            Assert.That(combat.PerformSkillAttack(p,victims,info,info.Levels[20]),Is.True);
            Assert.That(first.StatDebuff,Is.Null);
            while(p.BasicAttack.ElapsedMilliseconds+8<p.BasicAttack.HitDelayMilliseconds)combat.Update(.008f);
            Assert.That(first.StatDebuff,Is.Null);combat.Update(.008f);
            Assert.That(first.StatDebuff,Is.Not.Null);Assert.That(second.StatDebuff,Is.Null);
            Assert.That(first.HP,Is.EqualTo(10000));Assert.That(first.IsHit,Is.False);Assert.That(damageEvents,Is.Zero);
            var original=first.StatDebuff;Finish(combat);
            Assert.That(combat.PerformSkillAttack(p,new List<Monster>{first},info,info.Levels[20]),Is.True);Finish(combat);
            Assert.That(first.StatDebuff,Is.SameAs(original));Assert.That(original.RemainingMilliseconds,Is.EqualTo(60000));
        }
        [Test] public void DisorderMissAndTravelCancellationCannotApplyStatus()
        {
            var combat=new Combat(new Rolls(),new Rolls(.999));var mob=Mob();mob.Template.Avoidability=100000;
            var info=Assets.SkillData.GetSkill(4001002);int misses=0;combat.AttackResolved+=(_,__,hit)=>{if(hit.Miss)misses++;};
            combat.PerformSkillAttack(p,new List<Monster>{mob},info,info.Levels[20]);Finish(combat);
            Assert.That(mob.StatDebuff,Is.Null);Assert.That(misses,Is.EqualTo(1));
            mob.Template.Avoidability=0;combat.PerformSkillAttack(p,new List<Monster>{mob},info,info.Levels[20]);
            p.ResetMovementForMap();Finish(combat);Assert.That(mob.StatDebuff,Is.Null);
        }
        [Test] public void DoubleStabDeliversTwoTimedHitsToOneMonster()
        {
            var combat=new Combat(new Rolls(),new Rolls());var first=Mob();var second=Mob(.5f);int reports=0;
            var lines=new List<int>();combat.AttackResolved+=(_,target,hit)=>{reports++;lines.Add(hit.LineIndex);Assert.That(target,Is.SameAs(first));};
            var info=Assets.SkillData.GetSkill(4001334);
            Assert.That(combat.PerformSkillAttack(p,new List<Monster>{second,first},info,info.Levels[20]),Is.True);
            Assert.That(first.HP,Is.EqualTo(10000));Finish(combat);
            Assert.That(reports,Is.EqualTo(2));Assert.That(lines,Is.EqualTo(new[]{0,1}));
            Assert.That(first.HP,Is.LessThan(10000));Assert.That(second.HP,Is.EqualTo(10000));
        }
        [Test] public void CastCostsArePaidOnceAndWrongWeaponsConcealmentAndEmptyMpPreserveResources()
        {
            skills.SetSkillLevel(4001334,20);int mp=p.CurrentMP;
            Assert.That(skills.UseSkill(4001334).Success,Is.True);Assert.That(p.CurrentMP,Is.EqualTo(mp-14));
            Assert.That(skills.UseSkill(4001334).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(mp-14));Step(200);
            p.Inventory.AddItem(1302000,1);Assert.That(p.TryEquipItem(1302000,out _),Is.True);mp=p.CurrentMP;
            Assert.That(skills.UseSkill(4001334).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(mp));
            skills.SetSkillLevel(4001002,1);p.ApplyStatBuffs(7,"Hide",new Dictionary<BuffType,int>{{BuffType.Hide,1}},1000);
            Assert.That(skills.UseSkill(4001002).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(mp));
            p.RemoveStatBuffs(7);p.CurrentMP=4;Assert.That(skills.UseSkill(4001002).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(4));
        }
        [Test] public void WeakeningIsPerMonsterReplacesWithoutStackingAndExpiresOnSimulationTime()
        {
            var first=Mob();var second=Mob(.5f,first.Template);
            Assert.That(first.TryApplyDebuff(1,"Weak",Weakening(16)),Is.True);
            Assert.That(first.PhysicalAttack,Is.EqualTo(80));Assert.That(first.PhysicalDefense,Is.EqualTo(10));
            Assert.That(second.PhysicalAttack,Is.EqualTo(100));Assert.That(second.PhysicalDefense,Is.EqualTo(30));
            Assert.That(first.Template.PhysicalDamage,Is.EqualTo(100));Assert.That(first.Template.PhysicalDefense,Is.EqualTo(30));
            Assert.That(first.TryApplyDebuff(1,"Weak",Weakening()),Is.False);
            Assert.That(first.TryApplyDebuff(2,"Other",Weakening(16)),Is.True);Assert.That(first.PhysicalAttack,Is.EqualTo(80));
            first.UpdatePhysics(.008f,new Maps().GetMap(42));Assert.That(first.StatDebuff.RemainingMilliseconds,Is.EqualTo(8));
            first.UpdatePhysics(.008f,new Maps().GetMap(42));Assert.That(first.StatDebuff,Is.Null);Assert.That(first.PhysicalAttack,Is.EqualTo(100));
            first.TryApplyDebuff(1,"Weak",Weakening());first.TakeDamage(10000);Assert.That(first.StatDebuff,Is.Null);
        }
        [Test] public void WeakeningReducesContactDamageAndPhysicalDefenseButLeavesMagicDefenseAlone()
        {
            var mob=Mob(0);var original=p.PhysicalAttackStats;var random=new Rolls();
            var before=original.Roll(mob.Template,random).Damage;
            mob.TryApplyDebuff(1,"Weak",Weakening());
            var after=original.Roll(mob.Template,new Rolls(),mob.PhysicalDefense).Damage;
            Assert.That(after,Is.GreaterThan(before));
            var magic=new PhysicalAttackStats(100,200,100,10,0,true);
            Assert.That(magic.Roll(mob.Template,new Rolls(),mob.PhysicalDefense).Damage,Is.EqualTo(magic.Roll(mob.Template,new Rolls()).Damage));
            var defender=new Player {Position=p.Position,WeaponDefense=0,Avoidability=0};
            Assert.That(new Combat(contactRandom:new Rolls()).CheckContact(defender,new[]{mob}),Is.SameAs(mob));
            Assert.That(defender.CurrentHP,Is.EqualTo(28),"80 attack rolls 72 instead of the original 90 contact damage.");
        }
        private CustomSkillAsset Custom()
        {
            var a=UnityEngine.ScriptableObject.CreateInstance<CustomSkillAsset>();custom.Add(a);a.Id=185000000+custom.Count;
            a.DisplayName="Crippling Strike";a.JobId=400;a.Action="";a.Execution=SkillExecution.Attack;
            a.AttackFamily=SkillAttackFamily.Melee;a.DamagePolicy=SkillDamagePolicy.FixedPhysical;a.CastEffects=Array.Empty<string>();
            a.Ranks[0].Damage=100;a.Ranks[0].AttackCount=2;a.Ranks[0].ApplyTargetDebuff=true;
            a.Ranks[0].TargetPhysicalAttackChange=-15;a.Ranks[0].TargetPhysicalDefenseChange=-10;a.Ranks[0].TargetDebuffMilliseconds=1200;
            return a;
        }
        [TestCase(0)] [TestCase(100)]
        public void AuthoredTwoHitAttacksApplyTheSameDebuffOncePerMonster(int chance)
        {
            var a=Custom();a.Ranks[0].TargetDebuffChance=chance;
            Assert.That(((SkillCatalog)Assets.SkillData).TryRegister(a,out var error),Is.True,error);
            var info=Assets.SkillData.GetSkill(a.Id);var mob=Mob();var rolls=new Rolls();var combat=new Combat(new Rolls(),new Rolls(),effectRandom:rolls);
            combat.PerformSkillAttack(p,new List<Monster>{mob},info,info.Levels[1]);Finish(combat);
            Assert.That(mob.HP,Is.LessThan(10000));Assert.That(mob.StatDebuff!=null,Is.EqualTo(chance==100));
            Assert.That(rolls.Draws,Is.EqualTo(chance==100?0:1),"At most one proc attempt per cast/monster, independent of hit count.");
            if(chance==100)Assert.That(mob.PhysicalAttack,Is.EqualTo(85));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void InvalidAuthoredTargetEffectsAreRejectedBeforeRegistration(int invalid)
        {
            var a=Custom();var rank=a.Ranks[0];
            if(invalid==0)rank.TargetDebuffMilliseconds=0;
            if(invalid==1)rank.TargetPhysicalAttackChange=1;
            if(invalid==2)rank.TargetDebuffChance=101;
            if(invalid==3){a.Execution=SkillExecution.Self;a.CastEffects=new[]{SkillCastEffects.Recovery};rank.HealHp=10;}
            Assert.That(((SkillCatalog)Assets.SkillData).TryRegister(a,out _),Is.False);
        }
        [Test] public void DaggerPracticeIsRepeatableWithoutDuplicateSuppliesAndKeepsItsClaimAcrossSave()
        {
            p.TakeDamage(10);p.CurrentMP=20;skills.SetSkillLevel(4001334,10);
            Assert.That(world.RequestWeaponPractice(133,out var error),Is.True,error);int daggers=p.Inventory.GetItemCount(1332005),potions=p.Inventory.GetItemCount(2000003);
            Assert.That(world.RequestWeaponPractice(133,out error),Is.True,error);Assert.That(skills.GetSkillLevel(4001334),Is.EqualTo(10));
            Assert.That(p.Inventory.GetItemCount(1332005),Is.EqualTo(daggers));Assert.That(p.Inventory.GetItemCount(2000003),Is.EqualTo(potions));
            Assert.That(p.CurrentHP,Is.EqualTo(90));Assert.That(p.CurrentMP,Is.EqualTo(20));
            var save=world.CaptureProgress();Assert.That(save.RangedKits,Does.Contain(133));
            Assert.That(world.TryRestoreProgress(save,out error),Is.True,error);Assert.That(world.RequestWeaponPractice(133,out error),Is.True,error);
            Assert.That(p.Inventory.GetItemCount(2000003),Is.EqualTo(potions));
        }
    }
}
