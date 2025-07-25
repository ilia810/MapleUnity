using NUnit.Framework;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Tests.Fakes;

namespace MapleClient.GameLogic.Tests
{
    [TestFixture]
    public class GameWorldTests
    {
        [Test]
        public void LoadMap_WithValidMapId_SetsCurrentMap()
        {
            // Arrange
            var fakeMapLoader = new FakeMapLoader();
            var testMapData = new MapData 
            { 
                MapId = 100000000,
                Name = "Test Map",
                Width = 1000,
                Height = 600
            };
            fakeMapLoader.AddMap(100000000, testMapData);
            
            var gameWorld = new GameWorld(fakeMapLoader);
            
            // Act
            gameWorld.LoadMap(100000000);
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(100000000));
            Assert.That(gameWorld.CurrentMap, Is.Not.Null);
            Assert.That(gameWorld.CurrentMap.Name, Is.EqualTo("Test Map"));
        }

        [Test]
        public void LoadMap_WithInvalidMapId_DoesNotChangeCurrentMap()
        {
            // Arrange
            var fakeMapLoader = new FakeMapLoader();
            var initialMap = new MapData { MapId = 100000000, Name = "Initial Map" };
            fakeMapLoader.AddMap(100000000, initialMap);
            
            var gameWorld = new GameWorld(fakeMapLoader);
            gameWorld.LoadMap(100000000);
            
            // Act
            gameWorld.LoadMap(999999999); // Non-existent map
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(100000000));
            Assert.That(gameWorld.CurrentMap.Name, Is.EqualTo("Initial Map"));
        }

        [Test]
        public void LoadMap_PopulatesMapPlatforms()
        {
            // Arrange
            var fakeMapLoader = new FakeMapLoader();
            var testMap = new MapData 
            { 
                MapId = 100000000,
                Name = "Platform Test Map"
            };
            testMap.Platforms.Add(new Platform { Id = 1, X1 = 0, Y1 = 100, X2 = 200, Y2 = 100, Type = PlatformType.Normal });
            testMap.Platforms.Add(new Platform { Id = 2, X1 = 300, Y1 = 150, X2 = 500, Y2 = 150, Type = PlatformType.Normal });
            fakeMapLoader.AddMap(100000000, testMap);
            
            var gameWorld = new GameWorld(fakeMapLoader);
            
            // Act
            gameWorld.LoadMap(100000000);
            
            // Assert
            Assert.That(gameWorld.CurrentMap.Platforms.Count, Is.EqualTo(2));
            Assert.That(gameWorld.CurrentMap.Platforms[0].X1, Is.EqualTo(0));
            Assert.That(gameWorld.CurrentMap.Platforms[1].X1, Is.EqualTo(300));
        }

        [Test]
        public void LoadMap_RaisesMapLoadedEvent()
        {
            // Arrange
            var fakeMapLoader = new FakeMapLoader();
            var testMap = new MapData { MapId = 100000000, Name = "Event Test Map" };
            fakeMapLoader.AddMap(100000000, testMap);
            
            var gameWorld = new GameWorld(fakeMapLoader);
            MapData loadedMap = null;
            gameWorld.MapLoaded += (map) => loadedMap = map;
            
            // Act
            gameWorld.LoadMap(100000000);
            
            // Assert
            Assert.That(loadedMap, Is.Not.Null);
            Assert.That(loadedMap.MapId, Is.EqualTo(100000000));
            Assert.That(loadedMap.Name, Is.EqualTo("Event Test Map"));
        }
    }
}