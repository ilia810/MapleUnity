using System.Collections;
using System.Collections.Generic;
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
    public class AttackMovementSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) { if (type != LogType.Log) diagnostics.Add(type + ": " + message); }
        [TearDown] public void CheckLogs() { Application.logMessageReceived -= OnLog; Assert.That(diagnostics, Is.Empty); }
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
        }
        private static void Step(GameWorld world, int count = 1) { for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); } }

        [UnityTest]
        public IEnumerator WalkingAndProneSwingsLockControlsAndReturnToHeldMovement()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var player = world.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            Step(world, 100); Assert.That(world.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000 }) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            var body = visual.Find("Body").GetComponent<SpriteRenderer>();
            var trail = visual.Find("WeaponAfterimage").GetComponent<SpriteRenderer>();
            input.IsRightPressed = true; Step(world, 20);
            Assert.That(player.Velocity.X, Is.GreaterThan(0));
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            var swing = player.BasicAttack; float startX = player.Position.X; float speed = player.Velocity.X;
            input.IsRightPressed = false; input.IsLeftPressed = true; input.IsJumpPressed = true;
            Step(world, 12);
            Assert.That(player.Position.X, Is.GreaterThan(startX), "Ground momentum still decelerates during the swing.");
            Assert.That(player.Velocity.X, Is.InRange(0, speed)); Assert.That(player.FacingRight, Is.True);
            Assert.That(player.IsGrounded, Is.True); Assert.That(player.State, Is.EqualTo(PlayerState.Walking));
            for (int i = 0; i < 80 && swing.Frame < swing.Afterimage.FirstFrame; i++) Step(world);
            yield return null; yield return null;
            Assert.That(body.sprite.name, Does.Contain("/" + CharacterStances.Name(swing.Stance) + "/"));
            Assert.That(trail.sprite, Is.Not.Null); Assert.That(visual.localScale.x, Is.LessThan(0));
            InventorySceneSmokeTests.Capture(player, "-attack-movement-lock");
            for (int i = 0; i < 140 && player.IsBasicAttacking; i++) Step(world);
            Assert.That(player.IsBasicAttacking, Is.False); Assert.That(player.FacingRight, Is.False);
            Assert.That(player.State, Is.EqualTo(PlayerState.Walking)); Step(world, 8);
            Assert.That(player.Velocity.X, Is.LessThan(0)); Assert.That(player.IsGrounded, Is.True);
            yield return null; yield return null;
            Assert.That(body.sprite.name, Does.Contain("/walk1/")); Assert.That(trail.sprite, Is.Null);
            InventorySceneSmokeTests.Capture(player, "-attack-movement-resumed");

            input.IsLeftPressed = false; input.IsJumpPressed = false; input.IsDownPressed = true; Step(world, 30);
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching));
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            Assert.That(player.BasicAttack.Stance, Is.EqualTo(CharacterState.ProneStab));
            input.IsDownPressed = false; input.IsRightPressed = true; Step(world, 34);
            Assert.That(player.State, Is.EqualTo(PlayerState.Crouching)); Assert.That(player.FacingRight, Is.False);
            yield return null; yield return null;
            Assert.That(body.sprite.name, Does.Contain("/proneStab/"));
            InventorySceneSmokeTests.Capture(player, "-prone-attack-lock");
            for (int i = 0; i < 140 && player.IsBasicAttacking; i++) Step(world);
            Assert.That(player.IsBasicAttacking, Is.False); Assert.That(player.State, Is.EqualTo(PlayerState.Walking));
            Assert.That(player.FacingRight, Is.True); Step(world, 4);
            yield return null; yield return null;
            Assert.That(body.sprite.name, Does.Contain("/walk1/")); Assert.That(player.Velocity.X, Is.GreaterThan(0));
        }
    }
}
