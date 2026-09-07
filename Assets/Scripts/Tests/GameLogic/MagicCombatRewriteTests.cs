using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Data;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class MagicCombatRewriteTests
    {
        private sealed class Rolls : Random
        {
            private readonly double[] values; public int Used { get; private set; }
            public Rolls(params double[] values) { this.values = values; }
            public override double NextDouble() => values[Used++];
        }
        private static double[][] Rows(string name) => File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),
            "Assets/Scripts/Tests/GameLogic/Fixtures/" + name + ".csv")).Skip(1)
            .Select(s => s.Split(',').Select(n => double.Parse(n, CultureInfo.InvariantCulture)).ToArray()).ToArray();
        [TestCase(130)] [TestCase(137)] [TestCase(138)]
        public void SourceMagicBoundsDefenseAndRollsMatchCompiledCpp(int weapon)
        {
            var rows = Rows("HeavenMagicStats").Where(r => r[0] == weapon).ToArray();
            Assert.That(rows.Length, Is.EqualTo(60));
            foreach (var r in rows)
            {
                var raw = PhysicalAttackStats.Calculate(weapon * 10000, 200, 15, 15, (int)r[1], (int)r[2], (int)r[3], 0, (int)r[6], skillAttack: true);
                var stats = new PhysicalAttackStats(raw.Minimum, raw.Maximum, raw.Accuracy, raw.Level, raw.CriticalChance, true);
                var target = new MonsterTemplate { Level = (int)r[7], Avoidability = (int)r[8], PhysicalDefense = (int)r[9], MagicDefense = (int)r[10] };
                Assert.That(stats.Minimum, Is.EqualTo(r[14])); Assert.That(stats.Maximum, Is.EqualTo(r[15]));
                Assert.That(stats.Accuracy, Is.EqualTo(r[16])); Assert.That(stats.HitChance(target), Is.EqualTo(r[17]).Within(.000001));
                Assert.That(stats.MinimumAgainst(target), Is.EqualTo(r[18]).Within(.000001));
                Assert.That(stats.MaximumAgainst(target), Is.EqualTo(r[19]).Within(.000001));
                var rolls = new Rolls(r[11], r[12], r[13]); var hit = stats.Roll(target, rolls);
                Assert.That(hit.Damage, Is.EqualTo(r[20])); Assert.That(hit.Critical, Is.EqualTo(r[21] == 1)); Assert.That(rolls.Used, Is.EqualTo(r[22]));
            }
        }
        public static int[] Scenarios => Enumerable.Range(0, 24).ToArray();
        [TestCaseSource(nameof(Scenarios))]
        public void ProjectileMovementAndArrivalMatchOriginalCpp(int scenario)
        {
            var rows = Rows("HeavenProjectiles").Where(r => r[0] == scenario).ToArray();
            Assert.That(rows.Length, Is.GreaterThan(0)); var first = rows[0];
            var bullet = new SkillProjectile((int)first[2], (int)first[3], first[4] == 1, (int)first[5], (int)first[6],
                new SkillEffectDefinition { Frames = new[] { new AfterimageFrame { Path = "a", DelayMilliseconds = 80 }, new AfterimageFrame { Path = "b", DelayMilliseconds = 80 } } });
            foreach (var r in rows)
            {
                if (r[1] > 0) bullet.Tick((int)r[7]);
                Assert.That(bullet.SourceX, Is.EqualTo(r[8]).Within(1e-9), "tick " + r[1]);
                Assert.That(bullet.SourceY, Is.EqualTo(r[9]).Within(1e-9));
                Assert.That(bullet.HasArrived, Is.EqualTo(r[10] == 1)); Assert.That(bullet.FacingRight, Is.EqualTo(r[11] == 1));
                Assert.That(bullet.Sample(out _), Is.Not.Null, "Projectile artwork loops until arrival.");
            }
            Assert.That(bullet.HasArrived, Is.True);
        }
    }
}
