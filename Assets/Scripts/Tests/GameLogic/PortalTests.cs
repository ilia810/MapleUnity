using NUnit.Framework;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using System.Collections.Generic;
using PortalType = MapleClient.GameLogic.PortalType;

namespace MapleClient.Tests.GameLogic
{
    public class PortalTests
    {
        private GameWorld gameWorld;
        private TestMapLoader mapLoader;
        private TestInputProvider inputProvider;

        [SetUp]
        public void Setup()
        {
            mapLoader = new TestMapLoader();
            inputProvider = new TestInputProvider();
            gameWorld = new GameWorld(inputProvider, mapLoader);
        }

        [Test]
        public void Portal_PlayerEntersPortal_ChangesMap()
        {
            // Arrange
            mapLoader.AddTestMap(100, CreateMapWithPortal(100, 200)); // Map 100 has portal to map 200
            mapLoader.AddTestMap(200, CreateSimpleMap(200));
            
            gameWorld.LoadMap(100);
            var player = gameWorld.Player;
            player.Position = new Vector2(100, 100); // Position at portal
            
            // Act
            inputProvider.SetUpPressed(true);
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(200));
        }

        [Test]
        public void Portal_PlayerNotAtPortal_DoesNotChangeMap()
        {
            // Arrange
            mapLoader.AddTestMap(100, CreateMapWithPortal(100, 200));
            mapLoader.AddTestMap(200, CreateSimpleMap(200));
            
            gameWorld.LoadMap(100);
            var player = gameWorld.Player;
            player.Position = new Vector2(500, 100); // Far from portal
            
            // Act
            inputProvider.SetUpPressed(true);
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(100));
        }

        [Test]
        public void Portal_SpawnPortal_SetsPlayerPosition()
        {
            // Arrange
            var map = CreateSimpleMap(100);
            map.Portals.Add(new Portal
            {
                Id = 1,
                Name = "spawn",
                X = 250,
                Y = 150,
                Type = PortalType.Spawn
            });
            mapLoader.AddTestMap(100, map);
            
            // Act
            gameWorld.LoadMap(100);
            
            // Assert
            Assert.That(gameWorld.Player.Position.X, Is.EqualTo(250));
            Assert.That(gameWorld.Player.Position.Y, Is.EqualTo(150));
        }

        [Test]
        public void Portal_MapChange_RaisesMapLoadedEvent()
        {
            // Arrange
            mapLoader.AddTestMap(100, CreateMapWithPortal(100, 200));
            mapLoader.AddTestMap(200, CreateSimpleMap(200));
            
            gameWorld.LoadMap(100);
            gameWorld.Player.Position = new Vector2(100, 100);
            
            bool eventRaised = false;
            MapData loadedMap = null;
            gameWorld.MapLoaded += (map) => 
            {
                eventRaised = true;
                loadedMap = map;
            };
            
            // Act
            inputProvider.SetUpPressed(true);
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(eventRaised, Is.True);
            Assert.That(loadedMap.MapId, Is.EqualTo(200));
        }

        [Test]
        public void Portal_InvalidTargetMap_DoesNotChangeMap()
        {
            // Arrange
            mapLoader.AddTestMap(100, CreateMapWithPortal(100, 999)); // Portal to non-existent map
            
            gameWorld.LoadMap(100);
            gameWorld.Player.Position = new Vector2(100, 100);
            
            // Act
            inputProvider.SetUpPressed(true);
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(100));
        }

        [Test]
        public void Portal_HiddenPortal_DoesNotActivate()
        {
            // Arrange
            var map = CreateSimpleMap(100);
            map.Portals.Add(new Portal
            {
                Id = 1,
                Name = "hidden",
                X = 100,
                Y = 100,
                Type = PortalType.Hidden,
                TargetMapId = 200
            });
            mapLoader.AddTestMap(100, map);
            mapLoader.AddTestMap(200, CreateSimpleMap(200));
            
            gameWorld.LoadMap(100);
            gameWorld.Player.Position = new Vector2(100, 100);
            
            // Act
            inputProvider.SetUpPressed(true);
            gameWorld.Update(0.1f);
            
            // Assert
            Assert.That(gameWorld.CurrentMapId, Is.EqualTo(100));
        }

        private MapData CreateSimpleMap(int mapId)
        {
            return new MapData
            {
                MapId = mapId,
                Name = $"Map {mapId}",
                Width = 1000,
                Height = 1000,
                Platforms = new List<Platform>
                {
                    new Platform { Id = 1, X1 = -500, Y1 = 0, X2 = 500, Y2 = 0, Type = PlatformType.Normal }
                }
            };
        }

        private MapData CreateMapWithPortal(int mapId, int targetMapId)
        {
            var map = CreateSimpleMap(mapId);
            map.Portals.Add(new Portal
            {
                Id = 1,
                Name = "portal1",
                X = 100,
                Y = 100,
                Type = PortalType.Regular,
                TargetMapId = targetMapId
            });
            return map;
        }

        private class TestMapLoader : IMapLoader
        {
            private Dictionary<int, MapData> maps = new Dictionary<int, MapData>();

            public void AddTestMap(int mapId, MapData mapData)
            {
                maps[mapId] = mapData;
            }

            public MapData GetMap(int mapId)
            {
                return maps.TryGetValue(mapId, out var map) ? map : null;
            }
        }

        private class TestInputProvider : IInputProvider
        {
            private bool upPressed;

            public bool IsLeftPressed => false;
            public bool IsRightPressed => false;
            public bool IsJumpPressed => false;
            public bool IsAttackPressed => false;
            public bool IsUpPressed => upPressed;
            public bool IsDownPressed => false;

            public void SetUpPressed(bool pressed)
            {
                upPressed = pressed;
            }
        }
    }
}