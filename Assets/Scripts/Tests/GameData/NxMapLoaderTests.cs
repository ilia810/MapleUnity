using NUnit.Framework;
using MapleClient.GameData;
using MapleClient.GameLogic;

namespace MapleClient.GameData.Tests
{
    [TestFixture]
    public class NxMapLoaderTests
    {
        private NxMapLoader mapLoader;

        [SetUp]
        public void Setup()
        {
            // This will use MockNxFile since no real NX files exist
            mapLoader = new NxMapLoader();
        }

        [Test]
        public void GetMap_WithValidMapId_ReturnsMapData()
        {
            // Act
            var mapData = mapLoader.GetMap(100000000);

            // Assert
            Assert.That(mapData, Is.Not.Null);
            Assert.That(mapData.MapId, Is.EqualTo(100000000));
            Assert.That(mapData.Name, Is.EqualTo("Henesys"));
        }

        [Test]
        public void GetMap_LoadsPlatforms()
        {
            // Act
            var mapData = mapLoader.GetMap(100000000);

            // Assert
            Assert.That(mapData.Platforms.Count, Is.GreaterThan(0));
            var mainPlatform = mapData.Platforms[0];
            Assert.That(mainPlatform.X1, Is.EqualTo(-1500));
            Assert.That(mainPlatform.X2, Is.EqualTo(1500));
            Assert.That(mainPlatform.Y1, Is.EqualTo(0));
            Assert.That(mainPlatform.Y2, Is.EqualTo(0));
        }

        [Test]
        public void GetMap_LoadsPortals()
        {
            // Act
            var mapData = mapLoader.GetMap(100000000);

            // Assert
            Assert.That(mapData.Portals.Count, Is.GreaterThan(0));
            var spawnPortal = mapData.Portals.Find(p => p.Type == PortalType.Spawn);
            Assert.That(spawnPortal, Is.Not.Null);
            Assert.That(spawnPortal.Name, Is.EqualTo("sp"));
        }

        [Test]
        public void GetMap_WithInvalidMapId_ReturnsNull()
        {
            // Act
            var mapData = mapLoader.GetMap(999999999);

            // Assert
            Assert.That(mapData, Is.Null);
        }

        [Test]
        public void GetMap_LoadsBgmId()
        {
            // Act
            var mapData = mapLoader.GetMap(100000000);

            // Assert
            Assert.That(mapData.BgmId, Is.EqualTo("Bgm00/GoPicnic"));
        }
    }
}