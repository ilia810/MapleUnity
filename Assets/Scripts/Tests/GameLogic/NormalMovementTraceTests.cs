using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MapleClient.GameLogic;
using MapleClient.GameLogic.Core;
using NUnit.Framework;

namespace MapleClient.Tests.GameLogic
{
    public class NormalMovementTraceTests
    {
        public static readonly string[] Scenarios = {
            "walk_right", "walk_left", "walk_speed140", "jump100", "jump120",
            "moving_jump_brake", "air_input", "fast_fall", "slope_down", "slope_up", "ledge", "wall"
        };

        public static List<Foothold> Terrain(string name)
        {
            bool slope = name.StartsWith("slope"), ledge = name == "ledge", wall = name == "wall";
            var floors = new List<Foothold> {
                new Foothold(1,-1000,0,slope || ledge || wall ? 100 : 1000,0) { Layer=1, NextId=slope || wall ? 2 : 0 }
            };
            if (slope) floors.Add(new Foothold(2,100,0,300,name == "slope_up" ? -100 : 100) { Layer=1, PreviousId=1 });
            if (wall) floors.Add(new Foothold(2,100,-100,100,0) { Layer=1, PreviousId=1, IsWall=true });
            floors.Add(new Foothold(3,-1000,400,1000,400) { Layer=1 });
            return floors;
        }

        private static void Input(string name, int tick, out bool left, out bool right, out bool jump)
        {
            left = name == "walk_left" ? tick <= 40 : name == "moving_jump_brake" && tick > 21;
            right = name == "walk_right" || name == "walk_speed140" ? tick <= 40 :
                name == "moving_jump_brake" ? tick <= 21 : name.StartsWith("slope") || name == "ledge" || name == "wall" || name == "air_input";
            jump = name == "jump100" || name == "jump120" ? tick == 1 : name == "moving_jump_brake" && tick == 21;
        }

        private static IEnumerable<double[]> Rows(string name)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts/Tests/GameLogic/Fixtures/HeavenMovement.csv");
            Assert.That(File.Exists(path), Is.True, "Generate with Tools/Generate-HeavenMovementTrace.py.");
            return File.ReadLines(path).Where(line => line.StartsWith(name + ","))
                .Select(line => line.Split(',').Skip(1).Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray()).ToArray();
        }

        [TestCaseSource(nameof(Scenarios))]
        public void NormalEngineMatchesCompiledCppEveryTick(string name)
        {
            bool airborne = name == "air_input" || name == "fast_fall";
            var movement = new NormalMovement {
                X = name.StartsWith("slope") || name == "ledge" || name == "wall" ? 90 : 0,
                Y = airborne ? -200 : 0, OnGround = !airborne,
                VSpeed = name == "fast_fall" ? 12 : 0,
                State = airborne ? NormalMovement.Stance.Fall : NormalMovement.Stance.Stand
            };
            movement.SetTerrain(new NormalTerrain(Terrain(name)));
            var rows = Rows(name).ToArray();
            Assert.That(rows.Length, Is.EqualTo(160));
            foreach (var r in rows)
            {
                Input(name,(int)r[0],out bool left,out bool right,out bool jump);
                movement.Step(left,right,false,jump,NormalMovement.WalkForce(name == "walk_speed140" ? 140 : 100),
                    NormalMovement.JumpForce(name == "jump100" ? 100 : 120));
                    Assert.That(movement.X,Is.EqualTo(r[1]).Within(0.00001),name+" x tick "+r[0]);
                    Assert.That(movement.Y,Is.EqualTo(r[2]).Within(0.00001),name+" y tick "+r[0]);
                    Assert.That(movement.HSpeed,Is.EqualTo(r[3]).Within(0.000001),name+" vx tick "+r[0]);
                    Assert.That(movement.VSpeed,Is.EqualTo(r[4]).Within(0.000001),name+" vy tick "+r[0]);
                    Assert.That(movement.OnGround,Is.EqualTo(r[5] == 1),name+" ground tick "+r[0]);
                    Assert.That(movement.FootholdId,Is.EqualTo((int)r[6]),name+" foothold tick "+r[0]);
                    Assert.That((int)movement.State,Is.EqualTo((int)r[7]),name+" stance tick "+r[0]);
            }
        }

        [TestCaseSource(nameof(Scenarios))]
        public void RuntimePlayerMatchesCompiledCppAfterCoordinateConversion(string name)
        {
            var service = new FootholdService(); service.LoadFootholds(Terrain(name));
            bool airborne = name == "air_input" || name == "fast_fall";
            var player = new Player(service) {
                Position = new Vector2(name.StartsWith("slope") || name == "ledge" || name == "wall" ? 0.9f : 0, airborne ? 2.3f : 0.3f),
                IsGrounded = !airborne,
                Velocity = new Vector2(0,name == "fast_fall" ? -15 : 0),
                Speed = name == "walk_speed140" ? 140 : 100,
                JumpPower = name == "jump100" ? 100 : 120
            };
            var map = new MapData();
            foreach (var r in Rows(name))
            {
                Input(name,(int)r[0],out bool left,out bool right,out bool jump);
                player.MoveLeft(left); player.MoveRight(right);
                if (jump) player.Jump(); else player.ReleaseJump();
                player.UpdatePhysics(PhysicsUpdateManager.FIXED_TIMESTEP,map);
                    Assert.That(player.Position.X * 100,Is.EqualTo(r[1]).Within(0.002),name+" x tick "+r[0]);
                    Assert.That(-(player.Position.Y - Player.Height/2) * 100,Is.EqualTo(r[2]).Within(0.002),name+" y tick "+r[0]);
                    Assert.That(NormalMovement.ToTickSpeed(player.Velocity.X),Is.EqualTo(r[3]).Within(0.00001),name+" vx tick "+r[0]);
                    Assert.That(-NormalMovement.ToTickSpeed(player.Velocity.Y),Is.EqualTo(r[4]).Within(0.00001),name+" vy tick "+r[0]);
                    Assert.That(player.IsGrounded,Is.EqualTo(r[5] == 1),name+" ground tick "+r[0]);
                    Assert.That(player.CurrentFootholdId,Is.EqualTo((int)r[6]),name+" foothold tick "+r[0]);
            }
        }
    }
}
