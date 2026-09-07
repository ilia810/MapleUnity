using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;

namespace MapleClient.GameLogic.Tests
{
    public class WorldRecoveryTests
    {
        private static MapData Map(int id, float ground = 200)
        {
            var map = new MapData { MapId = id, Name = "Recovery test" };
            map.Platforms.Add(new Platform { Id = 1, X1 = -1000, X2 = 1000, Y1 = ground, Y2 = ground });
            map.Portals.Add(new Portal { Id = 0, Name = "sp", Type = PortalType.Spawn, X = 0, Y = (int)ground });
            return map;
        }

        [Test]
        public void ReplacingMap_ReplacesTerrainIncludingEmptyMap()
        {
            var loader = new FakeMapLoader();
            loader.AddMap(1, Map(1, 25));
            loader.AddMap(2, Map(2, -75));
            loader.AddMap(3, new MapData { MapId = 3 });
            var terrain = new FootholdService();
            var world = new GameWorld(new FakeInputProvider(), loader, footholdService: terrain);
            world.LoadMap(1);
            Assert.That(terrain.GetGroundBelow(0, -100), Is.EqualTo(24));
            world.LoadMap(2);
            Assert.That(terrain.GetGroundBelow(0, -100), Is.EqualTo(-76));
            world.LoadMap(3);
            Assert.That(terrain.GetFootholdsInArea(-1000, -1000, 1000, 1000), Is.Empty);
        }

        [Test]
        public void MapLoaded_IsRaisedBeforeNewMonsterViews()
        {
            var map = Map(1);
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 100100, X = 300, Y = 200 });
            var loader = new FakeMapLoader();
            loader.AddMap(1, map);
            var world = new GameWorld(new FakeInputProvider(), loader);
            var events = new List<string>();
            world.MapLoaded += _ => events.Add("map");
            world.MonsterSpawned += _ => events.Add("monster");
            world.LoadMap(1);
            CollectionAssert.AreEqual(new[] { "map", "monster" }, events);
            Assert.That(world.Monsters[0].Position.X, Is.EqualTo(3));
            Assert.That(world.Monsters[0].Position.Y, Is.EqualTo(-2));
        }

        [Test]
        public void DeathAndRepeatedMapChanges_ReleaseMonsterPhysicsRegistrations()
        {
            var map = Map(1);
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 100100 });
            var loader = new FakeMapLoader();
            loader.AddMap(1, map);
            loader.AddMap(2, Map(2));
            var world = new GameWorld(new FakeInputProvider(), loader);
            for (int i = 0; i < 5; i++)
            {
                world.LoadMap(1);
                Assert.That(world.GetPhysicsDebugStats().TotalObjectCount, Is.EqualTo(2));
                world.Monsters[0].TakeDamage(10000);
                Assert.That(world.GetPhysicsDebugStats().TotalObjectCount, Is.EqualTo(1));
                world.LoadMap(2);
                Assert.That(world.GetPhysicsDebugStats().TotalObjectCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void RemovedMonsters_CannotEmitEventsIntoNewMap()
        {
            var map = Map(1);
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 100100 });
            var loader = new FakeMapLoader();
            loader.AddMap(1, map);
            loader.AddMap(2, Map(2));
            var world = new GameWorld(new FakeInputProvider(), loader);
            world.LoadMap(1);
            var oldMonster = world.Monsters[0];
            oldMonster.Template.DropTable = new List<DropInfo> {
                new DropInfo { ItemId = 2000000, Quantity = 1, DropRate = 1 }
            };
            world.LoadMap(2);
            oldMonster.TakeDamage(10000);
            Assert.That(world.DroppedItems, Is.Empty);
            Assert.That(world.GetPhysicsDebugStats().TotalObjectCount, Is.EqualTo(1));
        }

        [Test]
        public void InputPolling_DoesNotAdvanceItemTimers()
        {
            var world = new GameWorld(new FakeInputProvider(), new FakeMapLoader());
            world.AddDroppedItem(2000000, 1, new Vector2(100, 100));
            for (int i = 0; i < 120; i++) world.ProcessInput();
            Assert.That(world.DroppedItems[0].LifeTime, Is.EqualTo(180));
            for (int i = 0; i < 125; i++) world.UpdatePhysics(PhysicsUpdateManager.FIXED_TIMESTEP);
            Assert.That(world.DroppedItems[0].LifeTime, Is.EqualTo(179).Within(0.001f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void CombinedUpdate_AdvancesEquivalentSimulationTime(int updatesPerSecond)
        {
            var world = new GameWorld(new FakeInputProvider(), new FakeMapLoader());
            world.AddDroppedItem(2000000, 1, new Vector2(100, 100));
            for (int i = 0; i < updatesPerSecond; i++) world.Update(1f / updatesPerSecond);
            Assert.That(world.GetPhysicsDebugStats().TotalPhysicsSteps, Is.EqualTo(125));
            Assert.That(world.DroppedItems[0].LifeTime, Is.EqualTo(179).Within(0.001f));
        }

        [Test]
        public void PortalUsesPixelCoordinates_AndRequiresAnotherPressToReturn()
        {
            var first = Map(1);
            first.Portals.Add(new Portal { Id = 1, X = 200, Y = 200, Type = PortalType.Regular,
                TargetMapId = 2, TargetPortalName = "return" });
            var second = Map(2);
            second.Portals.Add(new Portal { Id = 7, Name = "return", X = -300, Y = 200,
                Type = PortalType.Regular, TargetMapId = 1 });
            var loader = new FakeMapLoader();
            loader.AddMap(1, first);
            loader.AddMap(2, second);
            var input = new FakeInputProvider();
            var world = new GameWorld(input, loader);
            world.LoadMap(1);
            world.Player.Position = new Vector2(2, -2);
            input.IsUpPressed = true;
            world.ProcessInput();
            Assert.That(world.CurrentMapId, Is.EqualTo(1), "Portal commands wait for a physics tick.");
            world.UpdatePhysics(0.008f);
            Assert.That(world.CurrentMapId, Is.EqualTo(2));
            Assert.That(world.Player.Position.X, Is.EqualTo(-3));
            for (int i = 0; i < 10; i++) world.ProcessInput();
            Assert.That(world.CurrentMapId, Is.EqualTo(2));
        }

        [Test]
        public void PickupRange_IsHalfAWorldUnit()
        {
            var world = new GameWorld(new FakeInputProvider(), new FakeMapLoader());
            world.AddDroppedItem(2000000, 1, new Vector2(0.1f, 0));
            world.AddDroppedItem(2000001, 1, new Vector2(2, 0));
            world.UpdatePhysics(PhysicsUpdateManager.FIXED_TIMESTEP);
            Assert.That(world.Player.Inventory.GetItemCount(2000000), Is.EqualTo(1));
            Assert.That(world.DroppedItems.Select(item => item.ItemId), Is.EquivalentTo(new[] { 2000001 }));
        }

        [Test]
        public void MonsterGroundHeight_DoesNotUseSpawnX()
        {
            var monster = new Monster(new MonsterTemplate { MaxHP = 10 }, new Vector2(4, -2));
            monster.UpdatePhysics(PhysicsUpdateManager.FIXED_TIMESTEP, Map(1));
            Assert.That(monster.Position.Y, Is.EqualTo(-2));
        }
    }
}