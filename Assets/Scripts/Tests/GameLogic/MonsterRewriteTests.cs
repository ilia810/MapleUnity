using System;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class MonsterRewriteTests
    {
        private static GameWorld CreateWorld(FakeInputProvider input, float interval = -1)
        {
            var map = new MapData { MapId = 1 };
            map.Platforms.Add(new Platform { Id = 1, X1 = -200, X2 = 200, Y1 = 0, Y2 = 0, Layer = 2 });
            map.Portals.Add(new Portal { Id = 0, Type = PortalType.Spawn, X = 0, Y = 0 });
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 100101, X = 60, Y = -10, SpawnInterval = interval });
            map.MonsterSpawns.Add(new MonsterSpawn { MonsterId = 100101, X = -60, Y = -10, SpawnInterval = -1 });
            var loader = new FakeMapLoader(); loader.AddMap(1, map);
            var second = new MapData { MapId = 2, Platforms = map.Platforms, Portals = map.Portals };
            loader.AddMap(2, second);
            var world = new GameWorld(input, loader); world.LoadMap(1);
            world.Player.SetBaseDamage(1);
            for (int i = 0; i < 10; i++) world.UpdatePhysics(.008f);
            return world;
        }

        [Test]
        public void AttacksWaitForPhysicsAndOnlyDamageMonstersInFront()
        {
            var input = new FakeInputProvider(); var world = CreateWorld(input);
            var right = world.Monsters[0]; var left = world.Monsters[1];
            input.IsAttackPressed = true;
            for (int i = 0; i < 100; i++) world.ProcessInput();
            Assert.That(right.HP, Is.EqualTo(100));
            world.UpdatePhysics(.008f);
            Assert.That(right.HP, Is.EqualTo(99)); Assert.That(left.HP, Is.EqualTo(100));
            for (int i = 0; i < 10; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
            Assert.That(right.HP, Is.EqualTo(99), "Holding attack respects the cooldown.");
            input.IsAttackPressed = false; input.IsLeftPressed = true;
            world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(world.Player.FacingRight ?? true, Is.True, "A tap during a swing cannot turn the player.");
            input.IsLeftPressed = false;
            for (int i = 0; i < 80; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
            input.IsLeftPressed = true; world.ProcessInput(); world.UpdatePhysics(.008f);
            input.IsLeftPressed = false;
            input.IsAttackPressed = true; world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(left.HP, Is.EqualTo(99));
        }

        [TestCase(.004f)] [TestCase(.008f)] [TestCase(.016f)]
        public void HeldAttackCountUsesSimulationTimeAcrossHostRates(float hostDelta)
        {
            var input = new FakeInputProvider(); var world = CreateWorld(input);
            int hits = 0; world.Monsters[0].DamageTaken += (_, damage) => hits++;
            input.IsAttackPressed = true;
            int frames = (int)Math.Round(1.6 / hostDelta);
            for (int i = 0; i < frames; i++) { world.ProcessInput(); world.UpdatePhysics(hostDelta); }
            Assert.That(hits, Is.EqualTo(3));
        }

        [Test]
        public void RegularAttackHitsOneClosestMonsterRegardlessOfSpawnOrder()
        {
            var input = new FakeInputProvider(); var world = CreateWorld(input);
            var farther = world.Monsters[0];
            world.SpawnMonsterForTesting(100101, new Vector2(.3f, 0));
            var nearest = world.Monsters.Last();
            for (int i = 0; i < 10; i++) world.UpdatePhysics(.008f);
            input.IsAttackPressed = true; world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(nearest.HP, Is.EqualTo(99));
            Assert.That(farther.HP, Is.EqualTo(100));
        }

        [TestCase(0f, 7f)] [TestCase(.04f, .04f)]
        public void OfflineRespawnReplacesOnlyTheDeadSpawnAfterItsDelay(float authored, float delay)
        {
            var world = CreateWorld(new FakeInputProvider(), authored);
            var original = world.Monsters[0]; var survivor = world.Monsters[1];
            original.TakeDamage(1000);
            int before = (int)Math.Floor(delay / .008) - 1;
            for (int i = 0; i < before; i++) world.UpdatePhysics(.008f);
            Assert.That(world.Monsters.Count, Is.EqualTo(1));
            Assert.That(world.Monsters[0], Is.SameAs(survivor));
            for (int i = 0; i < 3; i++) world.UpdatePhysics(.008f);
            Assert.That(world.Monsters.Count, Is.EqualTo(2));
            var replacement = world.Monsters.Single(m => m != survivor);
            Assert.That(replacement.Id, Is.Not.EqualTo(original.Id));
            Assert.That(replacement.HP, Is.EqualTo(100));
        }

        [Test]
        public void LeavingMapCancelsPendingRespawns()
        {
            var world = CreateWorld(new FakeInputProvider(), 0);
            world.Monsters[0].TakeDamage(1000);
            world.LoadMap(2);
            for (int i = 0; i < 1000; i++) world.UpdatePhysics(.008f);
            Assert.That(world.Monsters, Is.Empty);
        }

        [Test]
        public void GroundMonsterSettlesOnSourceFootholdInsteadOfAuthoredSpawnHeight()
        {
            var world = CreateWorld(new FakeInputProvider());
            foreach (var monster in world.Monsters)
            {
                Assert.That(monster.Position.Y, Is.EqualTo(0));
                Assert.That(monster.CurrentFootholdLayer, Is.EqualTo(2));
                Assert.That(monster.CurrentFootholdId, Is.EqualTo(1));
                Assert.That(monster.IsGrounded, Is.True);
            }
        }
    }
}
