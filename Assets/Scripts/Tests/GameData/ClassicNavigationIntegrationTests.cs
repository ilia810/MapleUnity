using System.Linq;
using GameData;
using MapleClient.GameData;
using MapleClient.GameLogic;
using NUnit.Framework;

namespace MapleClient.Tests.GameData
{
    public class ClassicNavigationIntegrationTests
    {
        [TestCase(100000000,"Victoria Road","Henesys")]
        [TestCase(100000100,"Victoria Road","Henesys Market")]
        [TestCase(100000102,"Victoria Road","Henesys Department Store")]
        [TestCase(200000000,"Orbis","Orbis")]
        [TestCase(211000000,"El Nath","El Nath")]
        public void NamesResolveAcrossOriginalRegionGroups(int id,string street,string name)
        {
            var map = new NxMapLoader().PreviewMap(id);
            Assert.That(map.Name,Is.EqualTo(name)); Assert.That(map.StreetName,Is.EqualTo(street));
            Assert.That(NXDataManagerSingleton.Instance.DataManager.MapData.GetMapName(id),Is.EqualTo(name));
        }
        [Test]
        public void ProjectionUsesAuthoredOffsetPowerOfTwoAndFeetInDownwardPixels()
        {
            var map = new NxMapLoader().PreviewMap(100000000);
            Assert.That(map.MiniMap.CenterX,Is.EqualTo(1068)); Assert.That(map.MiniMap.CenterY,Is.EqualTo(661));
            Assert.That(map.MiniMap.Scale,Is.EqualTo(16)); Assert.That(map.MiniMap.MapMark,Is.EqualTo("Henesys"));
            Assert.That(map.MiniMap.HasCanvas,Is.True); Assert.That(map.MiniMap.Hidden,Is.False);
            var topLeft = map.MiniMap.ProjectSource(-1068,-661);
            Assert.That(topLeft.X,Is.EqualTo(0)); Assert.That(topLeft.Y,Is.EqualTo(0));
            var feet = map.MiniMap.ProjectFeet(new Vector2(-10.68f,6.61f));
            Assert.That(feet.X,Is.EqualTo(0).Within(.001)); Assert.That(feet.Y,Is.EqualTo(0).Within(.001));
            var moved = map.MiniMap.ProjectFeet(new Vector2(-10.36f,6.45f));
            Assert.That(moved.X,Is.EqualTo(2).Within(.001)); Assert.That(moved.Y,Is.EqualTo(1).Within(.001));
        }
        [Test]
        public void RegionalAtlasWinsOverTheOverviewContainingTheSameTown()
        {
            var atlases = NXDataManagerSingleton.Instance.DataManager.GetNode("map","WorldMap");
            Assert.That(NxWorldMapIndex.FindRegion(atlases,100000000).Name,Is.EqualTo("WorldMap010.img"));
            Assert.That(NxWorldMapIndex.FindRegion(atlases,100000001).Name,Is.EqualTo("WorldMap010.img"));
            Assert.That(NxWorldMapIndex.FindRegion(atlases,-123).Name,Is.EqualTo("WorldMap.img"));
        }
        [Test]
        public void ShopHasNoPreviewAndMustNotBorrowThePreviousMapsImage()
        { Assert.That(new NxMapLoader().PreviewMap(100000102).MiniMap.HasCanvas,Is.False); }
        [Test]
        public void HiddenMinimapMetadataIsPreserved()
        { Assert.That(new NxMapLoader().PreviewMap(200090500).MiniMap.Hidden,Is.True); }
        [Test]
        public void MissingNamesHaveAnExplicitFallback()
        {
            Assert.That(NxMapNames.Name(NXDataManagerSingleton.Instance.DataManager.GetNode("string","Map.img"),-123),Is.EqualTo("Map -123"));
            Assert.That(NxMapNames.Name(null,42),Is.EqualTo("Map 42"));
        }
        [Test]
        public void MinimapPreviewDoesNotReplaceLiveFootholds()
        {
            var footholds = new FootholdService(); var loader = new NxMapLoader("",footholds); loader.GetMap(100000000);
            var before = footholds.GetFootholdsInArea(float.MinValue,float.MinValue,float.MaxValue,float.MaxValue).ToArray();
            loader.PreviewMap(100000102);
            CollectionAssert.AreEqual(before,footholds.GetFootholdsInArea(float.MinValue,float.MinValue,float.MaxValue,float.MaxValue).ToArray());
        }
    }
}
