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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class SkillSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private static void Step(GameWorld world, int count = 1) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }
        [UnityTest]
        public IEnumerator RealSkillBookPracticeMasteryAndBoosterWorkThroughTheUi()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var player = world.Player; Step(world, 100);
            world.RequestPracticeSupplies(out _);
            foreach (int id in new[] {1040002,1060002,1072001,1302000}) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            player.AddExperience(5); long exp = player.Experience; int level = player.Level, maxHp = player.MaxHP;
            yield return InventorySceneSmokeTests.Click("SkillsToggle"); yield return null;
            yield return InventorySceneSmokeTests.Click("PracticeSkills"); yield return null;
            Assert.That(player.JobId, Is.EqualTo(110)); Assert.That(player.Level, Is.EqualTo(level)); Assert.That(player.Experience, Is.EqualTo(exp));
            Assert.That(player.MaxHP, Is.EqualTo(maxHp)); Assert.That(player.PhysicalAttackStats.Minimum, Is.EqualTo(8)); Assert.That(player.Accuracy, Is.EqualTo(39));
            Assert.That(world.RequestPracticeSkills(out _), Is.False);
            Assert.That(GameObject.Find("SkillRow_1100000").transform.Find("Icon").GetComponent<Image>().sprite, Is.Not.Null);
            yield return InventorySceneSmokeTests.Click("SkillRow_1100000");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text, Does.Contain("Passive"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-skills-mastery");
            yield return InventorySceneSmokeTests.Click("SkillRow_1101004"); yield return null;
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text, Does.Contain("HP 10 / MP 10").And.Contain("200s"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-skills-booster-small", 640, 480);
            int hp = player.CurrentHP, mp = player.CurrentMP;
            yield return InventorySceneSmokeTests.Click("CastSkill"); yield return null;
            Assert.That(player.CurrentHP, Is.EqualTo(hp - 10)); Assert.That(player.CurrentMP, Is.EqualTo(mp - 10));
            Assert.That(player.AttackSpeedModifier, Is.EqualTo(-2)); Assert.That(player.IsBasicAttacking, Is.True);
            Assert.That(GameObject.Find("CastSkill").GetComponent<Button>().interactable, Is.False);
            Assert.That(player.BasicAttack.Stance, Is.EqualTo(CharacterState.Alert));
            Object.FindFirstObjectByType<SkillMenu>().Show(false);
            Step(world, 15); yield return null; yield return null;
            var effect = GameObject.Find("SkillUseEffect").GetComponent<SpriteRenderer>();
            Assert.That(effect.sprite, Is.Not.Null); Assert.That(effect.sprite.texture.width, Is.GreaterThan(1));
            int frame = player.BasicAttack.Frame; int timer = player.ActiveBuffs.Single().RemainingMilliseconds;
            yield return new WaitForSeconds(.15f);
            Assert.That(player.BasicAttack.Frame, Is.EqualTo(frame)); Assert.That(player.ActiveBuffs.Single().RemainingMilliseconds, Is.EqualTo(timer));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-skills-casting");
            Step(world, 35); Assert.That(player.IsBasicAttacking, Is.False);
            Assert.That(player.PlayBasicAttackAnimation(0), Is.True); Assert.That(player.BasicAttack.HitDelayMilliseconds, Is.EqualTo(233));
            player.BasicAttack.Cancel();
            world.LoadMap(100000001); yield return null; yield return null;
            Assert.That(player.SkillEffect, Is.Null); Assert.That(player.CombatMastery, Is.EqualTo(.6f)); Assert.That(player.AttackSpeedModifier, Is.EqualTo(-2));
            Assert.That(world.SkillManager.GetSkillLevel(1100000), Is.EqualTo(20));
            Assert.That(world.RemoveEquipment(EquipSlot.Weapon, out _), Is.True);
            Assert.That(player.CombatMastery, Is.Zero); Assert.That(player.Accuracy, Is.EqualTo(19));
            Assert.That(world.UseInventoryItem(1302000, out _), Is.True); Assert.That(player.CombatMastery, Is.EqualTo(.6f));
            var booster = player.ActiveBuffs.Single(); player.ApplyStatBuffs(booster.SourceId, booster.Name, new Dictionary<BuffType,int> {[BuffType.Booster]=-2}, 16);
            Step(world, 2); yield return null; yield return null;
            Assert.That(player.AttackSpeedModifier, Is.Zero); Assert.That(player.CombatMastery, Is.EqualTo(.6f));
            Assert.That(GameObject.Find("CombatStats").GetComponent<Text>().text, Does.Not.Contain("Sword Booster"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-skills-after-travel");
        }
    }
}
