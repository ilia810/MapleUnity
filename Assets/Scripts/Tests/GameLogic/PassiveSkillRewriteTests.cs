using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameLogic
{
    public class PassiveSkillRewriteTests
    {
        private static readonly int[] Ids = {12,1100000,1100001,1200000,1200001,1300000,1300001,1120004,1220005,1320005,1320006,1000000};
        [TestCaseSource(nameof(Ids))]
        public void PassiveRegistryAndJobConditionsMatchCompiledCpp(int id)
        {
            int rows = 0;
            foreach (var line in File.ReadLines(Path.Combine(Application.dataPath, "Scripts/Tests/GameLogic/Fixtures/HeavenPassiveStats.csv")).Skip(1))
            {
                var values = line.Split(','); if (int.Parse(values[0]) != id) continue;
                int job = int.Parse(values[1]), weapon = int.Parse(values[2]), hp = int.Parse(values[3]);
                bool canUse = SourceSkillRules.CanUse(job, id);
                Assert.That(canUse, Is.EqualTo(values[11] == "1"), line);
                var data = new SkillInfo.LevelData { X = id == 1320006 ? 50 : id == 1120004 || id == 1220005 || id == 1320005 ? 850 : 20,
                    Y = 40, Z = 20, Mastery = 10, Damage = 100 };
                var result = new PassiveStats(); if (canUse) SourceSkillRules.ApplyPassive(ref result, id, data, weapon, 101, hp);
                var actual = new double[] { result.WeaponAttack, result.MagicAttack, result.Accuracy, result.Avoidability, result.Mastery ?? 0, result.DamagePercent ?? 0, result.DamageReduction };
                for (int i = 0; i < actual.Length; i++) Assert.That(actual[i], Is.EqualTo(double.Parse(values[i + 4], CultureInfo.InvariantCulture)).Within(1e-7), line);
                rows++;
            }
            Assert.That(rows, Is.EqualTo(351));
        }
    }
}
