using System;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class StanceAnimationIntegrationTests
    {
        private static MapData Ground()
        {
            var map = new MapData { MapId = 1 };
            map.Platforms.Add(new Platform { Id = 1, X1 = -2000, X2 = 2000, Y1 = 0, Y2 = 0 });
            return map;
        }
        private static Player PlayerOnGround()
        {
            var p = new Player { Position = new Vector2(0, Player.Height / 2), IsGrounded = true };
            p.StanceAnimation.SetData(new StanceAnimationTests.Data()); return p;
        }

        [Test] public void WalkingUsesPostPhysicsSpeedAndFinalStandingTransitionResetsTheClock()
        {
            var p = PlayerOnGround(); var map = Ground();
            p.UpdatePhysics(.08f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(80));
            p.MoveRight(true); p.UpdatePhysics(.008f, map);
            Assert.That(p.State, Is.EqualTo(PlayerState.Walking)); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
            int expectedTime = 0;
            for (int i = 0; i < 15; i++)
            {
                p.UpdatePhysics(.008f, map);
                float speed = (float)Math.Abs(NormalMovement.ToTickSpeed(p.Velocity.X));
                expectedTime += speed >= .125f ? (int)(8 * speed) : 0;
            }
            Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(expectedTime));
            p.MoveRight(false); p.UpdatePhysics(.008f, map);
            Assert.That(p.State, Is.EqualTo(PlayerState.Standing)); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
            p.UpdatePhysics(.008f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(8));
        }

        [TestCase(true)][TestCase(false)]
        public void ClimbEntrySpeedChangesStopsAndJumpOffUseSourceClock(bool isLadder)
        {
            var p = TraversalTraceTests.CreatePlayer("climb_jump_right"); var map = TraversalTraceTests.Map("climb_jump_right");
            map.Ladders[0].IsLadder = isLadder; p.StanceAnimation.SetData(new StanceAnimationTests.Data());
            p.ClimbUp(true); p.UpdatePhysics(.008f, map);
            Assert.That(p.StanceAnimation.Stance, Is.EqualTo(isLadder ? CharacterState.Ladder : CharacterState.Rope));
            Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero, "Stage grabs after the body update.");
            p.UpdatePhysics(.008f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(8));
            p.Speed = 140; p.UpdatePhysics(.008f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(19));
            p.ClimbUp(false); for (int i = 0; i < 20; i++) p.UpdatePhysics(.008f, map);
            Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(19));
            p.ClimbDown(true); p.UpdatePhysics(.008f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(30));
            p.ClimbDown(false); p.MoveRight(true); p.Jump(); p.UpdatePhysics(.008f, map);
            Assert.That(p.StanceAnimation.Stance, Is.EqualTo(CharacterState.Jump));
            Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(8), "Jump-off changes the pose before Char::update.");
        }

        [Test] public void AttackReturnAndMapResetRestartOrdinaryPoseWithoutAView()
        {
            var p = PlayerOnGround(); var map = Ground(); p.UpdatePhysics(.16f, map);
            Assert.That(p.StanceAnimation.Frame, Is.EqualTo(1)); Assert.That(p.PlayBasicAttackAnimation(), Is.True);
            int time = p.StanceAnimation.ElapsedMilliseconds;
            p.UpdatePhysics(.08f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(time));
            for (int i = 0; i < 100 && p.IsBasicAttacking; i++) p.BasicAttack.Advance(.008f);
            Assert.That(p.IsBasicAttacking, Is.False); Assert.That(p.StanceAnimation.Frame, Is.Zero);
            Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
            p.UpdatePhysics(.008f, map); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.EqualTo(8));
            p.ResetMovementForMap(); Assert.That(p.StanceAnimation.ElapsedMilliseconds, Is.Zero);
        }

        [TestCase(4)][TestCase(16)][TestCase(34)][TestCase(67)]
        public void CatchUpTicksGiveTheSameWalkingFramesAndElapsedTime(int milliseconds)
        {
            var actual = Replay(milliseconds); var expected = Replay(8);
            Assert.That(actual.Item1, Is.EqualTo(expected.Item1)); Assert.That(actual.Item2, Is.EqualTo(expected.Item2));
            Assert.That(actual.Item3, Is.EqualTo(expected.Item3));
        }
        private static Tuple<Vector2,int,int> Replay(int frameMilliseconds)
        {
            var map = Ground(); var loader = new FakeMapLoader(); loader.AddMap(1, map);
            var input = new FakeInputProvider(); var w = new GameWorld(input, loader); w.LoadMap(1);
            var p = w.Player; p.StanceAnimation.SetData(new StanceAnimationTests.Data()); w.UpdatePhysics(.08f);
            input.IsRightPressed = true;
            for (int time = 0; time < 960;)
            {
                int delta = Math.Min(frameMilliseconds, 960 - time); time += delta;
                w.ProcessInput(); w.UpdatePhysics(delta / 1000f);
            }
            return Tuple.Create(p.Position, p.StanceAnimation.Frame, p.StanceAnimation.ElapsedMilliseconds);
        }
    }
}
