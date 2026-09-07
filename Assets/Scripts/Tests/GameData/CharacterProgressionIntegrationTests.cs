using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Skills;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class CharacterProgressionIntegrationTests
    {
        private sealed class Maps:IMapLoader
        {
            private readonly MapData map=new MapData{MapId=42,
                Platforms=new List<Platform>{new Platform{Id=1,X1=-3000,X2=3000,Y1=0,Y2=0}},
                Portals=new List<Portal>{new Portal{Id=0,Name="sp",Type=MapleClient.GameLogic.PortalType.Spawn}}};
            public MapData GetMap(int id)=>id==42?map:null;
        }
        private GameWorld world;
        private Player player;
        private SkillManager skills;
        [SetUp] public void Setup()
        {
            world=new GameWorld(null,new Maps(),assetProvider:NXDataManagerSingleton.Instance.DataManager);world.LoadMap(42);
            player=world.Player;skills=world.SkillManager;
        }
        private void EarnTo(int level){while(player.Level<level)player.AddExperience(player.ExperienceToNextLevel-player.Experience);}
        private void Advance(int job)
        {EarnTo(OfflineProgression.Job(job).Level);Assert.That(world.TryAdvanceFirstJob(job,out var result),Is.True,result);}
        [Test] public void JobAdvancementEventRequiresACompletedTransactionAndNeverReplaysOnLoad()
        {
            var jobs=new List<int>();world.FirstJobAdvanced+=jobs.Add;
            Assert.That(world.TryAdvanceFirstJob(200,out _),Is.False);Assert.That(jobs,Is.Empty);
            Advance(200);Assert.That(jobs,Is.EqualTo(new[]{200}));
            Assert.That(world.TryAdvanceFirstJob(200,out _),Is.False);
            Assert.That(world.TryRestoreProgress(world.CaptureProgress(),out _),Is.True);
            Assert.That(jobs,Is.EqualTo(new[]{200}));
        }
        [TestCase(PrimaryAttribute.STR)] [TestCase(PrimaryAttribute.DEX)] [TestCase(PrimaryAttribute.INT)] [TestCase(PrimaryAttribute.LUK)]
        public void EarnedLevelsProvideFiveApAndEachClickSpendsExactlyOne(PrimaryAttribute stat)
        {
            Assert.That(world.TrySpendAbilityPoint(stat,out _),Is.False);int initial=player.BaseAttribute(stat);
            EarnTo(2);Assert.That(player.AbilityPoints,Is.EqualTo(5));
            for(int i=0;i<5;i++)Assert.That(world.TrySpendAbilityPoint(stat,out _),Is.True);
            Assert.That(player.BaseAttribute(stat),Is.EqualTo(initial+5));Assert.That(player.AbilityPoints,Is.Zero);
            Assert.That(world.TrySpendAbilityPoint(stat,out _),Is.False);Assert.That(player.BaseAttribute(stat),Is.EqualTo(initial+5));
        }
        [Test] public void AttributeCapsAndInvalidRequestsPreserveUnspentAp()
        {
            EarnTo(2);player.STR=998;
            Assert.That(world.TrySpendAbilityPoint(PrimaryAttribute.STR,out _),Is.True);
            Assert.That(world.TrySpendAbilityPoint(PrimaryAttribute.STR,out _),Is.False);
            Assert.That(world.TrySpendAbilityPoint((PrimaryAttribute)99,out _),Is.False);
            Assert.That(player.STR,Is.EqualTo(999));Assert.That(player.AbilityPoints,Is.EqualTo(4));
        }
        [Test] public void CatchUpLevelsAwardOnceAndPreviouslyRewardedLevelsCannotBeFarmedAgain()
        {
            player.AddExperience(ExperienceTable.RequiredForLevel(1)+ExperienceTable.RequiredForLevel(2)+7);
            Assert.That(player.Level,Is.EqualTo(3));Assert.That(player.AbilityPoints,Is.EqualTo(10));Assert.That(player.Experience,Is.EqualTo(7));
            player.Level=1;EarnTo(3);Assert.That(player.AbilityPoints,Is.EqualTo(10));
            EarnTo(4);Assert.That(player.AbilityPoints,Is.EqualTo(15));
        }
        [Test] public void LevelCapAwardsTheLastEarnedLevelOnlyOnce()
        {
            player.Level=199;EarnTo(200);Assert.That(player.AbilityPoints,Is.EqualTo(5));
            player.AddExperience(long.MaxValue);Assert.That(player.AbilityPoints,Is.EqualTo(5));Assert.That(player.Experience,Is.Zero);
        }
        [TestCase(100)] [TestCase(200)] [TestCase(300)] [TestCase(400)] [TestCase(500)]
        public void EachFirstJobCanBeEarnedAndItsStarterWeaponAndFirstSkillAreUsable(int jobId)
        {
            var job=OfflineProgression.Job(jobId);EarnTo(job.Level-1);
            Assert.That(world.TryAdvanceFirstJob(jobId,out _),Is.False);Assert.That(player.JobId,Is.Zero);
            EarnTo(job.Level);int ap=player.AbilityPoints;
            Assert.That(world.TryAdvanceFirstJob(jobId,out var message),Is.True,message);
            Assert.That(player.JobId,Is.EqualTo(jobId));Assert.That(player.HasChosenFirstJob,Is.True);
            Assert.That(player.SkillPoints,Is.EqualTo(1));Assert.That(player.AbilityPoints,Is.EqualTo(ap));
            var weapon=player.GetItemInfo(job.WeaponId);
            foreach(var requirement in new[]{Tuple.Create(PrimaryAttribute.STR,weapon.RequiredStr),Tuple.Create(PrimaryAttribute.DEX,weapon.RequiredDex),
                Tuple.Create(PrimaryAttribute.INT,weapon.RequiredInt),Tuple.Create(PrimaryAttribute.LUK,weapon.RequiredLuk)})
                while(player.BaseAttribute(requirement.Item1)<requirement.Item2)
                    Assert.That(world.TrySpendAbilityPoint(requirement.Item1,out message),Is.True,message);
            Assert.That(player.TryEquipItem(job.WeaponId,out message),Is.True,message);
            Assert.That(skills.TrySpendSkillPoint(job.FirstSkillId,out message),Is.True,message);
            Assert.That(skills.GetSkillLevel(job.FirstSkillId),Is.EqualTo(1));Assert.That(player.SkillPoints,Is.Zero);
            if(job.AmmunitionId!=0){Assert.That(player.AmmunitionId,Is.EqualTo(job.AmmunitionId));Assert.That(player.AmmunitionCount,Is.EqualTo(100));}
            int count=player.Inventory.GetItemCount(job.WeaponId);
            Assert.That(world.TryAdvanceFirstJob(jobId,out _),Is.False);Assert.That(world.TryAdvanceFirstJob(100,out _),Is.False);
            Assert.That(player.Inventory.GetItemCount(job.WeaponId),Is.EqualTo(count));Assert.That(player.SkillPoints,Is.Zero);
        }
        [Test] public void FullBagsRejectAdvancementWithoutLosingOrDuplicatingAnyRewards()
        {
            EarnTo(10);player.Inventory.AddItem(1302000,Inventory.SlotsPerCategory-1);player.Inventory.AddItem(2000000,Inventory.SlotsPerCategory*100);
            int ap=player.AbilityPoints;
            Assert.That(world.TryAdvanceFirstJob(300,out _),Is.False);
            Assert.That(player.JobId,Is.Zero);Assert.That(player.HasChosenFirstJob,Is.False);Assert.That(player.AbilityPoints,Is.EqualTo(ap));
            Assert.That(player.SkillPointsForJob(300),Is.Zero);Assert.That(player.Inventory.GetItemCount(1452002),Is.Zero);
            player.Inventory.RemoveItem(2000000,100);
            Assert.That(world.TryAdvanceFirstJob(300,out _),Is.True);Assert.That(player.Inventory.GetItemCount(1452002),Is.EqualTo(1));
            Assert.That(player.Inventory.GetItemCount(2060000),Is.EqualTo(100));
        }
        [TestCase(100,1001004,1001005)] [TestCase(200,2001004,2001005)] [TestCase(300,3001004,3001005)]
        public void RealPrerequisiteChainsStayVisibleAndSpendOnlyAfterTheirRequirementsAreMet(int job,int first,int second)
        {
            Advance(job);Assert.That(skills.GetAvailableSkills().ContainsKey(second),Is.True);
            Assert.That(skills.TrySpendSkillPoint(second,out _),Is.False);Assert.That(player.SkillPoints,Is.EqualTo(1));
            Assert.That(skills.TrySpendSkillPoint(first,out _),Is.True);Assert.That(skills.TrySpendSkillPoint(second,out _),Is.False);
            EarnTo(player.Level+1);Assert.That(player.SkillPoints,Is.EqualTo(3));
            Assert.That(skills.TrySpendSkillPoint(second,out _),Is.True);Assert.That(skills.GetSkillLevel(second),Is.EqualTo(1));
            Assert.That(skills.TrySpendSkillPoint(first,out _),Is.True);Assert.That(skills.GetSkillLevel(first),Is.EqualTo(2));
            Assert.That(player.SkillPoints,Is.EqualTo(1));
        }
        [TestCase(1000000)] [TestCase(1000001)] [TestCase(1001003)] [TestCase(2001004)] [TestCase(1000)] [TestCase(12)] [TestCase(9999999)]
        public void UnsupportedWrongJobAndBeginnerSkillsCannotSpendFirstJobSp(int skill)
        {
            Advance(100);Assert.That(skills.TrySpendSkillPoint(skill,out _),Is.False);
            Assert.That(skills.GetSkillLevel(skill),Is.Zero);Assert.That(player.SkillPoints,Is.EqualTo(1));
        }
        [Test] public void SkillMaximaPreserveSurplusPoints()
        {
            Advance(100);EarnTo(20);
            for(int i=0;i<20;i++)Assert.That(skills.TrySpendSkillPoint(1001004,out _),Is.True);
            int points=player.SkillPoints;Assert.That(points,Is.GreaterThan(0));
            Assert.That(skills.TrySpendSkillPoint(1001004,out _),Is.False);Assert.That(skills.GetSkillLevel(1001004),Is.EqualTo(20));Assert.That(player.SkillPoints,Is.EqualTo(points));
        }
        [Test] public void PracticeJumpsGrantNoPointsAndJobSwitchesKeepSeparatePools()
        {
            Assert.That(world.RequestPracticeMagicSkills(out _),Is.True);Assert.That(player.Level,Is.EqualTo(8));
            Assert.That(player.AbilityPoints,Is.Zero);Assert.That(player.SkillPoints,Is.Zero);
            EarnTo(9);Assert.That(player.AbilityPoints,Is.EqualTo(5));Assert.That(player.SkillPoints,Is.EqualTo(3));
            Assert.That(world.RequestRangedPractice(145,out _),Is.True);Assert.That(player.SkillPoints,Is.Zero);Assert.That(player.AbilityPoints,Is.EqualTo(5));
            EarnTo(11);Assert.That(player.SkillPoints,Is.EqualTo(3));Assert.That(player.SkillPointsForJob(200),Is.EqualTo(3));
            Assert.That(world.RequestRangedPractice(145,out _),Is.True);Assert.That(player.SkillPoints,Is.EqualTo(3));
            Assert.That(skills.TrySpendSkillPoint(2001004,out _),Is.False);Assert.That(player.SkillPointsForJob(200),Is.EqualTo(3));
        }
        [Test] public void ActiveAttacksAndDefeatBlockPointAllocation()
        {
            Advance(100);Assert.That(player.TryEquipItem(1302000,out _),Is.True);
            var combat=new Combat();combat.PerformBasicAttack(player,new List<Monster>(),1);Assert.That(player.IsBasicAttacking,Is.True);
            int ap=player.AbilityPoints;Assert.That(world.TrySpendAbilityPoint(PrimaryAttribute.STR,out _),Is.False);
            Assert.That(skills.TrySpendSkillPoint(1001004,out _),Is.False);player.ResetMovementForMap();player.TakeDamage(int.MaxValue);
            Assert.That(world.TrySpendAbilityPoint(PrimaryAttribute.STR,out _),Is.False);Assert.That(skills.TrySpendSkillPoint(1001004,out _),Is.False);
            Assert.That(player.AbilityPoints,Is.EqualTo(ap));Assert.That(player.SkillPoints,Is.EqualTo(1));
        }
        [Test] public void VersionTwoRoundTripPreservesPointsBaseStatsSkillsAndAdvancementWithoutRegranting()
        {
            Advance(200);world.TrySpendAbilityPoint(PrimaryAttribute.INT,out _);skills.TrySpendSkillPoint(2001004,out _);EarnTo(10);
            var saved=world.CaptureProgress();Assert.That(saved.Version,Is.EqualTo(5));int ap=player.AbilityPoints,sp=player.SkillPoints,intelligence=player.BaseAttribute(PrimaryAttribute.INT);
            world.TrySpendAbilityPoint(PrimaryAttribute.INT,out _);skills.TrySpendSkillPoint(2001005,out _);
            for(int i=0;i<2;i++)Assert.That(world.TryRestoreProgress(saved,out var result),Is.True,result);
            Assert.That(player.AbilityPoints,Is.EqualTo(ap));Assert.That(player.SkillPoints,Is.EqualTo(sp));Assert.That(player.BaseAttribute(PrimaryAttribute.INT),Is.EqualTo(intelligence));
            Assert.That(skills.GetSkillLevel(2001004),Is.EqualTo(1));Assert.That(skills.GetSkillLevel(2001005),Is.Zero);
            EarnTo(11);Assert.That(player.AbilityPoints,Is.EqualTo(ap+5));Assert.That(player.SkillPoints,Is.EqualTo(sp+3));
        }
        [Test] public void VersionOneCharactersRetainTheirProgressAndStartEarningPointsAtTheirNextLevel()
        {
            Assert.That(world.RequestPracticeMagicSkills(out _),Is.True);player.STR=29;
            var legacy=world.CaptureProgress();legacy.Version=1;legacy.Player.SkillPointPools=null;
            legacy.Player.HighestRewardedLevel=legacy.Player.AbilityPoints=0;
            EarnTo(9);Assert.That(world.TryRestoreProgress(legacy,out var result),Is.True,result);
            Assert.That(player.Level,Is.EqualTo(8));Assert.That(player.STR,Is.EqualTo(29));Assert.That(skills.GetSkillLevel(2001004),Is.EqualTo(20));
            Assert.That(player.AbilityPoints,Is.Zero);Assert.That(player.SkillPoints,Is.Zero);
            EarnTo(9);Assert.That(player.AbilityPoints,Is.EqualTo(5));Assert.That(player.SkillPoints,Is.EqualTo(3));
            Assert.That(world.CaptureProgress().Version,Is.EqualTo(5));
        }
        [TestCase("ap-negative")] [TestCase("ap-overflow")] [TestCase("sp-negative")] [TestCase("sp-overflow")]
        [TestCase("pools-missing")] [TestCase("pools-length")] [TestCase("reward-zero")] [TestCase("reward-overflow")]
        public void InvalidProgressionSavesLeaveTheLiveCharacterUntouched(string fault)
        {
            Advance(200);int ap=player.AbilityPoints;var saved=world.CaptureProgress();var p=saved.Player;
            if(fault=="ap-negative")p.AbilityPoints=-1;if(fault=="ap-overflow")p.AbilityPoints=int.MaxValue;
            if(fault=="sp-negative")p.SkillPointPools[0]=-1;if(fault=="sp-overflow")p.SkillPointPools[0]=int.MaxValue;
            if(fault=="pools-missing")p.SkillPointPools=null;if(fault=="pools-length")p.SkillPointPools=new int[4];
            if(fault=="reward-zero")p.HighestRewardedLevel=0;if(fault=="reward-overflow")p.HighestRewardedLevel=201;
            Assert.That(world.TryRestoreProgress(saved,out _),Is.False);Assert.That(player.AbilityPoints,Is.EqualTo(ap));
            Assert.That(player.JobId,Is.EqualTo(200));Assert.That(player.SkillPoints,Is.EqualTo(1));Assert.That(world.CurrentMapId,Is.EqualTo(42));
        }
        [Test] public void DiskStoreReadsVersionOneAndUpgradesTheNextSaveWithALegacyBackup()
        {
            EarnTo(8);var legacy=world.CaptureProgress();legacy.Version=1;legacy.Player.SkillPointPools=null;
            legacy.Player.AbilityPoints=legacy.Player.HighestRewardedLevel=0;
            string path=Path.Combine(Path.GetTempPath(),"maple-progression-"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                File.WriteAllText(path,UnityEngine.JsonUtility.ToJson(legacy));
                var store=new LocalSaveStore(path);Assert.That(store.TryRead(out var read,out _),Is.True);Assert.That(read.Version,Is.EqualTo(1));
                Assert.That(world.TryRestoreProgress(read,out _),Is.True);Assert.That(player.AbilityPoints,Is.Zero);
                EarnTo(9);Assert.That(player.AbilityPoints,Is.EqualTo(5));
                Assert.That(store.TryWrite(world.CaptureProgress(),out _),Is.True);
                Assert.That(store.TryRead(out read,out _),Is.True);Assert.That(read.Version,Is.EqualTo(5));Assert.That(read.Player.AbilityPoints,Is.EqualTo(5));
                Assert.That(new LocalSaveStore(path+".bak").TryRead(out var backup,out _),Is.True);Assert.That(backup.Version,Is.EqualTo(1));
                Assert.That(world.TryRestoreProgress(read,out _),Is.True);Assert.That(player.AbilityPoints,Is.EqualTo(5));
            }
            finally{foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);}
        }
    }
}
