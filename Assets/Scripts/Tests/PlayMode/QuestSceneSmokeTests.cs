using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using LogicVector=MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class QuestSceneSmokeTests
    {
        private readonly List<string> errors=new List<string>();
        [SetUp] public void Watch(){errors.Clear();Application.logMessageReceived+=Log;}
        private void Log(string text,string stack,LogType type){if(type!=LogType.Log)errors.Add(type+": "+text);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(errors,Is.Empty);}
        private static void NearBruce(GameWorld world)
        {
            var anchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc && a.NpcId==1012111);
            world.Player.ResetMovementForMap();world.Player.Position=new LogicVector(anchor.Feet.x,anchor.Feet.y+Player.Height/2);world.Player.IsGrounded=true;
        }
        [UnityTest] public IEnumerator BruceDialogueJournalHelperAndRewardWorkWithOtherWindowsOpen()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;
            while(world.Player.Level<10)world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);
            NearBruce(world);yield return null;yield return null;
            var journal=Object.FindFirstObjectByType<ClassicQuestView>();var talk=Object.FindFirstObjectByType<ClassicNpcDialogue>();var bag=Object.FindFirstObjectByType<InventoryView>();
            var bruceAnchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc && a.NpcId==1012111);
            var npcScreen=Camera.main.WorldToScreenPoint(bruceAnchor.GetComponentInChildren<SpriteRenderer>().bounds.center);
            Assert.That(talk.ClickWorld(npcScreen),Is.False);Assert.That(talk.ClickWorld(npcScreen),Is.True);talk.Close();
            bag.ShowBag(true);journal.Show(true);yield return null;yield return null;
            GameObject.Find("InventoryPanel").GetComponent<ClassicWindow>().SetPosition(new Vector2(-400,15));
            var journalScreen=RectTransformUtility.WorldToScreenPoint(null,GameObject.Find("QuestPanel").transform.position);
            Assert.That(talk.ClickWorld(journalScreen),Is.False);Assert.That(talk.ClickWorld(journalScreen),Is.False);Assert.That(talk.Visible,Is.False);
            yield return InventorySceneSmokeTests.Click("QuestRow_2088");
            Assert.That(journal.SelectedTab,Is.Zero);Assert.That(journal.SelectedQuest,Is.EqualTo(2088));Assert.That(bag.BagVisible,Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-available",1366,768);
            var bruce=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111);Assert.That(talk.TryOpen(bruce),Is.True);
            yield return InventorySceneSmokeTests.Click("NpcQuest_2088");
            Assert.That(GameObject.Find("NpcSpeech").GetComponentInChildren<Text>().text,Does.StartWith("Hey there, kid."));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-dialogue",1366,768);
            yield return InventorySceneSmokeTests.Click("NpcYes");Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.InProgress));
            Assert.That(journal.Visible,Is.True);Assert.That(bag.BagVisible,Is.True);
            for(int i=0;i<3;i++)yield return InventorySceneSmokeTests.Click("NpcNext");
            yield return InventorySceneSmokeTests.Click("NpcOkay");Assert.That(talk.Visible,Is.False);
            world.Player.Inventory.TryAddItem(4000011,10);world.Player.Inventory.TryAddItem(4000001,20);journal.OpenQuest(2088);
            yield return null;yield return null;
            Assert.That(GameObject.Find("TrackedObjectives_2088").GetComponent<Text>().text,Does.Contain("20/40"));
            Assert.That(journal.SelectedTab,Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-progress",1366,768);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-small",640,480,()=>{Inside("QuestPanel");Inside("QuestHelperPanel");});
            world.Player.Inventory.TryAddItem(4000001,20);yield return null;
            Assert.That(talk.TryOpen(bruce),Is.True);yield return InventorySceneSmokeTests.Click("NpcQuest_2088");
            yield return InventorySceneSmokeTests.Click("NpcYes");Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.Completed));
            Assert.That(world.Player.Inventory.GetItemCount(2000000),Is.EqualTo(25));Assert.That(world.Player.Inventory.GetItemCount(4000001),Is.Zero);
            yield return InventorySceneSmokeTests.Click("NpcOkay");journal.OpenQuest(2088);yield return null;yield return null;
            Assert.That(journal.SelectedTab,Is.EqualTo(2));Assert.That(GameObject.Find("QuestHelperPanel"),Is.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-completed",1366,768);
            var save=world.CaptureProgress();Assert.That(world.TryRestoreProgress(save,out var message),Is.True,message);yield return null;
            Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.Completed));Assert.That(journal.Visible,Is.True);Assert.That(bag.BagVisible,Is.True);
            Object.FindFirstObjectByType<ClassicWindowManager>().CloseFrontmost();yield return null;
            Assert.That(journal.Visible,Is.False);Assert.That(bag.BagVisible,Is.True);
        }
        [UnityTest] public IEnumerator TrackingAbandonConfirmationAndDialogueLifetimeUseIndependentWindows()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;
            while(world.Player.Level<10)world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);
            NearBruce(world);yield return null;yield return null;
            var bruce=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111);Assert.That(world.StartQuest(bruce,2088,out _),Is.True);
            var journal=Object.FindFirstObjectByType<ClassicQuestView>();var talk=Object.FindFirstObjectByType<ClassicNpcDialogue>();journal.OpenQuest(2088);
            yield return InventorySceneSmokeTests.Click("QuestTrack");Assert.That(world.Quests.IsTracked(2088),Is.False);
            yield return InventorySceneSmokeTests.Click("QuestTrack");Assert.That(world.Quests.IsTracked(2088),Is.True);
            yield return InventorySceneSmokeTests.Click("QuestAbandon");Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.InProgress));
            yield return InventorySceneSmokeTests.Click("CancelQuestAbandon");Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.InProgress));
            yield return InventorySceneSmokeTests.Click("QuestAbandon");yield return InventorySceneSmokeTests.Click("QuestAbandon");
            Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.Available));
            yield return InventorySceneSmokeTests.Click("QuestTab_0");yield return null;Assert.That(GameObject.Find("QuestRow_2088"),Is.Not.Null);
            Assert.That(talk.TryOpen(bruce),Is.True);yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-small",640,480,()=>Inside("NpcDialoguePanel"));
            world.Player.Position+=new LogicVector(10,0);yield return null;Assert.That(talk.Visible,Is.False);Assert.That(journal.Visible,Is.True);
            NearBruce(world);Assert.That(talk.TryOpen(bruce),Is.True);world.LoadMap(100000102);yield return null;yield return null;Assert.That(talk.Visible,Is.False);
            for(int i=0;i<100;i++)world.UpdatePhysics(.008f);yield return null;
            var luna=world.CurrentMap.NpcSpawns.Single();var anchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc);
            world.Player.Position=new LogicVector(anchor.Feet.x,anchor.Feet.y+Player.Height/2);world.Player.ResetMovementForMap();world.Player.IsGrounded=true;
            Assert.That(talk.TryOpen(luna),Is.True);yield return InventorySceneSmokeTests.Click("NpcOpenShop");
            Assert.That(GameObject.Find("LocalShopPanel"),Is.Not.Null);Assert.That(talk.Visible,Is.False);Assert.That(journal.Visible,Is.True);
        }
        private static void Inside(string name)
        {
            var r=GameObject.Find(name).GetComponent<RectTransform>();var canvas=(RectTransform)r.GetComponentInParent<Canvas>().transform;
            var corners=new Vector3[4];r.GetWorldCorners(corners);
            foreach(var c in corners){var p=canvas.InverseTransformPoint(c);Assert.That(p.x,Is.InRange(canvas.rect.xMin-.1f,canvas.rect.xMax+.1f),name);Assert.That(p.y,Is.InRange(canvas.rect.yMin-.1f,canvas.rect.yMax+.1f),name);}
        }
    }
}
