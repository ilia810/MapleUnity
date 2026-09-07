using System;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Interfaces;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class FaceAnimationIntegrationTests
    {
        private sealed class Input : IInputProvider, IExpressionInputProvider
        {
            public bool IsLeftPressed { get; set; } public bool IsRightPressed { get; set; }
            public bool IsUpPressed { get; set; } public bool IsDownPressed { get; set; }
            public bool IsJumpPressed { get; set; } public bool IsAttackPressed { get; set; }
            public CharacterExpression? ExpressionPressed { get; set; }
        }
        private static MapData Ground()
        {
            var map = new MapData { MapId = 1 };
            map.Platforms.Add(new Platform { Id = 1, X1 = -2000, X2 = 2000, Y1 = 0, Y2 = 0 }); return map;
        }
        private static Player MakePlayer()
        {
            var player = new Player { Position = new Vector2(0, Player.Height / 2), IsGrounded = true };
            player.FaceAnimation.SetData(FaceAnimationTests.Data()); return player;
        }
        [Test] public void FastKeyPressQueuesOneExpressionAndRejectsChangesDuringSourceCooldown()
        {
            var loader = new FakeMapLoader(); loader.AddMap(1, Ground()); var input = new Input(); var world = new GameWorld(input, loader);
            world.LoadMap(1); world.Player.FaceAnimation.SetData(FaceAnimationTests.Data());
            input.ExpressionPressed = CharacterExpression.Smile; world.ProcessInput(); input.ExpressionPressed = null;
            world.ProcessInput(); world.UpdatePhysics(.004f);
            Assert.That(world.Player.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Default));
            world.UpdatePhysics(.004f);
            Assert.That(world.Player.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Smile));
            Assert.That(world.Player.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(8));
            Assert.That(world.Player.FaceAnimation.CooldownMilliseconds, Is.EqualTo(4992));
            Assert.That(world.Player.RequestExpression(CharacterExpression.Cry), Is.False);
        }
        [TestCase(true)][TestCase(false)] public void StationaryLadderOrRopeFreezesFaceAndExpressionCooldown(bool ladder)
        {
            var p = TraversalTraceTests.CreatePlayer("climb_jump_right"); var map = TraversalTraceTests.Map("climb_jump_right");
            map.Ladders[0].IsLadder = ladder; p.FaceAnimation.SetData(FaceAnimationTests.Data());
            p.ClimbUp(true); p.UpdatePhysics(.008f, map); p.ClimbUp(false);
            Assert.That(p.RequestExpression(CharacterExpression.Angry), Is.True); p.UpdatePhysics(.008f, map);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.Zero); Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(5000));
            for (int i = 0; i < 80; i++) p.UpdatePhysics(.008f, map);
            Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(5000));
            p.Speed = 140; p.ClimbUp(true); p.UpdatePhysics(.008f, map);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(11)); Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(4992));
        }
        [Test] public void WalkingAndSwimmingAdvanceFaceAtTheirOwnBodySpeed()
        {
            var p = MakePlayer(); var map = Ground(); p.MoveRight(true);
            p.UpdatePhysics(.008f, map); Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.Zero);
            for (int i = 0; i < 20; i++) p.UpdatePhysics(.008f, map);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.GreaterThan(0).And.LessThan(160));
            int before = p.FaceAnimation.ElapsedMilliseconds;
            p.ResetMovementForMap(); p.Position = new Vector2(0, 2); p.IsGrounded = false;
            map.IsUnderwater = true; p.UpdatePhysics(.008f, map);
            Assert.That(p.State, Is.EqualTo(PlayerState.Swimming)); Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(before + 8));
            p.UpdatePhysics(.008f, map); Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(before + 16));
        }
        [Test] public void AttackAndTravelRetainFacePhaseWhileDeathFreezesAndPendingInputIsCleared()
        {
            var p = MakePlayer(); var map = Ground(); p.PlayBasicAttackAnimation();
            p.RequestExpression(CharacterExpression.Hit); p.UpdatePhysics(.08f, map);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(80)); Assert.That(p.IsBasicAttacking, Is.True);
            p.ResetMovementForMap(); Assert.That(p.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Hit));
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(80)); Assert.That(p.FaceAnimation.CooldownMilliseconds, Is.EqualTo(4920));
            var fresh = MakePlayer(); fresh.RequestExpression(CharacterExpression.Cry); fresh.ResetMovementForMap(); fresh.UpdatePhysics(.008f, map);
            Assert.That(fresh.FaceAnimation.Expression, Is.EqualTo(CharacterExpression.Default));
            p.TakeDamage(p.CurrentHP); Assert.That(p.RequestExpression(CharacterExpression.Cry), Is.False);
            int elapsed = p.FaceAnimation.ElapsedMilliseconds; p.UpdatePhysics(.08f, map);
            Assert.That(p.FaceAnimation.ElapsedMilliseconds, Is.EqualTo(elapsed));
        }
        [TestCase(4)][TestCase(16)][TestCase(34)][TestCase(67)] public void HostFrameSizeDoesNotChangeBlinkAndExpressionPhase(int milliseconds)
        {
            var expected = Replay(8); var actual = Replay(milliseconds);
            Assert.That(actual, Is.EqualTo(expected));
        }
        private static Tuple<CharacterExpression,int,int,int> Replay(int milliseconds)
        {
            var p = MakePlayer(); var map = Ground(); p.RequestExpression(CharacterExpression.Smile);
            for (int time = 0; time < 6400;)
            {
                int delta = Math.Min(milliseconds, 6400-time); time += delta; p.UpdatePhysics(delta/1000f, map);
            }
            return Tuple.Create(p.FaceAnimation.Expression, p.FaceAnimation.Frame, p.FaceAnimation.ElapsedMilliseconds, p.FaceAnimation.CooldownMilliseconds);
        }
    }
}
