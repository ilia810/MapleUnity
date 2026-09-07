using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using MapInfo = MapleClient.SceneGeneration.MapInfo;
using LogicVector = MapleClient.GameLogic.Vector2;

namespace MapleClient.Tests.PlayMode
{
    public class MapTransitionSmokeTests
    {
        private sealed class Input : IInputProvider
        {
            public bool IsLeftPressed => false;
            public bool IsRightPressed => false;
            public bool IsJumpPressed => false;
            public bool IsAttackPressed => false;
            public bool IsUpPressed { get; set; }
            public bool IsDownPressed => false;
        }
        private readonly List<string> diagnostics = new List<string>();
        [SetUp] public void WatchLogs() { diagnostics.Clear(); Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Log) diagnostics.Add(type + ": " + message);
        }
        [TearDown] public void CheckLogs()
        {
            Application.logMessageReceived -= OnLog;
            Assert.That(diagnostics, Is.Empty, "Map transitions must not emit runtime warnings/errors.");
        }

        [UnityTest]
        public IEnumerator RealPortals_RebuildMapsSnapCameraAndRetainOnePlayer()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindFirstObjectByType<GameManager>();
            Assert.That(manager, Is.Not.Null);
            manager.enabled = false;
            Camera.main.aspect = 1280f / 720f;
            var world = (GameWorld)typeof(GameManager).GetField("gameWorld", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            var input = new Input();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, input);
            var playerObject = GameObject.Find("Player");
            var follow = Camera.main.GetComponent<SimpleCameraFollow>();
            int listeners = ListenerCount(world.Player);
            Assert.That(GameManager.MapSceneFactory, Is.Not.Null, "Exercise the application startup bridge.");
            AssertMap(world, 100000000, 338, 35);

            world.AddDroppedItem(2000000, 1, new LogicVector(1000, 1000));
            var oldItem = Object.FindFirstObjectByType<DroppedItemView>();
            Assert.That(oldItem, Is.Not.Null);
            var oldMap = GameObject.Find("Map_100000000");
            Travel(world, input, playerObject, "in00", 100000100, "out00");
            Assert.That(oldMap.activeSelf, Is.False, "Hide old scenery in the same tick.");
            Assert.That(oldItem.gameObject.activeSelf, Is.False, "Hide old transient views in the same tick.");
            yield return null; yield return null;
            Assert.That(oldMap == null && oldItem == null, Is.True, "Release old map and transient objects.");
            Assert.That(world.DroppedItems, Is.Empty);
            AssertMap(world, 100000100, 210, 4);
            // Check the loader's own service before GameWorld can replace it.
            var loadedService = new MapleClient.GameLogic.FootholdService();
            var loadedMap = new MapleClient.GameData.NxMapLoader("", loadedService).GetMap(100000100);
            var loadedFloors = loadedService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue).ToArray();
            CollectionAssert.AreEqual(loadedMap.Platforms.Select(p => p.Id).ToArray(), loadedFloors.Select(f => f.Id).ToArray());
            foreach (var platform in loadedMap.Platforms)
            {
                var floor = loadedFloors.Single(f => f.Id == platform.Id);
                Assert.That(floor.PreviousId, Is.EqualTo(platform.PreviousId));
                Assert.That(floor.NextId, Is.EqualTo(platform.NextId));
                Assert.That(floor.Layer, Is.EqualTo(platform.Layer));
            }
            RecoverySceneSmokeTests.SaveCameraImage(Camera.main, "-market");

            // The market has a real teleport within the same map.
            var market = GameObject.Find("Map_100000100");
            world.AddDroppedItem(2000000, 1, new LogicVector(1000, 1000));
            var marketItem = Object.FindFirstObjectByType<DroppedItemView>();
            Travel(world, input, playerObject, "h_east", 100000100, "h_west");
            yield return null; yield return null;
            Assert.That(GameObject.Find("Map_100000100"), Is.SameAs(market), "A same-map teleport must reuse its scenery.");
            AssertMap(world, 100000100, 210, 4);
            Assert.That(world.DroppedItems.Count, Is.EqualTo(1), "An intramap warp preserves map state.");
            Assert.That(marketItem != null && marketItem.gameObject.activeInHierarchy, Is.True);

            Travel(world, input, playerObject, "out00", 100000000, "in00");
            yield return null; yield return null;
            Assert.That(market == null, Is.True);
            AssertMap(world, 100000000, 338, 35);
            Assert.That(GameObject.Find("Map_100000000").GetComponentsInChildren<ViewportBackgroundLayer>().Length, Is.EqualTo(8));
            RecoverySceneSmokeTests.SaveCameraImage(Camera.main, "-henesys-return");

            Travel(world, input, playerObject, "in02", 100000001, "out02");
            yield return null; yield return null;
            AssertMap(world, 100000001, 34, 1);
            var interior = GameObject.Find("Map_100000001").GetComponent<MapInfo>();
            Assert.That(interior.vrBounds.size.x, Is.LessThan(Camera.main.orthographicSize * 2 * Camera.main.aspect));
            Assert.That(Camera.main.transform.position.x, Is.EqualTo(interior.vrBounds.center.x).Within(0.001));
            RecoverySceneSmokeTests.SaveCameraImage(Camera.main, "-interior");

            Travel(world, input, playerObject, "out02", 100000000, "in02");
            yield return null; yield return null;
            AssertMap(world, 100000000, 338, 35);

            // Falling past the source terrain limit must also snap the views.
            var terrain = new NormalTerrain(manager.FootholdService.GetFootholdsInArea(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue));
            int recovered = 0;
            world.PlayerRecovered += () => recovered++;
            world.Player.Position = new LogicVector(world.Player.Position.X, (float)(-terrain.Bottom / 100 - 2));
            world.Player.Velocity = LogicVector.Zero;
            world.Player.ResetMovementForMap();
            Step(world);
            Assert.That(recovered, Is.EqualTo(1));
            AssertViewPosition(world, playerObject);
            AssertCamera(GameObject.Find("Map_100000000").GetComponent<MapInfo>().vrBounds, world.Player.Position);
            Assert.That(world.Player.PreviousPosition, Is.EqualTo(world.Player.Position));
            input.IsUpPressed = false;
            for (int i = 0; i < 32; i++) Step(world);
            Assert.That(world.Player.IsGrounded, Is.True);
            Assert.That(world.Player.CurrentFootholdId, Is.GreaterThan(0));
            yield return null; yield return null;
            RecoverySceneSmokeTests.SaveCameraImage(Camera.main, "-recovered");

            Assert.That(GameObject.Find("Player"), Is.SameAs(playerObject));
            Assert.That(Camera.main.GetComponent<SimpleCameraFollow>(), Is.SameAs(follow));
            Assert.That(ListenerCount(world.Player), Is.EqualTo(listeners), "Travel must not accumulate character/camera listeners.");
            Assert.That(Object.FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Object.Destroy(follow); Object.Destroy(playerObject);
            yield return null;
            Assert.That(world.Player.HasViewListeners, Is.False);
        }

        private static void Travel(GameWorld world, Input input, GameObject playerObject, string sourceName, int targetId, string targetName)
        {
            input.IsUpPressed = false; Step(world);
            float cameraSize = Camera.main.orthographicSize;
            var source = world.CurrentMap.Portals.Single(p => p.Name == sourceName);
            Assert.That(source.TargetMapId, Is.EqualTo(targetId));
            Assert.That(source.TargetPortalName, Is.EqualTo(targetName));
            world.Player.Position = new LogicVector(source.X / 100f, -source.Y / 100f + Player.Height / 2);
            world.Player.Velocity = LogicVector.Zero;
            world.Player.IsGrounded = false;
            world.Player.ResetMovementForMap();
            bool observed = false;
            Action<MapleClient.GameLogic.MapData> check = map => {
                observed = true;
                Assert.That(map.MapId, Is.EqualTo(targetId));
                var target = map.Portals.Single(p => p.Name == targetName);
                Assert.That(world.Player.Position.X, Is.EqualTo(target.X / 100f).Within(0.001));
                Assert.That(world.Player.PreviousPosition, Is.EqualTo(world.Player.Position));
                AssertViewPosition(world, playerObject);
                Assert.That(Camera.main.orthographicSize, Is.EqualTo(cameraSize), "Changing maps must retain camera zoom.");
                var info = GameObject.Find("Map_" + targetId).GetComponent<MapInfo>();
                AssertCamera(info.vrBounds, world.Player.Position);
            };
            Action teleported = () => check(world.CurrentMap);
            world.MapLoaded += check;
            world.PlayerTeleported += teleported;
            try { input.IsUpPressed = true; Step(world); }
            finally { world.MapLoaded -= check; world.PlayerTeleported -= teleported; }
            Assert.That(observed, Is.True, "Up at the authored portal must load " + targetId);
            for (int i = 0; i < 20; i++) Step(world);
            Assert.That(world.CurrentMapId, Is.EqualTo(targetId), "Holding Up must not immediately travel back.");
            Debug.Log("MAP_TRANSITION: " + sourceName + " -> " + targetId + "/" + targetName);
        }

        private static void AssertMap(GameWorld world, int id, int footholdCount, int npcCount)
        {
            Assert.That(world.CurrentMapId, Is.EqualTo(id));
            Assert.That(Object.FindObjectsByType<MapInfo>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            RecoverySceneSmokeTests.AssertPlayerOrder(GameObject.Find("Player"), world.Player);
            var root = GameObject.Find("Map_" + id);
            Assert.That(root, Is.Not.Null);
            var terrain = root.GetComponent<FootholdManager>();
            Assert.That(world.CurrentMap.Platforms.Count, Is.EqualTo(footholdCount));
            Assert.That(terrain.GetAllFootholds().Count, Is.EqualTo(footholdCount));
            foreach (var platform in world.CurrentMap.Platforms)
            {
                var source = terrain.GetFootholdById(platform.Id);
                Assert.That(source, Is.Not.Null);
                Assert.That(platform.X1, Is.EqualTo(source.X1)); Assert.That(platform.Y1, Is.EqualTo(source.Y1));
                Assert.That(platform.X2, Is.EqualTo(source.X2)); Assert.That(platform.Y2, Is.EqualTo(source.Y2));
                Assert.That(platform.PreviousId, Is.EqualTo(source.Prev)); Assert.That(platform.NextId, Is.EqualTo(source.Next));
                Assert.That(platform.Layer, Is.EqualTo(source.Layer)); Assert.That(platform.HasSourceTopology, Is.True);
            }
            var npcs = root.GetComponentsInChildren<NPCBehavior>();
            Assert.That(npcs.Length, Is.EqualTo(npcCount));
            foreach (var npc in npcs)
            {
                var floor = terrain.GetFootholdById(npc.footholdId);
                Assert.That(floor, Is.Not.Null);
                RecoverySceneSmokeTests.AssertNpcOrder(npc, floor.Layer);
                Assert.That(npc.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.sprite != null),
                    Is.True, "NPC art, including info/link aliases: " + npc.npcId);
                float x = npc.transform.position.x * 100;
                float y = floor.Y1 + (x - floor.X1) * (floor.Y2 - floor.Y1) / (floor.X2 - floor.X1);
                Assert.That(npc.transform.position.y, Is.EqualTo(-y / 100).Within(0.001), "NPC feet: " + npc.npcId);
            }
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled && r.sprite != null), Is.GreaterThan(10));
            foreach (var name in new[] { "Backgrounds", "Tiles", "Objects", "NPCs", "Portals" })
                Assert.That(root.transform.Cast<Transform>().Count(t => t.name == name), Is.EqualTo(1), "One map container: " + name);
        }

        private static void AssertCamera(Bounds bounds, LogicVector player)
        {
            var camera = Camera.main;
            float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
            float x = bounds.size.x <= halfWidth * 2 ? bounds.center.x : Mathf.Clamp(player.X, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
            float y = bounds.size.y <= halfHeight * 2 ? bounds.center.y : Mathf.Clamp(player.Y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
            Assert.That(camera.transform.position.x, Is.EqualTo(x).Within(0.001), "Camera X must already use destination VR bounds.");
            Assert.That(camera.transform.position.y, Is.EqualTo(y).Within(0.001), "Camera Y must already use destination VR bounds.");
        }
        private static void AssertViewPosition(GameWorld world, GameObject player)
        {
            Assert.That(player.transform.position.x, Is.EqualTo(world.Player.Position.X).Within(0.001));
            Assert.That(player.transform.position.y, Is.EqualTo(world.Player.Position.Y).Within(0.001));
        }
        private static int ListenerCount(Player player) =>
            ((ICollection)typeof(Player).GetField("viewListeners", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).Count;
        private static void Step(GameWorld world) { world.ProcessInput(); world.UpdatePhysics(0.008f); }
    }
}
