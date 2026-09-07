using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using PortalType = MapleClient.GameLogic.PortalType;
using DropInfo = MapleClient.GameLogic.DropInfo;

namespace MapleClient.Tests.GameData
{
    public class OfflineQuestIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData Map=new MapData{MapId=42,Platforms=new List<Platform>{new Platform{Id=1,X1=-3000,X2=3000,Y1=0,Y2=0}},
                Portals=new List<Portal>{new Portal{Id=0,Name="sp",Type=PortalType.Spawn}},
                NpcSpawns=new List<NpcSpawn>{new NpcSpawn{NpcId=1012111},new NpcSpawn{NpcId=2005},new NpcSpawn{NpcId=2103},new NpcSpawn{NpcId=12000},new NpcSpawn{NpcId=20100}}};
            public MapData GetMap(int id)=>id==42?Map:null;
        }
        private GameWorld world;private Player player;private Maps maps;
        private NpcSpawn Npc(int id)=>maps.Map.NpcSpawns.Single(n=>n.NpcId==id);
        [SetUp] public void Setup()
        {
            maps=new Maps();world=new GameWorld(null,maps,assetProvider:NXDataManagerSingleton.Instance.DataManager);world.LoadMap(42);player=world.Player;
            player.Position=new Vector2(0,Player.Height/2);player.IsGrounded=true;
        }
        private void EarnTo(int level){while(player.Level<level)player.AddExperience(player.ExperienceToNextLevel-player.Experience);}
        private void BruceItems(){player.Inventory.TryAddItem(4000011,10);player.Inventory.TryAddItem(4000001,40);}
        private void Kill(int id)
        {
            player.ResetMovementForMap();player.Position=new Vector2(0,Player.Height/2);player.IsGrounded=true;player.SetBaseDamage(1000);
            var monster=new Monster(new MonsterTemplate{MonsterId=id,MaxHP=1,Level=1,Exp=0,DropTable=new List<DropInfo>()},new Vector2(.25f,0));
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(world);
            combat.PerformBasicAttack(player,new List<Monster>{monster},1);combat.Update(2);Assert.That(monster.IsDead,Is.True);
            player.ResetMovementForMap();player.IsGrounded=true;
        }
        [Test] public void CatalogReadsActualRequirementsDialogueAndRewardsAndSkipsScriptedQuests()
        {
            CollectionAssert.AreEqual(new[]{1037,1038,1039,2088},world.Quests.Definitions.Select(q=>q.Id));
            var bruce=world.Quests.Get(2088);Assert.That(bruce.Start.MinLevel,Is.EqualTo(10));Assert.That(bruce.Start.Npc,Is.EqualTo(1012111));
            Assert.That(bruce.Finish.Items[4000011],Is.EqualTo(10));Assert.That(bruce.Finish.Items[4000001],Is.EqualTo(40));
            Assert.That(bruce.OnFinish.Experience,Is.EqualTo(300));Assert.That(bruce.OnFinish.Items[2000000],Is.EqualTo(25));
            Assert.That(bruce.Dialogue["0/yes"].Length,Is.EqualTo(4));Assert.That(world.Quests.Get(1021),Is.Null);
            Assert.That(world.Quests.Get(1038).Start.Quests[1037],Is.EqualTo(2));
        }
        [Test] public void AcceptRequiresLevelJobPrerequisitesAndAnActualNearbyNpc()
        {
            Assert.That(world.StartQuest(Npc(1012111),2088,out _),Is.False);
            Assert.That(world.StartQuest(Npc(2103),1038,out _),Is.False);
            player.JobId=100;Assert.That(world.StartQuest(Npc(2005),1037,out _),Is.False);player.JobId=0;
            EarnTo(10);Assert.That(world.StartQuest(new NpcSpawn{NpcId=1012111},2088,out _),Is.False);
            player.Position=new Vector2(2,.3f);Assert.That(world.StartQuest(Npc(1012111),2088,out _),Is.False);
            player.Position=new Vector2(0,.3f);Assert.That(world.StartQuest(Npc(1012111),2088,out var m),Is.True,m);
            Assert.That(world.StartQuest(Npc(1012111),2088,out _),Is.False);Assert.That(world.Quests.IsTracked(2088),Is.True);
        }
        [Test] public void CollectionCountsFollowBagAndFullRewardBagConsumesNothing()
        {
            var completed=new List<int>();world.QuestCompleted+=completed.Add;
            EarnTo(10);world.StartQuest(Npc(1012111),2088,out _);BruceItems();Assert.That(world.Quests.CanFinish(2088),Is.True);
            player.Inventory.RemoveItem(4000011,1);Assert.That(world.Quests.CanFinish(2088),Is.False);player.Inventory.TryAddItem(4000011,1);
            player.Inventory.TryAddItem(2000003,Inventory.SlotsPerCategory*100);long before=player.Experience;
            Assert.That(world.FinishQuest(Npc(1012111),2088,out var message),Is.False);Assert.That(message,Does.Contain("bag"));
            Assert.That(player.Inventory.GetItemCount(4000001),Is.EqualTo(40));Assert.That(player.Experience,Is.EqualTo(before));
            Assert.That(completed,Is.Empty,"A full reward bag must not emit completion feedback.");
            player.Inventory.RemoveItem(2000003,100);Assert.That(world.FinishQuest(Npc(1012111),2088,out message),Is.True,message);
            Assert.That(player.Experience,Is.EqualTo(before+300));Assert.That(player.Inventory.GetItemCount(4000001),Is.Zero);Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(25));
            Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.Completed));Assert.That(world.Quests.IsTracked(2088),Is.False);
            Assert.That(world.FinishQuest(Npc(1012111),2088,out _),Is.False);
            Assert.That(world.TryRestoreProgress(world.CaptureProgress(),out _),Is.True);
            Assert.That(completed,Is.EqualTo(new[]{2088}),"Only the successful reward emits completion.");
        }
        [Test] public void CompletionCannotBeReenteredFromItemCallbacks()
        {
            EarnTo(10);world.StartQuest(Npc(1012111),2088,out _);BruceItems();bool reentered=false;
            player.Inventory.ItemAdded+=(id,count)=>{reentered=true;Assert.That(world.FinishQuest(Npc(1012111),2088,out _),Is.False);};
            Assert.That(world.FinishQuest(Npc(1012111),2088,out _),Is.True);Assert.That(reentered,Is.True);Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(25));
        }
        [Test] public void OnlyCreditedPostAcceptanceKillsCountAndAbandonResetsThem()
        {
            Kill(100100);world.StartQuest(Npc(2005),1037,out _);Assert.That(world.Quests.Kills(1037,100100),Is.Zero);
            Kill(100101);Assert.That(world.Quests.Kills(1037,100100),Is.Zero);
            var other=new Monster(new MonsterTemplate{MonsterId=100100,MaxHP=1},Vector2.Zero);other.TakeDamage(1);Assert.That(world.Quests.Kills(1037,100100),Is.Zero);
            for(int i=0;i<12;i++)Kill(100100);Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(10));
            Assert.That(world.FinishQuest(Npc(2005),1037,out _),Is.False);Assert.That(world.Quests.CanFinish(1037),Is.True);
            Assert.That(world.Quests.Abandon(1037,out _),Is.True);world.StartQuest(Npc(2005),1037,out _);Assert.That(world.Quests.Kills(1037,100100),Is.Zero);
        }
        [Test] public void DeliveryChainIssuesOneLetterRecoversLossAndReclaimsOnAbandon()
        {
            world.StartQuest(Npc(2005),1037,out _);for(int i=0;i<10;i++)Kill(100100);
            Assert.That(world.FinishQuest(Npc(2103),1037,out var m),Is.True,m);Assert.That(world.StartQuest(Npc(2103),1038,out m),Is.True,m);
            Assert.That(player.Inventory.GetItemCount(4031800),Is.EqualTo(1));Assert.That(world.RecoverQuestDelivery(Npc(2103),1038,out _),Is.False);
            player.Inventory.RemoveItem(4031800,1);Assert.That(world.RecoverQuestDelivery(Npc(12000),1038,out _),Is.False);
            Assert.That(world.RecoverQuestDelivery(Npc(2103),1038,out m),Is.True,m);Assert.That(world.Quests.Abandon(1038,out _),Is.True);
            Assert.That(player.Inventory.GetItemCount(4031800),Is.Zero);Assert.That(world.StartQuest(Npc(2103),1038,out _),Is.True);
            Assert.That(world.FinishQuest(Npc(12000),1038,out m),Is.True,m);Assert.That(player.Inventory.GetItemCount(4031800),Is.Zero);
            Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(5));Assert.That(player.Inventory.GetItemCount(2000003),Is.EqualTo(5));
        }
        [Test] public void SaveRestoresHuntsTrackingAndCompletedRewardsWithoutAliasing()
        {
            world.StartQuest(Npc(2005),1037,out _);Kill(100100);world.Quests.SetTracked(1037,false,out _);
            var save=world.CaptureProgress();Kill(100100);Assert.That(save.Quests.Single().Kills.Single(),Is.EqualTo(1));
            Assert.That(world.TryRestoreProgress(save,out var m),Is.True,m);Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(1));Assert.That(world.Quests.IsTracked(1037),Is.False);
            save.Quests[0].Kills[0]=9;Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(1));
            EarnTo(10);world.StartQuest(Npc(1012111),2088,out _);BruceItems();world.FinishQuest(Npc(1012111),2088,out _);
            var completed=world.CaptureProgress();Assert.That(world.TryRestoreProgress(completed,out m),Is.True,m);Assert.That(world.FinishQuest(Npc(1012111),2088,out _),Is.False);
            Assert.That(player.Inventory.GetItemCount(2000000),Is.EqualTo(25));
        }
        [Test] public void AutoTrackingOnlyAffectsNewAcceptancesAndPreservesManualChoicesAndProgress()
        {
            Assert.That(world.Quests.AutoTrackAccepted,Is.True);
            Assert.That(world.StartQuest(Npc(2005),1037,out var message),Is.True,message);Kill(100100);
            world.Quests.SetAutoTrackAccepted(false);
            Assert.That(world.Quests.IsTracked(1037),Is.True);
            EarnTo(10);Assert.That(world.StartQuest(Npc(1012111),2088,out message),Is.True,message);
            Assert.That(world.Quests.IsTracked(2088),Is.False);
            world.Quests.SetTracked(1037,false,out _);
            world.Quests.SetAutoTrackAccepted(true);
            Assert.That(world.Quests.IsTracked(1037),Is.False);Assert.That(world.Quests.IsTracked(2088),Is.False);
            Kill(100100);Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(2));
            Assert.That(world.Quests.SetTracked(2088,true,out message),Is.True,message);
            var save=world.CaptureProgress();world.Quests.SetAutoTrackAccepted(false);
            Assert.That(world.TryRestoreProgress(save,out message),Is.True,message);
            Assert.That(world.Quests.AutoTrackAccepted,Is.False);Assert.That(world.Quests.IsTracked(2088),Is.True);
            Assert.That(world.Quests.IsTracked(1037),Is.False);Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(2));
            world.Quests.Abandon(2088,out _);world.Quests.SetAutoTrackAccepted(true);
            Assert.That(world.StartQuest(Npc(1012111),2088,out message),Is.True,message);
            Assert.That(world.Quests.IsTracked(2088),Is.True);
        }
        [TestCase("unknown")] [TestCase("duplicate")] [TestCase("count")] [TestCase("state")] [TestCase("null")] [TestCase("dependency")]
        public void InvalidQuestSaveRejectsBeforeChangingCharacter(string fault)
        {
            world.StartQuest(Npc(2005),1037,out _);Kill(100100);var save=world.CaptureProgress();save.Player.Name="Corrupted";
            if(fault=="unknown")save.Quests[0].Id=999999;if(fault=="duplicate")save.Quests=new[]{save.Quests[0],save.Quests[0]};
            if(fault=="count")save.Quests[0].Kills[0]=11;if(fault=="state")save.Quests[0].State=7;if(fault=="null")save.Quests=null;
            if(fault=="dependency")save.Quests=new[]{new SavedQuest{Id=1038,State=1,Kills=new int[0]}};
            Assert.That(world.TryRestoreProgress(save,out _),Is.False);Assert.That(player.Name,Is.Not.EqualTo("Corrupted"));Assert.That(world.Quests.Kills(1037,100100),Is.EqualTo(1));
        }
        [TestCase(1)] [TestCase(2)] public void OlderSaveVersionsMigrateWithAnEmptyJournal(int version)
        {
            var save=world.CaptureProgress();save.Version=version;save.Quests=null;
            world.StartQuest(Npc(2005),1037,out _);Assert.That(world.TryRestoreProgress(save,out var m),Is.True,m);
            Assert.That(world.Quests.Capture(),Is.Empty);Assert.That(world.CaptureProgress().Version,Is.EqualTo(5));
        }
        [Test] public void LocalShroomDropMakesTheSourceCollectionQuestObtainable()
        {Assert.That(OfflineEconomy.Drops(120100,2,player.GetItemInfo).Any(d=>d.ItemId==4000011 && d.Quantity==1),Is.True);}
    }
}
