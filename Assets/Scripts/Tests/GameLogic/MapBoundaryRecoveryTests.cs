using System;
using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class MapBoundaryRecoveryTests
    {
        private static MapData GapMap(int id=1, float ground=200)
        {
            var map=new MapData { MapId=id };
            map.Platforms.Add(new Platform { Id=1,X1=-200,X2=-50,Y1=ground,Y2=ground,HasSourceTopology=true,Layer=2 });
            map.Platforms.Add(new Platform { Id=2,X1=50,X2=200,Y1=ground,Y2=ground,HasSourceTopology=true,Layer=4 });
            map.Portals.Add(new Portal { Id=0,Name="sp",Type=PortalType.Spawn,X=-100,Y=(int)ground });
            return map;
        }
        private static GameWorld World(MapData map, FakeInputProvider input=null)
        {
            var loader=new FakeMapLoader(); loader.AddMap(map.MapId,map);
            var world=new GameWorld(input??new FakeInputProvider(),loader); world.LoadMap(map.MapId);
            return world;
        }
        [Test]
        public void FallingIntoGapReturnsToActualSpawnAndClearsInterpolation()
        {
            var map=GapMap();
            var world=World(map);
            int recovered=0;
            world.PlayerRecovered += () => {
                recovered++;
                Assert.That(world.Player.PreviousPosition,Is.EqualTo(world.Player.Position));
                Assert.That(world.Player.Position.X,Is.EqualTo(-1));
                Assert.That(world.Player.Position.Y,Is.EqualTo(-1.69f).Within(0.00001));
                Assert.That(world.Player.IsGrounded,Is.False,"Recovery spawns above a real floor; it does not fake contact.");
            };
            world.Player.Position=new Vector2(0,-1.5f); world.Player.Velocity=Vector2.Zero;
            for(int tick=0;tick<160;tick++) world.UpdatePhysics(0.008f);
            Assert.That(recovered,Is.EqualTo(1));
            Assert.That(world.Player.IsGrounded,Is.True);
            Assert.That(world.Player.CurrentFootholdId,Is.EqualTo(1));
            Assert.That(world.Player.CurrentFootholdLayer,Is.EqualTo(2));
        }
        [TestCase(float.NaN,0)]
        [TestCase(float.PositiveInfinity,0)]
        [TestCase(0,float.NaN)]
        [TestCase(0,float.NegativeInfinity)]
        public void InvalidExternalPositionRecoversWithoutPublishingInvalidPhysics(float x,float y)
        {
            var world=World(GapMap());
            world.Player.Position=new Vector2(x,y);
            world.UpdatePhysics(0.008f);
            Assert.That(world.Player.Position.X,Is.EqualTo(-1));
            Assert.That(world.Player.Position.Y,Is.EqualTo(-1.69f).Within(0.00001));
        }
        [Test]
        public void EmptyMapDoesNotReusePreviousTerrainOrInventRecoveryFloor()
        {
            var loader=new FakeMapLoader(); loader.AddMap(1,GapMap()); loader.AddMap(2,new MapData {MapId=2});
            var world=new GameWorld(new FakeInputProvider(),loader); world.LoadMap(1); world.LoadMap(2);
            int recovered=0; world.PlayerRecovered += () => recovered++;
            world.Player.Position=new Vector2(0,-20);
            world.UpdatePhysics(0.016f);
            Assert.That(recovered,Is.Zero);
            Assert.That(world.Player.IsGrounded,Is.False);
            Assert.That(world.Player.Position.Y,Is.LessThan(-20));
        }
        [Test]
        public void MissingSpawnColumnUsesClosestRealSurface()
        {
            var map=GapMap(); map.Portals[0].X=0;
            var world=World(map);
            Assert.That(world.Player.Position.X,Is.EqualTo(-0.5f));
            Assert.That(world.Player.Position.Y,Is.EqualTo(-1.69f).Within(0.00001));
            for(int i=0;i<10;i++) world.UpdatePhysics(0.008f);
            Assert.That(world.Player.CurrentFootholdId,Is.EqualTo(1));
        }
        [Test]
        public void PortalSpawnSkipsSurfaceAboveItsAuthoredY()
        {
            var map=new MapData {MapId=1};
            map.Platforms.Add(new Platform {Id=1,X1=-200,X2=200,Y1=0,Y2=0});
            map.Platforms.Add(new Platform {Id=2,X1=-200,X2=200,Y1=8,Y2=8});
            map.Portals.Add(new Portal {Id=0,Type=PortalType.Spawn,X=0,Y=1});
            var world=World(map);
            Assert.That(world.Player.Position.Y,Is.EqualTo(0.23f).Within(0.00001),"Source spawn query must not use the legacy 10-pixel upward tolerance.");
        }
        [Test]
        public void DestinationSpawnIsFinalBeforeMapLoadedObserversRun()
        {
            var first=GapMap(1);
            var second=GapMap(2,-300);
            first.Portals.Add(new Portal {Id=5,Name="door",Type=PortalType.Regular,X=-100,Y=200,TargetMapId=2,TargetPortalName="back"});
            second.Portals.Add(new Portal {Id=7,Name="back",Type=PortalType.Regular,X=100,Y=-300,TargetMapId=1});
            var loader=new FakeMapLoader(); loader.AddMap(1,first); loader.AddMap(2,second);
            var input=new FakeInputProvider();
            var world=new GameWorld(input,loader); world.LoadMap(1);
            bool observed=false;
            world.MapLoaded += map => {
                observed=true;
                Assert.That(map.MapId,Is.EqualTo(2));
                Assert.That(world.Player.Position.X,Is.EqualTo(1));
                Assert.That(world.Player.Position.Y,Is.EqualTo(3.31f).Within(0.00001));
                Assert.That(world.Player.PreviousPosition,Is.EqualTo(world.Player.Position));
            };
            input.IsUpPressed=true; world.Update(0.008f);
            Assert.That(observed,Is.True);
            for(int i=0;i<20;i++) world.Update(0.008f);
            Assert.That(world.CurrentMapId,Is.EqualTo(2),"Holding Up must not bounce back through the destination.");
        }
        [Test]
        public void IntramapPortalPreservesMonstersDropsAndTerrainRegistration()
        {
            var map=GapMap();
            map.Portals.Add(new Portal { Id=1, Name="from", Type=PortalType.Regular, X=-100, Y=200, TargetMapId=1, TargetPortalName="to" });
            map.Portals.Add(new Portal { Id=2, Name="to", Type=PortalType.Regular, X=100, Y=200, TargetMapId=1, TargetPortalName="from" });
            var input=new FakeInputProvider();
            var world=World(map,input);
            world.SpawnMonsterForTesting(100100,new Vector2(1,-1.7f));
            var monster=world.Monsters[0];
            world.AddDroppedItem(2000000,1,new Vector2(10,10));
            var drop=world.DroppedItems[0];
            int loaded=0, teleported=0;
            world.MapLoaded += _ => loaded++;
            world.PlayerTeleported += () => teleported++;
            input.IsUpPressed=true; world.Update(0.008f);
            Assert.That(loaded,Is.Zero);
            Assert.That(teleported,Is.EqualTo(1));
            Assert.That(world.Player.Position.X,Is.EqualTo(1));
            Assert.That(world.Monsters[0],Is.SameAs(monster));
            Assert.That(world.DroppedItems[0],Is.SameAs(drop));
            for(int i=0;i<20;i++) world.Update(0.008f);
            Assert.That(teleported,Is.EqualTo(1));
            Assert.That(world.Player.CurrentFootholdId,Is.EqualTo(2));
        }

        [TestCase(-200, -175)]
        [TestCase(200, 175)]
        public void SpawnAtOuterEdgeIsKeptInsideMovementWalls(int portalX, int expectedX)
        {
            var map=GapMap(); map.Portals[0].X=portalX;
            var world=World(map);
            Assert.That(world.Player.Position.X,Is.EqualTo(expectedX/100f));
        }
        [TestCase(float.NaN, 0)]
        [TestCase(0, float.PositiveInfinity)]
        public void InvalidExternalVelocityRecovers(float x, float y)
        {
            var world=World(GapMap());
            world.Player.Velocity=new Vector2(x,y);
            world.UpdatePhysics(0.008f);
            Assert.That(world.Player.Velocity,Is.EqualTo(Vector2.Zero));
            Assert.That(world.Player.Position.Y,Is.EqualTo(-1.69f).Within(0.00001));
        }
        [Test]
        public void NarrowMapHasOrderedMovementBounds()
        {
            var terrain=new NormalTerrain(new[] { new Foothold(1,0,200,40,200) });
            Assert.That(terrain.LeftWall,Is.EqualTo(20));
            Assert.That(terrain.RightWall,Is.EqualTo(20));
            Assert.That(terrain.TryFindSpawn(20,200,out var position),Is.True);
            Assert.That(position.Y,Is.EqualTo(-1.69f).Within(0.00001));
        }
    }
}
