using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class CombatStatSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
        }
        private sealed class Midpoint : System.Random { public override double NextDouble() => .5; }
        private static void Step(GameWorld world, int count = 1) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }
        private static void Finish(GameWorld world)
        {
            for (int i = 0; i < 150 && world.Player.IsBasicAttacking; i++) Step(world);
            Assert.That(world.Player.IsBasicAttacking, Is.False);
        }
        private static void Impact(GameWorld world, Input input)
        {
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            var swing = world.Player.BasicAttack;
            while (swing.ElapsedMilliseconds < swing.HitDelayMilliseconds) Step(world);
        }
        private static Rect ScreenRect(RectTransform transform)
        {
            var corners = new Vector3[4]; transform.GetWorldCorners(corners);
            var camera = transform.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private static void CheckReadout(int width, int height, bool inventoryOpen)
        {
            var panel = ScreenRect(GameObject.Find("CombatStatsPanel").GetComponent<RectTransform>());
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(width)); Assert.That(panel.yMax, Is.LessThanOrEqualTo(height));
            foreach (string name in new[] { "StatusContainer", "InventoryToggle" })
                Assert.That(panel.Overlaps(ScreenRect(GameObject.Find(name).GetComponent<RectTransform>())), Is.False, name);
            if (inventoryOpen) Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible,Is.True,"Stats and bag stay open together.");
            var text = GameObject.Find("CombatStats").GetComponent<Text>();
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + .1f), "Readout text must fit without clipping.");
        }

        [UnityTest]
        public IEnumerator RealPotionsShowLiveStatsAndTimersWhileMissesAndCriticalHitsUseDelayedFeedback()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            Object.FindFirstObjectByType<PlayerCombatFeedback>().ShowStats(true);
            var world = manager.World; var player = world.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            var combat = (Combat)typeof(GameWorld).GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
            // Fix only random rolls for reproducible miss/critical captures; use real stats and NX bounds.
            typeof(Combat).GetField("damageRandom", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, new Midpoint());
            Step(world, 100); Assert.That(world.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000 }) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            Assert.That(player.HasPracticeDamageOverride, Is.False); Assert.That(player.PhysicalAttackStats.Maximum, Is.EqualTo(12));
            var inventory = Object.FindFirstObjectByType<InventoryView>(); inventory.Show(true);
            yield return null; yield return InventorySceneSmokeTests.Click("InventoryCategory_2"); yield return null;
            yield return InventorySceneSmokeTests.Click("ItemRow_2002004"); yield return null;
            Assert.That(GameObject.Find("ItemDetails").GetComponent<Text>().text, Does.Contain("WeaponAttack +5"));
            Assert.That(GameObject.Find("ItemRow_2002004").transform.Find("Icon").GetComponent<Image>().sprite, Is.Not.Null);
            yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.PhysicalAttackStats.Maximum, Is.EqualTo(16));
            Assert.That(player.Inventory.GetItemCount(2002004), Is.EqualTo(2));
            yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.Inventory.GetItemCount(2002004), Is.EqualTo(1));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-warrior-potion", checkLayout: () => CheckReadout(1280, 720, true));
            yield return InventorySceneSmokeTests.Click("ItemRow_2002001"); yield return null; yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(player.Speed, Is.EqualTo(108));
            var stats = GameObject.Find("CombatStats").GetComponent<Text>();
            Assert.That(stats.text, Is.EqualTo("3–16")); Assert.That(GameObject.Find("Buff_-2002004/BuffTime").GetComponent<Text>().text,Is.EqualTo("180s"));
            Assert.That(GameObject.Find("Buff_-2002001/BuffTime").GetComponent<Text>().text,Is.EqualTo("180s"));
            yield return new WaitForSeconds(.1f);
            Assert.That(player.ActiveBuffs.First().RemainingMilliseconds, Is.EqualTo(180000), "Host time cannot expire a paused simulation buff.");
            inventory.Show(false); yield return PlayerCombatSceneSmokeTests.CaptureScreen("-active-buffs", checkLayout: () => CheckReadout(1280, 720, false));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-active-buffs-small", 640, 480, () => CheckReadout(640, 480, false));

            world.SpawnMonsterForTesting(100101, new LogicVector(player.Position.X + .65f, player.Position.Y - Player.Height / 2));
            var monster = world.Monsters.Single(); monster.Template.BodyAttack = false; monster.SetMovementPattern(MovementPattern.Stationary);
            monster.Template.Avoidability = 1000000; player.CriticalChance = 1;
            int hp = monster.HP; long experience = player.Experience; AttackHit result = default; int reports = 0;
            world.AttackResolved += (_, hit) => { result = hit; reports++; };
            Impact(world, input); yield return null; yield return null;
            Assert.That(result.Miss, Is.True); Assert.That(monster.HP, Is.EqualTo(hp)); Assert.That(monster.IsHit, Is.False);
            Assert.That(player.Experience, Is.EqualTo(experience)); Assert.That(GameObject.Find("MonsterMiss").GetComponent<Text>().text, Is.EqualTo("MISS"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-melee-miss");
            Finish(world); yield return new WaitForSeconds(1.05f); monster.Template.Avoidability = 0;
            Impact(world, input); yield return null; yield return null;
            Assert.That(reports, Is.EqualTo(2)); Assert.That(result.Critical, Is.True); Assert.That(result.Damage, Is.GreaterThan(0));
            Assert.That(monster.HP, Is.EqualTo(System.Math.Max(0, hp - result.Damage)));
            Assert.That(GameObject.Find("MonsterCritical").GetComponent<Text>().text, Is.EqualTo(result.Damage.ToString()));
            Assert.That(player.Experience, Is.EqualTo(experience + (monster.IsDead ? monster.Template.Exp : 0)));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-melee-critical");
            Finish(world); player.CriticalChance = .05f;
            world.LoadMap(100000001); yield return null; yield return null;
            Assert.That(player.WeaponAttack, Is.EqualTo(22)); Assert.That(player.Speed, Is.EqualTo(108));
            Assert.That(player.ActiveBuffs.Count(), Is.EqualTo(2));
            // Shorten only this fixture's remaining duration to exercise the expiry UI without a three-minute scene loop.
            foreach (int id in new[] { 2002004, 2002001 })
            {
                var item = player.GetItemInfo(id); player.ApplyStatBuffs(-id, item.Name, item.Buffs, 16);
            }
            Step(world, 2); yield return null; yield return null;
            Assert.That(player.ActiveBuffs, Is.Empty); Assert.That(player.WeaponAttack, Is.EqualTo(17)); Assert.That(player.Speed, Is.EqualTo(100));
            Assert.That(stats.text, Does.Not.Contain("Potion")); Assert.That(stats.text, Is.EqualTo("2–12"));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-buffs-expired");
        }
    }
}
