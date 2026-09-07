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
    public class SwimmingTraceTests
    {
        public static readonly string[] Scenarios={"water_idle","water_right","water_left","water_up","water_down","water_diagonal",
            "water_opposed","water_brake","water_jump_ignored","water_ground_jump","water_ground_speed","water_ledge",
            "water_wall","water_ceiling","water_land_prone","water_ladder","water_rope_jump","water_attack","water_knockback","water_knockback_steering"};
        private static bool Grounded(string name)=>name=="water_ground_jump"||name=="water_ground_speed"||name=="water_ledge";
        private static double X(string name)=>name=="water_wall"||name=="water_ledge"?90:0;
        private static double Y(string name)=>Grounded(name)?0:name=="water_ceiling"?-295:name=="water_land_prone"?-10:-150;
        public static List<Foothold> Terrain(string name)
        {
            var f=new List<Foothold>{new Foothold(1,-1000,0,name=="water_ledge"||name=="water_wall"?100:1000,0){Layer=1,NextId=name=="water_wall"?2:0},
                new Foothold(3,-1000,400,1000,400){Layer=4}};
            if(name=="water_wall")f.Insert(1,new Foothold(2,100,-200,100,0){Layer=1,PreviousId=1});return f;
        }
        public static MapData Map(string name)
        {
            var map=new MapData {IsUnderwater=true};
            if(name=="water_ladder"||name=="water_rope_jump")map.Ladders.Add(new LadderInfo {Id=1,X=0,Y1=0,Y2=2.5f,IsLadder=name!="water_rope_jump"});
            return map;
        }
        private static double[][] Rows(string name)
        {
            var rows=File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(),"Assets/Scripts/Tests/GameLogic/Fixtures/HeavenSwimming.csv"))
                .Where(l=>l.StartsWith(name+",",StringComparison.Ordinal))
                .Select(l=>l.Split(',').Skip(1).Select(v=>double.Parse(v,CultureInfo.InvariantCulture)).ToArray()).ToArray();
            Assert.That(rows.Length,Is.EqualTo(220));return rows;
        }
        private static void Compare(double[] r,double x,double y,double vx,double vy,bool ground,int fh,int layer,int stance,int ladder,double tolerance)
        {
            string at=" at tick "+r[0];
            Assert.That(x,Is.EqualTo(r[1]).Within(tolerance),"X"+at);Assert.That(y,Is.EqualTo(r[2]).Within(tolerance),"Y"+at);
            Assert.That(vx,Is.EqualTo(r[3]).Within(.00002),"HSpeed"+at);Assert.That(vy,Is.EqualTo(r[4]).Within(.00002),"VSpeed"+at);
            Assert.That(ground,Is.EqualTo(r[5]==1),"Contact"+at);Assert.That(fh,Is.EqualTo((int)r[6]),"Foothold"+at);
            Assert.That(stance,Is.EqualTo((int)r[7]),"Stance"+at);Assert.That(layer,Is.EqualTo((int)r[8]),"Layer"+at);
            Assert.That(ladder,Is.EqualTo((int)r[9]),"Ladder"+at);
        }
        [TestCaseSource(nameof(Scenarios))]
        public void SolverMatchesCompiledCppEveryWaterTick(string name)
        {
            var m=new NormalMovement {X=X(name),Y=Y(name),OnGround=Grounded(name),State=Grounded(name)?NormalMovement.Stance.Stand:NormalMovement.Stance.Fall,
                VSpeed=name=="water_ceiling"?-10:0};m.SetTerrain(new NormalTerrain(Terrain(name)));var map=Map(name);
            foreach(var r in Rows(name))
            {
                if(r[18]==1)m.ApplyContactKnockback(true);
                m.Step(r[11]==1,r[12]==1,r[14]==1,r[16]==1,NormalMovement.WalkForce((int)r[19]),NormalMovement.JumpForce((int)r[20]),
                    up:r[13]==1,jumpHeld:r[15]==1,climbForce:(float)r[19]/100,ladders:map.Ladders,underwater:true,
                    attacking:name=="water_attack"&&r[0]>=21&&r[0]<=95);
                if(name=="water_attack"&&r[0]==95)m.FinishAttack(r[11]==1,r[12]==1,r[14]==1);
                Compare(r,m.X,m.Y,m.HSpeed,m.VSpeed,m.OnGround,m.FootholdId,m.FootholdLayer,(int)m.State,m.CurrentLadder?.Id??0,.00001);
                Assert.That(m.FacingDirection,Is.EqualTo((int)r[10]),"Facing at tick "+r[0]);
            }
        }
        [TestCaseSource(nameof(Scenarios))]
        public void PlayerMatchesCompiledCppWaterInputsAndAttackCompletion(string name)
        {
            var service=new FootholdService();service.LoadFootholds(Terrain(name));
            var p=new Player(service){Position=new Vector2((float)X(name)/100,(float)-Y(name)/100+Player.Height/2),IsGrounded=Grounded(name),
                Velocity=new Vector2(0,name=="water_ceiling"?12.5f:0),Speed=name=="water_ground_speed"?140:100};var map=Map(name);
            foreach(var r in Rows(name))
            {
                if(name=="water_attack"&&r[0]==21)Assert.That(p.PlayBasicAttackAnimation(),Is.True);
                p.MoveLeft(r[11]==1);p.MoveRight(r[12]==1);p.ClimbUp(r[13]==1);p.Crouch(r[14]==1);p.ClimbDown(r[14]==1);
                if(r[15]==1)p.Jump();else p.ReleaseJump();if(r[18]==1)p.ReceiveContactDamage(1,true);
                p.UpdatePhysics(.008f,map);p.BasicAttack?.Advance(.008f);
                int stance=p.State==PlayerState.Swimming?6:p.State==PlayerState.Walking?1:p.State==PlayerState.Crouching?3:
                    p.State==PlayerState.Climbing?(p.GetCurrentLadder().IsLadder?4:5):
                    p.State==PlayerState.Falling||(p.State==PlayerState.Jumping&&!p.IsGrounded)?2:0;
                Compare(r,p.Position.X*100,-(p.Position.Y-Player.Height/2)*100,NormalMovement.ToTickSpeed(p.Velocity.X),-NormalMovement.ToTickSpeed(p.Velocity.Y),
                    p.IsGrounded,p.CurrentFootholdId,p.CurrentFootholdLayer,stance,p.GetCurrentLadder()?.Id??0,.002);
                if(r[10]!=0)Assert.That(p.FacingRight,Is.EqualTo(r[10]>0),"Facing at tick "+r[0]);
                Assert.That(p.IsBasicAttacking,Is.EqualTo(r[17]==1),"Attack at tick "+r[0]);
            }
        }
    }
}
