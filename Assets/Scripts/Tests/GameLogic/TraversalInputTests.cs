using System;
using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class TraversalInputTests
    {
        [Test]
        public void DownAtJumpEdgeSurvivesReleaseBeforeNextTick()
        {
            var player = TraversalTraceTests.CreatePlayer("drop_close");
            var map = TraversalTraceTests.Map("drop_close");
            player.Crouch(true); player.UpdatePhysics(0.008f,map);
            player.Jump(); player.Crouch(false); player.ReleaseJump();
            Assert.That(player.Position.Y, Is.EqualTo(0.3f));
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.Position.Y, Is.LessThan(0.3f));
            Assert.That(player.State, Is.EqualTo(PlayerState.Falling));
            for (int i=0;i<20;i++) player.UpdatePhysics(0.008f,map);
            Assert.That(player.CurrentFootholdId, Is.EqualTo(3), "Catch the floor only 8 pixels below.");
            Assert.That(player.IsGrounded, Is.True);
        }

        [Test]
        public void ClimbCommandsAndStopWaitForSimulationTick()
        {
            var player = TraversalTraceTests.CreatePlayer("climb_up");
            var map = TraversalTraceTests.Map("climb_up");
            var before = player.Position;
            player.ClimbUp(true);
            Assert.That(player.GetCurrentLadder(), Is.Null);
            player.UpdatePhysics(0.004f,map);
            Assert.That(player.Position, Is.EqualTo(before));
            player.UpdatePhysics(0.004f,map);
            Assert.That(player.GetCurrentLadder(), Is.SameAs(map.Ladders[0]));
            Assert.That(player.PreviousPosition.X, Is.EqualTo(player.Position.X), "Source set_x snaps the render history on a grab.");
            Assert.That(player.Velocity, Is.EqualTo(Vector2.Zero), "Entry follows this tick's movement.");
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.Velocity.Y, Is.EqualTo(1.25f));
            player.StopClimbing();
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.State, Is.EqualTo(PlayerState.Falling));
            Assert.That(player.ClimbCooldownMilliseconds, Is.EqualTo(992));
        }

        [Test]
        public void SolverTeleportClearsClimbStanceAndRestoresGravity()
        {
            var movement=new NormalMovement { X=0,Y=-40,State=NormalMovement.Stance.Fall };
            movement.SetTerrain(new NormalTerrain(TraversalTraceTests.Terrain("climb_up")));
            var map=TraversalTraceTests.Map("climb_up");
            movement.Step(false,false,false,false,NormalMovement.WalkForce(100),NormalMovement.JumpForce(120),
                up:true,ladders:map.Ladders);
            Assert.That(movement.IsClimbing,Is.True);
            movement.Teleport(300,-40);
            Assert.That(movement.State,Is.EqualTo(NormalMovement.Stance.Fall));
            movement.Step(false,false,false,false,NormalMovement.WalkForce(100),NormalMovement.JumpForce(120));
            Assert.That(movement.Y,Is.GreaterThan(-40));
            Assert.That(movement.IsClimbing,Is.False);
        }

        [Test]
        public void SpeedStatChangesClimbingOnTheNextTick()
        {
            var player = TraversalTraceTests.CreatePlayer("climb_up");
            var map = TraversalTraceTests.Map("climb_up");
            player.ClimbUp(true); player.UpdatePhysics(0.008f,map);
            player.Speed = 140;
            Assert.That(player.Velocity.Y, Is.Zero);
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.Velocity.Y, Is.EqualTo(1.75f).Within(0.00001));
        }

        [Test]
        public void JumpAloneStaysAttachedThenHeldJumpWithDirectionLetsGoOnce()
        {
            var player = TraversalTraceTests.CreatePlayer("climb_jump_held");
            var map = TraversalTraceTests.Map("climb_jump_held");
            int jumps=0; player.Jumped += () => jumps++;
            player.ClimbUp(true); player.Jump(); player.UpdatePhysics(0.008f,map);
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.State, Is.EqualTo(PlayerState.Climbing));
            Assert.That(jumps, Is.Zero);
            player.MoveRight(true); player.UpdatePhysics(0.008f,map);
            Assert.That(player.State, Is.EqualTo(PlayerState.Jumping));
            Assert.That(player.Velocity.X, Is.EqualTo(1.6f).Within(0.00001));
            Assert.That(jumps, Is.EqualTo(1));
            player.UpdatePhysics(0.08f,map);
            Assert.That(jumps, Is.EqualTo(1));
        }

        [TestCase(10.49,true)]
        [TestCase(10.5,false)]
        [TestCase(-10.49,true)]
        [TestCase(-10.5,false)]
        public void LadderUsesRoundedFeetCoordinatesAtGrabBoundary(double x, bool accepted)
        {
            var ladder = new LadderInfo { X=0,Y1=0,Y2=1 };
            Assert.That(ladder.InRange(x,0,true), Is.EqualTo(accepted));
            Assert.That(ladder.InRange(0,-100,true), Is.False, "Up cannot enter from above the top.");
            Assert.That(ladder.InRange(0,-100,false), Is.True, "Down enters from the top.");
            Assert.That(ladder.InRange(0,0,false), Is.False, "Down cannot enter below the bottom.");
        }

        [Test]
        public void MapResetClearsLadderCooldownAndPendingTraversal()
        {
            var player = TraversalTraceTests.CreatePlayer("climb_up");
            var map = TraversalTraceTests.Map("climb_up");
            player.ClimbUp(true); player.UpdatePhysics(0.008f,map);
            player.MoveLeft(true); player.Jump(); player.UpdatePhysics(0.008f,map);
            Assert.That(player.CanClimb, Is.False);
            player.StartClimbing(map.Ladders[0]);
            player.ResetMovementForMap();
            Assert.That(player.CanClimb, Is.True);
            Assert.That(player.GetCurrentLadder(), Is.Null);
            player.Position = new Vector2(3,0.3f); player.Velocity = Vector2.Zero; player.IsGrounded=true;
            player.UpdatePhysics(0.008f,new MapData());
            Assert.That(player.GetCurrentLadder(), Is.Null);
        }

        [Test]
        public void LadderEntryTakesPriorityOverPortalAtSamePosition()
        {
            var map = WorldMap(1, true);
            map.Portals.Add(new Portal { Id=1,X=0,Y=0,Type=PortalType.Regular,TargetMapId=2 });
            var loader = new FakeMapLoader(); loader.AddMap(1,map); loader.AddMap(2,WorldMap(2,false));
            var input = new FakeInputProvider { IsUpPressed=true };
            var world = new GameWorld(input,loader); world.LoadMap(1);
            world.Player.Position=new Vector2(0,0.3f); world.Player.IsGrounded=true;
            world.ProcessInput();
            Assert.That(world.Player.State, Is.Not.EqualTo(PlayerState.Climbing));
            Assert.That(world.CurrentMapId, Is.EqualTo(1));
            world.UpdatePhysics(0.008f);
            Assert.That(world.Player.State, Is.EqualTo(PlayerState.Climbing));
            Assert.That(world.CurrentMapId, Is.EqualTo(1));
        }

        [Test]
        public void PortalDuringCatchUpUsesDestinationLaddersOnRemainingTicks()
        {
            var first = WorldMap(1,false);
            first.Portals.Add(new Portal { Id=1,X=0,Y=0,Type=PortalType.Regular,TargetMapId=2,TargetPortalName="sp" });
            var loader = new FakeMapLoader(); loader.AddMap(1,first); loader.AddMap(2,WorldMap(2,true));
            var input = new FakeInputProvider { IsUpPressed=true };
            var world = new GameWorld(input,loader); world.LoadMap(1);
            world.Player.Position=new Vector2(0,0.3f); world.Player.IsGrounded=true;
            world.ProcessInput();
            // Refresh held intent after map reset, as the next input poll would.
            world.MapLoaded += _ => world.ProcessInput();
            world.UpdatePhysics(0.024f);
            Assert.That(world.CurrentMapId, Is.EqualTo(2));
            Assert.That(world.Player.GetCurrentLadder(), Is.SameAs(world.CurrentMap.Ladders[0]));
        }

        private static MapData WorldMap(int id, bool ladder)
        {
            var map = new MapData { MapId=id };
            map.Platforms.Add(new Platform { Id=1,X1=-1000,X2=1000,Y1=0,Y2=0 });
            map.Portals.Add(new Portal { Id=0,Name="sp",Type=PortalType.Spawn,X=0,Y=0 });
            if(ladder) map.Ladders.Add(new LadderInfo { Id=1,X=0,Y1=0,Y2=1 });
            return map;
        }
    }
}
