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
    public class TerrainTraceTests
    {
        public static readonly string[] Scenarios = {
            "overlap_fall","overlap_jump","overlap_walk","overlap_ledge","overlap_drop",
            "tie","tie_reverse","tie_rehash","crossing_fall","crossing_walk","negative_column",
            "wall_other_layer","ceiling","left_boundary","right_boundary","high_map"
        };
        public static List<Foothold> Terrain(string name)
        {
            bool crossing=name.StartsWith("crossing_"), tied=name.StartsWith("tie");
            int baseY=name=="high_map"?4500:0;
            var first=new Foothold(1,-200,crossing?-100:baseY,name=="wall_other_layer"?100:200,crossing?100:baseY) {
                Layer=1,NextId=name=="wall_other_layer"?2:0
            };
            var upper=new Foothold(2,crossing?-200:-100,crossing?100:tied?0:-80,
                crossing?200:100,crossing?-100:tied?0:-80) { Layer=4 };
            var result=new List<Foothold>();
            if(name=="tie_reverse") result.Add(upper);
            result.Add(name=="negative_column"?new Foothold(1,-200,0,-100,0) { Layer=1 }:first);
            if(name=="negative_column") result.Add(new Foothold(2,-99,-40,100,-40) { Layer=4 });
            else if(name=="wall_other_layer") result.Add(new Foothold(2,100,-80,100,0) { Layer=4,PreviousId=1,IsWall=true });
            else if(name!="tie_reverse" && (name.StartsWith("overlap_")||crossing||tied)) result.Add(upper);
            result.Add(new Foothold(3,-200,baseY+400,200,baseY+400) { Layer=6 });
            if(name=="tie_rehash") for(int id=4;id<=24;id++) result.Add(new Foothold(id,-100,0,100,0) { Layer=id%7 });
            return result;
        }
        private static bool Airborne(string name) => name=="overlap_fall"||name.StartsWith("tie")||
            name=="crossing_fall"||name=="negative_column"||name=="ceiling";
        private static double X(string name) => name=="overlap_walk"?-90:
            name=="overlap_ledge"||name=="wall_other_layer"?90:name=="crossing_fall"?-60:
            name=="crossing_walk"?-40:name=="negative_column"?-99.75:
            name=="left_boundary"?-170:name=="right_boundary"?170:0;
        private static double Y(string name) => name=="overlap_ledge"||name=="overlap_drop"?-80:
            name=="crossing_walk"?-20:name=="crossing_fall"?-180:name=="ceiling"?-295:
            Airborne(name)?-120:name=="high_map"?4500:0;
        private static double[][] Rows(string name)
        {
            var path=Path.Combine(Directory.GetCurrentDirectory(),"Assets/Scripts/Tests/GameLogic/Fixtures/HeavenTerrain.csv");
            var rows=File.ReadLines(path).Where(l => l.StartsWith(name+",")).Select(l =>
                l.Split(',').Skip(1).Select(v => double.Parse(v,CultureInfo.InvariantCulture)).ToArray()).ToArray();
            Assert.That(rows.Length,Is.EqualTo(160));
            return rows;
        }
        private static void Compare(double[] r,double x,double y,double vx,double vy,bool onGround,int foothold,int layer,double tolerance)
        {
            Assert.That(x,Is.EqualTo(r[1]).Within(tolerance),"X tick "+r[0]);
            Assert.That(y,Is.EqualTo(r[2]).Within(tolerance),"Y tick "+r[0]);
            Assert.That(vx,Is.EqualTo(r[3]).Within(0.00001),"VX tick "+r[0]);
            Assert.That(vy,Is.EqualTo(r[4]).Within(0.00001),"VY tick "+r[0]);
            Assert.That(onGround,Is.EqualTo(r[5]==1),"Ground tick "+r[0]);
            Assert.That(foothold,Is.EqualTo((int)r[6]),"Foothold tick "+r[0]);
            Assert.That(layer,Is.EqualTo((int)r[8]),"Layer tick "+r[0]);
        }
        [TestCaseSource(nameof(Scenarios))]
        public void SolverMatchesCppOverlappingTerrainAndBoundaries(string name)
        {
            var movement=new NormalMovement { X=X(name),Y=Y(name),OnGround=!Airborne(name),
                VSpeed=name=="ceiling"?-10:0,State=Airborne(name)?NormalMovement.Stance.Fall:NormalMovement.Stance.Stand };
            movement.SetTerrain(new NormalTerrain(Terrain(name)));
            foreach(var r in Rows(name))
            {
                movement.Step(r[9]==1,r[10]==1,r[11]==1,r[12]==1,NormalMovement.WalkForce(100),NormalMovement.JumpForce(120));
                Compare(r,movement.X,movement.Y,movement.HSpeed,movement.VSpeed,movement.OnGround,
                    movement.FootholdId,movement.FootholdLayer,0.00001);
                Assert.That((int)movement.State,Is.EqualTo((int)r[7]),"Stance tick "+r[0]);
            }
        }
        [TestCaseSource(nameof(Scenarios))]
        public void RuntimePlayerMatchesCppOverlappingTerrainAndBoundaries(string name)
        {
            var service=new FootholdService(); service.LoadFootholds(Terrain(name));
            var player=new Player(service) { Position=new Vector2((float)(X(name)/100),(float)(-Y(name)/100)+Player.Height/2),
                IsGrounded=!Airborne(name),Velocity=new Vector2(0,name=="ceiling"?12.5f:0) };
            var map=new MapData();
            foreach(var r in Rows(name))
            {
                player.MoveLeft(r[9]==1); player.MoveRight(r[10]==1); player.Crouch(r[11]==1);
                if(r[12]==1) player.Jump(); else player.ReleaseJump();
                player.UpdatePhysics(0.008f,map);
                Compare(r,player.Position.X*100,-(player.Position.Y-Player.Height/2)*100,
                    NormalMovement.ToTickSpeed(player.Velocity.X),-NormalMovement.ToTickSpeed(player.Velocity.Y),
                    player.IsGrounded,player.CurrentFootholdId,player.CurrentFootholdLayer,0.002);
            }
        }
    }
}
