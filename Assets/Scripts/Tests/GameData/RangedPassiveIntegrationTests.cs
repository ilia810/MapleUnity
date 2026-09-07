using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using UnityEngine;
using Vec=MapleClient.GameLogic.Vector2;
using Object=UnityEngine.Object;

namespace MapleClient.Tests.GameData
{
    public class RangedPassiveIntegrationTests
    {
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        private sealed class Rolls : System.Random { public override int Next()=>0; public override double NextDouble()=>0; }
        private sealed class MissRoll : System.Random { public override double NextDouble()=>.99; }
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id)=>new MapData {MapId=id,
                Platforms=new List<Platform>{new Platform{Id=1,X1=-3000,X2=3000,Y1=0,Y2=0}},
                Portals=new List<Portal>{new Portal{Id=0,Name="sp",Type=MapleClient.GameLogic.PortalType.Spawn}}};
        }
        private GameWorld world; private Player p; private SkillManager skills;
        [SetUp] public void Setup()
        {world=new GameWorld(null,new Maps(),assetProvider:Assets);world.LoadMap(42);p=world.Player;skills=world.SkillManager;Step(100);}
        private void Step(int ticks){for(int i=0;i<ticks;i++)world.UpdatePhysics(.008f);}
        private void Equip(int type)
        {Assert.That(world.RequestWeaponPractice(type,out _),Is.True);Assert.That(p.TryEquipItem(GameWorld.RangedPracticeWeapon(type),out _),Is.True);}
        private Combat Connect(List<Monster> targets)
        {
            var c=new Combat(new Rolls(),new Rolls());
            typeof(SkillManager).GetField("StartAttackSkill",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(skills,
                new Func<SkillInfo,SkillInfo.LevelData,bool>((s,l)=>c.PerformSkillAttack(p,targets,s,l)));return c;
        }
        private static Monster Mob(float x)=>new Monster(new MonsterTemplate {MaxHP=100000,Level=1,
            ContactAnimations=new Dictionary<string,MobContactAnimation>{["stand"]=new MobContactAnimation(new[]{
                new MobContactFrame{Left=-10,Right=10,Top=-20,Bottom=0,DelayMilliseconds=100}},false)}},new Vec(x,0));
        private static void Tick(Combat c,int ticks=600){for(int i=0;i<ticks;i++)c.Update(.008f);}
        [TestCase(3000002,145,15)] [TestCase(3000002,146,15)] [TestCase(4000001,147,25)]
        public void EveryRangeRankUsesPixelsAndRecomputesAfterJobWeaponAndRankChanges(int id,int weapon,int perRank)
        {
            Equip(weapon);int hp=p.CurrentHP,mp=p.CurrentMP;
            for(int rank=1;rank<=8;rank++)
            {Assert.That(skills.SetSkillLevel(id,rank),Is.True);Assert.That(p.ProjectileRangePixels,Is.EqualTo(400+perRank*rank));}
            Assert.That(skills.UseSkill(id).Success,Is.False);Assert.That(p.ActiveBuffs,Is.Empty);
            p.JobId=200;Assert.That(p.ProjectileRangePixels,Is.EqualTo(400));p.JobId=id/10000;
            Assert.That(p.TryUnequipItem(EquipSlot.Weapon,out _),Is.True);Assert.That(p.ProjectileRangePixels,Is.EqualTo(400));
            Assert.That(p.TryEquipItem(GameWorld.RangedPracticeWeapon(weapon),out _),Is.True);Assert.That(p.ProjectileRangePixels,Is.EqualTo(400+8*perRank));
            Assert.That(skills.SetSkillLevel(id,0),Is.True);Assert.That(p.ProjectileRangePixels,Is.EqualTo(400));
            Assert.That(p.CurrentHP,Is.EqualTo(hp));Assert.That(p.CurrentMP,Is.EqualTo(mp));
        }
        [TestCase(145,3000002,3001004,false)] [TestCase(145,3000002,3001004,true)]
        [TestCase(146,3000002,3001005,false)] [TestCase(146,3000002,3001005,true)]
        [TestCase(147,4000001,4001344,false)] [TestCase(147,4000001,4001344,true)]
        public void ProjectileSkillsRespectExpandedFrontBoundaryAndCannotHitBehind(int type,int passive,int attack,bool left)
        {
            Equip(type);skills.SetSkillLevel(passive,8);p.Position=new Vec(0,Player.Height/2);
            if(left){p.MoveLeft(true);Step(1);p.MoveLeft(false);p.Position=new Vec(0,Player.Height/2);}
            var data=Assets.SkillData.GetSkill(attack).Levels[20];float sign=left?-1:1;
            float limit=p.ProjectileRangePixels*data.Range/100f;
            var inside=Mob(sign*(limit+9)/100);var outside=Mob(sign*(limit+12)/100);var behind=Mob(-sign);
            var targets=new List<Monster>{outside,behind,inside};var c=Connect(targets);
            Assert.That(skills.UseSkill(attack).Success,Is.True);Tick(c);
            Assert.That(inside.HP,Is.LessThan(100000));Assert.That(outside.HP,Is.EqualTo(100000));Assert.That(behind.HP,Is.EqualTo(100000));
        }
        [TestCase(145,3000002,4.8f)] [TestCase(146,3000002,4.8f)] [TestCase(147,4000001,5.8f)]
        public void BasicShotsGainReachAndEmptyShotsTravelBeyondTheOldBoundary(int type,int passive,float distance)
        {
            Equip(type);p.Position=new Vec(0,Player.Height/2);var mob=Mob(distance);var targets=new List<Monster>{mob};var c=Connect(targets);
            Assert.That(c.PerformBasicAttack(p,targets,1),Is.Empty);Tick(c);Assert.That(mob.HP,Is.EqualTo(100000));
            skills.SetSkillLevel(passive,8);Assert.That(c.PerformBasicAttack(p,targets,1),Does.Contain(mob));Tick(c);Assert.That(mob.HP,Is.LessThan(100000));
            targets.Clear();c.PerformBasicAttack(p,targets,1);double farthest=0;
            for(int i=0;i<500;i++){c.Update(.008f);foreach(var shot in c.Projectiles)farthest=Math.Max(farthest,shot.SourceX);}
            Assert.That(farthest,Is.GreaterThan(p.ProjectileRangePixels-30));Assert.That(c.Projectiles,Is.Empty);
        }
        [TestCase(300,3000000,3000002)] [TestCase(400,4000000,4000001)]
        public void EarnedSkillPointsRespectRangePrerequisites(int job,int prerequisite,int id)
        {
            while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(job,out _),Is.True);p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            int points=p.SkillPoints;Assert.That(skills.TrySpendSkillPoint(id,out _),Is.False);Assert.That(p.SkillPoints,Is.EqualTo(points));
            for(int i=0;i<3;i++)Assert.That(skills.TrySpendSkillPoint(prerequisite,out _),Is.True);
            Assert.That(skills.TrySpendSkillPoint(id,out var error),Is.True,error);Assert.That(p.SkillPoints,Is.EqualTo(points-4));
            Assert.That(skills.GetSkillLevel(id),Is.EqualTo(1));
        }
        [TestCase(145)] [TestCase(146)]
        public void CriticalShotReplacesDefaultChanceAndDamageFromOriginalRankData(int type)
        {
            Equip(type);
            for(int rank=1;rank<=20;rank++)
            {
                skills.SetSkillLevel(3000001,rank);var node=Assets.GetNode("skill","300.img/skill/3000001/level/"+rank);
                Assert.That(p.CriticalChance,Is.EqualTo(node["prop"].GetValue<int>()/100f));
                Assert.That(p.CriticalDamageMultiplier,Is.EqualTo(node["damage"].GetValue<int>()/100f));
                Assert.That(p.PhysicalAttackStats.CriticalDamageMultiplier,Is.EqualTo(p.CriticalDamageMultiplier));
            }
            Assert.That(p.CriticalChance,Is.EqualTo(.4f));Assert.That(p.CriticalDamageMultiplier,Is.EqualTo(2));
            p.JobId=310;Assert.That(p.CriticalChance,Is.EqualTo(.4f));p.JobId=400;Assert.That(p.CriticalChance,Is.EqualTo(.05f));
            p.JobId=300;p.TryUnequipItem(EquipSlot.Weapon,out _);Assert.That(p.CriticalChance,Is.EqualTo(.05f));
            Assert.That(p.CriticalDamageMultiplier,Is.EqualTo(1.5f));
        }
        [Test] public void CriticalShotCanBeLearnedWithEarnedSpWithoutPrerequisite()
        {
            while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(300,out _),Is.True);Assert.That(skills.TrySpendSkillPoint(3000001,out _),Is.True);
            Assert.That(p.SkillPoints,Is.Zero);Assert.That(skills.UseSkill(3000001).Success,Is.False);
        }
        [Test] public void CriticalDamageIsCapturedBeforeAnArrowArrivesAndUnlearningRestoresDefaults()
        {
            Equip(145);skills.SetSkillLevel(3000001,20);p.Position=new Vec(0,Player.Height/2);var target=Mob(3);
            var c=Connect(new List<Monster>{target});var seen=new List<AttackHit>();c.AttackResolved+=(_,__,h)=>seen.Add(h);
            var info=Assets.SkillData.GetSkill(3001004);var stat=SkillRules.AttackStats(p,info,info.Levels[20]);
            int expected=stat.Roll(target.Template,new Rolls()).Damage;
            Assert.That(skills.UseSkill(3001004).Success,Is.True);Assert.That(seen,Is.Empty);skills.SetSkillLevel(3000001,0);
            p.WeaponAttack=999;Tick(c);Assert.That(seen.Single().Damage,Is.EqualTo(expected));Assert.That(seen.Single().Critical,Is.True);
            Assert.That(p.CriticalChance,Is.EqualTo(.05f));Assert.That(p.CriticalDamageMultiplier,Is.EqualTo(1.5f));
        }
        [Test] public void CriticalMultiplierKeepsMissBehaviorDamageCapAndNoncriticalDamage()
        {
            var target=new MonsterTemplate{Level=1};var hit=new PhysicalAttackStats(100,100,100,1,1,false,2).Roll(target,new Rolls());
            Assert.That(hit.Damage,Is.EqualTo(200));Assert.That(hit.Critical,Is.True);
            Assert.That(new PhysicalAttackStats(100,100,100,1,0,false,2).Roll(target,new Rolls()).Damage,Is.EqualTo(100));
            Assert.That(new PhysicalAttackStats(900000,900000,100,1,1,false,2).Roll(target,new Rolls()).Damage,Is.EqualTo(999999));
            target.Avoidability=100;hit=new PhysicalAttackStats(100,100,0,1,1,false,2).Roll(target,new MissRoll());Assert.That(hit.Miss,Is.True);Assert.That(hit.Critical,Is.False);
        }
        [Test] public void SaveRestoresLearnedPassivesWithoutSavingDerivedBonuses()
        {
            Equip(145);skills.SetSkillLevel(3000001,20);skills.SetSkillLevel(3000002,8);var save=world.CaptureProgress();
            skills.SetSkillLevel(3000001,0);skills.SetSkillLevel(3000002,0);
            Assert.That(world.TryRestoreProgress(save,out var error),Is.True,error);Assert.That(p.ProjectileRangePixels,Is.EqualTo(520));Assert.That(p.CriticalChance,Is.EqualTo(.4f));
            world.LoadMap(43);Assert.That(p.ProjectileRangePixels,Is.EqualTo(520));
        }
        [Test] public void CustomPassivesShareTheSameRangeAndCriticalRulesAndRejectInvalidValues()
        {
            var catalog=(SkillCatalog)Assets.SkillData;var asset=ScriptableObject.CreateInstance<CustomSkillAsset>();
            try
            {
                asset.Id=181000001;asset.DisplayName="Marksman instinct";asset.JobId=300;asset.Execution=SkillExecution.Passive;asset.AllowedWeapons=new[]{145};asset.CastEffects=Array.Empty<string>();
                var r=asset.Ranks[0];r.PassiveProjectileRangeBonus=160;r.PassiveCriticalChance=.8f;r.PassiveCriticalDamageMultiplier=3;
                Assert.That(catalog.TryRegister(asset,out var error),Is.True,error);Equip(145);skills.SetSkillLevel(asset.Id,1);skills.SetSkillLevel(3000001,20);skills.SetSkillLevel(3000002,8);
                Assert.That(p.ProjectileRangePixels,Is.EqualTo(680));Assert.That(p.CriticalChance,Is.EqualTo(.8f));Assert.That(p.CriticalDamageMultiplier,Is.EqualTo(3));
                var stats=SkillRules.AttackStats(p,new SkillInfo{Behavior=new SkillBehavior{DamagePolicy=SkillDamagePolicy.FixedMagic}},new SkillInfo.LevelData{Damage=100});
                Assert.That(stats.Roll(new MonsterTemplate{Level=1},new Rolls()).Damage,Is.EqualTo(300));
                catalog.RemoveCustom(asset.Id);r.PassiveCriticalChance=float.NaN;Assert.That(catalog.TryRegister(asset,out _),Is.False);
                r.PassiveCriticalChance=.8f;r.PassiveCriticalDamageMultiplier=.5f;Assert.That(catalog.TryRegister(asset,out _),Is.False);
                r.PassiveCriticalDamageMultiplier=3;r.PassiveProjectileRangeBonus=-1;Assert.That(catalog.TryRegister(asset,out _),Is.False);
            }
            finally{catalog.RemoveCustom(asset.Id);Object.DestroyImmediate(asset);}
        }
    }
}
