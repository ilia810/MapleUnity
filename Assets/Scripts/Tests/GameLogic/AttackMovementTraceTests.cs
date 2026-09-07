using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using MapleClient.GameLogic.Tests.Fakes;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class AttackMovementTraceTests
    {
        public static readonly string[] Scenarios = { "stand_turn", "walk_brake", "air_turn", "landing", "ledge",
            "prone_resume", "prone_drop", "walk_drop", "jump_discard", "jump_repress", "knockback",
            "climb_held", "both_resume", "stand_prone" };

        private static List<Foothold> Terrain(string name) => new List<Foothold> {
            new Foothold(1, -1000, 0, name == "ledge" ? 113 : 1000, 0) { Layer = 1 },
            new Foothold(3, -1000, 400, 1000, 400) { Layer = 1 }
        };
        private static MapData Map(string name)
        {
            var map = new MapData();
            if (name == "climb_held") map.Ladders.Add(new LadderInfo { Id = 1, IsLadder = true, X = 0, Y1 = 0, Y2 = 2 });
            return map;
        }
        private static IEnumerable<double[]> Rows(string name) => File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),
            "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenAttackMovement.csv"))
            .Where(line => line.StartsWith(name + ",", StringComparison.Ordinal))
            .Select(line => line.Split(',').Skip(1).Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray());
        private static void Compare(double[] r, double x, double y, double vx, double vy, bool ground, int foothold, double tolerance)
        {
            Assert.That(x, Is.EqualTo(r[1]).Within(tolerance), "X at tick " + r[0]);
            Assert.That(y, Is.EqualTo(r[2]).Within(tolerance), "Y at tick " + r[0]);
            Assert.That(vx, Is.EqualTo(r[3]).Within(tolerance), "HSpeed at tick " + r[0]);
            Assert.That(vy, Is.EqualTo(r[4]).Within(tolerance), "VSpeed at tick " + r[0]);
            Assert.That(ground, Is.EqualTo(r[5] == 1), "Contact at tick " + r[0]);
            Assert.That(foothold, Is.EqualTo((int)r[6]), "Foothold at tick " + r[0]);
        }

        [TestCaseSource(nameof(Scenarios))]
        public void SolverMatchesCompiledCppAttackStateAndPhysicsEveryTick(string name)
        {
            var movement = new NormalMovement { X = name == "ledge" ? 100 : 0, Y = name == "landing" ? -120 : 0,
                OnGround = name != "landing", State = name == "landing" ? NormalMovement.Stance.Fall : NormalMovement.Stance.Stand };
            movement.SetTerrain(new NormalTerrain(Terrain(name)));
            var map = Map(name); var rows = Rows(name).ToArray(); Assert.That(rows.Length, Is.EqualTo(150));
            foreach (var r in rows)
            {
                if (r[18] == 1) movement.ApplyContactKnockback(true);
                movement.Step(r[11] == 1, r[12] == 1, r[14] == 1, r[16] == 1,
                    NormalMovement.WalkForce(100), NormalMovement.JumpForce((int)r[19]), up: r[13] == 1,
                    jumpHeld: r[15] == 1, ladders: map.Ladders, attacking: r[0] >= 21 && r[0] <= 95);
                if (r[0] == 95)
                {
                    movement.FinishAttack(r[11] == 1, r[12] == 1, r[14] == 1);
                    movement.TryEnterLadder(r[13] == 1, r[14] == 1, map.Ladders);
                }
                Compare(r, movement.X, movement.Y, movement.HSpeed, movement.VSpeed, movement.OnGround, movement.FootholdId, .00001);
                Assert.That((int)movement.State, Is.EqualTo((int)r[7]), "Stance at tick " + r[0]);
                Assert.That(movement.CanDrop, Is.EqualTo(r[8] == 1));
                Assert.That(movement.CurrentLadder?.Id ?? 0, Is.EqualTo((int)r[9]));
                Assert.That(movement.FacingDirection, Is.EqualTo((int)r[10]), "Facing at tick " + r[0]);
            }
        }

        [TestCaseSource(nameof(Scenarios))]
        public void PlayerMatchesCompiledCppIncludingCompletionAndHeldInputs(string name)
        {
            var service = new FootholdService(); service.LoadFootholds(Terrain(name));
            var player = new Player(service) { Position = new Vector2(name == "ledge" ? 1 : 0, name == "landing" ? 1.5f : .3f),
                IsGrounded = name != "landing", JumpPower = name == "air_turn" ? 200 : 120 };
            var map = Map(name);
            foreach (var r in Rows(name))
            {
                if (r[0] == 21) Assert.That(player.PlayBasicAttackAnimation(), Is.True);
                player.MoveLeft(r[11] == 1); player.MoveRight(r[12] == 1);
                player.ClimbUp(r[13] == 1); player.Crouch(r[14] == 1); player.ClimbDown(r[14] == 1);
                if (r[15] == 1) player.Jump(); else player.ReleaseJump();
                if (r[18] == 1) player.ReceiveContactDamage(1, true);
                player.UpdatePhysics(.008f, map); player.BasicAttack?.Advance(.008f);
                Compare(r, player.Position.X * 100, -(player.Position.Y - Player.Height / 2) * 100,
                    NormalMovement.ToTickSpeed(player.Velocity.X), -NormalMovement.ToTickSpeed(player.Velocity.Y),
                    player.IsGrounded, player.CurrentFootholdId, .002);
                int stance = player.State == PlayerState.Walking ? 1 : player.State == PlayerState.Crouching ? 3 :
                    // Unity publishes the jump pose on launch; C++ keeps STAND until the next contact check.
                    player.State == PlayerState.Climbing ? 4 : player.State == PlayerState.Falling ||
                    (player.State == PlayerState.Jumping && !player.IsGrounded) ? 2 : 0;
                Assert.That(stance, Is.EqualTo((int)r[7]), "Stance at tick " + r[0]);
                Assert.That(player.IsBasicAttacking, Is.EqualTo(r[17] == 1), "Attack at tick " + r[0]);
                Assert.That(player.GetCurrentLadder()?.Id ?? 0, Is.EqualTo((int)r[9]));
                if (r[10] != 0) Assert.That(player.FacingRight, Is.EqualTo(r[10] > 0), "Facing at tick " + r[0]);
            }
        }

        [Test]
        public void JumpPressedBetweenPhysicsAndAttackCompletionIsDiscardedUntilReleased()
        {
            var service = new FootholdService(); service.LoadFootholds(Terrain("stand_turn"));
            var player = new Player(service) { Position = new Vector2(0, .3f), IsGrounded = true };
            var map = new MapData(); player.UpdatePhysics(.008f, map);
            Assert.That(player.PlayBasicAttackAnimation(), Is.True);
            player.BasicAttack.Advance(.592f); player.Jump(); player.BasicAttack.Advance(.008f);
            Assert.That(player.IsBasicAttacking, Is.False);
            player.Jump(); player.UpdatePhysics(.008f, map);
            Assert.That(player.Velocity.Y, Is.Zero);
            player.ReleaseJump(); player.Jump(); player.UpdatePhysics(.008f, map);
            Assert.That(player.Velocity.Y, Is.GreaterThan(0));
        }

        [Test]
        public void LegacyDoubleAndFlashJumpCannotBypassAnAirborneSwing()
        {
            var player = new Player { Position = new Vector2(0, 2), IsGrounded = false };
            player.EnableDoubleJump(true); player.EnableFlashJump(true); player.MoveRight(true);
            Assert.That(player.PlayBasicAttackAnimation(), Is.True);
            player.Jump(); player.FlashJump(); player.UpdatePhysics(.008f, new MapData());
            Assert.That(player.Position.X, Is.Zero); Assert.That(player.Velocity.Y, Is.LessThan(0));
            Assert.That(player.GetJumpCount(), Is.Zero); Assert.That(player.CanFlashJump(), Is.False);
        }

        private static GameWorld World(FakeInputProvider input, bool portal = false)
        {
            var loader = new FakeMapLoader();
            foreach (int id in new[] { 1, 2 })
            {
                var map = new MapData { MapId = id };
                map.Platforms.Add(new Platform { Id = 1, X1 = -1000, X2 = 1000, Y1 = 0, Y2 = 0 });
                map.Portals.Add(new Portal { Id = 0, Name = "sp", Type = PortalType.Spawn, X = 0, Y = 0 });
                if (portal && id == 1) map.Portals.Add(new Portal { Id = 1, Type = PortalType.Regular,
                    X = 0, Y = 0, TargetMapId = 2, TargetPortalName = "sp" });
                loader.AddMap(id, map);
            }
            var world = new GameWorld(input, loader); world.LoadMap(1); world.UpdatePhysics(.008f); return world;
        }

        [TestCase(true)] [TestCase(false)]
        public void PortalWaitsForSwingEndOnlyWhileUpRemainsHeld(bool held)
        {
            var input = new FakeInputProvider { IsAttackPressed = true }; var world = World(input, true);
            world.ProcessInput(); world.UpdatePhysics(.008f);
            input.IsAttackPressed = false; input.IsUpPressed = true;
            world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(world.CurrentMapId, Is.EqualTo(1)); Assert.That(world.Player.IsBasicAttacking, Is.True);
            input.IsUpPressed = held;
            for (int i = 0; i < 73; i++) { world.ProcessInput(); world.UpdatePhysics(.008f); }
            Assert.That(world.CurrentMapId, Is.EqualTo(1));
            world.ProcessInput(); world.UpdatePhysics(.008f);
            Assert.That(world.CurrentMapId, Is.EqualTo(held ? 2 : 1));
            Assert.That(world.Player.IsBasicAttacking, Is.False);
        }

        [TestCase(4)] [TestCase(16)] [TestCase(34)] [TestCase(67)]
        public void ChainedAttackMovementAndDiscardedJumpAreIndependentOfHostFrameSize(int milliseconds)
        {
            var expected = Replay(8); var actual = Replay(milliseconds);
            Assert.That(actual, Is.EqualTo(expected));
        }
        private static object[] Replay(int milliseconds)
        {
            var input = new FakeInputProvider { IsAttackPressed = true, IsRightPressed = true }; var world = World(input);
            world.ProcessInput(); world.UpdatePhysics(.008f);
            input.IsRightPressed = false; input.IsLeftPressed = true; input.IsJumpPressed = true;
            for (int elapsed = 0; elapsed < 1500;)
            {
                int frame = Math.Min(milliseconds, 1500 - elapsed);
                world.ProcessInput(); world.UpdatePhysics(frame / 1000f); elapsed += frame;
            }
            var player = world.Player;
            Assert.That(player.IsGrounded, Is.True, "The jump held during attacks must not fire between chained swings.");
            Assert.That(player.FacingRight, Is.False); Assert.That(player.IsBasicAttacking, Is.True);
            return new object[] { player.Position, player.Velocity, player.State, player.BasicAttack.Frame,
                player.BasicAttack.ElapsedMilliseconds, world.GetPhysicsDebugStats().TotalPhysicsSteps };
        }
    }
}
