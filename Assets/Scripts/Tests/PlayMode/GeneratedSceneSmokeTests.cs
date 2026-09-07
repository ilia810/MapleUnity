#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class GeneratedSceneSmokeTests
    {
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Log) diagnostics.Add(type + ": " + message);
        }
        [TearDown] public void CheckLogs()
        {
            Application.logMessageReceived -= OnLog;
            Assert.That(diagnostics, Is.Empty);
        }

        [UnityTest]
        public IEnumerator GeneratedMarketKeepsPreviewArtAndRepairsLostArtOnStartup()
        {
            // Load a normal scene first so the Unity test runner retains its own
            // persistent scene. Explicitly unloading its initial scene can stall it.
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null;
            foreach (var oldRoot in SceneManager.GetActiveScene().GetRootGameObjects())
                Object.Destroy(oldRoot);
            yield return null;
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.aspect = 1024f / 768f;
            var root = MapSceneGeneratorWindow.GeneratePlayableMap(100000100);
            var manager = Object.FindFirstObjectByType<GameManager>();
            Assert.That(manager.StartingMapId, Is.EqualTo(100000100), "Generated scenes must play their selected map.");
            // Exercise the exact helper used by the editor window, including its
            // cleanup. It previously destroyed all the sprites at this point.
            AssertArt(root);
            manager.gameObject.SetActive(false);
            root.SetActive(false);

            // Reproduce old scenes whose generated sprites were deleted on save
            // or NX shutdown. Startup must hydrate every damaged art category.
            MapleClient.GameData.SpriteLoader.ClearCache();
            yield return null;
            Assert.That(root.GetComponentsInChildren<MapTile>(true).Any(tile =>
                tile.GetComponentInChildren<SpriteRenderer>(true).sprite == null), Is.True);
            root.SetActive(true);
            manager.gameObject.SetActive(true);
            yield return null; yield return null;
            var world = (GameWorld)typeof(GameManager).GetField("gameWorld", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Assert.That(world.CurrentMapId, Is.EqualTo(100000100));
            Assert.That(Object.FindObjectsByType<MapInfo>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            AssertArt(root);
            Assert.That(root.GetComponentsInChildren<NPCBehavior>().Length, Is.EqualTo(4));
            Assert.That(root.GetComponentsInChildren<NPCBehavior>().All(n =>
                n.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite != null)), Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(3.84f), "768 source pixels at 100 pixels/unit.");
            float deadline = Time.realtimeSinceStartup + 3;
            while (!world.Player.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(world.Player.IsGrounded, Is.True);
            RecoverySceneSmokeTests.SaveCameraImage(camera, "-generated-market", 1024, 768);
        }

        private static void AssertArt(GameObject root)
        {
            var tiles = root.GetComponentsInChildren<MapTile>();
            var objects = root.GetComponentsInChildren<MapObject>();
            var backgrounds = root.GetComponentsInChildren<ViewportBackgroundLayer>();
            Assert.That(tiles, Is.Not.Empty);
            Assert.That(objects, Is.Not.Empty);
            Assert.That(backgrounds, Is.Not.Empty);
            foreach (var item in tiles.Cast<Component>().Concat(objects))
            {
                var renderer = item.GetComponentInChildren<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sprite, Is.Not.Null, "Missing generated sprite: " + item.name);
                Assert.That(renderer.sprite.texture, Is.Not.Null);
                Assert.That(renderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            Assert.That(backgrounds.All(b => b.tileSprite != null && b.tileSprite.texture != null), Is.True);
        }
    }
}
#endif
