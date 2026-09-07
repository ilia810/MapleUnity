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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class MeleeSkillSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Rolls : System.Random { public override int Next() => 0; public override double NextDouble() => .5; }
        private static void Step(GameWorld world, int count = 1) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }
        private static void Finish(GameWorld world) { for(int i=0;i<180 && world.Player.IsBasicAttacking;i++)Step(world); Assert.That(world.Player.IsBasicAttacking,Is.False); }
        private static void Spawn(GameWorld world,float distance)
        {
            world.SpawnMonsterForTesting(100101,new LogicVector(world.Player.Position.X+distance,world.Player.Position.Y-Player.Height/2));
            var monster=world.Monsters.Last();monster.Template.BodyAttack=false;monster.SetMovementPattern(MovementPattern.Stationary);
        }
        [UnityTest]
        public IEnumerator RealHotbarCastsSingleAndSixTargetSkillsWithSourceArtAndDelayedRewards()
        {
            yield return SceneManager.LoadSceneAsync("henesys",LoadSceneMode.Single);yield return null;yield return null;
            var manager=Object.FindFirstObjectByType<GameManager>();manager.enabled=false;var world=manager.World;var p=world.Player;
            var combat=(Combat)typeof(GameWorld).GetField("combat",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(world);
            typeof(Combat).GetField("attackRandom",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(combat,new Rolls());
            typeof(Combat).GetField("damageRandom",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(combat,new Rolls());
            Step(world,100);world.RequestPracticeSupplies(out _);
            foreach(int id in new[]{1040002,1060002,1072001,1302000})Assert.That(world.UseInventoryItem(id,out _),Is.True);
            yield return InventorySceneSmokeTests.Click("SkillsToggle");yield return null;
            yield return InventorySceneSmokeTests.Click("PracticeSkills");yield return null;
            Object.FindFirstObjectByType<SkillMenu>().Show(false);yield return null;
            Canvas.ForceUpdateCanvases();
            foreach(int slot in new[]{0,1,2})
            {
                var icon=GameObject.Find("SkillSlot_"+slot).transform.Find("Icon").GetComponent<Image>();
                Assert.That(icon.sprite,Is.Not.Null);
                Assert.That(icon.rectTransform.rect.size,Is.EqualTo(new Vector2(32,32)),"Keep the original icon canvas and its transparent margins at native size.");
                Assert.That(GameObject.Find("SkillSlot_"+slot).transform.Find("Cooldown").GetComponent<Image>().enabled,Is.False);
            }
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-skill-hotbar-small",640,480);
            Spawn(world,.65f);var victim=world.Monsters.Single();int mp=p.CurrentMP;long exp=p.Experience;
            yield return InventorySceneSmokeTests.Click("SkillSlot_0");yield return null;
            Assert.That(p.CurrentMP,Is.EqualTo(mp-12));Assert.That(victim.IsDead,Is.False);Assert.That(p.Experience,Is.EqualTo(exp));
            Step(world,15);yield return null;yield return null;
            Assert.That(GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>().sprite,Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-power-strike-windup");
            while(p.BasicAttack.ElapsedMilliseconds<p.BasicAttack.HitDelayMilliseconds)Step(world);
            yield return null;yield return null;
            Assert.That(victim.IsDead,Is.True);Assert.That(p.Experience,Is.EqualTo(exp+victim.Template.Exp));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-power-strike-impact");
            Finish(world);yield return new WaitForSeconds(1.05f);
            foreach(float distance in new[]{.24f,.44f,.64f,.84f,1.04f,1.24f,1.32f})Spawn(world,distance);
            var targets=world.Monsters.OrderBy(m=>m.Position.X).ToArray();var health=targets.Select(m=>m.HP).ToArray();int hp=p.CurrentHP;mp=p.CurrentMP;
            yield return InventorySceneSmokeTests.Click("SkillSlot_1");yield return null;
            Assert.That(p.CurrentHP,Is.EqualTo(hp-16));Assert.That(p.CurrentMP,Is.EqualTo(mp-14));
            Assert.That(targets.Select(m=>m.HP).ToArray(),Is.EqualTo(health));
            while(p.BasicAttack.ElapsedMilliseconds<p.BasicAttack.HitDelayMilliseconds)Step(world);
            yield return null;yield return null;
            for(int i=0;i<targets.Length;i++)Assert.That(targets[i].HP,i<6?Is.LessThan(health[i]):Is.EqualTo(health[i]),"Target "+i);
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Count(v=>v.transform.Find("SkillHitEffect").GetComponent<SpriteRenderer>().sprite!=null),Is.EqualTo(6));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-slash-blast-six-targets");
            Finish(world);world.LoadMap(100000001);yield return null;yield return null;
            Assert.That(p.SkillEffect,Is.Null);Assert.That(targets.All(m=>m.SkillHitEffect==null),Is.True);
            Assert.That(world.SkillManager.GetSkillLevel(1001005),Is.EqualTo(20));
        }
    }
}
