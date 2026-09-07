using System.Reflection;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using AssetSpriteLoader = MapleClient.GameData.SpriteLoader;

namespace MapleClient.Tests.CharacterRendering
{
    public class CharacterRenderingRegressionTests
    {
        private GameObject character;
        private INxFile previousCharacterFile;
        [SetUp]
        public void SetUp()
        {
            AssetSpriteLoader.ClearCache();
            previousCharacterFile = NXAssetLoader.Instance.GetNxFile("character");
            NXAssetLoader.Instance.RegisterNxFile("character", CreateCharacterFile());
        }
        [TearDown]
        public void TearDown()
        {
            if (character != null) Object.DestroyImmediate(character);
            NXAssetLoader.Instance.RegisterNxFile("character", previousCharacterFile);
            AssetSpriteLoader.ClearCache();
        }
        [TestCase(12f, 20f, -3f, -4f)]
        [TestCase(-5f, -4f, 2f, 3f)]
        public void ShiftedSpritePreservesAnchorsOutsideItsBitmap(float ox, float oy, float sx, float sy)
        {
            var origin = new Vector2(ox, oy); var shift = new Vector2(sx, sy);
            var sprite = AssetSpriteLoader.LoadSpriteWithShift(ImageNode("part", origin), shift, "test/part");
            Assert.That(sprite, Is.Not.Null);
            var anchor = origin - shift;
            Assert.That(sprite.pivot.x, Is.EqualTo(anchor.x).Within(0.001f));
            Assert.That(sprite.pivot.y, Is.EqualTo(8f - anchor.y).Within(0.001f));
            Assert.That(sprite.bounds.min.x, Is.EqualTo(-anchor.x / 100f).Within(0.0001f));
            Assert.That(sprite.bounds.max.y, Is.EqualTo(anchor.y / 100f).Within(0.0001f));
        }
        [Test]
        public void RepeatedFramesReuseSpritesAndOffsetVariantsShareOneTexture()
        {
            var node = ImageNode("face", Vector2.zero);
            var first = AssetSpriteLoader.LoadSpriteWithShift(node, Vector2.zero, "test/face");
            var offset = AssetSpriteLoader.LoadSpriteWithShift(node, new Vector2(1f, -2f), "test/face");
            for (int i = 0; i < 30; i++) Assert.That(AssetSpriteLoader.LoadSpriteWithShift(node, Vector2.zero, "test/face"), Is.SameAs(first));
            Assert.That(offset, Is.Not.SameAs(first));
            Assert.That(offset.texture, Is.SameAs(first.texture));
            Assert.That(offset.pivot, Is.Not.EqualTo(first.pivot));
            var texture = first.texture; AssetSpriteLoader.ClearCache();
            Assert.That(first == null && offset == null && texture == null, Is.True);
        }
        [Test]
        public void FirstMovementLeftFlipsInitialRightFacingCharacter()
        {
            var renderer = CreateRenderer(new Player());
            Assert.That(character.transform.Find("VisualRoot").localScale.x, Is.EqualTo(-1f));
            renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(-1f, 0f));
            Assert.That(character.transform.Find("VisualRoot").localScale.x, Is.EqualTo(1f));
            renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(1f, 0f));
            Assert.That(character.transform.Find("VisualRoot").localScale.x, Is.EqualTo(-1f));
        }
        [Test]
        public void AirInputFromRestUpdatesFacingWithoutHorizontalVelocity()
        {
            var player=new Player { Position=new MapleClient.GameLogic.Vector2(0,2) };
            var renderer=CreateRenderer(player);
            player.MoveLeft(true); player.UpdatePhysics(0.008f,new MapleClient.GameLogic.MapData());
            Assert.That(player.Velocity.X,Is.Zero);
            Assert.That(player.FacingRight,Is.EqualTo(false));
            typeof(MapleCharacterRenderer).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(renderer,null);
            Assert.That(character.transform.Find("VisualRoot").localScale.x,Is.EqualTo(1f));
        }
        [TestCase(PlayerState.Falling, CharacterState.Fall)]
        [TestCase(PlayerState.DoubleJumping, CharacterState.Jump)]
        [TestCase(PlayerState.FlashJumping, CharacterState.Jump)]
        [TestCase(PlayerState.Swimming, CharacterState.Fly)]
        public void AirbornePlayerUsesAirborneAnimation(PlayerState state, CharacterState expected)
        {
            var player = new Player();
            typeof(Player).GetProperty("State").SetValue(player, state);
            var renderer = CreateRenderer(player);
            var actual = typeof(MapleCharacterRenderer).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(renderer);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(character.transform.Find("VisualRoot/Body").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        }
        [Test]
        public void RebindReusesLayersAndDetachesPreviousPlayer()
        {
            var first = new Player(); var second = new Player(); var renderer = CreateRenderer(first);
            int layers = character.GetComponentsInChildren<SpriteRenderer>().Length;
            renderer.Initialize(first, new CharacterDataProvider());
            Assert.That(character.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(layers));
            renderer.Initialize(second, new CharacterDataProvider());
            Assert.That(character.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(layers));
            Assert.That(first.HasViewListeners, Is.False);
            Assert.That(second.HasViewListeners, Is.True);

        }
        [Test]
        public void UnbindHidesPreviousAppearance()
        {
            var player = new Player(); var renderer = CreateRenderer(player);
            renderer.Initialize(null, null);
            Assert.That(player.HasViewListeners, Is.False);
            foreach (var layer in character.GetComponentsInChildren<SpriteRenderer>()) Assert.That(layer.sprite, Is.Null);
        }
        [Test]
        public void VisualAnchorUsesFeetAndAnimationUsesNxDelay()
        {
            var player = new Player(); CreateRenderer(player);
            Assert.That(character.transform.Find("VisualRoot").localPosition.y, Is.EqualTo(-Player.Height / 2f));
            // The fixture's 500 ms delay now belongs to the fixed simulation clock.
            for (int i = 0; i < 62; i++) player.StanceAnimation.Advance(1);
            Assert.That(player.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(496));
            player.StanceAnimation.Advance(1);
            Assert.That(player.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(4));
        }
        private MapleCharacterRenderer CreateRenderer(Player player)
        {
            character = new GameObject("TestCharacter");
            var renderer = character.AddComponent<MapleCharacterRenderer>();
            renderer.Initialize(player, new CharacterDataProvider());
            return renderer;
        }
        private static NxNode ImageNode(string name, Vector2 origin)
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels); texture.Apply();
            var node = new NxNode(name, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            node.AddChild(new NxNode("origin", origin)); return node;
        }
        private static INxFile CreateCharacterFile()
        {
            var root = new NxNode("root"); var body = new NxNode("00002000.img"); root.AddChild(body);
            foreach (var state in new[] { "stand1", "jump", "fly" })
            {
                var animation = new NxNode(state); var frame = new NxNode("0");
                frame.AddChild(new NxNode("delay", 500)); frame.AddChild(ImageNode("body", new Vector2(4, 8))); animation.AddChild(frame); body.AddChild(animation);
            }
            return new TestNxFile(root);
        }
        private sealed class TestNxFile : INxFile
        {
            public bool IsLoaded => true;
            public INxNode Root { get; }
            public TestNxFile(INxNode root) { Root = root; }
            public INxNode GetNode(string path) => Root.GetNode(path);
        }
    }
}
