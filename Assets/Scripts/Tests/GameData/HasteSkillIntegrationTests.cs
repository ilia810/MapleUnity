using System.Collections.Generic;
using System.Linq;
using GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class HasteSkillIntegrationTests
    {
        private static NXDataManager Assets=>NXDataManagerSingleton.Instance.DataManager;
        private sealed class Maps:IMapLoader
        {
            public MapData GetMap(int id)=>new MapData{MapId=id,
                Platforms=new List<Platform>{new Platform{Id=1,X1=-30000,X2=30000,Y1=0,Y2=0}},
                Portals=new List<Portal>{new Portal{Id=0,Name="sp",Type=MapleClient.GameLogic.PortalType.Spawn}}};
        }
        private GameWorld world;private Player p;private SkillManager skills;
        [SetUp] public void Setup(){world=new GameWorld(null,new Maps(),assetProvider:Assets);world.LoadMap(42);p=world.Player;skills=world.SkillManager;Step(100);}
        private void Step(int n){for(int i=0;i<n;i++)world.UpdatePhysics(.008f);}
        private void Cast(int id,int rank){p.JobId=id/10000;skills.SetSkillLevel(id,rank);var result=skills.UseSkill(id);Assert.That(result.Success,Is.True,result.ErrorMessage);}
        [TestCase(4101004,1,15,10,2,1)] [TestCase(4101004,20,30,200,40,20)]
        [TestCase(4201003,1,15,10,2,1)] [TestCase(4201003,20,30,200,40,20)]
        public void OriginalHasteCostsDurationsArtAndCappedStatsAreActive(int id,int rank,int cost,int seconds,int speed,int jump)
        {
            int mp=p.CurrentMP;p.JumpPower=100;Cast(id,rank);var info=Assets.SkillData.GetSkill(id);
            Assert.That(p.Speed,Is.EqualTo(100+speed));Assert.That(p.JumpPower,Is.EqualTo(100+jump));
            Assert.That(p.CurrentMP,Is.EqualTo(mp-cost));Assert.That(p.CurrentHP,Is.EqualTo(100));
            Assert.That(p.ActiveBuffs.Count(),Is.EqualTo(2));Assert.That(p.ActiveBuffs.All(b=>b.RemainingMilliseconds==seconds*1000),Is.True);
            Assert.That(info.Action,Is.EqualTo("alert2"));Assert.That(p.SkillEffect,Is.Not.Null);
            Assert.That(info.EffectFrames.Length,Is.EqualTo(9));
            foreach(var f in info.EffectFrames)Assert.That(Assets.GetNode("skill",f.Path).Value,Is.TypeOf<byte[]>());
            p.Speed=139;p.JumpPower=122;Assert.That(p.Speed,Is.EqualTo(140));Assert.That(p.JumpPower,Is.EqualTo(123));
            Assert.That(skills.UseSkill(id).Success,Is.False);Assert.That(p.CurrentMP,Is.EqualTo(mp-cost));
        }
        [TestCase(4101004)] [TestCase(4201003)]
        public void HasteActuallyIncreasesWalkVelocityAndJumpApex(int id)
        {
            p.JumpPower=100;p.MoveRight(true);Step(180);float normalSpeed=p.Velocity.X;p.MoveRight(false);Step(200);
            p.Jump();float normalApex=p.Position.Y;for(int i=0;i<180;i++){Step(1);normalApex=System.Math.Max(normalApex,p.Position.Y);}p.ReleaseJump();Step(100);
            Cast(id,20);Step(150);p.MoveRight(true);Step(180);Assert.That(p.Velocity.X,Is.GreaterThan(normalSpeed*1.2f));p.MoveRight(false);Step(200);
            p.Jump();float hasteApex=p.Position.Y;for(int i=0;i<180;i++){Step(1);hasteApex=System.Math.Max(hasteApex,p.Position.Y);}p.ReleaseJump();
            Assert.That(hasteApex,Is.GreaterThan(normalApex+.2f));
        }
        [Test] public void RecastRefreshesWithoutStackingAndSimulationPauseDoesNotExpireIt()
        {
            Cast(4101004,1);Step(625);p.CurrentMP=50;Cast(4101004,1);Assert.That(p.Speed,Is.EqualTo(102));
            Assert.That(p.ActiveBuffs.All(b=>b.RemainingMilliseconds==10000),Is.True);skills.Update(60);Assert.That(p.Speed,Is.EqualTo(102));
            Step(1249);Assert.That(p.Speed,Is.EqualTo(102));Step(1);Assert.That(p.Speed,Is.EqualTo(100));Assert.That(p.JumpPower,Is.EqualTo(120));
        }
        [Test] public void DarkSightAndItemSpeedReplaceOnlyTheirOwnContributionAndCancelIndependently()
        {
            Cast(4101004,20);Step(100);p.CurrentMP=50;Cast(4001003,1);
            Assert.That(p.IsHidden,Is.True);Assert.That(p.Speed,Is.EqualTo(70));Assert.That(p.JumpPower,Is.EqualTo(123));
            Assert.That(skills.TryCancelBuff(4101004),Is.True);Assert.That(p.IsHidden,Is.True);Assert.That(p.Speed,Is.EqualTo(70));Assert.That(p.JumpPower,Is.EqualTo(120));
            skills.TryCancelBuff(4001003);Step(100);p.CurrentMP=50;Cast(4201003,20);
            p.ApplyStatBuffs(-2002001,"Speed Potion",new Dictionary<BuffType,int>{{BuffType.Speed,8}},80);
            Assert.That(p.Speed,Is.EqualTo(108));Assert.That(p.JumpPower,Is.EqualTo(123));Step(10);
            Assert.That(p.Speed,Is.EqualTo(100));Assert.That(p.JumpPower,Is.EqualTo(123));
            skills.TryCancelBuff(4201003);Assert.That(p.ActiveBuffs,Is.Empty);
        }
        [Test] public void TravelPreservesTheTimerButSaveLoadAndDeathClearTemporaryStats()
        {
            Cast(4101004,20);Step(100);var save=world.CaptureProgress();int remaining=p.ActiveBuffs.First().RemainingMilliseconds;
            world.LoadMap(43);Assert.That(p.ActiveBuffs.First().RemainingMilliseconds,Is.EqualTo(remaining));
            Assert.That(world.TryRestoreProgress(save,out var error),Is.True,error);Assert.That(p.ActiveBuffs,Is.Empty);Assert.That(p.Speed,Is.EqualTo(100));
            Step(100);p.CurrentMP=50;Cast(4101004,20);p.TakeDamage(int.MaxValue);Assert.That(p.ActiveBuffs,Is.Empty);Assert.That(p.Speed,Is.EqualTo(100));
        }
        [TestCase(410,4101004)] [TestCase(420,4201003)]
        public void PresetsKeepEarnedProgressAndHigherRanksWhileSpAwaitsSecondJobProgression(int job,int id)
        {
            p.TakeDamage(10);p.CurrentMP=31;skills.SetSkillLevel(id,12);int sp=p.SkillPoints;var save=world.CaptureProgress();
            Assert.That(world.RequestSupportPractice(job,out var error),Is.True,error);Assert.That(world.RequestSupportPractice(job,out _),Is.True);
            Assert.That(p.JobId,Is.EqualTo(job));Assert.That(skills.GetSkillLevel(id),Is.EqualTo(12));Assert.That(p.CurrentHP,Is.EqualTo(90));Assert.That(p.CurrentMP,Is.EqualTo(31));
            Assert.That(p.AbilityPoints,Is.EqualTo(save.Player.AbilityPoints));Assert.That(p.SkillPoints,Is.EqualTo(sp));Assert.That(p.Level,Is.EqualTo(save.Player.Level));
            Assert.That(skills.CanSpendSkillPoint(id,out _),Is.False);p.JobId=400;Assert.That(skills.UseSkill(id).Success,Is.False);
            p.JobId=job;p.CurrentMP=0;Assert.That(skills.UseSkill(id).Success,Is.False);Assert.That(p.ActiveBuffs,Is.Empty);Assert.That(p.IsBasicAttacking,Is.False);
        }
    }
}
