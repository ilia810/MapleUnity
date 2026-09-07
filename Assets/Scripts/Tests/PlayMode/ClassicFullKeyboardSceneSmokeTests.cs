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
    public class ClassicFullKeyboardSceneSmokeTests
    {
        private readonly List<string> errors = new List<string>();
        private string savePath;
        [SetUp] public void Watch()
        {
            errors.Clear(); Application.logMessageReceived += Log;
            savePath = Path.Combine(Application.temporaryCachePath, "full-keyboard-" + Guid.NewGuid().ToString("N") + ".json");
        }
        private void Log(string text, string stack, LogType type) { if (type != LogType.Log) errors.Add(type + ": " + text); }
        [TearDown] public void Check()
        {
            Application.logMessageReceived -= Log;
            foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".tmp" }) if (File.Exists(path)) File.Delete(path);
            Assert.That(errors, Is.Empty);
        }
        private static GameManager Pause()
        {
            var game = Object.FindFirstObjectByType<GameManager>(); game.enabled = false;
            for (int i = 0; i < 100; i++) game.World.UpdatePhysics(.008f); return game;
        }
        private static IEnumerator Move(KeyboardKey source, KeyboardKey destination) =>
            ClassicKeyboardScrollbarSceneSmokeTests.Drag(ClassicKeyboardLayout.ObjectName(source), ClassicKeyboardLayout.ObjectName(destination));
        [UnityTest] public IEnumerator EveryKeyHasADragTargetAndMovedCommandsDriveWindowsMovementExpressionsAndNumberKeys()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Pause(); var bar = Object.FindFirstObjectByType<SkillBar>();
            bar.ShowShortcuts(); yield return null; yield return null;
            var keys = GameObject.Find("QuickslotSettings").GetComponentsInChildren<ClassicKeyboardKey>();
            Assert.That(keys.Select(k => k.Key), Is.EquivalentTo(KeyboardMap.Keys));
            Assert.That(ClassicKeyboardLayout.Keys.Select(k => k.Key).Distinct().Count(), Is.EqualTo(KeyboardMap.Keys.Count));
            foreach (var key in keys)
            {
                Assert.That(key.GetComponent<ClassicQuickslotDrag>().SourceKey, Is.EqualTo(key.Key));
                Assert.That(ClassicKeyboardLayout.Native(key.Key), Is.Not.EqualTo(KeyCode.None));
            }
            yield return Move(KeyboardKey.I, KeyboardKey.R);
            var bag = Object.FindFirstObjectByType<InventoryView>();
            bar.HandleKeys(k => k == KeyCode.I); Assert.That(bag.BagVisible, Is.False);
            bar.HandleKeys(k => k == KeyCode.R); Assert.That(bag.BagVisible, Is.True);
            Assert.That(GameObject.Find("QuickslotSettings"), Is.Not.Null, "Windows remain independently open."); bag.ShowBag(false);
            yield return Move(KeyboardKey.A, KeyboardKey.T);
            Assert.That(bar.ActionPressed(QuickslotAction.Left, k => k == KeyCode.A), Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Left, k => k == KeyCode.T), Is.True);
            yield return Move(KeyboardKey.F1, KeyboardKey.B);
            Assert.That(bar.ExpressionPressed(k => k == KeyCode.F1), Is.Null);
            Assert.That(bar.ExpressionPressed(k => k == KeyCode.B), Is.EqualTo(MapleClient.GameLogic.Data.CharacterExpressions.ForFunctionKey(1)));
            yield return Move(KeyboardKey.Z, KeyboardKey.X);
            Assert.That(bar.ActionPressed(QuickslotAction.Attack, k => k == KeyCode.Z), Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Attack, k => k == KeyCode.X), Is.True);
            yield return Move(KeyboardKey.Space, KeyboardKey.J);
            Assert.That(bar.ActionPressed(QuickslotAction.Jump, k => k == KeyCode.Space), Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Jump, k => k == KeyCode.J), Is.True);
            yield return Move(KeyboardKey.R, KeyboardKey.K); // Swaps inventory and the skill book.
            Assert.That(bar.BindingAtKey(KeyboardKey.R).Id, Is.EqualTo((int)QuickslotAction.Skills));
            bar.HandleKeys(k => k == KeyCode.K); Assert.That(bag.BagVisible, Is.True); bag.ShowBag(false);
            yield return Move(KeyboardKey.K, KeyboardKey.Alpha4);
            Assert.That(bar.Bindings.IsNumberAlias(KeyboardKey.Alpha4), Is.False);
            Assert.That(bar.BindingAtKey(KeyboardKey.K).Id, Is.EqualTo(2000000));
            Assert.That(bar.BindingAt(3).Id, Is.EqualTo(2000000));
            bar.HandleKeys(k => k == KeyCode.Alpha4); Assert.That(bag.BagVisible, Is.True); bag.ShowBag(false);
            yield return Move(KeyboardKey.Escape, KeyboardKey.F12);
            bar.HandleKeys(k => k == KeyCode.Escape); Assert.That(GameObject.Find("QuickslotSettings"), Is.Not.Null);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-full-keyboard-wide", 1366, 768);
            yield return PlayerCombatSceneSmokeTests.CaptureScreen("-full-keyboard-small", 640, 480);
            bar.HandleKeys(k => k == KeyCode.F12); Assert.That(GameObject.Find("QuickslotSettings"), Is.Null);
            bar.HandleKeys(k => k == KeyCode.R); Assert.That(Object.FindFirstObjectByType<SkillMenu>().Visible, Is.True);
            var controller = game.GetComponent<LocalProgressController>(); controller.SaveFilePath = savePath;
            Assert.That(controller.TrySave(out var error), Is.True, error);
            var expected = bar.Bindings.Capture().Select(e => (e.Key, e.Binding.Kind, e.Binding.Id)).ToArray();
            game.World.LoadMap(100000102); yield return null; yield return null;
            Assert.That(bar.Bindings.Capture().Select(e => (e.Key, e.Binding.Kind, e.Binding.Id)), Is.EqualTo(expected));
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            game = Pause(); bar = Object.FindFirstObjectByType<SkillBar>(); controller = game.GetComponent<LocalProgressController>(); controller.SaveFilePath = savePath;
            Assert.That(controller.TryLoad(out error), Is.True, error);
            Assert.That(bar.Bindings.Capture().Select(e => (e.Key, e.Binding.Kind, e.Binding.Id)), Is.EqualTo(expected));
            bar.HandleKeys(k => k == KeyCode.I); Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible, Is.False);
            bar.HandleKeys(k => k == KeyCode.Alpha4); Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible, Is.True);
        }
        [UnityTest] public IEnumerator TypingBlocksRemappedCommandsAndClearAndDefaultsCoverTheEntireKeyboard()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Pause(); var bar = Object.FindFirstObjectByType<SkillBar>(); bar.ShowShortcuts(); yield return null; yield return null;
            yield return Move(KeyboardKey.I, KeyboardKey.Return);
            Assert.That(EventSystem.current.sendNavigationEvents, Is.False, "Return/Space must not also submit a selected Unity button.");
            var typing = new GameObject("KeyboardTypingProbe", typeof(RectTransform), typeof(InputField));
            EventSystem.current.SetSelectedGameObject(typing);
            bar.HandleKeys(k => k == KeyCode.Return);
            Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible, Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Left, _ => true), Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Jump, _ => true), Is.False);
            Assert.That(bar.ExpressionPressed(_ => true), Is.Null);
            EventSystem.current.SetSelectedGameObject(null); Object.Destroy(typing);
            bar.HandleKeys(k => k == KeyCode.Return); Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible, Is.True);
            Object.FindFirstObjectByType<InventoryView>().ShowBag(false);
            yield return InventorySceneSmokeTests.Click("ClearAllShortcuts");
            Assert.That(bar.Bindings.Capture(), Is.Empty);
            bar.HandleKeys(_ => true); Assert.That(Object.FindFirstObjectByType<InventoryView>().BagVisible, Is.False);
            Assert.That(bar.ActionPressed(QuickslotAction.Left, _ => true), Is.False);
            var save = game.GetComponent<LocalProgressController>(); save.SaveFilePath = savePath;
            Assert.That(save.TrySave(out var error), Is.True, error);
            bar.ResetKeyboard(); Assert.That(save.TryLoad(out error), Is.True, error);
            Assert.That(bar.Bindings.Capture(), Is.Empty);
            if (GameObject.Find("QuickslotSettings") == null) bar.ShowShortcuts();
            yield return InventorySceneSmokeTests.Click("DefaultShortcuts");
            Assert.That(bar.ActionPressed(QuickslotAction.Left, k => k == KeyCode.A), Is.True);
            Assert.That(bar.BindingAtKey(KeyboardKey.Return).Kind, Is.EqualTo(QuickslotKind.Empty));
            Assert.That(bar.BindingAtKey(KeyboardKey.I).Id, Is.EqualTo((int)QuickslotAction.Inventory));
            Assert.That(bar.Bindings.IsNumberAlias(KeyboardKey.Alpha4), Is.True);
            Assert.That(bar.BindingAt(3).Id, Is.EqualTo(2000000));
        }
    }
}
