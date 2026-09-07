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
    public class FaceAnimationTests
    {
        internal static FaceAnimationData Data(int profile = 0)
        {
            var frames = new Dictionary<CharacterExpression, int[]>();
            foreach (var e in CharacterExpressions.SourceOrder)
                frames[e] = profile == 1 ? new[] { 1, 3, 5 } : profile == 2 ? new[] { 65535 } :
                    profile == 3 ? new[] { 100 } : e == CharacterExpression.Default ? new[] { 2500 } :
                    e == CharacterExpression.Blink ? new[] { 100, 120, 100 } : new[] { 100, 250, 170 };
            if (profile == 4) frames[CharacterExpression.Blink] = new int[0];
            return new FaceAnimationData(20000, frames);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)]
        public void MatchesCompiledSourceAtEveryFrameExpressionAndCooldownBoundary(int id)
        {
            var clock = new SourceFaceAnimation(); int tick = -1, profile = -1, count = 0;
            foreach (string row in File.ReadLines(Path.Combine(Application.dataPath,
                "Scripts/Tests/GameLogic/Fixtures/HeavenFaceAnimation.csv")).Skip(1))
            {
                var p = row.Split(','); if (int.Parse(p[0]) != id) continue;
                if (tick != int.Parse(p[1]))
                {
                    tick = int.Parse(p[1]);
                    if (profile != int.Parse(p[4])) { profile = int.Parse(p[4]); clock.SetData(p[5] == "0" ? null : Data(profile)); }
                    int command = int.Parse(p[3]); if (command >= 0) clock.TrySetExpression(CharacterExpressions.SourceOrder[command]);
                    clock.Advance(int.Parse(p[2]));
                }
                float alpha = float.Parse(p[6], CultureInfo.InvariantCulture); string at = $"case {id}, tick {tick}, alpha {alpha}";
                Assert.That(clock.SampleExpression(alpha), Is.EqualTo(CharacterExpressions.SourceOrder[int.Parse(p[7])]), at);
                Assert.That(clock.SampleFrame(alpha), Is.EqualTo(int.Parse(p[8])), at);
                Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(int.Parse(p[9])), at);
                Assert.That(clock.CooldownMilliseconds > 0, Is.EqualTo(p[10] == "1"), at); count++;
            }
            Assert.That(count, Is.EqualTo(5000));
        }

        [Test] public void ManualExpressionReturnsToBlinkCycleBeforeCooldownExpires()
        {
            var clock = new SourceFaceAnimation(); clock.SetData(Data());
            Assert.That(clock.TrySetExpression(CharacterExpression.Smile), Is.True);
            for (int i = 0; i < 65; i++) clock.Advance(8);
            Assert.That(clock.Expression, Is.EqualTo(CharacterExpression.Default));
            Assert.That(clock.CooldownMilliseconds, Is.EqualTo(4480));
            Assert.That(clock.TrySetExpression(CharacterExpression.Hit), Is.False);
            for (int i = 0; i < 100; i++) clock.Advance(0);
            Assert.That(clock.CooldownMilliseconds, Is.EqualTo(4480));
            for (int i = 0; i < 560; i++) clock.Advance(12);
            Assert.That(clock.CooldownMilliseconds, Is.Zero);
            Assert.That(clock.TrySetExpression(CharacterExpression.Cry), Is.True);
        }

        [Test] public void DefaultToBlinkUsesTheExactRemainingDelayThreshold()
        {
            var clock = new SourceFaceAnimation(); clock.SetData(Data());
            for (int i = 0; i < 312; i++) clock.Advance(8);
            Assert.That(clock.Expression, Is.EqualTo(CharacterExpression.Default)); clock.Advance(8);
            Assert.That(clock.SampleExpression(.499f), Is.EqualTo(CharacterExpression.Default));
            Assert.That(clock.SampleExpression(.5f), Is.EqualTo(CharacterExpression.Blink));
            Assert.That(clock.ElapsedMilliseconds, Is.EqualTo(4));
            clock.Advance(0); Assert.That(clock.SampleExpression(0), Is.EqualTo(CharacterExpression.Blink));
        }

        [TestCase(1,CharacterExpression.Hit)][TestCase(2,CharacterExpression.Smile)]
        [TestCase(3,CharacterExpression.Troubled)][TestCase(4,CharacterExpression.Cry)]
        [TestCase(5,CharacterExpression.Angry)][TestCase(6,CharacterExpression.Bewildered)][TestCase(7,CharacterExpression.Stunned)]
        public void FunctionKeysSelectTheSevenSourceFaceActions(int number, CharacterExpression expected)
        {
            Assert.That(CharacterExpressions.ForFunctionKey(number), Is.EqualTo(expected));
        }
    }
}
