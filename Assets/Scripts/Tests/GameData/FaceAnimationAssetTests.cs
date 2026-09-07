using System.Collections.Generic;
using System.Linq;
using MapleClient.GameData;
using MapleClient.GameLogic.Data;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Skills;
using NUnit.Framework;
using UnityEngine;

namespace MapleClient.Tests.GameData
{
    public class FaceAnimationAssetTests
    {
        [OneTimeSetUp] public void LoadAssets() { var data = global::GameData.NXDataManagerSingleton.Instance.DataManager; }
        public static IEnumerable<CharacterExpression> Expressions => CharacterExpressions.SourceOrder;
        [TestCaseSource(nameof(Expressions))]
        public void SourceFaceFramesKeepTheirDelaysBitmapsAndIndividualBrowOffsets(CharacterExpression expression)
        {
            var loader = NXAssetLoader.Instance; var file = loader.GetNxFile("character"); var provider = new CharacterDataProvider();
            var data = provider.GetFaceAnimation(20000); string name = CharacterExpressions.Name(expression);
            var exp = file.GetNode("Face/00020000.img/" + name); int count = 0;
            if (expression == CharacterExpression.Default) count = 1;
            else while (exp?[count.ToString()] != null) count++;
            Assert.That(data.FrameCount(expression), Is.EqualTo(count));
            if (CharacterExpressions.SourceOrder.Take(9).Contains(expression)) Assert.That(count, Is.GreaterThan(0));
            var shift = new Vector2(14, -36);
            for (int frame = 0; frame < count; frame++)
            {
                var node = expression == CharacterExpression.Default ? exp : exp[frame.ToString()];
                ushort delay = unchecked((ushort)(node?["delay"]?.GetValue<int>() ?? 0));
                Assert.That(data.Delay(expression, frame), Is.EqualTo(delay == 0 ? 2500 : delay));
                var sprite = loader.LoadFaceWithShift(20000, name, shift, out _, frame);
                Assert.That(sprite, Is.Not.Null, name + "/" + frame);
                Assert.That(loader.LoadFaceWithShift(20000, name, shift, out _, frame), Is.SameAs(sprite));
                var face = node["face"]; var pixel = face["origin"].GetValue<Vector2>() + (face["map"]?["brow"]?.GetValue<Vector2>() ?? Vector2.zero);
                var brow = new Vector2((pixel.x - sprite.pivot.x) / 100, (sprite.rect.height - pixel.y - sprite.pivot.y) / 100);
                Assert.That(Vector2.Distance(brow, new Vector2(shift.x / 100, -shift.y / 100)), Is.LessThan(.0001f), name + "/" + frame);
            }
        }
        [Test] public void MissingFaceUsesDefaultFaceForMetadataAndEveryAnimatedBitmap()
        {
            var provider = new CharacterDataProvider(); var data = provider.GetFaceAnimation(99999999);
            Assert.That(data.FaceId, Is.EqualTo(20000)); Assert.That(provider.GetFaceAnimation(99999999), Is.SameAs(data));
            var loader = NXAssetLoader.Instance;
            Assert.That(loader.LoadFaceWithShift(99999999, "blink", Vector2.zero, out _, 1),
                Is.SameAs(loader.LoadFaceWithShift(20000, "blink", Vector2.zero, out _, 1)));
            Assert.That(loader.LoadFaceWithShift(20000, "blink", Vector2.zero, out _, 99), Is.Null);
        }
        [Test] public void EquippedAttackFaceClockReadsBoosterExpirationOnTheSameTickAsTheBody()
        {
            var assets = global::GameData.NXDataManagerSingleton.Instance.DataManager;
            var p = new Player { Position = new MapleClient.GameLogic.Vector2(0, Player.Height / 2), IsGrounded = true };
            p.SetItemData(assets.ItemData); p.Inventory.AddItem(1302000, 1); Assert.That(p.TryEquipItem(1302000, out _), Is.True);
            p.FaceAnimation.SetData(((IFaceDataProvider)assets.CharacterData).GetFaceAnimation(20000));
            var map = new MapleClient.GameLogic.MapData();
            map.Platforms.Add(new MapleClient.GameLogic.Platform { Id = 1, X1 = -1000, X2 = 1000, Y1 = 0, Y2 = 0 });
            Assert.That(p.PlayBasicAttackAnimation(), Is.True);
            p.ApplyStatBuffs(1101004, "Sword Booster", new Dictionary<BuffType, int> { [BuffType.Booster] = -2 }, 24);
            int boosted = SourceStanceAnimation.Timestep(1.7f - p.EffectiveAttackSpeed / 10f);
            p.UpdatePhysics(.016f, map); Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(2 * boosted));
            p.UpdatePhysics(.008f, map);
            int unboosted = SourceStanceAnimation.Timestep(1.7f - p.EffectiveAttackSpeed / 10f);
            Assert.That(unboosted, Is.LessThan(boosted));
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(2 * boosted + unboosted));
        }
        [TestCase(0,2500)][TestCase(-1,65535)][TestCase(65536,2500)][TestCase(32768,32768)]
        public void FaceDelayImportUsesUnsignedSourceStorageAndOnlyZeroGetsDefault(int raw, int expected)
        {
            var loader = NXAssetLoader.Instance; var previous = loader.GetNxFile("character");
            try
            {
                var root = new NxNode("root"); var faces = new NxNode("Face"); var face = new NxNode("00020000.img");
                var exp = new NxNode("default"); exp.AddChild(new NxNode("delay", raw)); face.AddChild(exp); faces.AddChild(face); root.AddChild(faces);
                loader.RegisterNxFile("character", new File(root));
                Assert.That(new CharacterDataProvider().GetFaceAnimation(20000).Delay(CharacterExpression.Default, 0), Is.EqualTo(expected));
            }
            finally { loader.RegisterNxFile("character", previous); }
        }
        private sealed class File : INxFile
        {
            public bool IsLoaded => true; public INxNode Root { get; }
            public File(INxNode root) { Root = root; }
            public INxNode GetNode(string path) => Root.GetNode(path);
        }
    }
}
