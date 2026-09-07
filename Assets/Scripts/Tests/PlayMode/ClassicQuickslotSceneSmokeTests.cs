using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class ClassicQuickslotSceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        private string savePath;
        [SetUp] public void Watch() { errors.Clear(); Application.logMessageReceived += Log; savePath = Path.Combine(Application.temporaryCachePath, "quickslot-scene-" + Guid.NewGuid().ToString("N") + ".json"); }
        private void Log(string text, string stack, LogType type) { if (type != LogType.Log) errors.Add(type + ": " + text); }
        [TearDown] public void Check()
        {
            Application.logMessageReceived -= Log;
            foreach (var path in new[] {savePath, savePath + ".bak", savePath + ".tmp"}) if (File.Exists(path)) File.Delete(path);
            Assert.That(errors, Is.Empty);
        }
        private static GameManager Pause()
        {
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            for (int i = 0; i < 100; i++) manager.World.UpdatePhysics(.008f);
            return manager;
        }
        private static PointerEventData Begin(GameObject source)
        {
            source.GetComponentInParent<ClassicWindow>()?.Focus(); Canvas.ForceUpdateCanvases();
            var r = (RectTransform)source.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                pointerDrag = source, position = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center)), eligibleForClick = true };
            ExecuteEvents.Execute(source, pointer, ExecuteEvents.beginDragHandler); return pointer;
        }
        private static void Drop(PointerEventData pointer, int index)
        {
            var destination = GameObject.Find("SkillSlot_" + index); var r = (RectTransform)destination.transform;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.dragHandler);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.First().gameObject.GetComponentInParent<SkillSlot>(), Is.EqualTo(destination.GetComponent<SkillSlot>()));
            ExecuteEvents.Execute(destination, pointer, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
        }
        private static Text Quantity(int index) => GameObject.Find("SkillSlot_" + index).transform.Find("Level").GetComponent<Text>();
        [UnityTest] public IEnumerator NativeKeysDragCountsAndShortcutsRemainUsableAtBothResolutions()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Pause(); var world = manager.World; var player = world.Player; var bar = Object.FindFirstObjectByType<SkillBar>();
            Assert.That(world.RequestPracticeSupplies(out var error), Is.True, error);
            Assert.That(world.RequestPracticeMagicSkills(out error), Is.True, error);
            player.Inventory.AddItem(2000000, 201);
            var inventory = Object.FindFirstObjectByType<InventoryView>(); inventory.ShowBag(true);
            yield return InventorySceneSmokeTests.Click("InventoryCategory_2"); yield return null; yield return null;
            int before = player.Inventory.GetItemCount(2000000);
            var pointer = Begin(GameObject.Find("ItemRow_2000000")); Assert.That(pointer.eligibleForClick, Is.False); Drop(pointer, 2);
            Assert.That(bar.BindingAt(2).Kind, Is.EqualTo(QuickslotKind.Item));
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(before), "Assignment must not move or consume a bag item.");
            yield return null; yield return null; Assert.That(Quantity(2).text, Is.EqualTo(before.ToString()));
            var stack = player.Inventory.GetStacks().First(s => s.ItemId == 2000000);
            Assert.That(world.MoveInventoryStack(2, stack.Slot, 20, stack.ItemId, player.Inventory.Revision, out error), Is.True, error);
            yield return null; yield return null; Assert.That(Quantity(2).text, Is.EqualTo(before.ToString()));
            player.SetHPMP(1, player.MaxMP);
            // Both aliases pressed in one frame still consume only once.
            bar.HandleKeys(key => key == KeyCode.Home || key == KeyCode.Alpha3);
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(before - 1));
            Assert.That(player.CurrentHP, Is.GreaterThan(1)); yield return null; yield return null;
            var stale = Begin(GameObject.Find("ItemRow_2000000")); player.Inventory.AddItem(2000003, 1); Drop(stale, 6);
            Assert.That(bar.BindingAt(6).Kind, Is.EqualTo(QuickslotKind.Empty));
            Assert.That(bar.AssignItemToSlot(1302000, 6), Is.False); Assert.That(bar.AssignItemToSlot(2060000, 6), Is.False);
            inventory.ShowBag(false);
            yield return InventorySceneSmokeTests.Click("SkillsToggle"); yield return InventorySceneSmokeTests.Click("SkillTier_1");
            yield return null; yield return null;
            Drop(Begin(GameObject.Find("SkillRow_2001004")), 0);
            Assert.That(bar.CaptureSlots()[0], Is.EqualTo(2001004));
            Drop(Begin(GameObject.Find("SkillSlot_0")), 2);
            Assert.That(bar.BindingAt(0).Kind, Is.EqualTo(QuickslotKind.Item)); Assert.That(bar.CaptureSlots()[2], Is.EqualTo(2001004));
            Drop(Begin(GameObject.Find("SkillSlot_2")), 0);
            bar.AssignSkillToSlot(2001005, 1); bar.AssignActionToSlot(QuickslotAction.Jump, 2);
            bar.AssignItemToSlot(2000001, 5); bar.AssignItemToSlot(2000002, 6);
            yield return InventorySceneSmokeTests.Click("CloseSkills"); yield return null; yield return null;
            for (int i = 0; i < 8; i++)
            {
                var slot = GameObject.Find("SkillSlot_" + i).GetComponent<SkillSlot>();
                Assert.That(slot.transform.Find("Hotkey").GetComponent<Image>().sprite.name, Does.EndWith("StatusBar.img/key/" + i));
                Assert.That(slot.Icon.sprite, Is.Not.Null); Assert.That(slot.transform.Find("Cooldown").GetComponent<Image>().sprite, Is.Not.Null);
            }
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quickslots-wide", 1366, 768);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-quickslots-small", 640, 480);
            yield return InventorySceneSmokeTests.Click("Shortcuts"); yield return InventorySceneSmokeTests.Click("ShortcutBinding_6");
            yield return InventorySceneSmokeTests.Click("BindJump"); Assert.That(bar.BindingAt(6).Id, Is.EqualTo(53));
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-shortcut-settings-small", 640, 480);
            yield return InventorySceneSmokeTests.Click("ClearQuickslot"); Assert.That(bar.BindingAt(6).Kind, Is.EqualTo(QuickslotKind.Empty));
            yield return InventorySceneSmokeTests.Click("CloseShortcuts");
            int total = player.Inventory.GetItemCount(2000003); player.Inventory.RemoveItem(2000003, total);
            yield return null; yield return null; Assert.That(Quantity(7).text, Is.EqualTo("0")); Assert.That(bar.Activate(7), Is.False);
            Assert.That(bar.BindingAt(7).Id, Is.EqualTo(2000003)); player.Inventory.AddItem(2000003, 1);
            yield return null; yield return null; Assert.That(Quantity(7).text, Is.EqualTo("1"));
            bar.ToggleVisible(); bar.HandleKeys(key => key == KeyCode.PageDown); // A full MP bar preserves the potion.
            Assert.That(player.Inventory.GetItemCount(2000003), Is.EqualTo(1)); player.CurrentMP = 1;
            bar.HandleKeys(key => key == KeyCode.PageDown); Assert.That(player.Inventory.GetItemCount(2000003), Is.Zero); bar.ToggleVisible();
            var controller = manager.GetComponent<LocalProgressController>(); controller.SaveFilePath = savePath;
            Assert.That(controller.TrySave(out error), Is.True, error); var bindings = bar.CaptureBindings();
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            manager = Pause(); controller = manager.GetComponent<LocalProgressController>(); controller.SaveFilePath = savePath;
            Assert.That(controller.TryLoad(out error), Is.True, error); bar = Object.FindFirstObjectByType<SkillBar>(); yield return null; yield return null;
            Assert.That(bar.CaptureBindings().Select(b => b.Kind), Is.EqualTo(bindings.Select(b => b.Kind)));
            Assert.That(bar.CaptureBindings().Select(b => b.Id), Is.EqualTo(bindings.Select(b => b.Id)));
            Assert.That(Quantity(7).text, Is.EqualTo("0"));
        }
        [UnityTest] public IEnumerator RebindingCtrlDoesNotAlsoAttackAndTypingBlocksAllQuickslotDispatch()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var manager = Pause(); var world = manager.World; var player = world.Player; var bar = Object.FindFirstObjectByType<SkillBar>();
            Assert.That(bar.ActionPressed(QuickslotAction.Attack, key => key == KeyCode.LeftControl), Is.True);
            player.Inventory.AddItem(2000000, 2); player.SetHPMP(1, player.MaxMP); bar.AssignItemToSlot(2000000, 4);
            Assert.That(bar.ActionPressed(QuickslotAction.Attack, key => key == KeyCode.LeftControl), Is.False);
            bar.HandleKeys(key => key == KeyCode.LeftControl); Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(1));
            Assert.That(player.IsBasicAttacking, Is.False);
            var field = new GameObject("TestTyping", typeof(RectTransform), typeof(InputField)); field.transform.SetParent(bar.transform, false);
            EventSystem.current.SetSelectedGameObject(field); player.SetHPMP(1, player.MaxMP);
            bar.HandleKeys(key => true); Assert.That(bar.Activate(4), Is.False);
            Assert.That(player.Inventory.GetItemCount(2000000), Is.EqualTo(1));
            bar.AssignActionToSlot(QuickslotAction.Attack, 0); Assert.That(bar.ActionPressed(QuickslotAction.Attack, key => true), Is.False);
            EventSystem.current.SetSelectedGameObject(null); Object.Destroy(field);
            yield return InventorySceneSmokeTests.Click("SkillSlot_0"); world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(player.IsBasicAttacking, Is.True, "Clicking Attack must go through the normal simulation input path.");
            Assert.That(bar.ActionPressed(QuickslotAction.Attack, key => false), Is.False, "A click is consumed only once.");
            for (int i = 0; i < 180; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
            Assert.That(player.IsBasicAttacking, Is.False);
            bar.AssignActionToSlot(QuickslotAction.Jump, 6); float start = player.Position.Y;
            yield return InventorySceneSmokeTests.Click("SkillSlot_6");
            for (int i = 0; i < 3; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
            Assert.That(player.IsGrounded, Is.False); Assert.That(player.Position.Y, Is.GreaterThan(start));
            bar.RestoreSlots(new[] {0, 2001004}); // Unknown/unlearned legacy assignment cannot activate a skill.
            Assert.That(bar.BindingAt(4).Kind, Is.EqualTo(QuickslotKind.Action));
            bar.Activate(4); world.LoadMap(world.CurrentMapId); Assert.That(bar.ActionPressed(QuickslotAction.Attack, key => false), Is.False);
        }
    }
}
