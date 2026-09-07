using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameLogic
{
    public class StanceAnimationTests
    {
        private static readonly CharacterState[] Stances = { CharacterState.Stand, CharacterState.Stand2,
            CharacterState.Walk, CharacterState.Walk2, CharacterState.Jump, CharacterState.Prone,
            CharacterState.Fly, CharacterState.Ladder, CharacterState.Rope };
        internal sealed class Data : IStanceDataProvider
        {
            private readonly bool shortFrames;
            public Data(bool shortFrames = false) { this.shortFrames = shortFrames; }
            public IReadOnlyList<int> GetStanceDelays(CharacterState stance)
            {
                if (shortFrames) return new[] { 1, 3, 5 };
                switch (stance)
                {
                    case CharacterState.Stand: case CharacterState.Stand2: return new[] { 100, 120, 180 };
                    case CharacterState.Walk: case CharacterState.Walk2: return new[] { 100, 100, 100, 100 };
                    case CharacterState.Jump: case CharacterState.Prone: return new[] { 100 };
                    default: return new[] { 100, 120 };
                }
            }
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)]
        public void MatchesCompiledSourceForSpeedsStopsTransitionsAndFrameThresholds(int id)
        {
            var clock = new SourceStanceAnimation(); clock.SetData(new Data(id == 7));
            int lastTick = -1, count = 0;
            foreach (string row in File.ReadLines(Path.Combine(Application.dataPath,
                "Scripts/Tests/GameLogic/Fixtures/HeavenStanceAnimation.csv")).Skip(1))
            {
                var p = row.Split(','); if (int.Parse(p[0]) != id) continue;
                int tick = int.Parse(p[1]);
                if (tick != lastTick)
                {
                    clock.SetStance(Stances[int.Parse(p[2])]);
                    clock.Advance(float.Parse(p[3], CultureInfo.InvariantCulture)); lastTick = tick;
                }
                string at = $"case {id}, tick {tick}, interpolation {p[4]}";
                Assert.That(clock.Sample(float.Parse(p[4], CultureInfo.InvariantCulture)), Is.EqualTo(int.Parse(p[5])), at);
                Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(int.Parse(p[6])), at); count++;
            }
            Assert.That(count, Is.EqualTo(3000));
        }

        [Test] public void StoppingNormalizesLastFrameButRetainsPartialDelayForResuming()
        {
            var clock = new SourceStanceAnimation(); clock.SetData(new Data()); clock.SetStance(CharacterState.Ladder);
            for (int i = 0; i < 13; i++) clock.Advance(1);
            Assert.That(clock.Sample(.49f), Is.Zero); Assert.That(clock.Sample(.5f), Is.EqualTo(1));
            clock.Advance(0);
            Assert.That(clock.Sample(0), Is.EqualTo(1)); Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(4));
            for (int i = 0; i < 60; i++) clock.Advance(0);
            clock.Advance(1.4f); Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(15));
        }

        [Test] public void SameSourceStanceAndProviderKeepPhaseWhileNewStanceRestarts()
        {
            var clock = new SourceStanceAnimation(); var data = new Data(); clock.SetData(data);
            for (int i = 0; i < 20; i++) clock.Advance(1);
            clock.SetData(data); clock.SetStance(CharacterState.Stand);
            Assert.That(clock.Frame, Is.EqualTo(1)); Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(60));
            clock.SetStance(CharacterState.Stand2);
            Assert.That(clock.Frame, Is.Zero); Assert.That(clock.ElapsedMilliseconds, Is.Zero);
            clock.SetStance(CharacterState.Jump); clock.Advance(1); clock.SetStance(CharacterState.Fall);
            Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(8));
        }
    }
}
