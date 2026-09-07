using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class RecoverySceneSmokeTests
    {
        private readonly List<string> unexpectedLogs = new List<string>();
        [SetUp]
        public void WatchRuntimeDiagnostics()
        {
            unexpectedLogs.Clear();
            Application.logMessageReceived += OnLog;
        }
        [TearDown]
        public void VerifyRuntimeDiagnostics()
        {
            Application.logMessageReceived -= OnLog;
            Assert.That(unexpectedLogs, Is.Empty, "Scene must not emit warnings or errors.");
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Log) unexpectedLogs.Add(type + ": " + message);
        }

        private sealed class TestInput : IInputProvider
        {
            public bool IsLeftPressed { get; set; }
            public bool IsRightPressed { get; set; }
            public bool IsJumpPressed { get; set; }
            public bool IsAttackPressed { get; set; }
            public bool IsUpPressed { get; set; }
            public bool IsDownPressed { get; set; }
        }
        [UnityTest]
        public IEnumerator Henesys_LoadsRenderedPlayer_WalksJumpsClimbsAndDrops()
        {
            yield return SceneManager.LoadSceneAsync("henesys", LoadSceneMode.Single);
            yield return null; yield return null;
            var manager = Object.FindObjectOfType<GameManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.Player, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            var world = (GameWorld)typeof(GameManager).GetField("gameWorld", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Assert.That(world.CurrentMapId, Is.EqualTo(100000000));
            Assert.That(world.CurrentMap.Platforms.Count, Is.GreaterThan(0));
            var input = new TestInput();
            typeof(GameWorld).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, input);
            typeof(GameManager).GetField("inputProvider", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, input);
            var playerObject = GameObject.Find("Player");
            Assert.That(playerObject, Is.Not.Null);
            Assert.That(playerObject.GetComponent<SimplePlayerController>(), Is.Not.Null);
            var map = GameObject.Find("Map_100000000");
            Assert.That(map, Is.Not.Null);
            Assert.That(map.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled && r.sprite != null), Is.GreaterThan(10));
            var mapInfo = map.GetComponent<MapleClient.SceneGeneration.MapInfo>();
            Assert.That(mapInfo.vrBounds.min.x, Is.EqualTo(-10.28f).Within(0.001f));
            Assert.That(mapInfo.vrBounds.max.x, Is.EqualTo(63.38f).Within(0.001f));
            Assert.That(mapInfo.vrBounds.min.y, Is.EqualTo(-6.54f).Within(0.001f));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (!manager.Player.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(manager.Player.IsGrounded, Is.True, "Player should settle on a real foothold.");
            AssertPlayerOrder(playerObject, manager.Player);
            var layers = playerObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (var name in new[] { "Body", "Head", "Face", "Hair", "HairOverHead" })
            {
                var layer = layers.FirstOrDefault(r => r.gameObject.name == name);
                Assert.That(layer, Is.Not.Null, "Missing character layer: " + name);
                Assert.That(layer.enabled && layer.gameObject.activeInHierarchy, Is.True);
                Assert.That(layer.sprite, Is.Not.Null, "Missing NX sprite: " + name);
                Assert.That(layer.sprite.texture, Is.Not.Null);
                Assert.That(Vector3.Distance(layer.bounds.center, playerObject.transform.position), Is.LessThan(1.5f), "Detached layer: " + name);
            }
            var footholds = map.GetComponent<FootholdManager>();
            Assert.That(world.CurrentMap.Platforms.Count, Is.EqualTo(footholds.GetAllFootholds().Count));
            foreach (var platform in world.CurrentMap.Platforms)
            {
                var source = footholds.GetFootholdById(platform.Id);
                Assert.That(source, Is.Not.Null, "Preserve source foothold IDs.");
                Assert.That(platform.HasSourceTopology, Is.True);
                Assert.That(platform.X1, Is.EqualTo(source.X1));
                Assert.That(platform.Y1, Is.EqualTo(source.Y1));
                Assert.That(platform.X2, Is.EqualTo(source.X2));
                Assert.That(platform.Y2, Is.EqualTo(source.Y2));
                Assert.That(platform.PreviousId, Is.EqualTo(source.Prev));
                Assert.That(platform.NextId, Is.EqualTo(source.Next));
                Assert.That(platform.Layer, Is.EqualTo(source.Layer));
            }
            var npcs = map.GetComponentsInChildren<NPCBehavior>();
            Assert.That(npcs.Length, Is.EqualTo(35));
            foreach (var npc in npcs)
            {
                var support = footholds.GetFootholdById(npc.footholdId);
                Assert.That(support, Is.Not.Null, "NPC foothold " + npc.npcId);
                AssertNpcOrder(npc, support.Layer);
                float sourceX = npc.transform.position.x * 100f;
                float sourceY = support.Y1 + (sourceX - support.X1) * (support.Y2 - support.Y1) / (support.X2 - support.X1);
                Assert.That(npc.transform.position.y, Is.EqualTo(-sourceY / 100f).Within(0.0001f),
                    "NPC must stand at its source foothold: " + npc.npcId);
            }
            Assert.That(playerObject.transform.Find("VisualRoot/DefaultBottom")
                .GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            var backgrounds = map.GetComponentsInChildren<ViewportBackgroundLayer>()
                .OrderBy(layer => layer.backgroundData.No).ToArray();
            Assert.That(backgrounds.Length, Is.EqualTo(8), "All source background layers must be hydrated.");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 5, 6, 5, 6 },
                backgrounds.Select(layer => layer.backgroundData.SpriteNo).ToArray());
            CollectionAssert.AreEqual(new[] { 256f, 1024f, 2493f, 315f, 1024f, 1024f, 1024f, 1024f },
                backgrounds.Select(layer => layer.tileSprite.rect.width).ToArray(),
                "Background selectors must load their own bitmaps, not repeat back/0.");
            Assert.That(backgrounds.All(layer => layer.GetComponentsInChildren<SpriteRenderer>()
                .Any(tile => tile.sprite != null)), Is.True);
            var groundTile = map.GetComponentsInChildren<MapTile>()
                .First(tile => tile.layer == 1 && tile.tileSet == "woodMarble" && tile.variant == "enH0" && tile.tileNumber == 2);
            var groundRenderer = groundTile.GetComponentInChildren<SpriteRenderer>();
            Assert.That(groundRenderer.sortingLayerName, Is.EqualTo("Objects"));
            Assert.That(groundRenderer.sortingOrder, Is.EqualTo(1381));
            foreach (var bush in map.GetComponentsInChildren<MapleClient.SceneGeneration.MapObject>().Where(obj => obj.layer <= 1))
                foreach (var sprite in bush.GetComponentsInChildren<SpriteRenderer>())
                {
                    Assert.That(sprite.sortingLayerID, Is.EqualTo(groundRenderer.sortingLayerID));
                    Assert.That(sprite.sortingOrder, Is.LessThan(groundRenderer.sortingOrder));
                }
            SaveCameraImage(Camera.main);
            var camera = Camera.main;
            var oldCameraPosition = camera.transform.position;
            float oldCameraSize = camera.orthographicSize;
            try
            {
                camera.transform.position = new Vector3(playerObject.transform.position.x,
                    playerObject.transform.position.y + 0.1f, oldCameraPosition.z);
                camera.orthographicSize = 0.8f;
                SaveCameraImage(camera, "-player", 640, 640);
                var npc = npcs.Single(n => n.npcId == "9200000");
                camera.transform.position = new Vector3(npc.transform.position.x,
                    npc.transform.position.y + 0.4f, oldCameraPosition.z);
                SaveCameraImage(camera, "-npc", 640, 640);
            }
            finally
            {
                camera.transform.position = oldCameraPosition;
                camera.orthographicSize = oldCameraSize;
            }
            float startX = manager.Player.Position.X;
            input.IsRightPressed = true;
            yield return new WaitForSeconds(0.2f);
            SaveCameraImage(Camera.main, "-walking");
            input.IsRightPressed = false;
            Assert.That(manager.Player.Position.X, Is.GreaterThan(startX + 0.05f));
            yield return new WaitForSeconds(0.3f);
            Assert.That(playerObject.transform.position.x, Is.EqualTo(manager.Player.Position.X).Within(0.1f));
            deadline = Time.realtimeSinceStartup + 4f;
            while (!manager.Player.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(manager.Player.IsGrounded, Is.True);
            float startY = manager.Player.Position.Y;
            input.IsJumpPressed = true;
            yield return new WaitForSeconds(0.08f);
            SaveCameraImage(Camera.main, "-jumping");
            AssertPlayerOrder(playerObject, manager.Player);
            input.IsJumpPressed = false;
            Assert.That(manager.Player.Position.Y, Is.GreaterThan(startY + 0.1f));
            Assert.That(manager.Player.IsGrounded, Is.False);
            deadline = Time.realtimeSinceStartup + 4f;
            while (!manager.Player.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(manager.Player.IsGrounded, Is.True);
            Assert.That(manager.Player.Position.Y, Is.EqualTo(startY).Within(0.15f));
            manager.enabled = false;
            yield return CheckTraversal(world, input, playerObject);
            var simulatedPlayer = manager.Player;
            var cameraFollow = Camera.main.GetComponent<SimpleCameraFollow>();
            if (cameraFollow != null) Object.Destroy(cameraFollow);
            Object.Destroy(playerObject);
            yield return null;
            Assert.That(simulatedPlayer.HasViewListeners, Is.False, "Runtime views must detach their listeners.");
        }
        private static IEnumerator CheckTraversal(GameWorld world, TestInput input, GameObject playerObject)
        {
            var source = global::GameData.NXDataManagerSingleton.Instance.GetMapNode(100000000)["ladderRope"];
            Assert.That(world.CurrentMap.Ladders.Count, Is.EqualTo(source.Children.Count()));
            Assert.That(world.CurrentMap.Ladders, Is.Not.Empty);
            foreach (var ladder in world.CurrentMap.Ladders)
            {
                var node = source[ladder.Id.ToString()];
                Assert.That(ladder.SourceX, Is.EqualTo(node["x"].GetValue<int>()));
                Assert.That(ladder.SourceTop, Is.EqualTo(node["y1"].GetValue<int>()));
                Assert.That(ladder.SourceBottom, Is.EqualTo(node["y2"].GetValue<int>()));
                Assert.That(ladder.IsLadder, Is.EqualTo(node["l"].GetValue<int>() != 0));
            }
            var player = world.Player;
            foreach (bool isLadder in new[] { true, false })
            {
                var ladder = world.CurrentMap.Ladders.Where(l => l.IsLadder == isLadder && l.Y2-l.Y1 > 0.5f)
                    .OrderBy(l => Math.Abs(l.X-player.Position.X)).FirstOrDefault();
                if (ladder == null) continue; // This map may not contain both climbable types.
                Debug.Log("TRAVERSAL_SCENE: "+(isLadder ? "ladder" : "rope")+" "+ladder.Id);
                input.IsJumpPressed=false; input.IsDownPressed=false; input.IsUpPressed=true;
                input.IsLeftPressed=false; input.IsRightPressed=false;
                player.ResetMovementForMap();
                player.Position=new MapleClient.GameLogic.Vector2(ladder.X,ladder.Y1+Player.Height/2);
                player.Velocity=MapleClient.GameLogic.Vector2.Zero; player.IsGrounded=false;
                StepWorld(world);
                Assert.That(player.GetCurrentLadder(), Is.SameAs(ladder));
                float entryY=player.Position.Y;
                for(int i=0;i<25;i++) StepWorld(world);
                Assert.That(player.Position.Y, Is.GreaterThan(entryY+0.2f));
                input.IsUpPressed=false; StepWorld(world);
                yield return null; yield return null;
                var body=playerObject.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>();
                Assert.That(body.sprite.name, Does.Contain(isLadder ? "/ladder/" : "/rope/"));
                Assert.That(playerObject.transform.Find("VisualRoot/Face").GetComponent<SpriteRenderer>().sprite, Is.Null);
                Assert.That(player.CurrentFootholdLayer, Is.EqualTo(7));
                AssertPlayerOrder(playerObject, player);
                SavePlayerCrop(playerObject, isLadder ? "-ladder" : "-rope");
                var pausedSprite=body.sprite;
                int pausedTime=player.StanceAnimation.ElapsedMilliseconds;
                yield return new WaitForSeconds(0.1f);
                Assert.That(body.sprite, Is.SameAs(pausedSprite), "Resting on a ladder freezes the climb pose.");
                Assert.That(player.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(pausedTime));
                input.IsUpPressed=true;
                int steps=0;
                while(player.State==PlayerState.Climbing && steps++<2000) StepWorld(world);
                Assert.That(player.State, Is.EqualTo(PlayerState.Falling), "Climb beyond the top without clamping.");
                Assert.That(player.ClimbCooldownMilliseconds, Is.GreaterThan(0));
                input.IsUpPressed=false;
                steps=0;
                while(!player.IsGrounded && steps++<500) StepWorld(world);
                Assert.That(player.IsGrounded, Is.True, "Settle on real terrain after the top exit.");
            }

            var terrain = new NormalTerrain(world.CurrentMap.Platforms.Select(p =>
                new MapleClient.GameLogic.Foothold(p.Id,p.X1,p.Y1,p.X2,p.Y2) {
                    PreviousId=p.PreviousId,NextId=p.NextId,Layer=p.Layer,IsWall=p.X1==p.X2
                }));
            var candidates = world.CurrentMap.Platforms.Where(p => p.Y1==p.Y2 && p.X2-p.X1>60)
                .Select(p => new { Platform=p, X=Math.Floor((p.X1+p.X2)/2.0) })
                .Where(c => !world.CurrentMap.Ladders.Any(l => Math.Abs(l.SourceX-c.X)<20))
                .Select(c => new { c.Platform,c.X, Below=terrain.Get(terrain.Below(c.X,c.Platform.Y1+1)) })
                .Where(c => c.Below!=null && NormalTerrain.Ground(c.Below,c.X)-c.Platform.Y1>30 &&
                    NormalTerrain.Ground(c.Below,c.X)-c.Platform.Y1<500)
                .OrderBy(c => Math.Abs(c.X-player.Position.X*100)).ToArray();
            Assert.That(candidates, Is.Not.Empty, "Henesys needs a real stacked platform for drop validation.");
            var drop=candidates[0];
            Debug.Log("TRAVERSAL_SCENE: drop "+drop.Platform.Id+" -> "+drop.Below.Id+" at X="+drop.X);
            player.ResetMovementForMap();
            player.Position=new MapleClient.GameLogic.Vector2((float)(drop.X/100),-drop.Platform.Y1/100f+Player.Height/2);
            player.Velocity=MapleClient.GameLogic.Vector2.Zero; player.IsGrounded=true;
            input.IsUpPressed=false; input.IsDownPressed=true; input.IsJumpPressed=false;
            StepWorld(world); StepWorld(world);
            Assert.That(player.CanDropThroughPlatform, Is.True);
            float platformY=player.Position.Y;
            input.IsJumpPressed=true; StepWorld(world);
            Assert.That(player.Position.Y, Is.LessThan(platformY));
            Assert.That(player.State, Is.EqualTo(PlayerState.Falling));
            input.IsJumpPressed=false; input.IsDownPressed=false;
            for(int i=0;i<10;i++) StepWorld(world);
            yield return null; yield return null;
            AssertPlayerOrder(playerObject, player);
            SavePlayerCrop(playerObject,"-drop-through");
            int fallingSteps=0;
            while(!player.IsGrounded && fallingSteps++<500) StepWorld(world);
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.CurrentFootholdId, Is.Not.EqualTo(drop.Platform.Id));
            Assert.That(-(player.Position.Y-Player.Height/2)*100,
                Is.EqualTo(NormalTerrain.Ground(drop.Below,drop.X)).Within(0.01));
            yield return null; yield return null;
            AssertPlayerOrder(playerObject, player);
        }

        internal static void AssertPlayerOrder(GameObject playerObject, Player player)
        {
            var group = playerObject.transform.Find("VisualRoot").GetComponent<SortingGroup>();
            Assert.That(group, Is.Not.Null, "The whole character must sort as one actor.");
            Assert.That(group.sortingLayerName, Is.EqualTo("Objects"));
            Assert.That(group.sortingOrder, Is.EqualTo(player.CurrentFootholdLayer * 1000 + 800));
            Assert.That(playerObject.GetComponent<SortingGroup>(), Is.Null, "Player UI stays outside the stage group.");
        }

        internal static void AssertNpcOrder(NPCBehavior npc, int footholdLayer)
        {
            foreach (var sprite in npc.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.That(sprite.sortingLayerName, Is.EqualTo("Objects"));
                Assert.That(sprite.sortingOrder, Is.EqualTo(footholdLayer * 1000 + 600), npc.npcId);
            }
        }

        private static void StepWorld(GameWorld world)
        {
            world.ProcessInput();
            world.UpdatePhysics(0.008f);
        }

        private static void SavePlayerCrop(GameObject playerObject, string suffix)
        {
            var camera=Camera.main;
            var previousPosition=camera.transform.position;
            float previousSize=camera.orthographicSize;
            try
            {
                camera.transform.position=new Vector3(playerObject.transform.position.x,
                    playerObject.transform.position.y+0.1f,previousPosition.z);
                camera.orthographicSize=1f;
                SaveCameraImage(camera,suffix,640,640);
            }
            finally { camera.transform.position=previousPosition; camera.orthographicSize=previousSize; }
        }

        internal static void SaveCameraImage(Camera camera, string suffix = "", int width = 1280, int height = 720)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-recoveryScreenshot");
            string path = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/henesys-smoke.png"));
            if (!string.IsNullOrEmpty(suffix))
                path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + suffix + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            float previousAspect = camera.aspect;
            var target = new RenderTexture(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.aspect = (float)width / height;
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG()); Debug.Log("RECOVERY_SCENE_SCREENSHOT: " + path);
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.aspect = previousAspect; RenderTexture.active = previousActive;
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
