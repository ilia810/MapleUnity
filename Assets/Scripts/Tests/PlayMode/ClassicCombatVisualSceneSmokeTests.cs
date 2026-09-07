using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using LogicVector=MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicCombatVisualSceneSmokeTests
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
        private static Rect Pixels(RectTransform r)
        {
            var corners=new Vector3[4];r.GetWorldCorners(corners);var canvas=r.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [UnityTest] public IEnumerator NativeDamageFamiliesHealthFramesAndLifetimeSurviveResolutionAndMapChanges()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();var feedback=Object.FindFirstObjectByType<PlayerCombatFeedback>();
            // Exercise every authored digit plus outgoing/incoming misses; no text fallback is acceptable with NX installed.
            var testFont=Font.CreateDynamicFontFromOSFont("Arial",12);
            foreach(var style in new[]{ClassicDamageStyle.Normal,ClassicDamageStyle.Critical,ClassicDamageStyle.Incoming})
            foreach(int value in new[]{0,1023456789})
            {
                var text=ClassicUI.Text("DigitCoverage",feedback.transform,testFont,value.ToString());
                var digits=text.gameObject.AddComponent<ClassicDamageNumber>();digits.Bind(text,value,style);
                Assert.That(digits.HasArtwork,Is.True,style+" "+value);Assert.That(text.enabled,Is.False);
                Assert.That(digits.GetComponentsInChildren<Image>().All(i=>!i.raycastTarget && i.sprite!=null),Is.True);
                text.gameObject.SetActive(false);Object.Destroy(text.gameObject);
            }
            Object.Destroy(testFont);
            var feet=world.Player.Position.Y-Player.Height/2;
            foreach(float offset in new[]{-2.6f,1.2f,3.4f})world.SpawnMonsterForTesting(100101,new LogicVector(world.Player.Position.X+offset,feet));
            foreach(var monster in world.Monsters){monster.Template.BodyAttack=false;monster.SetMovementPattern(MovementPattern.Stationary);}
            yield return null;yield return null;
            var targets=world.Monsters.OrderBy(m=>m.Position.X).ToArray();var target=targets[1];
            Assert.That(target.MaxHP,Is.EqualTo(15));target.TakeDamage(7);yield return null;
            var view=Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Single(v=>v.Model==target);
            var health=Object.FindFirstObjectByType<ClassicMonsterHealthView>();Assert.That(health.HasVisibleBar(view),Is.True);
            var show=typeof(PlayerCombatFeedback).GetMethod("ShowAttack",BindingFlags.NonPublic|BindingFlags.Instance);
            System.Action showNumbers=()=>{
                show.Invoke(feedback,new object[]{targets[0],new AttackHit(3457,false)});
                show.Invoke(feedback,new object[]{targets[1],new AttackHit(3457,true)});
                show.Invoke(feedback,new object[]{targets[1],new AttackHit(1829,true,1)});
                show.Invoke(feedback,new object[]{targets[2],new AttackHit(0,false)});
            };
            world.Player.TakeDamage(7);showNumbers();yield return null;
            Assert.That(GameObject.Find("WorldDamageNumbers").transform.GetSiblingIndex(),Is.LessThan(GameObject.Find("MiniMapPanel").transform.GetSiblingIndex()),"World effects render below the minimap.");
            Assert.That(Object.FindObjectsByType<ClassicDamageNumber>(FindObjectsSortMode.None).Any(n=>n.Style==ClassicDamageStyle.Incoming),Is.True);
            System.Action check=()=>{
                var r=GameObject.Find("MonsterHealth_"+view.GetInstanceID()).GetComponent<RectTransform>();var rect=Pixels(r);
                Assert.That(rect.width,Is.EqualTo(50).Within(.15));Assert.That(rect.height,Is.EqualTo(10).Within(.15));
                var head=Camera.main.WorldToScreenPoint(view.HeadPosition);
                Assert.That(rect.center.x,Is.EqualTo(head.x).Within(1));Assert.That(rect.yMax,Is.EqualTo(head.y+30).Within(1));
                Assert.That(r.Find("Health").GetComponent<RectTransform>().rect.width,Is.EqualTo(23),"8/15 HP truncates to 53%, then to 23 source fill pixels.");
                var critical=Object.FindObjectsByType<ClassicDamageNumber>(FindObjectsSortMode.None).Where(n=>n.Style==ClassicDamageStyle.Critical).ToArray();
                Assert.That(critical.Length,Is.EqualTo(2));Assert.That(Mathf.Abs(critical[0].Rect.anchoredPosition.y-critical[1].Rect.anchoredPosition.y),Is.EqualTo(36).Within(1));
                Assert.That(critical.All(n=>n.transform.Find("CriticalBurst")!=null),Is.True);
                var first=critical[0].GetComponentsInChildren<Image>().First(i=>i.name.Contains("NoCri1_"));
                Assert.That(Pixels(first.rectTransform).width,Is.EqualTo(first.sprite.rect.width).Within(.15));
            };
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-combat-art",1366,768,check);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-combat-art-small",640,480,check);
            yield return new WaitForSeconds(.8f);Assert.That(Object.FindObjectsByType<ClassicDamageNumber>(FindObjectsSortMode.None),Is.Empty);
            yield return new WaitForSeconds(1.3f);Assert.That(health.HasVisibleBar(view),Is.False);
            target.TakeDamage(1);showNumbers();yield return null;Assert.That(health.HasVisibleBar(view),Is.True);
            world.LoadMap(100000001);yield return null;yield return null;
            Assert.That(Object.FindObjectsByType<ClassicDamageNumber>(FindObjectsSortMode.None),Is.Empty);
            Assert.That(health.transform.Find("MonsterHealthBars").childCount,Is.Zero);
        }
        [UnityTest] public IEnumerator QuestBubblesFollowEligibilityAndCursorRespectsWindowsAndNativeHotspots()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();var markers=Object.FindFirstObjectByType<ClassicQuestIndicators>();var cursor=Object.FindFirstObjectByType<ClassicCursorView>();
            Assert.That(markers.ForNpc(1012111),Is.EqualTo(ClassicQuestIndicators.Marker.None));
            while(world.Player.Level<10)world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);
            var anchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc && a.NpcId==1012111);
            world.Player.ResetMovementForMap();world.Player.Position=new LogicVector(anchor.Feet.x,anchor.Feet.y+Player.Height/2);world.Player.IsGrounded=true;
            yield return null;yield return null;
            Assert.That(markers.ForNpc(anchor.NpcId),Is.EqualTo(ClassicQuestIndicators.Marker.Available));
            Assert.That(GameObject.Find("NpcQuest_1012111").GetComponent<Image>().sprite,Is.Not.Null);
            var npcScreen=(Vector2)Camera.main.WorldToScreenPoint(anchor.GetComponentInChildren<SpriteRenderer>().bounds.center);
            Assert.That(cursor.StateAt(npcScreen),Is.EqualTo(ClassicCursorView.Clickable));
            var indicator=GameObject.Find("NpcQuest_1012111").GetComponent<RectTransform>();
            var hits=new List<RaycastResult>();
            foreach(int state in new[]{ClassicCursorView.Idle,ClassicCursorView.Clickable,ClassicCursorView.Grabbable,ClassicCursorView.Grabbing,ClassicCursorView.Clicking})
            {
                cursor.DrawPointer(npcScreen,state,0);Assert.That(cursor.Pointer.sprite,Is.Not.Null);Assert.That(cursor.Pointer.raycastTarget,Is.False);
                var frame=new ClassicSpriteFrames("ui","Basic.img/Cursor/"+state).Sample(0);var rect=Pixels(cursor.Pointer.rectTransform);
                Assert.That(rect.xMin,Is.EqualTo(npcScreen.x-frame.Origin.x).Within(1));Assert.That(rect.yMax,Is.EqualTo(npcScreen.y+frame.Origin.y).Within(1));
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=npcScreen},hits);Assert.That(hits.Any(h=>h.gameObject==cursor.Pointer.gameObject),Is.False);hits.Clear();
            }
            cursor.DrawPointer(npcScreen,ClassicCursorView.Clickable,0);var firstCursor=cursor.Pointer.sprite;
            float nextFrame=new ClassicSpriteFrames("ui","Basic.img/Cursor/1").Frames[0].Milliseconds/1000f+.001f;
            cursor.DrawPointer(npcScreen,ClassicCursorView.Clickable,nextFrame);Assert.That(cursor.Pointer.sprite,Is.Not.SameAs(firstCursor),"Hover frames animate using authored delays.");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-marker-available",1366,768,()=>{
                var screen=Pixels(indicator).center;cursor.DrawPointer(screen,cursor.StateAt(screen),1);
            });
            var talk=Object.FindFirstObjectByType<ClassicNpcDialogue>();
            yield return InventorySceneSmokeTests.Click("NpcQuest_1012111");Assert.That(talk.Visible,Is.True);talk.Close();
            var bruce=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111);
            Assert.That(world.StartQuest(bruce,2088,out _),Is.True);yield return null;yield return null;
            Assert.That(GameObject.Find("NpcQuest_1012111"),Is.Null);
            world.Player.Inventory.TryAddItem(4000011,10);world.Player.Inventory.TryAddItem(4000001,40);yield return null;yield return null;
            Assert.That(markers.ForNpc(anchor.NpcId),Is.EqualTo(ClassicQuestIndicators.Marker.Ready));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-marker-ready",1366,768);
            var journal=Object.FindFirstObjectByType<ClassicQuestView>();journal.Show(true);yield return null;yield return null;
            var panel=GameObject.Find("QuestPanel").GetComponent<RectTransform>();
            Assert.That(cursor.StateAt(Pixels(panel.GetComponentInChildren<ClassicWindowDrag>().GetComponent<RectTransform>()).center),Is.EqualTo(ClassicCursorView.Grabbable));
            Assert.That(cursor.StateAt(Pixels(panel).center),Is.EqualTo(ClassicCursorView.Idle),"A window blocks the NPC behind it.");
            var button=GameObject.Find("QuestTab_0").GetComponent<Button>();button.interactable=false;
            Assert.That(cursor.StateAt(Pixels(button.GetComponent<RectTransform>()).center),Is.EqualTo(ClassicCursorView.Idle));button.interactable=true;
            Assert.That(cursor.StateAt(Pixels(button.GetComponent<RectTransform>()).center),Is.EqualTo(ClassicCursorView.Clickable));
            Assert.That(world.FinishQuest(bruce,2088,out _),Is.True);yield return null;yield return null;
            Assert.That(GameObject.Find("NpcQuest_1012111"),Is.Null);
            world.LoadMap(100000001);yield return null;yield return null;Assert.That(markers.transform.Find("NpcQuestIndicators").childCount,Is.Zero);
        }
    }
}
