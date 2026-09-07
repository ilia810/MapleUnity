using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameData
{
    public class KeyboardProgressIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => id == 42 ? new MapData { MapId = 42,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } } : null;
        }
        private GameWorld world;
        private static QuickslotBinding Action(QuickslotAction action) => new QuickslotBinding(QuickslotKind.Action, (int)action);
        [SetUp] public void Setup()
        {
            world = new GameWorld(null, new Maps(), assetProvider: global::GameData.NXDataManagerSingleton.Instance.DataManager);
            world.LoadMap(42); world.SkillManager.SetSkillLevel(2001004, 1);
        }
        [Test] public void SwapsMoveCommandsAndResolveAliasesWithoutCyclesOrSharedMutableBindings()
        {
            var map = world.Keyboard;
            Assert.That(map.Swap(KeyboardKey.I, KeyboardKey.R, map.Revision), Is.True);
            Assert.That(map.Get(KeyboardKey.I).Kind, Is.EqualTo(QuickslotKind.Empty));
            Assert.That(map.IsPressed(QuickslotAction.Inventory, k => k == KeyboardKey.R), Is.True);
            Assert.That(map.IsPressed(QuickslotAction.Inventory, k => k == KeyboardKey.I), Is.False);
            Assert.That(map.Swap(KeyboardKey.R, KeyboardKey.A, map.Revision), Is.True);
            Assert.That(map.IsPressed(QuickslotAction.Left, k => k == KeyboardKey.R), Is.True);
            Assert.That(map.Get(KeyboardKey.A).Id, Is.EqualTo((int)QuickslotAction.Inventory));
            map.Set(KeyboardKey.PageUp, new QuickslotBinding(QuickslotKind.Item, 2000000));
            Assert.That(map.Pressed(k => k == KeyboardKey.PageUp || k == KeyboardKey.Alpha4).Count(), Is.EqualTo(1));
            Assert.That(map.Swap(KeyboardKey.Alpha4, KeyboardKey.PageUp, map.Revision), Is.True);
            Assert.That(map.IsNumberAlias(KeyboardKey.Alpha4), Is.False);
            map.Set(KeyboardKey.PageUp, Action(QuickslotAction.Jump));
            Assert.That(map.Get(KeyboardKey.Alpha4).Id, Is.EqualTo(2000000));
            var copy = map.Get(KeyboardKey.A); copy.Id = 999;
            Assert.That(map.Get(KeyboardKey.A).Id, Is.EqualTo((int)QuickslotAction.Inventory));
            int revision = map.Revision; map.Clear();
            Assert.That(map.Swap(KeyboardKey.A, KeyboardKey.B, revision), Is.False);
            Assert.That(map.Capture(), Is.Empty);
        }
        [Test] public void EveryPhysicalKeyAcceptsEveryCommandAndNeverExecutesTheOldAssignmentAfterMoving()
        {
            var map = world.Keyboard;
            foreach (QuickslotAction action in Enum.GetValues(typeof(QuickslotAction)))
            foreach (var key in KeyboardMap.Keys)
            {
                map.Clear(); Assert.That(map.Set(key, Action(action)), Is.True);
                Assert.That(map.IsPressed(action, candidate => candidate == key), Is.True);
                var destination = key == KeyboardKey.R ? KeyboardKey.T : KeyboardKey.R;
                Assert.That(map.Swap(key, destination, map.Revision), Is.True);
                Assert.That(map.IsPressed(action, candidate => candidate == key), Is.False);
                Assert.That(map.IsPressed(action, candidate => candidate == destination), Is.True);
            }
        }
        [TestCase(false)][TestCase(true)] public void EntireKeyboardIncludingAnIntentionallyEmptyMapRoundTripsThroughDisk(bool clear)
        {
            var map = world.Keyboard;
            map.Swap(KeyboardKey.Escape, KeyboardKey.F12, map.Revision);
            map.Set(KeyboardKey.R, new QuickslotBinding(QuickslotKind.Skill, 2001004));
            map.Set(KeyboardKey.T, new QuickslotBinding(QuickslotKind.Item, 2000000));
            map.Set(KeyboardKey.Alpha4, Action(QuickslotAction.Inventory));
            if (clear) map.Clear();
            var saved = world.CaptureProgress(); var path = Path.Combine(Application.temporaryCachePath, "keyboard-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new LocalSaveStore(path); Assert.That(store.TryWrite(saved, out var error), Is.True, error);
                Assert.That(store.TryRead(out var loaded, out error), Is.True, error);
                Assert.That(loaded.Version, Is.EqualTo(5)); map.ResetDefaults();
                Assert.That(world.TryRestoreProgress(loaded, out error), Is.True, error);
                Assert.That(JsonUtility.ToJson(world.CaptureProgress()).Contains("\"Keyboard\""), Is.True);
                Assert.That(map.Capture().Select(e => (e.Key, e.Binding.Kind, e.Binding.Id)), Is.EqualTo(saved.Keyboard.Select(e => (e.Key, e.Binding.Kind, e.Binding.Id))));
                if (!clear) { loaded.Keyboard[0].Binding.Id = 999; Assert.That(KeyboardMap.ValidEntries(map.Capture()), Is.True); }
                else Assert.That(map.Capture(), Is.Empty);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
        [TestCase("duplicate")][TestCase("key")][TestCase("none")][TestCase("null")][TestCase("binding")]
        [TestCase("array")][TestCase("kind")][TestCase("action")][TestCase("item")][TestCase("skill")]
        [TestCase("passive")][TestCase("reference")][TestCase("cycle")][TestCase("empty")]
        public void InvalidKeyboardRejectsTheWholeSaveWithoutChangingCharacterMapOrBindings(string fault)
        {
            var save = world.CaptureProgress(); save.Player.Mesos = 321;
            save.Keyboard = new[] { new KeyboardEntry(KeyboardKey.R, Action(QuickslotAction.Inventory)) };
            if (fault == "duplicate") save.Keyboard = new[] { save.Keyboard[0], save.Keyboard[0] };
            if (fault == "key") save.Keyboard[0].Key = (KeyboardKey)999;
            if (fault == "none") save.Keyboard[0].Key = KeyboardKey.None;
            if (fault == "null") save.Keyboard[0] = null;
            if (fault == "binding") save.Keyboard[0].Binding = null;
            if (fault == "array") save.Keyboard = null;
            if (fault == "kind") save.Keyboard[0].Binding.Kind = (QuickslotKind)999;
            if (fault == "action") save.Keyboard[0].Binding.Id = 999;
            if (fault == "item") save.Keyboard[0].Binding = new QuickslotBinding(QuickslotKind.Item, 1302000);
            if (fault == "skill") save.Keyboard[0].Binding = new QuickslotBinding(QuickslotKind.Skill, 2001005);
            if (fault == "passive") { world.SkillManager.SetSkillLevel(1100000, 1); save.Skills = world.CaptureProgress().Skills; save.Keyboard[0].Binding = new QuickslotBinding(QuickslotKind.Skill, 1100000); }
            if (fault == "reference") save.Keyboard[0] = new KeyboardEntry(KeyboardKey.Alpha4, new QuickslotBinding(QuickslotKind.KeyReference, (int)KeyboardKey.Home));
            if (fault == "cycle") save.Keyboard[0].Binding = new QuickslotBinding(QuickslotKind.KeyReference, (int)KeyboardKey.R);
            if (fault == "empty") save.Keyboard[0].Binding = new QuickslotBinding(QuickslotKind.Empty, 1);
            int mesos = world.Player.Mesos, revision = world.Keyboard.Revision;
            Assert.That(world.TryRestoreProgress(save, out _), Is.False);
            Assert.That(world.Player.Mesos, Is.EqualTo(mesos)); Assert.That(world.CurrentMapId, Is.EqualTo(42));
            Assert.That(world.Keyboard.Revision, Is.EqualTo(revision));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)] public void LegacySavesKeepTheirTrayAndReceiveTheRemainingDefaultCommands(int version)
        {
            var save = world.CaptureProgress(); save.Version = version; save.Keyboard = null;
            save.Hotbar = new[] { 2001004 };
            save.Quickslots = new[] { new QuickslotBinding(QuickslotKind.Skill, 2001004), new QuickslotBinding(QuickslotKind.Item, 2000000) };
            world.Keyboard.Clear(); Assert.That(world.TryRestoreProgress(save, out var error), Is.True, error);
            Assert.That(world.Keyboard.Get(KeyboardKey.LeftShift).Id, Is.EqualTo(2001004));
            Assert.That(world.Keyboard.Get(KeyboardKey.Alpha1).Id, Is.EqualTo(2001004));
            Assert.That(world.Keyboard.Get(KeyboardKey.I).Id, Is.EqualTo((int)QuickslotAction.Inventory));
            Assert.That(world.Keyboard.Get(KeyboardKey.LeftControl).Kind, Is.EqualTo(version < 4 ? QuickslotKind.Action : QuickslotKind.Empty));
            Assert.That(world.TryRestoreProgress(world.CaptureProgress(), out error), Is.True, error);
        }
    }
}

