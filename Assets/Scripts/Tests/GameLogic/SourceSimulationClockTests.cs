using System;
using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class SourceSimulationClockTests
    {
        private static Player Grounded()
        {
            var service = new FootholdService();
            service.LoadFootholds(NormalMovementTraceTests.Terrain("walk_right"));
            return new Player(service) { Position=new Vector2(0,0.3f), IsGrounded=true };
        }

        [Test]
        public void FourTicksKeepTheFractionalRemainder()
        {
            var clock = new PhysicsUpdateManager();
            clock.Update(0.034f,new MapData());
            Assert.That(clock.TotalPhysicsSteps,Is.EqualTo(4));
            Assert.That(clock.Accumulator,Is.EqualTo(0.002f).Within(0.0000001));
            Assert.That(clock.GetInterpolationFactor(),Is.EqualTo(0.25f).Within(0.00001));
            clock.Update(0.006f,new MapData());
            Assert.That(clock.TotalPhysicsSteps,Is.EqualTo(5));
            Assert.That(clock.Accumulator,Is.EqualTo(0).Within(0.0000001));
        }

        [Test]
        public void JumpEdgeIsConsumedOnceOnATickEvenAfterKeyRelease()
        {
            var player = Grounded();
            var clock = new PhysicsUpdateManager(); clock.RegisterPhysicsObject(player);
            int jumps=0;
            player.Jumped += () => {
                jumps++;
                Assert.That(player.Position.Y,Is.GreaterThan(0.3f),"Publish movement before jump observers run.");
            };
            player.MoveRight(true); player.Jump(); player.Jump(); player.ReleaseJump();
            Assert.That(player.Position,Is.EqualTo(new Vector2(0,0.3f)));
            Assert.That(player.Velocity,Is.EqualTo(Vector2.Zero));
            Assert.That(player.State,Is.EqualTo(PlayerState.Standing));
            clock.Update(0.004f,new MapData());
            Assert.That(jumps,Is.Zero);
            clock.Update(0.036f,new MapData());
            Assert.That(jumps,Is.EqualTo(1));
            Assert.That(clock.TotalPhysicsSteps,Is.EqualTo(5));
            // Jump 120: launch -5.2, then four gravity increments of +0.14.
            Assert.That(player.Velocity.Y,Is.EqualTo(5.8f).Within(0.00001));
        }

        [Test]
        public void CrouchDoesNotMutateStateBetweenTicks()
        {
            var player = Grounded();
            player.Crouch(true);
            Assert.That(player.State,Is.EqualTo(PlayerState.Standing));
            player.UpdatePhysics(0.008f,new MapData());
            Assert.That(player.State,Is.EqualTo(PlayerState.Crouching));
        }

        [Test]
        public void TeleportClearsPendingJumpAndInterpolationHistory()
        {
            var player = Grounded();
            player.Jump();
            player.Position = new Vector2(2,0.3f);
            Assert.That(player.PreviousPosition,Is.EqualTo(player.Position));
            player.UpdatePhysics(0.008f,new MapData());
            Assert.That(player.Velocity,Is.EqualTo(Vector2.Zero));
            Assert.That(player.IsJumping,Is.False);
        }

        [Test]
        public void StationaryTickAdvancesInterpolationHistory()
        {
            var player = Grounded();
            player.MoveRight(true);
            for (int i=0;i<20;i++) player.UpdatePhysics(0.008f,new MapData());
            player.MoveRight(false);
            for (int i=0;i<40;i++) player.UpdatePhysics(0.008f,new MapData());
            Assert.That(player.Velocity.X,Is.Zero);
            Assert.That(player.PreviousPosition,Is.EqualTo(player.Position),
                "An unchanged final position must not replay the last movement every render frame.");
        }

        [Test]
        public void MovementStatsAreReadOnTheFollowingTick()
        {
            var player = Grounded();
            player.MoveRight(true);
            player.UpdatePhysics(0.008f,new MapData()); // Source STAND -> WALK.
            player.Speed=140;
            player.UpdatePhysics(0.008f,new MapData());
            Assert.That(NormalMovement.ToTickSpeed(player.Velocity.X),Is.EqualTo(0.204).Within(0.000001));
            player.JumpPower=100; player.Jump();
            player.UpdatePhysics(0.008f,new MapData());
            Assert.That(player.Velocity.Y,Is.EqualTo(5.625).Within(0.000001));
        }

        [Test]
        public void SingleFloorAtPositiveYRemainsInsideMapBounds()
        {
            var service = new FootholdService();
            service.LoadFootholds(new List<Foothold> { new Foothold(7,-100,200,100,200) });
            var player = new Player(service) { Position = new Vector2(0,-1.2f) };
            var map = new MapData();
            for(int i=0;i<150;i++) player.UpdatePhysics(0.008f,map);
            Assert.That(player.Position.Y,Is.EqualTo(-1.7f).Within(0.00001));
            Assert.That(player.IsGrounded,Is.True);
            Assert.That(player.CurrentFootholdId,Is.EqualTo(7));
        }

        [Test]
        public void AirborneFacingFollowsInputWhileMomentumStillPointsTheOtherWay()
        {
            var player = Grounded();
            var map = new MapData();
            player.MoveRight(true);
            for(int i=0;i<20;i++) player.UpdatePhysics(0.008f,map);
            player.Jump();
            player.UpdatePhysics(0.008f,map);
            player.UpdatePhysics(0.008f,map);
            player.MoveRight(false); player.MoveLeft(true);
            player.UpdatePhysics(0.008f,map);
            Assert.That(player.Velocity.X,Is.GreaterThan(0));
            Assert.That(player.FacingRight,Is.EqualTo(false));
        }

        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(34)]
        [TestCase(67)]
        public void FrameBatchesProduceIdenticalCompletedTicks(int millisecondsPerFrame)
        {
            var expected = Replay(8);
            var actual = Replay(millisecondsPerFrame);
            Assert.That(actual.Count,Is.EqualTo(160));
            for (int i=0;i<actual.Count;i++)
                Assert.That(actual[i],Is.EqualTo(expected[i]),"Frame size "+millisecondsPerFrame+" tick "+(i+1));
        }

        private static List<Vector2> Replay(int frameMilliseconds)
        {
            var driver = new InputReplay(Grounded());
            var clock = new PhysicsUpdateManager(); clock.RegisterPhysicsObject(driver);
            int elapsed=0;
            while (elapsed<1280)
            {
                int frame=Math.Min(frameMilliseconds,1280-elapsed);
                clock.Update(frame/1000f,new MapData());
                elapsed+=frame;
            }
            Assert.That(driver.Jumps,Is.EqualTo(1));
            Assert.That(driver.Landings,Is.EqualTo(1));
            return driver.Positions;
        }

        private sealed class InputReplay : IPhysicsObject
        {
            private readonly Player player;
            public readonly List<Vector2> Positions = new List<Vector2>();
            public int Jumps, Landings;
            public InputReplay(Player player)
            {
                this.player=player;
                player.Jumped += () => Jumps++;
                player.Landed += () => Landings++;
            }
            public int PhysicsId => 1;
            public Vector2 Position { get => player.Position; set => player.Position=value; }
            public Vector2 Velocity { get => player.Velocity; set => player.Velocity=value; }
            public bool UseGravity => true;
            public bool IsPhysicsActive => true;
            public void OnTerrainCollision(Vector2 point,Vector2 normal) {}
            public void UpdatePhysics(float step,MapData map)
            {
                int tick=Positions.Count+1;
                player.MoveRight(tick<=21); player.MoveLeft(tick>21);
                if(tick==21) player.Jump(); else player.ReleaseJump();
                player.UpdatePhysics(step,map);
                Positions.Add(player.Position);
            }
        }
    }
}
