using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.GameView.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class PlayerCombatSceneSmokeTests
    {
        private readonly System.Collections.Generic.List<string> diagnostics = new System.Collections.Generic.List<string>();
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
        public IEnumerator ContactXpDefeatAndRevivalWorkInTheHuntingScene()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>();
            var world = (GameWorld)typeof(GameManager).GetField("gameWorld", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(world, input);
            manager.enabled = false; world.LoadMap(100010000);
            Step(world, 100); yield return null; yield return null;
            var player = world.Player;
            var target = world.Monsters.Where(m => m.MonsterId == 100101).OrderBy(m => Math.Abs(m.Position.X - 5.3f)).First();
            player.ResetMovementForMap(); player.Position = new LogicVector(target.Position.X, target.Position.Y + Player.Height / 2);
            player.Velocity = LogicVector.Zero; player.IsGrounded = true;
            int hp = player.CurrentHP; Step(world, 1);
            Assert.That(player.CurrentHP, Is.LessThan(hp)); Assert.That(player.IsInvulnerable, Is.True);
            Step(world, 10);
            yield return null; yield return null;
            var feedback = Object.FindFirstObjectByType<PlayerCombatFeedback>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.GetComponentsInChildren<Text>().Any(t => t.name == "PlayerDamage"), Is.True);
            var body = GameObject.Find("Player").transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
            Assert.That(body.color.a, Is.LessThan(1));
            var camera = Camera.main;
            camera.transform.position = new Vector3(player.Position.X, player.Position.Y + .2f, -10);
            camera.orthographicSize = 1.8f;
            yield return null;
            yield return CaptureScreen("-player-contact");

            // Attack from outside contact range while still protected by the first hit.
            player.Position = new LogicVector(target.Position.X - .4f, target.Position.Y + Player.Height / 2);
            player.Velocity = LogicVector.Zero; input.IsRightPressed = true; Step(world, 1); input.IsRightPressed = false;
            player.SetBaseDamage(1000); input.IsAttackPressed = true; Step(world, 1); input.IsAttackPressed = false;
            Assert.That(target.IsDead, Is.True); Assert.That(player.Experience, Is.EqualTo(4));
            yield return null; yield return null;
            var exp = Object.FindFirstObjectByType<ExperienceBar>().transform.Find("ClassicHUD/ExperienceBar/Text").GetComponent<Text>();
            Assert.That(exp.text, Does.StartWith("4 [26.67%]"));
            var expFill = Object.FindFirstObjectByType<ExperienceBar>().transform.Find("ClassicHUD/ExperienceBar/Missing").GetComponent<Image>();
            Assert.That(expFill.rectTransform.rect.width, Is.EqualTo(115-Mathf.Round(115*4f/15)).Within(.0001),
                "The EXP mask follows the earned ratio on whole native pixels.");
            yield return CaptureScreen("-experience");
            player.AddExperience(11); yield return null;
            Assert.That(player.Level, Is.EqualTo(2));
            Assert.That(feedback.GetComponentsInChildren<Text>().Any(t => t.name == "EventNotification" && t.text.Contains("Level 2")), Is.True);
            yield return CaptureScreen("-level-up");

            Object.FindFirstObjectByType<InventoryView>().Show(true); // Revival must stay reachable above an open window.
            player.TakeDamage(100000); var position = player.Position;
            input.IsRightPressed = input.IsAttackPressed = true; Step(world, 20);
            Assert.That(player.Position, Is.EqualTo(position));
            yield return null; yield return null;
            var button = feedback.transform.Find("CombatFeedback/DefeatPanel/DefeatFrame/ReviveButton").GetComponent<Button>();
            Assert.That(button, Is.Not.Null); Assert.That(button.interactable, Is.True);
            Assert.That(GameObject.Find("DefeatPanel"), Is.Not.Null);
            yield return CaptureScreen("-player-defeated");
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center)) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0)); Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            input.IsRightPressed = input.IsAttackPressed = false;
            Step(world, 10); yield return null; yield return null;
            Assert.That(player.IsDead, Is.False); Assert.That(player.CurrentHP, Is.EqualTo(player.MaxHP));
            Assert.That(world.CurrentMapId, Is.EqualTo(100000000));
            Assert.That(player.Level, Is.EqualTo(2)); Assert.That(player.Experience, Is.Zero);
            Assert.That(GameObject.Find("DefeatPanel"), Is.Null);
            Assert.That(feedback.GetComponentsInChildren<Text>().Any(t => t.name == "PlayerDamage"), Is.False, "Damage numbers must not travel into the return map.");
            Assert.That(world.Monsters, Is.Empty);
            Assert.That(Object.FindObjectsByType<MonsterView>(FindObjectsSortMode.None), Is.Empty);
            yield return CaptureScreen("-player-revived");

        }

        private static void Step(GameWorld world, int count)
        {
            for (int i = 0; i < count; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
        }
        internal static IEnumerator CaptureScreen(string suffix, int width = 1280, int height = 720, System.Action checkLayout = null)
        {
            // Batch-mode Unity does not resume WaitForEndOfFrame. Render the overlay
            // through the camera temporarily so both the world and HUD reach the artifact.
            yield return null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var camera = Camera.main;
            var previousTarget = camera.targetTexture;
            float previousAspect = camera.aspect;
            var layoutTarget = new RenderTexture(width, height, 24);
            var layers = canvases.Select(c => c.sortingLayerID).ToArray();
            var orders = canvases.Select(c => c.sortingOrder).ToArray();
            try
            {
                // Let responsive UI update at the capture size before rendering it.
                camera.targetTexture = layoutTarget; camera.aspect = (float)width / height;
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingLayerName = "UI"; canvas.sortingOrder = 30000; }
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                checkLayout?.Invoke();
                RecoverySceneSmokeTests.SaveCameraImage(camera, suffix, width, height);
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.aspect = previousAspect;
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
                for (int i = 0; i < canvases.Length; i++) { canvases[i].sortingLayerID = layers[i]; canvases[i].sortingOrder = orders[i]; }
                Canvas.ForceUpdateCanvases();
                layoutTarget.Release(); Object.Destroy(layoutTarget);
            }
        }
    }
}
