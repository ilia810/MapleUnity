using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
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
    public class ClassicNpcPresentationSceneSmokeTests
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
        private static void Beside(GameWorld world,WorldLabelAnchor npc,float x)
        {
            world.Player.ResetMovementForMap();world.Player.Position=new LogicVector(npc.Feet.x+x,npc.Feet.y+Player.Height/2);
            world.Player.IsGrounded=false;for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
        }
        [UnityTest] public IEnumerator AuthoredAmbientBubblesAndBothDialogueSidesStayReadableAndNonblocking()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();
            while(world.Player.Level<10)world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);
            var anchor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc && a.NpcId==1012111);
            Beside(world,anchor,-.5f);yield return null;yield return new WaitForSeconds(2.2f);
            var bubbles=Object.FindFirstObjectByType<ClassicNpcBubbles>();float speechClock=12-anchor.NpcId%120/10f+.1f;
            bubbles.PresentAt(speechClock);var bubble=GameObject.Find("NpcSpeechBubble_1012111");Assert.That(bubble,Is.Not.Null);
            Assert.That(bubble.GetComponentInChildren<Text>().text,Does.Contain("scholar"));
            Assert.That(bubble.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),Is.True);
            Assert.That(bubble.GetComponentsInChildren<Image>().All(i=>i.sprite!=null),Is.True);
            var speechLayer=GameObject.Find("NpcSpeechBubbles").transform.GetSiblingIndex();
            Assert.That(speechLayer,Is.GreaterThan(GameObject.Find("WorldNameplates").transform.GetSiblingIndex()));
            Assert.That(speechLayer,Is.LessThan(GameObject.Find("MiniMapPanel").transform.GetSiblingIndex()));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Pixels(bubble.GetComponent<RectTransform>()).center},hits);
            Assert.That(hits.Any(h=>h.gameObject.transform.IsChildOf(bubble.transform)),Is.False);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-ambient",1366,768,()=>bubbles.PresentAt(speechClock));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-ambient-small",640,480,()=>{
                bubbles.PresentAt(speechClock);
                foreach(Transform b in GameObject.Find("NpcSpeechBubbles").transform)
                    if(b.gameObject.activeSelf){var rect=Pixels((RectTransform)b);Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(rect.xMax,Is.LessThanOrEqualTo(640));Assert.That(rect.yMax,Is.LessThanOrEqualTo(480));}
            });
            bubbles.PresentAt(speechClock+4.1f);Assert.That(GameObject.Find("NpcSpeechBubble_1012111"),Is.Null);
            var talk=Object.FindFirstObjectByType<ClassicNpcDialogue>();var npc=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==anchor.NpcId);
            Assert.That(talk.TryOpen(npc),Is.True);Assert.That(talk.PortraitOnRight,Is.True);
            yield return InventorySceneSmokeTests.Click("NpcQuest_2088");bubbles.PresentAt(speechClock);Assert.That(GameObject.Find("NpcSpeechBubble_1012111"),Is.Null,"The speaking NPC's ambient bubble pauses during conversation.");
            System.Action checkRight=()=>{
                var text=GameObject.Find("NpcSpeech").GetComponent<RectTransform>();var portrait=GameObject.Find("DialogueNpcPortrait").GetComponent<RectTransform>();
                Assert.That(Pixels(text).xMax,Is.LessThan(Pixels(portrait).xMin));Assert.That(portrait.localScale.x,Is.EqualTo(1));
                Assert.That(GameObject.Find("DialogueTop").transform.localScale.x,Is.EqualTo(-1));
                Assert.That(text.GetComponent<ScrollRect>().verticalScrollbar.gameObject.activeSelf,Is.False,"Short dialogue pages do not need a scrollbar.");
            };
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-dialogue-right",1366,768,checkRight);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-dialogue-right-small",640,480,checkRight);
            talk.Close();Beside(world,anchor,.5f);yield return null;Assert.That(talk.TryOpen(npc),Is.True);Assert.That(talk.PortraitOnRight,Is.False);
            yield return InventorySceneSmokeTests.Click("NpcQuest_2088");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-dialogue-left",1366,768,()=>{
                Assert.That(Pixels(GameObject.Find("DialogueNpcPortrait").GetComponent<RectTransform>()).xMax,Is.LessThan(Pixels(GameObject.Find("NpcSpeech").GetComponent<RectTransform>()).xMin));
                Assert.That(GameObject.Find("DialogueTop").transform.localScale.x,Is.EqualTo(1));
            });
            yield return InventorySceneSmokeTests.Click("NpcYes");Assert.That(world.Quests.State(2088),Is.EqualTo(MapleClient.GameLogic.Data.QuestState.InProgress));
            world.LoadMap(100000102);yield return null;yield return null;Assert.That(GameObject.Find("NpcSpeechBubble_1012111"),Is.Null);Assert.That(talk.Visible,Is.False);
        }
        [UnityTest] public IEnumerator LevelUpUsesOriginalOneShotAndExpPickupMessagesStackAboveTheHud()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();
            var effects=Object.FindFirstObjectByType<ClassicPlayerEffects>();var notices=Object.FindFirstObjectByType<ClassicNotifications>();
            world.Player.AddExperience(15);yield return null;Assert.That(effects.Playing,Is.True);Assert.That(effects.Renderer.sprite,Is.Not.Null);
            var first=effects.Renderer.sprite;yield return new WaitForSeconds(.13f);Assert.That(effects.Renderer.sprite,Is.Not.SameAs(first));
            world.AddDroppedItem(0,50,world.Player.Position);world.AddDroppedItem(2000000,2,world.Player.Position);world.UpdatePhysics(.008f);yield return null;
            var messages=notices.GetComponentsInChildren<Text>().Where(t=>t.name=="EventNotification").ToArray();
            Assert.That(messages.Any(t=>t.text.Contains("Level 2")),Is.True);Assert.That(messages.Any(t=>t.text=="You received EXP (+15)."),Is.True);
            Assert.That(messages.Any(t=>t.text.Contains("50 mesos")),Is.True);Assert.That(messages.Any(t=>t.text.Contains("Red Potion ×2")),Is.True);
            Assert.That(messages.All(t=>!t.raycastTarget),Is.True);
            yield return new WaitForSeconds(.95f);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-level-up-art",1366,768,()=>{
                Assert.That(effects.Renderer.sprite.texture.name,Does.Contain("LevelUp"));
                Assert.That(effects.Renderer.flipX,Is.False);Assert.That(effects.Renderer.sortingOrder,Is.EqualTo(StageRenderOrder.PlayerOrder(world.Player.CurrentFootholdLayer)+1));
            });
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-notifications-small",640,480,()=>{
                var r=Pixels(GameObject.Find("EventNotifications").GetComponent<RectTransform>());
                Assert.That(r.xMax,Is.EqualTo(632).Within(1));Assert.That(r.yMin,Is.GreaterThan(125));Assert.That(r.yMax,Is.LessThan(480));
                foreach(var t in messages)Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height+.1f));
            });
            yield return new WaitForSeconds(1.1f);Assert.That(effects.Playing,Is.False);Assert.That(effects.Renderer.enabled,Is.False);
            for(int i=0;i<12;i++)notices.Post("Notification "+i,Color.white);Assert.That(notices.Count,Is.EqualTo(6));
            yield return new WaitForSeconds(6.1f);Assert.That(notices.Count,Is.Zero);
            world.Player.AddExperience(world.Player.ExperienceToNextLevel-world.Player.Experience);yield return null;Assert.That(effects.Playing,Is.True);
            world.LoadMap(100000001);yield return null;yield return null;Assert.That(effects.Playing,Is.False);Assert.That(notices.Count,Is.Zero);
            var save=world.CaptureProgress();Assert.That(world.TryRestoreProgress(save,out _),Is.True);yield return null;Assert.That(effects.Playing,Is.False,"Loading progress does not replay level-up art.");
        }
    }
}
