using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class StanceAnimationSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
        }
        private static void Step(GameWorld w, int ticks = 1) { for (int i = 0; i < ticks; i++) { w.ProcessInput(); w.UpdatePhysics(.008f); } }

        [UnityTest]
        public IEnumerator EquippedIdleAndWalkingPosesFollowSimulationAndKeepTheirPhaseAcrossAppearanceRefresh()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single); yield return null; yield return null;
            var game = Object.FindFirstObjectByType<GameManager>(); game.enabled = false;
            var w = game.World; var p = w.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(w, input);
            Step(w, 100); Assert.That(w.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] { 1040002, 1060002, 1072001 }) Assert.That(w.UseInventoryItem(id, out _), Is.True);
            // The original long ground is split into linked short footholds.
            var spans = new List<Tuple<float, float, float>>();
            foreach (var row in w.CurrentMap.Platforms.Where(f => f.Y1 == f.Y2 && f.X2 > f.X1).GroupBy(f => f.Y1))
            {
                float start = 0, end = 0; bool hasSpan = false;
                foreach (var f in row.OrderBy(f => f.X1))
                {
                    if (hasSpan && f.X1 > end) { spans.Add(Tuple.Create(start, end, row.Key)); hasSpan = false; }
                    if (!hasSpan) { start = f.X1; end = f.X2; hasSpan = true; }
                    else end = Math.Max(end, f.X2);
                }
                if (hasSpan) spans.Add(Tuple.Create(start, end, row.Key));
            }
            var platform = spans.OrderByDescending(f => f.Item2 - f.Item1).First();
            Assert.That(platform.Item2 - platform.Item1, Is.GreaterThan(450));
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            var body = visual.Find("Body").GetComponent<SpriteRenderer>();
            var shirt = visual.Find("Equipment_Top_mail").GetComponent<SpriteRenderer>();
            var renderer = GameObject.Find("Player").GetComponent<MapleCharacterRenderer>();
            foreach (int weapon in new[] { 1302000, 1442079 })
            {
                input.IsRightPressed = false; p.ResetMovementForMap();
                p.Position = new MapleClient.GameLogic.Vector2((platform.Item1 + 100) / 100f, -platform.Item3 / 100f + Player.Height / 2);
                p.Velocity = MapleClient.GameLogic.Vector2.Zero; p.IsGrounded = true; Step(w, 2);
                Assert.That(w.UseInventoryItem(weapon, out _), Is.True);
                var stance = p.CurrentWeapon.Stand; var frames = new HashSet<int>();
                for (int i = 0; i < 320 && frames.Count < 3; i++)
                {
                    Step(w); yield return null;
                    int frame = p.StanceAnimation.Sample(w.GetPhysicsInterpolationFactor()); frames.Add(frame);
                    Assert.That(body.sprite.name, Does.Contain($"/{CharacterStances.Name(stance)}/{frame}/"));
                    Assert.That(shirt.sprite.name, Does.Contain($"/{CharacterStances.Name(stance)}/{frame}/"));
                }
                Assert.That(frames.Count, Is.EqualTo(3));
                var sprite = body.sprite; int elapsed = p.StanceAnimation.ElapsedMilliseconds;
                yield return new WaitForSeconds(.35f);
                Assert.That(body.sprite, Is.SameAs(sprite)); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(elapsed));
                renderer.UpdateAppearance(); yield return null;
                Assert.That(body.sprite, Is.SameAs(sprite), "Refreshing clothes in the same stance retains the source phase.");
                InventorySceneSmokeTests.Capture(p, "-stance-" + CharacterStances.Name(stance));

                input.IsRightPressed = true; frames.Clear(); stance = p.CurrentWeapon.Walk;
                for (int i = 0; i < 250 && frames.Count < 4; i++)
                {
                    Step(w); yield return null;
                    Assert.That(p.State, Is.EqualTo(PlayerState.Walking));
                    int frame = p.StanceAnimation.Sample(w.GetPhysicsInterpolationFactor()); frames.Add(frame);
                    Assert.That(body.sprite.name, Does.Contain($"/{CharacterStances.Name(stance)}/{frame}/"));
                    Assert.That(shirt.sprite.name, Does.Contain($"/{CharacterStances.Name(stance)}/{frame}/"));
                }
                Assert.That(frames.Count, Is.EqualTo(4));
                sprite = body.sprite; var position = p.Position;
                yield return new WaitForSeconds(.35f);
                Assert.That(body.sprite, Is.SameAs(sprite)); Assert.That(p.Position, Is.EqualTo(position));
                InventorySceneSmokeTests.Capture(p, "-stance-" + CharacterStances.Name(stance));
                if (weapon == 1302000)
                {
                    // Both weapons use walk1; changing the grip must not restart its phase.
                    elapsed = p.StanceAnimation.ElapsedMilliseconds;
                    Assert.That(w.UseInventoryItem(1402009, out _), Is.True); yield return null;
                    Assert.That(body.sprite, Is.SameAs(sprite)); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(elapsed));
                    Assert.That(w.UseInventoryItem(1302000, out _), Is.True); yield return null;
                    Assert.That(body.sprite, Is.SameAs(sprite));
                }
                input.IsRightPressed = false; Step(w); yield return null;
                Assert.That(p.StanceAnimation.Frame, Is.Zero); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
                Assert.That(body.sprite.name, Does.Contain($"/{CharacterStances.Name(p.CurrentWeapon.Stand)}/0/"));
            }
            w.LoadMap(100000001); yield return null; yield return null;
            Assert.That(p.StanceAnimation.Frame, Is.Zero); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
            Assert.That(p.GetEquippedItems().Values, Does.Contain(1442079));
        }
    }
}
