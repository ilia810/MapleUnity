using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Vec = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class ThiefSkillSceneSmokeTests
    {
        private readonly List<string> diagnostics=new List<string>();
        [SetUp] public void Watch(){diagnostics.Clear();Application.logMessageReceived+=Log;}
        private void Log(string text,string stack,LogType type){if(type!=LogType.Log)diagnostics.Add(type+": "+text);}
        [TearDown] public void Check(){Application.logMessageReceived-=Log;Assert.That(diagnostics,Is.Empty);}
        private sealed class Rolls : System.Random { public override int Next()=>0; public override double NextDouble()=>.5; }
        private static void Step(GameWorld world,int ticks=1){for(int i=0;i<ticks;i++)world.UpdatePhysics(.008f);}
        private static GameWorld Prepare()
        {
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;Step(world,100);
            world.RequestPracticeSupplies(out _);
            foreach(int id in new[]{1040002,1060002,1072001})Assert.That(world.UseInventoryItem(id,out _),Is.True);
            return world;
        }
        [UnityTest] public IEnumerator EarnedDisorderUnlocksDarkSightInTheOriginalBook()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Prepare();var p=world.Player;
            while(p.Level<10)p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            Assert.That(world.TryAdvanceFirstJob(400,out _),Is.True);p.AddExperience(p.ExperienceToNextLevel-p.Experience);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return InventorySceneSmokeTests.Click("SkillRow_4001003");
            Assert.That(GameObject.Find("SpendSkillPoint_4001003").GetComponent<Button>().interactable,Is.False);
            yield return InventorySceneSmokeTests.Click("SkillRow_4001002");
            for(int i=0;i<3;i++)yield return InventorySceneSmokeTests.Click("SpendSkillPoint_4001002");
            yield return InventorySceneSmokeTests.Click("SkillRow_4001002");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("Enemy attack -3, defense -3"));
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text,Does.Contain("No damage"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-thief-earned-disorder");
            yield return InventorySceneSmokeTests.Click("SkillRow_4001003");yield return InventorySceneSmokeTests.Click("SpendSkillPoint_4001003");
            yield return InventorySceneSmokeTests.Click("AssignSkill_2");Object.FindFirstObjectByType<SkillMenu>().Show(false);
            Assert.That(p.SkillPoints,Is.Zero);yield return InventorySceneSmokeTests.Click("SkillSlot_2");Step(world,20);yield return null;yield return null;
            Assert.That(p.IsHidden,Is.True);Assert.That(GameObject.Find("Buff_4001003"),Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-thief-earned-dark-sight");
            var save=world.CaptureProgress();Assert.That(world.TryRestoreProgress(save,out var error),Is.True,error);
            Assert.That(world.SkillManager.GetSkillLevel(4001002),Is.EqualTo(3));Assert.That(world.SkillManager.GetSkillLevel(4001003),Is.EqualTo(1));
            Assert.That(p.IsHidden,Is.False);
        }
        [UnityTest] public IEnumerator DaggerPracticeCastsDisorderAndDoubleStabWithNativeMonsterEffects()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var world=Prepare();var p=world.Player;
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            typeof(Combat).GetField("attackRandom",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(combat,new Rolls());
            typeof(Combat).GetField("damageRandom",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(combat,new Rolls());
            yield return InventorySceneSmokeTests.OpenPractice();
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-thief-practice-small",640,480);
            yield return InventorySceneSmokeTests.Click("RangedPreset_133");
            Assert.That(world.UseInventoryItem(1332005,out var error),Is.True,error);
            Object.FindFirstObjectByType<InventoryView>().ShowBag(false);yield return null;
            Assert.That(world.SkillManager.GetSkillLevel(4001002),Is.EqualTo(1));
            world.SkillManager.SetSkillLevel(4001002,20);
            world.SpawnMonsterForTesting(130101,new Vec(p.Position.X+.45f,p.Position.Y-Player.Height/2));
            var mob=world.Monsters.Single();mob.Template.BodyAttack=false;mob.SetMovementPattern(MovementPattern.Stationary);
            int hp=mob.HP,attack=mob.PhysicalAttack,mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");
            Assert.That(mob.StatDebuff,Is.Null);Assert.That(p.CurrentMP,Is.EqualTo(mp-10));
            while(p.BasicAttack.ElapsedMilliseconds<p.BasicAttack.HitDelayMilliseconds)Step(world);
            Step(world,35);yield return null;yield return null;
            Assert.That(mob.StatDebuff,Is.Not.Null);Assert.That(mob.HP,Is.EqualTo(hp));Assert.That(mob.PhysicalAttack,Is.EqualTo(System.Math.Max(0,attack-20)));
            var view=Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Single(v=>v.Model==mob);
            var status=view.transform.Find("MonsterStatusEffect").GetComponent<SpriteRenderer>();
            Assert.That(status.sprite,Is.Not.Null);Assert.That(status.sprite.name,Does.Contain("4001002/mob/"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-disorder-monster-effect");
            var frame=status.sprite;int time=mob.StatDebuff.RemainingMilliseconds;
            yield return null;yield return null;Assert.That(status.sprite,Is.SameAs(frame));Assert.That(mob.StatDebuff.RemainingMilliseconds,Is.EqualTo(time));
            Step(world,200);int reports=0;combat.AttackResolved+=(_,target,hit)=>{if(target==mob)reports++;};mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_1");Assert.That(p.CurrentMP,Is.EqualTo(mp-8));
            while(reports<2&&p.IsBasicAttacking)Step(world);
            yield return null;yield return null;Assert.That(reports,Is.EqualTo(2));Assert.That(mob.HP,Is.LessThan(hp));
            var hit=view.transform.Find("SkillHitEffect").GetComponent<SpriteRenderer>();Assert.That(hit.sprite,Is.Not.Null);
            Assert.That(hit.sprite.name,Does.Contain("4001334/CharLevel/10/hit/0/"));
            Assert.That(status.sprite,Is.Not.Null,"The debuff and hit effect coexist.");
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-double-stab-impact");
            Step(world,8000);yield return null;yield return null;
            Assert.That(mob.StatDebuff,Is.Null);Assert.That(status.sprite,Is.Null);Assert.That(mob.PhysicalAttack,Is.EqualTo(attack));
            world.LoadMap(100000001);yield return null;yield return null;
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).All(v=>v.Model!=mob),Is.True);
        }
    }
}
