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
    public class MagicSkillSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Rolls : System.Random { public override int Next() => 0; public override double NextDouble() => .5; }
        private static void Step(GameWorld world, int count = 1) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }
        [UnityTest]
        public IEnumerator MagicianPracticeRendersTravellingBoltAndTwoClawHitsWithCostsAndTravelCleanup()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false; var world = manager.World; var p = world.Player;
            var combat = (Combat)typeof(GameWorld).GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
            foreach (string field in new[] { "attackRandom", "damageRandom" })
                typeof(Combat).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, new Rolls());
            Step(world, 100); p.AddExperience(3); world.RequestPracticeSupplies(out _);
            foreach (int id in new[] { 1040002, 1060002, 1072001 }) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            yield return InventorySceneSmokeTests.Click("SkillsToggle"); yield return null;
            yield return InventorySceneSmokeTests.Click("PracticeMagicSkills"); yield return null;
            Assert.That(p.Level, Is.EqualTo(8)); Assert.That(p.Experience, Is.EqualTo(3)); Assert.That(p.JobId, Is.EqualTo(200));
            Assert.That(world.CanRequestPracticeMagicSkills, Is.False);
            Assert.That(world.SkillManager.GetSkillLevel(2001005), Is.EqualTo(20));
            yield return InventorySceneSmokeTests.Click("SkillRow_2001004");
            Assert.That(GameObject.Find("SkillDetails").GetComponent<Text>().text, Does.Contain("MP 14"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-magician-book-small", 640, 480);
            yield return InventorySceneSmokeTests.Click("CloseSkills"); yield return null;
            yield return InventorySceneSmokeTests.Click("InventoryToggle"); yield return null;
            yield return InventorySceneSmokeTests.Click("ItemRow_1372005"); yield return null;
            yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(p.EquippedWeaponType, Is.EqualTo(137));
            Object.FindFirstObjectByType<InventoryView>().Show(false); yield return null;
            Assert.That(GameObject.Find("StatMagic").GetComponent<Text>().text,Is.EqualTo(p.MagicAttack.ToString()),"The MAGIC stat shows the real magic attack value.");
            world.SpawnMonsterForTesting(100101, new LogicVector(p.Position.X + 3.2f, p.Position.Y - Player.Height / 2));
            var target = world.Monsters.Single(); target.Template.BodyAttack = false; target.SetMovementPattern(MovementPattern.Stationary);
            int hp = target.HP, mp = p.CurrentMP; var hits = new List<AttackHit>(); world.AttackResolved += (_, hit) => hits.Add(hit);
            yield return InventorySceneSmokeTests.Click("SkillSlot_0"); yield return null;
            Assert.That(p.CurrentMP, Is.EqualTo(mp - 14)); Assert.That(target.HP, Is.EqualTo(hp));
            for (int i = 0; i < 160 && !world.Projectiles.Any(); i++) Step(world);
            Assert.That(world.Projectiles.Count(), Is.EqualTo(1)); Step(world, 20); yield return null; yield return null;
            var bullet = world.Projectiles.Single(); var bulletSprite = GameObject.Find("SkillProjectile").GetComponent<SpriteRenderer>();
            Assert.That(bulletSprite.sprite, Is.Not.Null); Assert.That(bulletSprite.sprite.name, Does.Contain("/ball/"));
            Assert.That(bulletSprite.flipX, Is.True); Assert.That(target.HP, Is.EqualTo(hp));
            var position = bullet.Position; int elapsed = bullet.ElapsedMilliseconds;
            yield return new WaitForSeconds(.12f);
            Assert.That(bullet.Position, Is.EqualTo(position)); Assert.That(bullet.ElapsedMilliseconds, Is.EqualTo(elapsed));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-energy-bolt-flight");
            for (int i = 0; i < 160 && hits.Count == 0; i++) Step(world);
            yield return null; yield return null;
            Assert.That(hits.Count, Is.EqualTo(1)); Assert.That(hits[0].Damage, Is.EqualTo(7)); Assert.That(target.HP, Is.EqualTo(hp - 7));
            Assert.That(world.Projectiles, Is.Empty);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-energy-bolt-impact");
            Step(world, 150); yield return new WaitForSeconds(1.05f); hits.Clear(); long exp = p.Experience;
            yield return InventorySceneSmokeTests.Click("SkillSlot_1"); yield return null;
            Assert.That(p.CurrentMP, Is.EqualTo(mp - 34));
            for (int i = 0; i < 160 && hits.Count == 0; i++) Step(world);
            yield return null; yield return null;
            Assert.That(hits.Count, Is.EqualTo(2)); Assert.That(target.IsDead, Is.True); Assert.That(p.Experience, Is.EqualTo(exp + target.Template.Exp));
            var numbers = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.name == "MonsterDamage").OrderBy(t => t.rectTransform.anchoredPosition.y).ToArray();
            Assert.That(numbers.Length, Is.EqualTo(2));
            Assert.That(numbers[1].rectTransform.anchoredPosition.y - numbers[0].rectTransform.anchoredPosition.y, Is.GreaterThan(20));
            Assert.That(Object.FindFirstObjectByType<MonsterView>().transform.Find("SkillHitEffect").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-magic-claw-two-hits");
            Step(world, 150); yield return InventorySceneSmokeTests.Click("SkillSlot_0");
            for (int i = 0; i < 160 && !world.Projectiles.Any(); i++) Step(world);
            Assert.That(world.Projectiles.Any(), Is.True);
            world.LoadMap(100000001); yield return null; yield return null;
            Assert.That(world.Projectiles, Is.Empty); Assert.That(GameObject.Find("SkillProjectile"), Is.Null);
            Assert.That(world.SkillManager.GetSkillLevel(2001004), Is.EqualTo(20)); Assert.That(p.EquippedWeaponType, Is.EqualTo(137));
        }
    }
}
