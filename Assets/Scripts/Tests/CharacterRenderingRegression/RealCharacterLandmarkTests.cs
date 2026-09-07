using System.IO;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Core;
using MapleClient.GameView;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using NxSpriteLoader = MapleClient.GameData.SpriteLoader;

namespace MapleClient.Tests.CharacterRendering
{
    public class RealCharacterLandmarkTests
    {
        private INxFile previous;
        private RealNxFile file;
        private GameObject character;
        [SetUp]
        public void SetUp()
        {
            string path = Path.Combine(new global::GameData.NXDataManager().DataPath, "Character.nx");
            if (!File.Exists(path)) Assert.Ignore("Real Character.nx is required for source asset parity.");
            NxSpriteLoader.ClearCache();
            previous = NXAssetLoader.Instance.GetNxFile("character");
            file = new RealNxFile(path);
            NXAssetLoader.Instance.RegisterNxFile("character", file);
        }
        [TearDown]
        public void TearDown()
        {
            if (character != null)
            {
                character.GetComponent<MapleCharacterRenderer>().Initialize(null, null);
                Object.DestroyImmediate(character);
            }
            if (file != null)
            {
                NXAssetLoader.Instance.RegisterNxFile("character", previous);
                NxSpriteLoader.ClearCache(); file.Dispose(); file = null;
            }
        }
        [TestCase(PlayerState.Standing,"stand1",false,-8f,-52f)]
        [TestCase(PlayerState.Standing,"stand1",true,-8f,-52f)]
        [TestCase(PlayerState.Walking,"walk1",false,-8f,-52f)]
        [TestCase(PlayerState.Walking,"walk1",true,-8f,-52f)]
        [TestCase(PlayerState.Jumping,"jump",false,-8f,-50f)]
        [TestCase(PlayerState.Jumping,"jump",true,-8f,-50f)]
        [TestCase(PlayerState.Crouching,"prone",false,-27f,-24f)]
        [TestCase(PlayerState.Crouching,"prone",true,-27f,-24f)]
        public void AuthoredNeckAndBrowLandmarksMeetInWorldSpace(PlayerState state,string animation,
            bool right,float browX,float browY)
        {
            Create(state,right);
            var body = file.GetNode("00002000.img/"+animation+"/0/body");
            var head = file.GetNode("00012000.img/"+animation+"/0/head");
            var face = file.GetNode("Face/00020000.img/default/face");
            AssertPoint(Landmark("Body",body,"neck"),Landmark("Head",head,"neck"),"neck");
            var brow = Landmark("Head",head,"brow");
            AssertPoint(brow,Landmark("Face",face,"brow"),"face brow must meet head brow");
            var feet = character.transform.Find("VisualRoot");
            AssertPoint(brow,feet.TransformPoint(new Vector3(browX/100f,-browY/100f,0)),"source brow");
            foreach (string layer in new[] {"Hair","HairOverHead"})
            {
                string nodeName = layer == "Hair" ? "hair" : "hairOverHead";
                var hair = file.GetNode("Hair/00030000.img/"+animation+"/0/"+nodeName);
                AssertPoint(brow,Landmark(layer,hair,"brow"),nodeName+" brow");
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void StandingFaceBitmapHasSourceTopLeftAndDefaultPantsFollowNavel(bool right)
        {
            Create(PlayerState.Standing,right);
            var sprite = Layer("Face").sprite;
            Assert.That(sprite.bounds.min.x,Is.EqualTo(-0.20f).Within(0.0001f));
            Assert.That(sprite.bounds.max.y,Is.EqualTo(0.48f).Within(0.0001f));
            var pants = Layer("DefaultBottom");
            Assert.That(pants.sprite,Is.Not.Null);
            Assert.That(pants.sprite.name,Does.Contain("Pants/01060026.img/stand1/0/pants"));
            Assert.That(pants.sprite.bounds.min.x,Is.EqualTo(-0.11f).Within(0.0001f));
            Assert.That(pants.sprite.bounds.max.y,Is.EqualTo(0.20f).Within(0.0001f));
            AssertPoint(Landmark("Body",file.GetNode("00002000.img/stand1/0/body"),"navel"),
                Landmark("DefaultBottom",file.GetNode("Pants/01060026.img/stand1/0/pants"),"navel"),"pants navel");
            Assert.That(Layer("HairShade").sprite,Is.Not.Null,"Nested skin shade layer must load.");
            Assert.That(Layer("HairShade").sprite.bounds.min.x,Is.EqualTo(-0.21f).Within(0.0001f));
            Assert.That(Layer("HairShade").sprite.bounds.max.y,Is.EqualTo(0.60f).Within(0.0001f));
        }
        [TestCase(false, true)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(true, false)]
        public void ClimbingUsesAlignedBackHairWithoutForwardFace(bool right, bool isLadder)
        {
            Create(PlayerState.Climbing,right,isLadder);
            string stance=isLadder ? "ladder" : "rope";
            Assert.That(Layer("Body").sprite.name,Does.Contain("/"+stance+"/"));
            Assert.That(Layer("Face").sprite,Is.Null);
            Assert.That(Layer("HairOverHead").sprite,Is.Null);
            Assert.That(Layer("HairShade").sprite,Is.Null);
            Assert.That(Layer("Hair").sprite.name,Does.EndWith("/backHair"));
            AssertPoint(Landmark("Head",file.GetNode("00012000.img/"+stance+"/0/head"),"brow"),
                Landmark("Hair",file.GetNode("Hair/00030000.img/"+stance+"/0/backHair"),"brow"),"back hair brow");
        }
        [Test]
        public void JumpPreservesBothHandsAndUsesAuthoredZLayers()
        {
            Create(PlayerState.Jumping,false);
            Assert.That(Layer("Arm").sprite,Is.Null);
            Assert.That(Layer("ArmOverHair").sprite,Is.Not.Null);
            Assert.That(Layer("ArmOverHair").sortingOrder,Is.GreaterThan(Layer("HairOverHead").sortingOrder));
            Assert.That(Layer("Hand").sprite.name,Does.EndWith("/lHand"));
            Assert.That(Layer("HandOverHair").sprite.name,Does.EndWith("/rHand"));
            Assert.That(Layer("HandOverHair").sortingOrder,Is.GreaterThan(Layer("HairOverHead").sortingOrder));
            Assert.That(Layer("Hand").sprite.bounds.min.x,Is.EqualTo(-0.13f).Within(0.0001f));
            Assert.That(Layer("Hand").sprite.bounds.max.y,Is.EqualTo(0.24f).Within(0.0001f));
        }
        private void Create(PlayerState state,bool right,bool isLadder=true)
        {
            var player = new Player();
            if(state==PlayerState.Climbing)
            {
                var ladder=new LadderInfo { X=0,Y1=0,Y2=1,IsLadder=isLadder };
                var map=new MapleClient.GameLogic.MapData(); map.Ladders.Add(ladder);
                player.Position=new MapleClient.GameLogic.Vector2(0,0.8f);
                player.ClimbUp(true); player.UpdatePhysics(0.008f,map);
                Assert.That(player.GetCurrentLadder(),Is.SameAs(ladder));
            }
            else typeof(Player).GetProperty("State").SetValue(player,state);
            character = new GameObject("RealNxCharacter");
            var renderer = character.AddComponent<MapleCharacterRenderer>();
            renderer.Initialize(player,new CharacterDataProvider());
            renderer.OnVelocityChanged(new MapleClient.GameLogic.Vector2(right?1:-1,0));
            Assert.That(character.transform.localScale,Is.EqualTo(Vector3.one),"Facing must not mirror UI or physics root.");
        }
        private SpriteRenderer Layer(string name) => character.GetComponentsInChildren<SpriteRenderer>()
            .Single(layer => layer.gameObject.name == name);
        private Vector3 Landmark(string layerName,INxNode node,string landmark)
        {
            var layer = Layer(layerName);
            Assert.That(layer.sprite,Is.Not.Null,layerName);
            Assert.That(node,Is.Not.Null,"Source part "+layerName);
            Vector2 pixel = node["origin"].GetValue<Vector2>()+node["map"][landmark].GetValue<Vector2>();
            var sprite = layer.sprite;
            var local = new Vector3((pixel.x-sprite.pivot.x)/sprite.pixelsPerUnit,
                (sprite.rect.height-pixel.y-sprite.pivot.y)/sprite.pixelsPerUnit,0);
            return layer.transform.TransformPoint(local);
        }
        private static void AssertPoint(Vector3 actual,Vector3 expected,string reason)
        {
            Assert.That(Vector3.Distance(actual,expected),Is.LessThan(0.0001f),reason);
        }
    }
}
