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
    public class QuickslotProgressIntegrationTests
    {
        private sealed class Maps : IMapLoader
        {
            public MapData GetMap(int id) => id == 42 ? new MapData { MapId = 42,
                Platforms = new List<Platform> { new Platform { Id = 1, X1 = -3000, X2 = 3000, Y1 = 0, Y2 = 0 } },
                Portals = new List<Portal> { new Portal { Id = 0, Name = "sp", Type = MapleClient.GameLogic.PortalType.Spawn } } } : null;
        }
        private GameWorld world;
        [SetUp] public void Setup()
        {
            world = new GameWorld(null, new Maps(), assetProvider: global::GameData.NXDataManagerSingleton.Instance.DataManager);
            world.LoadMap(42); world.SkillManager.SetSkillLevel(2001004, 1);
        }
        private LocalProgress Saved()
        {
            var saved = world.CaptureProgress(); saved.Version = 4; saved.Hotbar = new[] {2001004};
            saved.Quickslots = new[] { new QuickslotBinding(QuickslotKind.Skill, 2001004),
                new QuickslotBinding(QuickslotKind.Item, 2000000), new QuickslotBinding(QuickslotKind.Action, 52), new QuickslotBinding() };
            return saved;
        }
        [Test] public void MixedBindingsRoundTripEvenWhenItemsAreExhaustedAndSkillsAreFromAnotherJob()
        {
            var saved = Saved(); var path = Path.Combine(Application.temporaryCachePath, "shortcuts-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new LocalSaveStore(path); Assert.That(store.TryWrite(saved, out var error), Is.True, error);
                Assert.That(store.TryRead(out var loaded, out error), Is.True, error);
                Assert.That(loaded.Version, Is.EqualTo(4)); Assert.That(loaded.Quickslots.Select(b => b.Kind), Is.EqualTo(saved.Quickslots.Select(b => b.Kind)));
                Assert.That(loaded.Quickslots.Select(b => b.Id), Is.EqualTo(saved.Quickslots.Select(b => b.Id)));
                Assert.That(world.TryRestoreProgress(loaded, out error), Is.True, error);
                Assert.That(world.Player.Inventory.GetItemCount(2000000), Is.Zero);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
        [TestCase("kind")][TestCase("null")][TestCase("array")][TestCase("size")][TestCase("item")][TestCase("gear")]
        [TestCase("skill")][TestCase("passive")][TestCase("action")][TestCase("empty")][TestCase("quest")]
        public void InvalidBindingsOrQuestDataRejectTheWholeSaveBeforeChangingThePlayer(string fault)
        {
            var saved = Saved(); saved.Player.Mesos = 321;
            if (fault == "kind") saved.Quickslots[0].Kind = (QuickslotKind)99;
            if (fault == "null") saved.Quickslots[0] = null;
            if (fault == "array") saved.Quickslots = null;
            if (fault == "size") saved.Quickslots = Enumerable.Range(0, 9).Select(i => new QuickslotBinding()).ToArray();
            if (fault == "item") saved.Quickslots[1].Id = 2060000;
            if (fault == "gear") saved.Quickslots[1].Id = 1302000;
            if (fault == "skill") saved.Quickslots[0].Id = 2001005;
            if (fault == "passive") { world.SkillManager.SetSkillLevel(1100000, 1); saved.Skills = world.CaptureProgress().Skills; saved.Quickslots[0].Id = 1100000; }
            if (fault == "action") saved.Quickslots[2].Id = 999;
            if (fault == "empty") saved.Quickslots[3].Id = 1;
            if (fault == "quest") saved.Quests = null;
            int mesos = world.Player.Mesos;
            Assert.That(world.TryRestoreProgress(saved, out _), Is.False);
            Assert.That(world.Player.Mesos, Is.EqualTo(mesos)); Assert.That(world.CurrentMapId, Is.EqualTo(42));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)] public void OldSkillOnlySavesStillLoad(int version)
        {
            var saved = Saved(); saved.Version = version; saved.Quickslots = null;
            Assert.That(world.TryRestoreProgress(saved, out var error), Is.True, error);
            Assert.That(world.SkillManager.GetSkillLevel(2001004), Is.EqualTo(1));
        }
    }
}

