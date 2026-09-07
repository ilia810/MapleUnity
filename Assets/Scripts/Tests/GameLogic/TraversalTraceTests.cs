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
    public class TraversalTraceTests
    {
        public static readonly string[] Scenarios = {
            "drop_prone", "drop_walk", "drop_simultaneous", "drop_599", "drop_600", "drop_close", "drop_bottom",
            "climb_up", "climb_down", "climb_rope", "climb_pause", "climb_speed140", "climb_jump_left",
            "climb_jump_right", "climb_jump_both", "climb_jump_alone", "climb_jump_held", "climb_cooldown",
            "climb_range_inside", "climb_range_outside"
        };
        public static List<Foothold> Terrain(string name)
        {
            int lower = name == "drop_599" ? 599 : name == "drop_600" ? 600 : name == "drop_close" ? 8 : 400;
            var result = new List<Foothold> {
                new Foothold(1,-1000,0,1000,0) { Layer=1 },
                new Foothold(3,-1000,lower,1000,lower) { Layer=1 }
            };
            int top = name.StartsWith("climb_jump_") ? -400 : -80;
            if (name.StartsWith("climb_")) result.Add(new Foothold(2,-1000,top,1000,top) { Layer=1 });
            return result;
        }
        public static MapData Map(string name)
        {
            var map = new MapData();
            if (name.StartsWith("climb_")) map.Ladders.Add(new LadderInfo {
                Id=1, X=0, Y1=0, Y2=name.StartsWith("climb_jump_") ? 4 : 0.8f, IsLadder=name!="climb_rope"
            });
            return map;
        }
        private static double InitialX(string name) => !name.StartsWith("climb_") ? 0 :
            name == "climb_range_inside" ? 10.49 : name == "climb_range_outside" ? 10.5 : 7;
        private static double InitialY(string name) => name.StartsWith("climb_jump_") ? -40 :
            name == "climb_down" ? -80 : name == "drop_bottom" ? 400 : 0;
        public static Player CreatePlayer(string name)
        {
            var service = new FootholdService(); service.LoadFootholds(Terrain(name));
            return new Player(service) {
                Position=new Vector2((float)(InitialX(name)/100), (float)(-InitialY(name)/100)+Player.Height/2),
                IsGrounded=!name.StartsWith("climb_jump_"), Speed=name=="climb_speed140"?140:100
            };
        }
        private static double[][] Rows(string name)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),"Assets/Scripts/Tests/GameLogic/Fixtures/HeavenTraversal.csv");
            var rows = File.ReadLines(path).Where(l => l.StartsWith(name+",")).Select(l =>
                l.Split(',').Skip(1).Select(v => double.Parse(v,CultureInfo.InvariantCulture)).ToArray()).ToArray();
            Assert.That(rows.Length, Is.EqualTo(260), "Regenerate the C++ traversal fixture.");
            return rows;
        }
        private static void Compare(double[] r, double x, double y, double vx, double vy, bool grounded, int fh, double tolerance)
        {
            Assert.That(x, Is.EqualTo(r[1]).Within(tolerance), "X at tick "+r[0]);
            Assert.That(y, Is.EqualTo(r[2]).Within(tolerance), "Y at tick "+r[0]);
            Assert.That(vx, Is.EqualTo(r[3]).Within(0.00001), "HSpeed at tick "+r[0]);
            Assert.That(vy, Is.EqualTo(r[4]).Within(0.00001), "VSpeed at tick "+r[0]);
            Assert.That(grounded, Is.EqualTo(r[5]==1), "Contact at tick "+r[0]);
            Assert.That(fh, Is.EqualTo((int)r[6]), "Foothold at tick "+r[0]);
        }
        [TestCaseSource(nameof(Scenarios))]
        public void EngineMatchesCompiledCppTraversalEveryTick(string name)
        {
            var movement = new NormalMovement {
                X=InitialX(name), Y=InitialY(name), OnGround=!name.StartsWith("climb_jump_"),
                State=name.StartsWith("climb_jump_")?NormalMovement.Stance.Fall:NormalMovement.Stance.Stand
            };
            movement.SetTerrain(new NormalTerrain(Terrain(name)));
            var map = Map(name);
            foreach (var r in Rows(name))
            {
                movement.Step(r[12]==1,r[13]==1,r[15]==1,r[17]==1,
                    NormalMovement.WalkForce((int)r[18]),NormalMovement.JumpForce((int)r[19]),
                    up:r[14]==1,jumpHeld:r[16]==1,climbForce:(float)r[18]/100,ladders:map.Ladders);
                Compare(r,movement.X,movement.Y,movement.HSpeed,movement.VSpeed,movement.OnGround,movement.FootholdId,0.00001);
                Assert.That((int)movement.State, Is.EqualTo((int)r[7]), "Stance at tick "+r[0]);
                Assert.That(movement.CanDrop, Is.EqualTo(r[8]==1), "Drop eligibility at tick "+r[0]);
                Assert.That(movement.ClimbCooldownMilliseconds==0, Is.EqualTo(r[9]==1), "Cooldown at tick "+r[0]);
                Assert.That(movement.CurrentLadder?.Id??0, Is.EqualTo((int)r[10]), "Ladder at tick "+r[0]);
                Assert.That(movement.FacingDirection, Is.EqualTo((int)r[11]), "Facing at tick "+r[0]);
            }
        }
        [TestCaseSource(nameof(Scenarios))]
        public void RuntimePlayerMatchesCompiledCppTraversalEveryTick(string name)
        {
            var player = CreatePlayer(name);
            var map = Map(name);
            foreach (var r in Rows(name))
            {
                player.MoveLeft(r[12]==1); player.MoveRight(r[13]==1);
                player.ClimbUp(r[14]==1); player.ClimbDown(r[15]==1); player.Crouch(r[15]==1);
                if (r[16]==1) player.Jump(); else player.ReleaseJump();
                player.UpdatePhysics(0.008f,map);
                Compare(r,player.Position.X*100,-(player.Position.Y-Player.Height/2)*100,
                    NormalMovement.ToTickSpeed(player.Velocity.X),-NormalMovement.ToTickSpeed(player.Velocity.Y),
                    player.IsGrounded,player.CurrentFootholdId,0.002);
                Assert.That(player.CanDropThroughPlatform, Is.EqualTo(r[8]==1 && r[5]==1), "Drop at tick "+r[0]);
                Assert.That(player.CanClimb, Is.EqualTo(r[9]==1), "Cooldown at tick "+r[0]);
                Assert.That(player.GetCurrentLadder()?.Id??0, Is.EqualTo((int)r[10]), "Ladder at tick "+r[0]);
                Assert.That(player.State==PlayerState.Climbing, Is.EqualTo(r[7]==4 || r[7]==5), "Climbing at tick "+r[0]);
                if (r[11]!=0) Assert.That(player.FacingRight, Is.EqualTo(r[11]>0), "Facing at tick "+r[0]);
            }
        }
    }
}
