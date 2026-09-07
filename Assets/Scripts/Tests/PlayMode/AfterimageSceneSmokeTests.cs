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
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class AfterimageSceneSmokeTests
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
        private static void FinishSwing(GameWorld world)
        {
            for (int i = 0; i < 180 && world.Player.IsBasicAttacking; i++) Step(world);
            Assert.That(world.Player.IsBasicAttacking, Is.False);
        }
        private static void Capture(Player player, string suffix)
        {
            var camera = Camera.main; var position = camera.transform.position; float size = camera.orthographicSize;
            try {
                camera.transform.position = new Vector3(player.Position.X, player.Position.Y + .05f, -10);
                camera.orthographicSize = 1.25f;
                RecoverySceneSmokeTests.SaveCameraImage(camera, suffix, 800, 600);
            }
            finally { camera.transform.position = position; camera.orthographicSize = size; }
        }

        [UnityTest]
        public IEnumerator SourceTrailsFadeAtTheFeetAndImpactsAwardDamageAndExperienceAfterWindup()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>(); manager.enabled = false;
            var world = manager.World; var player = world.Player; var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            Step(world, 100); Assert.That(world.RequestPracticeSupplies(out _), Is.True);
            foreach (int id in new[] { 1040002, 1060002, 1072001, 1302000 }) Assert.That(world.UseInventoryItem(id, out _), Is.True);
            // A stationary real-NX target isolates melee timing from the separate contact/patrol checks.
            world.SpawnMonsterForTesting(100101, new LogicVector(player.Position.X + .65f, player.Position.Y - Player.Height / 2));
            var target = world.Monsters.Single(); target.SetMovementPattern(MovementPattern.Stationary); target.Template.BodyAttack = false;
            player.SetBaseDamage(1 - player.EquipmentBonus(StatType.WeaponAttack));
            yield return null; yield return null;
            var visual = GameObject.Find("Player").transform.Find("VisualRoot");
            var trail = visual.Find("WeaponAfterimage").GetComponent<SpriteRenderer>();
            var targetView = Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None).Single(v => v.Model == target);
            int hp = target.HP; long exp = player.Experience; int hits = 0;
            target.DamageTaken += (_, __) => hits++;
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            var motion = player.BasicAttack; var effect = motion.Afterimage;
            Assert.That(effect, Is.Not.Null); Assert.That(target.HP, Is.EqualTo(hp)); Assert.That(player.Experience, Is.EqualTo(exp));
            yield return null; Assert.That(trail.sprite, Is.Null); Capture(player, "-sword-windup");
            while (motion.ElapsedMilliseconds + 8 < motion.HitDelayMilliseconds) Step(world);
            Assert.That(target.HP, Is.EqualTo(hp)); Step(world);
            Assert.That(target.HP, Is.EqualTo(hp - 1)); Assert.That(hits, Is.EqualTo(1));
            for (int i = 0; i < 20 && motion.Frame < effect.FirstFrame; i++) Step(world);
            Assert.That(motion.Frame, Is.GreaterThanOrEqualTo(effect.FirstFrame));
            yield return null; yield return null;
            Assert.That(trail.sprite, Is.Not.Null); Assert.That(trail.sprite.name, Does.StartWith(effect.Path + "/"));
            // Compare visible character art; an unused skill-effect layer has no sprite.
            Assert.That(trail.sortingOrder, Is.GreaterThan(visual.GetComponentsInChildren<SpriteRenderer>().Where(r => r != trail && r.sprite != null).Max(r => r.sortingOrder)));
            Assert.That(trail.bounds.center.x, Is.GreaterThan(visual.position.x), "A rightward source trail must be in front of the feet.");
            Assert.That(Object.FindFirstObjectByType<MapleClient.GameView.UI.ClassicMonsterHealthView>().HasVisibleBar(targetView), Is.True);
            Capture(player, "-sword-impact");
            int pausedTime = motion.AfterimageMilliseconds; float pausedAlpha = trail.color.a;
            yield return new WaitForSeconds(.12f);
            Assert.That(motion.AfterimageMilliseconds, Is.EqualTo(pausedTime)); Assert.That(trail.color.a, Is.EqualTo(pausedAlpha));
            Step(world, 10); yield return null;
            Assert.That(trail.color.a, Is.LessThan(pausedAlpha)); Assert.That(trail.color.a, Is.GreaterThan(0));
            Capture(player, "-sword-fade");
            FinishSwing(world); yield return null; Assert.That(trail.sprite, Is.Null); Assert.That(hits, Is.EqualTo(1));

            Assert.That(world.UseInventoryItem(1442079, out _), Is.True);
            input.IsLeftPressed = true; Step(world); input.IsLeftPressed = false;
            target.Position = new LogicVector(player.Position.X - 1.15f, player.Position.Y - Player.Height / 2);
            target.Velocity = LogicVector.Zero; player.SetBaseDamage(1000);
            int reward = target.Template.Exp;
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            motion = player.BasicAttack; effect = motion.Afterimage;
            Assert.That(target.IsDead, Is.False); Assert.That(player.Experience, Is.EqualTo(exp));
            while (motion.ElapsedMilliseconds + 8 < motion.HitDelayMilliseconds) Step(world);
            Assert.That(target.IsDead, Is.False); Step(world);
            Assert.That(target.IsDead, Is.True); Assert.That(player.Experience, Is.EqualTo(exp + reward));
            for (int i = 0; i < 20 && motion.Frame < effect.FirstFrame; i++) Step(world);
            yield return null; yield return null;
            Assert.That(trail.sprite, Is.Not.Null); Assert.That(trail.bounds.center.x, Is.LessThan(visual.position.x));
            Capture(player, "-polearm-impact");
            // Input may change, but the in-progress body and trail retain the selected attack's direction.
            input.IsRightPressed = true; Step(world); input.IsRightPressed = false; yield return null;
            Assert.That(visual.localScale.x, Is.GreaterThan(0));
            world.LoadMap(100000001); yield return null; yield return null;
            Assert.That(trail.sprite, Is.Null); Assert.That(player.IsBasicAttacking, Is.False);

            world.SpawnMonsterForTesting(100101, new LogicVector(player.Position.X - .8f, player.Position.Y - Player.Height / 2));
            var canceledTarget = world.Monsters.Single(); canceledTarget.Template.BodyAttack = false;
            input.IsLeftPressed = true; Step(world); input.IsLeftPressed = false;
            input.IsAttackPressed = true; Step(world); input.IsAttackPressed = false;
            int oldHp = canceledTarget.HP; long oldExp = player.Experience;
            world.LoadMap(100000000); Step(world, 150); yield return null; yield return null;
            Assert.That(canceledTarget.HP, Is.EqualTo(oldHp)); Assert.That(player.Experience, Is.EqualTo(oldExp));
            Assert.That(trail.sprite, Is.Null); Assert.That(world.Monsters, Is.Empty);
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None), Is.Empty);
        }
    }
}
