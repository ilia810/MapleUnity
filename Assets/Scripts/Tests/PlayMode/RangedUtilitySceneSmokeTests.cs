using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Skills;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using Vec=MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class RangedUtilitySceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void Watch(){diagnostics.Clear();Application.logMessageReceived+=Log;}
        private void Log(string message,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+message);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(diagnostics,Is.Empty);}
        private sealed class Rolls:System.Random{public override int Next()=>0;public override double NextDouble()=>0;}
        private static void Step(GameWorld w,int n){for(int i=0;i<n;i++)w.UpdatePhysics(.008f);}
        private static GameWorld Prepare()
        {
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var w=manager.World;Step(w,100);
            w.RequestPracticeSupplies(out _);foreach(int id in new[]{1040002,1060002,1072001})Assert.That(w.UseInventoryItem(id,out _),Is.True);return w;
        }
        [UnityTest] public IEnumerator ArcherPassivesLearnThroughTheBookUpdateStatsAndRenderCriticalHits()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var w=Prepare();var p=w.Player;
            while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(w.TryAdvanceFirstJob(300,out _),Is.True);
            while(p.Level<12)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("SkillRow_3000002");
            Assert.That(GameObject.Find("SpendSkillPoint_3000002").GetComponent<Button>().interactable,Is.False);
            for(int i=0;i<3;i++)yield return InventorySceneSmokeTests.Click("SpendSkillPoint_3000000");
            yield return InventorySceneSmokeTests.Click("SpendSkillPoint_3000002");yield return InventorySceneSmokeTests.Click("SpendSkillPoint_3000001");
            yield return InventorySceneSmokeTests.Click("SkillRow_3000002");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Projectile range +15 px"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-eye-of-amazon");yield return null;
            yield return InventorySceneSmokeTests.Click("SkillRow_3000001");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Critical chance 12%"));
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Critical damage 105%"));
            Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return null;
            Assert.That(w.RequestWeaponPractice(145,out _),Is.True);Assert.That(p.TryEquipItem(GameWorld.RangedPracticeWeapon(145),out _),Is.True);
            w.SkillManager.SetSkillLevel(3000001,20);w.SkillManager.SetSkillLevel(3000002,8);
            yield return InventorySceneSmokeTests.Click("StatsToggle");yield return null;yield return null;
            Assert.That(GameObject.Find("StatCritRate").GetComponent<Text>().text,Does.Contain("40"));
            Assert.That(GameObject.Find("StatCritDamage").GetComponent<Text>().text,Does.Contain("200"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-critical-shot-stats");
            Object.FindFirstObjectByType<CharacterProgressionView>().Show(false);yield return null;
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w);
            foreach(string field in new[]{"attackRandom","damageRandom"})typeof(Combat).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(combat,new Rolls());
            w.SpawnMonsterForTesting(130101,new Vec(p.Position.X+4.8f,p.Position.Y-Player.Height/2));
            var mob=w.Monsters.Single();mob.Template.BodyAttack=false;mob.SetMovementPattern(MovementPattern.Stationary);
            var seen=new List<AttackHit>();w.AttackResolved+=(_,hit)=>seen.Add(hit);
            Object.FindFirstObjectByType<SkillBar>().AssignSkillToSlot(3001004,0);yield return null;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");
            for(int i=0;i<500&&seen.Count==0;i++)Step(w,1);yield return null;yield return null;
            Assert.That(seen.Single().Critical,Is.True);Assert.That(GameObject.Find("MonsterCritical"),Is.Not.Null);
            Assert.That(GameObject.Find("MonsterCritical").transform.Find("CriticalBurst").GetComponent<Image>().sprite,Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-critical-shot-impact");
        }
        [UnityTest] public IEnumerator KeenEyesIsLearnableAfterNimbleBodyAndUsesItsOriginalIcon()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var w=Prepare();var p=w.Player;while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(w.TryAdvanceFirstJob(400,out _),Is.True);p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("SkillRow_4000001");
            Assert.That(GameObject.Find("SpendSkillPoint_4000001").GetComponent<Button>().interactable,Is.False);
            for(int i=0;i<3;i++)yield return InventorySceneSmokeTests.Click("SpendSkillPoint_4000000");
            yield return InventorySceneSmokeTests.Click("SpendSkillPoint_4000001");yield return InventorySceneSmokeTests.Click("SkillRow_4000001");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Projectile range +25 px"));
            var icon=GameObject.Find("SkillRow_4000001").transform.Find("Icon").GetComponent<Image>();Assert.That(icon.sprite,Is.Not.Null);
            Assert.That(w.SkillManager.GetSkillLevel(4000001),Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-keen-eyes-book",640,480);
        }
        [UnityTest] public IEnumerator BothHastePresetsFitAndCastOriginalArtworkFromQuickslots()
        {
            foreach(int job in new[]{410,420})
            {
                yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
                var w=Prepare();var p=w.Player;int id=GameWorld.SupportPracticeSkills(job).Single();
                yield return InventorySceneSmokeTests.OpenPractice();yield return InventorySceneSmokeTests.Click("OpenSupportPractice");
                if(job==410)
                {
                    yield return PlayerCombatSceneSmokeTests.CaptureScreen("-haste-picker-small",640,480);yield return null;yield return null;
                    var corners=new UnityEngine.Vector3[4];GameObject.Find("SupportPracticePanel").GetComponent<RectTransform>().GetWorldCorners(corners);
                    Assert.That(corners.All(v=>v.x>=0&&v.x<=Screen.width&&v.y>=0&&v.y<=Screen.height),Is.True);
                }
                yield return InventorySceneSmokeTests.Click("SupportPreset_"+job);
                Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return null;yield return null;
                int mp=p.CurrentMP;yield return InventorySceneSmokeTests.Click("SkillSlot_0");Step(w,15);yield return null;yield return null;
                Assert.That(p.Speed,Is.EqualTo(102));Assert.That(p.JumpPower,Is.EqualTo(121));Assert.That(p.CurrentMP,Is.EqualTo(mp-15));
                var effect=GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>();Assert.That(effect.sprite,Is.Not.Null);Assert.That(effect.sprite.name,Does.Contain(id+"/effect/"));
                var icon=GameObject.Find("Buff_"+id).transform.Find("Icon").GetComponent<Image>();Assert.That(icon.sprite,Is.Not.Null);
                yield return PlayerCombatSceneSmokeTests.CaptureScreen("-haste-"+job);yield return null;
                var cell=GameObject.Find("Buff_"+id);var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right};
                ExecuteEvents.Execute(cell,pointer,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
                Assert.That(p.Speed,Is.EqualTo(100));Assert.That(p.JumpPower,Is.EqualTo(120));Assert.That(GameObject.Find("Buff_"+id),Is.Null);
            }
        }
    }
}
