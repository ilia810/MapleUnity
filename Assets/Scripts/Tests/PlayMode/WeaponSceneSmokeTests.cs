using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class WeaponSceneSmokeTests
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
        private static void Step(GameWorld world, int count) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }

        [UnityTest]
        public IEnumerator WeaponSwapsRenderSourcePosesAndGripOrdersWhileCombatUsesTheSameSwingClock()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var player = world.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            Step(world, 100);
            Assert.That(world.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] { 1040002, 1060002, 1072001 }) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            var inventory = Object.FindFirstObjectByType<InventoryView>(); inventory.Show(true);
            yield return null; yield return null; // Let the newly opened canvas build its row graphics.
            yield return InventorySceneSmokeTests.Click("ItemRow_1402009");
            yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon], Is.EqualTo(1402009));
            yield return InventorySceneSmokeTests.OpenEquipment();
            yield return InventorySceneSmokeTests.Click("EquipmentRow_1402009"); yield return null;
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-weapon-inventory");
            yield return InventorySceneSmokeTests.OpenBag();
            inventory.Show(false);
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            SpriteRenderer Layer(string name) => visual.Find(name).GetComponent<SpriteRenderer>();
            var body = Layer("Body"); var arm = Layer("Arm"); var sleeve = Layer("Equipment_Top_mailArm"); var weapon = Layer("Equipment_Weapon_weapon");
            Assert.That(body.sprite.name, Does.Contain("/stand2/"));
            Assert.That(weapon.sortingOrder, Is.GreaterThan(sleeve.sortingOrder));
            InventorySceneSmokeTests.Capture(player, "-bat-standing");

            input.IsRightPressed = true; Step(world, 12); yield return null;
            Assert.That(body.sprite.name, Does.Contain("/walk1/"), "The bat explicitly uses walk1 despite being two-handed.");
            Assert.That(weapon.sortingOrder, Is.LessThan(arm.sortingOrder));
            InventorySceneSmokeTests.Capture(player, "-bat-walking");
            input.IsRightPressed = false; Step(world, 15);
            input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false; yield return null;
            var motion = player.BasicAttack;
            Assert.That(WeaponProfile.AttackStances(5), Does.Contain(motion.Stance));
            Assert.That(body.sprite.name, Does.Contain("/" + CharacterStances.Name(motion.Stance) + "/"));
            Assert.That(world.UseInventoryItem(1302000, out _), Is.False, "An unfinished swing locks equipment changes.");
            yield return new WaitForSeconds(.12f);
            Assert.That(motion.Frame, Is.Zero, "Host rendering must not advance a paused simulation's attack.");
            Step(world, 40); yield return null;
            Assert.That(motion.Frame, Is.EqualTo(1)); Assert.That(weapon.sprite, Is.Not.Null); Assert.That(sleeve.sprite, Is.Not.Null);
            Assert.That(weapon.sprite.name, Does.Contain($"/{CharacterStances.Name(motion.Stance)}/{motion.Frame}/"));
            InventorySceneSmokeTests.Capture(player, "-bat-attack");
            Step(world, 48); Assert.That(player.IsBasicAttacking, Is.True); Step(world, 1);
            Assert.That(player.IsBasicAttacking, Is.False); yield return null;

            // The kit preserves the fence's real level requirement. Set a fixture level to test its art and swaps.
            Assert.That(world.UseInventoryItem(1092003, out _), Is.False);
            player.Level = 5; inventory.Show(true);
            yield return InventorySceneSmokeTests.Click("ItemRow_1092003"); yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            Assert.That(player.GetEquippedItems().ContainsKey(EquipSlot.Weapon), Is.False);
            Assert.That(player.Inventory.GetItemCount(1402009), Is.EqualTo(1));
            yield return InventorySceneSmokeTests.Click("ItemRow_1302000"); yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            inventory.Show(false);
            Assert.That(player.GetEquippedItems()[EquipSlot.Shield], Is.EqualTo(1092003));
            Assert.That(player.WeaponDefense, Is.EqualTo(22)); Assert.That(weapon.sprite.name, Does.Contain("01302000.img"));
            Assert.That(Layer("Equipment_Shield_shield").sprite, Is.Not.Null);
            InventorySceneSmokeTests.Capture(player, "-sword-shield");

            inventory.Show(true);
            yield return InventorySceneSmokeTests.Click("ItemRow_1442079"); yield return InventorySceneSmokeTests.Click("ItemAction"); yield return null;
            inventory.Show(false);
            Assert.That(player.GetEquippedItems().ContainsKey(EquipSlot.Shield), Is.False);
            Assert.That(player.Inventory.GetItemCount(1092003), Is.EqualTo(1)); Assert.That(player.Inventory.GetItemCount(1302000), Is.EqualTo(1));
            Assert.That(Layer("Equipment_Shield_shield").sprite, Is.Null);
            input.IsLeftPressed = true; Step(world, 12); yield return null;
            Assert.That(body.sprite.name, Does.Contain("/walk2/"));
            Assert.That(weapon.sortingOrder, Is.GreaterThan(sleeve.sortingOrder));
            InventorySceneSmokeTests.Capture(player, "-polearm-walking");
            input.IsLeftPressed = false; Step(world, 30);
            input.IsDownPressed = true; Step(world, 1); input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false;
            yield return null;
            Assert.That(player.BasicAttack.Stance, Is.EqualTo(CharacterState.ProneStab));
            Assert.That(body.sprite.name, Does.Contain("/proneStab/")); Assert.That(weapon.sprite, Is.Not.Null);
            InventorySceneSmokeTests.Capture(player, "-polearm-prone");
            input.IsDownPressed = false;
            world.LoadMap(100000001); yield return null;
            Assert.That(player.IsBasicAttacking, Is.False, "Travel cancels the old map's swing.");
            Assert.That(player.GetEquippedItems()[EquipSlot.Weapon], Is.EqualTo(1442079));
            Assert.That(player.EquipmentBonus(StatType.WeaponAttack), Is.EqualTo(22));
        }
    }
}
