using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MapleClient.GameLogic.Core;
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
    public class ClassicVisualFinishSceneSmokeTests
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
        private static void Beside(GameWorld world,WorldLabelAnchor npc,float x=0)
        {
            world.Player.ResetMovementForMap();world.Player.Position=new LogicVector(npc.Feet.x+x,npc.Feet.y+Player.Height/2);
            world.Player.IsGrounded=false;for(int i=0;i<100;i++)world.UpdatePhysics(.008f);
        }
        private static Rect Pixels(RectTransform r)
        {
            var corners=new Vector3[4];r.GetWorldCorners(corners);var canvas=r.GetComponentInParent<Canvas>().rootCanvas;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        [UnityTest] public IEnumerator NpcFramesKeepTheirFeetAndPortraitOriginWhileOverlappingNamesSeparate()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();
            var bruce=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc&&a.NpcId==1012111);
            var animator=bruce.GetComponent<ClassicNpcAnimator>();Assert.That(animator,Is.Not.Null);
            Assert.That(animator.Stances,Does.Contain("say"));Assert.That(animator.Stances,Does.Not.Contain("info"));
            var feet=bruce.Feet;var art=bruce.GetComponentInChildren<SpriteRenderer>();
            animator.DialogueSpeaking=true;animator.enabled=false;animator.PresentAt(0);var first=art.sprite;
            animator.PresentAt(1.1);Assert.That(art.sprite,Is.Not.SameAs(first));Assert.That(bruce.Feet,Is.EqualTo(feet));
            Assert.That(art.transform.localPosition.x,Is.EqualTo(-animator.Frame.Origin.x/100).Within(.0001));
            Assert.That(art.transform.localPosition.y,Is.EqualTo(animator.Frame.Origin.y/100).Within(.0001));
            bruce.transform.localScale=new Vector3(-1,1,1);animator.PresentAt(1.2);
            Assert.That(bruce.Feet,Is.EqualTo(feet));Assert.That(art.flipX,Is.False,"Facing belongs to the NPC root, not a second flip on its frames.");
            Beside(world,bruce);world.Player.Name="Local Explorer";yield return null;yield return null;
            Action names=()=>{
                var labels=GameObject.Find("WorldNameplates").transform;
                var player=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Player);
                var p=(RectTransform)labels.Find("Nameplate_Player_"+player.GetInstanceID());
                var n=(RectTransform)labels.Find("Nameplate_Npc_"+bruce.GetInstanceID());
                Assert.That(n.gameObject.activeSelf&&p.gameObject.activeSelf,Is.True);
                Assert.That(Pixels(n).Overlaps(Pixels(p)),Is.False,"Both names must remain legible when the actors share their feet position.");
                Assert.That(Mathf.Abs(Pixels(n).yMax-Pixels(p).yMax),Is.LessThanOrEqualTo(108));
                Assert.That(labels.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),Is.True);
            };
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-separated-nameplates",1366,768,names);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-separated-nameplates-small",640,480,names);
            var talk=Object.FindFirstObjectByType<ClassicNpcDialogue>();
            Assert.That(talk.TryOpen(world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111)),Is.True);yield return null;
            animator.DialogueSpeaking=true;
            Action portrait=()=>{
                animator.PresentAt(1.1);var view=GameObject.Find("DialogueNpcPortrait").GetComponent<ClassicNpcPortrait>();view.Present();
                Assert.That(view.GetComponent<Image>().sprite,Is.SameAs(animator.Frame.Sprite));
                Assert.That(view.transform.localScale.x,Is.EqualTo(1));
                var r=view.GetComponent<RectTransform>();Assert.That(-r.anchoredPosition.y+r.rect.height,Is.LessThanOrEqualTo(155.01f));
            };
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-animated-npc-portrait",1366,768,portrait);
            talk.Close();animator.enabled=true;
            var linked=new GameObject("LinkedNpcAnimationCheck");linked.transform.position=feet+Vector3.right*2;
            var linkedArt=new GameObject("Sprite").AddComponent<SpriteRenderer>();linkedArt.transform.SetParent(linked.transform,false);
            var linkedAnimator=linked.AddComponent<ClassicNpcAnimator>();linkedAnimator.Bind(2005,linkedArt);
            Assert.That(linkedAnimator.Stances,Does.Contain("heart"));Assert.That(linkedAnimator.Frame.Sprite.texture.name,Does.Contain("0002003.img"));
            linked.AddComponent<WorldLabelAnchor>().BindNpc(2005);Assert.That(linked.GetComponent<WorldLabelAnchor>().Label,Is.EqualTo("Sam"));
            Object.Destroy(linked);world.LoadMap(100000102);yield return null;yield return null;
            Assert.That(bruce==null,Is.True);Assert.That(talk.Visible,Is.False);
            var luna=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc);
            Assert.That(luna.GetComponent<ClassicNpcAnimator>().Frame.Sprite,Is.Not.Null);
        }
        [UnityTest] public IEnumerator NpcNamesStayAnchoredWhilePlayersPassOnBothSides()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Pause();
            var npc=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc&&a.NpcId==1012111);
            Beside(world,npc);yield return null;yield return null;
            // Hold the camera still so this tests label displacement, independently of camera follow.
            var camera=Camera.main;var behaviours=camera.GetComponents<MonoBehaviour>().Where(b=>b.enabled).ToArray();
            foreach(var b in behaviours)b.enabled=false;
            var labels=GameObject.Find("WorldNameplates").transform;
            var actor=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Player);
            var n=(RectTransform)labels.Find("Nameplate_Npc_"+npc.GetInstanceID());
            var p=(RectTransform)labels.Find("Nameplate_Player_"+actor.GetInstanceID());
            var baseline=n.anchoredPosition;var feet=npc.Feet;
            foreach(float x in new[]{-2f,-.6f,0f,.6f,2f,0f})
            {
                Beside(world,npc,x);yield return null;yield return null;
                Assert.That(npc.Feet,Is.EqualTo(feet));Assert.That(n.gameObject.activeSelf,Is.True);
                Assert.That(n.anchoredPosition,Is.EqualTo(baseline),"An NPC's name and service cannot be displaced by a player.");
                if(p.gameObject.activeSelf)Assert.That(Pixels(n).Overlaps(Pixels(p)),Is.False);
            }
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-npc-name-priority");
            foreach(var b in behaviours)b.enabled=true;
        }
        [UnityTest] public IEnumerator SuccessfulQuestJobAndBuffItemActionsPlayIndependentOneShotsWithoutReplay()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Pause();
            var player=world.Player;var effects=Object.FindFirstObjectByType<ClassicPlayerEffects>();
            Assert.That(world.TryAdvanceFirstJob(100,out _),Is.False);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.JobChanged),Is.False);
            while(player.Level<10)player.AddExperience(player.ExperienceToNextLevel-player.Experience);
            var bruce=WorldLabelAnchor.Active.Single(a=>a.Kind==WorldLabelAnchor.ActorKind.Npc&&a.NpcId==1012111);Beside(world,bruce,-.5f);
            yield return new WaitForSeconds(2.2f);
            Assert.That(world.TryAdvanceFirstJob(100,out var message),Is.True,message);yield return null;
            var job=effects.RendererFor(ClassicPlayerEffects.Kind.JobChanged);Assert.That(job.sprite.texture.name,Does.Contain("JobChanged"));
            Assert.That(job.sortingOrder,Is.EqualTo(StageRenderOrder.PlayerOrder(player.CurrentFootholdLayer)-1));
            yield return new WaitForSeconds(.35f);yield return PlayerCombatSceneSmokeTests.CaptureScreen("-job-advancement-effect",1366,768);
            yield return new WaitForSeconds(1.1f);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.JobChanged),Is.False);
            Assert.That(world.TryAdvanceFirstJob(100,out _),Is.False);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.JobChanged),Is.False);
            var npc=world.CurrentMap.NpcSpawns.Single(n=>n.NpcId==1012111);
            Assert.That(world.StartQuest(npc,2088,out message),Is.True,message);
            Assert.That(world.FinishQuest(npc,2088,out _),Is.False);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.QuestClear),Is.False);
            player.Inventory.TryAddItem(4000011,10);player.Inventory.TryAddItem(4000001,40);
            player.AddExperience(player.ExperienceToNextLevel-player.Experience-100);
            Assert.That(world.FinishQuest(npc,2088,out message),Is.True,message);yield return null;
            Assert.That(effects.Playing,Is.True,"The reward's level-up must coexist with quest-clear art.");
            Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.QuestClear),Is.True);
            Assert.That(effects.RendererFor(ClassicPlayerEffects.Kind.QuestClear).sprite.texture.name,Does.Contain("QuestClear"));
            yield return new WaitForSeconds(.35f);yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quest-completion-effects",1366,768);
            yield return new WaitForSeconds(2.1f);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.QuestClear),Is.False);
            Assert.That(world.FinishQuest(npc,2088,out _),Is.False);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.QuestClear),Is.False);
            Assert.That(player.TryUseItem(2002004,out _),Is.False);Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.Buff),Is.False);
            player.Inventory.TryAddItem(2002004,1);Assert.That(player.TryUseItem(2002004,out message),Is.True,message);yield return null;
            Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.Buff),Is.True);
            yield return new WaitForSeconds(.25f);yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buff-item-effect",1366,768);
            var saved=world.CaptureProgress();Assert.That(world.TryRestoreProgress(saved,out message),Is.True,message);yield return null;yield return null;
            foreach(ClassicPlayerEffects.Kind kind in Enum.GetValues(typeof(ClassicPlayerEffects.Kind)))Assert.That(effects.IsPlaying(kind),Is.False,"Restoring progress must not replay "+kind);
            player.Inventory.TryAddItem(2002004,1);Assert.That(player.TryUseItem(2002004,out _),Is.True);yield return null;
            world.LoadMap(100000001);yield return null;yield return null;
            Assert.That(effects.IsPlaying(ClassicPlayerEffects.Kind.Buff),Is.False,"Travel clears effects parented to the previous actor.");
        }
    }
}
