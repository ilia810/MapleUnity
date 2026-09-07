using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class MonsterSceneSmokeTests
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

        [UnityTest]
        public IEnumerator HuntingGroundRendersSourceMonstersAttacksDeathAndRespawn()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>();
            var world = (GameWorld)typeof(GameManager).GetField("gameWorld", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, input);
            manager.enabled = false;
            world.LoadMap(100010000);
            Step(world, 100);
            yield return null; yield return null;
            Assert.That(world.Monsters.Count, Is.EqualTo(49));
            var views = Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None);
            Assert.That(views.Length, Is.EqualTo(49));
            foreach (var view in views)
            {
                var monster = view.Model;
                Assert.That(view.BodyRenderer.sprite, Is.Not.Null, "Monster art " + monster.MonsterId);
                Assert.That(view.BodyRenderer.sprite.name, Does.StartWith("mob/"));
                Assert.That(monster.IsGrounded, Is.True, "Monster feet " + monster.Id);
                var group = view.GetComponent<SortingGroup>();
                Assert.That(group.sortingLayerName, Is.EqualTo("Objects"));
                Assert.That(group.sortingOrder, Is.EqualTo(monster.CurrentFootholdLayer * 1000 + 650));
            }
            var targetView = views.Where(v => v.Model.MonsterId == 100101)
                .Where(v => !views.Any(other => other != v &&
                    System.Math.Abs(other.Model.Position.Y - v.Model.Position.Y) < .5f &&
                    System.Math.Abs(other.Model.Position.X - v.Model.Position.X) < .8f))
                .OrderBy(v => System.Math.Abs(v.Model.Position.X - 5.3f)).First();
            var target = targetView.Model;
            var player = world.Player;
            player.ResetMovementForMap();
            player.Position = new LogicVector(target.Position.X - .4f, target.Position.Y + Player.Height / 2);
            player.Velocity = LogicVector.Zero; player.IsGrounded = true;
            input.IsRightPressed = true; Step(world, 1); input.IsRightPressed = false;
            Step(world, 5);
            yield return null; yield return null;
            Capture(target, "-monsters");
            var firstSprite = targetView.BodyRenderer.sprite;
            yield return new WaitForSeconds(.14f);
            Assert.That(targetView.BodyRenderer.sprite, Is.Not.SameAs(firstSprite), "Source move frames must animate.");

            player.SetBaseDamage(1);
            int health = target.HP;
            input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false;
            Assert.That(target.HP, Is.EqualTo(health - 1));
            yield return null; yield return null;
            Assert.That(targetView.CurrentAnimationName, Is.EqualTo("hit1"));
            Assert.That(Object.FindFirstObjectByType<MapleClient.GameView.UI.ClassicMonsterHealthView>().HasVisibleBar(targetView), Is.True);
            var playerBody = GameObject.Find("Player").transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
            Assert.That(playerBody.sprite.name, Does.Contain("/stabO1/"), "Basic attack must reach the assembled character renderer.");
            Capture(target, "-monster-hit");

            Step(world, 80);
            player.Position = new LogicVector(target.Position.X - .4f, target.Position.Y + Player.Height / 2);
            player.Velocity = LogicVector.Zero;
            player.SetBaseDamage(1000);
            input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false;
            Assert.That(target.IsDead, Is.True);
            yield return null; yield return null;
            Assert.That(targetView.CurrentAnimationName, Is.EqualTo("die1"));
            Capture(target, "-monster-death");
            yield return new WaitForSeconds(.8f);
            Assert.That(targetView == null, Is.True, "The die1 sequence finishes and releases its view.");
            Step(world, 900);
            yield return null; yield return null;
            Assert.That(world.Monsters.Count, Is.EqualTo(49), "Local spawns recover after the default seven-second interval.");
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Length, Is.EqualTo(49));
            Assert.That(world.Monsters.All(m => m.Id != target.Id), Is.True);

            // Leaving while a death sequence is still visible must remove both
            // living and dying art and cancel every old-map respawn.
            world.Monsters[0].TakeDamage(100000);
            world.LoadMap(100000000);
            yield return null; yield return null;
            Step(world, 1000);
            Assert.That(world.Monsters, Is.Empty);
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None), Is.Empty);
        }

        private static void Step(GameWorld world, int count)
        {
            for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
        }
        private static void Capture(Monster monster, string suffix)
        {
            var camera = Camera.main; var position = camera.transform.position; float size = camera.orthographicSize;
            try
            {
                camera.transform.position = new Vector3(monster.Position.X - .15f, monster.Position.Y + .35f, position.z);
                camera.orthographicSize = .85f;
                RecoverySceneSmokeTests.SaveCameraImage(camera, suffix, 640, 640);
            }
            finally { camera.transform.position = position; camera.orthographicSize = size; }
        }
    }
}
