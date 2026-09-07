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

namespace MapleClient.Tests.GameData
{
    public class SupportSkillIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => new MapData { MapId = id,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } };
        }
        private GameWorld world;
        private Player p;
        private SkillManager skills;
        private static NXDataManager Assets => NXDataManagerSingleton.Instance.DataManager;
        [SetUp] public void Setup()
        {
            world = new GameWorld(null, new Maps(), assetProvider: Assets); world.LoadMap(42);
            p = world.Player; skills = world.SkillManager; Step(100);
        }
        private void Step(int ticks) { for (int i = 0; i < ticks; i++) world.UpdatePhysics(.008f); }
        private void Cast(int id, int level = 1)
        {
            p.JobId = id / 10000; skills.SetSkillLevel(id, level);
            var result = skills.UseSkill(id); Assert.That(result.Success, Is.True, result.ErrorMessage);
        }

        [TestCase(1001003,8,75000,BuffType.WeaponDefense,2)]
        [TestCase(2001002,6,111000,BuffType.MagicGuard,11)]
        [TestCase(2001003,8,54000,BuffType.WeaponDefense,2)]
        [TestCase(1101006,12,46000,BuffType.WeaponAttack,3)]
        [TestCase(1301006,12,15000,BuffType.MagicDefense,1)]
        [TestCase(1301007,20,10000,BuffType.MaxHPPercent,2)]
        [TestCase(2101001,10,10000,BuffType.MagicAttack,1)]
        [TestCase(2201001,10,10000,BuffType.MagicAttack,1)]
        public void RealNxBuffsCastWithAuthoredCostsStatsActionsAndArt(int id,int cost,int duration,BuffType type,int value)
        {
            var info = Assets.SkillData.GetSkill(id);
            Assert.That(info.Levels[1].Buffs[type], Is.EqualTo(value));
            int hp=p.CurrentHP, mp=p.CurrentMP;
            Cast(id);
            Assert.That(p.CurrentHP, Is.EqualTo(hp)); Assert.That(p.CurrentMP, Is.EqualTo(mp-cost));
            Assert.That(p.ActiveBuffs.Single(b=>b.Type==type).RemainingMilliseconds, Is.EqualTo(duration));
            Assert.That(p.BasicAttack.Stance, Is.EqualTo(CharacterState.Alert));
            Assert.That(skills.UseSkill(id).Success, Is.False); Assert.That(p.CurrentMP, Is.EqualTo(mp-cost));
            if (SourceSkillRules.IsArmorEcho(id)) Assert.That(p.ArmorEchoMilliseconds, Is.EqualTo(500));
            else
            {
                Assert.That(p.SkillEffect, Is.Not.Null);
                foreach(var frame in info.EffectFrames)
                    Assert.That(SpriteLoader.LoadSpriteWithShift(NXAssetLoader.Instance.GetNxFile("skill").GetNode(frame.Path),
                        UnityEngine.Vector2.zero,"skill/"+frame.Path), Is.Not.Null, frame.Path);
            }
        }

        [TestCase(100,80,20)] [TestCase(9,7,2)] [TestCase(1,0,1)]
        public void MagicGuardUsesPostDefenseDamageAndIntegerMpTransfer(int damage,int mpLoss,int hpLoss)
        {
            p.MaxMP=200; p.CurrentMP=200; Cast(2001002,20); int mp=p.CurrentMP;
            Assert.That(p.ReceiveContactDamage(damage,false),Is.True);
            Assert.That(p.CurrentMP,Is.EqualTo(mp-mpLoss)); Assert.That(p.CurrentHP,Is.EqualTo(100-hpLoss));
            Assert.That(p.ReceiveContactDamage(damage,false),Is.False); Assert.That(p.CurrentMP,Is.EqualTo(mp-mpLoss));
            Assert.That(p.InvulnerableMilliseconds,Is.EqualTo(2000));
        }
        [TestCase(0)] [TestCase(7)]
        public void MagicGuardSpillsMpShortageToHpWithoutBlockingTheContact(int available)
        {
            Cast(2001002,20); p.CurrentMP=available;
            Assert.That(p.ReceiveContactDamage(20,false),Is.True);
            Assert.That(p.CurrentMP,Is.Zero); Assert.That(p.CurrentHP,Is.EqualTo(80+available));
        }
        [Test] public void GuardDoesNotInterceptSkillCostsOrDirectDamageAndDeathClearsBuffs()
        {
            Cast(2001002,20); Step(100); int mp=p.CurrentMP;
            p.TakeDamage(20); Assert.That(p.CurrentHP,Is.EqualTo(80)); Assert.That(p.CurrentMP,Is.EqualTo(mp));
            Cast(2001003); Assert.That(p.CurrentMP,Is.EqualTo(mp-8));
            p.TakeDamage(int.MaxValue); Assert.That(p.IsDead,Is.True); Assert.That(p.ActiveBuffs,Is.Empty);
            Assert.That(p.ArmorEchoMilliseconds,Is.Zero); p.Revive(); Assert.That(p.WeaponDefense,Is.EqualTo(10));
        }
        [Test] public void StatReplacementKeepsUnrelatedBuffsAndDoesNotAccumulateBaseValues()
        {
            Cast(1101006,20); Assert.That(p.WeaponAttack,Is.EqualTo(12)); Assert.That(p.WeaponDefense,Is.EqualTo(-2));
            Step(100); p.CurrentMP=p.MaxMP; Cast(1301006,20);
            Assert.That(p.WeaponAttack,Is.EqualTo(12)); Assert.That(p.WeaponDefense,Is.EqualTo(30)); Assert.That(p.MagicDefense,Is.EqualTo(30));
            p.RemoveStatBuffs(1101006); Assert.That(p.WeaponAttack,Is.Zero); Assert.That(p.WeaponDefense,Is.EqualTo(30));
            p.RemoveStatBuffs(1301006); Assert.That(p.WeaponDefense,Is.EqualTo(10)); Assert.That(p.MagicDefense,Is.EqualTo(10));
            Step(100); p.CurrentMP=50; Cast(2101001,20); Assert.That(p.MagicAttack,Is.EqualTo(20));
            p.ApplyStatBuffs(-2002002,"Magic Potion",new Dictionary<BuffType,int>{{BuffType.MagicAttack,5}},16);
            Assert.That(p.MagicAttack,Is.EqualTo(5)); Assert.That(skills.IsBuffActive(2101001),Is.False);
            Step(2); Assert.That(p.MagicAttack,Is.Zero);
        }
        [Test] public void HyperBodyUsesPercentagesCapsAndNeverHealsOnCastOrRecast()
        {
            p.MaxHP=101; p.MaxMP=103; p.CurrentMP=100; p.Heal(1);
            Cast(1301007,30); Assert.That(p.MaxHP,Is.EqualTo(161)); Assert.That(p.MaxMP,Is.EqualTo(164));
            Assert.That(p.CurrentHP,Is.EqualTo(101)); Assert.That(p.CurrentMP,Is.EqualTo(40));
            p.Heal(999); p.CurrentMP=164; Step(100);
            Cast(1301007,30); Assert.That(p.MaxHP,Is.EqualTo(161)); Assert.That(p.CurrentHP,Is.EqualTo(161));
            p.MaxHP=25000; p.MaxMP=25000; Assert.That(p.MaxHP,Is.EqualTo(30000)); Assert.That(p.MaxMP,Is.EqualTo(30000));
            Assert.That(p.CaptureProgress().MaxHP,Is.EqualTo(25000));
            p.MaxHP=101; p.MaxMP=103; p.Heal(999); p.CurrentMP=p.MaxMP;
            p.RemoveStatBuffs(1301007); Assert.That(p.MaxHP,Is.EqualTo(101)); Assert.That(p.CurrentHP,Is.EqualTo(101));
            Assert.That(p.MaxMP,Is.EqualTo(103)); Assert.That(p.CurrentMP,Is.EqualTo(103));
        }
        [Test] public void HyperBodyRecalculatesAfterRealEquipmentChangesWithoutBakingInItsBonus()
        {
            p.JobId=130;p.Level=40;p.STR=90;p.MaxMP=100;p.CurrentMP=100;
            p.Inventory.AddItem(1002025,1); // Red Duke: original Cap.nx incMHP = 10.
            Assert.That(p.TryEquipItem(1002025,out var message),Is.True,message);
            Assert.That(p.MaxHP,Is.EqualTo(110));Cast(1301007,30);Assert.That(p.MaxHP,Is.EqualTo(176));
            p.Heal(999);Step(100);
            Assert.That(p.TryUnequipItem(EquipSlot.Hat,out message),Is.True,message);
            Assert.That(p.MaxHP,Is.EqualTo(160));Assert.That(p.CurrentHP,Is.EqualTo(160));
            Assert.That(p.TryEquipItem(1002025,out message),Is.True,message);
            Assert.That(p.MaxHP,Is.EqualTo(176));Assert.That(p.CurrentHP,Is.EqualTo(160));
            p.RemoveStatBuffs(1301007);Assert.That(p.MaxHP,Is.EqualTo(110));Assert.That(p.CurrentHP,Is.EqualTo(110));
            Assert.That(p.CaptureProgress().MaxHP,Is.EqualTo(100));
        }
        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void ExpiryUsesSimulationTimeAndClampsBothResources(float interval)
        {
            Cast(1301007); p.Heal(999); p.CurrentMP=p.MaxMP;
            for(int i=0;i<(int)System.Math.Round(10/interval);i++)world.UpdatePhysics(interval);
            Assert.That(p.ActiveBuffs,Is.Empty); Assert.That(p.CurrentHP,Is.EqualTo(100)); Assert.That(p.CurrentMP,Is.EqualTo(50));
        }
        [Test] public void TravelRetainsBuffsButSaveRestoreNeverBakesInTemporaryMaxima()
        {
            Cast(1301007); p.Heal(999); p.CurrentMP=p.MaxMP;
            var save=world.CaptureProgress(); Assert.That(save.Player.MaxHP,Is.EqualTo(100)); Assert.That(save.Player.MaxMP,Is.EqualTo(50));
            world.LoadMap(43); Assert.That(p.MaxHP,Is.EqualTo(102)); Assert.That(p.SkillEffect,Is.Null);
            Assert.That(world.TryRestoreProgress(save,out var message),Is.True,message);
            Assert.That(p.MaxHP,Is.EqualTo(100)); Assert.That(p.CurrentHP,Is.EqualTo(100)); Assert.That(p.MaxMP,Is.EqualTo(50));
            Assert.That(p.CurrentMP,Is.EqualTo(50)); Assert.That(skills.GetSkillLevel(1301007),Is.EqualTo(1)); Assert.That(p.ActiveBuffs,Is.Empty);
        }
        [Test] public void NormalMagicianSpUnlocksArmorOnlyAfterThreeGuardLevels()
        {
            while(p.Level<8)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(200,out var message),Is.True,message);
            Assert.That(skills.TrySpendSkillPoint(2001003,out _),Is.False);
            Assert.That(skills.TrySpendSkillPoint(2001002,out _),Is.True);
            p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(skills.TrySpendSkillPoint(2001002,out _),Is.True);
            Assert.That(skills.TrySpendSkillPoint(2001003,out _),Is.False);
            Assert.That(skills.TrySpendSkillPoint(2001002,out _),Is.True);
            Assert.That(skills.TrySpendSkillPoint(2001003,out _),Is.True); Assert.That(p.SkillPoints,Is.Zero);
            Assert.That(skills.UseSkill(2001003).Success,Is.True);
        }
        [TestCase(100)] [TestCase(200)] [TestCase(110)] [TestCase(130)] [TestCase(210)] [TestCase(220)]
        public void PracticePreservesEarnedProgressItemsAndHigherSkillRanks(int job)
        {
            p.AddExperience(p.ExperienceToNextLevel+3); var before=p.CaptureProgress();
            Assert.That(world.RequestSupportPractice(job,out var message),Is.True,message);
            Assert.That(p.JobId,Is.EqualTo(job)); Assert.That(p.Level,Is.EqualTo(before.Level)); Assert.That(p.Experience,Is.EqualTo(before.Experience));
            Assert.That(p.AbilityPoints,Is.EqualTo(before.AbilityPoints)); Assert.That(p.CurrentMP,Is.EqualTo(before.MP));
            var id=GameWorld.SupportPracticeSkills(job)[0]; Assert.That(skills.GetSkillLevel(id),Is.EqualTo(1));
            skills.SetSkillLevel(id,10); Assert.That(world.RequestSupportPractice(job,out _),Is.True);
            Assert.That(skills.GetSkillLevel(id),Is.EqualTo(10)); Assert.That(p.Inventory.GetStacks().Count(),Is.EqualTo(before.Bag.Length));
        }
        [Test] public void InvalidAndUnaffordableBuffsHaveNoSideEffects()
        {
            p.JobId=200; skills.SetSkillLevel(2001002,1); p.CurrentMP=5;
            Assert.That(skills.UseSkill(2001002).Success,Is.False); Assert.That(p.CurrentMP,Is.EqualTo(5)); Assert.That(p.SkillEffect,Is.Null);
            p.CurrentMP=50; p.JobId=100; Assert.That(skills.UseSkill(2001002).Success,Is.False);
            Assert.That(world.RequestSupportPractice(999,out _),Is.False); Assert.That(p.JobId,Is.EqualTo(100));
            Assert.That(p.ActiveBuffs,Is.Empty); Assert.That(p.CurrentMP,Is.EqualTo(50));
        }
    }
}
