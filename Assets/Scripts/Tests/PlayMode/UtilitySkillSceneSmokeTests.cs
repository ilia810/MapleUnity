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

namespace MapleClient.Tests.PlayMode
{
    public class UtilitySkillSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void Watch(){diagnostics.Clear();Application.logMessageReceived+=Log;}
        private void Log(string text,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+text);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(diagnostics,Is.Empty);}
        private static void Step(GameWorld world,int ticks){for(int i=0;i<ticks;i++)world.UpdatePhysics(.008f);}
        private static GameWorld Prepare()
        {
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;Step(world,100);
            world.RequestPracticeSupplies(out _);
            foreach(int id in new[]{1040002,1060002,1072001})Assert.That(world.UseInventoryItem(id,out _),Is.True);
            return world;
        }
        private static IEnumerator Cancel(string name)
        {
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            var cell=GameObject.Find(name);Assert.That(cell,Is.Not.Null);
            var r=cell.GetComponent<RectTransform>();var canvas=cell.GetComponentInParent<Canvas>();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right,
                position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,r.TransformPoint(r.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<ClassicItemClick>(),Is.SameAs(cell.GetComponent<ClassicItemClick>()));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
        }
        private sealed class MissRoll : System.Random { public override double NextDouble()=>.999; }
        [UnityTest] public IEnumerator NimbleBodyEvasionShowsOriginalMissArtWithoutLosingResources()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Prepare();var p=world.Player;
            p.JobId=400;world.SkillManager.SetSkillLevel(4000000,20);
            world.SpawnMonsterForTesting(130101,new MapleClient.GameLogic.Vector2(p.Position.X,p.Position.Y-Player.Height/2));
            var mob=world.Monsters.Single();mob.Template.BodyAttack=true;mob.Template.Accuracy=10;
            int hp=p.CurrentHP,mp=p.CurrentMP;var combat=new Combat(contactRandom:new MissRoll());
            Assert.That(combat.CheckContact(p,new[]{mob}),Is.SameAs(mob));yield return null;yield return null;
            Assert.That(p.CurrentHP,Is.EqualTo(hp));Assert.That(p.CurrentMP,Is.EqualTo(mp));
            var number=GameObject.Find("PlayerDamage");Assert.That(number,Is.Not.Null);Assert.That(number.GetComponent<Text>().text,Is.EqualTo("MISS"));
            Assert.That(number.GetComponent<ClassicDamageNumber>(),Is.Not.Null);
            Assert.That(number.GetComponentsInChildren<Image>().Any(i=>i.sprite!=null),Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-nimble-body-miss");
        }
        [UnityTest] public IEnumerator EarnedArcherPassivesUnlockFocusAndBuffIconsCancelTheirEffects()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Prepare();var p=world.Player;
            while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(300,out _),Is.True);p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("SkillRow_3001003");
            Assert.That(GameObject.Find("SpendSkillPoint_3001003").GetComponent<Button>().interactable,Is.False);
            yield return InventorySceneSmokeTests.Click("SkillRow_3000000");
            for(int i=0;i<3;i++)yield return InventorySceneSmokeTests.Click("SpendSkillPoint_3000000");
            Assert.That(world.SkillManager.GetSkillLevel(3000000),Is.EqualTo(3));
            yield return InventorySceneSmokeTests.Click("SkillRow_3000000");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Accuracy +3"));
            yield return InventorySceneSmokeTests.Click("SkillRow_3001003");yield return InventorySceneSmokeTests.Click("SpendSkillPoint_3001003");
            Assert.That(p.SkillPoints,Is.Zero);yield return InventorySceneSmokeTests.Click("AssignSkill_0");
            Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return new WaitForSeconds(3);
            int accuracy=p.Accuracy,avoid=p.Avoidability,mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(world,10);yield return null;yield return null;
            Assert.That(p.Accuracy,Is.EqualTo(accuracy+1));Assert.That(p.Avoidability,Is.EqualTo(avoid+1));Assert.That(p.CurrentMP,Is.EqualTo(mp-8));
            var effect=GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>();Assert.That(effect.sprite,Is.Not.Null);
            Assert.That(effect.sprite.name,Does.Contain("3001003/effect"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-focus-cast");yield return null;yield return null;
            yield return Cancel("Buff_3001003");Assert.That(GameObject.Find("Buff_3001003"),Is.Null);
            Assert.That(p.Accuracy,Is.EqualTo(accuracy));Assert.That(p.Avoidability,Is.EqualTo(avoid));
            p.Inventory.AddItem(2002005,1);Assert.That(world.UseInventoryItem(2002005,out _),Is.True);yield return null;yield return null;
            Assert.That(p.Accuracy,Is.EqualTo(accuracy+5));yield return Cancel("Buff_-2002005");Assert.That(p.Accuracy,Is.EqualTo(accuracy));
        }
        [UnityTest] public IEnumerator DarkSightRendersAllCharacterLayersAndRestoresThemAfterCancellationAndLoad()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;var world=Prepare();var p=world.Player;
            yield return InventorySceneSmokeTests.OpenPractice();yield return InventorySceneSmokeTests.Click("OpenSupportPractice");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-utility-picker-small",640,480);yield return null;yield return null;
            yield return InventorySceneSmokeTests.Click("SupportPreset_400");Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return null;
            int hp=p.CurrentHP,mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(world,20);yield return null;yield return null;
            Assert.That(p.IsHidden,Is.True);Assert.That(p.Speed,Is.EqualTo(70));Assert.That(p.CurrentMP,Is.EqualTo(mp-24));
            Assert.That(p.ReceiveContactDamage(20,false),Is.False);Assert.That(p.CurrentHP,Is.EqualTo(hp));
            var root=GameObject.Find("Player").transform.Find("VisualRoot");
            var layers=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.transform.parent==root&&r.enabled&&r.sprite!=null&&r.name!="SkillUseEffect"&&r.name!="WeaponAfterimage").ToArray();
            Assert.That(layers.Length,Is.GreaterThan(3));
            Assert.That(layers.All(r=>Mathf.Abs(r.color.a-.35f)<.001f),Is.True,"Every visible body/equipment layer shares concealment opacity.");
            var icon=GameObject.Find("Buff_4001003").transform.Find("Icon").GetComponent<Image>();Assert.That(icon.sprite,Is.Not.Null);Assert.That(icon.color.a,Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-dark-sight");yield return null;yield return null;
            Assert.That(p.PlayBasicAttackAnimation(),Is.False);yield return Cancel("Buff_4001003");
            Assert.That(p.IsHidden,Is.False);Assert.That(p.Speed,Is.EqualTo(100));Assert.That(layers.All(r=>Mathf.Abs(r.color.a-1)<.001f),Is.True);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-dark-sight-cancelled");yield return null;yield return null;
            Step(world,100);yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(world,20);yield return null;yield return null;
            var save=world.CaptureProgress();world.LoadMap(100000001);yield return null;yield return null;Assert.That(p.IsHidden,Is.True);
            Assert.That(world.TryRestoreProgress(save,out var message),Is.True,message);yield return null;yield return null;
            Assert.That(p.IsHidden,Is.False);Assert.That(GameObject.Find("Buff_4001003"),Is.Null);
            Assert.That(world.SkillManager.GetSkillLevel(4001003),Is.EqualTo(1));
        }
    }
}
