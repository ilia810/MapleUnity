using System.Collections;
using System.Collections.Generic;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using MapleClient.SceneGeneration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MapleClient.Tests.PlayMode
{
    public class ActorOcclusionSmokeTests
    {
        [UnityTest]
        public IEnumerator DroppingBetweenLayersChangesOcclusionWithoutSplittingCharacterParts()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "This regression requires a graphics-enabled Unity run.");
            var root = new GameObject("ActorOcclusionTest");
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2);
            var target = new RenderTexture(32, 32, 24);
            var pixels = new Texture2D(32, 32, TextureFormat.RGB24, false);
            try
            {
                var service = new MapleClient.GameLogic.FootholdService();
                service.LoadFootholds(new List<MapleClient.GameLogic.Foothold> {
                    new MapleClient.GameLogic.Foothold(1, -200, 0, 200, 0) { Layer = 1 },
                    new MapleClient.GameLogic.Foothold(2, -100, -80, 100, -80) { Layer = 4 }
                });
                var map = new MapleClient.GameLogic.MapData();
                var player = new Player(service) {
                    Position = new MapleClient.GameLogic.Vector2(0, .8f + Player.Height / 2), IsGrounded = true
                };
                player.UpdatePhysics(.008f, map);
                Assert.That(player.CurrentFootholdLayer, Is.EqualTo(4));

                var actor = new GameObject("Player"); actor.transform.SetParent(root.transform);
                var renderer = actor.AddComponent<MapleCharacterRenderer>();
                // Use known opaque colors in the real character hierarchy to measure
                // occlusion independently of transparent NX artwork/animation frames.
                renderer.Initialize(player, null);
                var visual = actor.transform.Find("VisualRoot");
                var body = visual.Find("Body").GetComponent<SpriteRenderer>();
                body.sprite = sprite; body.color = Color.red;
                var head = visual.Find("Head").GetComponent<SpriteRenderer>();
                head.sprite = sprite; head.color = Color.blue;
                head.transform.localScale = new Vector3(.5f, 1, 1);
                head.transform.localPosition = new Vector3(.25f, 0, 0);
                var group = visual.GetComponent<SortingGroup>();
                Assert.That(group.sortingLayerName, Is.EqualTo("Objects"));

                var prop = MakeSprite(root, sprite, Color.magenta, "Objects", MapRenderOrder.ObjectOrder(3, 0));
                var npc = MakeSprite(root, sprite, Color.yellow, "Objects", StageRenderOrder.NpcOrder(1));
                var foreground = MakeSprite(root, sprite, Color.cyan, "Foreground", 0);
                foreground.enabled = false;
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var camera = new GameObject("Occlusion Camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform);
                camera.transform.position = new Vector3(0, -Player.Height / 2, -10);
                camera.orthographic = true; camera.orthographicSize = .5f;
                camera.aspect = 1; camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.targetTexture = target; camera.enabled = false;

                yield return null; yield return null;
                AssertPixels(camera, pixels, Color.red, Color.blue);

                player.Crouch(true); player.UpdatePhysics(.008f, map);
                player.Jump(); player.UpdatePhysics(.008f, map);
                player.Crouch(false); player.ReleaseJump();
                Assert.That(player.IsGrounded, Is.False);
                Assert.That(player.CurrentFootholdLayer, Is.EqualTo(4), "Retain the launch layer while falling.");
                yield return null; yield return null;
                AssertPixels(camera, pixels, Color.red, Color.blue);
                int ticks = 0;
                while (!player.IsGrounded && ticks++ < 500) player.UpdatePhysics(.008f, map);
                Assert.That(player.IsGrounded, Is.True);
                Assert.That(player.CurrentFootholdLayer, Is.EqualTo(1));
                yield return null; yield return null;
                Assert.That(group.sortingOrder, Is.EqualTo(1800));
                AssertPixels(camera, pixels, Color.magenta, Color.magenta);

                // Same-layer scenery precedes both actors; the player follows NPCs.
                prop.sortingOrder = MapRenderOrder.TileOrder(1, 127);
                AssertPixels(camera, pixels, Color.red, Color.blue);
                visual.gameObject.SetActive(false);
                AssertPixels(camera, pixels, Color.yellow, Color.yellow);
                prop.sortingOrder = MapRenderOrder.ObjectOrder(2, -128);
                AssertPixels(camera, pixels, Color.magenta, Color.magenta);
                visual.gameObject.SetActive(true);
                foreground.enabled = true;
                AssertPixels(camera, pixels, Color.cyan, Color.cyan);
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(sprite); Object.Destroy(texture);
                target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            }
        }

        private static SpriteRenderer MakeSprite(GameObject root, Sprite sprite, Color color, string layer, int order)
        {
            var renderer = new GameObject(layer).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(root.transform);
            renderer.transform.localPosition = new Vector3(0, -Player.Height / 2, 0);
            renderer.sprite = sprite; renderer.color = color;
            renderer.sortingLayerName = layer; renderer.sortingOrder = order;
            return renderer;
        }

        private static void AssertPixels(Camera camera, Texture2D pixels, Color left, Color right)
        {
            var previous = RenderTexture.active;
            try
            {
                camera.Render(); RenderTexture.active = camera.targetTexture;
                pixels.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); pixels.Apply();
                Assert.That(Vector4.Distance(pixels.GetPixel(8, 16), left), Is.LessThan(.02f), "Left pixel: " + pixels.GetPixel(8, 16));
                Assert.That(Vector4.Distance(pixels.GetPixel(24, 16), right), Is.LessThan(.02f), "Right pixel: " + pixels.GetPixel(24, 16));
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
