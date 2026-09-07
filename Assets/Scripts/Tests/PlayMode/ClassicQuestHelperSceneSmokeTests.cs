using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicQuestHelperSceneSmokeTests
    {
        private readonly List<string> errors=new List<string>();
        [SetUp] public void Watch(){errors.Clear();Application.logMessageReceived+=Log;}
        private void Log(string text,string stack,LogType type){if(type!=LogType.Log)errors.Add(type+": "+text);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(errors,Is.Empty);}
        private static GameWorld Pause()
        {
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;
            for(int i=0;i<100;i++)manager.World.UpdatePhysics(.008f);return manager.World;
        }
        private static void RestoreQuests(GameWorld world,bool multiple)
        {
            var progress=world.CaptureProgress();
            progress.Quests=(multiple?new[]{1037,1038,1039,2088}:new[]{2088}).Select(id=>new SavedQuest{
                Id=id,State=id==1037?2:1,Tracked=id!=1037,
                Kills=world.Quests.Get(id).Finish.Mobs.OrderBy(m=>m.Key).Select(m=>id==1037?m.Value:0).ToArray()
            }).ToArray();
            Assert.That(world.TryRestoreProgress(progress,out var message),Is.True,message);
        }
        private static Rect CanvasRect(RectTransform r)
        {
            var canvas=(RectTransform)r.GetComponentInParent<Canvas>().rootCanvas.transform;
            var corners=new Vector3[4];r.GetWorldCorners(corners);
            var a=canvas.InverseTransformPoint(corners[0]);var b=canvas.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        private static void CheckLayout()
        {
            Object.FindFirstObjectByType<ClassicBuffView>().Layout();
            var helper=GameObject.Find("QuestHelperPanel").GetComponent<RectTransform>();helper.GetComponent<ClassicQuestHelperBounds>().Layout();Canvas.ForceUpdateCanvases();
            var canvas=(RectTransform)helper.parent;var rect=CanvasRect(helper);
            Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(canvas.rect.xMin+7));Assert.That(rect.xMax,Is.LessThanOrEqualTo(canvas.rect.xMax-7));
            float hud=151*Mathf.Min(1,canvas.rect.width/800f)+8;
            Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(canvas.rect.yMin+hud-.1f));Assert.That(rect.yMax,Is.LessThanOrEqualTo(canvas.rect.yMax-15));
            var buffs=GameObject.Find("ActiveBuffs").GetComponent<RectTransform>();
            if(buffs.rect.width>0)Assert.That(rect.yMax,Is.LessThanOrEqualTo(CanvasRect(buffs).yMin-7));
            foreach(var label in helper.GetComponentsInChildren<Text>())Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.1f),label.name);
            var title=GameObject.Find("HelperTitle").GetComponent<Text>();
            Assert.That(title.preferredWidth,Is.LessThanOrEqualTo(title.rectTransform.rect.width));
            var auto=GameObject.Find("QuestHelperAuto").GetComponent<RectTransform>();
            Assert.That(auto.rect.size,Is.EqualTo(new Vector2(21,12)));
            Assert.That(CanvasRect(title.rectTransform).xMax,Is.LessThan(CanvasRect(auto).xMin));
            Assert.That(CanvasRect(auto).xMax,Is.LessThan(CanvasRect(GameObject.Find("QuestHelperCollapse").GetComponent<RectTransform>()).xMin));
            var close=GameObject.Find("QuestHelperClose").GetComponent<Image>();
            Assert.That(close.sprite.name,Does.EndWith("Basic.img/BtClose/normal/0"));
            Assert.That(close.rectTransform.rect.size,Is.EqualTo(new Vector2(12,12)));
            Assert.That(CanvasRect(close.rectTransform).xMax,Is.LessThan(rect.xMax));
        }
        private static IEnumerator Drag(Vector2 delta)
        {
            // CaptureScreen temporarily changes the canvas camera/size; let it return to screen space.
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            GameObject.Find("QuestHelperPanel").GetComponent<ClassicQuestHelperBounds>().Layout();Canvas.ForceUpdateCanvases();
            var header=GameObject.Find("HelperTitleFrame");var r=header.GetComponent<RectTransform>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector2(90,-9))),delta=delta};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits,Is.Not.Empty,"Helper title must receive pointer input at "+pointer.position);Assert.That(hits[0].gameObject,Is.EqualTo(header));
            Assert.That(Object.FindFirstObjectByType<ClassicCursorView>().StateAt(pointer.position),Is.EqualTo(ClassicCursorView.Grabbable));
            ExecuteEvents.Execute(header,pointer,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(header,pointer,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(header,pointer,ExecuteEvents.endDragHandler);yield return null;
        }
        [UnityTest] public IEnumerator NativeHelperControlsUntrackWithoutAbandoningAndCoexistWithBuffs()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();RestoreQuests(world,false);yield return null;yield return null;
            world.Player.Inventory.TryAddItem(4000001,20);world.Player.Inventory.TryAddItem(4000011,10);yield return null;
            Assert.That(GameObject.Find("TrackedObjectives_2088").GetComponent<Text>().text,Does.Contain("20/40 Orange Mushroom Cap"));
            Assert.That(GameObject.Find("QuestHelperCollapse").GetComponent<Image>().sprite.name,Does.Contain("Basic.img/BtMin"));
            Assert.That(GameObject.Find("QuestHelperRemove_2088").GetComponent<Image>().sprite.name,Does.Contain("Basic.img/BtClose2"));
            yield return InventorySceneSmokeTests.Click("QuestHelperCollapse");yield return null;
            Assert.That(GameObject.Find("QuestHelperPanel").GetComponent<RectTransform>().rect.height,Is.EqualTo(20));
            Assert.That(GameObject.Find("TrackedObjectives_2088"),Is.Null);Assert.That(world.Quests.IsTracked(2088),Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-collapsed",1366,768,CheckLayout);
            yield return InventorySceneSmokeTests.Click("QuestHelperCollapse");yield return null;
            foreach(int id in new[]{2002004,2002001}){var item=world.Player.GetItemInfo(id);Assert.That(world.Player.ApplyStatBuffs(-id,item.Name,item.Buffs,180000),Is.True);}
            yield return null;yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-buffs",1366,768,CheckLayout);
            var journal=Object.FindFirstObjectByType<ClassicQuestView>();journal.OpenQuest(2088);yield return null;
            Object.FindFirstObjectByType<InventoryView>().ShowBag(true);yield return null;
            Assert.That(GameObject.Find("QuestListScroll").GetComponent<Scrollbar>().interactable,Is.False);
            Assert.That(GameObject.Find("QuestListScroll").GetComponent<Scrollbar>().handleRect.gameObject.activeSelf,Is.False);
            Assert.That(GameObject.Find("QuestListUp").GetComponent<Button>().interactable,Is.False);
            Assert.That(GameObject.Find("QuestListDown").GetComponent<Button>().interactable,Is.False);
            yield return InventorySceneSmokeTests.Click("QuestHelperRemove_2088");yield return null;
            Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.InProgress));Assert.That(world.Quests.IsTracked(2088),Is.False);
            Assert.That(world.Player.Inventory.GetItemCount(4000001),Is.EqualTo(20));
            Assert.That(GameObject.Find("QuestHelperPanel").GetComponent<RectTransform>().rect.height,Is.EqualTo(20));
            Assert.That(GameObject.Find("HelperTitle").GetComponent<Text>().text,Is.EqualTo("Quest Helper (0/5)"));
            Assert.That(journal.Visible,Is.True);Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible,Is.True);
            yield return InventorySceneSmokeTests.Click("QuestTrack");yield return null;
            Assert.That(GameObject.Find("QuestHelperPanel"),Is.Not.Null);Assert.That(GameObject.Find("Buff_-2002004"),Is.Not.Null);
            Object.FindFirstObjectByType<ClassicWindowManager>().CloseFrontmost();yield return null;
            Assert.That(journal.Visible,Is.False);Assert.That(GameObject.Find("QuestHelperPanel"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator MultipleQuestRowsStayReadableAndDraggedPositionSurvivesResizeAndTravel()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();RestoreQuests(world,true);yield return null;yield return null;
            Assert.That(GameObject.Find("HelperTitle").GetComponent<Text>().text,Is.EqualTo("Quest Helper (3/5)"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-multiple",1366,768,CheckLayout);
            yield return Drag(new Vector2(-270,-50));var helper=GameObject.Find("QuestHelperPanel").GetComponent<RectTransform>();
            var position=helper.anchoredPosition;
            yield return InventorySceneSmokeTests.Click("QuestHelperCollapse");yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperCollapse");yield return null;
            Assert.That(helper.anchoredPosition,Is.EqualTo(position));
            Vector2 widePosition=Vector2.zero;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-dragged",1366,768,()=>{CheckLayout();widePosition=helper.anchoredPosition;});
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-small",640,480,CheckLayout);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-resized",1366,768,()=>{CheckLayout();Assert.That(Vector2.Distance(helper.anchoredPosition,widePosition),Is.LessThan(.1f));});
            world.LoadMap(100000102);yield return null;yield return null;
            Assert.That(GameObject.Find("QuestHelperPanel").GetComponent<RectTransform>().anchoredPosition,Is.EqualTo(position));
            var current=world.CaptureProgress();Assert.That(current.Quests.Count(q=>q.Tracked),Is.EqualTo(3));
            Assert.That(world.TryRestoreProgress(current,out _),Is.True);yield return null;Assert.That(GameObject.Find("HelperTitle").GetComponent<Text>().text,Is.EqualTo("Quest Helper (3/5)"));
        }
        [UnityTest] public IEnumerator AutoControlUsesNativeStatesAndRemainsReachableWithoutTrackedQuests()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();RestoreQuests(world,false);yield return null;yield return null;
            const string autoPath="UIWindow.img/QuestAlarm/BtAuto/";
            Assert.That(GameObject.Find("QuestHelperAuto").GetComponent<Image>().sprite.name,Does.EndWith(autoPath+"normal/0"));
            yield return InventorySceneSmokeTests.Click("QuestHelperAuto");yield return null;
            Assert.That(world.Quests.AutoTrackAccepted,Is.False);Assert.That(world.Quests.IsTracked(2088),Is.True);
            Assert.That(GameObject.Find("QuestHelperAuto").GetComponent<Image>().sprite.name,Does.EndWith(autoPath+"disabled/0"));
            Assert.That(GameObject.Find("QuestHelperAuto").GetComponent<Button>().interactable,Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-auto-off",1366,768,CheckLayout);
            yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperRemove_2088");yield return null;
            Assert.That(GameObject.Find("QuestHelperCollapse").GetComponent<Button>().interactable,Is.False);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-auto-empty",640,480,CheckLayout);
            yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperAuto");yield return null;
            Assert.That(world.Quests.AutoTrackAccepted,Is.True);Assert.That(world.Quests.IsTracked(2088),Is.False);
            yield return InventorySceneSmokeTests.Click("QuestHelperAuto");yield return null;
            Assert.That(world.Quests.Abandon(2088,out _),Is.True);
            while(world.Player.Level<10)world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);
            var anchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc&&a.NpcId==1012111);
            world.Player.ResetMovementForMap();world.Player.Position=new MapleClient.GameLogic.Vector2(anchor.Feet.x,anchor.Feet.y+Player.Height/2);world.Player.IsGrounded=true;
            Assert.That(world.StartQuest(world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111),2088,out var message),Is.True,message);yield return null;
            Assert.That(world.Quests.State(2088),Is.EqualTo(QuestState.InProgress));Assert.That(world.Quests.IsTracked(2088),Is.False);
            Object.FindFirstObjectByType<ClassicQuestView>().OpenQuest(2088);yield return null;
            yield return InventorySceneSmokeTests.Click("QuestTrack");yield return null;
            Assert.That(world.Quests.IsTracked(2088),Is.True);Assert.That(world.Quests.AutoTrackAccepted,Is.False);
            yield return InventorySceneSmokeTests.Click("QuestHelperAuto");yield return null;
            Assert.That(world.Quests.AutoTrackAccepted,Is.True);
            Object.FindFirstObjectByType<ClassicQuestView>().Show(false);RestoreQuests(world,true);yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-auto-on",1366,768,CheckLayout);
            yield return null;yield return null;
            var journal=Object.FindFirstObjectByType<ClassicQuestView>();
            yield return InventorySceneSmokeTests.Click("QuestHelperClose");yield return null;
            Assert.That(journal.HelperVisible,Is.False);Assert.That(world.Quests.Capture().Count(q=>q.Tracked),Is.EqualTo(3));
            world.LoadMap(100000102);yield return null;Assert.That(journal.HelperVisible,Is.False);
            journal.OpenQuest(2088);yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperToggle");yield return null;
            Assert.That(journal.HelperVisible,Is.True);Assert.That(world.Quests.AutoTrackAccepted,Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-journal-toggle",1366,768,CheckLayout);
            yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperToggle");yield return null;
            Assert.That(journal.HelperVisible,Is.False);Assert.That(world.Quests.IsTracked(2088),Is.True);
            yield return InventorySceneSmokeTests.Click("QuestTrack");yield return null;
            yield return InventorySceneSmokeTests.Click("QuestTrack");yield return null;
            Assert.That(journal.HelperVisible,Is.True);Assert.That(world.Quests.IsTracked(2088),Is.True);
            journal.Show(false);yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperCollapse");yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-helper-auto-collapsed",1366,768,CheckLayout);
            yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click("QuestHelperAuto");yield return null;
            var save=world.CaptureProgress();world.LoadMap(100000102);Assert.That(world.TryRestoreProgress(save,out _),Is.True);yield return null;
            Assert.That(world.Quests.AutoTrackAccepted,Is.False);Assert.That(world.Quests.Capture().Count(q=>q.Tracked),Is.EqualTo(3));
        }
    }
}
