using System.Collections.Generic;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class PlayerPhysicsRegressionTests
    {
        private const float Step = PhysicsUpdateManager.FIXED_TIMESTEP;
        private static FootholdService Ground(params Foothold[] footholds)
        {
            var service = new FootholdService();
            service.LoadFootholds(new List<Foothold>(footholds));
            return service;
        }
        private static Player Grounded(FootholdService service) =>
            new Player(service) { Position = new Vector2(0, 0.3f), IsGrounded = true };

        [Test]
        public void InputPolling_DoesNotIntegrateMovement()
        {
            var player = Grounded(Ground(new Foothold(1, -1000, 0, 1000, 0)));
            for (int i = 0; i < 10; i++) { player.MoveLeft(false); player.MoveRight(true); }
            Assert.That(player.Velocity.X, Is.Zero);
            Assert.That(player.Position.X, Is.Zero);
            player.UpdatePhysics(Step, new MapData());
            Assert.That(player.Velocity.X, Is.Zero, "Source STAND first transitions to WALK.");
            player.UpdatePhysics(Step, new MapData());
            Assert.That(player.Velocity.X, Is.EqualTo(0.2f).Within(0.00001f));
        }
        [Test]
        public void Movement_IsIndependentOfInputPollingFrequency()
        {
            var service = Ground(new Foothold(1, -1000, 0, 1000, 0));
            var once = Grounded(service);
            var repeated = Grounded(service);
            for (int step = 0; step < 20; step++)
            {
                bool right = step < 8;
                once.MoveLeft(false); once.MoveRight(right);
                for (int poll = 0; poll < 4; poll++) { repeated.MoveLeft(false); repeated.MoveRight(right); }
                once.UpdatePhysics(Step, new MapData());
                repeated.UpdatePhysics(Step, new MapData());
                Assert.That(repeated.Position.X, Is.EqualTo(once.Position.X).Within(0.00001f));
                Assert.That(repeated.Velocity.X, Is.EqualTo(once.Velocity.X).Within(0.00001f));
            }
        }
        [Test]
        public void PartialTicksWaitBeforeSourceAccelerationAndDrag()
        {
            var player = Grounded(Ground(new Foothold(1, -1000, 0, 1000, 0)));
            player.MoveRight(true);
            player.UpdatePhysics(Step, new MapData()); // STAND -> WALK.
            player.UpdatePhysics(0.004f, new MapData());
            Assert.That(player.Velocity.X, Is.Zero);
            player.UpdatePhysics(0.004f, new MapData());
            Assert.That(player.Velocity.X, Is.EqualTo(0.2f).Within(0.00001f));
            player.Velocity = new Vector2(1, 0); player.MoveRight(false);
            player.UpdatePhysics(Step, new MapData());
            Assert.That(player.Velocity.X, Is.EqualTo(0.8f).Within(0.00001f),
                "Source release drag multiplies flat-ground velocity by 0.8 per tick.");
        }
        [Test]
        public void TerminalSpeedFall_CrossingMoreThanTenPixels_Lands()
        {
            var player = new Player(Ground(new Foothold(1, -1000, 0, 1000, 0))) {
                Position = new Vector2(0, 0.301f), Velocity = new Vector2(0, -MaplePhysics.MaxFallSpeed) };
            player.UpdatePhysics(Step, new MapData());
            player.UpdatePhysics(Step, new MapData());
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.Position.Y, Is.EqualTo(0.3f).Within(0.00001f));
            Assert.That(player.Velocity.Y, Is.Zero);
        }
        [Test]
        public void SweptFall_LandsOnFirstCrossedFloor()
        {
            var player = new Player(Ground(new Foothold(1, -1000, -200, 1000, -200), new Foothold(2, -1000, 200, 1000, 200))) {
                Position = new Vector2(0, 3), Velocity = new Vector2(0, -MaplePhysics.MaxFallSpeed) };
            player.UpdatePhysics(0.25f, new MapData());
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.Position.Y, Is.EqualTo(2.3f).Within(0.00001f));
        }
        [Test]
        public void LeavingLedge_FallsWithoutSnappingToLowerFloor()
        {
            var player = new Player(Ground(new Foothold(1, -1000, -200, 100, -200), new Foothold(2, -1000, 200, 1000, 200))) {
                Position = new Vector2(0.999f, 2.3f), Velocity = new Vector2(MaplePhysics.WalkSpeed, 0), IsGrounded = true };
            player.MoveRight(true); player.UpdatePhysics(Step, new MapData());
            Assert.That(player.Position.X, Is.GreaterThan(1));
            // Source foothold transitions use floor/ceil pixel columns at the edge.
            for (int i=0;i<8 && player.IsGrounded;i++) player.UpdatePhysics(Step,new MapData());
            Assert.That(player.IsGrounded, Is.False);
            Assert.That(player.Position.Y, Is.GreaterThan(2.29f));
            player.UpdatePhysics(Step, new MapData());
            Assert.That(player.Velocity.Y, Is.LessThan(0));
        }
        [TestCase(-100f)]
        [TestCase(100f)]
        public void ConnectedSlope_RemainsSupported(float farY)
        {
            var slope = new Foothold(2, 100, 0, 300, farY) { PreviousId=1 };
            var player = new Player(Ground(new Foothold(1, -1000, 0, 100, 0) { NextId=2 }, slope)) {
                Position = new Vector2(0.999f, 0.3f), Velocity = new Vector2(MaplePhysics.WalkSpeed, 0), IsGrounded = true };
            player.MoveRight(true);
            for (int i = 0; i < 30; i++)
            {
                player.UpdatePhysics(Step, new MapData());
                Assert.That(player.IsGrounded, Is.True);
                float expected = -slope.GetYAtX(player.Position.X * 100) / 100 + 0.3f;
                Assert.That(player.Position.Y, Is.EqualTo(expected).Within(0.011f), "Source applies slope support before horizontal integration (within one source pixel).");
            }
        }
        [TestCase(PlayerState.Falling)]
        [TestCase(PlayerState.DoubleJumping)]
        [TestCase(PlayerState.FlashJumping)]
        public void Landing_FromAirborneState_RestoresStandingAndRaisesOneEvent(PlayerState state)
        {
            var player = new Player(Ground(new Foothold(1, -1000, 0, 1000, 0))) { Position = new Vector2(0, 2) };
            if (state == PlayerState.DoubleJumping) { player.EnableDoubleJump(true); player.Jump(); player.UpdatePhysics(Step, new MapData()); }
            else if (state == PlayerState.FlashJumping) { player.EnableFlashJump(true); player.MoveRight(true); player.FlashJump(); player.MoveRight(false); }
            else player.UpdatePhysics(Step, new MapData());
            Assert.That(player.State, Is.EqualTo(state));
            player.Position = new Vector2(0, 0.35f); player.Velocity = new Vector2(0, -MaplePhysics.MaxFallSpeed);
            int landed = 0; player.Landed += () => landed++;
            for (int i = 0; i < 10; i++) player.UpdatePhysics(Step, new MapData());
            Assert.That(player.State, Is.EqualTo(PlayerState.Standing));
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.IsJumping, Is.False);
            Assert.That(landed, Is.EqualTo(1));
        }
        [Test]
        public void Walls_AreExcludedFromGroundQueriesAndLanding()
        {
            var service = Ground(new Foothold(1, 0, -100, 0, 100), new Foothold(2, -100, -100, 100, -100) { IsWall = true });
            Assert.That(service.GetGroundBelow(0, -200), Is.EqualTo(float.MaxValue));
            Assert.That(service.GetFootholdBelow(0, -200), Is.Null);
            Assert.That(service.GetFootholdAt(0, -100), Is.Null);
            var player = new Player(service) { Position = new Vector2(0, 1.35f), Velocity = new Vector2(0, -6.7f) };
            player.UpdatePhysics(0.1f, new MapData());
            Assert.That(player.IsGrounded, Is.False);
            Assert.That(player.Position.Y, Is.LessThan(1));
        }
        [Test]
        public void PlatformFallback_UsesMapleYDirectionAndReversedEndpoints()
        {
            var map = new MapData();
            map.Platforms.Add(new Platform { Id = 1, X1 = 100, Y1 = 200, X2 = -100, Y2 = 200 });
            var player = new Player { Position = new Vector2(0, -1.65f), Velocity = new Vector2(0, -6.7f) };
            player.UpdatePhysics(Step, map);
            player.UpdatePhysics(Step, map);
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(player.Position.Y, Is.EqualTo(-1.7f).Within(0.00001f));
            Assert.That(player.GetCurrentPlatform(map), Is.SameAs(map.Platforms[0]));
        }
    }
}
