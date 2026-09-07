using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class MonsterGroundTraceTests
    {
        [TestCase("walk_right")] [TestCase("walk_left")] [TestCase("slow")] [TestCase("fast")]
        [TestCase("edge")] [TestCase("slope_down")] [TestCase("slope_up")] [TestCase("wall")]
        [TestCase("hit_right")] [TestCase("hit_left")]
        public void RuntimeGroundMonsterMatchesCompiledHeavenClient(string name)
        {
            bool slope = name == "slope_up" || name == "slope_down";
            bool shortFloor = slope || name == "wall" || name == "edge";
            var floors = new List<Foothold> {
                new Foothold(1, -200, 0, shortFloor ? 100 : 300, 0) { Layer = 2, NextId = slope || name == "wall" ? 2 : 0 }
            };
            if (slope) floors.Add(new Foothold(2, 100, 0, 300, name == "slope_up" ? -100 : 100) { Layer = 4, PreviousId = 1 });
            if (name == "wall") floors.Add(new Foothold(2, 100, -100, 100, 0) { Layer = 4, PreviousId = 1, IsWall = true });
            floors.Add(new Foothold(3, -200, 400, 500, 400) { Layer = 6 });
            var monster = new Monster(new MonsterTemplate {
                MonsterId = 100101, MaxHP = 10000, CanMove = true, KnockbackThreshold = 1,
                Speed = name == "slow" ? -50 : name == "fast" ? 5 : -20
            }, new Vector2(shortFloor ? .9f : 0, .01f));
            monster.ConfigureTerrain(new NormalTerrain(floors), name != "walk_left");
            monster.SetMovementPattern(MovementPattern.Patrol);
            var rows = File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),
                "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenMonsters.csv"))
                .Where(l => l.StartsWith(name + ",")).ToArray();
            Assert.That(rows.Length, Is.EqualTo(500));
            foreach (var line in rows)
            {
                var r = line.Split(',').Skip(1).Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                if (r[0] == 50 && name.StartsWith("hit_")) monster.TakeDamage(2, name == "hit_left");
                monster.UpdatePhysics(.008f, null);
                string tick = "tick " + r[0];
                Assert.That(monster.Position.X * 100, Is.EqualTo(r[1]).Within(.002), "X " + tick);
                Assert.That(-monster.Position.Y * 100, Is.EqualTo(r[2]).Within(.002), "Y " + tick);
                Assert.That(NormalMovement.ToTickSpeed(monster.Velocity.X), Is.EqualTo(r[3]).Within(.00001), "VX " + tick);
                Assert.That(-NormalMovement.ToTickSpeed(monster.Velocity.Y), Is.EqualTo(r[4]).Within(.00001), "VY " + tick);
                Assert.That(monster.IsGrounded, Is.EqualTo(r[5] == 1), tick);
                Assert.That(monster.CurrentFootholdId, Is.EqualTo(r[6]), tick);
                Assert.That(monster.CurrentFootholdLayer, Is.EqualTo(r[7]), tick);
                Assert.That(monster.FacingRight, Is.EqualTo(r[8] == 1), tick);
                Assert.That(monster.IsHit, Is.EqualTo(r[9] == 1), tick);
            }
        }
    }
}
